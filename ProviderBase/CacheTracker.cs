using System;
using System.Collections.Generic;
using System.Linq;
using PokerBot.Database;
using System.Threading;
using PokerBot.Definitions;

namespace PokerBot.AI.InfoProviders
{
  /// <summary>
  /// Used to determine the caches the AI is currently handling (allows for efficient provider caching)
  /// </summary>
  public class CacheTracker
  {
    /// <summary>
    /// Information about a player the AI is currently interacting with
    /// </summary>
    protected class tablePlayer
    {
      long playerId;
      long tableId;
      DateTime lastRefreshTime;
      long lastRefreshHandId;
      AIGeneration aiType = AIGeneration.NoAi_Human;
      string aiConfigStr;

      public tablePlayer(long playerId, long tableId, long handId)
      {
        this.playerId = playerId;
        this.tableId = tableId;
        lastRefreshTime = DateTime.Now;
        lastRefreshHandId = handId;

        //Determine a possible AI version for this player
        int aiTypeInt;
        databaseQueries.aiPlayerConfig(playerId, out aiTypeInt, out aiConfigStr);
        aiType = (AIGeneration)aiTypeInt;
      }

      public void RefreshTime()
      {
        lastRefreshTime = DateTime.Now;
      }

      public void Refresh(long handId)
      {
        lastRefreshTime = DateTime.Now;
        lastRefreshHandId = handId;
      }

      public long LastRefreshHandId
      {
        get { return lastRefreshHandId; }
      }

      public long PlayerId
      {
        get { return playerId; }
      }

      public long TableId
      {
        get { return tableId; }
      }

      public AIGeneration AiType
      {
        get { return aiType; }
      }

      public string AiConfigStr
      {
        get { return aiConfigStr; }
      }

      public DateTime LastRefreshTime
      {
        get { return lastRefreshTime; }
      }
    }

    /// <summary>
    /// A collection of active table caches which will allow collusion
    /// The actual cache merger process will be left out for now.
    /// </summary>
    protected class tableCache
    {
      long tableId;
      DateTime lastRefresh;
      databaseCache cache;

      public tableCache(long tableId, databaseCache cache)
      {
        this.tableId = tableId;
        this.cache = cache;
        lastRefresh = DateTime.Now;
      }

      public DateTime LastRefresh
      {
        get { return lastRefresh; }
      }

      public long TableId
      {
        get { return tableId; }
      }

      public databaseCache Cache
      {
        get { return cache; }
      }

      public void UpdateCache(databaseCache cache)
      {
        this.cache = cache;
        lastRefresh = DateTime.Now;
      }

      public void Refresh()
      {
        lastRefresh = DateTime.Now;
      }
    }

    protected object locker = new object();
    //protected List<tablePlayer> tablePlayers = new List<tablePlayer>();
    protected Dictionary<long, tableCache> tableCaches;

    protected Dictionary<long, tablePlayer> tablePlayersDict;

    protected Queue<UpdateItem> updateQueue;

    protected class UpdateItem
    {
      long[] playerIds;
      long tableId;
      long currentHandId;

      databaseCache cache;

      /// <summary>
      /// Creates a new update item but only using playerIds and tableId. Cache reference remains null
      /// </summary>
      /// <param name="playerIds"></param>
      /// <param name="tableId"></param>
      public UpdateItem(long[] playerIds, long tableId, long handId)
      {
        this.currentHandId = handId;
        this.playerIds = playerIds;
        this.tableId = tableId;
        this.cache = null;
      }

      /// <summary>
      /// Creates a new update item and creates a NEW COPY of the passed cache
      /// Can we quite slow ~ 1ms.
      /// </summary>
      /// <param name="cache"></param>
      public UpdateItem(databaseCache cache)
      {
        currentHandId = cache.getCurrentHandId();
        playerIds = cache.getSatInPlayerIds();
        tableId = cache.TableId;
        cache = databaseCache.DeSerialise(cache.Serialise());
      }

      public long[] PlayerIds
      {
        get { return playerIds; }
      }

      public long TableId
      {
        get { return tableId; }
      }

      public long HandId
      {
        get { return currentHandId; }
      }

      public databaseCache Cache
      {
        get { return cache; }
      }
    }

    /// <summary>
    /// The most recent handId the cache tracker has come across
    /// </summary>
    protected long latestHandId = 0;
    public long MostRecentHandId
    {
      get { lock (locker) { return latestHandId; } }
    }

    protected volatile bool closeWorkerThread;
    //protected volatile ManualResetEvent workerCloseWait;

