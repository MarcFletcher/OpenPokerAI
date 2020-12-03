using System;
using System.Collections.Generic;
using System.Linq;
using PokerBot.Definitions;

namespace PokerBot.Database
{
  /// <summary>
  /// Contains all poker game data access methods
  /// </summary>
  public abstract partial class databaseCache
  {
    #region currentPokerTable

    /// <summary>
    /// Returns the current table id
    /// </summary>
    public long TableId
    { get { return currentPokerTable.TableId; } }

    /// <summary>
    /// Returns the current table pokerClientId
    /// </summary>
    public short PokerClientId
    {
      get { return currentPokerTable.PokerClientId; }
    }

    /// <summary>
    /// Returns the current table littleBlind amount
    /// </summary>
    public decimal LittleBlind
    {
      get { return currentPokerTable.LittleBlind; }
    }

    /// <summary>
    /// Returns the current table bigBlind amount
    /// </summary>
    public decimal BigBlind
    {
      get { return currentPokerTable.BigBlind; }
    }

    /// <summary>
    /// Returns the current table maxStack amount
    /// </summary>
    public decimal MaxStack
    {
      get { return currentPokerTable.MaxStack; }
    }

    /// <summary>
    /// Returns the current table tableRef (i.e. table name)
    /// </summary>
    public string TableRef
    {
      get { return currentPokerTable.TableRef; }
    }

    /// <summary>
    /// Returns the maximum number of seats available at the current table
    /// </summary>
    public byte NumSeats
    {
      get { return currentPokerTable.NumSeats; }
    }

    /// <summary>
    /// returns the hand data source for the current poker table
    /// </summary>
    public HandDataSource DataSource
    {
      get { return (HandDataSource)currentPokerTable.DataSource; }
    }

    /// <summary>
    /// Returns the random number stored alongside this pokertable
    /// </summary>
    public int TableRandomNumber
    {
      get { return currentPokerTable.TableRandomNumber; }
      set { currentPokerTable.TableRandomNumber = value; }
    }

    /// <summary>
    /// Retuns the playerId of the last player who acted
    /// </summary>
    /// <returns></returns>
    public long getLastActedPlayerId()
    {
      startRead();

      long playerId;
      if (handActions.Count == 0)
      {
        endRead();
        return -1;
      }
      else
      {
        var lastActions = from
            actions in handActions
                          where actions.ActionTypeId == (byte)PokerAction.Call || actions.ActionTypeId == (byte)PokerAction.Raise || actions.ActionTypeId == (byte)PokerAction.Fold || actions.ActionTypeId == (byte)PokerAction.Check || actions.ActionTypeId == (byte)PokerAction.DealFlop || actions.ActionTypeId == (byte)PokerAction.DealTurn || actions.ActionTypeId == (byte)PokerAction.DealRiver
                          where actions.HandId == currentPokerHand.HandId
                          orderby actions.LocalSeqIndex ascending
                          select actions;

        if (lastActions.Count() == 0)
          playerId = -1;
        else
          playerId = lastActions.Last().PlayerId;
      }

      endRead();

      return playerId;
    }

    public long getLastActedPlayerId(PokerAction action)
    {
      startRead();

      long playerId;
      if (handActions.Count == 0)
      {
        endRead();
        return -1;
      }
      else
      {
        var lastActions = from
            actions in handActions
                          where actions.ActionTypeId == (byte)action
                          where actions.HandId == currentPokerHand.HandId
                          orderby actions.LocalSeqIndex ascending
                          select actions;

        if (lastActions.Count() == 0)
          playerId = -1;
        else
          playerId = lastActions.Last().PlayerId;
      }

      endRead();

      return playerId;
    }

    /// <summary>
    /// Returns the position of the next active player in this current hand. NOTE: IF A PLAYER IS ALL IN OR HAS FOLDED THEY CAN NO LONGER ACT
    /// If there is no nextActiveTablePosition returns Byte.MaxValue
    /// </summary>
    /// <returns></returns>
    public byte getNextActiveTablePosition(byte currentPosition)
    {

      //If the current position is last in list then return first position
      startRead();
      byte returnPosition = Byte.MaxValue;
      byte returnPositionIndex;

      byte[] activePositions = getActivePositions();
      byte[] allInPositions = getAllInPositions();

      if (currentPosition + 1 >= NumSeats)
        returnPositionIndex = 0;
      else
        returnPositionIndex = (byte)(currentPosition + 1);

      //First we look for the next active position going forward
      for (byte k = returnPositionIndex; k < NumSeats; k++)
      {
        if (activePositions.Contains(k) && !allInPositions.Contains(k))
        {
          returnPosition = k;
          break;
        }
      }

      //If we have still not found it we start from 0 and work upwards to returnPositionIndex
      if (returnPosition == Byte.MaxValue)
      {
        for (byte k = 0; k < returnPositionIndex; k++)
        {
          if (activePositions.Contains(k) && !allInPositions.Contains(k))
          {
            returnPosition = k;
            break;
          }
        }
      }

      endRead();

      return returnPosition;

    }

    /// <summary>
    /// Returns the position of the current active table position, i.e. who must make the next decision.
    /// </summary>
    /// <returns>If return value is byte.maxValue there is no current activeTablePosition.</returns>
    public byte getCurrentActiveTablePosition()
    {
      byte returnPosition;

      object cacheValue = startRead("getCurrentActiveTablePosition");

      if (cacheValue != null)
        returnPosition = (byte)cacheValue;
      else
      {
        //Determine last handAction we don't include folds as we have to deal with out of turn folds
        var lastToAct = (from
            action in handActions
                         where action.HandId == getCurrentHandId()
                         where action.ActionTypeId == (byte)PokerAction.Call || action.ActionTypeId == (byte)PokerAction.Raise || action.ActionTypeId == (byte)PokerAction.Check || action.ActionTypeId == (byte)PokerAction.BigBlind || action.ActionTypeId == (byte)PokerAction.DealFlop || action.ActionTypeId == (byte)PokerAction.DealTurn || action.ActionTypeId == (byte)PokerAction.DealRiver
                         orderby action.LocalSeqIndex descending
                         select action).ToArray();

        if (lastToAct.Count() == 0)
          //If there have not been any actions yet then return the current dealer position
          returnPosition = getCurrentHandDetails().dealerPosition;
        else
        {
          //If the most recent action was a bigblind return the nextActiveTablePosition as the currentActivePosition
          if (lastToAct.First().ActionTypeId == (byte)PokerAction.BigBlind)
          {
            //It's possible for multiple big blinds to be played for any given hand
            //Make sure to select the big blind closest the dealer
            var firstBigBlind = from
                action in lastToAct
                                where action.ActionTypeId == (byte)PokerAction.BigBlind
                                orderby getActivePlayerDistanceToDealer(action.PlayerId) ascending
                                select action;

            //Once we have worked out where the first big blind was played we can determine the next active table position
            returnPosition = getNextActiveTablePosition(getPlayerPosition(firstBigBlind.First().PlayerId));
          }
          else
          {
            if (lastToAct[0].ActionTypeId == (byte)PokerAction.DealFlop ||
                lastToAct[0].ActionTypeId == (byte)PokerAction.DealTurn ||
                lastToAct[0].ActionTypeId == (byte)PokerAction.DealRiver)
              //If the last recorded action was a dealer action we return the next activeTablePosition along from the dealer
              returnPosition = getNextActiveTablePosition(getCurrentHandDetails().dealerPosition);
            else
              //If the last action was a betting action then return the next active position along
              returnPosition = getNextActiveTablePosition(getPlayerPosition(lastToAct[0].PlayerId));
          }
        }

        addMethodReturnValue("getCurrentActiveTablePosition", returnPosition);
      }

      endRead();

      return returnPosition;

    }

    /// <summary>
    /// Returns the next currently sat in player at the table. Currently only used in simulation for determining the next dealer position.
    /// </summary>
    /// <param name="currentPosition"></param>
    /// <returns></returns>
    public byte getNextSatInPlayerPosition(byte currentPosition)
    {
      //throw new NotImplementedException();

      //If the current position is last in list then return first position
      byte returnPosition = Byte.MaxValue;

      startRead();

      var currentTablePlayers =
          (from p in tablePlayers
           join t in pokerPlayers on p.PlayerId equals t.PlayerId
           where t.PlayerName != "" && p.IsDead == false
           where p.TableId == currentPokerTable.TableId
           orderby p.Position ascending
           select p.Position).ToArray();

      byte[] activePositions = getActivePositions();

      for (int i = 0; i < currentTablePlayers.Length; i++)
      {
        //Look for the currentposition.
        if (currentTablePlayers[i] >= currentPosition || currentPosition > currentTablePlayers[currentTablePlayers.Length - 1])
        {

          if (currentTablePlayers[i] == currentTablePlayers[currentTablePlayers.Length - 1] || currentPosition > currentTablePlayers[currentTablePlayers.Length - 1])
          {  //If this is the last active position return the first
            returnPosition = currentTablePlayers[0];
            break;
          }
          else
          {
            //If this is not the last element return the next position
            returnPosition = currentTablePlayers[i + 1];
            break;
          }
        }
      }

      if (returnPosition == Byte.MaxValue)
      {
        endRead();
        throw new Exception("Unable to determine the next player position. Was currentPosition specified correctly?");
      }

      endRead();

      return returnPosition;
    }

    /// <summary>
    /// Returns all table positions which currently have sat in players (i.e. someone who is active at the table but
    /// not necessarily in the current hand, e.g. if they have folded)
    /// </summary>
    /// <returns></returns>
    public byte[] getSatInPositions()
    {
      //byte[] returnArray = new byte[0];
      byte[] currentTablePlayers;
      object returnValue = startRead("getSatInPositions");

      if (returnValue != null)
        currentTablePlayers = returnValue as byte[];
      else
      {
        currentTablePlayers =
            (from p in tablePlayers
             join t in pokerPlayers on p.PlayerId equals t.PlayerId
             where t.PlayerName != "" && p.IsDead == false //&& p.Stack > 0
             orderby p.Position ascending
             select p.Position).ToArray();

        if (currentTablePlayers.Length > NumSeats)
        {
          endRead();
          throw new Exception("More players than there are seats cannot be sat at the table.");
        }

        addMethodReturnValue("getSatInPositions", currentTablePlayers);
      }

      endRead();

      return currentTablePlayers;
    }

    /// <summary>
    /// Returns all table positions which currently have players
    /// </summary>
    /// <returns></returns>
    public byte[] getSatDownPositions()
    {
      //byte[] returnArray = new byte[0];
      startRead();

      var currentTablePlayers =
          (from p in tablePlayers
           join t in pokerPlayers on p.PlayerId equals t.PlayerId
           where t.PlayerName != ""
           select p.Position).ToArray();

      if (currentTablePlayers.Length > NumSeats)
      {
        endRead();
        throw new Exception("More players than there are seats cannot be sat at the table.");
      }
      /*
      if (currentTablePlayers.Length > 0)
      {
          returnArray = new byte[currentTablePlayers.Length];

          for (int i = 0; i < currentTablePlayers.Length; i++)
          {
              returnArray[i] = currentTablePlayers[i];
          }
      }
      */

      endRead();

      return currentTablePlayers;
    }

