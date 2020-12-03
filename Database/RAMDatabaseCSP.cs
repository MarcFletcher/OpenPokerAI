using System;
using System.Collections.Generic;
using System.Linq;
using PokerBot.Definitions;

#if databaseLogging
using System.Diagnostics;
#endif

namespace PokerBot.Database
{
  partial class RAMDatabase
  {
    /// <summary>
    /// Returns the number of hands played by the provided playerId.
    /// </summary>
    /// <param name="playerId"></param>
    /// <returns></returns>
    public int csp_NumHandsPlayed(long playerId, bool countUniqueHandsOnly, databaseSnapshotUsage snapshotUsage = databaseSnapshotUsage.CreateNew)
    {
      DatabaseSnapshot snapshot = GetDatabaseSnapshot(snapshotUsage);

      //We can check for a cached value here
      //The bool on the inner dictionary represents countUniqueHandsOnly
      Dictionary<long, object> cacheValues = snapshot.GetSnapshotCacheValue("csp_NumHandsPlayed") as Dictionary<long, object>;

      if (cacheValues != null)
      {
        if (cacheValues.ContainsKey(playerId))
        {
          Dictionary<bool, object> innerPlayerDictionary = cacheValues[playerId] as Dictionary<bool, object>;
          if (innerPlayerDictionary.ContainsKey(countUniqueHandsOnly))
            return (int)innerPlayerDictionary[countUniqueHandsOnly];
        }
      }

      //There are cases where a single player, possibly as a genetic opponent may have played the 'same' hand
      //sometimes it is desireably to only count the unique hands not the repetitions
      int handsWithActions = 0;
      if (countUniqueHandsOnly)
        //We don't need to save the value here because it gets cahced within RAMDatabase.HandIdsInWhichPlayerActsUnique
        handsWithActions = RAMDatabase.HandIdsInWhichPlayerActsUnique(playerId, snapshot).Length;
      else
        handsWithActions = csp_HandIdsPlayedByPlayerId(playerId, snapshotUsage).Length;

      snapshot.AddSnapshotCacheValue("csp_NumHandsPlayed", new Dictionary<long, object>() { { playerId, new Dictionary<bool, object>() { { countUniqueHandsOnly, handsWithActions } } } });
      return handsWithActions;
    }

    /// <summary>
    /// Returns the player agression metrics for the RAM database
    /// </summary>
    /// <param name="playerId"></param>
    /// <param name="handsRange">The maximum number of hands to count.</param>
    /// <param name="handsStartIndex">-1 means from most recent backwards. 1 means the first hand</param>
    /// <param name="startWeight">The weight at which the scaling starts. 1 means no scaling.</param>
    /// <param name="numHandsCounted">Returns the number of hands counted which may be less than handsRange</param>
    /// <param name="RFreq_PreFlop"></param>
    /// <param name="RFreq_PostFlop"></param>
    /// <param name="CFreq_PreFlop"></param>
    /// <param name="CFreq_PostFlop"></param>
    /// <param name="PreFlopPlayFreq"></param>
    /// Written and extensive testing completed on 08/02/2011
    public void csp_PlayerAgressionMetrics(long playerId, long handsRange, long handsStartIndex, decimal startWeight, ref int numHandsCounted,
        ref decimal RFreq_PreFlop, ref decimal RFreq_PostFlop,
        ref decimal CFreq_PreFlop, ref decimal CFreq_PostFlop,
        ref decimal CheckFreq_PreFlop, ref decimal CheckFreq_PostFlop,
        ref decimal PreFlopPlayFreq, ref decimal PostFlopPlayFreq,
        databaseSnapshotUsage snapshotUsage = databaseSnapshotUsage.CreateNew)
    {
      //We override the start index for the RAM version as we need to make sure we don't accidently use hands from some subsequent slice
      //handsStartIndex = 1;

      #region Setup
      DatabaseSnapshot snapshot = GetDatabaseSnapshot(snapshotUsage);
      RAMDatabaseDataLists snapshotData = snapshot.EnterSnapshot();

      //Get the hands in which this player acts
      //So that we don't count hands for genetic opponeent players multiple times this method returns only unique hands
      long[] handIds = csp_HandIdsPlayedByPlayerId(playerId, snapshotUsage);

      if (handsRange == 0)
        throw new Exception("HandsRange must be greater than 0.");

      //From here we can more closely follow the original stored procedure
      if (handIds.Length == 0)
      {
        numHandsCounted = 0;
        RFreq_PreFlop = 0;
        RFreq_PostFlop = 0;
        CFreq_PreFlop = 0;
        CFreq_PostFlop = 0;
        PreFlopPlayFreq = 0;

        return;
      }

      if (handsStartIndex > handIds.Length)
        throw new Exception("handStartIndex was larger than number of available hands.");

      if (handsStartIndex == -1)
      {
        //We need to be carefull about going into negative starting value here
        if (handIds.Length < handsRange)
          handsStartIndex = 0;
        else
          handsStartIndex = handIds.Length - handsRange;
      }
      else
        //As an input 1 means the first hand so we need to convert to 0 index within this method
        handsStartIndex -= 1;

      //Determine the per hand weight increment
      //Again carefull with the different ranges
      decimal weightIncrement;

      if (handIds.Length < handsRange)
        weightIncrement = (1.0m - startWeight) / (decimal)(handIds.Length - handsStartIndex);
      else
        weightIncrement = (1.0m - startWeight) / (handsRange);

      decimal countPreFlopRaise = 0, countPostFlopRaise = 0, countPostTurnRaise = 0, countPostRiverRaise = 0;
      decimal countPreFlopCall = 0, countPostFlopCall = 0, countPostTurnCall = 0, countPostRiverCall = 0;
      decimal countPreFlopCheck = 0, countPostFlopCheck = 0, countPostTurnCheck = 0, countPostRiverCheck = 0;
      decimal countPreFlopFold = 0, countPostFlopFold = 0, countPostTurnFold = 0, countPostRiverFold = 0;
      decimal countPreFlopPlay = 0, countPostFlopPlay = 0, countHandsWithFlop = 0, countHandsScaled = 0;
      numHandsCounted = 0;
      #endregion

      //Now we loop over the hands
      for (long i = handsStartIndex; i < handIds.Length; i++)
      {
        if (numHandsCounted >= handsRange)
          break;

        decimal currentWeightAddition = (startWeight + (weightIncrement * numHandsCounted));

        #region Flop/Turn/River Indexes
        var currentHandActions = snapshotData.handActionsSnapshot[handIds[i]];

        //Need access to the hand
        //databaseCache.pokerHand pokerHand;
        //foreach (KeyValuePair<long, SortedDictionary<long, databaseCache.pokerHand>> hands in snapshotData.pokerHandsSnapshot)
        //{
        //    if (hands.Value.ContainsKey(handIds[i]))
        //    {
        //        pokerHand = hands.Value[handIds[i]];
        //        break;
        //    }
        //}

        byte startIndex = currentHandActions[0].LocalSeqIndex;
        byte endIndex = currentHandActions[currentHandActions.Count - 1].LocalSeqIndex;

        byte flopIndex = endIndex, turnIndex = endIndex, riverIndex = endIndex;

        //Get the indexes of the necessary hand stages so that we can break up the raises
        //Loop through the handActions to pick out the necessary stage indexes in one go
        for (int k = 0; k < currentHandActions.Count; k++)
        {
          if (currentHandActions[k].ActionTypeId == (byte)PokerAction.DealFlop)
            flopIndex = currentHandActions[k].LocalSeqIndex;
          else if (currentHandActions[k].ActionTypeId == (byte)PokerAction.DealTurn)
            turnIndex = currentHandActions[k].LocalSeqIndex;
          else if (currentHandActions[k].ActionTypeId == (byte)PokerAction.DealRiver)
          {
            riverIndex = currentHandActions[k].LocalSeqIndex;
            break;
          }
        }

        //Get the indexes of the necessary hand stages so that we can break up the raises
        //flopIndex = (from current in currentHandActions where current.ActionTypeId == (byte)PokerAction.DealFlop select current.LocalSeqIndex).Take(1).ToArray();
        //turnIndex = (from current in currentHandActions where current.ActionTypeId == (byte)PokerAction.DealTurn select current.LocalSeqIndex).Take(1).ToArray();
        //riverIndex = (from current in currentHandActions where current.ActionTypeId == (byte)PokerAction.DealRiver select current.LocalSeqIndex).Take(1).ToArray();

        //if (flopIndex.Length == 0)
        //    flopIndex = new byte[] { endIndex };
        //if (turnIndex.Length == 0)
        //    turnIndex = new byte[] { endIndex };
        //if (riverIndex.Length == 0)
        //    riverIndex = new byte[] { endIndex };
        #endregion

        #region HandPlays
        ///////////////////////////////////////////////////////////////////
        //First we can look at plays, i.e. did the player partake of the hand
        ///////////////////////////////////////////////////////////////////

        //Look at preflop plays by the player of interest
        //If the player raised or called pre flop
        if ((from current in currentHandActions
             where (current.PlayerId == playerId) && (current.ActionTypeId == (byte)PokerAction.Call || current.ActionTypeId == (byte)PokerAction.Raise) && (current.LocalSeqIndex >= startIndex && current.LocalSeqIndex < flopIndex)
             select current.LocalSeqIndex).Count() > 0)
        {
          countPreFlopPlay += currentWeightAddition;
        }

        //Now the postflop stuff
        //If the player did not fold preflop and their was a flop
        if (flopIndex < endIndex && ((from current in currentHandActions
                                      where (current.PlayerId == playerId) && (current.ActionTypeId == (byte)PokerAction.Fold) && (current.LocalSeqIndex >= startIndex && current.LocalSeqIndex < flopIndex)
                                      select current.LocalSeqIndex).Count() == 0))
        {
          countHandsWithFlop += currentWeightAddition;
        }

        //Now check if the player played postflop
        if ((from current in currentHandActions
             where (current.PlayerId == playerId) && (current.ActionTypeId == (byte)PokerAction.Call || current.ActionTypeId == (byte)PokerAction.Raise) && (current.LocalSeqIndex > flopIndex && current.LocalSeqIndex < endIndex)
             select current.LocalSeqIndex).Count() > 0)
        {
          countPostFlopPlay += currentWeightAddition;
        }
        #endregion

        #region PreFlop
        ///////////////////////////////////////////////////////////////////
        //Specific preflop actions
        ///////////////////////////////////////////////////////////////////
        countPreFlopRaise += (from current in currentHandActions where (current.PlayerId == playerId) && (current.ActionTypeId == (byte)PokerAction.Raise) && (current.LocalSeqIndex >= startIndex && current.LocalSeqIndex < flopIndex) select current.LocalSeqIndex).Count() * currentWeightAddition;
        countPreFlopCall += (from current in currentHandActions where (current.PlayerId == playerId) && (current.ActionTypeId == (byte)PokerAction.Call) && (current.LocalSeqIndex >= startIndex && current.LocalSeqIndex < flopIndex) select current.LocalSeqIndex).Count() * currentWeightAddition;
        countPreFlopCheck += (from current in currentHandActions where (current.PlayerId == playerId) && (current.ActionTypeId == (byte)PokerAction.Check) && (current.LocalSeqIndex >= startIndex && current.LocalSeqIndex < flopIndex) select current.LocalSeqIndex).Count() * currentWeightAddition;
        countPreFlopFold += (from current in currentHandActions where (current.PlayerId == playerId) && (current.ActionTypeId == (byte)PokerAction.Fold) && (current.LocalSeqIndex >= startIndex && current.LocalSeqIndex < flopIndex) select current.LocalSeqIndex).Count() * currentWeightAddition;
        #endregion

        #region Flop
        ///////////////////////////////////////////////////////////////////
        //Specific flop actions
        ///////////////////////////////////////////////////////////////////
        if (flopIndex < endIndex)
        {
          countPostFlopRaise += (from current in currentHandActions where (current.PlayerId == playerId) && (current.ActionTypeId == (byte)PokerAction.Raise) && (current.LocalSeqIndex > flopIndex && current.LocalSeqIndex < turnIndex) select current.LocalSeqIndex).Count() * currentWeightAddition;
          countPostFlopCall += (from current in currentHandActions where (current.PlayerId == playerId) && (current.ActionTypeId == (byte)PokerAction.Call) && (current.LocalSeqIndex > flopIndex && current.LocalSeqIndex < turnIndex) select current.LocalSeqIndex).Count() * currentWeightAddition;
          countPostFlopCheck += (from current in currentHandActions where (current.PlayerId == playerId) && (current.ActionTypeId == (byte)PokerAction.Check) && (current.LocalSeqIndex > flopIndex && current.LocalSeqIndex < turnIndex) select current.LocalSeqIndex).Count() * currentWeightAddition;
          countPostFlopFold += (from current in currentHandActions where (current.PlayerId == playerId) && (current.ActionTypeId == (byte)PokerAction.Fold) && (current.LocalSeqIndex > flopIndex && current.LocalSeqIndex < turnIndex) select current.LocalSeqIndex).Count() * currentWeightAddition;
        }
        #endregion

        #region Turn
        ///////////////////////////////////////////////////////////////////
        //Specific turn actions
        ///////////////////////////////////////////////////////////////////
        if (turnIndex < endIndex)
        {
          countPostTurnRaise += (from current in currentHandActions where (current.PlayerId == playerId) && (current.ActionTypeId == (byte)PokerAction.Raise) && (current.LocalSeqIndex > turnIndex && current.LocalSeqIndex < riverIndex) select current.LocalSeqIndex).Count() * currentWeightAddition;
          countPostTurnCall += (from current in currentHandActions where (current.PlayerId == playerId) && (current.ActionTypeId == (byte)PokerAction.Call) && (current.LocalSeqIndex > turnIndex && current.LocalSeqIndex < riverIndex) select current.LocalSeqIndex).Count() * currentWeightAddition;
          countPostTurnCheck += (from current in currentHandActions where (current.PlayerId == playerId) && (current.ActionTypeId == (byte)PokerAction.Check) && (current.LocalSeqIndex > turnIndex && current.LocalSeqIndex < riverIndex) select current.LocalSeqIndex).Count() * currentWeightAddition;
          countPostTurnFold += (from current in currentHandActions where (current.PlayerId == playerId) && (current.ActionTypeId == (byte)PokerAction.Fold) && (current.LocalSeqIndex > turnIndex && current.LocalSeqIndex < riverIndex) select current.LocalSeqIndex).Count() * currentWeightAddition;
        }
        #endregion

        #region River
        ///////////////////////////////////////////////////////////////////
        //Specific river actions
        ///////////////////////////////////////////////////////////////////
        if (riverIndex < endIndex)
        {
          countPostRiverRaise += (from current in currentHandActions where (current.PlayerId == playerId) && (current.ActionTypeId == (byte)PokerAction.Raise) && (current.LocalSeqIndex > riverIndex && current.LocalSeqIndex < endIndex) select current.LocalSeqIndex).Count() * currentWeightAddition;
          countPostRiverCall += (from current in currentHandActions where (current.PlayerId == playerId) && (current.ActionTypeId == (byte)PokerAction.Call) && (current.LocalSeqIndex > riverIndex && current.LocalSeqIndex < endIndex) select current.LocalSeqIndex).Count() * currentWeightAddition;
          countPostRiverCheck += (from current in currentHandActions where (current.PlayerId == playerId) && (current.ActionTypeId == (byte)PokerAction.Check) && (current.LocalSeqIndex > riverIndex && current.LocalSeqIndex < endIndex) select current.LocalSeqIndex).Count() * currentWeightAddition;
          countPostRiverFold += (from current in currentHandActions where (current.PlayerId == playerId) && (current.ActionTypeId == (byte)PokerAction.Fold) && (current.LocalSeqIndex > riverIndex && current.LocalSeqIndex < endIndex) select current.LocalSeqIndex).Count() * currentWeightAddition;
        }
        #endregion

        #region FinishUp
        countHandsScaled += (startWeight + (weightIncrement * numHandsCounted));
        numHandsCounted++;
        #endregion
      }

      #region Calculate Stats
      //Once all hands have been counted we can work out the stats
      if ((countPreFlopRaise + countPostFlopRaise + countPostTurnRaise + countPostRiverRaise) +
      (countPreFlopCall + countPostFlopCall + countPostTurnCall + countPostRiverCall) +
      (countPreFlopCheck + countPostFlopCheck + countPostTurnCheck + countPostRiverCheck) +
      (countPreFlopFold + countPostFlopFold + countPostTurnFold + countPostRiverFold) == 0)
      {
        //THere have been no betting actions so just set stuff to 0
        RFreq_PreFlop = 0;
        RFreq_PostFlop = 0;
        CFreq_PreFlop = 0;
        CFreq_PostFlop = 0;

        CheckFreq_PreFlop = 0;
        CheckFreq_PostFlop = 0;

        PreFlopPlayFreq = 0;
        PostFlopPlayFreq = 0;
      }
      else
      {
        RFreq_PreFlop = countPreFlopRaise / (countPreFlopRaise + countPreFlopCall + countPreFlopCheck + countPreFlopFold);
        CFreq_PreFlop = countPreFlopCall / (countPreFlopRaise + countPreFlopCall + countPreFlopCheck + countPreFlopFold);
        CheckFreq_PreFlop = countPreFlopCheck / (countPreFlopRaise + countPreFlopCall + countPreFlopCheck + countPreFlopFold);

        PreFlopPlayFreq = countPreFlopPlay / countHandsScaled;

        if (countHandsWithFlop > 0)
          PostFlopPlayFreq = countPostFlopPlay / countHandsWithFlop;
        else
          PostFlopPlayFreq = 0;

        //It's possible there are no postlfop actions for this player which will result in a divide by 0 error
        if ((countPostFlopRaise + countPostTurnRaise + countPostRiverRaise) +
        (countPostFlopCall + countPostTurnCall + countPostRiverCall) +
        (countPostFlopCheck + countPostTurnCheck + countPostRiverCheck) +
        (countPostFlopFold + countPostTurnFold + countPostRiverFold) > 0)
        {
          RFreq_PostFlop = (countPostFlopRaise + countPostTurnRaise + countPostRiverRaise) / ((countPostFlopRaise + countPostTurnRaise + countPostRiverRaise) + (countPostFlopCall + countPostTurnCall + countPostRiverCall) + (countPostFlopCheck + countPostTurnCheck + countPostRiverCheck) + (countPostFlopFold + countPostTurnFold + countPostRiverFold));
          CFreq_PostFlop = (countPostFlopCall + countPostTurnCall + countPostRiverCall) / ((countPostFlopRaise + countPostTurnRaise + countPostRiverRaise) + (countPostFlopCall + countPostTurnCall + countPostRiverCall) + (countPostFlopCheck + countPostTurnCheck + countPostRiverCheck) + (countPostFlopFold + countPostTurnFold + countPostRiverFold));
          CheckFreq_PostFlop = (countPostFlopCheck + countPostTurnCheck + countPostRiverCheck) / ((countPostFlopRaise + countPostTurnRaise + countPostRiverRaise) + (countPostFlopCall + countPostTurnCall + countPostRiverCall) + (countPostFlopCheck + countPostTurnCheck + countPostRiverCheck) + (countPostFlopFold + countPostTurnFold + countPostRiverFold));
        }
        else
        {
          RFreq_PostFlop = 0;
          CFreq_PostFlop = 0;
          CheckFreq_PostFlop = 0;
        }
      }
      #endregion
    }

