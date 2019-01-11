using System.IO;

namespace CANLib
{
  public interface Log
  {
    uint DataCount
    {
      get;
    }

    void Load(string filename, uint dataPointLimit = 0);

    void Load(StreamReader streamReader, uint dataPointLimit = 0);

    void Save(string filename);

    void Save(StreamWriter streamWriter);
  }
}
