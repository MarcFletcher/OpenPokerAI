using System;
using System.Collections.Generic;
using System.Linq;
using PokerBot.Definitions;
using System.Diagnostics;
using ProtoBuf;

namespace PokerBot.Database
{
  [ProtoContract]
  [ProtoInclude(100, typeof(databaseCacheClient))]
  public abstract partial class databaseCache
  {
    /// <summary>
    /// All data for this table is stored as lists
    /// </summary>
    [ProtoMember(1)]
    internal pokerTable currentPokerTable; //Each table maintains it's own instance of databaseCache to avoid database transaction processing problems.
    [ProtoMember(2)]
    internal List<tablePlayer> tablePlayers;
    [ProtoMember(3)]
    internal List<pokerPlayer> pokerPlayers;
    [ProtoMember(4)]
    internal List<pokerHand> pokerHands;
    [ProtoMember(5)]
    internal List<holeCard> holeCards;
    [ProtoMember(6)]
    internal List<handAction> handActions;

    [ProtoMember(7)]
    internal long numPokerHands = 0;

    [ProtoMember(8)]
    //Current hand is stored manually because it is referenced in a MASSIVE number of places (i.e. this is a performance fix). 
    //Previously we just stored handId but this is just as fast with more flexibility
    internal pokerHand currentPokerHand;

    [ProtoMember(9)]
    internal int totalActionCounter = 0;

    public int TotalActionCounter
    {
      get { return totalActionCounter; }
    }

    #region cacheDataClasses

    [ProtoContract]
    [DebuggerDisplay("{ToString()}")]
    internal class pokerTable
    {
      [ProtoMember(1)]
      long tableId;
      [ProtoMember(2)]
      short pokerClientId;
      [ProtoMember(3)]
      decimal littleBlind;
      [ProtoMember(4)]
      decimal bigBlind;
      [ProtoMember(5)]
      decimal maxStack;
      [ProtoMember(6)]
      string tableRef;
      [ProtoMember(7)]
      byte numSeats;

      [ProtoMember(8, AsReference = true)]
      databaseCache parentCache;

      [ProtoMember(9)]
      int tableRandomNumber = -1;

      [ProtoMember(10)]
      byte dataSource;

      private pokerTable() { }

      /// <summary>
      /// Create a new poker table which does not exist in the database
      /// </summary>
      /// <param name="pokerClientId"></param>
      /// <param name="littleBlind"></param>
      /// <param name="bigBlind"></param>
      /// <param name="maxStack"></param>
      /// <param name="tableRef"></param>
      public pokerTable(short pokerClientId, decimal littleBlind, decimal bigBlind, decimal maxStack, string tableRef, byte numSeats, byte dataSource, databaseCache parentCache, int tableRandomNumber)
      {
        //Create this table in the DatabaseCache instance
        this.pokerClientId = pokerClientId;
        //this.startTime = DateTime.Now;
        //this.endTime = this.startTime;
        this.littleBlind = littleBlind;
        this.bigBlind = bigBlind;
        this.maxStack = maxStack;
        this.tableRef = tableRef;
        this.numSeats = numSeats;
        this.parentCache = parentCache;
        this.dataSource = dataSource;

        this.tableRandomNumber = tableRandomNumber;

        databaseCache.databaseRAM.NewPokerTable(this);
      }

      #region Get & Set Methods

      public long TableId
      {
        get { return tableId; }
        internal set { tableId = value; }
      }

      public short PokerClientId
      {
        get { return pokerClientId; }
      }

      public decimal LittleBlind
      {
        get { return littleBlind; }
      }

      public decimal BigBlind
      {
        get { return bigBlind; }
      }

      public decimal MaxStack
      {
        get { return maxStack; }
      }

      public string TableRef
      {
        get { return tableRef; }
      }

      public byte NumSeats
      {
        get { return numSeats; }
      }

      public int TableRandomNumber
      {
        get { return tableRandomNumber; }
        set { tableRandomNumber = value; }
      }

      public byte DataSource
      {
        get { return dataSource; }
        set { dataSource = value; }
      }

      #endregion Get & Set Methods
    }

    [ProtoContract]
    [DebuggerDisplay("{ToString()}")]
    internal class tablePlayer
    {
      [ProtoMember(1)]
      long tableId;
      [ProtoMember(2)]
      long playerId;
      [ProtoMember(3)]
      decimal stack;
      [ProtoMember(4)]
      byte position;
      [ProtoMember(5)]
      bool isDead;

