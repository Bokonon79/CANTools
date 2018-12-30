using CANLib;
using System;
using System.Collections.Generic;
using System.IO;

namespace CANConvert
{
  class Program
  {
    static void Main(string[] args)
    {
      IEnumerable<string> files = Directory.EnumerateFiles(".", "*.csv");
      foreach (string fileSrc in files)
      {
        Console.WriteLine("R {0}", fileSrc);
        CANLib.CANopen_Magic_Log log = new CANopen_Magic_Log();
        log.Load(fileSrc);

        Console.WriteLine("C");
        CANLib.GVRET_Log logConverted = new GVRET_Log(log);

        string fileTgt = @"GVRET\" + Path.GetFileName(fileSrc);
        Console.WriteLine("W {0}", fileTgt);
        if (!Directory.Exists("GVRET"))
        {
          Directory.CreateDirectory("GVRET");
        }
        logConverted.Save(fileTgt);
      }
    }
  }
}
