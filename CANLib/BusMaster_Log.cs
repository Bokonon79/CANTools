using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Text.RegularExpressions;

/*
***BUSMASTER Ver 2.4.0***
***PROTOCOL CAN***
***NOTE: PLEASE DO NOT EDIT THIS DOCUMENT***
***[START LOGGING SESSION]***
***START DATE AND TIME 23:9:2015 22:17:28:928***
***HEX***
***SYSTEM MODE***
***START CHANNEL BAUD RATE***
***CHANNEL 1 - Kvaser - Kvaser Leaf Light HS #0 (Channel 0), Serial Number- 0, Firmware- 0x00000037 0x00020000 - 500000 bps***
***END CHANNEL BAUD RATE***
***START DATABASE FILES (DBF/DBC)***
***END OF DATABASE FILES (DBF/DBC)***
***<Time><Tx/Rx><Channel><CAN ID><Type><DLC><DataBytes>***
22:20:14:992 Rx 0 0000021A s 8 FE 36 12 FE 69 05 07 AD 
*/

namespace CANLib
{
  public class BusMaster_Log : SSV_Log<BusMaster_Log>
  {
    public readonly static string[] ExpectedColumnNames =
    {
      "Time",
      "Tx/Rx",
      "Channel",
      "CAN ID",
      "Type",
      "DLC",
      "DataBytes"
    };

    public BusMaster_Log() : base(false, true)
    {
    }

    public IEnumerable<KeyValuePair<uint, string>> Channels
    {
      get { return channels; }
    }
    SortedDictionary<uint, string> channels = new SortedDictionary<uint, string>();

    public string ProgramVersion
    {
      get;
      private set;
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
        ValidateColumnNames(ExpectedColumnNames)
        );
    }

    static readonly Regex regexChannel = new Regex(
      @"^\*\*\*CHANNEL (\d+) - (.+)\*\*\*$",
      RegexOptions.Compiled | RegexOptions.CultureInvariant
      );
    const string EndChannelBaudRate = @"***END CHANNEL BAUD RATE***";
    const string StartChannelBaudRate = @"***START CHANNEL BAUD RATE***";
    void ParseChannelBaudRate(StreamReader streamReader)
    {
      ParseFixedString(streamReader, StartChannelBaudRate);

      // REVIEW: Support multiple channels.
      string line = streamReader.ReadLine();
      Match matchChannel = regexChannel.Match(line);
      if (!matchChannel.Success)
      {
        throw new DataMisalignedException();
      }
      Debug.Assert(matchChannel.Groups.Count == 3);
      channels.Add(uint.Parse(matchChannel.Groups[1].Value), matchChannel.Groups[2].Value);

      ParseFixedString(streamReader, EndChannelBaudRate);
    }

    static readonly Regex regexColumnNames = new Regex(
      @"^\*\*\*(?:<([^>]+)>)+\*\*\*$",
      RegexOptions.Compiled | RegexOptions.CultureInvariant
      );
    void ParseColumnNames(StreamReader streamReader)
    {
      string line = streamReader.ReadLine();
      Match matchColumnNames = regexColumnNames.Match(line);
      if (!matchColumnNames.Success)
      {
        throw new DataMisalignedException();
      }
      Debug.Assert(matchColumnNames.Groups.Count == 2);

      List<string> columnNames = new List<string>();
      foreach (Capture capture in matchColumnNames.Groups[1].Captures)
      {
        columnNames.Add(capture.Value);
      }

      SetColumnNames(columnNames);
    }

    const string EndDatabaseFiles = @"***END OF DATABASE FILES (DBF/DBC)***";
    const string StartDatabaseFiles = @"***START DATABASE FILES (DBF/DBC)***";
    void ParseDatabaseFiles(StreamReader streamReader)
    {
      ParseFixedString(streamReader, StartDatabaseFiles);
      ParseFixedString(streamReader, EndDatabaseFiles);
    }

    void ParseFixedString(StreamReader streamReader, string expected)
    {
      string line = streamReader.ReadLine();
      if (line != expected)
      {
        throw new DataMisalignedException();
      }
    }