    /// <summary>
    /// NOTE: THIS IS NOT PERFECT
    /// This method is mainly used by the RAM agression provider. A problem with this is that if an opponent player has played the same hand 8 times
    /// it will completely screw the metrics.
    /// In order to fix this we need to find an intelligent way of ignoring duplicate hands. This method will therefore only return handIds
    /// of unique hands.
    /// This list will build a list of all duplicate hands and for each duplicate return a randomly select handId
    /// </summary>
    /// <param name="playerId"></param>
    /// <param name="localHandActions"></param>
    /// <returns></returns>
    public static long[] HandIdsInWhichPlayerActsUnique(long playerId, DatabaseSnapshot snapshot)
    {
      //Before we enter the snapshot we can check for a cached value for this method and playerId
      Dictionary<long, object> cacheValues = snapshot.GetSnapshotCacheValue("HandIdsInWhichPlayerActsUnique") as Dictionary<long, object>;

      //If we have already calculated the value for this player
      if (cacheValues != null)
      {
        if (cacheValues.ContainsKey(playerId))
          return cacheValues[playerId] as long[];
      }

      //If we have not already calculated it we do so now
      RAMDatabaseDataLists snapshotData = snapshot.EnterSnapshot();

      Dictionary<long, List<long>> uniqueHandsDict = new Dictionary<long, List<long>>();

      //System.Security.Cryptography.MD5CryptoServiceProvider md5 = new System.Security.Cryptography.MD5CryptoServiceProvider();

      //Keeps track of the current and last used tableId
      long tableId = 0;

      //Key is handId
      foreach (KeyValuePair<long, List<databaseCache.handAction>> hand in snapshotData.handActionsSnapshot)
      {
        //For each hand look for an appropriate player action
        for (int i = 0; i < hand.Value.Count; i++)
        {
          if (hand.Value[i].PlayerId == playerId)
          {
            //Break the if down as a performance enhanchment
            if (hand.Value[i].ActionTypeId == (byte)PokerAction.LittleBlind || hand.Value[i].ActionTypeId == (byte)PokerAction.BigBlind || hand.Value[i].ActionTypeId == (byte)PokerAction.Fold || hand.Value[i].ActionTypeId == (byte)PokerAction.Check || hand.Value[i].ActionTypeId == (byte)PokerAction.Call || hand.Value[i].ActionTypeId == (byte)PokerAction.Raise)
            {
              //We need to work out some sort of hand hash here which allows us to remove duplicate hands
              //We can use handCount, current dealer position, table cards and hole cards
              //Get the correct tableId for this hand
              tableId = RAMDatabase.LocateHandTableId(hand.Key, tableId, snapshotData.pokerHandsSnapshot);
              databaseCache.pokerHand currentHand = snapshotData.pokerHandsSnapshot[tableId][hand.Key];

              var hashFunc = new Func<long, long>((long key) =>
              {
                key = (~key) + (key << 21); // key = (key << 21) - key - 1;
                key = key ^ (key >> 24);
                key = (key + (key << 3)) + (key << 8); // key * 265
                key = key ^ (key >> 14);
                key = (key + (key << 2)) + (key << 4); // key * 21
                key = key ^ (key >> 28);
                key = key + (key << 31);
                return key;
              });

              //Hash the table state
              long handHash = hashFunc(hashFunc(currentHand.CacheHandIndex) ^
                  hashFunc(currentHand.DealerPosition + 1) ^
                  hashFunc((1L << currentHand.TableCard1) +
                  (1L << currentHand.TableCard2) + (1L << currentHand.TableCard3) + (1L << currentHand.TableCard4) +
                  (1L << currentHand.TableCard5)));

              //Add the holdCards data
              foreach (var holeCardSet in snapshotData.holeCardsSnapshot[hand.Key])
                handHash = handHash ^ hashFunc((1L << holeCardSet.Value.HoleCard1) + (1L << holeCardSet.Value.HoleCard2));

              //We record every hand we come across
              if (!uniqueHandsDict.ContainsKey(handHash))
                uniqueHandsDict.Add(handHash, new List<long>() { hand.Key });
              else
                uniqueHandsDict[handHash].Add(hand.Key);

              //Once we know the  player of interest acted we can move on to the next hand
              break;
            }
          }
        }
      }

      //We can now choose a random hand from each hash entry
      Random r = new Random();
      int arrayIndex = 0;
      long[] returnArray = new long[uniqueHandsDict.Keys.Count];
      foreach (var hashEntry in uniqueHandsDict)
      {
        if (hashEntry.Value.Count > 1)
          returnArray[arrayIndex] = hashEntry.Value[(int)(r.NextDouble() * hashEntry.Value.Count)];
        else
          returnArray[arrayIndex] = hashEntry.Value[0];

        arrayIndex++;
      }

      //Try to add is the snapshot cache
      snapshot.AddSnapshotCacheValue("HandIdsInWhichPlayerActsUnique", new Dictionary<long, object>() { { playerId, returnArray } });

      return returnArray;
    }

