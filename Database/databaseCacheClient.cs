using System;
using System.Collections.Generic;
using System.Linq;
using PokerBot.Definitions;
using ProtoBuf;

namespace PokerBot.Database
{
  [ProtoContract]
  public class databaseCacheClient : databaseCache
  {
    /// <summary>
    /// DatabaseCache constructor to be used client side to create a new table
    /// </summary>
    /// <param name="pokerClientId"></param>
    /// <param name="littleBlind"></param>
    /// <param name="bigBlind"></param>
    /// <param name="maxStack"></param>
    public databaseCacheClient(short pokerClientId, string tableName, decimal littleBlind, decimal bigBlind, decimal maxStack, byte numberSeats, HandDataSource dataSource, Random randomGen)
        : base()
    {
      baseConstructor(randomGen);
      constructor(pokerClientId, tableName, littleBlind, bigBlind, maxStack, numberSeats, dataSource);
    }

    /// <summary>
    /// DatabaseCache constructor to be used client side to create a new table
    /// </summary>
    /// <param name="pokerClientId"></param>
    /// <param name="littleBlind"></param>
    /// <param name="bigBlind"></param>
    /// <param name="maxStack"></param>
    public databaseCacheClient(short pokerClientId, string tableName, decimal littleBlind, decimal bigBlind, decimal maxStack, byte numberSeats, HandDataSource dataSource)
        : base()
    {
      baseConstructor(new Random());
      constructor(pokerClientId, tableName, littleBlind, bigBlind, maxStack, numberSeats, dataSource);
    }

    private void constructor(short pokerClientId, string tableName, decimal littleBlind, decimal bigBlind, decimal maxStack, byte numberSeats, HandDataSource dataSource)
    {
      startWrite();
      currentPokerTable = new pokerTable(pokerClientId, littleBlind, bigBlind, maxStack, tableName, numberSeats, (byte)dataSource, this, (int)(randGen.NextDouble() * int.MaxValue));

      //Not sure why we tried to sit down dead players here as they are just empty seats??
      /*for (byte i = 0; i < numberSeats; i++)
      {
        long temp = -1;
        newTablePlayer("", 0, i, true, ref temp);
      }*/

      endWrite();
    }

    /// <summary>
    /// Required for serialisation
    /// </summary>
    private databaseCacheClient()
        : base()
    {
      //Put nothing here
    }

    /// <summary>
    /// Adds a player to a table
    /// </summary>
    /// <param name="playerName"></param>
    /// <param name="playerStack"></param>
    /// <param name="playerPosition"></param>
    /// <param name="playerDead"></param>
    /// <param name="playerId"></param>
    /// <returns></returns>
    public CacheError newTablePlayer(string playerName, decimal playerStack, byte playerPosition, bool playerDead, ref long playerId)
    {
      startWrite();

      if (playerPosition >= NumSeats)
      {
        endWrite();
        throw new Exception();
      }

      //Checks first whether the player position has already been taken
      if ((from tp in tablePlayers
           join pp in pokerPlayers on tp.PlayerId equals pp.PlayerId
           where tp.Position == playerPosition && pp.PlayerName != ""
           select tp).Count() == 1)
      {
        CacheError result = new CacheError(CacheError.ErrorType.AmountInvalid, currentPokerTable.TableId, null, null, "Player position " + playerPosition.ToString() + " already taken");
        endWrite();
        return result;
      }

      //Make sure supplied player stack is greater than 0 and that if it is the blank player stack = 0
      if (playerStack < 0 || (playerName == "" && playerStack != 0))
      {
        CacheError result = new CacheError(CacheError.ErrorType.AmountInvalid, currentPokerTable.TableId, null, null, playerStack);
        endWrite();
        return result;
      }

      //Get players from pokerplayers table with name = supplied name
      var playerIdFromName =
          from pp in pokerPlayers
          where pp.PlayerName == playerName && pp.PokerClientId == currentPokerTable.PokerClientId
          select pp;

      //if count is 0 we need to add to both poker players table and table players table
      if (playerIdFromName.Count() == 0)
      {
        // add player to poker players table
        pokerPlayers.Add(new pokerPlayer(playerName, currentPokerTable.PokerClientId, this));

        //get new players id
        playerIdFromName =
            from pp in pokerPlayers
            where pp.PlayerName == playerName && pp.PokerClientId == currentPokerTable.PokerClientId
            select pp;

        //finally add to table players removing the blank player

        var blankPlayer =
            from tp in tablePlayers
            join pp in pokerPlayers on tp.PlayerId equals pp.PlayerId
            where tp.Position == playerPosition && pp.PlayerName == ""
            select tp;

        if (blankPlayer.Count() > 1)
        {
          endWrite();
          throw new Exception();
        }
        else if (blankPlayer.Count() == 1)
          tablePlayers.Remove(blankPlayer.First());

        tablePlayers.Add(new tablePlayer(currentPokerTable.TableId, playerIdFromName.First().PlayerId, playerStack, playerPosition, playerDead, this));

      }
      else
      {
        //Need to check whether player name is already in table players table
        var playerNameAlreadyInTable =
            from tp in tablePlayers
            where tp.PlayerId == playerIdFromName.First().PlayerId
            select tp;

        if (playerNameAlreadyInTable.Count() != 0 && playerName != "")
        {
          CacheError result = new CacheError(CacheError.ErrorType.PlayerNameInvalid, currentPokerTable.TableId, null, playerNameAlreadyInTable.First().PlayerId, playerName);
          endWrite();
          return result;
        }
        else  //otherwise add player to table players 
        {
          var blankPlayer =
              from tp in tablePlayers
              join pp in pokerPlayers on tp.PlayerId equals pp.PlayerId
              where tp.Position == playerPosition && pp.PlayerName == ""
              select tp;

          if (blankPlayer.Count() > 1)
          {
            endWrite();
            throw new Exception();
          }
          else if (blankPlayer.Count() == 1)
            tablePlayers.Remove(blankPlayer.First());

          tablePlayers.Add(new tablePlayer(currentPokerTable.TableId, playerIdFromName.First().PlayerId, playerStack, playerPosition, playerDead, this));
        }
      }

      //set output parameter
      playerId = playerIdFromName.First().PlayerId;
      endWrite();
      return CacheError.noError;
    }

    static List<long> deletedHandIds = new List<long>();
    static object deletedLocker = new object();

    /// <summary>
    /// Deletes all reference to the provider handId. Deletes from holeCards, handActions and pokerHands.
    /// </summary>
    /// <param name="handId"></param>
    public void DeleteEntirePokerHand(long handId)
    {
      startWrite();

      //First guarantee commit queue is empty
      databaseRAM.WaitUntilCommitQueueIsEmpty();

      lock (deletedLocker)
        deletedHandIds.Add(handId);

      //Now call the stored procedure
      databaseQueries.DeleteDatabasePokerHand(handId);

      endWrite();
    }