    static readonly Regex regexProgram = new Regex(
      @"^\*\*\*(BUSMASTER Ver \d+\.\d+\.\d+)\*\*\*$",
      RegexOptions.Compiled | RegexOptions.CultureInvariant
      );
    void ParseProgram(StreamReader streamReader)
    {
      string line = streamReader.ReadLine();
      Match matchProgram = regexProgram.Match(line);
      if (!matchProgram.Success)
      {
        throw new DataMisalignedException();
      }
      Debug.Assert(matchProgram.Groups.Count == 2);

      ProgramVersion = matchProgram.Groups[1].Value;
    }

    static readonly Regex regexStart = new Regex(
      @"^\*\*\*START DATE AND TIME (\d+):(\d+):(\d+) (\d+):(\d+):(\d+):(\d+)\*\*\*$",
      RegexOptions.Compiled | RegexOptions.CultureInvariant
      );
    void ParseStartDateAndTime(StreamReader streamReader)
    {
      string line = streamReader.ReadLine();
      Match matchStart = regexStart.Match(line);
      if (!matchStart.Success)
      {
        throw new DataMisalignedException();
      }
      Debug.Assert(matchStart.Groups.Count == 8);

      int day = int.Parse(matchStart.Groups[1].Value);
      int month = int.Parse(matchStart.Groups[2].Value);
      int year = int.Parse(matchStart.Groups[3].Value);

      int hour = int.Parse(matchStart.Groups[4].Value);
      int minute = int.Parse(matchStart.Groups[5].Value);
      int second = int.Parse(matchStart.Groups[6].Value);
      int millisecond = int.Parse(matchStart.Groups[7].Value);

      Start = new DateTime(year, month, day, hour, minute, second, millisecond, DateTimeKind.Utc);
    }

    const string DoNotEdit = @"***NOTE: PLEASE DO NOT EDIT THIS DOCUMENT***";
    const string Hex = @"***HEX***";
    const string ProtocolCan = @"***PROTOCOL CAN***";
    const string StartLoggingSession = @"***[START LOGGING SESSION]***";
    const string SystemMode = @"***SYSTEM MODE***";
    protected override void ReadCustomHeader(StreamReader streamReader)
    {
      ParseProgram(streamReader);
      ParseFixedString(streamReader, ProtocolCan);
      ParseFixedString(streamReader, DoNotEdit);
      ParseFixedString(streamReader, StartLoggingSession);
      ParseStartDateAndTime(streamReader);
      ParseFixedString(streamReader, Hex);
      ParseFixedString(streamReader, SystemMode);
      ParseChannelBaudRate(streamReader);
      ParseDatabaseFiles(streamReader);
      ParseColumnNames(streamReader);

      base.ReadCustomHeader(streamReader);
    }

    protected override void WriteCustomHeader(StreamWriter streamWriter)
    {
      streamWriter.WriteLine("***{0}***", ProgramVersion);
      streamWriter.WriteLine(ProtocolCan);
      streamWriter.WriteLine(DoNotEdit);
      streamWriter.WriteLine(StartLoggingSession);
      streamWriter.WriteLine("***START DATE AND TIME {0}:{1}:{2} {3}:{4}:{5}:{6}***",
        Start.Day,
        Start.Month,
        Start.Year,
        Start.Hour,
        Start.Minute,
        Start.Second,
        Start.Millisecond
        );
      streamWriter.WriteLine(Hex);
      streamWriter.WriteLine(SystemMode);

      streamWriter.WriteLine(StartChannelBaudRate);
      foreach (KeyValuePair<uint, string> pair in Channels)
      {
        streamWriter.WriteLine("***CHANNEL {0} - {1}***",
          pair.Key,
          pair.Value
          );
      }
      streamWriter.WriteLine(EndChannelBaudRate);

      streamWriter.WriteLine(StartDatabaseFiles);
      streamWriter.WriteLine(EndDatabaseFiles);

      string columnNameString = "";
      foreach (string column in Columns)
      {
        columnNameString += "<" + column + ">";
      }
      streamWriter.WriteLine("***{0}***", columnNameString);
    }
  }
}