    /// <summary>
    /// Locates the tableId for the provided handId given that the dictionaries are sorted by tableId first
    /// If the handId is not located in suggestedTableId all tableIds are searched
    /// </summary>
    /// <param name="handId"></param>
    /// <param name="suggestedTableId">Looks for hand in suggestedTableId first.</param>
    /// <returns></returns>
    private static long LocateHandTableId(long handId, long suggestedTableId, SortedDictionary<long, SortedDictionary<long, databaseCache.pokerHand>> localPokerHands)
    {
      //Only called by HandIdsInWhichPlayerActsUnique so no need for caching here

      if (localPokerHands.ContainsKey(suggestedTableId))
      {
        if (localPokerHands[suggestedTableId].ContainsKey(handId))
          return suggestedTableId;
        else
        {
          foreach (var table in localPokerHands)
          {
            if (table.Value.ContainsKey(handId))
              return table.Key;
          }
        }
      }
      else
      {
        foreach (var table in localPokerHands)
        {
          if (table.Value.ContainsKey(handId))
            return table.Key;
        }
      }

      throw new Exception("handId not located in any available tables.");
    }

    /// <summary>
    /// Returns the number of hands played within the provided pokerClientId
    /// </summary>
    /// <param name="pokerClientId"></param>
    /// <returns></returns>
    public int csp_NumHandsPlayed(short pokerClientId)
    {
      //SET @handsPlayed = (SELECT COUNT(DISTINCT hands.id) 
      //FROM tbl_hands as hands JOIN tbl_tables as tbls ON hands.tableId=tbls.id 
      //WHERE tbls.pokerClientId = @pokerClientId)

      //Get the tableIds
      //Dictionary<long, databaseCache.pokerTable> pokerTablesLocal = ShallowCloneTables();
      //Dictionary<long, Dictionary<long, databaseCache.pokerHand>> pokerHandsLocal = ShallowCloneHands();
      long[] pokerTableIds;

      lock (tableLocker)
      {
        pokerTableIds = (from current in pokerTables
                         where current.Value.PokerClientId == pokerClientId
                         select current.Value.TableId).ToArray();
      }

      int numHandsPlayed = 0;
      //Go through each table and sum the hands
      lock (handLocker)
      {
        foreach (long tableId in pokerTableIds)
          numHandsPlayed += pokerHands[tableId].Count;
      }

      return numHandsPlayed;
    }

    /// <summary>
    /// Returns the amounts a player has gambled, i.e. during playing poker hands.
    /// To work out total amount won or lost use this method in conjunction with 'amountsWonByPlayerId'.
    /// StartingIndex can be used to do incremental counts, default is 1. Hands counted start at the startingIndex. MaxHands is the number of hands counted. i.e. start at handIndex 2 and count two hands (hands counted are 2 and 3).
    /// Set maxHands = - 1 for all available hands.
    /// </summary>
    /// <param name="playerId"></param>
    /// <returns></returns>
    public decimal csp_amountsGambledByPlayerId(long playerId, int startingIndex, int maxHands)
    {
      throw new NotImplementedException();
    }

    /// <summary>
    /// Returns the average additional raise amount of the provided player
    /// StartingIndex can be used to do incremental counts, default is 1. Hands counted start at the startingIndex. 
    /// MaxHands is the number of hands counted. i.e. start at handIndex 2 and count two hands (hands counted are 2 and 3).
    /// Set maxHands = -1 for all available hands.
    /// </summary>
    /// <param name="playerId"></param>
    /// <param name="startingIndex"></param>
    /// <param name="maxHands"></param>
    /// <returns></returns>
    public databaseQueries.PlayerAverageAdditionalRaiseAmountResultScaled[] csp_playerAdditionalRaiseAmounts(long playerId, databaseSnapshotUsage snapshotUsage = databaseSnapshotUsage.CreateNew)
    {
      DatabaseSnapshot snapshot = GetDatabaseSnapshot(snapshotUsage);
      RAMDatabaseDataLists snapshotData = snapshot.EnterSnapshot();

      List<databaseQueries.PlayerAverageAdditionalRaiseAmountResultScaled> resultList = new List<databaseQueries.PlayerAverageAdditionalRaiseAmountResultScaled>();

      //We go through all hands inthe ram database
      foreach (KeyValuePair<long, List<databaseCache.handAction>> hand in snapshotData.handActionsSnapshot)
      {
        //For each hand go through the actions
        for (int i = 0; i < hand.Value.Count; i++)
        {
          //If this hand refers to the expected playerId
          if (hand.Value[i].PlayerId == playerId)
          {
            //We are interested in this hand if the player has raised
            if (hand.Value[i].ActionTypeId == (byte)PokerAction.Raise)
              csp_playerHandAdditionalRaiseAmounts(hand.Value, playerId, resultList);
          }
        }
      }

      //Return the average amount won / lost per hand
      return resultList.ToArray();
    }