    /// <summary>
    /// Removes a player from tablePlayers and queues the deletion in the database.
    /// </summary>
    /// <param name="playerId"></param>
    public void removeTablePlayer(long playerId)
    {
      startWrite();

      byte removePosition;
      long newPlayerId = 0;

      //Remove a player from tablePlayers and record that in the database
      var removePlayer =
          from currentTablePlayers in tablePlayers
          where currentTablePlayers.PlayerId == playerId
          select currentTablePlayers;

      if (removePlayer.Count() == 1)
      {
        removePosition = removePlayer.First().Position;

        tablePlayers =
            (from currentTablePlayers in tablePlayers
             where currentTablePlayers.PlayerId != playerId
             select currentTablePlayers).ToList();
      }
      else
      {
        endWrite();
        throw new Exception("Cannot remove a table player who does not exist.");
      }

      newTablePlayer("", 0, removePosition, false, ref newPlayerId);

      endWrite();
    }

    /// <summary>
    /// Starts a new hand
    /// </summary>
    /// <param name="dealerPosition"></param>
    /// <param name="handId"></param>
    /// <returns></returns>
    public CacheError newHand(byte dealerPosition, ref long handId)
    {
      return newHand(dealerPosition, ref handId, DateTime.Now);
    }

    /// <summary>
    /// Starts a new hand at the specified time.
    /// </summary>
    /// <param name="dealerPosition"></param>
    /// <param name="handId"></param>
    /// <param name="startTime"></param>
    /// <returns></returns>
    public CacheError newHand(byte dealerPosition, ref long handId, DateTime startTime)
    {
      startWrite();

      var playersPlaying =
          from tp in tablePlayers
          where !tp.IsDead && tp.Stack != 0
          select tp;

      //if count is not zero a running hand exists Arhhh, otherwise add a new hand
      if (currentPokerHand == null)
        pokerHands.Add(new pokerHand(currentPokerTable.TableId, (byte)playersPlaying.Count(), dealerPosition, this, startTime, (int)(randGen.NextDouble() * int.MaxValue)));
      else
      {
        CacheError result = new CacheError(CacheError.ErrorType.HandStillOpen, currentPokerTable.TableId, currentPokerHand.HandId, null, currentPokerHand.HandId);
        endWrite();
        return result;
      }

      //set output parameter
      handId = currentPokerHand.HandId;

      endWrite();

      return CacheError.noError;
    }

    /// <summary>
    /// Adds hole cards for a given hand and player
    /// </summary>
    /// <param name="handId"></param>
    /// <param name="playerId"></param>
    /// <param name="holeCard1"></param>
    /// <param name="holeCard2"></param>
    /// <returns></returns>
    public CacheError newHoleCards(long playerId, byte holeCard1, byte holeCard2)
    {
      startWrite();

      long handId = getCurrentHandId();

      var playerOfInterest =
          from tp in tablePlayers
          join pp in pokerPlayers on tp.PlayerId equals pp.PlayerId
          where tp.PlayerId == playerId
          select new { tp, pp };

      if (playerOfInterest.Count() != 1)
      {
        CacheError result = new CacheError(CacheError.ErrorType.PlayerNameInvalid, currentPokerTable.TableId, handId, playerId, playerId);
        endWrite();
        return result;
      }

      if (playerOfInterest.First().pp.PlayerName == "")
      {
        CacheError result = new CacheError(CacheError.ErrorType.PlayerNameInvalid, currentPokerTable.TableId, handId, playerId, "");
        endWrite();
        return result;
      }

      var currentHandTableCardsMatchingHoleCard1 =
          from hands in pokerHands
          where hands.HandId == handId &&
          (hands.TableCard1 == holeCard1 ||
         hands.TableCard2 == holeCard1 ||
         hands.TableCard3 == holeCard1 ||
         hands.TableCard4 == holeCard1 ||
         hands.TableCard5 == holeCard1)
          select hands;

      var currentHandTableCardsMatchingHoleCard2 =
          from hands in pokerHands
          where hands.HandId == handId &&
          (hands.TableCard1 == holeCard2 ||
          hands.TableCard2 == holeCard2 ||
          hands.TableCard3 == holeCard2 ||
          hands.TableCard4 == holeCard2 ||
          hands.TableCard5 == holeCard2)
          select hands;

      var holeCardsDealtMatchingHoleCard1 =
          from card in holeCards
          where (card.HoleCard1 == handId && (card.HoleCard1 == holeCard1 || card.HoleCard2 == holeCard1 || card.HoleCard2 == holeCard2))
          select card;

      var holeCardsDealtMatchingHoleCard2 =
          from card in holeCards
          where (card.HoleCard1 == handId && (card.HoleCard1 == holeCard2 || card.HoleCard2 == holeCard2))
          select card;

      if (holeCardsDealtMatchingHoleCard1.Count() != 0 || currentHandTableCardsMatchingHoleCard1.Count() != 0)
      {
        CacheError result = new CacheError(CacheError.ErrorType.CardInvalid, currentPokerTable.TableId, handId, null, holeCard1);
        endWrite();
        return result;
      }

      if (holeCardsDealtMatchingHoleCard2.Count() != 0 || currentHandTableCardsMatchingHoleCard2.Count() != 0)
      {
        CacheError result = new CacheError(CacheError.ErrorType.CardInvalid, currentPokerTable.TableId, handId, null, holeCard2);
        endWrite();
        return result;
      }

      var holeCardsAlreadyDefined =
          from hc in holeCards
          where hc.HandId == handId && hc.PlayerId == playerId
          select hc;

      if (holeCardsAlreadyDefined.Count() != 0 && (holeCardsAlreadyDefined.First().HoleCard1 != holeCard1 || holeCardsAlreadyDefined.First().HoleCard2 != holeCard2))
      {
        CacheError result = new CacheError(CacheError.ErrorType.ActionError, currentPokerTable.TableId, handId, playerId, "Hole cards already defined for given player");
        endWrite();
        return result;
      }

      if (holeCardsAlreadyDefined.Count() == 0)
        holeCards.Add(new holeCard(handId, playerId, holeCard1, holeCard2, this));

      endWrite();

      return CacheError.noError;
    }

