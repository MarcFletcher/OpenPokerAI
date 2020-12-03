using System;
using System.Collections.Generic;
using System.Linq;
using System.IO;
using System.Threading;
using ProtoBuf;
using NetworkCommsDotNet.DPSBase;

namespace PokerBot.Database
{

  /// <summary>
  /// Controls all shared communication between client->database and server->database
  /// Maintains database table objects in memory for quicker access/search
  /// </summary>
  public abstract partial class databaseCache
  {
    /// <summary>
    /// Any RAM database is shared amongst all caches.
    /// </summary>
    internal static RAMDatabase databaseRAM;
    protected static object databaseRAMLocker = new object();
    protected static bool nonRAMDatabaseStarted = false;

    //Cache thread locker
    //Any function which adds data to the cache must take an exclusive lock on this object (i.e. clientCache)
    //Read functions do not take a lock because they are not adding information
    protected object locker = new object();

    //Advanced multithreading lock counters.
    public volatile int readLockCounter = 0;
    public volatile int writeLockCounter = 0;
    public volatile Thread currentWriteThread;

    protected object methodReturnValueCacheLocker = new object();
    protected Dictionary<string, object> methodReturnValueCache = new Dictionary<string, object>();

    //Each cache gets 
    public static Random randGen;

    /// <summary>
    /// This is for serialisation only
    /// </summary>
    protected databaseCache()
    {
      //Put nothing here
    }

    /// <summary>
    /// Initialise a new empty cache. This takes an unused int parameter so that the private paramaterless consutrctor can be used for serialisation.
    /// </summary>
    protected void baseConstructor(Random randomGen)
    {
      tablePlayers = new List<tablePlayer>();
      pokerPlayers = new List<pokerPlayer>();
      pokerHands = new List<pokerHand>();
      holeCards = new List<holeCard>();
      handActions = new List<handAction>();

      databaseCache.randGen = randomGen;

      lock (databaseRAMLocker)
      {
        if (databaseRAM == null)
        {
          nonRAMDatabaseStarted = true;
          databaseRAM = new RAMDatabase(false);
        }
      }
    }

    public static void InitialiseRAMDatabase()
    {
      lock (databaseRAMLocker)
      {
        if (nonRAMDatabaseStarted)
          throw new Exception("Cannot initialise the RAM database if main database connections have been established.");
        else
          databaseRAM = new RAMDatabase(true);
      }
    }

    public static void ResetRAMDatabase()
    {
      lock (databaseRAMLocker)
      {
        if (nonRAMDatabaseStarted || databaseRAM == null)
          throw new Exception("RAM database cannot be reset if it has not yet been initialised or being used in nonRAM mode.");
        else
          databaseRAM = new RAMDatabase(true);
      }
    }

    public static void WaitUntilRAMCommitQueueIsEmpty(int timeoutSeconds = 5)
    {
      lock (databaseRAMLocker)
        if (databaseRAM != null)
          databaseRAM.WaitUntilCommitQueueIsEmpty(timeoutSeconds);
    }

    /// <summary>
    /// Returns true if the RAMDatabase has been initialised
    /// </summary>
    public static bool RAMDatabaseInitialised
    {
      get { lock (databaseRAMLocker) { return (databaseRAM != null); } }
    }

    /// <summary>
    /// Serialise the current cache and return a byte array
    /// </summary>
    /// <returns></returns>
    public byte[] Serialise()
    {
      byte[] returnArray;

      startRead();
      returnArray = DPSManager.GetDataSerializer<ProtobufSerializer>().SerialiseDataObject<databaseCache>(this).ThreadSafeStream.ToArray();
      endRead();

      return returnArray;
    }

    /// <summary>
    /// Saves the current cache to disk at saveLocation with fileName.FBPcache
    /// </summary>
    /// <param name="saveLocation">The directory where to save the file. Must end in \\ or be blank meaning current working directory.</param>
    /// <param name="fileName">The filename to use for this cache.</param>
    public void SaveToDisk(string saveLocation, string fileName)
    {
      File.WriteAllBytes(saveLocation + fileName + ".FBPcache", this.Serialise());
    }

    /// <summary>
    /// DeSerialise the cache, resets ALL current cache data!
    /// </summary>
    /// <param name="cacheData"></param>
    /// <returns></returns>
    public static databaseCache DeSerialise(byte[] cacheData)
    {
      try
      {
        //return FBPSerialiser.DeserialiseDataObject<databaseCache>(cacheData);
        return DPSManager.GetDataSerializer<ProtobufSerializer>().DeserialiseDataObject<databaseCache>(cacheData);
      }
      catch (Exception)
      {
        throw new Exception("Unable to deserialise cache from provided bytes.");
      }
    }

    [ProtoAfterDeserialization]
    private void CheckForNullLists()
    {
      //Check for null lists
      if (tablePlayers == null)
        this.tablePlayers = new List<tablePlayer>();
      if (pokerHands == null)
        this.pokerHands = new List<pokerHand>();
      if (holeCards == null)
        this.holeCards = new List<holeCard>();
      if (handActions == null)
        this.handActions = new List<handAction>();
      if (pokerPlayers == null)
        this.pokerPlayers = new List<pokerPlayer>();
    }

    public void Shutdown(bool waitForDatabaseCommitsToComplete = false)
    {
      if (waitForDatabaseCommitsToComplete)
        databaseRAM.WaitUntilCommitQueueIsEmpty(600);
    }

    #region advancedMultiThreadingLocks