    /// <summary>
    /// Calculates the additional raise amounts for a player in a given hand and returns each amount as a list entry.
    /// If the player does not raise returns -1
    /// </summary>
    /// <param name="handActions"></param>
    /// <param name="playerId"></param>
    /// <returns></returns>
    private void csp_playerHandAdditionalRaiseAmounts(List<databaseCache.handAction> handActions, long playerId, List<databaseQueries.PlayerAverageAdditionalRaiseAmountResultScaled> resultList)
    {
      byte startIndex = handActions[0].LocalSeqIndex;
      byte endIndex = handActions[handActions.Count - 1].LocalSeqIndex;
      var flopIndex = (from current in handActions where current.ActionTypeId == (byte)PokerAction.DealFlop select current.LocalSeqIndex).Take(1).ToArray();
      var turnIndex = (from current in handActions where current.ActionTypeId == (byte)PokerAction.DealTurn select current.LocalSeqIndex).Take(1).ToArray();
      var riverIndex = (from current in handActions where current.ActionTypeId == (byte)PokerAction.DealRiver select current.LocalSeqIndex).Take(1).ToArray();

      if (flopIndex.Length == 0)
        flopIndex = new byte[] { endIndex };
      if (turnIndex.Length == 0)
        turnIndex = new byte[] { endIndex };
      if (riverIndex.Length == 0)
        riverIndex = new byte[] { endIndex };

      short[] bettingActionTypes = new short[] { (byte)PokerAction.LittleBlind, (byte)PokerAction.BigBlind, (byte)PokerAction.Call, (byte)PokerAction.Raise };

      decimal bigBlindAmount = (from current in handActions where current.ActionTypeId == (byte)PokerAction.BigBlind select current.ActionValue).First();
      decimal lastAdditionalRaiseAmount = bigBlindAmount;
      decimal lastAdditionalRaiseToAmount = bigBlindAmount;

      //Keeps a track of what money each player has in a pot
      decimal previousRoundPotAmount = 0;
      Dictionary<long, decimal> playerMoneyInPot = new Dictionary<long, decimal>();

      /////////////////////////////////////////////////////////////
      /////////////////////////PRE-FLOP////////////////////////////
      /////////////////////////////////////////////////////////////

      //Did the player raise pre flop?
      var playerRaisesPreFlopIndex = (from current in handActions
                                      where current.ActionTypeId == (byte)PokerAction.Raise
                                      where current.PlayerId == playerId
                                      where current.LocalSeqIndex >= startIndex && current.LocalSeqIndex <= flopIndex[0]
                                      orderby current.LocalSeqIndex ascending
                                      select current.LocalSeqIndex).ToArray();

      //In order to know the additional raise amount we need to know the pot as well as the minimum and maximum raise at each point
      if (playerRaisesPreFlopIndex.Length > 0)
      {
        #region Calculate Player Additional Raise Amounts
        for (int i = 0; i < playerRaisesPreFlopIndex.Length; i++)
        {
          //Work out the pot and lastAdditionalRaiseAmount upto the players raise
          for (int j = (i == 0 ? 0 : playerRaisesPreFlopIndex[i - 1]); j < playerRaisesPreFlopIndex[i]; j++)
          {
            long actionPlayerId = handActions[j].PlayerId;
            decimal actionAmount = handActions[j].ActionValue;

            //Ensure the raising player has an entry in the dictionary
            if (!playerMoneyInPot.ContainsKey(actionPlayerId))
              playerMoneyInPot.Add(actionPlayerId, 0);

            //Loop through every action and decide what to add to the pot
            switch ((PokerAction)handActions[j].ActionTypeId)
            {
              case PokerAction.LittleBlind:
              {
                playerMoneyInPot[actionPlayerId] += actionAmount;
                break;
              }
              case PokerAction.BigBlind:
              {
                playerMoneyInPot[actionPlayerId] += actionAmount;
                break;
              }
              case PokerAction.Call:
              {
                playerMoneyInPot[actionPlayerId] += actionAmount;
                break;
              }
              case PokerAction.Raise:
              {
                //If someone raises then the addition to the pot depends on what they have done previously this hand
                playerMoneyInPot[actionPlayerId] += actionAmount - playerMoneyInPot[actionPlayerId];
                lastAdditionalRaiseAmount = actionAmount - lastAdditionalRaiseToAmount;
                lastAdditionalRaiseToAmount = actionAmount;
                break;
              }
            }
          }

          decimal additionalAmountRaised = handActions[playerRaisesPreFlopIndex[i]].ActionValue - lastAdditionalRaiseToAmount;

          //We restrict a raise to a minimum of a big blind
          if (bigBlindAmount > additionalAmountRaised)
            additionalAmountRaised = bigBlindAmount;
          //If a player raises all in then their raise will be less than handAdditionalRaiseAmountsPreflop
          if (lastAdditionalRaiseAmount > additionalAmountRaised)
            lastAdditionalRaiseAmount = additionalAmountRaised;

          resultList.Add(new databaseQueries.PlayerAverageAdditionalRaiseAmountResultScaled(handActions[0].HandId, playerRaisesPreFlopIndex[i], 0, RaiseAmountsHelper.ScaleAdditionalRaiseAmount(previousRoundPotAmount + playerMoneyInPot.Values.Sum(), bigBlindAmount, lastAdditionalRaiseAmount, 100 * bigBlindAmount, additionalAmountRaised)));
        }
        #endregion
      }

      //Now finish of calculating the pot based on all remaining actions
      #region Finish calculating possible pot
      //Calculate the pot upto the flop
      for (int j = (playerRaisesPreFlopIndex.Length == 0 ? 0 : playerRaisesPreFlopIndex.Last()); j < flopIndex[0]; j++)
      {
        long actionPlayerId = handActions[j].PlayerId;
        decimal actionAmount = handActions[j].ActionValue;

        if (!playerMoneyInPot.ContainsKey(actionPlayerId))
          playerMoneyInPot.Add(actionPlayerId, 0);

        //Loop through every action and decide what to add to the pot
        switch ((PokerAction)handActions[j].ActionTypeId)
        {
          case PokerAction.LittleBlind:
          {
            playerMoneyInPot[actionPlayerId] += actionAmount;
            break;
          }
          case PokerAction.BigBlind:
          {
            playerMoneyInPot[actionPlayerId] += actionAmount;
            break;
          }
          case PokerAction.Call:
          {
            playerMoneyInPot[actionPlayerId] += actionAmount;
            break;
          }
          case PokerAction.Raise:
          {
            //If someone raises then the addition to the pot depends on what they have done previously this hand
            playerMoneyInPot[actionPlayerId] += actionAmount - playerMoneyInPot[actionPlayerId];
            break;
          }
        }
      }
      #endregion

      /////////////////////////////////////////////////////////////
      /////////////////////////FLOP////////////////////////////////
      /////////////////////////////////////////////////////////////
      if (flopIndex[0] < endIndex)
      {
        previousRoundPotAmount += playerMoneyInPot.Values.Sum();
        playerMoneyInPot = new Dictionary<long, decimal>();
        lastAdditionalRaiseAmount = 0;
        lastAdditionalRaiseToAmount = 0;

        var playerRaisesFlopIndex = (from current in handActions
                                     where current.ActionTypeId == (byte)PokerAction.Raise
                                     where current.PlayerId == playerId
                                     where current.LocalSeqIndex >= flopIndex[0] && current.LocalSeqIndex <= turnIndex[0]
                                     orderby current.LocalSeqIndex ascending
                                     select current.LocalSeqIndex).ToArray();

        if (playerRaisesFlopIndex.Length > 0)
        {
          #region Calculate Player Additional Raise Amounts
          for (int i = 0; i < playerRaisesFlopIndex.Length; i++)
          {
            //Work out the pot and lastAdditionalRaiseAmount upto the players raise
            for (int j = (i == 0 ? flopIndex[0] : playerRaisesFlopIndex[i - 1]); j < playerRaisesFlopIndex[i]; j++)
            {
              long actionPlayerId = handActions[j].PlayerId;
              decimal actionAmount = handActions[j].ActionValue;

              if (!playerMoneyInPot.ContainsKey(actionPlayerId))
                playerMoneyInPot.Add(actionPlayerId, 0);

              //Loop through every action and decide what to add to the pot
              switch ((PokerAction)handActions[j].ActionTypeId)
              {
                case PokerAction.Call:
                {
                  playerMoneyInPot[actionPlayerId] += actionAmount;
                  break;
                }
                case PokerAction.Raise:
                {
                  //If someone raises then the addition to the pot depends on what they have done previously this hand
                  playerMoneyInPot[actionPlayerId] += actionAmount - playerMoneyInPot[actionPlayerId];
                  lastAdditionalRaiseAmount = actionAmount - lastAdditionalRaiseToAmount;
                  lastAdditionalRaiseToAmount = actionAmount;
                  break;
                }
              }
            }

            decimal additionalAmountRaised = handActions[playerRaisesFlopIndex[i]].ActionValue - lastAdditionalRaiseToAmount;

            //We restrict a raise to a minimum of a big blind
            if (bigBlindAmount > additionalAmountRaised)
              additionalAmountRaised = bigBlindAmount;
            //If a player raises all in then their raise will be less than handAdditionalRaiseAmountsPreflop
            if (lastAdditionalRaiseAmount > additionalAmountRaised)
              lastAdditionalRaiseAmount = additionalAmountRaised;

            resultList.Add(new databaseQueries.PlayerAverageAdditionalRaiseAmountResultScaled(handActions[0].HandId, playerRaisesFlopIndex[i], 1, RaiseAmountsHelper.ScaleAdditionalRaiseAmount(previousRoundPotAmount + playerMoneyInPot.Values.Sum(), bigBlindAmount, lastAdditionalRaiseAmount, 100 * bigBlindAmount, additionalAmountRaised)));
          }
          #endregion
        }

        //Now finish of calculating the pot based on all remaining actions
        #region Finish calculating possible pot
        //Calculate the pot upto the flop
        for (int j = (playerRaisesFlopIndex.Length == 0 ? flopIndex[0] : playerRaisesFlopIndex.Last()); j < turnIndex[0]; j++)
        {
          long actionPlayerId = handActions[j].PlayerId;
          decimal actionAmount = handActions[j].ActionValue;

          if (!playerMoneyInPot.ContainsKey(actionPlayerId))
            playerMoneyInPot.Add(actionPlayerId, 0);

          //Loop through every action and decide what to add to the pot
          switch ((PokerAction)handActions[j].ActionTypeId)
          {
            case PokerAction.Call:
            {
              playerMoneyInPot[actionPlayerId] += actionAmount;
              break;
            }
            case PokerAction.Raise:
            {
              //If someone raises then the addition to the pot depends on what they have done previously this hand
              playerMoneyInPot[actionPlayerId] += actionAmount - playerMoneyInPot[actionPlayerId];
              break;
            }
          }
        }
        #endregion
      }

      /////////////////////////////////////////////////////////////
      /////////////////////////TURN////////////////////////////////
      /////////////////////////////////////////////////////////////
      if (turnIndex[0] < endIndex)
      {
        previousRoundPotAmount += playerMoneyInPot.Values.Sum();
        playerMoneyInPot = new Dictionary<long, decimal>();
        lastAdditionalRaiseAmount = 0;
        lastAdditionalRaiseToAmount = 0;

        var playerRaisesTurnIndex = (from current in handActions
                                     where current.ActionTypeId == (byte)PokerAction.Raise
                                     where current.PlayerId == playerId
                                     where current.LocalSeqIndex >= turnIndex[0] && current.LocalSeqIndex <= riverIndex[0]
                                     orderby current.LocalSeqIndex ascending
                                     select current.LocalSeqIndex).ToArray();

        if (playerRaisesTurnIndex.Length > 0)
        {
          #region Calculate Player Additional Raise Amounts
          for (int i = 0; i < playerRaisesTurnIndex.Length; i++)
          {
            //Work out the pot and lastAdditionalRaiseAmount upto the players raise
            for (int j = (i == 0 ? turnIndex[0] : playerRaisesTurnIndex[i - 1]); j < playerRaisesTurnIndex[i]; j++)
            {
              long actionPlayerId = handActions[j].PlayerId;
              decimal actionAmount = handActions[j].ActionValue;

              if (!playerMoneyInPot.ContainsKey(actionPlayerId))
                playerMoneyInPot.Add(actionPlayerId, 0);

              //Loop through every action and decide what to add to the pot
              switch ((PokerAction)handActions[j].ActionTypeId)
              {
                case PokerAction.Call:
                {
                  playerMoneyInPot[actionPlayerId] += actionAmount;
                  break;
                }
                case PokerAction.Raise:
                {
                  //If someone raises then the addition to the pot depends on what they have done previously this hand
                  playerMoneyInPot[actionPlayerId] += actionAmount - playerMoneyInPot[actionPlayerId];
                  lastAdditionalRaiseAmount = actionAmount - lastAdditionalRaiseToAmount;
                  lastAdditionalRaiseToAmount = actionAmount;
                  break;
                }
              }
            }

            decimal additionalAmountRaised = handActions[playerRaisesTurnIndex[i]].ActionValue - lastAdditionalRaiseToAmount;

            //We restrict a raise to a minimum of a big blind
            if (bigBlindAmount > additionalAmountRaised)
              additionalAmountRaised = bigBlindAmount;
            //If a player raises all in then their raise will be less than handAdditionalRaiseAmountsPreflop
            if (lastAdditionalRaiseAmount > additionalAmountRaised)
              lastAdditionalRaiseAmount = additionalAmountRaised;

            resultList.Add(new databaseQueries.PlayerAverageAdditionalRaiseAmountResultScaled(handActions[0].HandId, playerRaisesTurnIndex[i], 2, RaiseAmountsHelper.ScaleAdditionalRaiseAmount(previousRoundPotAmount + playerMoneyInPot.Values.Sum(), bigBlindAmount, lastAdditionalRaiseAmount, 100 * bigBlindAmount, additionalAmountRaised)));
          }
          #endregion
        }

        //Now finish of calculating the pot based on all remaining actions
        #region Finish calculating possible pot
        //Calculate the pot upto the flop
        for (int j = (playerRaisesTurnIndex.Length == 0 ? turnIndex[0] : playerRaisesTurnIndex.Last()); j < riverIndex[0]; j++)
        {
          long actionPlayerId = handActions[j].PlayerId;
          decimal actionAmount = handActions[j].ActionValue;

          if (!playerMoneyInPot.ContainsKey(actionPlayerId))
            playerMoneyInPot.Add(actionPlayerId, 0);

          //Loop through every action and decide what to add to the pot
          switch ((PokerAction)handActions[j].ActionTypeId)
          {
            case PokerAction.Call:
            {
              playerMoneyInPot[actionPlayerId] += actionAmount;
              break;
            }
            case PokerAction.Raise:
            {
              //If someone raises then the addition to the pot depends on what they have done previously this hand
              playerMoneyInPot[actionPlayerId] += actionAmount - playerMoneyInPot[actionPlayerId];
              break;
            }
          }
        }
        #endregion
      }

      /////////////////////////////////////////////////////////////
      /////////////////////////RIVER///////////////////////////////
      /////////////////////////////////////////////////////////////
      if (riverIndex[0] < endIndex)
      {
        previousRoundPotAmount += playerMoneyInPot.Values.Sum();
        playerMoneyInPot = new Dictionary<long, decimal>();
        lastAdditionalRaiseAmount = 0;
        lastAdditionalRaiseToAmount = 0;

        var playerRaisesRiverIndex = (from current in handActions
                                      where current.ActionTypeId == (byte)PokerAction.Raise
                                      where current.PlayerId == playerId
                                      where current.LocalSeqIndex >= riverIndex[0] && current.LocalSeqIndex <= endIndex
                                      orderby current.LocalSeqIndex ascending
                                      select current.LocalSeqIndex).ToArray();

        if (playerRaisesRiverIndex.Length > 0)
        {
          #region Calculate Player Additional Raise Amounts
          for (int i = 0; i < playerRaisesRiverIndex.Length; i++)
          {
            //Work out the pot and lastAdditionalRaiseAmount upto the players raise
            for (int j = (i == 0 ? riverIndex[0] : playerRaisesRiverIndex[i - 1]); j < playerRaisesRiverIndex[i]; j++)
            {
              long actionPlayerId = handActions[j].PlayerId;
              decimal actionAmount = handActions[j].ActionValue;

              if (!playerMoneyInPot.ContainsKey(actionPlayerId))
                playerMoneyInPot.Add(actionPlayerId, 0);

              //Loop through every action and decide what to add to the pot
              switch ((PokerAction)handActions[j].ActionTypeId)
              {
                case PokerAction.Call:
                {
                  playerMoneyInPot[actionPlayerId] += actionAmount;
                  break;
                }
                case PokerAction.Raise:
                {
                  //If someone raises then the addition to the pot depends on what they have done previously this hand
                  playerMoneyInPot[actionPlayerId] += actionAmount - playerMoneyInPot[actionPlayerId];
                  lastAdditionalRaiseAmount = actionAmount - lastAdditionalRaiseToAmount;
                  lastAdditionalRaiseToAmount = actionAmount;
                  break;
                }
              }
            }

            decimal additionalAmountRaised = handActions[playerRaisesRiverIndex[i]].ActionValue - lastAdditionalRaiseToAmount;

            //We restrict a raise to a minimum of a big blind
            if (bigBlindAmount > additionalAmountRaised)
              additionalAmountRaised = bigBlindAmount;
            //If a player raises all in then their raise will be less than handAdditionalRaiseAmountsPreflop
            if (lastAdditionalRaiseAmount > additionalAmountRaised)
              lastAdditionalRaiseAmount = additionalAmountRaised;

            resultList.Add(new databaseQueries.PlayerAverageAdditionalRaiseAmountResultScaled(handActions[0].HandId, playerRaisesRiverIndex[i], 3, RaiseAmountsHelper.ScaleAdditionalRaiseAmount(previousRoundPotAmount + playerMoneyInPot.Values.Sum(), bigBlindAmount, lastAdditionalRaiseAmount, 100 * bigBlindAmount, additionalAmountRaised)));
          }
          #endregion
        }
      }
    }