    /// <summary>
    /// Returns a byte array containing all positions where there is currently no player.
    /// If a player is in a position but sat out his position is NOT returned.
    /// </summary>
    /// <returns></returns>
    public byte[] getEmptyPositions()
    {
      startRead();

      byte[] emptyPositions =
          (from current in tablePlayers
           join players in pokerPlayers on current.PlayerId equals players.PlayerId
           where players.PlayerName == "" && current.Stack == 0
           select current.Position).ToArray();

      endRead();

      return emptyPositions;
    }

    /// <summary>
    /// Returns the number of hands played on this table (Takes into account cache may have been purged).
    /// </summary>
    /// <returns></returns>
    public long getNumHandsPlayed()
    {
      long numHandsPlayed;
      startRead();

      if (numPokerHands > pokerHands.Count)
        numHandsPlayed = numPokerHands;
      else
        numHandsPlayed = pokerHands.Count;

      endRead();

      return numHandsPlayed;
    }

    #endregion currentPokerTable

    #region tablePlayer

    /// <summary>
    /// Data struct for playerDetails
    /// </summary>
    public struct playerDetails
    {
      public long playerId;
      public string playerName;
      public decimal stack;
      public byte position;
      public bool isDead;
      public bool isBot;

      public playerDetails(long playerId, string playerName, decimal stack, byte position, bool isBot, bool isDead)
      {
        this.playerId = playerId;
        this.playerName = playerName;
        this.stack = stack;
        this.position = position;
        this.isBot = isBot;
        this.isDead = isDead;
      }
    }

    /// <summary>
    /// Returns array of playerIds and playerNames
    /// </summary>
    /// <returns></returns>
    public playerDetails[] getPlayerDetails()
    {

      startRead();

      playerDetails[] playerDetails;

      var currentTablePlayers =
          (from p in tablePlayers
           join t in pokerPlayers on p.PlayerId equals t.PlayerId
           where t.PlayerName != ""
           orderby p.Position
           select new
           {
             PlayerId = p.PlayerId,
             PlayerName = t.PlayerName,
             Stack = p.Stack,
             IsDead = p.IsDead,
             Position = p.Position,
             IsBot = t.IsBot
           }).ToArray();

      playerDetails = new playerDetails[currentTablePlayers.Length];

      for (int i = 0; i < currentTablePlayers.Length; i++)
      {
        playerDetails[i] = new playerDetails(
            currentTablePlayers[i].PlayerId,
            currentTablePlayers[i].PlayerName,
            currentTablePlayers[i].Stack,
            currentTablePlayers[i].Position,
            currentTablePlayers[i].IsBot,
            currentTablePlayers[i].IsDead
            );
      }

      endRead();

      return playerDetails;
    }

    /// <summary>
    /// Returns the details of a single player
    /// </summary>
    /// <param name="playerId"></param>
    /// <returns></returns>
    public playerDetails getPlayerDetails(long playerId)
    {
      startRead();

      playerDetails returnDetails;

      var players =
          (from p in tablePlayers
           join t in pokerPlayers on p.PlayerId equals t.PlayerId
           where t.PlayerName != "" && t.PlayerId == playerId
           orderby p.Position
           select new
           {
             PlayerId = p.PlayerId,
             PlayerName = t.PlayerName,
             Stack = p.Stack,
             IsDead = p.IsDead,
             Position = p.Position,
             IsBot = t.IsBot
           }).Take(1);

      if (players.Count() == 1)
        returnDetails = new playerDetails(
            players.First().PlayerId,
            players.First().PlayerName,
            players.First().Stack,
            players.First().Position,
            players.First().IsBot,
            players.First().IsDead
            );
      else
      {
        endRead();
        throw new Exception("PlayerId was not found in the cache!");
      }

      endRead();
      return returnDetails;
    }

    ///// <summary>
    ///// Returns the actionId of this players last action for the current cache.
    ///// Currently used when recording AI decisions.
    ///// </summary>
    ///// <param name="playerId"></param>
    ///// <returns></returns>
    //public long getPlayerLastActionId(long playerId)
    //{
    //    startRead();

    //    //We must commit to the database in order to get the action Id's
    //    long? lastActionId = 0;
    //    CommitDatabase();
    //    databaseCurrent.sp_LastPlayerActionId(playerId, TableId, ref lastActionId);

    //    if (lastActionId == null)
    //    {
    //        endRead();
    //        throw new Exception("No action has been recorded for the provided player.");
    //    }

    //    endRead();
    //    return (long)lastActionId;
    //}

    /// <summary>
    /// Returns true if a player has acted this current hand other than blindss
    /// </summary>
    /// <param name="playerId"></param>
    /// <returns></returns>
    public bool playerHasPerformedNonBlindActions(long playerId)
    {
      startRead();
      bool returnValue;

      var playerActions = from
          actions in handActions
                          where actions.PlayerId == playerId
                          where actions.HandId == getCurrentHandId()
                          where actions.ActionTypeId == (byte)PokerAction.Call || actions.ActionTypeId == (byte)PokerAction.Check || actions.ActionTypeId == (byte)PokerAction.Raise || actions.ActionTypeId == (byte)PokerAction.Fold
                          select actions;

      if (playerActions.Count() > 0)
        returnValue = true;
      else
        returnValue = false;

      endRead();
      return returnValue;

    }

    /// <summary>
    /// Returns a player name for a given playerId. Attempts to located in pokerPlayers first then defaults to database.
    /// </summary>
    /// <param name="playerId"></param>
    /// <returns></returns>
    public string getPlayerName(long playerId)
    {
      startRead();
      string playerName;

      var players =
          from p in pokerPlayers
          where p.PlayerId == playerId
          select p.PlayerName;

      if (players.Count() == 1)
        playerName = players.First();
      else
      {
        /*
        var databasePlayers =
            from p in database.tbl_players
            where p.id == playerId
            select p.playerName;



        playerName = databasePlayers.First();
         */
        playerName = databaseQueries.convertToPlayerNameFromId(playerId);
      }

      endRead();
      return playerName;

    }

    /// <summary>
    /// Returns a players position on a table
    /// </summary>
    /// <param name="playerId"></param>
    /// <returns></returns>
    public byte getPlayerPosition(long playerId)
    {
      startRead();
      byte returnPosition;

      //Look in the active cache
      var players =
          (from tp in tablePlayers
           where tp.PlayerId == playerId
           select tp).ToArray();

      if (players.Length == 1)
        returnPosition = players[0].Position;
      else
      {
        //If the player is no longer at the table try to determine their last known position
        //using the cache action data
        var lastPositionOfPlayer =
            from pp in pokerPlayers
            join ha in handActions on pp.PlayerId equals ha.PlayerId
            where ha.ActionTypeId == (byte)PokerAction.JoinTable || ha.ActionTypeId == (byte)PokerAction.LeaveTable
            where pp.PlayerId == playerId
            orderby ha.LocalSeqIndex descending
            select ha;

        if (lastPositionOfPlayer.Count() > 0)
          returnPosition = (byte)lastPositionOfPlayer.First().ActionValue;
        else
        {
          throw new Exception("Unable to determine last known position of provided player. They must not have played on the provided tableId atall.");
        }

      }

      endRead();
      return returnPosition;
    }

    /// <summary>
    /// Returns the player id of the player sat in the provided position
    /// </summary>
    /// <param name="tablePosition"></param>
    /// <returns></returns>
    public long getPlayerId(byte tablePosition)
    {
      startRead();
      long returnPlayerId = -1;

      for (int i = 0; i < tablePlayers.Count; i++)
      {
        if (tablePlayers[i].Position == tablePosition)
        {
          returnPlayerId = tablePlayers[i].PlayerId;
          break;
        }
      }

      //var player =
      //    (from p in tablePlayers                
      //    where p.Position == tablePosition
      //    select p.PlayerId).ToArray();

      if (returnPlayerId == -1)
      {
        endRead();
        throw new Exception("Position provided invalid, make sure you are not trying to get the id of a blank position.");
      }

      endRead();
      return returnPlayerId;
    }

    public long getPlayerId(string playerName)
    {
      startRead();
      long returnPlayerId;

      var player =
          (from pp in pokerPlayers
           join tp in tablePlayers on pp.PlayerId equals tp.PlayerId
           where pp.PlayerName == playerName
           select pp.PlayerId).ToArray();

      if (player.Length != 1)
      {
        endRead();
        throw new Exception("Player with name " + playerName + " is not sat at the table");
      }

      returnPlayerId = player[0];

      endRead();
      return returnPlayerId;
    }

    public decimal getPlayerStack(long playerId)
    {
      startRead();

      decimal returnStack;

      var player =
          from p in tablePlayers
          where p.PlayerId == playerId
          select p.Stack;

      if (player.Count() != 1)
      {
        endRead();
        throw new Exception("Could not locate the correct stack.");
      }

      returnStack = player.First();

      endRead();
      return returnStack;
    }

    /// <summary>
    /// Returns the pot amount up to but not including the localActionId
    /// </summary>
    /// <param name="localActionId"></param>
    /// <returns></returns>
    public decimal getPotUpToActionId(long localActionId)
    {
      decimal pot = 0;
      long currentHandId = getCurrentHandId();

      startRead();

      long[] players =
          (from ha in handActions
           where ha.HandId == getCurrentHandId() &&
           (ha.ActionTypeId == (byte)PokerAction.BigBlind ||
           ha.ActionTypeId == (byte)PokerAction.LittleBlind ||
           ha.ActionTypeId == (byte)PokerAction.Call ||
           ha.ActionTypeId == (byte)PokerAction.Raise)
           select ha.PlayerId).Distinct().ToArray();

      decimal lastRaiseAmount = getMinimumPlayAmount(localActionId);

      byte[] lastDealerActionIndex =
          (from ha in handActions
           where ha.ActionTypeId == (byte)PokerAction.DealFlop || ha.ActionTypeId == (byte)PokerAction.DealRiver || ha.ActionTypeId == (byte)PokerAction.DealTurn
           && ha.HandId == currentHandId && ha.LocalSeqIndex < localActionId
           orderby ha.LocalSeqIndex ascending
           select ha.LocalSeqIndex).ToArray();

      PokerBot.Database.databaseCache.handAction[] actionsInBettingRound;
      PokerBot.Database.databaseCache.handAction[] playerActionsInBettingRoundSinceLastRaise;

      for (int p = 0; p < players.Length; p++)
      {
        long playerId = players[p];
        decimal playerMoneyInPot = 0;

        for (int i = 0; i < lastDealerActionIndex.Count() + 1; i++)
        {
          actionsInBettingRound =
              (from ha in handActions
               where (ha.LocalSeqIndex < (i == lastDealerActionIndex.Length ? long.MaxValue : lastDealerActionIndex[i])) &&
                      (ha.LocalSeqIndex > (i == 0 ? -1 : lastDealerActionIndex[i - 1])) && ha.HandId == currentHandId && ha.LocalSeqIndex < localActionId
               select ha).ToArray();

          var playerLastRaise =
              from ha in actionsInBettingRound
              where ha.PlayerId == playerId
              where ha.ActionTypeId == (byte)PokerAction.Raise
              orderby ha.LocalSeqIndex descending
              select ha;

          if (playerLastRaise.Count() == 0)
            playerActionsInBettingRoundSinceLastRaise =
                (from ha in actionsInBettingRound
                 where ha.PlayerId == playerId
                 where ha.ActionTypeId == (byte)PokerAction.LittleBlind || ha.ActionTypeId == (byte)PokerAction.BigBlind || ha.ActionTypeId == (byte)PokerAction.Call || ha.ActionTypeId == (byte)PokerAction.Raise
                 select ha).ToArray();
          else
            playerActionsInBettingRoundSinceLastRaise =
                (from ha in actionsInBettingRound
                 where ha.PlayerId == playerId
                 where ha.LocalSeqIndex >= playerLastRaise.First().LocalSeqIndex
                 where ha.ActionTypeId == (byte)PokerAction.LittleBlind || ha.ActionTypeId == (byte)PokerAction.BigBlind || ha.ActionTypeId == (byte)PokerAction.Call || ha.ActionTypeId == (byte)PokerAction.Raise
                 select ha).ToArray();

          for (int j = 0; j < playerActionsInBettingRoundSinceLastRaise.Length; j++)
            playerMoneyInPot += playerActionsInBettingRoundSinceLastRaise[j].ActionValue;
        }

        var returnedBets =
            (from ha in handActions
             where ha.HandId == currentHandId
             && ha.PlayerId == playerId
             && ha.ActionTypeId == (byte)PokerAction.ReturnBet && ha.LocalSeqIndex < localActionId
             select ha).ToArray();

        for (int j = 0; j < returnedBets.Length; j++)
          playerMoneyInPot -= returnedBets[j].ActionValue;

        pot += playerMoneyInPot;
      }

      endRead();
      return pot;

    }

