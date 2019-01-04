using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Text.RegularExpressions;

/*
Time Stamp,ID,Extended,Bus,LEN,D1,D2,D3,D4,D5,D6,D7,D8
166064000,0000021A,false,0,8,FE,36,12,FE,69,05,07,AD,
*/

namespace CANLib
{
  public class GVRET_Log : DataWithColumns, Log
  {
    public readonly static string[] ExpectedColumnNames =
    {
      "Time Stamp",
      "ID",
      "Extended",
      "Bus",
      "LEN",
      "D1",
      "D2",
      "D3",
      "D4",
      "D5",
      "D6",
      "D7",
      "D8"
    };

    public GVRET_Log() : base(SeparatorType.Comma, true, false)
    {
    }

    static readonly DateTime dtEpochBegin = DateTime.Parse("January 01, 1970 Z");

    static Regex regexBusMasterTime = new Regex(
      @"^(\d+):(\d+):(\d+):(\d+)$",
      RegexOptions.Compiled | RegexOptions.CultureInvariant
      );
    public GVRET_Log(BusMaster_Log logSrc) : this()
    {
      SetColumnNames(ExpectedColumnNames);

      TimeSpan timeSpanLast = TimeSpan.MinValue;

      data = new List<List<string>>();
      foreach (IEnumerable<string> rowSrcEnumerable in logSrc.Data)
      {
        List<string> rowSrc = new List<string>(rowSrcEnumerable);

        Match matchBusMasterTime = regexBusMasterTime.Match(rowSrc[logSrc.ColumnMap["Time"]]);
        if (!matchBusMasterTime.Success)
        {
          throw new DataMisalignedException();
        }
        Debug.Assert(matchBusMasterTime.Groups.Count == 5);

        int hour = int.Parse(matchBusMasterTime.Groups[1].Value);
        int minute = int.Parse(matchBusMasterTime.Groups[2].Value);
        int second = int.Parse(matchBusMasterTime.Groups[3].Value);
        int millisecond = int.Parse(matchBusMasterTime.Groups[4].Value);

        TimeSpan timeSpan = new TimeSpan(0, hour, minute, second, millisecond);
        if (timeSpan < timeSpanLast)
        {
          timeSpan += new TimeSpan(1, 0, 0, 0);
        }

        DateTime dt = logSrc.Start.Date + timeSpan;
        long microseconds = (dt - dtEpochBegin).Ticks / 10;

        List<string> dataFields = new List<string>();
        for (int i = logSrc.ColumnMap["DataBytes"]; i < rowSrc.Count; i++)
        {
          dataFields.Add(rowSrc[i]);
        }
        if (dataFields.Count > 8)
        {
          dataFields.RemoveRange(8, dataFields.Count - 8);
        }
        while (dataFields.Count < 8)
        {
          dataFields.Add("");
        }

        List<string> rowTgt = new List<string>
          {
            microseconds.ToString(),
            rowSrc[logSrc.ColumnMap["CAN ID"]],
            "false",
            rowSrc[logSrc.ColumnMap["Channel"]],
            rowSrc[logSrc.ColumnMap["DLC"]],
            dataFields[0],
            dataFields[1],
            dataFields[2],
            dataFields[3],
            dataFields[4],
            dataFields[5],
            dataFields[6],
            dataFields[7]
          };

        data.Add(rowTgt);

        timeSpanLast = timeSpan;
      }
    }

    public GVRET_Log(CANopen_Magic_Log logSrc) : this()
    {
      SetColumnNames(ExpectedColumnNames);

      data = new List<List<string>>();
      foreach (IEnumerable<string> rowSrcEnumerable in logSrc.Data)
      {
        List<string> rowSrc = new List<string>(rowSrcEnumerable);

        string[] rawMessageFields = rowSrc[logSrc.ColumnMap["Raw Message"]].Split(' ');
        List<string> dataFields = new List<string>(rawMessageFields);
        if (dataFields.Count > 8)
        {
          dataFields.RemoveRange(8, dataFields.Count - 8);
        }
        while (dataFields.Count < 8)
        {
          dataFields.Add("");
        }

        string rawID = rowSrc[logSrc.ColumnMap["ID"]];
        if (rawID.Length > 0)
        {
          double excelTime = double.Parse(rowSrc[logSrc.ColumnMap["Excel Time"]]);
          DateTime dt = DateTime.Parse("January 01, 1900 Z").AddDays(excelTime - 2);
          long microseconds = (dt - dtEpochBegin).Ticks / 10;

          List<string> rowTgt = new List<string>
          {
            microseconds.ToString(),
            "00000" + rawID.Substring(2),
            "false",
            "0",
            rowSrc[logSrc.ColumnMap["Length"]],
            dataFields[0],
            dataFields[1],
            dataFields[2],
            dataFields[3],
            dataFields[4],
            dataFields[5],
            dataFields[6],
            dataFields[7]
          };

          data.Add(rowTgt);
        }
      }
    }

    public GVRET_Log(Microchip_Log logSrc) : this()
    {
      SetColumnNames(ExpectedColumnNames);

      data = new List<List<string>>();
      foreach (IEnumerable<string> rowSrcEnumerable in logSrc.Data)
      {
        List<string> rowSrc = new List<string>(rowSrcEnumerable);

        List<string> dataFields = new List<string>();
        for (int i = 4; i < rowSrc.Count; i++)
        {
          string fieldString = rowSrc[i];
          if (fieldString.Length > 0)
          {
            fieldString = fieldString.Substring(2);
          }
          dataFields.Add(fieldString);
        }
        if (dataFields.Count > 8)
        {
          dataFields.RemoveRange(8, dataFields.Count - 8);
        }
        while (dataFields.Count < 8)
        {
          dataFields.Add("");
        }

        List<string> rowTgt = new List<string>
          {
            rowSrc[0],
            rowSrc[2].Substring(2),
            "false",
            "0",
            rowSrc[3],
            dataFields[0],
            dataFields[1],
            dataFields[2],
            dataFields[3],
            dataFields[4],
            dataFields[5],
            dataFields[6],
            dataFields[7]
          };

        data.Add(rowTgt);
      }
    }

    protected override bool LoadValidate()
    {
      return (
        base.LoadValidate() &&
        ValidateColumnNames(ExpectedColumnNames)
        );
    }
  }
}