      [ProtoMember(6, AsReference = true)]
      databaseCache parentCache;

      public override string ToString()
      {
        //return "pID:" + playerId + " pos:" + position + " isDead:" + Convert.ToDouble(isDead) + " stack:" + stack;
        return parentCache.getPlayerName(PlayerId) + " - pos:" + position + " isDead:" + Convert.ToDouble(isDead) + " stack:" + stack;
      }

      private tablePlayer() { }

      /// <summary>
      /// Adds a player to the tablePlayer object which does not already exist in the database
      /// </summary>
      /// <param name="tableId"></param>
      /// <param name="playerId"></param>
      /// <param name="stack"></param>
      /// <param name="position"></param>
      /// <param name="isDead"></param>
      public tablePlayer(long tableId, long playerId, decimal stack, byte position, bool isDead, databaseCache parentCache)
      {
        this.tableId = tableId;
        this.playerId = playerId;
        this.stack = stack;
        this.position = position;
        this.isDead = isDead;
        this.parentCache = parentCache;
      }

      #region Get & Set Methods

      public long TableId
      {
        get { return tableId; }
      }

      public long PlayerId
      {
        get { return playerId; }
      }

      public byte Position
      {
        get { return position; }
        set { position = value; }
      }

      public decimal Stack
      {
        get { return stack; }
        set { stack = value; }
      }

      public bool IsDead
      {
        get { return isDead; }
        set { isDead = value; }
      }

      #endregion Get & Set Methods

    }

    [ProtoContract]
    [DebuggerDisplay("{ToString()}")]
    internal class pokerPlayer
    {
      [ProtoMember(1)]
      long playerId;
      [ProtoMember(2)]
      string playerName;
      [ProtoMember(3)]
      short pokerClientId;
      [ProtoMember(4)]
      bool isBot;

      [ProtoMember(5, AsReference = true)]
      databaseCache parentCache;

      public override string ToString()
      {
        return "(" + playerId + ") " + playerName + " isBot:" + isBot;
      }

      private pokerPlayer() { }

      /// <summary>
      /// Populates the pokerPlayer object from values stored in the database
      /// </summary>
      /// <param name="playerId"></param>
      public pokerPlayer(long playerId, string playerName, bool isBot, databaseCache parentCache)
      {
        this.parentCache = parentCache;

        this.playerId = playerId;
        this.playerName = playerName;
        this.pokerClientId = parentCache.PokerClientId;
        this.isBot = isBot;
      }

      /// <summary>
      /// If the player already exists in the database, pokerPlayer object is populated. If player does not exist, a new player is created and pokerPlayer object is populated.
      /// </summary>
      /// <param name="playerId"></param>
      /// <param name="botConfigId"></param>
      /// <param name="playerName"></param>
      /// <param name="pokerClientId"></param>
      public pokerPlayer(string playerName, short pokerClientId, databaseCache parentCache)
      {
        this.parentCache = parentCache;
        PokerPlayer pokerPlayers = databaseQueries.playerDetailsByPlayerName(playerName, pokerClientId);

        if (pokerPlayers != null)
        {
          //Player already exists, populate pokerPlayer object
          this.playerId = pokerPlayers.PlayerId;
          this.playerName = pokerPlayers.PlayerName;
          this.pokerClientId = pokerPlayers.PokerClientId;
          this.isBot = (pokerPlayers.AiType == AIGeneration.NoAi_Human ? false : true);
        }
        else
        {
          //Player does not exist, so create new player
          this.playerName = playerName;
          this.pokerClientId = pokerClientId;
          this.isBot = false;

          this.playerId = databaseQueries.CreateNewNonBotPlayer(playerName, pokerClientId);
        }

      }

      #region Get & Set Methods

      public long PlayerId
      {
        get { return playerId; }
      }

      public bool IsBot
      {
        get { return isBot; }
      }

      public string PlayerName
      {
        get { return playerName; }
      }

      public short PokerClientId
      {
        get { return pokerClientId; }
      }

      #endregion Get & Set Methods

    }

    [ProtoContract]
    [DebuggerDisplay("{ToString()}")]
    internal class holeCard
    {
      [ProtoMember(1)]
      long handId;
      [ProtoMember(2)]
      long playerId;
      [ProtoMember(3)]
      byte holeCard1;
      [ProtoMember(4)]
      byte holeCard2;

