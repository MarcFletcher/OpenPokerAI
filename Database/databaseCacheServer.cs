using System;

namespace PokerBot.Database
{
  public class databaseCacheServer : databaseCache
  {

    /// <summary>
    /// Create new empty server cache
    /// </summary>
    public databaseCacheServer()
        : base()
    {
      baseConstructor(new Random());
    }

  }
}