    /// <summary>
    /// Returns the total amount that has been bet by the provided playerId for the current hand.
    /// </summary>
    /// <param name="playerId"></param>
    /// <returns></returns>
    public decimal getTotalPlayerMoneyInPot(long playerId)
    {
      decimal playerMoneyInPot = 0;
      Dictionary<long, decimal> cacheValues = startRead("getTotalPlayerMoneyInPot") as Dictionary<long, decimal>;

      if (cacheValues != null)
      {
        lock (cacheValues)
        {
          if (cacheValues.ContainsKey(playerId))
          {
            playerMoneyInPot = cacheValues[playerId];
            endRead();
            return playerMoneyInPot;
          }
        }
      }

      long currentHandId = getCurrentHandId();
      decimal lastRaiseAmount = getMinimumPlayAmount();

      byte[] lastDealerActionIndex =
          (from ha in handActions
           where ha.ActionTypeId == (byte)PokerAction.DealFlop || ha.ActionTypeId == (byte)PokerAction.DealRiver || ha.ActionTypeId == (byte)PokerAction.DealTurn
           && ha.HandId == currentHandId
           orderby ha.LocalSeqIndex ascending
           select ha.LocalSeqIndex).ToArray();

      PokerBot.Database.databaseCache.handAction[] actionsInBettingRound;
      PokerBot.Database.databaseCache.handAction[] playerActionsInBettingRoundSinceLastRaise;

      for (int i = 0; i < lastDealerActionIndex.Count() + 1; i++)
      {
        actionsInBettingRound =
            (from ha in handActions
             where (ha.LocalSeqIndex < (i == lastDealerActionIndex.Length ? long.MaxValue : lastDealerActionIndex[i])) &&
                    (ha.LocalSeqIndex > (i == 0 ? -1 : lastDealerActionIndex[i - 1])) && ha.HandId == currentHandId
             select ha).ToArray();

        var playerLastRaise =
            from ha in actionsInBettingRound
            where ha.PlayerId == playerId
            where ha.ActionTypeId == (byte)PokerAction.Raise
            orderby ha.LocalSeqIndex descending
            select ha;

        if (playerLastRaise.Count() == 0)
        {
          playerActionsInBettingRoundSinceLastRaise =
              (from ha in actionsInBettingRound
               where ha.PlayerId == playerId
               where ha.ActionTypeId == (byte)PokerAction.LittleBlind || ha.ActionTypeId == (byte)PokerAction.BigBlind || ha.ActionTypeId == (byte)PokerAction.Call || ha.ActionTypeId == (byte)PokerAction.Raise
               select ha).ToArray();
        }
        else
        {
          playerActionsInBettingRoundSinceLastRaise =
              (from ha in actionsInBettingRound
               where ha.PlayerId == playerId
               where ha.LocalSeqIndex >= playerLastRaise.First().LocalSeqIndex
               where ha.ActionTypeId == (byte)PokerAction.LittleBlind || ha.ActionTypeId == (byte)PokerAction.BigBlind || ha.ActionTypeId == (byte)PokerAction.Call || ha.ActionTypeId == (byte)PokerAction.Raise
               select ha).ToArray();
        }

        for (int j = 0; j < playerActionsInBettingRoundSinceLastRaise.Length; j++)
          playerMoneyInPot += playerActionsInBettingRoundSinceLastRaise[j].ActionValue;
      }

      var returnedBets =
          (from ha in handActions
           where ha.HandId == currentHandId
           && ha.PlayerId == playerId
           && ha.ActionTypeId == (byte)PokerAction.ReturnBet
           select ha).ToArray();

      for (int j = 0; j < returnedBets.Length; j++)
        playerMoneyInPot -= returnedBets[j].ActionValue;

      if (cacheValues != null)
      {
        lock (cacheValues)
          if (!cacheValues.ContainsKey(playerId))
            cacheValues.Add(playerId, playerMoneyInPot);
      }
      else
        addMethodReturnValue("getTotalPlayerMoneyInPot", new Dictionary<long, decimal>() { { playerId, playerMoneyInPot } });

      endRead();
      return playerMoneyInPot;

    }

    public decimal getDeadBlindMoneyInPot()
    {
      var deadBlindActions =
          (from action in handActions
           where action.ActionTypeId == (byte)PokerAction.DeadBlind
           select action.ActionValue).ToArray();

      if (deadBlindActions.Length == 0)
        return 0;
      else
        return deadBlindActions.Sum();
    }

    public decimal getRemainingPotAmount()
    {
      startRead();

      decimal[] winActions =
          (from ha in handActions
           where ha.ActionTypeId == (byte)PokerAction.WinPot || ha.ActionTypeId == (byte)PokerAction.TableRake
           select ha.ActionValue).ToArray();

      decimal allocatedPot = winActions.Sum();
      decimal potValue = pokerHands[pokerHands.Count - 1].PotValue;

      endRead();

      return potValue - allocatedPot;
    }

    /// <summary>
    /// Returns the total number of active player seats between the provided playerId and dealer for the current handId
    /// If the player is dealer then the number returned is the number of active players.
    /// </summary>
    /// <param name="playerId"></param>
    /// <returns></returns>
    public byte getActivePlayerDistanceToDealer(long playerId)
    {
      startRead();

      var handInfo = getCurrentHandDetails();
      byte[] activePositions = getActivePositions();
      //byte[] satInPositions = getSatInPositions();

      byte[] allPositions = new byte[NumSeats];

      for (byte i = 0; i < NumSeats; i++)
        allPositions[i] = i;

      byte[] foldedOrSatOutPositions = allPositions.Except(activePositions).ToArray();

      int dealerPosition = handInfo.dealerPosition;
      int playerPosition = getPlayerPosition(playerId);
      int numSeatsBetweenPlayerAndDealer;
      int dealerDistance;

      numSeatsBetweenPlayerAndDealer = playerPosition - dealerPosition;

      if (numSeatsBetweenPlayerAndDealer < 1)
        dealerDistance = numSeatsBetweenPlayerAndDealer + NumSeats;
      else
        dealerDistance = numSeatsBetweenPlayerAndDealer;

      if (numSeatsBetweenPlayerAndDealer > 0)
      {
        var inBetweenFoldedOrSatOutPositions = from
                                        folded in foldedOrSatOutPositions
                                               join all in allPositions on folded equals all
                                               where all > dealerPosition && all < playerPosition
                                               select folded;

        if (inBetweenFoldedOrSatOutPositions.Count() > 0)
          dealerDistance -= inBetweenFoldedOrSatOutPositions.Count();
      }
      else
      {
        var inBetweenFoldedOrSatOutPositions = from
                                folded in foldedOrSatOutPositions
                                               join all in allPositions on folded equals all
                                               where all > dealerPosition || all < playerPosition
                                               select folded;

        if (inBetweenFoldedOrSatOutPositions.Count() > 0)
          dealerDistance -= inBetweenFoldedOrSatOutPositions.Count();
      }

      endRead();
      return (byte)dealerDistance;

    }

    /// <summary>
    /// Returns the total number of active player seats between the provided playerId and dealer for the current handId
    /// If the player is dealer then the number returned is the number of active players.
    /// </summary>
    /// <param name="playerId"></param>
    /// <param name="localActionId"></param>
    /// <returns></returns>
    public byte getActivePlayerDistanceToDealer(long playerId, long localActionId)
    {
      startRead();

      var handInfo = getCurrentHandDetails();
      byte[] activePositions = getActivePositions(localActionId);
      //byte[] satInPositions = getSatInPositions();

      byte[] allPositions = new byte[NumSeats];

      for (byte i = 0; i < NumSeats; i++)
        allPositions[i] = i;

      byte[] foldedOrSatOutPositions = allPositions.Except(activePositions).ToArray();

      int dealerPosition = handInfo.dealerPosition;
      int playerPosition = getPlayerPosition(playerId);
      int numSeatsBetweenPlayerAndDealer;
      int dealerDistance;

      numSeatsBetweenPlayerAndDealer = playerPosition - dealerPosition;

      if (numSeatsBetweenPlayerAndDealer < 1)
        dealerDistance = numSeatsBetweenPlayerAndDealer + NumSeats;
      else
        dealerDistance = numSeatsBetweenPlayerAndDealer;

      if (numSeatsBetweenPlayerAndDealer > 0)
      {
        var inBetweenFoldedOrSatOutPositions = from
                                        folded in foldedOrSatOutPositions
                                               join all in allPositions on folded equals all
                                               where all > dealerPosition && all < playerPosition
                                               select folded;

        if (inBetweenFoldedOrSatOutPositions.Count() > 0)
          dealerDistance -= inBetweenFoldedOrSatOutPositions.Count();
      }
      else
      {
        var inBetweenFoldedOrSatOutPositions = from
                                folded in foldedOrSatOutPositions
                                               join all in allPositions on folded equals all
                                               where all > dealerPosition || all < playerPosition
                                               select folded;

        if (inBetweenFoldedOrSatOutPositions.Count() > 0)
          dealerDistance -= inBetweenFoldedOrSatOutPositions.Count();
      }

      endRead();
      return (byte)dealerDistance;

    }

