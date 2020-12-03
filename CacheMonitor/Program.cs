using System;
using System.Windows.Forms;
using PokerBot.Database;
using System.IO;

namespace PokerBot.CacheMonitor
{
  static class Program
  {
    /// <summary>
    /// The main entry point for the application.
    /// </summary>
    [STAThread]
    static void Main(string[] args)
    {
      Application.EnableVisualStyles();
      Application.SetCompatibleTextRenderingDefault(false);
      databaseCache genericCache;

      if (args == null || args.Length != 1 || !File.Exists(args[0]))
      {
        genericCache = new databaseCacheClient(1, "Empty Cache", 10, 20, 2000, 9, Definitions.HandDataSource.Undefined);
      }
      else
        genericCache = databaseCache.DeSerialise(File.ReadAllBytes(args[0]));

      Application.Run(new CacheMonitor(genericCache, false, false));
    }
  }
}
