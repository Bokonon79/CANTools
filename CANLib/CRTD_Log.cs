using System;
using System.Diagnostics;
using System.IO;
using System.Text.RegularExpressions;

/*
166.064000 CXX GVRET-PC Reverse Engineering Tool Output V138
166.064000 R11 0000021A FE 36 12 FE 69 05 07 AD 
*/

namespace CANLib
{
  public class CRTD_Log : DataWithColumns, Log
  {
    public readonly static string[] AssumedColumnNames =
    {
      "Time",
      "Node",
      "MessageID",
      "DataBytes"
    };

    public CRTD_Log() : base(SeparatorType.Space, false, true)
    {
    }

    public string ProgramVersion
    {
      get;
      private set;
    }

    public UInt64 StartMicroseconds
    {
      get;
      private set;
    }

    protected override bool LoadValidate()
    {
      return (
        base.LoadValidate() &&
        ValidateColumnNames(AssumedColumnNames)
        );
    }

    static readonly Regex regexStartAndProgram = new Regex(
      @"^(\d+\.\d+) CXX (GVRET-PC Reverse Engineering Tool Output V\d+)$",
      RegexOptions.Compiled | RegexOptions.CultureInvariant
      );
    void ReadStartAndProgram(StreamReader streamReader)
    {
      string line = streamReader.ReadLine();
      Match matchStartAndProgram = regexStartAndProgram.Match(line);
      if (!matchStartAndProgram.Success)
      {
        throw new DataMisalignedException();
      }
      Debug.Assert(matchStartAndProgram.Groups.Count == 3);

      StartMicroseconds = (UInt64)(double.Parse(matchStartAndProgram.Groups[1].Value) * 1000000);
      ProgramVersion = matchStartAndProgram.Groups[2].Value;
    }

    protected override void ReadCustomHeader(StreamReader streamReader)
    {
      ReadStartAndProgram(streamReader);

      SetColumnNames(AssumedColumnNames);

      base.ReadCustomHeader(streamReader);
    }

    protected override void WriteCustomHeader(StreamWriter streamWriter)
    {
      streamWriter.WriteLine("{0:0.000000} CXX {1}", ((double)StartMicroseconds)/1000000, ProgramVersion);
    }
  }
}
