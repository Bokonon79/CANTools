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

    protected override bool LoadValidate()
    {
      return (
        base.LoadValidate() &&
        ValidateColumnNames(ExpectedColumnNames)
        );
    }
  }
}
