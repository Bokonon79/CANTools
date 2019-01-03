using CANLib;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System.IO;
using System.Reflection;
using System.Text;

namespace CANLibUnitTest
{
  [TestClass]
  public class ReadWrite
  {
    const int bufferSize = 4096;
    uint dataPointLimit = 100;

    [TestMethod]
    public void RoundTrip()
    {
      Assembly executingAssembly = Assembly.GetExecutingAssembly();
      string[] resourceNames = executingAssembly.GetManifestResourceNames();
      foreach (string resourceName in resourceNames)
      {
        Stream streamSrc = executingAssembly.GetManifestResourceStream(resourceName);

        Stream streamTgt = new MemoryStream((int)streamSrc.Length);
        using (StreamWriter streamWriter = new StreamWriter(streamTgt, new UTF8Encoding(false), bufferSize, true))
        {
          using (StreamReader streamReader = new StreamReader(streamSrc, Encoding.UTF8, false, bufferSize, true))
          {
            CANLib.Log log = null;
            switch (resourceName)
            {
              case "CANLibUnitTest.Resources.BusMasterLog.log":
                log = new BusMaster_Log();
                break;

              case "CANLibUnitTest.Resources.GVRET_Log.csv":
                log = new GVRET_Log();
                break;

              case "CANLibUnitTest.Resources.post-186027.csv":
                log = new CANopen_Magic_Log();
                break;
            }
            log.Load(streamReader, dataPointLimit);
            log.Save(streamWriter);
          }

          streamWriter.Flush();
        }

        if (dataPointLimit == 0)
        {
          Assert.AreEqual(streamSrc.Length, streamTgt.Length);
        }

        streamSrc.Position = 0;
        streamTgt.Position = 0;

        using (StreamReader streamReaderTgt = new StreamReader(streamTgt, Encoding.UTF8))
        {
          using (StreamReader streamReaderSrc = new StreamReader(streamSrc, Encoding.UTF8))
          {
            while (!streamReaderTgt.EndOfStream)
            {
              string lineSrc = streamReaderSrc.ReadLine();
              string lineTgt = streamReaderTgt.ReadLine();
              Assert.AreEqual(lineSrc, lineTgt);
            }
            if (dataPointLimit == 0)
            {
              Assert.IsTrue(streamReaderSrc.EndOfStream);
            }
          }
        }
      }
    }
  }
}
