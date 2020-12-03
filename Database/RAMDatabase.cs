using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Collections;
using System.Threading.Tasks;
using PokerBot.Definitions;

namespace PokerBot.Database
{
  /// <summary>
  /// In memory alternative to using sql database. Give phenomenal performance improvments.
  /// </summary>
  partial class RAMDatabase
  {
#if databaseLogging
        private static readonly ILog databaseLogger = LogManager.GetLogger(typeof(RAMDatabase));
#endif

    #region ClassObjects
    /// <summary>
    /// Key is TableId
    /// </summary>
    protected SortedDictionary<long, databaseCache.pokerTable> pokerTables;

    /// <summary>
    /// Used to convert local PokerTableId to realDatabaseId if we are writing to the database
    /// Key is LocalPokerTableId
    /// </summary>
    protected Dictionary<long, long> localPokerTableIdToDatabaseTableId;

    /// <summary>
    /// Key is PokerClientId, PlayerId
    /// </summary>
    protected Dictionary<PokerClients, Dictionary<long, PokerPlayer>> pokerPlayers;

    /// <summary>
    /// Key is TableId, Then HandId
    /// </summary>
    protected SortedDictionary<long, SortedDictionary<long, databaseCache.pokerHand>> pokerHands;

    /// <summary>
    /// Key is HandId, Then PlayerId
    /// </summary>
    protected Dictionary<long, Dictionary<long, databaseCache.holeCard>> holeCards;

    /// <summary>
    /// Keys is HandId
    /// </summary>
    protected SortedDictionary<long, List<databaseCache.handAction>> handActions;

    protected long pokerTableCounter = 1;
    protected object pokerTableCounterLocker = new object();

    protected long pokerHandCounter = 1;
    protected object pokerHandCounterLocker = new object();

    protected object tableLocker = new object();
    protected object handLocker = new object();
    protected object actionLocker = new object();
    protected object holeCardLocker = new object();

    //Used to speed up use of the database snapshot
    protected object databaseSnapshotLocker = new object();
    protected DatabaseSnapshot currentDatabaseSnapshot;
    protected volatile bool databaseChangesSinceLastSnapshot = true;

    protected object runningSubmitLocker = new object();
    protected int runningSubmitCounter = 0;

    protected bool useRAMOnly = true;
    public bool UseRAMOnly
    {
      get { return useRAMOnly; }
    }

    #region RAMDatabase Protected Classes
    /// <summary>
    /// PokerHandInfo object is used as a container for incoming data
    /// </summary>
    protected class PokerHandInfo
    {
      public databaseCache.pokerHand pokerHand;
      public List<databaseCache.holeCard> holeCards;
      public List<databaseCache.handAction> handActions;

      public PokerHandInfo(databaseCache.pokerHand pokerHand, List<databaseCache.holeCard> holeCards, List<databaseCache.handAction> handActions)
      {
        this.pokerHand = pokerHand;
        this.holeCards = holeCards;
        this.handActions = handActions;
      }
    }

    /// <summary>
    /// An object which contains instances of RAMDatabase data lists. Makes passing all lists around in one go easier.
    /// </summary>
    public class RAMDatabaseDataLists
    {
      public SortedDictionary<long, databaseCache.pokerTable> pokerTablesSnapshot;
      public Dictionary<PokerClients, Dictionary<long, PokerPlayer>> pokerPlayersSnapshot;
      public SortedDictionary<long, SortedDictionary<long, databaseCache.pokerHand>> pokerHandsSnapshot;
      public Dictionary<long, Dictionary<long, databaseCache.holeCard>> holeCardsSnapshot;
      public SortedDictionary<long, List<databaseCache.handAction>> handActionsSnapshot;
    }

    /// <summary>
    /// Creates a database snapshot. Snapshot is protected by the enter and leave snapshot methods
    /// </summary>
    public class DatabaseSnapshot
    {
      protected DateTime snapshotCreationTime;
      protected object locker = new object();
      protected int snapshotUseCounter;

      protected RAMDatabaseDataLists dataLists = new RAMDatabaseDataLists();

      /// <summary>
      /// Once we have created a snapshot we can store method return values in that snapshot
      /// </summary>
      protected Dictionary<string, object> snapshotMethodReturnCache = new Dictionary<string, object>();
      protected object snapshotMethodReturnCacheLocker = new object();

