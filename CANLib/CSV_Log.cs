using System;
using System.IO;

namespace CANLib
{
  public class CSV_Log<T> : DataWithColumns, Log
    where T : CSV_Log<T>, new()
  {
    protected CSV_Log() : base(SeparatorType.Comma, true, false)
    {
    }

    protected void ReadBlankLine(StreamReader streamReader)
    {
      string line = streamReader.ReadLine();
      if (line.Length > 0)
      {
        throw new DataMisalignedException();
      }
    }
  }
}