    protected bool maintainRecentCacheHistory;

    /// <summary>
    /// The primary timeout method
    /// </summary>
    public static double activeTimeOutMins = 30;

    /// <summary>
    /// As an alternative to the time based timeout we can timeout things based on the distance the handId is from the current handId
    /// using numTables * handsPerTableTimeout.
    /// </summary>
    public static int handsPerTableTimeout = 15;

    private static CacheTracker instance;
    private static object instanceCreationLocker = new object();
    public static CacheTracker Instance
    {
      get
      {
        lock (instanceCreationLocker)
          if (instance == null || instance.closeWorkerThread)
            instance = new CacheTracker();

        return instance;
      }
    }

    private CacheTracker()
    {
      this.maintainRecentCacheHistory = false;
      updateQueue = new Queue<UpdateItem>();

      closeWorkerThread = false;

      tableCaches = new Dictionary<long, tableCache>();
      tablePlayersDict = new Dictionary<long, tablePlayer>();

      updateThread = new Thread(UpdateWorker);
      updateThread.Name = "CacheTrackerWorker";
      updateThread.Priority = ThreadPriority.BelowNormal;
      updateThread.Start();
    }

    /// <summary>
    /// Shuts down the cacheTracker and return when the worker thread is finished.
    /// </summary>
    public void Shutdown()
    {
      closeWorkerThread = true;
      updateThread.Join();
      //workerCloseWait.WaitOne();
    }

    private Thread updateThread;

    private void UpdateWorker()
    {
      UpdateItem currentItem;

      do
      {
        try
        {
          currentItem = null;

          lock (updateQueue)
          {
            if (updateQueue.Count > 0)
              currentItem = updateQueue.Dequeue();
          }

          if (currentItem != null)
          {
            Dictionary<long, tablePlayer> tablePlayersTempDict;

            //Create a copy of the tablePlayersDict
            lock (locker)
              tablePlayersTempDict =
              (from temp in tablePlayersDict
               select temp).ToDictionary(k => k.Key, k => k.Value);

            #region tablePlayer
            if (latestHandId < currentItem.HandId)
              latestHandId = currentItem.HandId;

            List<tablePlayer> alreadyAddedIdsList = (from current in tablePlayersTempDict.Values where currentItem.PlayerIds.Contains(current.PlayerId) select current).ToList();

            //Refresh players we already knew about
            for (int i = 0; i < alreadyAddedIdsList.Count; i++)
              alreadyAddedIdsList[i].Refresh(latestHandId);

            //Add players which don't yet exist
            long[] newPlayerIds = (currentItem.PlayerIds.Except((from current in alreadyAddedIdsList select current.PlayerId).ToArray())).ToArray();
            for (int i = 0; i < newPlayerIds.Length; i++)
              if (!tablePlayersTempDict.ContainsKey(newPlayerIds[i]))
                tablePlayersTempDict.Add(newPlayerIds[i], new tablePlayer(newPlayerIds[i], currentItem.TableId, latestHandId));

            //Remove anything from cachePlayers which is now too old
            int numActiveTables = AllActiveTableIds().Length;

            tablePlayersTempDict =
                (from current in tablePlayersTempDict
                 where current.Value.LastRefreshTime > DateTime.Now.AddMinutes(-activeTimeOutMins) || current.Value.LastRefreshHandId > latestHandId - (numActiveTables * CacheTracker.handsPerTableTimeout)
                 select current).ToDictionary(k => k.Key, k => k.Value);

            lock (locker)
              tablePlayersDict = tablePlayersTempDict;

            #endregion tablePlayer

            #region tableCache

            //Delete any caches which have timed out
            lock (locker)
              tableCaches =
                  (from current in tableCaches
                   where current.Value.LastRefresh > DateTime.Now.AddMinutes(-activeTimeOutMins)
                   select current).ToDictionary(k => k.Key, k => k.Value);

            #endregion tableCache
          }

          int queueCount = 0;
          lock (updateQueue)
            queueCount = updateQueue.Count;

          if (queueCount == 0)
            Thread.Sleep(100);
        }
        catch (Exception ex)
        {
          LogError.Log(ex, "CacheTracker");
        }
      } while (!closeWorkerThread);
    }

