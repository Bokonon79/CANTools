using System;
using System.Diagnostics;
using System.IO;
using System.Text.RegularExpressions;

/*
//---------------------------------
Microchip Technology Inc.
CAN BUS Analyzer
SavvyCAN Exporter
Logging Started: 23/9/2015 22:17:43
//---------------------------------
166064;RX;0x0000021A;8;0xFE;0x36;0x12;0xFE;0x69;0x05;0x07;0xAD;
*/

namespace CANLib
{
  public class Microchip_Log : DataWithColumns, Log
  {
    public readonly static string[] AssumedColumnNames =
    {
      "Time",
      "Tx/Rx",
      "MessageId",
      "Length",
      "DataBytes"
    };

    public Microchip_Log() : base(SeparatorType.Semicolon, false, false)
    {
    }

    public DateTime Start
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

    const string Divider = @"//---------------------------------";
    const string CAN_BUS_Analyzer = @"CAN BUS Analyzer";
    const string Microchip_Technology_Inc = @"Microchip Technology Inc.";
    const string SavvyCAN_Exporter = @"SavvyCAN Exporter";
    static readonly Regex regexStart = new Regex(
      @"^Logging Started: (\d+)/(\d+)/(\d+) (\d+):(\d+):(\d+)$",
      RegexOptions.Compiled | RegexOptions.CultureInvariant
      );
    protected override void ReadCustomHeader(StreamReader streamReader)
    {
      ReadFixedString(streamReader, Divider);
      ReadFixedString(streamReader, Microchip_Technology_Inc);
      ReadFixedString(streamReader, CAN_BUS_Analyzer);
      ReadFixedString(streamReader, SavvyCAN_Exporter);

      string line = streamReader.ReadLine();
      Match matchStart = regexStart.Match(line);
      if (!matchStart.Success)
      {
        throw new DataMisalignedException();
      }
      Debug.Assert(matchStart.Groups.Count == 7);
      int day = int.Parse(matchStart.Groups[1].Value);
      int month = int.Parse(matchStart.Groups[2].Value);
      int year = int.Parse(matchStart.Groups[3].Value);
      int hour = int.Parse(matchStart.Groups[4].Value);
      int minute = int.Parse(matchStart.Groups[5].Value);
      int second = int.Parse(matchStart.Groups[6].Value);
      Start = new DateTime(year, month, day, hour, minute, second, 0, DateTimeKind.Utc);

      ReadFixedString(streamReader, Divider);

      SetColumnNames(AssumedColumnNames);

      base.ReadCustomHeader(streamReader);
    }

    protected override void WriteCustomHeader(StreamWriter streamWriter)
    {
      streamWriter.WriteLine(Divider);
      streamWriter.WriteLine(Microchip_Technology_Inc);
      streamWriter.WriteLine(CAN_BUS_Analyzer);
      streamWriter.WriteLine(SavvyCAN_Exporter);
      streamWriter.WriteLine(string.Format(
        "Logging Started: {0}/{1}/{2} {3}:{4}:{5}",
        Start.Day,
        Start.Month,
        Start.Year,
        Start.Hour,
        Start.Minute,
        Start.Second
        ));
      streamWriter.WriteLine(Divider);
    }
  }
}
