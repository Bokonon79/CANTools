namespace CANLib
{
  public class SSV_Log<T> : DataWithColumns, Log
    where T : SSV_Log<T>, new()
  {
    protected SSV_Log(bool headerRow, bool trailingSpaceInDataRows) : base(SeparatorType.Space, headerRow, trailingSpaceInDataRows)
    {
    }
  }
}
