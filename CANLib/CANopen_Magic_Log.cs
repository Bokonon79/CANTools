﻿using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Text.RegularExpressions;

/*
"CANopen Magic Ultimate 9.40 build 5290 Trace Recording, generated on 12/13/2018 5:10:58 PM"
"$FileVersion=5$"
"$TimestampMode=Absolute$"

"Message Number","Time (ms)","Time","Excel Time","Count","ID","Flags","Message Type","Node","Details","Process Data","Data (Hex)","Data (Text)","Data (Decimal)","Length","Raw Message"
"0","0.000","8:09:42:48.7953090'",43447.7100146116,"","0x2E1","","Default: PDO","","Default: TPDO 2 of Node 0x61 (97)","","10 21 04 00 00 00 00 00 ",". ! . . . . . . ","U:0 S:0","8","10 21 04 00 00 00 00 00"
*/

// http://www.cpearson.com/Excel/datetime.htm
// Base is "January 00, 1900" and there is an artificial
// Lotus-compat day in the calculation.  Thus, "-2".
// The Ticks is in unused of 100 nanoseconds, so divide
// by 10 to produce the desired units of microseconds.
//
// Powershell example:
// ([DateTime]::Parse("January 01, 1900 Z")).AddDays(43447.7100146116 - 2)

namespace CANLib
{
  public class CANopen_Magic_Log : DataWithColumns, Log
  {
    public readonly static string[] ExpectedColumnNames =
    {
      "Message Number",
      "Time (ms)",
      "Time",
      "Excel Time",
      "Count",
      "ID",
      "Flags",
      "Message Type",
      "Node",
      "Details",
      "Process Data",
      "Data (Hex)",
      "Data (Text)",
      "Data (Decimal)",
      "Length",
      "Raw Message"
    };

    public CANopen_Magic_Log() : base(SeparatorType.Comma, true, false)
    {
    }

    public uint FileVersion
    {
      get;
      private set;
    }

    public DateTime GeneratedTimestamp
    {
      get;
      private set;
    }

    public string ProgramVersion
    {
      get;
      private set;
    }

    public string TimestampMode
    {
      get;
      private set;
    }

    protected override bool LoadValidate()
    {
      return (
        base.LoadValidate() &&
        ValidateColumnNames(ExpectedColumnNames)
        );
    }

    static readonly Regex regexFileVersion = new Regex(
      @"^""\$FileVersion=(\d+)\$""$",
      RegexOptions.Compiled | RegexOptions.CultureInvariant);
    void ReadFileVersion(StreamReader reader)
    {
      string line = reader.ReadLine();
      Match matchFileVersion = regexFileVersion.Match(line);
      if (!matchFileVersion.Success)
      {
        throw new DataMisalignedException();
      }
      Debug.Assert(matchFileVersion.Groups.Count == 2);
      FileVersion = uint.Parse(matchFileVersion.Groups[1].Value);
    }

    static readonly Regex regexProgramAndGeneratedTimestamp = new Regex(
      @"^""(CANopen Magic Ultimate \d+\.\d+ build \d+) Trace Recording, generated on (\d\d/\d\d/\d\d\d\d \d+:\d\d:\d\d [AP]M)""$",
      RegexOptions.Compiled | RegexOptions.CultureInvariant);
    void ReadProgramAndGeneratedTimestamp(StreamReader reader)
    {
      string line = reader.ReadLine();
      Match matchProgramAndGeneratedTimestamp = regexProgramAndGeneratedTimestamp.Match(line);
      if (!matchProgramAndGeneratedTimestamp.Success)
      {
        throw new DataMisalignedException();
      }
      Debug.Assert(matchProgramAndGeneratedTimestamp.Groups.Count == 3);
      ProgramVersion = matchProgramAndGeneratedTimestamp.Groups[1].Value;
      GeneratedTimestamp = DateTime.Parse(matchProgramAndGeneratedTimestamp.Groups[2].Value);
    }

    static readonly Regex regexTimestampMode = new Regex(
      @"^""\$TimestampMode=(.+)\$""$",
      RegexOptions.Compiled | RegexOptions.CultureInvariant);
    void ReadTimestampMode(StreamReader reader)
    {
      string line = reader.ReadLine();
      Match matchTimestampMode = regexTimestampMode.Match(line);
      if (!matchTimestampMode.Success)
      {
        throw new DataMisalignedException();
      }
      Debug.Assert(matchTimestampMode.Groups.Count == 2);
      TimestampMode = matchTimestampMode.Groups[1].Value;
    }

    protected override void ReadCustomHeader(StreamReader streamReader)
    {
      ReadProgramAndGeneratedTimestamp(streamReader);
      ReadFileVersion(streamReader);
      ReadTimestampMode(streamReader);
      ReadBlankLine(streamReader);

      base.ReadCustomHeader(streamReader);
    }

    protected override void WriteCustomHeader(StreamWriter streamWriter)
    {
      streamWriter.WriteLine(@"""{0} Trace Recording, generated on {1}""",
        ProgramVersion,
        GeneratedTimestamp.ToString("MM/dd/yyyy h:mm:ss tt")
        );
      streamWriter.WriteLine(@"""$FileVersion={0}$""",
        FileVersion
        );
      streamWriter.WriteLine(@"""$TimestampMode={0}$""",
        TimestampMode
        );

      streamWriter.WriteLine();
    }

    protected override void WriteQueryQuoteOverride(
      ref HashSet<int> quotedHeaderColumnsOverride,
      ref HashSet<int> quotedDataColumnsOverride
      )
    {
      quotedHeaderColumnsOverride = new HashSet<int>();
      for (int i = 0; i < columns.Count; i++)
      {
        quotedHeaderColumnsOverride.Add(i);
      }

      quotedDataColumnsOverride = new HashSet<int>();
      for (int i = 0; i < columns.Count; i++)
      {
        if (i != 3)
        {
          quotedDataColumnsOverride.Add(i);
        }
      }
    }
  }
}