    /// <summary>
    /// Returns the average additional raise amount of the provided player
    /// StartingIndex can be used to do incremental counts, default is 1. Hands counted start at the startingIndex. 
    /// MaxHands is the number of hands counted. i.e. start at handIndex 2 and count two hands (hands counted are 2 and 3).
    /// Set maxHands = -1 for all available hands.
    /// </summary>
    /// <param name="playerId"></param>
    /// <param name="startingIndex"></param>
    /// <param name="maxHands"></param>
    /// <returns></returns>
    public PlayerAverageAdditionalRaiseAmountResult[] csp_playerAdditionalRaiseAmountsRaw(long playerId, databaseSnapshotUsage snapshotUsage = databaseSnapshotUsage.CreateNew)
    {
      DatabaseSnapshot snapshot = GetDatabaseSnapshot(snapshotUsage);
      RAMDatabaseDataLists snapshotData = snapshot.EnterSnapshot();

      List<PlayerAverageAdditionalRaiseAmountResult> resultList = new List<PlayerAverageAdditionalRaiseAmountResult>();

      //We go through all hands inthe ram database
      foreach (KeyValuePair<long, List<databaseCache.handAction>> hand in snapshotData.handActionsSnapshot)
      {
        //For each hand go through the actions
        for (int i = 0; i < hand.Value.Count; i++)
        {
          //If this hand refers to the expected playerId
          if (hand.Value[i].PlayerId == playerId)
          {
            //We are interested in this hand if the player has raised
            if (hand.Value[i].ActionTypeId == (byte)PokerAction.Raise)
              csp_playerHandAdditionalRaiseAmountsRaw(hand.Value, playerId, resultList);
          }
        }
      }

      //Return the average amount won / lost per hand
      return resultList.ToArray();
    }