    /// <summary>
    /// Update table cards (only if value provided is greater than 0)
    /// </summary>
    /// <param name="handId"></param>
    /// <param name="tableCard1"></param>
    /// <param name="tableCard2"></param>
    /// <param name="tableCard3"></param>
    /// <param name="tableCard4"></param>
    /// <param name="tableCard5"></param>
    /// <returns></returns>
    public CacheError updateTableCards(byte tableCard1, byte tableCard2, byte tableCard3, byte tableCard4, byte tableCard5)
    {
      startWrite();

      long handId = getCurrentHandId();

      var currentHandWithDealer =
          from hands in pokerHands
          join player in tablePlayers on hands.DealerPosition equals player.Position
          where hands.HandId == handId
          select new { hands, player };

      if (currentHandWithDealer.Count() != 1)
      {
        CacheError result = new CacheError(CacheError.ErrorType.IdNumberInvalid, currentPokerTable.TableId, handId, currentHandWithDealer.First().player.PlayerId, handId);
        endWrite();
        return result;
      }

      List<byte> drawnCards = new List<byte>(21);

      if (currentHandWithDealer.First().hands.TableCard1 != 0)
        drawnCards.Add(currentHandWithDealer.First().hands.TableCard1);
      if (currentHandWithDealer.First().hands.TableCard2 != 0)
        drawnCards.Add(currentHandWithDealer.First().hands.TableCard2);
      if (currentHandWithDealer.First().hands.TableCard3 != 0)
        drawnCards.Add(currentHandWithDealer.First().hands.TableCard3);
      if (currentHandWithDealer.First().hands.TableCard4 != 0)
        drawnCards.Add(currentHandWithDealer.First().hands.TableCard4);
      if (currentHandWithDealer.First().hands.TableCard5 != 0)
        drawnCards.Add(currentHandWithDealer.First().hands.TableCard5);

      var dealtHoleCards =
          from hc in holeCards
          where hc.HandId == handId
          select hc;

      foreach (holeCard hc in dealtHoleCards)
      {
        drawnCards.Add(hc.HoleCard1);
        drawnCards.Add(hc.HoleCard2);
      }

      if (tableCard1 != 0 && tableCard1 != currentHandWithDealer.First().hands.TableCard1)
      {
        if (currentHandWithDealer.First().hands.TableCard1 == 0 && !drawnCards.Contains(tableCard1))
        {
          currentHandWithDealer.First().hands.TableCard1 = tableCard1;
          drawnCards.Add(tableCard1);
        }
        else
        {
          CacheError result = new CacheError(CacheError.ErrorType.CardInvalid, currentPokerTable.TableId, handId, currentHandWithDealer.First().player.PlayerId, tableCard1);
          endWrite();
          return result;
        }
      }

      if (tableCard2 != 0 && tableCard2 != currentHandWithDealer.First().hands.TableCard2)
      {
        if (currentHandWithDealer.First().hands.TableCard2 == 0 && !drawnCards.Contains(tableCard2))
        {
          currentHandWithDealer.First().hands.TableCard2 = tableCard2;
          drawnCards.Add(tableCard2);
        }
        else
        {
          CacheError result = new CacheError(CacheError.ErrorType.CardInvalid, currentPokerTable.TableId, handId, currentHandWithDealer.First().player.PlayerId, tableCard2);
          endWrite();
          return result;
        }
      }

      if (tableCard3 != 0 && tableCard3 != currentHandWithDealer.First().hands.TableCard3)
      {
        if (currentHandWithDealer.First().hands.TableCard3 == 0 && !drawnCards.Contains(tableCard3))
        {
          currentHandWithDealer.First().hands.TableCard3 = tableCard3;
          drawnCards.Add(tableCard3);
        }
        else
        {
          CacheError result = new CacheError(CacheError.ErrorType.CardInvalid, currentPokerTable.TableId, handId, currentHandWithDealer.First().player.PlayerId, tableCard3);
          endWrite();
          return result;
        }
      }

      if (tableCard4 != 0 && tableCard4 != currentHandWithDealer.First().hands.TableCard4)
      {
        if (currentHandWithDealer.First().hands.TableCard4 == 0 && !drawnCards.Contains(tableCard4))
        {
          currentHandWithDealer.First().hands.TableCard4 = tableCard4;
          drawnCards.Add(tableCard4);
        }
        else
        {
          CacheError result = new CacheError(CacheError.ErrorType.CardInvalid, currentPokerTable.TableId, handId, currentHandWithDealer.First().player.PlayerId, tableCard4);
          endWrite();
          return result;
        }
      }

      if (tableCard5 != 0 && tableCard5 != currentHandWithDealer.First().hands.TableCard5)
      {
        if (currentHandWithDealer.First().hands.TableCard5 == 0 && !drawnCards.Contains(tableCard5))
        {
          currentHandWithDealer.First().hands.TableCard5 = tableCard5;
          drawnCards.Add(tableCard5);
        }
        else
        {
          CacheError result = new CacheError(CacheError.ErrorType.CardInvalid, currentPokerTable.TableId, handId, currentHandWithDealer.First().player.PlayerId, tableCard5);
          endWrite();
          return result;
        }
      }

      endWrite();

      return CacheError.noError;

    }

    /// <summary>
    /// Swaps the positions of two players
    /// </summary>
    /// <param name="player1Id"></param>
    /// <param name="player2Id"></param>
    /// <returns></returns>
    public CacheError swapPlayerPositions(byte player1Position, byte player2Position)
    {
      //No longer implmented as actionId has been removed.
      throw new NotImplementedException();

      #region OldCode
      //startWrite();

      //var playerName = getPlayerName(getPlayerId(player1Position));

      //if (playerName != "")
      //{

      //    //First fix the handAction entry for position of player1
      //    var sitDownInCachePlayer1 =
      //        from actions in handActions
      //        where actions.PlayerId == getPlayerId(player1Position) && actions.ActionTypeId == (byte)PokerAction.JoinTable
      //        select actions;

      //    if (sitDownInCachePlayer1.Count() == 1)
      //        sitDownInCachePlayer1.First().UpdatePlayerPosition(player2Position);
      //    else
      //    {
      //        var sitDownInDatabasePlayer1 = from
      //            actions in databaseCurrent.tbl_handActions
      //                                       join hands in databaseCurrent.tbl_hands on actions.handId equals hands.id
      //                                       where hands.tableId == TableId
      //                                       where actions.playerId == getPlayerId(player1Position) && actions.actionTypeId == (byte)PokerAction.JoinTable
      //                                       orderby actions.id descending
      //                                       select actions;

      //        if (sitDownInDatabasePlayer1.Count() > 0)
      //        {
      //            handAction sitDownAction = new handAction(sitDownInDatabasePlayer1.First().id, this);
      //            sitDownAction.UpdatePlayerPosition(player2Position);
      //        }
      //        else
      //        {
      //            CacheError result = new CacheError(CacheError.ErrorType.IdNumberInvalid, TableId, null, null, "Player with position " + player1Position.ToString() + " does not have a sitdown action entry in cache or database.");
      //            endWrite();
      //            return result;
      //        }
      //    }
      //}

      //playerName = getPlayerName(getPlayerId(player2Position));

      //if (playerName != "")
      //{
      //    //Now fix the handAction entry for position of player2
      //    var sitDownInCachePlayer2 =
      //        from actions in handActions
      //        where actions.PlayerId == getPlayerId(player2Position) && actions.ActionTypeId == (byte)PokerAction.JoinTable
      //        select actions;

      //    if (sitDownInCachePlayer2.Count() == 1)
      //        sitDownInCachePlayer2.First().UpdatePlayerPosition(player1Position);
      //    else
      //    {
      //        var sitDownInDatabasePlayer2 = from
      //            actions in databaseCurrent.tbl_handActions
      //                                       join hands in databaseCurrent.tbl_hands on actions.handId equals hands.id
      //                                       where hands.tableId == TableId
      //                                       where actions.playerId == getPlayerId(player2Position) && actions.actionTypeId == (byte)PokerAction.JoinTable
      //                                       orderby actions.id descending
      //                                       select actions;

      //        if (sitDownInDatabasePlayer2.Count() > 0)
      //        {
      //            handAction sitDownAction = new handAction(sitDownInDatabasePlayer2.First().id, this);
      //            sitDownAction.UpdatePlayerPosition(player2Position);
      //        }
      //        else
      //        {
      //            CacheError result = new CacheError(CacheError.ErrorType.IdNumberInvalid, TableId, null, null, "Player with position " + player2Position.ToString() + " does not have a sitdown action entry in cache or database.");
      //            endWrite();
      //            return result;
      //        }
      //    }
      //}

      ////Now fix position entries in tablePlayers
      //var playersInTablePlayers =
      //    from tp in tablePlayers
      //    where tp.Position == player1Position || tp.Position == player2Position
      //    select tp;

      //if (playersInTablePlayers.Count() != 2)
      //{
      //    CacheError result = new CacheError(CacheError.ErrorType.IdNumberInvalid, TableId, null, null, "Players with position's " + player1Position.ToString() + " and " + player2Position.ToString() + " are not both in tablePlayers");
      //    endWrite();
      //    return result;
      //}

      //byte temp = playersInTablePlayers.First().Position;
      //playersInTablePlayers.First().Position = playersInTablePlayers.ElementAt(1).Position;
      //playersInTablePlayers.ElementAt(1).Position = temp;

      ////The positions have now been swapped in tablePlayers but we ened to update the entry in handActions as well
      ////If the entry is not in handActions then we need to go direct to the database

      //endWrite();

      //return CacheError.noError; 
      #endregion

    }