      /// <summary>
      /// Create a database snapshot
      /// </summary>
      /// <param name="currentRAMDatabase"></param>
      public DatabaseSnapshot(RAMDatabase currentRAMDatabase)
      {
        //If we set the bool to false here and the database changes
        //while we are copying the lists we happy for the value to go back to true
        //For now anyway - Thread Safety sucks balls for speed!
        currentRAMDatabase.databaseChangesSinceLastSnapshot = false;

        this.snapshotCreationTime = DateTime.Now;

        dataLists.pokerTablesSnapshot = currentRAMDatabase.ShallowCloneTables();
        dataLists.pokerPlayersSnapshot = currentRAMDatabase.ShallowClonePlayers();
        dataLists.pokerHandsSnapshot = currentRAMDatabase.ShallowCloneHands();
        dataLists.holeCardsSnapshot = currentRAMDatabase.ShallowCloneHoleCads();
        dataLists.handActionsSnapshot = currentRAMDatabase.ShallowCloneActions();
      }

      public object GetSnapshotCacheValue(string callingMethod)
      {
        object returnObject;

        lock (snapshotMethodReturnCacheLocker)
        {
          if (snapshotMethodReturnCache.ContainsKey(callingMethod))
            returnObject = snapshotMethodReturnCache[callingMethod];
          else
            returnObject = null;
        }

        return returnObject;
      }

      public void AddSnapshotCacheValue(string callingMethod, object cacheValue)
      {
        lock (snapshotMethodReturnCacheLocker)
        {
          //If this is the first entry we can just throw it straight in
          if (!snapshotMethodReturnCache.ContainsKey(callingMethod))
            snapshotMethodReturnCache.Add(callingMethod, cacheValue);
          else
          {
            //If we tried to add something to the dictionary but it was already there we can do something clever
            //At the moment we will only worry about dictioanries with long keys
            if (snapshotMethodReturnCache[callingMethod] is IDictionary)
            {
              //The dictionary for the current method
              Dictionary<long, object> dictionary = snapshotMethodReturnCache[callingMethod] as Dictionary<long, object>;

              if (dictionary == null)
                throw new Exception("Attempted to add an unhandled snapshot cache type.");

              //The new entry we are trying to merge
              KeyValuePair<long, object> dictionaryNewEntry = (cacheValue as Dictionary<long, object>).First();

              if (dictionaryNewEntry.Value is Dictionary<bool, object>)
              {
                //Check to see if we already have an entry for this player
                if (dictionary.ContainsKey(dictionaryNewEntry.Key))
                {
                  //If we do we need to merge in the new value
                  KeyValuePair<bool, object> newSubEntry = (dictionaryNewEntry.Value as Dictionary<bool, object>).First();
                  Dictionary<bool, object> dictionaryForPlayerId = dictionary[dictionaryNewEntry.Key] as Dictionary<bool, object>;

                  if (!dictionaryForPlayerId.ContainsKey(newSubEntry.Key))
                    dictionaryForPlayerId.Add(newSubEntry.Key, newSubEntry.Value);
                }
                else
                  //If not we can just add the entry
                  dictionary.Add(dictionaryNewEntry.Key, dictionaryNewEntry.Value);
              }
              else
              {
                //We need to double check it has not already been added
                //Else we just assume it's a single object and can add it
                if (!dictionary.ContainsKey(dictionaryNewEntry.Key))
                  dictionary.Add(dictionaryNewEntry.Key, dictionaryNewEntry.Value);
              }
            }
            else
              throw new Exception("Attempted to add an unhandled snapshot cache type.");
          }
        }
      }

      public DateTime SnapshotCreationTime
      {
        get { return snapshotCreationTime; }
      }

      /// <summary>
      /// Increments the snapshotUseCounter
      /// </summary>
      public RAMDatabaseDataLists EnterSnapshot()
      {
        lock (locker)
        {
          if (snapshotUseCounter < 0)
            throw new Exception("snapshotUseCounter should never be less than 0");

          snapshotUseCounter++;

          //Now return the pointer to the database lists
          return dataLists;
        }
      }