    /// <summary>
    /// Calculates the additional raise amounts for a player in a given hand and returns each amount as a list entry.
    /// If the player does not raise returns -1
    /// </summary>
    /// <param name="handActions"></param>
    /// <param name="playerId"></param>
    /// <returns></returns>
    private void csp_playerHandAdditionalRaiseAmountsRaw(List<databaseCache.handAction> handActions, long playerId, List<PlayerAverageAdditionalRaiseAmountResult> resultList)
    {
      byte startIndex = handActions[0].LocalSeqIndex;
      byte endIndex = handActions[handActions.Count - 1].LocalSeqIndex;
      var flopIndex = (from current in handActions where current.ActionTypeId == (byte)PokerAction.DealFlop select current.LocalSeqIndex).Take(1).ToArray();
      var turnIndex = (from current in handActions where current.ActionTypeId == (byte)PokerAction.DealTurn select current.LocalSeqIndex).Take(1).ToArray();
      var riverIndex = (from current in handActions where current.ActionTypeId == (byte)PokerAction.DealRiver select current.LocalSeqIndex).Take(1).ToArray();

      if (flopIndex.Length == 0)
        flopIndex = new byte[] { endIndex };
      if (turnIndex.Length == 0)
        turnIndex = new byte[] { endIndex };
      if (riverIndex.Length == 0)
        riverIndex = new byte[] { endIndex };

      short[] bettingActionTypes = new short[] { (byte)PokerAction.LittleBlind, (byte)PokerAction.BigBlind, (byte)PokerAction.Call, (byte)PokerAction.Raise };

      decimal bigBlindAmount = (from current in handActions where current.ActionTypeId == (byte)PokerAction.BigBlind select current.ActionValue).First();
      decimal lastAdditionalRaiseAmount = bigBlindAmount;
      decimal lastAdditionalRaiseToAmount = bigBlindAmount;

      //Keeps a track of what money each player has in a pot
      decimal previousRoundPotAmount = 0;
      Dictionary<long, decimal> playerMoneyInPot = new Dictionary<long, decimal>();

      /////////////////////////////////////////////////////////////
      /////////////////////////PRE-FLOP////////////////////////////
      /////////////////////////////////////////////////////////////

      //Did the player raise pre flop?
      var playerRaisesPreFlopIndex = (from current in handActions
                                      where current.ActionTypeId == (byte)PokerAction.Raise
                                      where current.PlayerId == playerId
                                      where current.LocalSeqIndex >= startIndex && current.LocalSeqIndex <= flopIndex[0]
                                      orderby current.LocalSeqIndex ascending
                                      select current.LocalSeqIndex).ToArray();

      //In order to know the additional raise amount we need to know the pot as well as the minimum and maximum raise at each point
      if (playerRaisesPreFlopIndex.Length > 0)
      {
        #region Calculate Player Additional Raise Amounts
        for (int i = 0; i < playerRaisesPreFlopIndex.Length; i++)
        {
          //Work out the pot and lastAdditionalRaiseAmount upto the players raise
          for (int j = (i == 0 ? 0 : playerRaisesPreFlopIndex[i - 1]); j < playerRaisesPreFlopIndex[i]; j++)
          {
            long actionPlayerId = handActions[j].PlayerId;
            decimal actionAmount = handActions[j].ActionValue;

            //Ensure the raising player has an entry in the dictionary
            if (!playerMoneyInPot.ContainsKey(actionPlayerId))
              playerMoneyInPot.Add(actionPlayerId, 0);

            //Loop through every action and decide what to add to the pot
            switch ((PokerAction)handActions[j].ActionTypeId)
            {
              case PokerAction.LittleBlind:
              {
                playerMoneyInPot[actionPlayerId] += actionAmount;
                break;
              }
              case PokerAction.BigBlind:
              {
                playerMoneyInPot[actionPlayerId] += actionAmount;
                break;
              }
              case PokerAction.Call:
              {
                playerMoneyInPot[actionPlayerId] += actionAmount;
                break;
              }
              case PokerAction.Raise:
              {
                //If someone raises then the addition to the pot depends on what they have done previously this hand
                playerMoneyInPot[actionPlayerId] += actionAmount - playerMoneyInPot[actionPlayerId];
                lastAdditionalRaiseAmount = actionAmount - lastAdditionalRaiseToAmount;
                lastAdditionalRaiseToAmount = actionAmount;
                break;
              }
            }
          }

          decimal additionalAmountRaised = handActions[playerRaisesPreFlopIndex[i]].ActionValue - lastAdditionalRaiseToAmount;

          //We restrict a raise to a minimum of a big blind
          if (bigBlindAmount > additionalAmountRaised)
            additionalAmountRaised = bigBlindAmount;
          //If a player raises all in then their raise will be less than handAdditionalRaiseAmountsPreflop
          if (lastAdditionalRaiseAmount > additionalAmountRaised)
            lastAdditionalRaiseAmount = additionalAmountRaised;

          resultList.Add(new PlayerAverageAdditionalRaiseAmountResult(handActions[0].HandId, playerRaisesPreFlopIndex[i], 0, previousRoundPotAmount + playerMoneyInPot.Values.Sum(), bigBlindAmount, lastAdditionalRaiseAmount, 100 * bigBlindAmount, additionalAmountRaised));
        }
        #endregion
      }

      //Now finish of calculating the pot based on all remaining actions
      #region Finish calculating possible pot
      //Calculate the pot upto the flop
      for (int j = (playerRaisesPreFlopIndex.Length == 0 ? 0 : playerRaisesPreFlopIndex.Last()); j < flopIndex[0]; j++)
      {
        long actionPlayerId = handActions[j].PlayerId;
        decimal actionAmount = handActions[j].ActionValue;

        if (!playerMoneyInPot.ContainsKey(actionPlayerId))
          playerMoneyInPot.Add(actionPlayerId, 0);

        //Loop through every action and decide what to add to the pot
        switch ((PokerAction)handActions[j].ActionTypeId)
        {
          case PokerAction.LittleBlind:
          {
            playerMoneyInPot[actionPlayerId] += actionAmount;
            break;
          }
          case PokerAction.BigBlind:
          {
            playerMoneyInPot[actionPlayerId] += actionAmount;
            break;
          }
          case PokerAction.Call:
          {
            playerMoneyInPot[actionPlayerId] += actionAmount;
            break;
          }
          case PokerAction.Raise:
          {
            //If someone raises then the addition to the pot depends on what they have done previously this hand
            playerMoneyInPot[actionPlayerId] += actionAmount - playerMoneyInPot[actionPlayerId];
            break;
          }
        }
      }
      #endregion

      /////////////////////////////////////////////////////////////
      /////////////////////////FLOP////////////////////////////////
      /////////////////////////////////////////////////////////////
      if (flopIndex[0] < endIndex)
      {
        previousRoundPotAmount += playerMoneyInPot.Values.Sum();
        playerMoneyInPot = new Dictionary<long, decimal>();
        lastAdditionalRaiseAmount = 0;
        lastAdditionalRaiseToAmount = 0;

        var playerRaisesFlopIndex = (from current in handActions
                                     where current.ActionTypeId == (byte)PokerAction.Raise
                                     where current.PlayerId == playerId
                                     where current.LocalSeqIndex >= flopIndex[0] && current.LocalSeqIndex <= turnIndex[0]
                                     orderby current.LocalSeqIndex ascending
                                     select current.LocalSeqIndex).ToArray();

        if (playerRaisesFlopIndex.Length > 0)
        {
          #region Calculate Player Additional Raise Amounts
          for (int i = 0; i < playerRaisesFlopIndex.Length; i++)
          {
            //Work out the pot and lastAdditionalRaiseAmount upto the players raise
            for (int j = (i == 0 ? flopIndex[0] : playerRaisesFlopIndex[i - 1]); j < playerRaisesFlopIndex[i]; j++)
            {
              long actionPlayerId = handActions[j].PlayerId;
              decimal actionAmount = handActions[j].ActionValue;

              if (!playerMoneyInPot.ContainsKey(actionPlayerId))
                playerMoneyInPot.Add(actionPlayerId, 0);

              //Loop through every action and decide what to add to the pot
              switch ((PokerAction)handActions[j].ActionTypeId)
              {
                case PokerAction.Call:
                {
                  playerMoneyInPot[actionPlayerId] += actionAmount;
                  break;
                }
                case PokerAction.Raise:
                {
                  //If someone raises then the addition to the pot depends on what they have done previously this hand
                  playerMoneyInPot[actionPlayerId] += actionAmount - playerMoneyInPot[actionPlayerId];
                  lastAdditionalRaiseAmount = actionAmount - lastAdditionalRaiseToAmount;
                  lastAdditionalRaiseToAmount = actionAmount;
                  break;
                }
              }
            }

            decimal additionalAmountRaised = handActions[playerRaisesFlopIndex[i]].ActionValue - lastAdditionalRaiseToAmount;

            //We restrict a raise to a minimum of a big blind
            if (bigBlindAmount > additionalAmountRaised)
              additionalAmountRaised = bigBlindAmount;
            //If a player raises all in then their raise will be less than handAdditionalRaiseAmountsPreflop
            if (lastAdditionalRaiseAmount > additionalAmountRaised)
              lastAdditionalRaiseAmount = additionalAmountRaised;

            resultList.Add(new PlayerAverageAdditionalRaiseAmountResult(handActions[0].HandId, playerRaisesFlopIndex[i], 1, previousRoundPotAmount + playerMoneyInPot.Values.Sum(), bigBlindAmount, lastAdditionalRaiseAmount, 100 * bigBlindAmount, additionalAmountRaised));
          }
          #endregion
        }

        //Now finish of calculating the pot based on all remaining actions
        #region Finish calculating possible pot
        //Calculate the pot upto the flop
        for (int j = (playerRaisesFlopIndex.Length == 0 ? flopIndex[0] : playerRaisesFlopIndex.Last()); j < turnIndex[0]; j++)
        {
          long actionPlayerId = handActions[j].PlayerId;
          decimal actionAmount = handActions[j].ActionValue;

          if (!playerMoneyInPot.ContainsKey(actionPlayerId))
            playerMoneyInPot.Add(actionPlayerId, 0);

          //Loop through every action and decide what to add to the pot
          switch ((PokerAction)handActions[j].ActionTypeId)
          {
            case PokerAction.Call:
            {
              playerMoneyInPot[actionPlayerId] += actionAmount;
              break;
            }
            case PokerAction.Raise:
            {
              //If someone raises then the addition to the pot depends on what they have done previously this hand
              playerMoneyInPot[actionPlayerId] += actionAmount - playerMoneyInPot[actionPlayerId];
              break;
            }
          }
        }
        #endregion
      }

      /////////////////////////////////////////////////////////////
      /////////////////////////TURN////////////////////////////////
      /////////////////////////////////////////////////////////////
      if (turnIndex[0] < endIndex)
      {
        previousRoundPotAmount += playerMoneyInPot.Values.Sum();
        playerMoneyInPot = new Dictionary<long, decimal>();
        lastAdditionalRaiseAmount = 0;
        lastAdditionalRaiseToAmount = 0;

        var playerRaisesTurnIndex = (from current in handActions
                                     where current.ActionTypeId == (byte)PokerAction.Raise
                                     where current.PlayerId == playerId
                                     where current.LocalSeqIndex >= turnIndex[0] && current.LocalSeqIndex <= riverIndex[0]
                                     orderby current.LocalSeqIndex ascending
                                     select current.LocalSeqIndex).ToArray();

        if (playerRaisesTurnIndex.Length > 0)
        {
          #region Calculate Player Additional Raise Amounts
          for (int i = 0; i < playerRaisesTurnIndex.Length; i++)
          {
            //Work out the pot and lastAdditionalRaiseAmount upto the players raise
            for (int j = (i == 0 ? turnIndex[0] : playerRaisesTurnIndex[i - 1]); j < playerRaisesTurnIndex[i]; j++)
            {
              long actionPlayerId = handActions[j].PlayerId;
              decimal actionAmount = handActions[j].ActionValue;

              if (!playerMoneyInPot.ContainsKey(actionPlayerId))
                playerMoneyInPot.Add(actionPlayerId, 0);

              //Loop through every action and decide what to add to the pot
              switch ((PokerAction)handActions[j].ActionTypeId)
              {
                case PokerAction.Call:
                {
                  playerMoneyInPot[actionPlayerId] += actionAmount;
                  break;
                }
                case PokerAction.Raise:
                {
                  //If someone raises then the addition to the pot depends on what they have done previously this hand
                  playerMoneyInPot[actionPlayerId] += actionAmount - playerMoneyInPot[actionPlayerId];
                  lastAdditionalRaiseAmount = actionAmount - lastAdditionalRaiseToAmount;
                  lastAdditionalRaiseToAmount = actionAmount;
                  break;
                }
              }
            }

            decimal additionalAmountRaised = handActions[playerRaisesTurnIndex[i]].ActionValue - lastAdditionalRaiseToAmount;

            //We restrict a raise to a minimum of a big blind
            if (bigBlindAmount > additionalAmountRaised)
              additionalAmountRaised = bigBlindAmount;
            //If a player raises all in then their raise will be less than handAdditionalRaiseAmountsPreflop
            if (lastAdditionalRaiseAmount > additionalAmountRaised)
              lastAdditionalRaiseAmount = additionalAmountRaised;

            resultList.Add(new PlayerAverageAdditionalRaiseAmountResult(handActions[0].HandId, playerRaisesTurnIndex[i], 2, previousRoundPotAmount + playerMoneyInPot.Values.Sum(), bigBlindAmount, lastAdditionalRaiseAmount, 100 * bigBlindAmount, additionalAmountRaised));
          }
          #endregion
        }

        //Now finish of calculating the pot based on all remaining actions
        #region Finish calculating possible pot
        //Calculate the pot upto the flop
        for (int j = (playerRaisesTurnIndex.Length == 0 ? turnIndex[0] : playerRaisesTurnIndex.Last()); j < riverIndex[0]; j++)
        {
          long actionPlayerId = handActions[j].PlayerId;
          decimal actionAmount = handActions[j].ActionValue;

          if (!playerMoneyInPot.ContainsKey(actionPlayerId))
            playerMoneyInPot.Add(actionPlayerId, 0);

          //Loop through every action and decide what to add to the pot
          switch ((PokerAction)handActions[j].ActionTypeId)
          {
            case PokerAction.Call:
            {
              playerMoneyInPot[actionPlayerId] += actionAmount;
              break;
            }
            case PokerAction.Raise:
            {
              //If someone raises then the addition to the pot depends on what they have done previously this hand
              playerMoneyInPot[actionPlayerId] += actionAmount - playerMoneyInPot[actionPlayerId];
              break;
            }
          }
        }
        #endregion
      }

      /////////////////////////////////////////////////////////////
      /////////////////////////RIVER///////////////////////////////
      /////////////////////////////////////////////////////////////
      if (riverIndex[0] < endIndex)
      {
        previousRoundPotAmount += playerMoneyInPot.Values.Sum();
        playerMoneyInPot = new Dictionary<long, decimal>();
        lastAdditionalRaiseAmount = 0;
        lastAdditionalRaiseToAmount = 0;

        var playerRaisesRiverIndex = (from current in handActions
                                      where current.ActionTypeId == (byte)PokerAction.Raise
                                      where current.PlayerId == playerId
                                      where current.LocalSeqIndex >= riverIndex[0] && current.LocalSeqIndex <= endIndex
                                      orderby current.LocalSeqIndex ascending
                                      select current.LocalSeqIndex).ToArray();

        if (playerRaisesRiverIndex.Length > 0)
        {
          #region Calculate Player Additional Raise Amounts
          for (int i = 0; i < playerRaisesRiverIndex.Length; i++)
          {
            //Work out the pot and lastAdditionalRaiseAmount upto the players raise
            for (int j = (i == 0 ? riverIndex[0] : playerRaisesRiverIndex[i - 1]); j < playerRaisesRiverIndex[i]; j++)
            {
              long actionPlayerId = handActions[j].PlayerId;
              decimal actionAmount = handActions[j].ActionValue;

              if (!playerMoneyInPot.ContainsKey(actionPlayerId))
                playerMoneyInPot.Add(actionPlayerId, 0);

              //Loop through every action and decide what to add to the pot
              switch ((PokerAction)handActions[j].ActionTypeId)
              {
                case PokerAction.Call:
                {
                  playerMoneyInPot[actionPlayerId] += actionAmount;
                  break;
                }
                case PokerAction.Raise:
                {
                  //If someone raises then the addition to the pot depends on what they have done previously this hand
                  playerMoneyInPot[actionPlayerId] += actionAmount - playerMoneyInPot[actionPlayerId];
                  lastAdditionalRaiseAmount = actionAmount - lastAdditionalRaiseToAmount;
                  lastAdditionalRaiseToAmount = actionAmount;
                  break;
                }
              }
            }

            decimal additionalAmountRaised = handActions[playerRaisesRiverIndex[i]].ActionValue - lastAdditionalRaiseToAmount;

            //We restrict a raise to a minimum of a big blind
            if (bigBlindAmount > additionalAmountRaised)
              additionalAmountRaised = bigBlindAmount;
            //If a player raises all in then their raise will be less than handAdditionalRaiseAmountsPreflop
            if (lastAdditionalRaiseAmount > additionalAmountRaised)
              lastAdditionalRaiseAmount = additionalAmountRaised;

            resultList.Add(new PlayerAverageAdditionalRaiseAmountResult(handActions[0].HandId, playerRaisesRiverIndex[i], 3, previousRoundPotAmount + playerMoneyInPot.Values.Sum(), bigBlindAmount, lastAdditionalRaiseAmount, 100 * bigBlindAmount, additionalAmountRaised));
          }
          #endregion
        }
      }
    }

