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
  }
}