      [ProtoMember(5, AsReference = true)]
      databaseCache parentCache;

      public override string ToString()
      {
        //return "pID:" + PlayerId + " - " + (Card)holeCard1 + " " + (Card)holeCard2;
        return parentCache.getPlayerName(PlayerId) + " - " + (Card)holeCard1 + " " + (Card)holeCard2;
      }

      private holeCard() { }

      /// <summary>
      /// Populates cache with hold cards and adds to the database submit queue.
      /// </summary>
      /// <param name="handId"></param>
      /// <param name="playerId"></param>
      /// <param name="holeCard1"></param>
      /// <param name="holeCard2"></param>
      public holeCard(long handId, long playerId, byte holeCard1, byte holeCard2, databaseCache parentCache)
      {
        this.parentCache = parentCache;

        this.handId = handId;
        this.playerId = playerId;
        this.holeCard1 = holeCard1;
        this.holeCard2 = holeCard2;
      }

      #region Get & Set Methods

      public long HandId
      {
        get { return handId; }
      }

      public long PlayerId
      {
        get { return playerId; }
      }

      public byte HoleCard1
      {
        get { return holeCard1; }
      }

      public byte HoleCard2
      {
        get { return holeCard2; }
      }

      #endregion Get & Set Methods

    }

    [ProtoContract]
    [DebuggerDisplay("{ToString()}")]
    internal class pokerHand
    {
      [ProtoMember(1)]
      long handId;
      [ProtoMember(2)]
      long tableId;
      [ProtoMember(3)]
      decimal potValue;
      [ProtoMember(4)]
      DateTime startTime;
      [ProtoMember(5)]
      byte numStartPlayers;
      [ProtoMember(6)]
      byte dealerPosition;
      [ProtoMember(7)]
      byte tableCard1;
      [ProtoMember(8)]
      byte tableCard2;
      [ProtoMember(9)]
      byte tableCard3;
      [ProtoMember(10)]
      byte tableCard4;
      [ProtoMember(11)]
      byte tableCard5;

      [ProtoMember(12)]
      byte seqIndex = 0; //THis will now be used to index handActions

      /// <summary>
      /// We use this variable to cache which number hand this is on the associated tableId
      /// </summary>
      [ProtoMember(13)]
      long cacheHandIndex;

      [ProtoMember(14, AsReference = true)]
      databaseCache parentCache;

      /// <summary>
      /// Some parts of the AI might want a per hand random number. That can be generated on hand creation and saved here.
      /// </summary>
      [ProtoMember(15)]
      int handRandomNumber = -1;

      public override string ToString()
      {
        if (tableCard5 > 0)
          return "hId:" + handId.ToString() + " cards:" + (Card)tableCard1 + ", " + (Card)tableCard2 + ", " + (Card)tableCard3 + ", " + (Card)tableCard4 + ", " + (Card)tableCard5;
        else if (tableCard4 > 0)
          return "hId:" + handId.ToString() + " cards:" + (Card)tableCard1 + ", " + (Card)tableCard2 + ", " + (Card)tableCard3 + ", " + (Card)tableCard4;
        else if (tableCard3 > 0)
          return "hId:" + handId.ToString() + " cards:" + (Card)tableCard1 + ", " + (Card)tableCard2 + ", " + (Card)tableCard3;
        else
          return "hId:" + handId.ToString();
      }

      private pokerHand() { }

      /// <summary>
      /// Populates pokerHand object and adds data to database immediately.
      /// </summary>
      /// <param name="handId"></param>
      /// <param name="tableId"></param>
      /// <param name="startTime"></param>
      /// <param name="numStartPlayers"></param>
      /// <param name="dealerPosition"></param>
      public pokerHand(long tableId, byte numStartPlayers, byte dealerPosition, databaseCache parentCache, int handRandomNumber)
      {
        handConstructor(tableId, numStartPlayers, dealerPosition, parentCache, DateTime.Now, handRandomNumber);
      }

      /// <summary>
      /// Populates pokerHand object and adds data to database immediately.
      /// </summary>
      /// <param name="handId"></param>
      /// <param name="tableId"></param>
      /// <param name="startTime"></param>
      /// <param name="numStartPlayers"></param>
      /// <param name="dealerPosition"></param>
      public pokerHand(long tableId, byte numStartPlayers, byte dealerPosition, databaseCache parentCache, DateTime startTime, int handRandomNumber)
      {
        handConstructor(tableId, numStartPlayers, dealerPosition, parentCache, startTime, handRandomNumber);
      }