    /// <summary>
    /// Returns the total amount won by a player Id, defined as summing all 'Win Pot' actions.
    /// To work out total amount won or lost you need to minus 'amountsGambledByPlayerId'.
    /// StartingIndex can be used to do incremental counts, default is 1. Hands counted start at the startingIndex. MaxHands is the number of hands counted. i.e. start at handIndex 2 and count two hands (hands counted are 2 and 3).
    /// Set maxHands = -1 for all available hands.
    /// </summary>
    /// <param name="playerId"></param>
    /// <returns></returns>
    public decimal csp_amountsWonByPlayerId(long playerId, int startingIndex, int maxHands)
    {
      throw new NotImplementedException();
    }

    /// <summary>
    /// Returns the average amount won per hand by a player Id.
    /// StartingIndex can be used to do incremental counts, default is 1. Hands counted start at the startingIndex. 
    /// MaxHands is the number of hands counted. i.e. start at handIndex 2 and count two hands (hands counted are 2 and 3).
    /// Set maxHands = -1 for all available hands.
    /// </summary>
    /// <param name="playerId"></param>
    /// <returns></returns>
    public void csp_amountsGambledWonByPlayerId(long playerId, int startingIndex, int maxHands, ref decimal amountsWon, ref decimal amountsGambled, databaseSnapshotUsage snapshotUsage = databaseSnapshotUsage.CreateNew)
    {
      //We are not going to both with the caching here because there are too many method inputs
      DatabaseSnapshot snapshot = GetDatabaseSnapshot(snapshotUsage);
      RAMDatabaseDataLists snapshotData = snapshot.EnterSnapshot();

      int playerHandCount = 0;
      int handsCounted = 0;

      //We use maxHands -1 to mean all hands
      if (maxHands == -1)
      {
        maxHands = int.MaxValue;
        startingIndex = 1;
      }

      //Go through hands until playerHandCount = startIndex -1
      //Then count forward for maxHands keeping track of amountsGambled and amountsWon

      foreach (KeyValuePair<long, List<databaseCache.handAction>> hand in snapshotData.handActionsSnapshot)
      {
        //For each hand look for an appropriate player action
        for (int i = 0; i < hand.Value.Count; i++)
        {
          if (hand.Value[i].PlayerId == playerId)
          {
            //We are interested in this hand if the player has acted
            if (hand.Value[i].ActionTypeId == (byte)PokerAction.LittleBlind || hand.Value[i].ActionTypeId == (byte)PokerAction.BigBlind || hand.Value[i].ActionTypeId == (byte)PokerAction.Fold || hand.Value[i].ActionTypeId == (byte)PokerAction.Check || hand.Value[i].ActionTypeId == (byte)PokerAction.Call || hand.Value[i].ActionTypeId == (byte)PokerAction.Raise)
            {
              //-1 because startingIndex is 1 indexed 
              if (playerHandCount >= startingIndex - 1)
              {
                //Work out amounts gambled and won for this hand
                decimal currentAmountsGambled = 0, currentAmountsWon = 0;
                csp_AmountsWonGambled(hand.Value, playerId, ref currentAmountsGambled, ref currentAmountsWon);

                amountsGambled += currentAmountsGambled;
                amountsWon += currentAmountsWon;

                playerHandCount++;
                handsCounted++;

                //Once we have counted this hand we can break;
                break;
              }
              else
                //If we have not yet reached the necessary starting hand just increment playerHandCount
                playerHandCount++;
            }
          }
        }

        //Once handsCounted equals maxHands we can stop going through the hands
        if (handsCounted >= maxHands)
          break;
      }

      //Return the average amount won / lost per hand
      //return (amountsWon-amountsGambled)/(decimal)handsCounted;
    }

    /// <summary>
    /// Goes through the hand and sets the amountsGambled and amountsWon
    /// </summary>
    /// <param name="handActions"></param>
    /// <param name="amountsGambled"></param>
    /// <param name="amountsWon"></param>
    private static void csp_AmountsWonGambled(List<databaseCache.handAction> handActions, long playerId, ref decimal amountsGambled, ref decimal amountsWon)
    {
      //Get the indexes of the various rounds in the hand
      byte startIndex = handActions[0].LocalSeqIndex;
      byte endIndex = handActions[handActions.Count - 1].LocalSeqIndex;
      var flopIndex = (from current in handActions where current.ActionTypeId == (byte)PokerAction.DealFlop select current.LocalSeqIndex).Take(1).ToArray();
      var turnIndex = (from current in handActions where current.ActionTypeId == (byte)PokerAction.DealTurn select current.LocalSeqIndex).Take(1).ToArray();
      var riverIndex = (from current in handActions where current.ActionTypeId == (byte)PokerAction.DealRiver select current.LocalSeqIndex).Take(1).ToArray();

      if (flopIndex.Length == 0)
        flopIndex = new byte[] { endIndex };
      if (turnIndex.Length == 0)
        turnIndex = new byte[] { endIndex };
      if (riverIndex.Length == 0)
        riverIndex = new byte[] { endIndex };

      short[] bettingActionTypes = new short[] { (byte)PokerAction.LittleBlind, (byte)PokerAction.BigBlind, (byte)PokerAction.Call, (byte)PokerAction.Raise };

      /////////////////////////////////////////////////////////////
      /////////////////////////PRE-FLOP////////////////////////////
      /////////////////////////////////////////////////////////////
      var lastRaisePreFlopIndex = (from current in handActions
                                   where current.ActionTypeId == (byte)PokerAction.Raise
                                   where current.PlayerId == playerId
                                   where current.LocalSeqIndex >= startIndex && current.LocalSeqIndex <= flopIndex[0]
                                   orderby current.LocalSeqIndex descending
                                   select current.LocalSeqIndex).Take(1).ToArray();

      if (lastRaisePreFlopIndex.Length == 0)
        lastRaisePreFlopIndex = new byte[] { startIndex };

      amountsGambled += (from current in handActions
                         where bettingActionTypes.Contains(current.ActionTypeId)
                         where current.PlayerId == playerId
                         where current.LocalSeqIndex >= lastRaisePreFlopIndex[0] && current.LocalSeqIndex <= flopIndex[0]
                         select current.ActionValue).Sum();

      /////////////////////////////////////////////////////////////
      /////////////////////////FLOP////////////////////////////////
      /////////////////////////////////////////////////////////////
      if (flopIndex[0] < endIndex)
      {
        var lastRaiseFlopIndex = (from current in handActions
                                  where current.ActionTypeId == (byte)PokerAction.Raise
                                  where current.PlayerId == playerId
                                  where current.LocalSeqIndex >= flopIndex[0] && current.LocalSeqIndex <= turnIndex[0]
                                  orderby current.LocalSeqIndex descending
                                  select current.LocalSeqIndex).Take(1).ToArray();

        if (lastRaiseFlopIndex.Length == 0)
          lastRaiseFlopIndex = flopIndex;

        amountsGambled += (from current in handActions
                           where bettingActionTypes.Contains(current.ActionTypeId)
                           where current.PlayerId == playerId
                           where current.LocalSeqIndex >= lastRaiseFlopIndex[0] && current.LocalSeqIndex <= turnIndex[0]
                           select current.ActionValue).Sum();
      }

      /////////////////////////////////////////////////////////////
      /////////////////////////TURN////////////////////////////////
      /////////////////////////////////////////////////////////////
      if (turnIndex[0] < endIndex)
      {
        var lastRaiseTurnIndex = (from current in handActions
                                  where current.ActionTypeId == (byte)PokerAction.Raise
                                  where current.PlayerId == playerId
                                  where current.LocalSeqIndex >= turnIndex[0] && current.LocalSeqIndex <= riverIndex[0]
                                  orderby current.LocalSeqIndex descending
                                  select current.LocalSeqIndex).Take(1).ToArray();

        if (lastRaiseTurnIndex.Length == 0)
          lastRaiseTurnIndex = turnIndex;

        amountsGambled += (from current in handActions
                           where bettingActionTypes.Contains(current.ActionTypeId)
                           where current.PlayerId == playerId
                           where current.LocalSeqIndex >= lastRaiseTurnIndex[0] && current.LocalSeqIndex <= riverIndex[0]
                           select current.ActionValue).Sum();
      }

      /////////////////////////////////////////////////////////////
      /////////////////////////RIVER///////////////////////////////
      /////////////////////////////////////////////////////////////
      if (riverIndex[0] < endIndex)
      {
        var lastRaiseRiverIndex = (from current in handActions
                                   where current.ActionTypeId == (byte)PokerAction.Raise
                                   where current.PlayerId == playerId
                                   where current.LocalSeqIndex >= riverIndex[0] && current.LocalSeqIndex <= endIndex
                                   orderby current.LocalSeqIndex descending
                                   select current.LocalSeqIndex).Take(1).ToArray();

        if (lastRaiseRiverIndex.Length == 0)
          lastRaiseRiverIndex = riverIndex;

        amountsGambled += (from current in handActions
                           where bettingActionTypes.Contains(current.ActionTypeId)
                           where current.PlayerId == playerId
                           where current.LocalSeqIndex >= lastRaiseRiverIndex[0] && current.LocalSeqIndex <= endIndex
                           select current.ActionValue).Sum();
      }

      /////////////////////////////////////////////////////////////
      /////////////////////////AMOUNTSWON//////////////////////////
      /////////////////////////////////////////////////////////////
      amountsWon += (from current in handActions
                     where current.ActionTypeId == (byte)PokerAction.WinPot || current.ActionTypeId == (byte)PokerAction.ReturnBet
                     where current.PlayerId == playerId
                     select current.ActionValue).Sum();
    }