    /// <summary>
    /// Returns a list of PokerActions containing all of the players actions current betting round ordered by localSeqIndex.
    /// </summary>
    /// <param name="playerId"></param>
    /// <returns></returns>
    public List<PokerAction> getPlayerCurrentRoundActions(long playerId)
    {
      startRead();

      List<PokerAction> returnActions = new List<PokerAction>();

      var lastDealerAction =
          (from ha in handActions
           where (ha.ActionTypeId == (byte)PokerAction.DealFlop || ha.ActionTypeId == (byte)PokerAction.DealRiver || ha.ActionTypeId == (byte)PokerAction.DealTurn) &&
           ha.HandId == getCurrentHandId()
           orderby ha.LocalSeqIndex descending
           select ha.LocalSeqIndex).ToArray();

      //If there have been dealer actions then getting all actions after the most recent
      var actionsInBettingRound =
          (from ha in handActions
           where ha.LocalSeqIndex > (lastDealerAction.Length == 0 ? -1 : lastDealerAction[0]) &&
                   ha.PlayerId == playerId &&
                   ha.HandId == getCurrentHandId()
           orderby ha.LocalSeqIndex ascending
           select ha).ToArray();

      foreach (var action in actionsInBettingRound)
        returnActions.Add((PokerAction)action.ActionTypeId);

      endRead();
      return returnActions;
    }

    /// <summary>
    /// Returns a list of PokerActions containing all of the players actions last round.
    /// If the current round is the first round the list.count = 0.
    /// </summary>
    /// <param name="playerId"></param>
    /// <returns></returns>
    public List<PokerAction> getPlayerLastRoundActions(long playerId)
    {
      startRead();

      List<PokerAction> returnActions = new List<PokerAction>();

      var lastDealerAction =
          (from ha in handActions
           where (ha.ActionTypeId == (byte)PokerAction.DealFlop || ha.ActionTypeId == (byte)PokerAction.DealRiver || ha.ActionTypeId == (byte)PokerAction.DealTurn) &&
           ha.HandId == getCurrentHandId()
           orderby ha.LocalSeqIndex descending
           select ha.LocalSeqIndex).ToArray();

      //Need to make sure there has been a betting round before this one.
      if (lastDealerAction.Count() > 0)
      {
        var actionsInBettingRound =
            (from ha in handActions
             where ha.LocalSeqIndex < lastDealerAction[0] &&
                             ha.LocalSeqIndex > (lastDealerAction.Length == 1 ? -1 : lastDealerAction[1]) &&
                             ha.PlayerId == playerId &&
                             ha.HandId == getCurrentHandId()
             select ha).ToArray();

        foreach (var action in actionsInBettingRound)
          returnActions.Add((PokerAction)action.ActionTypeId);
      }

      endRead();
      return returnActions;
    }

    /// <summary>
    /// Returns all actions by a player for the current hand
    /// </summary>
    /// <param name="playerId"></param>
    /// <returns></returns>
    public List<PokerAction> getPlayerActionsCurrentHand(long playerId)
    {
      startRead();

      //If there have been dealer actions then getting all actions after the most recent
      List<PokerAction> returnActions =
          (from ha in handActions
           where ha.PlayerId == playerId &&
                   ha.HandId == getCurrentHandId()
           select (PokerAction)ha.ActionTypeId).ToList();

      endRead();
      return returnActions;
    }

    /// <summary>
    /// Returns all post flop actions by a player for the current hand. Returns empty list if there has been no flop or no player actions.
    /// </summary>
    /// <param name="playerId"></param>
    /// <returns></returns>
    public List<PokerAction> getPlayerActionsCurrentHandPostFlop(long playerId)
    {
      startRead();

      List<PokerAction> returnActions = new List<PokerAction>();

      var flopAction =
          (from ha in handActions
           where ha.ActionTypeId == (byte)PokerAction.DealFlop &&
           ha.HandId == getCurrentHandId()
           orderby ha.LocalSeqIndex descending
           select ha.LocalSeqIndex).ToArray();

      //If there have been dealer actions then getting all actions after the most recent
      var actionsInBettingRound =
          (from ha in handActions
           where ha.LocalSeqIndex > (flopAction.Length == 0 ? byte.MaxValue : flopAction[0]) &&
                   ha.PlayerId == playerId &&
                   ha.HandId == getCurrentHandId()
           orderby ha.LocalSeqIndex ascending
           select ha).ToArray();

      foreach (var action in actionsInBettingRound)
        returnActions.Add((PokerAction)action.ActionTypeId);

      endRead();
      return returnActions;
    }


    #endregion tablePlayer

    #region pokerHand

    /// <summary>
    /// Data struct for handActions
    /// </summary>
    public struct playActions
    {
      public long localIndex;
      public PokerAction actionType;
      public long playerId;
      public long handId;
      public decimal actionValue;
    }

    /// <summary>
    /// Data struct for handDetails
    /// </summary>
    public struct handDetails
    {
      public long handId;
      public long tableId;
      public decimal potValue;
      public DateTime startTime;
      //public DateTime endTime;
      public byte numStartPlayers;
      public byte dealerPosition;
      public byte tableCard1;
      public byte tableCard2;
      public byte tableCard3;
      public byte tableCard4;
      public byte tableCard5;
      //public long handErrorId;
      //public byte numActivePlayers;
      public bool currentHandExists;
      //public long lastActionId;
      //public long firstActionId;
    }

    /// <summary>
    /// Data struct for player hole cards
    /// </summary>
    public struct playerCards
    {
      public byte holeCard1;
      public byte holeCard2;
    }

    public long[] HandIdsInCache()
    {
      return
          (from hand in pokerHands
           orderby hand.HandId
           select hand.HandId).ToArray();
    }

    /// <summary>
    /// Builds hash of hand based on variables only available within the current hand
    /// </summary>
    /// <returns></returns>
    public string CurrentHandHash(bool includeStackAmounts)
    {
      System.Security.Cryptography.MD5CryptoServiceProvider md5 = new System.Security.Cryptography.MD5CryptoServiceProvider();

      decimal stacksPositionCardsValue = 0;
      byte[] playerPositions = getSatDownPositions();
      playerCards cards;
      long playerId;
      for (int i = 0; i < playerPositions.Length; i++)
      {
        playerId = getPlayerId(playerPositions[i]);
        cards = getPlayerHoleCards(playerId);

        if (includeStackAmounts)
          stacksPositionCardsValue += ((getPlayerStack(playerId) + 1) * (playerPositions[i] + 1 + cards.holeCard1 + cards.holeCard2));
        else
          stacksPositionCardsValue += ((playerPositions[i] + 1 + cards.holeCard1 + cards.holeCard2));
      }

      //Need actions total and value total
      handDetails currentHand = getCurrentHandDetails();
      playActions[] actions = getHandActions(getCurrentHandId());

      double actionsSum = 0;
      for (int i = 0; i < actions.Length; i++)
        actionsSum += (i + 1) * (((double)actions[i].actionType + 0.5) * 0.93 + ((double)actions[i].actionValue + 0.003) * 1.13);

      double currentHandValue = (actionsSum + (double)stacksPositionCardsValue + (double)currentHand.dealerPosition + (double)currentHand.numStartPlayers + (double)currentHand.potValue + ((double)currentHand.tableCard1 + (double)currentHand.tableCard2 * 10.0 + (double)currentHand.tableCard3 * 100.0 + (double)currentHand.tableCard4 * 1000.0 + (double)currentHand.tableCard5 * 10000.0)) % Double.MaxValue;

      if (currentHandValue == 0.0)
        throw new Exception("Possible overflow exception occurred.");

      var mem = BitConverter.GetBytes(currentHandValue);
      string md5_1 = BitConverter.ToString(md5.ComputeHash(mem));

      return md5_1;
    }

    /// <summary>
    /// Return all cache hand actions
    /// </summary>
    /// <returns></returns>
    public playActions[] getAllHandActions()
    {
      startRead();

      playActions[] returnActions = new playActions[handActions.Count];

      for (int i = 0; i < returnActions.Length; i++)
      {
        returnActions[i].handId = handActions[i].HandId;
        returnActions[i].localIndex = handActions[i].LocalSeqIndex;
        returnActions[i].playerId = handActions[i].PlayerId;
        returnActions[i].actionType = (PokerAction)handActions[i].ActionTypeId;
        returnActions[i].actionValue = handActions[i].ActionValue;
      }

      endRead();
      return returnActions;
    }

    /// <summary>
    /// Returns all actions with localIndex greater than provided value
    /// </summary>
    /// <param name="localIndex"></param>
    /// <returns></returns>
    public playActions[] getHandActionsBasedOnLocalIndex(long handId, byte localIndex)
    {
      startRead();

      var actions =
          (from ha in handActions
           where (ha.LocalSeqIndex > localIndex && ha.HandId == handId) || (ha.HandId > handId)
           orderby ha.LocalSeqIndex ascending
           select ha).ToArray();

      playActions[] returnActions = new playActions[actions.Length];

      for (int i = 0; i < actions.Length; i++)
      {
        returnActions[i].handId = actions[i].HandId;
        returnActions[i].localIndex = actions[i].LocalSeqIndex;
        returnActions[i].playerId = actions[i].PlayerId;
        returnActions[i].actionType = (PokerAction)actions[i].ActionTypeId;
        returnActions[i].actionValue = actions[i].ActionValue;
      }

      endRead();
      return returnActions;
    }

    /// <summary>
    /// Returns most recent local index. If handActions.Count() == 0 returns -1
    /// </summary>
    /// <returns>return[0] = handId, return[1] = currentSeqIndex</returns>
    public long[] getMostRecentLocalIndex()
    {
      //We now need to return handId and seqIndex instead of just the old actionId
      long[] returnValue = new long[2];

      startRead();

      if (currentPokerHand != null)
      {
        returnValue[0] = currentPokerHand.HandId;
        returnValue[1] = currentPokerHand.CurrentSeqIndex;
      }
      else
        returnValue[0] = -1;

      endRead();

      return returnValue;
    }

    /// <summary>
    /// Returns the total number of actions in the current hand.
    /// </summary>
    /// <param name="includeBlindsInCount">If false will not count dead, small or big  blinds.</param>
    /// <returns></returns>
    public int CurrentHandHandActionsCount()
    {
      return (from actions in handActions
              where actions.HandId == getCurrentHandId()
              select actions.LocalSeqIndex).Count();
    }

    /// <summary>
    /// Returns all cache hand actions for the provided handId specified.
    /// </summary>
    /// <param name="handId"></param>
    /// <returns></returns>
    public playActions[] getHandActions(long handId)
    {
      startRead();

      playActions[] returnActions;

      var actions =
          (from ha in handActions
           where ha.HandId == handId
           orderby ha.LocalSeqIndex ascending
           select ha).ToArray();

      returnActions = new playActions[actions.Length];

      for (int i = 0; i < actions.Length; i++)
      {
        returnActions[i].handId = actions[i].HandId;
        returnActions[i].localIndex = actions[i].LocalSeqIndex;
        returnActions[i].playerId = actions[i].PlayerId;
        returnActions[i].actionType = (PokerAction)actions[i].ActionTypeId;
        returnActions[i].actionValue = actions[i].ActionValue;
      }

      endRead();
      return returnActions;
    }