      /// <summary>
      /// Decrements the snapshotUseCounter
      /// </summary>
      public void LeaveSnapshot()
      {
        //We don't really care about this method for the moment but it's there just in case.
        //Throwing an error so that we don't implement it un necessarily.
        throw new NotImplementedException();

        lock (locker)
        {
          snapshotUseCounter--;

          if (snapshotUseCounter < 0)
            throw new Exception("snapshotUseCounter should never be less than 0");
        }
      }

      /// <summary>
      /// Returns true if the snapshotCounter is greater than 0
      /// </summary>
      /// <returns></returns>
      public bool SnapshotInUse()
      {
        lock (locker)
          return (snapshotUseCounter != 0);
      }
    }
    #endregion

    #endregion

    public RAMDatabase(bool useRAMOnly)
    {
      this.useRAMOnly = useRAMOnly;

      if (useRAMOnly)
      {
        pokerTables = new SortedDictionary<long, databaseCache.pokerTable>();
        localPokerTableIdToDatabaseTableId = new Dictionary<long, long>();
        pokerPlayers = new Dictionary<PokerClients, Dictionary<long, PokerPlayer>>();
        pokerHands = new SortedDictionary<long, SortedDictionary<long, databaseCache.pokerHand>>();
        holeCards = new Dictionary<long, Dictionary<long, databaseCache.holeCard>>();
        handActions = new SortedDictionary<long, List<databaseCache.handAction>>();
      }

#if databaseLogging
            //If we are logging configure the logger
            ILoggerRepository repository = LogManager.GetRepository(Assembly.GetCallingAssembly());
            IBasicRepositoryConfigurator configurableRepository = repository as IBasicRepositoryConfigurator;

            PatternLayout layout = new PatternLayout();
            layout.ConversionPattern = "%level% [%thread%] - %message%newline";
            layout.ActivateOptions();

            FileAppender appender = new FileAppender();
            appender.Layout = layout;
            appender.Threshold = Level.Warn;
            appender.File = "RAMDatabase.csv";
            appender.AppendToFile = true;
            appender.ActivateOptions();
            configurableRepository.Configure(appender);
#endif
    }

    #region DictionaryShallowCloneMethods
    protected SortedDictionary<long, databaseCache.pokerTable> ShallowCloneTables()
    {
      SortedDictionary<long, databaseCache.pokerTable> returnDictionary = new SortedDictionary<long, databaseCache.pokerTable>();

      lock (tableLocker)
      {
        foreach (var table in pokerTables)
          returnDictionary.Add(table.Key, table.Value);
      }

      return returnDictionary;
    }

    protected Dictionary<PokerClients, Dictionary<long, PokerPlayer>> ShallowClonePlayers()
    {
      Dictionary<PokerClients, Dictionary<long, PokerPlayer>> returnDictionary = new Dictionary<PokerClients, Dictionary<long, PokerPlayer>>();

      lock (playerLocker)
      {
        foreach (PokerClients client in pokerPlayers.Keys)
        {
          returnDictionary.Add(client, new Dictionary<long, PokerPlayer>());
          foreach (var player in pokerPlayers[client])
            returnDictionary[client].Add(player.Key, player.Value);
        }
      }

      return returnDictionary;
    }

    protected SortedDictionary<long, SortedDictionary<long, databaseCache.pokerHand>> ShallowCloneHands()
    {
      SortedDictionary<long, SortedDictionary<long, databaseCache.pokerHand>> returnDictionary = new SortedDictionary<long, SortedDictionary<long, databaseCache.pokerHand>>();

      lock (handLocker)
      {
        foreach (var table in pokerHands)
        {
          returnDictionary.Add(table.Key, new SortedDictionary<long, databaseCache.pokerHand>());
          foreach (var hands in table.Value)
            returnDictionary[table.Key].Add(hands.Key, hands.Value);
        }
      }

      return returnDictionary;
    }

    protected SortedDictionary<long, List<databaseCache.handAction>> ShallowCloneActions()
    {
      SortedDictionary<long, List<databaseCache.handAction>> returnDictionary = new SortedDictionary<long, List<databaseCache.handAction>>();

      lock (actionLocker)
      {
        foreach (var hand in handActions)
        {
          returnDictionary.Add(hand.Key, new List<databaseCache.handAction>());
          foreach (var action in hand.Value)
            returnDictionary[hand.Key].Add(action);
        }
      }

      return returnDictionary;
    }