    /// <summary>
    /// Records a hand action
    /// </summary>
    /// <param name="handId"></param>
    /// <param name="playerId"></param>
    /// <param name="?"></param>
    /// <returns></returns>
    public CacheError newHandAction(long playerId, PokerAction action, decimal actionValue)
    {
      startWrite();

      if (actionValue < 0)
        return new CacheError(CacheError.ErrorType.AmountInvalid, TableId, currentPokerHand.HandId, playerId, actionValue);

      long handId = getCurrentHandId();

      /*
      lock (deletedLocker)
          if (deletedHandIds.Contains(handId))
              return new CacheError(CacheError.ErrorType.IdNumberInvalid, TableId, handId, playerId, actionValue);
      */

      if (action == PokerAction.NoAction)
      {
        CacheError result = new CacheError(CacheError.ErrorType.ActionError, currentPokerTable.TableId, handId, playerId, action);
        endWrite();
        return result;
      }

      var playerOfInterest =
          from tp in tablePlayers
          where tp.PlayerId == playerId
          select tp;

      var handOfInterest =
          from ph in pokerHands
          where ph.HandId == handId
          select ph;

      if (playerOfInterest.Count() < 1)
      {
        CacheError result = new CacheError(CacheError.ErrorType.IdNumberInvalid, currentPokerTable.TableId, handId, playerId, playerId);
        endWrite();
        return result;
      }

      if (handOfInterest.Count() != 1)
      {
        CacheError result = new CacheError(CacheError.ErrorType.IdNumberInvalid, currentPokerTable.TableId, handId, playerId, handId);
        endWrite();
        return result;
      }

      #region Big blind

      if (action == PokerAction.BigBlind)
      {
        if (actionValue != currentPokerTable.BigBlind || (playerOfInterest.First().Stack < actionValue && (playerOfInterest.First().Stack - actionValue != 0)))
        {
          CacheError result = new CacheError(CacheError.ErrorType.AmountInvalid, currentPokerTable.TableId, handId, playerId, actionValue);
          endWrite();
          return result;
        }

        if (playerOfInterest.First().IsDead)
        {
          var previousBettingActions =
              from ha in handActions
              join tp in tablePlayers on ha.PlayerId equals tp.PlayerId
              where tp.PlayerId == playerId &&
                      (ha.ActionTypeId == (byte)PokerAction.BigBlind ||
                      ha.ActionTypeId == (byte)PokerAction.Call ||
                      ha.ActionTypeId == (byte)PokerAction.Check ||
                      ha.ActionTypeId == (byte)PokerAction.Fold ||
                      ha.ActionTypeId == (byte)PokerAction.Raise ||
                      ha.ActionTypeId == (byte)PokerAction.LittleBlind)
              select ha;

          if (previousBettingActions.Count() != 0)
          {
            CacheError result = new CacheError(CacheError.ErrorType.ActionError, TableId, handId, playerOfInterest.First().PlayerId, "Sat out players cannot place bets!!!!");
            endWrite();
            return result;
          }
          else
          {
            CacheError cacheResult = newHandAction(playerId, PokerAction.SitIn, playerOfInterest.First().Position);
            if (cacheResult != CacheError.noError)
            {
              endWrite();
              return cacheResult;
            }
          }
        }

        if (actionValue > playerOfInterest.First().Stack)
          actionValue = playerOfInterest.First().Stack;

        playerOfInterest.First().Stack -= actionValue;
        handOfInterest.First().PotValue += actionValue;
        handActions.Add(new handAction(handId, playerId, (byte)action, actionValue, this));

        endWrite();

        return CacheError.noError;
      }

      #endregion

      #region Little blind

      if (action == PokerAction.LittleBlind)
      {
        if (actionValue != currentPokerTable.LittleBlind || (playerOfInterest.First().Stack < actionValue && (playerOfInterest.First().Stack - actionValue != 0)))
        {
          CacheError result = new CacheError(CacheError.ErrorType.AmountInvalid, currentPokerTable.TableId, handId, playerId, actionValue);
          endWrite();
          return result;
        }

        #region OLD

        /*
        var lastDealerActionId =
            from actions in handActions
            where actions.HandId == handId && (actions.ActionTypeId >= (byte)PokerAction.DealFlop && actions.ActionTypeId <= (byte)PokerAction.DealRiver)
            orderby actions.LocalIndex descending
            select actions;

        if (lastDealerActionId.Count() != 0)
        {
            var bettingActionsSinceDealerAction =
               from actions in handActions
               where actions.LocalIndex > lastDealerActionId.First().LocalIndex && actions.HandId == handId && actions.ActionTypeId == (byte)PokerAction.LittleBlind
               orderby actions.LocalIndex descending
               select actions;

            if (bettingActionsSinceDealerAction.Count() != 0)
            {
                CacheError result = new CacheError(CacheError.ErrorType.ActionError, currentPokerTable.TableId, handId, bettingActionsSinceDealerAction.First().PlayerId, "Big blind already played");
                endWrite();
                return result;
            }
        }
        else
        {
            var bettingActionsSinceDealerAction =
               from actions in handActions
               where actions.HandId == handId && actions.ActionTypeId == (byte)PokerAction.LittleBlind
               orderby actions.LocalIndex descending
               select actions;

            if (bettingActionsSinceDealerAction.Count() != 0)
            {
                CacheError result = new CacheError(CacheError.ErrorType.ActionError, currentPokerTable.TableId, handId, bettingActionsSinceDealerAction.First().PlayerId, "Big blind already played");
                endWrite();
                return result;
            }
        }
        */

        #endregion

        var littleBlindActionsThisHand =
            (from ha in handActions
             where ha.HandId == handId && ha.ActionTypeId == (byte)PokerAction.LittleBlind
             select ha).Count() > 0;

        if (littleBlindActionsThisHand)
        {
          CacheError result = new CacheError(CacheError.ErrorType.ActionError, currentPokerTable.TableId, handId, playerId, "Little blind already played");
          endWrite();
          return result;
        }

        if (actionValue > playerOfInterest.First().Stack)
          actionValue = playerOfInterest.First().Stack;

        playerOfInterest.First().Stack -= actionValue;
        handOfInterest.First().PotValue += actionValue;
        handActions.Add(new handAction(handId, playerId, (byte)action, actionValue, this));
        endWrite();
        return CacheError.noError;
      }

      #endregion

      #region Dealing actions

      if (action == PokerAction.DealFlop || action == PokerAction.DealRiver || action == PokerAction.DealTurn)
      {
        if (getActivePositionsLeftToAct().Length != 0)
        {
          endWrite();
          return new CacheError(CacheError.ErrorType.ActionError, TableId, getCurrentHandId(), null, "Cannot end betting round with players still to act");
        }

        int numberDealActions =
            (from actions in handActions
             where (actions.ActionTypeId == (byte)PokerAction.DealFlop || actions.ActionTypeId == (byte)PokerAction.DealTurn ||
                 actions.ActionTypeId == (byte)PokerAction.DealRiver) && actions.HandId == handId
             select action).Count();

        if ((action == PokerAction.DealFlop && numberDealActions != 0) ||
            (action == PokerAction.DealTurn && numberDealActions != 1) ||
            (action == PokerAction.DealRiver && numberDealActions != 2))
        {
          endWrite();
          return new CacheError(CacheError.ErrorType.ActionError, TableId, handId, playerId, "Error with deal action out of place action was " + action.ToString() + ", number of deal actions previously this hand was " + numberDealActions.ToString());
        }

        handActions.Add(new handAction(handId, playerId, (byte)action, actionValue, this));
        endWrite();
        return CacheError.noError;
      }

      #endregion

      #region Add stack cash

      if (action == PokerAction.AddStackCash)
      {
        if (actionValue <= 0)
        {
          CacheError result = new CacheError(CacheError.ErrorType.AmountInvalid, currentPokerTable.TableId, handId, playerId, actionValue);
          endWrite();
          return result;
        }

        playerOfInterest.First().Stack += actionValue;
        handActions.Add(new handAction(handId, playerId, (byte)action, actionValue, this));
        endWrite();
        return CacheError.noError;
      }

      #endregion

      #region Check function

      if (action == PokerAction.Check)
      {
        #region Old

        /*
        var lastDealerActionIds =
            from actions in handActions.Select((o, i) => new { Value = o, Index = i })
            where actions.Value.HandId == handId && (actions.Value.ActionTypeId >= (byte)PokerAction.DealFlop && actions.Value.ActionTypeId <= (byte)PokerAction.DealRiver)
            orderby actions.Index descending
            select actions.Index;

        var bettingActionsSinceDealerAction =
                from actions in handActions.Select((o, i) => new { Value = o, Index = i })
                where (actions.Value.ActionTypeId == (byte)PokerAction.Check ||
                    actions.Value.ActionTypeId == (byte)PokerAction.Call ||
                    actions.Value.ActionTypeId == (byte)PokerAction.Raise ||
                    actions.Value.ActionTypeId == (byte)PokerAction.LittleBlind ||
                    actions.Value.ActionTypeId == (byte)PokerAction.BigBlind) &&
                    actions.Value.HandId == handId
                orderby actions.Index descending
                select actions.Value;

        if (lastDealerActionIds.Count() != 0)
        {
            var lastDealerActionId = lastDealerActionIds.First();

            bettingActionsSinceDealerAction =
                from actions in handActions.Select((o, i) => new { Value = o, Index = i })
                where actions.Index > lastDealerActionId &&
                (actions.Value.ActionTypeId == (byte)PokerAction.Check ||
                    actions.Value.ActionTypeId == (byte)PokerAction.Call ||
                    actions.Value.ActionTypeId == (byte)PokerAction.Raise ||
                    actions.Value.ActionTypeId == (byte)PokerAction.LittleBlind ||
                    actions.Value.ActionTypeId == (byte)PokerAction.BigBlind) &&
                    actions.Value.HandId == handId
                orderby actions.Index descending
                select actions.Value;
        }

        if (bettingActionsSinceDealerAction.Count() != 0)
        {

            var lastRaise =
                from ha in bettingActionsSinceDealerAction.Select((o, i) => new { Value = o, Index = i })
                where ha.Value.ActionTypeId == (byte)PokerAction.Raise || ha.Value.ActionTypeId == (byte)PokerAction.BigBlind
                orderby ha.Index ascending
                select ha.Value;

            decimal lastRaiseAmount;
            if (lastRaise.Count() != 0)
                lastRaiseAmount = lastRaise.First().ActionValue;
            else
                lastRaiseAmount = 0;

            var playerBetsSinceLastDealerAction =
                from ba in bettingActionsSinceDealerAction
                where ba.PlayerId == playerId
                select ba.ActionValue;

            decimal playerBetAmount = 0;
            foreach (var bet in playerBetsSinceLastDealerAction)
                playerBetAmount += bet;
            */
        #endregion

        decimal playerBetAmount = getPlayerCurrentRoundBetAmount(playerId);
        decimal lastRaiseAmount = getMinimumPlayAmount();

        if (playerBetAmount != lastRaiseAmount)
        {
          CacheError result = new CacheError(CacheError.ErrorType.AmountInvalid, currentPokerTable.TableId, handId, playerId, actionValue);
          endWrite();
          return result;
        }

        handActions.Add(new handAction(handId, playerId, (byte)action, actionValue, this));
        endWrite();
        return CacheError.noError;
      }

      #endregion

      #region Call function

      if (action == PokerAction.Call)
      {
        if (playerOfInterest.First().Stack < actionValue && (playerOfInterest.First().Stack - actionValue != 0))
        {
          CacheError result = new CacheError(CacheError.ErrorType.AmountInvalid, currentPokerTable.TableId, handId, playerId, actionValue);
          endWrite();
          return result;
        }

        //Never allow call amount of 0
        if (actionValue == 0)
        {
          CacheError result = new CacheError(CacheError.ErrorType.AmountInvalid, currentPokerTable.TableId, handId, playerId, actionValue);
          endWrite();
          return result;
        }

        #region Old
        /*
        var lastDealerActionIds =
            from actions in handActions.Select((o, i) => new { Value = o, Index = i })
            where actions.Value.HandId == handId && (actions.Value.ActionTypeId >= (byte)PokerAction.DealFlop && actions.Value.ActionTypeId <= (byte)PokerAction.DealRiver)
            orderby actions.Index descending
            select actions.Index;

        var bettingActionsSinceDealerAction =
                from actions in handActions.Select((o, i) => new { Value = o, Index = i })
                where (actions.Value.ActionTypeId == (byte)PokerAction.Check ||
                    actions.Value.ActionTypeId == (byte)PokerAction.Call ||
                    actions.Value.ActionTypeId == (byte)PokerAction.Raise ||
                    actions.Value.ActionTypeId == (byte)PokerAction.LittleBlind ||
                    actions.Value.ActionTypeId == (byte)PokerAction.BigBlind) &&
                    actions.Value.HandId == handId
                orderby actions.Index descending
                select actions.Value;

        if (lastDealerActionIds.Count() != 0)
        {
            var lastDealerActionId = lastDealerActionIds.First();

            bettingActionsSinceDealerAction =
                from actions in handActions.Select((o, i) => new { Value = o, Index = i })
                where actions.Index > lastDealerActionId &&
                (actions.Value.ActionTypeId == (byte)PokerAction.Check ||
                    actions.Value.ActionTypeId == (byte)PokerAction.Call ||
                    actions.Value.ActionTypeId == (byte)PokerAction.Raise ||
                    actions.Value.ActionTypeId == (byte)PokerAction.LittleBlind ||
                    actions.Value.ActionTypeId == (byte)PokerAction.BigBlind) &&
                    actions.Value.HandId == handId
                orderby actions.Index descending
                select actions.Value;
        }

        if (bettingActionsSinceDealerAction.Count() != 0)
        {

            var lastRaise =
                from ha in bettingActionsSinceDealerAction.Select((o, i) => new { Value = o, Index = i })
                where ha.Value.ActionTypeId == (byte)PokerAction.Raise || ha.Value.ActionTypeId == (byte)PokerAction.BigBlind
                orderby ha.Index ascending
                select ha.Value;

            decimal lastRaiseAmount;
            if (lastRaise.Count() != 0)
                lastRaiseAmount = lastRaise.First().ActionValue;
            else
                lastRaiseAmount = 0;

            var playerBetsSinceLastDealerAction =
                from ba in bettingActionsSinceDealerAction.Select((o, i) => new { Value = o, Index = i })
                where ba.Value.PlayerId == playerId
                orderby ba.Index descending
                select ba.Value;

            decimal playerBetAmount = 0;
            foreach (var bet in playerBetsSinceLastDealerAction)
            {
                if (bet.ActionTypeId == (byte)PokerAction.Raise)
                    playerBetAmount = bet.ActionValue;
                else
                    playerBetAmount += bet.ActionValue;
            }
            */
        #endregion

        decimal playerBetAmount = getPlayerCurrentRoundBetAmount(playerId);
        decimal lastRaiseAmount = getMinimumPlayAmount();

        if ((actionValue + playerBetAmount != lastRaiseAmount && playerOfInterest.First().Stack - actionValue != 0) || playerOfInterest.First().Stack - actionValue < 0 ||
            (playerOfInterest.First().Stack - actionValue == 0 && actionValue + playerBetAmount > lastRaiseAmount))
        {
          CacheError result = new CacheError(CacheError.ErrorType.AmountInvalid, currentPokerTable.TableId, handId, playerId, actionValue);
          endWrite();
          return result;
        }

        //Old}

        playerOfInterest.First().Stack -= actionValue;
        handOfInterest.First().PotValue += actionValue;
        handActions.Add(new handAction(handId, playerId, (byte)action, actionValue, this));
        endWrite();
        return CacheError.noError;
      }

      #endregion

      #region Raise

      //Deal with Check, call, raise
      if (action == PokerAction.Raise)
      {
        #region Old

        /*
        var lastDealerActionIds =
            from actions in handActions.Select((o, i) => new { Value = o, Index = i })
            where actions.Value.HandId == handId && (actions.Value.ActionTypeId >= (byte)PokerAction.DealFlop && actions.Value.ActionTypeId <= (byte)PokerAction.DealRiver)
            orderby actions.Index descending
            select actions.Index;

        var bettingActionsSinceDealerAction =
                from actions in handActions.Select((o, i) => new { Value = o, Index = i })
                where (actions.Value.ActionTypeId == (byte)PokerAction.Check ||
                    actions.Value.ActionTypeId == (byte)PokerAction.Call ||
                    actions.Value.ActionTypeId == (byte)PokerAction.Raise ||
                    actions.Value.ActionTypeId == (byte)PokerAction.LittleBlind ||
                    actions.Value.ActionTypeId == (byte)PokerAction.BigBlind) &&
                    actions.Value.HandId == handId
                orderby actions.Index descending
                select actions.Value;

        if (lastDealerActionIds.Count() != 0)
        {
            var lastDealerActionId = lastDealerActionIds.First();

            bettingActionsSinceDealerAction =
                from actions in handActions.Select((o, i) => new { Value = o, Index = i })
                where actions.Index > lastDealerActionId &&
                (actions.Value.ActionTypeId == (byte)PokerAction.Check ||
                    actions.Value.ActionTypeId == (byte)PokerAction.Call ||
                    actions.Value.ActionTypeId == (byte)PokerAction.Raise ||
                    actions.Value.ActionTypeId == (byte)PokerAction.LittleBlind ||
                    actions.Value.ActionTypeId == (byte)PokerAction.BigBlind) &&
                    actions.Value.HandId == handId
                orderby actions.Index descending
                select actions.Value;
        }

        if (bettingActionsSinceDealerAction.Count() != 0)
        {
            /*
            var lastRaise =
                from ha in bettingActionsSinceDealerAction.Select((o, i) => new { Value = o, Index = i })
                where ha.Value.ActionTypeId == (byte)PokerAction.Raise || ha.Value.ActionTypeId == (byte)PokerAction.BigBlind
                orderby ha.Index ascending
                select ha.Value;

            decimal lastRaiseAmount;
            if (lastRaise.Count() != 0)
                lastRaiseAmount = lastRaise.First().ActionValue;
            else
                lastRaiseAmount = 0;

            var playerBetsSinceLastDealerAction =
                from ba in bettingActionsSinceDealerAction.Select((o, i) => new { Value = o, Index = i })
                where ba.Value.PlayerId == playerId
                orderby ba.Index ascending
                select ba.Value;

            decimal playerBetAmount = 0;
            foreach (var bet in playerBetsSinceLastDealerAction)
            {
                if (bet.ActionTypeId == (byte)PokerAction.Raise)
                    playerBetAmount = bet.ActionValue;
                else
                    playerBetAmount += bet.ActionValue;
            }
            */

        #endregion

        decimal playerBetAmount = getPlayerCurrentRoundBetAmount(playerId);
        decimal lastRaiseAmount = getMinimumPlayAmount();

        if (actionValue <= lastRaiseAmount || playerBetAmount >= actionValue)
        {
          CacheError result = new CacheError(CacheError.ErrorType.AmountInvalid, currentPokerTable.TableId, handId, playerId, actionValue);
          endWrite();
          return result;
        }
        else
        {
          playerOfInterest.First().Stack += playerBetAmount;
          handOfInterest.First().PotValue -= playerBetAmount;
        }

        //Old}

        if (playerOfInterest.First().Stack < actionValue)
        {
          CacheError result = new CacheError(CacheError.ErrorType.AmountInvalid, currentPokerTable.TableId, handId, playerId, actionValue);
          endWrite();
          return result;
        }

        playerOfInterest.First().Stack -= actionValue;
        handOfInterest.First().PotValue += actionValue;
        handActions.Add(new handAction(handId, playerId, (byte)action, actionValue, this));
        endWrite();
        return CacheError.noError;
      }

      #endregion

      #region Fold action

      //Deal with fold action
      if (action == PokerAction.Fold)
      {

        if ((from actions in handActions
             where (actions.HandId == handId) && (actions.PlayerId == playerId) && (actions.ActionTypeId == (byte)PokerAction.Fold)
             select actions).Count() == 1)
        {
          CacheError result = new CacheError(CacheError.ErrorType.ActionError, currentPokerTable.TableId, handId, playerId, action);
          endWrite();
          return result;
        }
        else
        {
          handActions.Add(new handAction(handId, playerId, (byte)action, actionValue, this));
          endWrite();
          return CacheError.noError;
        }
      }

      #endregion

      #region Win pot

      //Deal with win pot action
      if (action == PokerAction.WinPot)
      {
        var playerFolded =
            from ha in handActions
            where ha.HandId == handId
            && ha.PlayerId == playerId
            && ha.ActionTypeId == (byte)PokerAction.Fold
            select ha;

        if (playerFolded.Count() != 0)
        {
          CacheError result = new CacheError(CacheError.ErrorType.ActionError, TableId, handId, playerId, "Player has folded cannot win pot");
          endWrite();
          return result;
        }

        //Get all pot win actions that have occurred this hand
        var potWinActions =
            from actions in handActions
            where (actions.ActionTypeId == (byte)PokerAction.WinPot || actions.ActionTypeId == (byte)PokerAction.TableRake) &&
            actions.HandId == handId
            select actions;

        //Subtract pot win values from this hand off total pot to check remaining pot to allocate
        decimal remainingPotTotal = handOfInterest.First().PotValue;
        foreach (handAction handAction in potWinActions)
          remainingPotTotal -= handAction.ActionValue;

        //If there is not enough pot left throw an error
        if (remainingPotTotal < actionValue)
        {
          CacheError result = new CacheError(CacheError.ErrorType.AmountInvalid, currentPokerTable.TableId, handId, playerId, remainingPotTotal);
          endWrite();
          return result;
        }

        //Otherwise add winnings to players stack, add action to actions table and return no error
        playerOfInterest.First().Stack += actionValue;
        handActions.Add(new handAction(handId, playerId, (byte)action, actionValue, this));
        endWrite();
        return CacheError.noError;
      }

      #endregion

      #region TableRake

      if (action == PokerAction.TableRake)
      {
        //Get all pot win actions that have occurred this hand
        var potWinActions =
            from actions in handActions
            where (actions.ActionTypeId == (byte)PokerAction.WinPot || actions.ActionTypeId == (byte)PokerAction.TableRake) &&
            actions.HandId == handId
            select actions;

        //Subtract pot win values from this hand off total pot to check remaining pot to allocate
        decimal remainingPotTotal = handOfInterest.First().PotValue;
        foreach (handAction handAction in potWinActions)
          remainingPotTotal -= handAction.ActionValue;

        //If there is not enough pot left throw an error
        if (remainingPotTotal < actionValue)
        {
          CacheError result = new CacheError(CacheError.ErrorType.AmountInvalid, currentPokerTable.TableId, handId, playerId, remainingPotTotal);
          endWrite();
          return result;
        }

        //Otherwise add winnings to players stack, add action to actions table and return no error
        handActions.Add(new handAction(handId, playerId, (byte)action, actionValue, this));
        endWrite();
        return CacheError.noError;
      }

      #endregion

      #region Return Bet

      if (action == PokerAction.ReturnBet)
      {
        #region Old
        /*
        var lastDealerActionIds =
            from actions in handActions.Select((o, i) => new { Value = o, Index = i })
            where actions.Value.HandId == handId && (actions.Value.ActionTypeId >= (byte)PokerAction.DealFlop && actions.Value.ActionTypeId <= (byte)PokerAction.DealRiver)
            orderby actions.Index descending
            select actions.Index;

        var bettingActionsSinceDealerAction =
                from actions in handActions.Select((o, i) => new { Value = o, Index = i })
                where actions.Value.ActionTypeId == (byte)PokerAction.Check ||
                    actions.Value.ActionTypeId == (byte)PokerAction.Call ||
                    actions.Value.ActionTypeId == (byte)PokerAction.Raise ||
                    actions.Value.ActionTypeId == (byte)PokerAction.LittleBlind ||
                    actions.Value.ActionTypeId == (byte)PokerAction.BigBlind
                orderby actions.Index descending
                select actions.Value;


        if (lastDealerActionIds.Count() != 0)
        {
            var lastDealerActionId = lastDealerActionIds.First();

            bettingActionsSinceDealerAction =
                from actions in handActions
                where actions.ActionTime > lastDealerActionId.ActionTime &&
                (actions.ActionTypeId == (byte)PokerAction.Check ||
                    actions.ActionTypeId == (byte)PokerAction.Call ||
                    actions.ActionTypeId == (byte)PokerAction.Raise ||
                    actions.ActionTypeId == (byte)PokerAction.LittleBlind ||
                    actions.ActionTypeId == (byte)PokerAction.BigBlind)
                orderby actions.ActionTime descending
                select actions;
        }


        var playerBetsSinceLastDealerAction =
           from ba in bettingActionsSinceDealerAction
           where ba.PlayerId == playerId
           select ba.ActionValue;

        decimal totalPlayerBet = 0;

        foreach (var a in playerBetsSinceLastDealerAction)
            totalPlayerBet += a;
        */

        #endregion

        decimal totalPlayerBet = getTotalPlayerMoneyInPot(playerId);

        if (totalPlayerBet < actionValue)
        {
          var result = new CacheError(CacheError.ErrorType.AmountInvalid, currentPokerTable.TableId, handId, playerId, actionValue);
          endWrite();
          return result;
        }

        playerOfInterest.First().Stack += actionValue;
        handOfInterest.First().PotValue -= actionValue;
        handActions.Add(new handAction(handId, playerId, (byte)action, actionValue, this));
        endWrite();
        return CacheError.noError;
      }

      #endregion

      #region Dead blind

      if (action == PokerAction.DeadBlind)
      {
        if (playerOfInterest.First().Stack < actionValue)
        {
          CacheError result = new CacheError(CacheError.ErrorType.AmountInvalid, currentPokerTable.TableId, handId, playerId, actionValue);
          endWrite();
          return result;
        }

        playerOfInterest.First().Stack -= actionValue;
        handOfInterest.First().PotValue += actionValue;
        handActions.Add(new handAction(handId, playerId, (byte)action, actionValue, this));
        endWrite();
        return CacheError.noError;
      }

      #endregion

      #region Sitout

      if (action == PokerAction.SitOut)
      {
        if (playerOfInterest.First().IsDead)
        {
          endWrite();
          return CacheError.noError;
        }

        playerOfInterest.First().IsDead = true;
        handActions.Add(new handAction(handId, playerId, (byte)action, playerOfInterest.First().Position, this));
        endWrite();
        return CacheError.noError;
      }

      #endregion

      #region Rejoin

      if (action == PokerAction.SitIn)
      {
        if (!playerOfInterest.First().IsDead)
        {
          CacheError result = new CacheError(CacheError.ErrorType.ActionError, currentPokerTable.TableId, handId, playerId, action);
          endWrite();
          return result;
        }

        playerOfInterest.First().IsDead = false;
        handActions.Add(new handAction(handId, playerId, (byte)action, playerOfInterest.First().Position, this));
        endWrite();
        return CacheError.noError;
      }

      #endregion

      #region Sitdown

      if (action == PokerAction.JoinTable)
      {
        var playerAtTable =
            from tp in tablePlayers
            where tp.PlayerId == playerId && tp.TableId == currentPokerTable.TableId
            select tp;

        if (playerAtTable.Count() != 1)
        {
          CacheError result = new CacheError(CacheError.ErrorType.IdNumberInvalid, currentPokerTable.TableId, handId, playerId, playerId);
          endWrite();
          return result;
        }

        handActions.Add(new handAction(handId, playerId, (byte)action, actionValue, this));
        endWrite();
        return CacheError.noError;
      }

      #endregion

      #region Standup

      if (action == PokerAction.LeaveTable)
      {
        var playerAtTable =
            from tp in tablePlayers
            where tp.PlayerId == playerId && tp.TableId == currentPokerTable.TableId
            select tp;

        if (playerAtTable.Count() != 1)
        {
          CacheError result = new CacheError(CacheError.ErrorType.IdNumberInvalid, currentPokerTable.TableId, handId, playerId, playerId);
          endWrite();
          return result;
        }

        handActions.Add(new handAction(handId, playerId, (byte)action, playerOfInterest.First().Position, this));
        endWrite();
        return CacheError.noError;
      }

      #endregion

      #region StackErrorAdjustment

      if (action == PokerAction.StackErrorAdjustment)
      {
        if (actionValue < 0)
        {
          CacheError result = new CacheError(CacheError.ErrorType.AmountInvalid, currentPokerTable.TableId, handId, playerId, actionValue);
          endWrite();
          return result;
        }

        playerOfInterest.First().Stack = actionValue;
        handActions.Add(new handAction(handId, playerId, (byte)action, actionValue, this));

        endWrite();
        return CacheError.noError;
      }

      #endregion

      #region CatastrophicError

      if (action == PokerAction.CatastrophicError)
      {
        handActions.Add(new handAction(handId, playerId, (byte)action, actionValue, this));
        endWrite();
        return CacheError.noError;
      }

      #endregion

      #region CommsTimeout

      if (action == PokerAction.CommsTimeoutError)
      {
        handActions.Add(new handAction(handId, playerId, (byte)action, actionValue, this));
        endWrite();
        return CacheError.noError;
      }
      #endregion CommsTimeout

      endWrite();
      throw new Exception("Error: Unable to handle provided actionType of " + action);
    }