    /// <summary>
    /// Returns all the available information about the currentHand
    /// </summary>
    public handDetails getCurrentHandDetails()
    {
      handDetails handDetails = new handDetails();
      object returnValue = startRead("getCurrentHandDetails");

      if (returnValue != null)
        handDetails = (handDetails)returnValue;
      else
      {

        //Determine the current hand
        var currentHand =
            (from hands in pokerHands
             where hands.HandId == getCurrentHandId()
             select hands).ToArray();

        if (currentHand.Length == 1)
        {
          handDetails.currentHandExists = true;

          //pokerHand hand = currentHand[0];

          handDetails.handId = currentHand[0].HandId;
          handDetails.tableId = currentHand[0].TableId;
          handDetails.startTime = currentHand[0].StartTime;
          //handDetails.endTime = currentHand[0].EndTime;
          handDetails.numStartPlayers = currentHand[0].NumStartPlayers;
          handDetails.dealerPosition = currentHand[0].DealerPosition;
          handDetails.tableCard1 = currentHand[0].TableCard1;
          handDetails.tableCard2 = currentHand[0].TableCard2;
          handDetails.tableCard3 = currentHand[0].TableCard3;
          handDetails.tableCard4 = currentHand[0].TableCard4;
          handDetails.tableCard5 = currentHand[0].TableCard5;
          handDetails.potValue = currentHand[0].PotValue;

          //Search handActions for the current hand where the action is fold
          if (handDetails.numStartPlayers == 0)
          {
            handDetails.numStartPlayers =
                (byte)(from actions in handActions
                       where actions.HandId == getCurrentHandId() && actions.ActionTypeId == (int)PokerAction.JoinTable
                       select actions.ActionValue).Distinct().Count();

            //handDetails.numStartPlayers = (byte)satDownPlayers.Count();
          }
        }
        else
        {
          handDetails.currentHandExists = false;
        }

        addMethodReturnValue("getCurrentHandDetails", handDetails);
      }

      endRead();

      return handDetails;

    }

    /// <summary>
    /// Returns all the available information about the currentHand
    /// </summary>
    public handDetails getHandDetails(long handId)
    {
      handDetails handDetails = new handDetails();
      startRead();

      //Determine the current hand
      var currentHand =
          (from hands in pokerHands
           where hands.HandId == handId
           select hands).ToArray();

      if (currentHand.Length == 1)
      {
        handDetails.currentHandExists = true;

        //pokerHand hand = currentHand[0];

        handDetails.handId = currentHand[0].HandId;
        handDetails.tableId = currentHand[0].TableId;
        handDetails.startTime = currentHand[0].StartTime;
        //handDetails.endTime = currentHand[0].EndTime;
        handDetails.numStartPlayers = currentHand[0].NumStartPlayers;
        handDetails.dealerPosition = currentHand[0].DealerPosition;
        handDetails.tableCard1 = currentHand[0].TableCard1;
        handDetails.tableCard2 = currentHand[0].TableCard2;
        handDetails.tableCard3 = currentHand[0].TableCard3;
        handDetails.tableCard4 = currentHand[0].TableCard4;
        handDetails.tableCard5 = currentHand[0].TableCard5;
        handDetails.potValue = currentHand[0].PotValue;

        //Search handActions for the current hand where the action is fold
        if (handDetails.numStartPlayers == 0)
        {
          handDetails.numStartPlayers =
              (byte)(from actions in handActions
                     where actions.HandId == getCurrentHandId() && actions.ActionTypeId == (int)PokerAction.JoinTable
                     select actions.ActionValue).Distinct().Count();

          //handDetails.numStartPlayers = (byte)satDownPlayers.Count();
        }
      }
      else
        throw new Exception("Hand with ID " + handId.ToString() + " does not exist");

      endRead();

      return handDetails;
    }

    /// <summary>
    /// Returns the current handId. If there is no current open hand returns -1
    /// </summary>
    /// <returns></returns>
    public long getCurrentHandId()
    {
      if (currentPokerHand == null)
        return -1;
      else
        return currentPokerHand.HandId;
    }

    /// <summary>
    /// Returns the random number associated with the currentPokerHand. If there is no current hand returns 0
    /// </summary>
    /// <returns></returns>
    public int CurrentHandRandomNumber()
    {
      if (currentPokerHand == null)
        return 0;
      else
        return currentPokerHand.HandRandomNumber;
    }

    /// <summary>
    /// Sets the current hand Random number
    /// </summary>
    public void SetCurrentHandRandomNumber(int handRandomNumber)
    {
      if (currentPokerHand == null)
        throw new Exception("There is no current hand.");
      else
        currentPokerHand.HandRandomNumber = handRandomNumber;
    }

    public byte getCurrentHandSeqIndex()
    {
      return currentPokerHand.CurrentSeqIndex;
    }

    /// <summary>
    /// Returns true if a current hand exists
    /// </summary>
    /// <returns></returns>
    public bool currentHandExists()
    {
      startRead();
      bool returnValue;

      returnValue = currentPokerHand != null;

      endRead();
      return returnValue;

    }

    /// <summary>
    /// Returns an array of current known cards, including holeCards and tableCards
    /// </summary>
    /// <returns></returns>
    public byte[] KnownCurrentHandCards()
    {
      List<byte> knownCards = new List<byte>();

      startRead();

      var currentHandDetails = getCurrentHandDetails();
      if (currentHandDetails.tableCard1 > 0)
        knownCards.Add(currentHandDetails.tableCard1);
      if (currentHandDetails.tableCard2 > 0)
        knownCards.Add(currentHandDetails.tableCard2);
      if (currentHandDetails.tableCard3 > 0)
        knownCards.Add(currentHandDetails.tableCard3);
      if (currentHandDetails.tableCard4 > 0)
        knownCards.Add(currentHandDetails.tableCard4);
      if (currentHandDetails.tableCard5 > 0)
        knownCards.Add(currentHandDetails.tableCard5);

      var cards = (from hc in holeCards where (hc.HandId == getCurrentHandId()) select hc).ToArray();

      for (int i = 0; i < cards.Length; i++)
      {
        knownCards.Add(cards[i].HoleCard1);
        knownCards.Add(cards[i].HoleCard2);
      }

      endRead();

      return knownCards.ToArray();
    }


    public playerCards getPlayerHoleCards(long playerId, long handId)
    {
      startRead();

      playerCards returnCards = new playerCards();

      var cards =
          from hc in holeCards
          where (hc.HandId == handId && hc.PlayerId == playerId)
          select hc;

      if (cards.Count() != 0)
      {
        returnCards.holeCard1 = cards.First().HoleCard1;
        returnCards.holeCard2 = cards.First().HoleCard2;
      }
      else
      {
        returnCards.holeCard1 = 0;
        returnCards.holeCard2 = 0;
      }

      endRead();
      return returnCards;

    }

    /// <summary>
    /// Returns the hole cards for a player. If no hole cards are found 0's are returned.
    /// </summary>
    /// <param name="playerId"></param>
    /// <returns></returns>
    public playerCards getPlayerHoleCards(long playerId)
    {
      return getPlayerHoleCards(playerId, getCurrentHandId());
    }

    /// <summary>
    /// Returns the current round of betting. 0 - preFlop, 1 - postFlop, 2 - postTurn, 3 - postRiver
    /// </summary>
    /// <returns></returns>
    public byte getBettingRound()
    {
      startRead();

      long currentHandId = getCurrentHandId();
      //byte currentRound = 0;

      int currentRound =
          (from ac in handActions
           where ac.HandId == currentHandId && ac.ActionTypeId >= (byte)PokerAction.DealFlop && ac.ActionTypeId <= (byte)PokerAction.DealRiver
           select ac).Count();

      //Some very brief profiling seems to suggest the above linq is slightly faster
      //Search back through the hand actions and stop at the most recent dealer action
      //for (int i = handActions.Count - 1; i >= 0; i--)
      //{
      //    if (handActions[i].HandId == currentHandId)
      //    {
      //        if (handActions[i].ActionTypeId == (byte)PokerAction.DealRiver)
      //        {
      //            currentRound = 3;
      //            break;
      //        }
      //        else if (handActions[i].ActionTypeId == (byte)PokerAction.DealTurn)
      //        {
      //            currentRound = 2;
      //            break;
      //        }
      //        else if (handActions[i].ActionTypeId == (byte)PokerAction.DealFlop)
      //        {
      //            currentRound = 1;
      //            break;
      //        }
      //    }
      //}

      //currentRound = (byte)actions.Count();

      endRead();

      return (byte)currentRound;
    }

    /// <summary>
    /// Returns the amount a player has bet in the current active betting round.
    /// </summary>
    /// <param name="playerId"></param>
    /// <returns></returns>
    public decimal getPlayerCurrentRoundBetAmount(long playerId)
    {
      startRead();
      decimal currentRoundPlayerBetAmounts;

      if (getPlayerCurrentRoundBetAmount(playerId, out currentRoundPlayerBetAmounts) != CacheError.noError)
      {
        endRead();
        throw new Exception("Error determing player current round bet amount.");
      }

      endRead();
      return currentRoundPlayerBetAmounts;
    }

    /// <summary>
    /// Returns the amount a player has bet in the current active betting round.
    /// </summary>
    /// <param name="playerId"></param>
    /// <param name="localActionId">The local action id to consider up to </param>
    /// <returns></returns>
    public decimal getPlayerCurrentRoundBetAmount(long playerId, long localActionId)
    {
      startRead();
      decimal currentRoundPlayerBetAmounts;

      if (getPlayerCurrentRoundBetAmount(playerId, localActionId, out currentRoundPlayerBetAmounts) != CacheError.noError)
      {
        endRead();
        throw new Exception("Error determing player current round bet amount.");
      }

      endRead();
      return currentRoundPlayerBetAmounts;
    }