    /// <summary>
    /// Returns an array which encapsulates a players hole card usage.
    /// Card 1 is always larger than card 2.
    /// Suited cards are all changed to clubs.
    /// Unsuited cards are clubs and diamods.
    /// </summary>
    /// <param name="playerId"></param>
    /// <returns></returns>
    public Dictionary<Card, Dictionary<Card, CardUsage>> csp_PlayerPreflopCardUsage(long playerId, databaseSnapshotUsage snapshotUsage = databaseSnapshotUsage.CreateNew)
    {
      //Build starting array
      var cardUsageResult = CardUsage.EmptyCardUsageResultDict();

      //Now start going through player hands and recording stats
      DatabaseSnapshot snapshot = GetDatabaseSnapshot(snapshotUsage);
      RAMDatabaseDataLists snapshotData = snapshot.EnterSnapshot();

      //Get the hands in which this player acts
      //So that we don't count hands for genetic opponeent players multiple times this method returns only unique hands
      long[] handIds = RAMDatabase.HandIdsInWhichPlayerActsUnique(playerId, snapshot);

      for (int i = 0; i < handIds.Length; i++)
      {

        //Get the hole cards for this player for this hand
        #region GetCards
        var handHoleCards = snapshotData.holeCardsSnapshot[handIds[i]];

        if (!handHoleCards.ContainsKey(playerId))
          throw new Exception("Unable to locate player hole cards for handId " + handIds[i]);

        Card playerCard1 = (Card)handHoleCards[playerId].HoleCard1;
        Card playerCard2 = (Card)handHoleCards[playerId].HoleCard2;

        //Make sure the cards are the right way round
        if (playerCard1 < playerCard2)
          //a = a + b - (b = a); // http://tiagoe.blogspot.com/2007/10/swap-two-variables-in-one-line-in-c.html
          playerCard1 = (Card)((byte)playerCard1 + (byte)playerCard2 - (byte)(playerCard2 = playerCard1));

        //Get the correct index
        Card index1 = 0, index2 = 0;
        if (Deck.GetCardSuit(playerCard1) == Deck.GetCardSuit(playerCard2))
        {
          //Convert both cards to clubs
          index1 = (Card)(Deck.GetCardNumber(playerCard1) * 4 - 3);
          index2 = (Card)(Deck.GetCardNumber(playerCard2) * 4 - 3);
        }
        else
        {
          //Convert cards to clubs and diamonds
          index1 = (Card)(Deck.GetCardNumber(playerCard1) * 4 - 3);
          index2 = (Card)(Deck.GetCardNumber(playerCard2) * 4 - 2);
        }
        #endregion

        //Increment the recieved count
        //We used to do this increment here but we need to make sure the player actually got to act
        //i.e. if the player was big blind and everyone folds round they wont get to act!
        //cardUsageResult[index1][index2].receivedCount++;

        //Now determine if the player played the hand
        var currentHandActions = snapshotData.handActionsSnapshot[handIds[i]];

        //If the players first action is fold they did not play
        //If the players first action is check we look for a fold or call/raise actions
        foreach (var action in currentHandActions)
        {
          //We are only interested in preflop actions
          if (action.ActionTypeId == (byte)PokerAction.DealFlop)
            break;

          if (action.PlayerId == playerId)
          {
            //We are interested in a players first action
            if (action.ActionTypeId == (byte)PokerAction.Fold)
            {
              //We only increment the received count if they actually acted
              cardUsageResult[index1][index2].receivedCount++;
              break;
            }
            else if (action.ActionTypeId == (byte)PokerAction.Check)
            {
              //We only increment the received count if they actually acted
              cardUsageResult[index1][index2].receivedCount++;
              cardUsageResult[index1][index2].checkedCount++;
              break;
            }
            else if (action.ActionTypeId == (byte)PokerAction.Call)
            {
              //We only increment the received count if they actually acted
              cardUsageResult[index1][index2].receivedCount++;
              cardUsageResult[index1][index2].calledCount++;
              break;
            }
            else if (action.ActionTypeId == (byte)PokerAction.Raise)
            {
              //We only increment the received count if they actually acted
              cardUsageResult[index1][index2].receivedCount++;
              cardUsageResult[index1][index2].raisedCount++;
              break;
            }
          }
        }
      }

      return cardUsageResult;
    }

    /// <summary>
    /// Deletes database data for the provided pokerClientId
    /// </summary>
    /// <param name="pokerClientId"></param>
    /// <param name="deletePlayers"></param>
    public void csp_DeleteDatabaseData(short pokerClientId, bool deletePlayers)
    {
      if (deletePlayers)
        throw new NotImplementedException("Not yet implemented.");

      throw new NotImplementedException();
    }

    public void csp_DeleteOldPlayerHands(long[] playerIds, int numberHandsToDelete)
    {
      int[] handsDeleteCounter = new int[playerIds.Length];
      List<long> handsToDelete = new List<long>();

      databaseChangesSinceLastSnapshot = true;

      #region handActions
      lock (actionLocker)
      {
        //Go through hands until playerHandCount = startIndex -1
        //Then count forward for maxHands keeping track of amountsGambled and amountsWon
        foreach (KeyValuePair<long, List<databaseCache.handAction>> hand in handActions)
        {
          //For each hand look at the actions
          for (int i = 0; i < hand.Value.Count; i++)
          {
            //Look if the player is in playerIds
            for (int j = 0; j < playerIds.Length; j++)
            {
              if (hand.Value[i].PlayerId == playerIds[j])
              {
                if (handsDeleteCounter[j] < numberHandsToDelete)
                {
                  handsToDelete.Add(hand.Value[0].HandId);
                  handsDeleteCounter[j]++;

                  //Once we have chosen to delete this hand we can go to the next one
                  i = hand.Value.Count;
                  break;
                }
              }
            }
          }
        }

        //Delete the necessary actions
        handActions = new SortedDictionary<long, List<databaseCache.handAction>>((from current in handActions
                                                                                  where !handsToDelete.Contains(current.Key)
                                                                                  select current).ToDictionary(k => k.Key, k => k.Value));
      }
      #endregion

      #region holeCards
      lock (holeCardLocker)
      {
        holeCards = (from current in holeCards
                     where !handsToDelete.Contains(current.Key)
                     select current).ToDictionary(k => k.Key, k => k.Value);
      }
      #endregion

      #region pokerHands
      //We now need to get the tableIds
      List<long> tablesToDelete = new List<long>();
      lock (handLocker)
      {
        foreach (var table in pokerHands)
        {
          for (int i = 0; i < handsToDelete.Count; i++)
          {
            //If this table contains a hand of interest we can delete the hand
            if (table.Value.ContainsKey(handsToDelete[i]))
              table.Value.Remove(handsToDelete[i]);
          }

          //If we have removed all hands from this table then we can delete the table as well
          if (table.Value.Count == 0)
            tablesToDelete.Add(table.Key);
        }

        //Reset the pokerHands list
        pokerHands = new SortedDictionary<long, SortedDictionary<long, databaseCache.pokerHand>>((from current in pokerHands
                                                                                                  where !tablesToDelete.Contains(current.Key)
                                                                                                  select current).ToDictionary(k => k.Key, k => k.Value));
      }
      #endregion

      #region pokerTables
      lock (tableLocker)
      {
        pokerTables = new SortedDictionary<long, databaseCache.pokerTable>((from current in pokerTables
                                                                            where !tablesToDelete.Contains(current.Key)
                                                                            select current).ToDictionary(k => k.Key, k => k.Value));
      }
      #endregion

      GC.Collect();
    }

    public long[] csp_HandIdsPlayedByPlayerId(long playerId, databaseSnapshotUsage snapshotUsage = databaseSnapshotUsage.CreateNew)
    {
      DatabaseSnapshot snapshot = GetDatabaseSnapshot(snapshotUsage);

      Dictionary<long, object> cacheValues = snapshot.GetSnapshotCacheValue("csp_HandIdsPlayedByPlayerId") as Dictionary<long, object>;
      if (cacheValues != null)
      {
        if (cacheValues.ContainsKey(playerId))
          return cacheValues[playerId] as long[];
      }

      RAMDatabaseDataLists cacheData = snapshot.EnterSnapshot();
      List<long> handIdsPlayed = new List<long>();

      foreach (KeyValuePair<long, List<databaseCache.handAction>> hand in cacheData.handActionsSnapshot)
      {
        //For each hand look for an appropriate player action
        for (int i = 0; i < hand.Value.Count; i++)
        {
          if (hand.Value[i].PlayerId == playerId)
          {
            //Break the if down as a performance enhanchment
            if (hand.Value[i].ActionTypeId == (byte)PokerAction.LittleBlind || hand.Value[i].ActionTypeId == (byte)PokerAction.BigBlind || hand.Value[i].ActionTypeId == (byte)PokerAction.Fold || hand.Value[i].ActionTypeId == (byte)PokerAction.Check || hand.Value[i].ActionTypeId == (byte)PokerAction.Call || hand.Value[i].ActionTypeId == (byte)PokerAction.Raise)
            {
              handIdsPlayed.Add(hand.Key);

              //Once we have a player action we can end the current hand
              break;
            }
          }
        }
      }

      long[] returnArray = handIdsPlayed.ToArray();
      snapshot.AddSnapshotCacheValue("csp_HandIdsPlayedByPlayerId", new Dictionary<long, object>() { { playerId, returnArray } });
      return returnArray;
    }

    /// <summary>
    /// Write out the contents of the ramDatabase for the provided tableId in typical hand history format
    /// </summary>
    /// <param name="saveoutFileName"></param>
    public void csp_SaveOutRamDataHandHistory(long tableId, string saveoutFileName)
    {
      DatabaseSnapshot snapshot = GetDatabaseSnapshot(databaseSnapshotUsage.CreateNew);
      RAMDatabaseDataLists snapshotData = snapshot.EnterSnapshot();

      if (!snapshotData.pokerHandsSnapshot.ContainsKey(tableId))
        throw new Exception("RAM database contains no data for the provided tableId.");

      long[] handIds = snapshotData.pokerHandsSnapshot[tableId].Keys.ToArray();

      using (System.IO.StreamWriter sw = new System.IO.StreamWriter(saveoutFileName, false))
      {
        sw.WriteLine("RAMDatabase dump for tableId:" + tableId + " at " + DateTime.Now.ToLongDateString() + " " + DateTime.Now.ToLongTimeString() + ".\n");

        for (int i = 0; i < handIds.Length; i++)
        {
          databaseCache.pokerHand thisPokerHand = snapshotData.pokerHandsSnapshot[tableId][handIds[i]];
          Dictionary<long, databaseCache.holeCard> playerHoleCards = snapshotData.holeCardsSnapshot[handIds[i]];
          long[] handPlayerIds = playerHoleCards.Keys.ToArray();

          sw.WriteLine(
              "###########################################\n" +
              "###########################################\n" +
              "Starting hand " + handIds[i] + " with " + handPlayerIds.Length + " players:\n");

          //Write out hole cards
          for (int j = 0; j < handPlayerIds.Length; j++)
            sw.WriteLine(databaseQueries.playerDetailsByPlayerId(handPlayerIds[j]).PlayerName + " - [" + (Card)playerHoleCards[handPlayerIds[j]].HoleCard1 + ", " + (Card)playerHoleCards[handPlayerIds[j]].HoleCard2 + "]");

          sw.WriteLine("");

          List<databaseCache.handAction> handHandActions = snapshotData.handActionsSnapshot[handIds[i]];
          for (int j = 0; j < handHandActions.Count; j++)
          {
            if (handHandActions[j].ActionTypeId == (byte)PokerAction.DealFlop)
              sw.WriteLine("\nFlop Dealt - [" + (Card)thisPokerHand.TableCard1 + ", " + (Card)thisPokerHand.TableCard2 + ", " + (Card)thisPokerHand.TableCard3 + "]");
            else if (handHandActions[j].ActionTypeId == (byte)PokerAction.DealTurn)
              sw.WriteLine("\nTurn Dealt - [" + (Card)thisPokerHand.TableCard1 + ", " + (Card)thisPokerHand.TableCard2 + ", " + (Card)thisPokerHand.TableCard3 + ", " + (Card)thisPokerHand.TableCard4 + "]");
            else if (handHandActions[j].ActionTypeId == (byte)PokerAction.DealRiver)
              sw.WriteLine("\nRiver Dealt - [" + (Card)thisPokerHand.TableCard1 + ", " + (Card)thisPokerHand.TableCard2 + ", " + (Card)thisPokerHand.TableCard3 + ", " + (Card)thisPokerHand.TableCard4 + ", " + (Card)thisPokerHand.TableCard5 + "]");
            else if (handHandActions[j].ActionValue == 0)
              sw.WriteLine(databaseQueries.playerDetailsByPlayerId(handHandActions[j].PlayerId).PlayerName + " - " + (PokerAction)handHandActions[j].ActionTypeId);
            else
              sw.WriteLine(databaseQueries.playerDetailsByPlayerId(handHandActions[j].PlayerId).PlayerName + " - " + (PokerAction)handHandActions[j].ActionTypeId + " " + handHandActions[j].ActionValue);
          }

          sw.WriteLine("\n");
        }
      }

    }
  }
}