      /// <summary>
      /// The private constructor that gets called from the other two, allowing a custom startTime
      /// </summary>
      /// <param name="tableId"></param>
      /// <param name="numStartPlayers"></param>
      /// <param name="dealerPosition"></param>
      /// <param name="parentCache"></param>
      /// <param name="startTime"></param>
      private void handConstructor(long tableId, byte numStartPlayers, byte dealerPosition, databaseCache parentCache, DateTime startTime, int handRandomNumber)
      {
        this.parentCache = parentCache;
        this.tableId = tableId;
        this.numStartPlayers = numStartPlayers;
        this.dealerPosition = dealerPosition;

        //Use defaults
        this.startTime = startTime;
        //this.endTime = startTime;
        this.potValue = 0;
        this.tableCard1 = 0;
        this.tableCard2 = 0;
        this.tableCard4 = 0;
        this.tableCard3 = 0;
        this.tableCard5 = 0;
        //this.handErrorId = 1;

        this.cacheHandIndex = parentCache.getNumHandsPlayed();
        this.handRandomNumber = handRandomNumber;

        databaseCache.databaseRAM.NewHand(this);

        parentCache.currentPokerHand = this;
      }

      /// <summary>
      /// Returns the next seqIndex which should be used for actions
      /// </summary>
      /// <returns></returns>
      public byte NextSeqIndex()
      {
        return seqIndex++;
      }

      public byte CurrentSeqIndex
      {
        get { return seqIndex; }
      }

      #region Get & Set Methods

      public long HandId
      {
        get { return handId; }
        internal set { handId = value; }
      }

      public long TableId
      {
        get { return tableId; }
      }

      public DateTime StartTime
      {
        get { return startTime; }
      }

      public byte NumStartPlayers
      {
        get { return numStartPlayers; }
      }

      public byte DealerPosition
      {
        get { return dealerPosition; }
      }

      //public DateTime EndTime
      //{
      //    get { return endTime; }
      //    private set
      //    {
      //        endTime = value;

      //        if (databaseCache.databaseRAM == null)
      //            thisPokerHand.endTime = value;
      //    }
      //}

      public int HandRandomNumber
      {
        get { return handRandomNumber; }
        set { handRandomNumber = value; }
      }

      public decimal PotValue
      {
        get { return potValue; }
        set { potValue = value; }
      }

      public byte TableCard1
      {
        get { return tableCard1; }
        set { tableCard1 = value; }
      }

      public byte TableCard2
      {
        get { return tableCard2; }
        set { tableCard2 = value; }
      }

      public byte TableCard3
      {
        get { return tableCard3; }
        set { tableCard3 = value; }
      }

      public byte TableCard4
      {
        get { return tableCard4; }
        set { tableCard4 = value; }
      }

      public byte TableCard5
      {
        get { return tableCard5; }
        set { tableCard5 = value; }
      }

      public long CacheHandIndex
      {
        get { return cacheHandIndex; }
      }

      #endregion Get & Set Methods

      public void endHand()
      {
        //long? firstActionIdTemp = 0;
        //long? lastActionIdTemp = 0;

        //if (DateTime.Compare(startTime,endTime)>0)
        //    throw new Exception("Attempted to end a hand with a time which precedes the startTime.");

        //EndTime = endTime;

        if (parentCache.currentPokerHand != null)
          parentCache.totalActionCounter += parentCache.currentPokerHand.CurrentSeqIndex;

        parentCache.currentPokerHand = null;

        databaseCache.databaseRAM.EndHand(this, parentCache.holeCards, parentCache.handActions);
      }
    }

    [ProtoContract]
    [DebuggerDisplay("{ToString()}")]
    internal class handAction
    {
      [ProtoMember(1)]
      long handId;
      [ProtoMember(2)]
      byte localSeqIndex;
      [ProtoMember(3)]
      long playerId;
      [ProtoMember(4)]
      byte actionTypeId;
      [ProtoMember(5)]
      decimal actionValue;

      [ProtoMember(6, AsReference = true)]
      databaseCache parentCache;