    /// <summary>
    /// Returns the amount a player has bet in the current active betting round.
    /// </summary>
    /// <param name="playerId"></param>
    /// <returns></returns>
    public CacheError getPlayerCurrentRoundBetAmount(long playerId, out decimal betAmounts)
    {
      Dictionary<long, decimal> cacheValues = startRead("getPlayerCurrentRoundBetAmount") as Dictionary<long, decimal>;
      CacheError result;

      betAmounts = -1;

      if (cacheValues != null)
      {
        lock (cacheValues)
        {
          if (cacheValues.ContainsKey(playerId))
          {
            betAmounts = cacheValues[playerId];
            endRead();
            return CacheError.noError;
          }
        }
      }

      #region buildCurrentPlayerActions

      //First check playerId is in tablePlayers if not return error
      var currentPlayer = from tp in tablePlayers
                          where tp.PlayerId == playerId
                          select tp;

      if (currentPlayer.Count() != 1)
      {
        result = new CacheError(CacheError.ErrorType.IdNumberInvalid, TableId, getCurrentHandId(), playerId, "Either there are players with same name in table or a blank playerhas been requested");
        endRead();
        return result;
      }

      if (!currentHandExists())
      {
        result = new CacheError(CacheError.ErrorType.handDoesNotExist, TableId, -1, playerId, "Current hand does not exist");
        endRead();
        return result;
      }

      byte[] lastDealerActionIdsIndex =
          (from actions in handActions
           where actions.HandId == getCurrentHandId() && (actions.ActionTypeId >= (byte)PokerAction.DealFlop && actions.ActionTypeId <= (byte)PokerAction.DealRiver)
           orderby actions.LocalSeqIndex descending
           select actions.LocalSeqIndex).ToArray();

      byte[] lastPlayerRaiseIndex =
           (from ha in handActions
            where ha.HandId == getCurrentHandId() && ha.PlayerId == playerId &&
                         (ha.ActionTypeId == (byte)PokerAction.Raise || ha.ActionTypeId == (byte)PokerAction.BigBlind || ha.ActionTypeId == (byte)PokerAction.LittleBlind)
            orderby ha.LocalSeqIndex descending
            select ha.LocalSeqIndex).ToArray();

      decimal[] currentRoundPlayerActions = new decimal[0];

      if ((lastDealerActionIdsIndex.Length == 0 && lastPlayerRaiseIndex.Length == 0) || (lastDealerActionIdsIndex == null && lastPlayerRaiseIndex == null))
      {
        //No dealer actions and no raises
        currentRoundPlayerActions =
             (from ha in handActions
              where ha.HandId == getCurrentHandId() && ha.PlayerId == playerId &&
              (ha.ActionTypeId == (byte)PokerAction.Raise ||
                 ha.ActionTypeId == (byte)PokerAction.Call ||
                 ha.ActionTypeId == (byte)PokerAction.LittleBlind ||
                 ha.ActionTypeId == (byte)PokerAction.BigBlind ||
                 ha.ActionTypeId == (byte)PokerAction.Check ||
                 ha.ActionTypeId == (byte)PokerAction.Fold)
              orderby ha.LocalSeqIndex descending
              select ha.ActionValue).ToArray();
      }
      //We need to make sure we only sum actions since this users last raise
      else if (lastDealerActionIdsIndex.Length > 0 && lastPlayerRaiseIndex.Length > 0)
      {
        currentRoundPlayerActions =
             (from ha in handActions
              where ha.HandId == getCurrentHandId() &&
                             ha.PlayerId == playerId &&
                             ha.LocalSeqIndex > lastDealerActionIdsIndex[0] && ha.LocalSeqIndex >= lastPlayerRaiseIndex[0] &&
                             (ha.ActionTypeId == (byte)PokerAction.Raise ||
                             ha.ActionTypeId == (byte)PokerAction.Call ||
                             ha.ActionTypeId == (byte)PokerAction.LittleBlind ||
                             ha.ActionTypeId == (byte)PokerAction.BigBlind ||
                             ha.ActionTypeId == (byte)PokerAction.Check ||
                             ha.ActionTypeId == (byte)PokerAction.Fold)
              orderby ha.LocalSeqIndex descending
              select ha.ActionValue).ToArray();
      }
      else if (lastDealerActionIdsIndex.Length == 0 && lastPlayerRaiseIndex.Length > 0)
      {
        currentRoundPlayerActions =
             (from ha in handActions
              where ha.HandId == getCurrentHandId() &&
                             ha.PlayerId == playerId &&
                             ha.LocalSeqIndex >= lastPlayerRaiseIndex[0] &&
                             (ha.ActionTypeId == (byte)PokerAction.Raise ||
                             ha.ActionTypeId == (byte)PokerAction.Call ||
                             ha.ActionTypeId == (byte)PokerAction.LittleBlind ||
                             ha.ActionTypeId == (byte)PokerAction.BigBlind ||
                             ha.ActionTypeId == (byte)PokerAction.Check ||
                             ha.ActionTypeId == (byte)PokerAction.Fold)
              orderby ha.LocalSeqIndex descending
              select ha.ActionValue).ToArray();
      }
      else if (lastDealerActionIdsIndex.Length > 0 && lastPlayerRaiseIndex.Length == 0)
      {
        //if the player has never even folded return an error
        //if (currentRoundPlayerActions.Length == 0)
        //{
        //    result = new CacheError(CacheError.ErrorType.IdNumberInvalid, TableId, getCurrentHandId(), playerId, "Player has never performed a betting action");
        //    endRead();
        //    return result;
        //}

        currentRoundPlayerActions =
             (from ha in handActions
              where ha.HandId == getCurrentHandId() &&
                      ha.PlayerId == playerId &&
                      ha.LocalSeqIndex > lastDealerActionIdsIndex[0] &&
                      (ha.ActionTypeId == (byte)PokerAction.Raise ||
                      ha.ActionTypeId == (byte)PokerAction.Call ||
                      ha.ActionTypeId == (byte)PokerAction.LittleBlind ||
                      ha.ActionTypeId == (byte)PokerAction.BigBlind ||
                      ha.ActionTypeId == (byte)PokerAction.Check ||
                      ha.ActionTypeId == (byte)PokerAction.Fold)
              orderby ha.LocalSeqIndex descending
              select ha.ActionValue).ToArray();
      }
      else if (currentPlayer.First().IsDead)
      {
        result = new CacheError(CacheError.ErrorType.IdNumberInvalid, TableId, getCurrentHandId(), playerId, "Current player is sat out and shouldn't be pre flop if they haven't even folded");
        endRead();
        return result;
      }
      #endregion

      if (currentRoundPlayerActions.Length == 0)
      {
        if (cacheValues != null)
        {
          lock (cacheValues)
          {
            if (!cacheValues.ContainsKey(playerId))
              cacheValues.Add(playerId, 0);
          }
        }
        else
          addMethodReturnValue("getPlayerCurrentRoundBetAmount", new Dictionary<long, decimal>() { { playerId, 0 } });

        betAmounts = 0;
        endRead();
        return CacheError.noError;
      }
      else
      {
        betAmounts = 0;
        for (int i = 0; i < currentRoundPlayerActions.Length; i++)
          betAmounts += currentRoundPlayerActions[i];

        if (cacheValues != null)
        {
          lock (cacheValues)
          {
            if (!cacheValues.ContainsKey(playerId))
              cacheValues.Add(playerId, betAmounts);
          }
        }
        else
          addMethodReturnValue("getPlayerCurrentRoundBetAmount", new Dictionary<long, decimal>() { { playerId, betAmounts } });

        endRead();
        return CacheError.noError;
      }

    }

    /// <summary>
    /// Returns the amount a player has bet in the current active betting round.
    /// </summary>
    /// <param name="playerId"></param>
    /// <param name="localActionId">The local action id to consider up to </param>
    /// <param name="betAmounts"></param>
    /// <returns></returns>
    public CacheError getPlayerCurrentRoundBetAmount(long playerId, long localActionId, out decimal betAmounts)
    {
      startRead();

      CacheError result;
      betAmounts = -1;

      #region buildCurrentPlayerActions

      //First check playerId is in tablePlayers if not return error
      var currentPlayer = from tp in tablePlayers
                          where tp.PlayerId == playerId
                          select tp;

      if (currentPlayer.Count() != 1)
      {
        result = new CacheError(CacheError.ErrorType.IdNumberInvalid, TableId, getCurrentHandId(), playerId, "Either there are players with same name in table or a blank playerhas been requested");
        endRead();
        return result;
      }

      if (!currentHandExists())
      {
        result = new CacheError(CacheError.ErrorType.handDoesNotExist, TableId, -1, playerId, "Current hand does not exist");
        endRead();
        return result;
      }

      byte[] lastDealerActionIdsIndex =
          (from actions in handActions
           where actions.HandId == getCurrentHandId() && (actions.ActionTypeId >= (byte)PokerAction.DealFlop && actions.ActionTypeId <= (byte)PokerAction.DealRiver)
           && actions.LocalSeqIndex < localActionId
           orderby actions.LocalSeqIndex descending
           select actions.LocalSeqIndex).ToArray();

      byte[] lastPlayerRaiseIndex =
           (from ha in handActions
            where ha.HandId == getCurrentHandId() && ha.PlayerId == playerId &&
                   (ha.ActionTypeId == (byte)PokerAction.Raise || ha.ActionTypeId == (byte)PokerAction.BigBlind || ha.ActionTypeId == (byte)PokerAction.LittleBlind) &&
                   ha.LocalSeqIndex < localActionId
            orderby ha.LocalSeqIndex descending
            select ha.LocalSeqIndex).ToArray();

      decimal[] currentRoundPlayerActions = new decimal[0];

      if ((lastDealerActionIdsIndex.Length == 0 && lastPlayerRaiseIndex.Length == 0) || (lastDealerActionIdsIndex == null && lastPlayerRaiseIndex == null))
      {
        //No dealer actions and no raises
        currentRoundPlayerActions =
             (from ha in handActions
              where ha.HandId == getCurrentHandId() && ha.PlayerId == playerId &&
              (ha.ActionTypeId == (byte)PokerAction.Raise ||
                 ha.ActionTypeId == (byte)PokerAction.Call ||
                 ha.ActionTypeId == (byte)PokerAction.LittleBlind ||
                 ha.ActionTypeId == (byte)PokerAction.BigBlind ||
                 ha.ActionTypeId == (byte)PokerAction.Check ||
                 ha.ActionTypeId == (byte)PokerAction.Fold) &&
                 ha.LocalSeqIndex < localActionId
              orderby ha.LocalSeqIndex descending
              select ha.ActionValue).ToArray();
      }
      //We need to make sure we only sum actions since this users last raise
      else if (lastDealerActionIdsIndex.Length > 0 && lastPlayerRaiseIndex.Length > 0)
      {
        currentRoundPlayerActions =
             (from ha in handActions
              where ha.HandId == getCurrentHandId() &&
                     ha.PlayerId == playerId &&
                     ha.LocalSeqIndex > lastDealerActionIdsIndex[0] && ha.LocalSeqIndex >= lastPlayerRaiseIndex[0] &&
                     (ha.ActionTypeId == (byte)PokerAction.Raise ||
                     ha.ActionTypeId == (byte)PokerAction.Call ||
                     ha.ActionTypeId == (byte)PokerAction.LittleBlind ||
                     ha.ActionTypeId == (byte)PokerAction.BigBlind ||
                     ha.ActionTypeId == (byte)PokerAction.Check ||
                     ha.ActionTypeId == (byte)PokerAction.Fold) &&
                     ha.LocalSeqIndex < localActionId
              orderby ha.LocalSeqIndex descending
              select ha.ActionValue).ToArray();
      }
      else if (lastDealerActionIdsIndex.Length == 0 && lastPlayerRaiseIndex.Length > 0)
      {
        currentRoundPlayerActions =
             (from ha in handActions
              where ha.HandId == getCurrentHandId() &&
                     ha.PlayerId == playerId &&
                     ha.LocalSeqIndex >= lastPlayerRaiseIndex[0] &&
                     (ha.ActionTypeId == (byte)PokerAction.Raise ||
                     ha.ActionTypeId == (byte)PokerAction.Call ||
                     ha.ActionTypeId == (byte)PokerAction.LittleBlind ||
                     ha.ActionTypeId == (byte)PokerAction.BigBlind ||
                     ha.ActionTypeId == (byte)PokerAction.Check ||
                     ha.ActionTypeId == (byte)PokerAction.Fold) &&
                     ha.LocalSeqIndex < localActionId
              orderby ha.LocalSeqIndex descending
              select ha.ActionValue).ToArray();
      }
      else if (lastDealerActionIdsIndex.Length > 0 && lastPlayerRaiseIndex.Length == 0)
      {
        //if the player has never even folded return an error
        //if (currentRoundPlayerActions.Length == 0)
        //{
        //    result = new CacheError(CacheError.ErrorType.IdNumberInvalid, TableId, getCurrentHandId(), playerId, "Player has never performed a betting action");
        //    endRead();
        //    return result;
        //}

        currentRoundPlayerActions =
             (from ha in handActions
              where ha.HandId == getCurrentHandId() &&
              ha.PlayerId == playerId &&
              ha.LocalSeqIndex > lastDealerActionIdsIndex[0] &&
              (ha.ActionTypeId == (byte)PokerAction.Raise ||
              ha.ActionTypeId == (byte)PokerAction.Call ||
              ha.ActionTypeId == (byte)PokerAction.LittleBlind ||
              ha.ActionTypeId == (byte)PokerAction.BigBlind ||
              ha.ActionTypeId == (byte)PokerAction.Check ||
              ha.ActionTypeId == (byte)PokerAction.Fold) &&
              ha.LocalSeqIndex < localActionId
              orderby ha.LocalSeqIndex descending
              select ha.ActionValue).ToArray();
      }
      else if (currentPlayer.First().IsDead)
      {
        result = new CacheError(CacheError.ErrorType.IdNumberInvalid, TableId, getCurrentHandId(), playerId, "Current player is sat out and shouldn't be pre flop if they haven't even folded");
        endRead();
        return result;
      }
      #endregion

      if (currentRoundPlayerActions.Length == 0)
      {
        betAmounts = 0;
        endRead();
        return CacheError.noError;
      }
      else
      {
        betAmounts = 0;
        for (int i = 0; i < currentRoundPlayerActions.Length; i++)
          betAmounts += currentRoundPlayerActions[i];

        endRead();
        return CacheError.noError;
      }

    }