    protected Dictionary<long, Dictionary<long, databaseCache.holeCard>> ShallowCloneHoleCads()
    {
      Dictionary<long, Dictionary<long, databaseCache.holeCard>> returnDictionary = new Dictionary<long, Dictionary<long, databaseCache.holeCard>>();

      lock (holeCardLocker)
      {
        foreach (var hand in holeCards)
        {
          returnDictionary.Add(hand.Key, new Dictionary<long, databaseCache.holeCard>());

          foreach (var holeCardSet in hand.Value)
            returnDictionary[hand.Key].Add(holeCardSet.Value.PlayerId, holeCardSet.Value);
        }
      }

      return returnDictionary;
    }
    #endregion

    public void ShutdownRAMDatabase()
    {
      throw new NotImplementedException("There is no need to call this method as threading no longer exists in the RAM database.");
    }

    /// <summary>
    /// Returns a new thread safe tableId
    /// </summary>
    /// <returns></returns>
    private long NewTableId()
    {
      long returnValue;

      if (useRAMOnly)
      {
        lock (pokerTableCounterLocker)
        {
          returnValue = pokerTableCounter;
          pokerTableCounter++;
        }
      }
      else
        throw new Exception("This method should not have be called.");

      return returnValue;
    }

    /// <summary>
    /// Returns a new thread safe handId
    /// </summary>
    /// <returns></returns>
    private long NewHandId()
    {
      long returnValue;

      if (useRAMOnly)
      {
        lock (pokerHandCounterLocker)
        {
          returnValue = pokerHandCounter;
          pokerHandCounter++;
        }
      }
      else
        throw new Exception("This method should not have be called.");

      return returnValue;
    }

    public int RunningSubmitCounter
    {
      get { lock (runningSubmitLocker) { return runningSubmitCounter; } }
    }

    /// <summary>
    /// Returns when the running submit counter reaches 0. Thread.Sleep(200) while it's >0.
    /// Default throws a timeout exception after 5 seconds.
    /// </summary>
    /// <param name="timeoutSeconds">Provide a timeout if other than default (5 secs)</param>
    public void WaitUntilCommitQueueIsEmpty(int timeoutSeconds = 5)
    {
      int currentCount = int.MaxValue;
      lock (runningSubmitLocker)
        currentCount = runningSubmitCounter;

      DateTime startTime = DateTime.Now;

      while (currentCount > 0)
      {
        if ((DateTime.Now - startTime).TotalSeconds > timeoutSeconds)
          throw new TimeoutException("Timeout waiting for RAM database commit queue to clear.");

        Thread.Sleep(100);

        lock (runningSubmitLocker)
          currentCount = runningSubmitCounter;
      }
    }

    public void NewHand(databaseCache.pokerHand pokerHand)
    {
      databaseChangesSinceLastSnapshot = true;

      if (useRAMOnly)
      {
        //Takes a hand and returns that hand with an attached handId
        pokerHand.HandId = NewHandId();
        lock (handLocker)
        {
          if (pokerHands.ContainsKey(pokerHand.TableId))
            pokerHands[pokerHand.TableId].Add(pokerHand.HandId, pokerHand);
          else
            pokerHands.Add(pokerHand.TableId, new SortedDictionary<long, databaseCache.pokerHand>() { { pokerHand.HandId, pokerHand } });
        }
      }
      else
      {
        throw new NotImplementedException();
      }
    }

    public void NewPokerTable(databaseCache.pokerTable pokerTable)
    {
      databaseChangesSinceLastSnapshot = true;

      if (useRAMOnly)
      {
        pokerTable.TableId = NewTableId();
        lock (tableLocker)
          pokerTables.Add(pokerTable.TableId, pokerTable);
      }
      else
      {
        throw new NotImplementedException();
      }
    }