      public override string ToString()
      {
        return "[" + localSeqIndex + "] " + parentCache.getPlayerName(playerId) + " - " + (PokerAction)actionTypeId + ": " + actionValue;
      }

      private handAction() { }

      /// <summary>
      /// Populates the handAction object from provided values (a shortcut without database!)
      /// </summary>
      /// <param name="actionId"></param>
      /// <param name="handId"></param>
      /// <param name="playerId"></param>
      /// <param name="actionTypeId"></param>
      /// <param name="actionTime"></param>
      /// <param name="actionValue"></param>
      public handAction(long handId, byte seqIndex, long playerId, byte actionTypeId, decimal actionValue, databaseCache parentCache)
      {
        this.parentCache = parentCache;
        if (seqIndex == 0 || handId == 0 || playerId == 0)
          throw new Exception("This constructor can only be used if all values are known.");

        this.localSeqIndex = seqIndex;
        this.handId = handId;
        this.playerId = playerId;
        this.actionTypeId = actionTypeId;
        this.actionValue = actionValue;
      }

      /// <summary>
      /// Adds an action to the handAction object and adds it to the database insert queue using a provided actionTime
      /// </summary>
      /// <param name="handId"></param>
      /// <param name="playerId"></param>
      /// <param name="actionTypeId"></param>
      /// <param name="actionValue"></param>
      /// <param name="parentCache"></param>
      /// <param name="actionTime"></param>
      public handAction(long handId, long playerId, byte actionTypeId, decimal actionValue, databaseCache parentCache)
      {
        //ActionId will not be known until the entry is committed to the database so should not be used client side
        actionConstructor(handId, playerId, actionTypeId, actionValue, parentCache);
      }

      private void actionConstructor(long handId, long playerId, byte actionTypeId, decimal actionValue, databaseCache parentCache)
      {
        //ActionId will not be known until the entry is committed to the database so should not be used client side
        this.parentCache = parentCache;
        this.handId = handId;
        this.playerId = playerId;
        this.actionTypeId = actionTypeId;
        this.actionValue = actionValue;

        //Set the localindex
        //We don't have to worry about multithreading here because we can only ever do things serially for an individual table
        localSeqIndex = parentCache.currentPokerHand.NextSeqIndex();
      }

      #region Get & Set Methods

      //public long ActionId
      //{
      //    get { return actionId; }
      //}

      public long HandId
      {
        get { return handId; }
      }

      public long PlayerId
      {
        get { return playerId; }
      }

      public byte ActionTypeId
      {
        get { return actionTypeId; }
      }

      //public DateTime ActionTime
      //{
      //    get { return actionTime; }
      //}

      public decimal ActionValue
      {
        get { return actionValue; }
      }

      public byte LocalSeqIndex
      {
        get { return localSeqIndex; }
      }

      //public void UpdatePlayerPosition(decimal newPosition)
      //{
      //    if (databaseCache.databaseRAM != null)
      //        throw new NotImplementedException("Only implemented for the main database.");

      //    if (actionTypeId != (byte)PokerAction.JoinTable)
      //        throw new Exception("You are only allowed to modify the values of SitDown actions!");

      //    //Takes care of local cache copy
      //    actionValue = newPosition;

      //    //Takes care of pending commits
      //    var pendingHandActions =
      //        from actions in parentCache.databaseCurrent.GetChangeSet().Inserts.OfType<tbl_handAction>()
      //        where actions.handId == handId && actions.playerId == playerId && actions.actionTypeId == (byte)PokerAction.JoinTable
      //        select actions;

      //    if (pendingHandActions.Count() > 1)
      //        throw new Exception("More than one sitdown action has been recorded in the database for this hand for this player.");
      //    else if (pendingHandActions.Count() == 1)
      //        pendingHandActions.First().actionValue = newPosition;
      //    else
      //    {
      //        //Takes care of already committed database copies
      //        var handActions =
      //            from actions in parentCache.databaseCurrent.tbl_handActions
      //            where actions.handId == handId && actions.playerId == playerId && actions.actionTypeId == (byte)PokerAction.JoinTable
      //            select actions;

      //        if (handActions.Count() > 1)
      //            throw new Exception("More than one sitdown action has been recorded in the database for this hand for this player.");
      //        else if (handActions.Count() == 1)
      //            handActions.First().actionValue = newPosition;
      //    }

      //}

      #endregion Get & Set Methods

    }

    #endregion cacheDataClasses
  }
}
