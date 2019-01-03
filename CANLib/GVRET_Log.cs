using System;
using System.Collections.Generic;
using System.Text.RegularExpressions;

/*
Time Stamp,ID,Extended,Bus,LEN,D1,D2,D3,D4,D5,D6,D7,D8
166064000,0000021A,false,0,8,FE,36,12,FE,69,05,07,AD,
*/

namespace CANLib
{
  public class GVRET_Log : CSV_Log<GVRET_Log>
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

    public GVRET_Log()
    {
    }

    static Regex regexBusMasterTime = new Regex(
      @"^\d+:\d+:\d+:\d+$",
      RegexOptions.Compiled | RegexOptions.CultureInvariant
      );
    public GVRET_Log(BusMaster_Log logSrc)
    {
      SetColumnNames(ExpectedColumnNames);

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

    static readonly DateTime dtEpochBegin = DateTime.Parse("January 01, 1970 Z");
    public GVRET_Log(CANopen_Magic_Log logSrc)
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

    protected override bool LoadValidate()
    {
      return (
        base.LoadValidate() &&
        ValidateColumnNames(ExpectedColumnNames)
        );
    }
  }
}