    /// <summary>
    /// Returns the minimum amount required to continue betting in the current round, i.e. if the last raise to amount was 80, returns 80 (regardless of previous amounts bet)
    /// </summary>
    /// <returns></returns>
    public decimal getMinimumPlayAmount()
    {
      object cacheValue = startRead("getMinimumPlayAmount");
      decimal returnValue = 0;

      if (cacheValue != null)
        returnValue = (decimal)cacheValue;
      else
      {
        byte[] lastDealerActionIds =
            (from actions in handActions
             where
                 actions.HandId == getCurrentHandId() &&
                 (actions.ActionTypeId >= (byte)PokerAction.DealFlop && actions.ActionTypeId <= (byte)PokerAction.DealRiver)
             orderby actions.LocalSeqIndex descending
             select actions.LocalSeqIndex).ToArray();

        long lastDealerIndex = 0;
        if (lastDealerActionIds.Length > 0)
          lastDealerIndex = lastDealerActionIds[0];

        var raiseActionsSinceDealerAction =
            (from actions in handActions
             where
                 actions.LocalSeqIndex > lastDealerIndex &&
                 (actions.ActionTypeId == (byte)PokerAction.Raise ||
                 actions.ActionTypeId == (byte)PokerAction.BigBlind) &&
                 actions.HandId == getCurrentHandId()
             orderby actions.LocalSeqIndex descending
             select actions.ActionValue).ToArray();

        //If there have been any betting actions
        if (raiseActionsSinceDealerAction.Length > 0)
          returnValue = raiseActionsSinceDealerAction[0];

        addMethodReturnValue("getMinimumPlayAmount", returnValue);
      }

      endRead();
      return returnValue;
    }

    /// <summary>
    /// Returns the minimum amount required to continue betting in the current round, i.e. if the last raise to amount was 80, returns 80 (regardless of previous amounts bet)
    /// </summary>
    /// <param name="localActionId"></param>
    /// <returns></returns>
    public decimal getMinimumPlayAmount(long localActionId)
    {
      startRead();
      decimal returnValue = 0;

      byte[] lastDealerActionIds =
          (from actions in handActions
           where
               actions.HandId == getCurrentHandId() &&
               (actions.ActionTypeId >= (byte)PokerAction.DealFlop && actions.ActionTypeId <= (byte)PokerAction.DealRiver) && actions.LocalSeqIndex < localActionId
           orderby actions.LocalSeqIndex descending
           select actions.LocalSeqIndex).ToArray();

      long lastDealerIndex = 0;
      if (lastDealerActionIds.Length > 0)
        lastDealerIndex = lastDealerActionIds[0];

      var raiseActionsSinceDealerAction =
          (from actions in handActions
           where
               actions.LocalSeqIndex > lastDealerIndex &&
               (actions.ActionTypeId == (byte)PokerAction.Raise ||
               actions.ActionTypeId == (byte)PokerAction.BigBlind) &&
               actions.HandId == getCurrentHandId() && actions.LocalSeqIndex < localActionId
           orderby actions.LocalSeqIndex descending
           select actions.ActionValue).ToArray();

      //If there have been any betting actions
      if (raiseActionsSinceDealerAction.Length > 0)
        returnValue = raiseActionsSinceDealerAction[0];

      endRead();
      return returnValue;
    }

    /// <summary>
    /// Returns the last raise amount for the current round of betting.
    /// A raise to 80 after a previous bet of 20, return a last raise amount of 60.
    /// Exceptional cases exist where players raise all in. This method takes that into account by returning the minimum raise
    /// they would have had to carry out had the all in not existed.
    /// </summary>
    /// <returns></returns>
    public decimal getCurrentRoundLastRaiseAmount()
    {
      decimal returnValue;
      object cacheValue = startRead("getCurrentRoundLastRaiseAmount");

      if (cacheValue != null)
        returnValue = (decimal)cacheValue;
      else
      {
        //Dealer actions
        byte[] lastDealerActionIdsIndex =
            (from actions in handActions
             where actions.HandId == getCurrentHandId() && (actions.ActionTypeId >= (byte)PokerAction.DealFlop && actions.ActionTypeId <= (byte)PokerAction.DealRiver)
             orderby actions.LocalSeqIndex descending
             select actions.LocalSeqIndex).ToArray();

        //We want betting actions for the current betting round only
        decimal[] raisingActionsSinceDealerAction =
            (from actions in handActions
             where actions.LocalSeqIndex > (lastDealerActionIdsIndex.Length > 0 ? lastDealerActionIdsIndex[0] : -1) &&
                 (actions.ActionTypeId == (byte)PokerAction.Raise ||
                 actions.ActionTypeId == (byte)PokerAction.BigBlind) &&
                 actions.HandId == getCurrentHandId()
             orderby actions.LocalSeqIndex descending
             select actions.ActionValue).ToArray();

        //Now determine the last raise amount
        if (raisingActionsSinceDealerAction.Length > 1)
        {
          /////////////////
          //Original code//
          //returnValue = raisingActionsSinceDealerAction[0] - raisingActionsSinceDealerAction[1];
          /////////////////
          //Using the above code a situation can arise where someone raises all in but with an amount
          //that is less than the minimum allowed raise, i.e. from $3.00 (minimum raise would be to $4.00) and someone goes all in to $3.02
          //In order to guarantee method returns the last GREATEST raise amount we need to look at all raises for this round
          decimal[] raiseDifferences = new decimal[raisingActionsSinceDealerAction.Length - 1];
          for (int i = 0; i < raiseDifferences.Length; i++)
            raiseDifferences[i] = raisingActionsSinceDealerAction[i] - raisingActionsSinceDealerAction[i + 1];

          //Now select the biggest difference
          returnValue = raiseDifferences.Max();
        }
        else if (raisingActionsSinceDealerAction.Length == 1)
        {
          returnValue = raisingActionsSinceDealerAction[0];

          //If the value calculate this far is greater than zero (i.e. there has been a raise), but it is less than the big blind
          //it happened because someone raised all in and as such we must return the minimum legal raise amount of a big blind
          if (returnValue < BigBlind)
            returnValue = BigBlind;
        }
        else
          returnValue = 0;

        addMethodReturnValue("getCurrentRoundLastRaiseAmount", returnValue);
      }

      endRead();
      return returnValue;
    }

    /// <summary>
    /// Returns the last raise amount for the current round of betting.
    /// A raise to 80 after a previous bet of 20, return a last raise amount of 60.
    /// Exceptional cases exist where players raise all in. This method takes that into account by returning the minimum raise
    /// they would have had to carry out had the all in not existed.
    /// </summary>
    /// <param name="localActionId"></param>
    /// <returns></returns>
    public decimal getCurrentRoundLastRaiseAmount(long localActionId)
    {
      decimal returnValue;
      startRead();

      //Dealer actions
      byte[] lastDealerActionIdsIndex =
          (from actions in handActions
           where actions.HandId == getCurrentHandId() && (actions.ActionTypeId >= (byte)PokerAction.DealFlop && actions.ActionTypeId <= (byte)PokerAction.DealRiver) &&
           actions.LocalSeqIndex < localActionId
           orderby actions.LocalSeqIndex descending
           select actions.LocalSeqIndex).ToArray();

      //We want betting actions for the current betting round only
      decimal[] raisingActionsSinceDealerAction =
          (from actions in handActions
           where actions.LocalSeqIndex > (lastDealerActionIdsIndex.Length > 0 ? lastDealerActionIdsIndex[0] : -1) &&
               (actions.ActionTypeId == (byte)PokerAction.Raise ||
               actions.ActionTypeId == (byte)PokerAction.BigBlind) &&
               actions.HandId == getCurrentHandId() &&
               actions.LocalSeqIndex < localActionId
           orderby actions.LocalSeqIndex descending
           select actions.ActionValue).ToArray();

      //Now determine the last raise amount
      if (raisingActionsSinceDealerAction.Length > 1)
      {
        /////////////////
        //Original code//
        //returnValue = raisingActionsSinceDealerAction[0] - raisingActionsSinceDealerAction[1];
        /////////////////
        //Using the above code a situation can arise where someone raises all in but with an amount
        //that is less than the minimum allowed raise, i.e. from $3.00 (minimum raise would be to $4.00) and someone goes all in to $3.02
        //In order to guarantee method returns the last GREATEST raise amount we need to look at all raises for this round
        decimal[] raiseDifferences = new decimal[raisingActionsSinceDealerAction.Length - 1];
        for (int i = 0; i < raiseDifferences.Length; i++)
          raiseDifferences[i] = raisingActionsSinceDealerAction[i] - raisingActionsSinceDealerAction[i + 1];

        //Now select the biggest difference
        returnValue = raiseDifferences.Max();
      }
      else if (raisingActionsSinceDealerAction.Length == 1)
      {
        returnValue = raisingActionsSinceDealerAction[0];

        //If the value calculate this far is greater than zero (i.e. there has been a raise), but it is less than the big blind
        //it happened because someone raised all in and as such we must return the minimum legal raise amount of a big blind
        if (returnValue < BigBlind)
          returnValue = BigBlind;
      }
      else
        returnValue = 0;

      endRead();
      return returnValue;
    }



