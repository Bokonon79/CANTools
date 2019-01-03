using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;

namespace CANLib
{
  public class DataWithColumns
  {
    protected DataWithColumns(SeparatorType separator)
    {
      Separator = separator;
    }

    public IEnumerable<string> Columns
    {
      get
      {
        return columns;
      }
    }
    protected List<string> columns;

    public Dictionary<string, int> ColumnMap
    {
      get;
      private set;
    }

    public IEnumerable<IEnumerable<string>> Data
    {
      get
      {
        return data;
      }
    }
    protected List<List<string>> data;

    public enum SeparatorType
    {
      Comma,
      Space
    }
    public SeparatorType Separator
    {
      get;
      private set;
    }

    protected string BuildRow(
      List<string> fields,
      string separator,
      HashSet<int> quotedHeaderColumnsOverride = null
      )
    {
      StringBuilder stringBuilder = new StringBuilder(512);
      for (int i = 0; i < fields.Count; i++)
      {
        if (i > 0)
        {
          stringBuilder.Append(separator);
        }

        bool quoted = fields[i].Contains(@"""");
        if (quotedHeaderColumnsOverride != null)
        {
          quoted = quotedHeaderColumnsOverride.Contains(i);
        }

        if (quoted)
        {
          stringBuilder.Append('"');
        }
        stringBuilder.Append(fields[i].Replace(@"""", @""""""));
        if (quoted)
        {
          stringBuilder.Append('"');
        }
      }
      return stringBuilder.ToString();
    }

    public void Load(string filename, uint dataPointLimit = 0)
    {
      if (!File.Exists(filename))
      {
        throw new FileNotFoundException();
      }

      using (StreamReader streamReader = new StreamReader(filename, Encoding.UTF8))
      {
        Load(streamReader, dataPointLimit);
      }
    }

    public void Load(StreamReader streamReader, uint dataPointLimit = 0)
    {
      var currentCulture = Thread.CurrentThread.CurrentCulture;
      Thread.CurrentThread.CurrentCulture = CultureInfo.InvariantCulture;
      try
      {
        ReadCustomHeader(streamReader);
        ReadColumnHeader(streamReader);
        ReadData(streamReader, dataPointLimit);

        if (!LoadValidate())
        {
          throw new DataMisalignedException();
        }
      }
      finally
      {
        Thread.CurrentThread.CurrentCulture = currentCulture;
      }
    }

    protected virtual bool LoadValidate()
    {
      return true;
    }

    static readonly Regex regexSeparatedValuesByComma = new Regex(
      @"^(?:(?:^|,)(?:()|([^""][^,]*)|""((?:[^""]|"""")*)""))+$",
      RegexOptions.Compiled | RegexOptions.CultureInvariant);

    static readonly Regex regexSeparatedValuesBySpace = new Regex(
      @"^(?:(?:^| )(?:()|([^""][^ ]*)|""((?:[^""]|"""")*)""))+$",
      RegexOptions.Compiled | RegexOptions.CultureInvariant);

    List<string> ParseRow(string line)
    {
      List<string> fields = new List<string>();

      Regex regexSeparatedValues = null;
      switch (Separator)
      {
        case SeparatorType.Comma:
          regexSeparatedValues = regexSeparatedValuesByComma;
          break;
        case SeparatorType.Space:
          regexSeparatedValues = regexSeparatedValuesBySpace;
          break;
      }

      Match matchSeparatedValues = regexSeparatedValues.Match(line);
      if (!matchSeparatedValues.Success)
      {
        throw new DataMisalignedException();
      }
      Debug.Assert(matchSeparatedValues.Groups.Count == 4);

      SortedDictionary<int, string> mapOffsetToValue = new SortedDictionary<int, string>();
      for (int i = 1; i < matchSeparatedValues.Groups.Count; i++)
      {
        foreach (Capture capture in matchSeparatedValues.Groups[i].Captures)
        {
          mapOffsetToValue.Add(capture.Index, capture.Value.Replace(@"""""", @""""));
        }
      }

      foreach (KeyValuePair<int, string> pair in mapOffsetToValue)
      {
        fields.Add(pair.Value);
      }

      return fields;
    }

    void ReadColumnHeader(StreamReader streamReader)
    {
      string line = streamReader.ReadLine();
      SetColumnNames(ParseRow(line));
    }

    protected virtual void ReadCustomHeader(StreamReader streamReader)
    {
    }

    void ReadData(StreamReader streamReader, uint dataPointLimit = 0)
    {
      data = new List<List<string>>();

      int dataPointCount = 0;
      while (!streamReader.EndOfStream)
      {
        string line = streamReader.ReadLine();
        line = line.Trim();
        if (line.Length > 0)
        {
          List<string> row = ParseRow(line);
          data.Add(row);
          dataPointCount++;
          if ((dataPointLimit > 0) && (dataPointCount >= dataPointLimit))
          {
            return;
          }
        }
      }
    }

    public void Save(string filename)
    {
      string directory = Path.GetDirectoryName(filename);
      if (Directory.Exists(directory))
      {
        Directory.CreateDirectory(directory);
      }

      using (StreamWriter streamWriter = new StreamWriter(filename, false, new UTF8Encoding(false)))
      {
        Save(streamWriter);
      }
    }

    public void Save(StreamWriter streamWriter)
    {
      var currentCulture = Thread.CurrentThread.CurrentCulture;
      Thread.CurrentThread.CurrentCulture = CultureInfo.InvariantCulture;
      try
      {
        WriteCustomHeader(streamWriter);

        HashSet<int> quotedHeaderColumnsOverride = null;
        HashSet<int> quotedDataColumnsOverride = null;
        WriteQueryQuoteOverride(
          ref quotedHeaderColumnsOverride,
          ref quotedDataColumnsOverride
          );

        string separatorString = null;
        switch (Separator)
        {
          case SeparatorType.Comma:
            separatorString = ",";
            break;
          case SeparatorType.Space:
            separatorString = " ";
            break;
          default:
            Debug.Assert(false);
            break;
        }

        streamWriter.WriteLine(BuildRow(columns, separatorString, quotedHeaderColumnsOverride));
        foreach (List<string> row in data)
        {
          string rowString = BuildRow(row, separatorString, quotedDataColumnsOverride);
          streamWriter.WriteLine(rowString);
        }
      }
      finally
      {
        Thread.CurrentThread.CurrentCulture = currentCulture;
      }
    }

    protected void SetColumnNames(IEnumerable<string> columnNames)
    {
      columns = new List<string>(columnNames);

      ColumnMap = new Dictionary<string, int>();
      for (int i = 0; i < columns.Count; i++)
      {
        ColumnMap.Add(columns[i], i);
      }
    }

    protected bool ValidateColumnNames(string[] expectedColumnNames)
    {
      if (columns.Count != expectedColumnNames.Length)
      {
        return false;
      }

      for (int i = 0; i < expectedColumnNames.Length; i++)
      {
        if (columns[i] != expectedColumnNames[i])
        {
          return false;
        }
      }

      return true;
    }

    protected virtual void WriteCustomHeader(StreamWriter streamWriter)
    {
    }

    protected virtual void WriteQueryQuoteOverride(
      ref HashSet<int> quotedHeaderColumnsOverride,
      ref HashSet<int> quotedDataColumnsOverride
      )
    {
    }
  }
}