    protected void addMethodReturnValue(string key, object returnValue)
    {
      //We only need to lock on the write for thread safety.
      lock (methodReturnValueCacheLocker)
      {
        if (methodReturnValueCache != null)
          if (!methodReturnValueCache.ContainsKey(key))
            methodReturnValueCache.Add(key, returnValue);
      }
    }

    /// <summary>
    /// Starts a cache read operation. A cache read may be started without or inside a read operation.
    /// Returns null once complete if a cached return value does not exist.
    /// Returns the cached object if it does exist.
    /// </summary>
    public object startRead(string callingMethodName)
    {
      do
      {
        while (currentWriteThread != Thread.CurrentThread && currentWriteThread != null)
          Thread.Sleep(1);

        lock (locker)
        {
          if (currentWriteThread == null || currentWriteThread == Thread.CurrentThread)
          {
            readLockCounter++;

            if (callingMethodName != null)
            {
              lock (methodReturnValueCacheLocker)
              {
                if (methodReturnValueCache != null)
                  if (methodReturnValueCache.ContainsKey(callingMethodName))
                    return methodReturnValueCache[callingMethodName];
              }
            }

            return null;
          }
        }

      } while (currentWriteThread != null && currentWriteThread != Thread.CurrentThread);

      return null;
    }

    public void startRead()
    {
      startRead(null);
    }

    /// <summary>
    /// Ends a read.
    /// </summary>
    public void endRead()
    {
      lock (locker)
      {
        if (readLockCounter < 1)
          throw new Exception("ReadLockCounter is less than 1.");

        readLockCounter--;
      }
    }

    /// <summary>
    /// Starts a cache write operation. A write operation should never be started within an existing read (within the same thread).
    /// A new write operation may be started within an existing write.
    /// </summary>
    protected void startWrite()
    {
      do
      {

        while ((currentWriteThread != Thread.CurrentThread && currentWriteThread != null) || readLockCounter > 0)
          Thread.Sleep(1);

        lock (locker)
        {
          //if ((currentWriteThread == null || currentWriteThread == Thread.CurrentThread) && (readLockCounter==0 || (currentWriteThread == Thread.CurrentThread || currentWriteThread == null)))
          if ((currentWriteThread == null || currentWriteThread == Thread.CurrentThread) && readLockCounter == 0)
          {
            currentWriteThread = Thread.CurrentThread;
            writeLockCounter++;

            //We now reset the dictionary because we are about to change something in the cache.
            lock (methodReturnValueCacheLocker)
              methodReturnValueCache = null;
          }
        }

      } while (currentWriteThread != Thread.CurrentThread);
    }

    /// <summary>
    /// Ends a write lock. If this is the most outer write lock taken by this thread, other threads may now write.
    /// </summary>
    protected void endWrite()
    {
      lock (locker)
      {
        //Release the write lock
        writeLockCounter--;

        if (writeLockCounter == 0)
        {
          currentWriteThread = null;

          lock (methodReturnValueCacheLocker)
            methodReturnValueCache = new Dictionary<string, object>();
        }
      }
    }

    #endregion advancedMultiThreadingLocks

    #region cacheCleanUp

    /// <summary>
    /// Removes the last n hand actions. Does not remove cards if going past game round stage.
    /// </summary>
    /// <param name="n"></param>
    public void UndoNHandActions(int n)
    {
      startWrite();

      //Undo the action
      for (int i = n; i > 0; i--)
      {
        //Need to undo the action for calls and raises
        //throw new NotImplementedException();

        //Then remove the action
        handActions.RemoveAt(handActions.Count - 1);
      }

      endWrite();
    }

    /// <summary>
    /// Remove all pokerHands from the cache only (integer reference to total hands played remains).
    /// </summary>
    public void purgePokerHands()
    {
      startWrite();
      numPokerHands += pokerHands.Count;

      //Creating a new list or clearing the old one seems to make little difference
      pokerHands = new List<pokerHand>();
      //pokerHands.Clear();

      endWrite();
    }

    /// <summary>
    /// Remove all holeCards from the cache only
    /// </summary>
    public void purgeHoleCards()
    {
      startWrite();

      //Creating a new list or clearing the old one seems to make little difference
      holeCards = new List<holeCard>();
      //holeCards.Clear();

      endWrite();
    }

    /// <summary>
    /// Remove all pokerActions from the cache only
    /// </summary>
    public void purgePokerActions()
    {
      startWrite();

      //Creating a new list or clearing the old one seems to make little difference
      handActions = new List<handAction>();
      //handActions.Clear();

      endWrite();
    }

    /// <summary>
    /// Remove all tablePlayers from the cache only
    /// </summary>
    public void purgeTablePlayers()
    {
      startWrite();

      //Creating a new list or clearing the old one seems to make little difference
      tablePlayers = new List<tablePlayer>();
      //tablePlayers.Clear();

      endWrite();
    }

    /// <summary>
    /// Remove all pokerPlayers from the cache only
    /// </summary>
    public void purgePokerPlayers()
    {
      startWrite();

      //Creating a new list or clearing the old one seems to make little difference
      pokerPlayers = new List<pokerPlayer>();
      //pokerPlayers.Clear();

      endWrite();
    }

    public void PurgeActionsCardsHandsEarlierThanCurrentHand()
    {
      startWrite();

      holeCards = (from current in holeCards
                   where current.HandId == getCurrentHandId()
                   select current).ToList();


      handActions = (from current in handActions
                     where current.HandId == getCurrentHandId()
                     select current).ToList();

      pokerHands = (from current in pokerHands
                    where current.HandId == getCurrentHandId()
                    select current).ToList();

      endWrite();
    }

    #endregion cacheCleanUp
  }


}