    public void Update(long playerId, databaseCache currentCache)
    {
      if (maintainRecentCacheHistory)
      {
        lock (updateQueue)
          updateQueue.Enqueue(new UpdateItem(currentCache));
      }
      else
      {
        //We need to guarantee the table is in the cacheTracker for the winRatio provider before this method returns
        lock (locker)
        {
          if (tableCaches.ContainsKey(currentCache.TableId))
            tableCaches[currentCache.TableId].Refresh();
          else
            tableCaches.Add(currentCache.TableId, new tableCache(currentCache.TableId, currentCache));
        }

        lock (updateQueue)
          updateQueue.Enqueue(new UpdateItem(currentCache.getSatInPlayerIds(), currentCache.TableId, currentCache.getCurrentHandId()));
      }
    }

    /// <summary>
    /// Reset the cache tracker as if it were newly started
    /// </summary>
    public void ResetCacheTracker()
    {
      lock (updateQueue)
        updateQueue = new Queue<UpdateItem>();

      lock (locker)
      {
        tableCaches = new Dictionary<long, tableCache>();
        tablePlayersDict = new Dictionary<long, tablePlayer>();
      }
    }

    /// <summary>
    /// Removes a player from the active tables list.
    /// </summary>
    /// <param name="tableId"></param>
    public void RemoveTablePlayer(long tableId, long playerId)
    {
      lock (locker)
      {
        tablePlayersDict =
                (from temp in tablePlayersDict
                 where temp.Value.TableId != tableId && temp.Value.PlayerId != playerId
                 select temp).ToDictionary(k => k.Key, k => k.Value);
      }
    }

    /// <summary>
    /// Returns the AI type to use for a given player and table.
    /// </summary>
    /// <param name="playerId"></param>
    /// <param name="tableId"></param>
    /// <returns></returns>
    public void PlayerAiConfig(long playerId, long tableId, out AIGeneration aiServerType, out string aiConfigStr)
    {
      lock (locker)
      {
        if (tablePlayersDict.ContainsKey(playerId))
        {
          aiServerType = tablePlayersDict[playerId].AiType;
          aiConfigStr = tablePlayersDict[playerId].AiConfigStr;
        }
        else
        {
          //If this config is not yet in the cache then get it directly from the database
          int aiTypeInt;

          databaseQueries.aiPlayerConfig(playerId, out aiTypeInt, out aiConfigStr);
          aiServerType = (AIGeneration)aiTypeInt;

          //Allthough an entry is added to the dictionary here it may be removed again by the cacheTrackerWorker when it replaces tablePlayersDict
          lock (locker)
            tablePlayersDict.Add(playerId, new tablePlayer(playerId, tableId, latestHandId));
        }
      }
    }

    public void CloseTableIds(long[] tableIdsToRemove)
    {
      lock (locker)
      {
        tableCaches = (from current in tableCaches
                       where !tableIdsToRemove.Contains(current.Value.TableId)
                       select current).ToDictionary(k => k.Key, k => k.Value);
      }
    }

    /// <summary>
    /// Returns the player ids of all adversary players.
    /// Will contain bot players if bot is adversary of another bot.
    /// </summary>
    /// <returns></returns>
    public long[] AllActivePlayers()
    {
      long[] returnIds;

      lock (locker)
      {
        var allPlayerIds =
            (from current in tablePlayersDict
             select current.Key).Distinct().ToArray();

        returnIds = allPlayerIds;
      }

      return returnIds;
    }

    /// <summary>
    /// Returns all active tableIds.
    /// </summary>
    /// <returns></returns>
    public long[] AllActiveTableIds()
    {
      long[] returnIds;

      lock (locker)
      {
        returnIds =
            (from current in tableCaches.Keys
             select current).ToArray();

        //returnIds = new long[allTableIds.Count()];

        //for (int i = 0; i < returnIds.Length; i++)
        //    returnIds[i] = allTableIds[i];
      }

      return returnIds;
    }

    /// <summary>
    /// Returns the activetableIds with an entry for each of the bot players on that table.
    /// [0] = TableId, [1] = PlayerId
    /// </summary>
    /// <returns></returns>
    public long[][] AllActiveTablesIncBotPlayers()
    {
      List<long[]> returnArray = new List<long[]>();
      long[] allTableIds = AllActiveTableIds();

      lock (locker)
      {

        for (int i = 0; i < allTableIds.Length; i++)
        {
          var players =
              (from current in tablePlayersDict
               where current.Value.TableId == allTableIds[i] && current.Value.AiType != AIGeneration.NoAi_Human
               select current.Value).ToList();

          if (players.Count() > 0)
          {
            for (int j = 0; j < players.Count(); j++)
            {
              returnArray.Add(new long[] { allTableIds[i], players[j].PlayerId });
            }
          }
        }
      }

      return returnArray.ToArray();
    }
  }
}