    /// <summary>
    /// Ends a hand
    /// </summary>
    /// <param name="handId"></param>
    /// <returns></returns>
    public CacheError endCurrentHand()
    {
      startWrite();
      long handId = getCurrentHandId();

      if (currentPokerHand == null)
      {
        CacheError result = new CacheError(CacheError.ErrorType.IdNumberInvalid, currentPokerTable.TableId, handId, null, handId);
        endWrite();
        return result;
      }

      //Find all actions during said hand that involved dishing out winnings
      var potWinActions =
          from actions in handActions
          where (actions.ActionTypeId == (byte)PokerAction.WinPot || actions.ActionTypeId == (byte)PokerAction.TableRake) &&
          actions.HandId == handId
          select actions;

      //Make sure entire pot has been dealt out
      decimal remainingPotTotal = currentPokerHand.PotValue;
      foreach (handAction action in potWinActions)
        remainingPotTotal -= action.ActionValue;

      if (remainingPotTotal != 0)
      {
        //We no longer commit hands to the database unless this end succesfully
        //handActions.Add(new handAction(handId, getPlayerId(currentPokerHand.DealerPosition), (byte)PokerAction.TotalPotError, remainingPotTotal, this));
        //currentPokerHand.endHand();

        CacheError result = new CacheError(CacheError.ErrorType.AmountInvalid, currentPokerTable.TableId, handId, null, remainingPotTotal);
        endWrite();
        return result;
      }

      //End the hand
      currentPokerHand.endHand();

      //currentHandId = -1;

      endWrite();
      return CacheError.noError;
    }