    /// <summary>
    /// Returns a snapshot of the RAM database. If the usage when converted to an int is greater than 0
    /// </summary>
    /// <param name="snapshotUsage"></param>
    /// <returns></returns>
    public DatabaseSnapshot GetDatabaseSnapshot(databaseSnapshotUsage snapshotUsage)
    {
      DatabaseSnapshot returnSnapshot;

      if (!useRAMOnly)
        throw new Exception("Snapshots are not used when committing to the database and as such this method should not be called.");

      lock (databaseSnapshotLocker)
      {
        if (snapshotUsage == databaseSnapshotUsage.UseMostRecent || !databaseChangesSinceLastSnapshot)
        {
          //Else if just the most recent snapshot has been requested or there have been no changes
          if (currentDatabaseSnapshot == null)
          {
            returnSnapshot = new DatabaseSnapshot(this);
            currentDatabaseSnapshot = returnSnapshot;
          }
          else
          {
            returnSnapshot = currentDatabaseSnapshot;
          }
        }
        else if (snapshotUsage == databaseSnapshotUsage.CreateNew)
        {
          //If a new snapshot has been requested and there have been database changes since the last snapshot
          returnSnapshot = new DatabaseSnapshot(this);
          currentDatabaseSnapshot = returnSnapshot;
        }
        else if ((int)snapshotUsage > 0)
        {
          //Else if a recent snapshot has been requested within a specific timeframe
          if (currentDatabaseSnapshot.SnapshotCreationTime > DateTime.Now.AddSeconds(-(int)snapshotUsage))
          {
            if (currentDatabaseSnapshot == null)
            {
              returnSnapshot = new DatabaseSnapshot(this);
              currentDatabaseSnapshot = returnSnapshot;
            }
            else
            {
              returnSnapshot = currentDatabaseSnapshot;
            }
          }
          else
          {
            returnSnapshot = new DatabaseSnapshot(this);
            currentDatabaseSnapshot = returnSnapshot;
          }
        }
        else
          throw new Exception("Unable to determine correct database snapshot usage.");
      }

      return returnSnapshot;
    }

    /// <summary>
    /// Rather than take holeCards and actions one at a time we can optimize by just taking them once at the end of the hand
    /// </summary>
    /// <param name="holeCards"></param>
    /// <param name="handActions"></param>
    public void EndHand(databaseCache.pokerHand pokerHand, List<databaseCache.holeCard> holeCards, List<databaseCache.handAction> handActions)
    {
      databaseChangesSinceLastSnapshot = true;
      PokerHandInfo handInfo = new PokerHandInfo(pokerHand, holeCards, handActions);

      //So that we can keep an easy count on the number of current commit tasks we increment here
      lock (runningSubmitLocker)
        runningSubmitCounter++;

      //We use tasks to do the heavy lifting
      if (ConcurrencyMode.Concurrency == ConcurrencyMode.ConcurencyModel.MultiCore)
        Task.Factory.StartNew(HandlePokerHandInfoItem, handInfo);
      else
        HandlePokerHandInfoItem(handInfo);
    }

    /// <summary>
    /// The method called by the endhand task when it gets round to it
    /// </summary>
    /// <param name="currentPokerHandInfo"></param>
    protected void HandlePokerHandInfoItem(object currentPokerHandInfo)
    {
      //We can do one of two things.
      //1. Keep everything in RAM and forget about the database
      //2. Commit things to the database

      //For now we are just going to keep things in memory so that we can profile
      if (useRAMOnly)
        HandlePokerHandInfoItemRAMOnly((PokerHandInfo)currentPokerHandInfo);
      else
      {
        throw new NotImplementedException();
      }

      //Once we are done we can decrement the counter
      lock (runningSubmitLocker)
        runningSubmitCounter--;
    }

    /// <summary>
    /// Adds the hand information to the RAM database
    /// </summary>
    /// <param name="currentPokerHandInfo"></param>
    protected void HandlePokerHandInfoItemRAMOnly(PokerHandInfo currentPokerHandInfo)
    {
      //Add the holeCards
      lock (holeCardLocker)
      {
        holeCards.Add(currentPokerHandInfo.pokerHand.HandId, new Dictionary<long, databaseCache.holeCard>());

        //Add the holeCards
        foreach (databaseCache.holeCard cardSet in currentPokerHandInfo.holeCards)
          holeCards[currentPokerHandInfo.pokerHand.HandId].Add(cardSet.PlayerId, cardSet);
      }

      //Add the handActions
      lock (actionLocker)
        handActions.Add(currentPokerHandInfo.pokerHand.HandId, currentPokerHandInfo.handActions);
    }
  }
}
