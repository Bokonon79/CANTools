namespace CANLib
{
  public class SSV_Log<T> : DataWithColumns, Log
    where T : SSV_Log<T>, new()
  {
    protected SSV_Log() : base(SeparatorType.Space)
    {
    }
  }
}