    /// <summary>
    /// Returns a byte array containing the current active player positions sorted with the startPosition at the beginning
    /// </summary>
    /// <param name="dealerIndex"></param>
    /// <returns></returns>
    public byte[] getActivePositions(byte startPosition)
    {
      startRead();

      //Get the current active positions and then reorder
      byte[] activePositions = getActivePositions();
      byte[] returnArray = new byte[activePositions.Length];

      byte writeArrayIndex = 0;

      for (byte i = 0; i < activePositions.Length; i++)
      {
        if (activePositions[i] >= (startPosition) && writeArrayIndex == 0)
        {
          returnArray[0] = activePositions[i];
          writeArrayIndex++;
        }
        else if (writeArrayIndex > 0)
        {
          returnArray[writeArrayIndex] = activePositions[i];
          writeArrayIndex++;
        }
      }

      //returnArray will now be populated upto [writeArray]
      //we need to finish it off
      byte remaningPositions = (byte)(activePositions.Length - writeArrayIndex);

      for (byte i = 0; i < remaningPositions; i++)
      {
        returnArray[writeArrayIndex] = activePositions[i];
        writeArrayIndex++;
      }

      endRead();
      return returnArray;
    }

    /// <summary>
    /// Returns a byte array containing the current active player positions for the current hand in positional order.
    /// NOTE: ALL-IN POSITIONS ARE CONSIDERED ACTIVE
    /// </summary>
    /// <returns></returns>
    public byte[] getActivePositions()
    {
      byte[] activePositions;
      object cacheValue = startRead("getActivePositions");

      if (cacheValue != null)
        activePositions = cacheValue as byte[];
      else
      {
        //Get all folded positions
        byte[] foldedPositions =
            (from actions in handActions
             join tp in tablePlayers on actions.PlayerId equals tp.PlayerId
             where actions.HandId == getCurrentHandId() && !tp.IsDead && actions.ActionTypeId == (int)PokerAction.Fold
             select tp.Position).ToArray();

        //Get all positions where people are currently sat down at
        activePositions = getSatInPositions().Except(foldedPositions).ToArray();

        addMethodReturnValue("getActivePositions", activePositions);
      }

      endRead();
      return activePositions;

    }

    /// <summary>
    /// Returns a byte array containing the current active player positions for the current hand in positional order.
    /// NOTE: ALL-IN POSITIONS ARE CONSIDERED ACTIVE
    /// </summary>
    /// <param name="localActionId"></param>
    /// <returns></returns>
    public byte[] getActivePositions(long localActionId)
    {
      byte[] activePositions;
      startRead();

      //Get all folded positions
      byte[] foldedPositions =
          (from actions in handActions
           join tp in tablePlayers on actions.PlayerId equals tp.PlayerId
           where actions.HandId == getCurrentHandId() && !tp.IsDead && actions.ActionTypeId == (int)PokerAction.Fold && actions.LocalSeqIndex < localActionId
           select tp.Position).ToArray();

      //Get all positions where people are currently sat down at
      activePositions = getSatInPositions().Except(foldedPositions).ToArray();

      endRead();
      return activePositions;

    }

    /// <summary>
    /// Returns a byte array containing the positions of all players in the current hand who are all in.
    /// </summary>
    /// <returns></returns>
    public byte[] getAllInPositions()
    {
      byte[] allInPositions;
      object cacheValue = startRead("getAllInPositions");

      if (cacheValue != null)
        allInPositions = cacheValue as byte[];
      else
      {
        allInPositions =
            (from p in tablePlayers
             join t in pokerPlayers on p.PlayerId equals t.PlayerId
             where p.TableId == currentPokerTable.TableId && p.IsDead == false && p.Stack == 0 && t.PlayerName != ""
             orderby p.Position ascending
             select p.Position).ToArray();

        addMethodReturnValue("getAllInPositions", allInPositions);
      }

      endRead();
      return allInPositions;
    }

    /// <summary>
    /// Returns a long array of all current active player ids
    /// </summary>
    /// <returns></returns>
    public long[] getActivePlayerIds()
    {
      startRead();

      byte[] activePositions = getActivePositions();
      long[] activePlayerIds = new long[activePositions.Length];

      for (int i = 0; i < activePositions.Length; i++)
        activePlayerIds[i] = getPlayerId(activePositions[i]);

      endRead();
      return activePlayerIds;
    }

    /// <summary>
    /// Returns the player Ids of the players who have been involved in the current hand
    /// </summary>
    /// <returns></returns>
    public long[] PlayerIdsStartedHand()
    {
      //We define a player having satin when they are actually in a hand
      List<long> satInPlayers = getSatInPlayerIds().ToList();

      //We now need to add any players who may have folded and immediately left the table
      List<long> foldedPlayers = (from current in handActions
                                  where current.HandId == getCurrentHandId()
                                  where current.ActionTypeId == (byte)PokerAction.Fold
                                  select current.PlayerId).ToList();

      return satInPlayers.Union(foldedPlayers).Distinct().ToArray();
    }

    /// <summary>
    /// Returns a long array of all current sat in players
    /// </summary>
    /// <returns></returns>
    public long[] getSatInPlayerIds()
    {
      long[] satInPlayerIds;
      object cacheValue = startRead("getSatInPlayerIds");

      if (cacheValue != null)
        satInPlayerIds = cacheValue as long[];
      else
      {
        satInPlayerIds =
            (from p in tablePlayers
             join t in pokerPlayers on p.PlayerId equals t.PlayerId
             where t.PlayerName != "" && p.IsDead == false //&& p.Stack > 0
             orderby p.Position ascending
             select p.PlayerId).ToArray();

        addMethodReturnValue("getSatInPlayerIds", satInPlayerIds);
      }

      endRead();
      return satInPlayerIds;
    }

    /// <summary>
    /// Returns the postitions left to act sorted in order of action starting with currentActiveTablePosition.
    /// First element is next to act and so on.
    /// </summary>
    /// <param name="playerId"></param>
    /// <returns></returns>
    public byte[] getActivePositionsLeftToAct()
    {
      byte[] positionsLeftToAct;
      object cacheValue = startRead("getActivePositionsLeftToAct");

      if (cacheValue != null)
        positionsLeftToAct = cacheValue as byte[];
      else
      {

        byte currentActiveTablePosition = getCurrentActiveTablePosition();
        byte[] activePlayerPositions = getActivePositions(currentActiveTablePosition);

        //if only one position is left active there cannot be any players left to act
        if (activePlayerPositions.Length == 1)
        {
          endRead();
          return new byte[0];
        }

        //If the pot has already been awarded then no-one is left to act
        var lastAwardPot =
            (from ha in handActions
             where ha.HandId == getCurrentHandId()
             where ha.ActionTypeId == (byte)PokerAction.WinPot || ha.ActionTypeId == (byte)PokerAction.ReturnBet
             select ha.LocalSeqIndex).ToArray();

        if (currentActiveTablePosition == Byte.MaxValue || lastAwardPot.Length > 0)
        {
          endRead();
          return new byte[0];
        }

        //Determine when the last raise occurred
        var lastRaise =
            (from ha in handActions
             where (ha.ActionTypeId == (byte)PokerAction.Raise || ha.ActionTypeId == (byte)PokerAction.DealFlop || ha.ActionTypeId == (byte)PokerAction.DealRiver || ha.ActionTypeId == (byte)PokerAction.DealTurn)
             where ha.HandId == getCurrentHandId()
             orderby ha.LocalSeqIndex descending
             select ha).ToArray();

        //If there have been raises
        if (lastRaise.Length > 0)
        {
          //If the last raise was a deal action we need to check for all in players
          if (lastRaise[0].ActionTypeId == (byte)PokerAction.DealFlop || lastRaise[0].ActionTypeId == (byte)PokerAction.DealTurn || lastRaise[0].ActionTypeId == (byte)PokerAction.DealRiver)
          {
            //In if statement to only cover unraised pot after deal only.  Cannot be applied generally due to callers needing to act to all-in raisers

            //If less than two people are still playing the hand, i.e. all but one is all in, then there are no positions left to act
            if (activePlayerPositions.Length - getAllInPositions().Length < 2)
            {
              endRead();
              return new byte[0];
            }
          }

          //If we have reached here there was a raise action but it was not a dealer action
          byte[] actedPositionsSinceLastRaise =
              (from ha in handActions
               join tp in tablePlayers on ha.PlayerId equals tp.PlayerId
               where ha.HandId == getCurrentHandId() && !tp.IsDead
               where ((ha.ActionTypeId == (byte)PokerAction.Call || ha.ActionTypeId == (byte)PokerAction.Check || ha.ActionTypeId == (byte)PokerAction.Raise) && ha.LocalSeqIndex >= lastRaise[0].LocalSeqIndex) || tp.Stack == 0
               select tp.Position).ToArray();

          positionsLeftToAct = activePlayerPositions.Except(actedPositionsSinceLastRaise).ToArray();
        }
        else
        {
          //to get here no dealer actions or raises have happened and thus the current round bet amount is a big blind

          byte[] actedPositionsSinceLastRaise =
              (from ha in handActions
               join tp in tablePlayers on ha.PlayerId equals tp.PlayerId
               where ha.HandId == getCurrentHandId() && !tp.IsDead
               where (ha.ActionTypeId == (byte)PokerAction.Call || ha.ActionTypeId == (byte)PokerAction.Check) || tp.Stack == 0
               select tp.Position).ToArray();

          positionsLeftToAct = activePlayerPositions.Except(actedPositionsSinceLastRaise).ToArray();

          //if only one position has to act, they have bet a big blind, and every other player has either sat out, folded or is all in then no one is really left to act.
          //This also implies there have been no calls or checks by people who are not then all in
          //This gets around bug where there is a single all-in caller to a big blind and thus the big blind does not need to act
          if (positionsLeftToAct.Length == 1 && getPlayerCurrentRoundBetAmount(getPlayerId(positionsLeftToAct[0])) == BigBlind)
          {
            var checkCallCount = (from ha in handActions
                                  join tp in tablePlayers on ha.PlayerId equals tp.PlayerId
                                  where ha.HandId == getCurrentHandId() && !tp.IsDead
                                  where (ha.ActionTypeId == (byte)PokerAction.Call || ha.ActionTypeId == (byte)PokerAction.Check) && tp.Stack != 0
                                  select tp.Position).ToArray();

            if (checkCallCount.Length == 0)
            {
              endRead();
              return new byte[0];
            }
          }
        }

        addMethodReturnValue("getActivePositionsLeftToAct", positionsLeftToAct);
      }

      endRead();
      return positionsLeftToAct;

    }

    #endregion pokerHand
  }
}