    /// <summary>
    /// Ends a table and calls the necessary cleanup functions.
    /// </summary>
    public void endTable()
    {
      //startWrite();
      //Remove all players from tablePlayers in database
      /*
      var currentTablePlayers =
          from currentPlayers in database.tbl_activeTablePlayers
          where currentPlayers.tableId == currentPokerTable.TableId
          select currentPlayers;

      database.tbl_activeTablePlayers.DeleteAllOnSubmit(currentTablePlayers);
      */
      //Record endTime
      //currentPokerTable.endTable();
      //endWrite();

      //Ending table has become redundant.
      throw new NotImplementedException();
    }

    /// <summary>
    /// Records a new hand error in tbl_handErrors for a given hand
    /// </summary>
    /// <param name="handId"></param>
    /// <param name="error"></param>
    public void fatalHandError(CacheError error)
    {
      //startWrite();

      //long handId = getCurrentHandId();

      ////first find out if hand in question is open
      //var handInQuestion =
      //    from hands in pokerHands
      //    where handId == hands.HandId && hands.StartTime == hands.EndTime
      //    select hands;

      ////End hand if open
      //if (handInQuestion.Count() != 0)
      //    foreach (pokerHand hand in handInQuestion)
      //        hand.endHand();

      //handActions.Add(new handAction(handId, 0, (byte)PokerAction.CatastrophicError, 0, this));

      //endWrite();

      //need to add error to errors table
      throw new NotImplementedException();
    }

  }
}
