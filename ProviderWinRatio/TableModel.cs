using System;
using System.Collections.Generic;
using System.Linq;
using PokerBot.Database;
using PokerBot.Definitions;

namespace PokerBot.AI.ProviderWinRatio
{
  public partial class WinRatioProvider
  {

    public partial class TableModel
    {
      long tableId;
      byte numPlayers;
      //Dictionary<long, PlayerModel> playersOld;
      Dictionary<long, PlayerModelFixed> playersNew;

      long lastHandIdConsidered = -1;
      long lastActionIdConsidered = -1;
      bool wasRaisedPot;
      HandState wasHandState;
      int lastNumberActivePlayers;
      WinRatioProvider wrProv;

      public TableModel(databaseCache cache, WinRatioProvider wrProv, bool useAllPlayersCards)
      {
        this.tableId = cache.TableId;
        this.numPlayers = cache.NumSeats;
        this.wrProv = wrProv;

        //playersOld = new Dictionary<long, PlayerModel>(numPlayers);
        playersNew = new Dictionary<long, PlayerModelFixed>(numPlayers);

        var satDownPositions = cache.getSatDownPositions();

        for (byte i = 0; i < numPlayers; i++)
        {
          if (satDownPositions.Contains(i))
          {
            //playersOld.Add(cache.getPlayerId(i), new PlayerModel(tableId, cache.getCurrentHandId(), cache.getPlayerId(i), wrProv));
            playersNew.Add(cache.getPlayerId(i), new PlayerModelFixed(tableId, cache.BigBlind, cache.getCurrentHandId(), cache.getPlayerId(i), wrProv));
          }
        }
      }

      public void UpdatePlayerModels(databaseCache cache)
      {
        //First get hand details
        var handDetails = cache.getCurrentHandDetails();

        //If hand id has changed since last time then some reseting is needed
        if (cache.getCurrentHandId() != lastHandIdConsidered)
        {
          //reset variables
          lastHandIdConsidered = cache.getCurrentHandId();
          wasRaisedPot = false;
          wasHandState = HandState.PreFlop;
          lastActionIdConsidered = -1;

          //get keys in players dictionary and players in cache who are sat in
          //long[] keys = playersOld.Keys.ToArray();
          long[] satInPlayerIds = cache.getSatInPlayerIds();

          lastNumberActivePlayers = cache.PlayerIdsStartedHand().Length;

          //Remove from players all items that don't correspond to sat in players
          //long[] toRemove = keys.Except(satInPlayerIds).ToArray();

          //for (byte i = 0; i < toRemove.Length; i++)
          //    playersOld.Remove(toRemove[i]);

          //Go through each seat
          //for (int i = 0; i < satInPlayerIds.Length; i++)
          //{
          //    //Get player id for person in seat
          //    long pid = satInPlayerIds[i];

          //    //and the player is not in dictionary add them, otherwise reset their entry
          //    if (!playersOld.ContainsKey(pid))
          //        playersOld.Add(pid, new PlayerModel(tableId, lastHandIdConsidered, pid, wrProv));
          //    else
          //        playersOld[pid].ResetProbsToDefault(lastHandIdConsidered);

          //    playersOld[pid].UpdateCardsWinPercentages(new Card[] { }, lastNumberActivePlayers);
          //}

          //get keys in players dictionary and players in cache who are sat in
          long[] keys = playersNew.Keys.ToArray();

          //Remove from players all items that don't correspond to sat in players
          long[] toRemove = keys.Except(satInPlayerIds).ToArray();

          for (byte i = 0; i < toRemove.Length; i++)
            playersNew.Remove(toRemove[i]);

          //Go through each seat
          for (int i = 0; i < satInPlayerIds.Length; i++)
          {
            //Get player id for person in seat
            long pid = satInPlayerIds[i];

            //and the player is not in dictionary add them, otherwise reset their entry
            if (!playersNew.ContainsKey(pid))
              playersNew.Add(pid, new PlayerModelFixed(tableId, cache.BigBlind, lastHandIdConsidered, pid, wrProv));
            else
              playersNew[pid].ResetProbsToDefault(lastHandIdConsidered);

            playersNew[pid].UpdateCardsWinPercentages(new Card[] { }, lastNumberActivePlayers);
          }
        }

        //Setup cards array based on cards that were present at end of last update
        Card[] cards = new Card[0];

        switch (wasHandState)
        {
          case HandState.Flop:
            cards = new Card[] { (Card)(handDetails.tableCard1), (Card)(handDetails.tableCard2), (Card)(handDetails.tableCard3) };
            break;
          case HandState.Turn:
            cards = new Card[] { (Card)(handDetails.tableCard1), (Card)(handDetails.tableCard2), (Card)(handDetails.tableCard3), (Card)(handDetails.tableCard4) };
            break;
          case HandState.River:
            cards = new Card[] { (Card)(handDetails.tableCard1), (Card)(handDetails.tableCard2), (Card)(handDetails.tableCard3), (Card)(handDetails.tableCard4), (Card)(handDetails.tableCard5) };
            break;
        }

        //Get all dealer and betting actions since last update
        var handActions = cache.getHandActions(lastHandIdConsidered);

        handActions =
            (from a in handActions
             where a.localIndex > lastActionIdConsidered && (a.actionType == PokerAction.Check ||
                                                              a.actionType == PokerAction.Call ||
                                                              a.actionType == PokerAction.Raise ||
                                                              a.actionType == PokerAction.Fold ||
                                                              a.actionType == PokerAction.DealFlop ||
                                                              a.actionType == PokerAction.DealTurn ||
                                                              a.actionType == PokerAction.DealRiver)
             orderby a.localIndex
             select a).ToArray();

        //Get a list of active players by the end of the hand actions above
        var activePlayers = cache.getActivePlayerIds();

        //Now go through each new hand action
        foreach (var handAction in handActions)
        {
          //if the action is a dealer action then update hand state, change raised pot flag and for each active player update card probs change after deal
          if (handAction.actionType == PokerAction.DealFlop)
          {
            wasHandState = HandState.Flop;
            wasRaisedPot = false;
            cards = new Card[] { (Card)(handDetails.tableCard1), (Card)(handDetails.tableCard2), (Card)(handDetails.tableCard3) };

            foreach (var player in activePlayers)
            {
              //playersOld[player].UpdateProbsAfterCardDealt((Card)(handDetails.tableCard1));
              //playersOld[player].UpdateProbsAfterCardDealt((Card)(handDetails.tableCard2));
              //playersOld[player].UpdateProbsAfterCardDealt((Card)(handDetails.tableCard3));

              playersNew[player].UpdateProbsAfterCardDealt((Card)(handDetails.tableCard1));
              playersNew[player].UpdateProbsAfterCardDealt((Card)(handDetails.tableCard2));
              playersNew[player].UpdateProbsAfterCardDealt((Card)(handDetails.tableCard3));
            }

            continue;
          }
          else if (handAction.actionType == PokerAction.DealTurn)
          {
            wasHandState = HandState.Turn;
            wasRaisedPot = false;
            cards = new Card[] { (Card)(handDetails.tableCard1), (Card)(handDetails.tableCard2), (Card)(handDetails.tableCard3), (Card)(handDetails.tableCard4) };

            foreach (var player in activePlayers)
            {
              //playersOld[player].UpdateProbsAfterCardDealt((Card)(handDetails.tableCard4));
              playersNew[player].UpdateProbsAfterCardDealt((Card)(handDetails.tableCard4));
            }

            continue;
          }
          else if (handAction.actionType == PokerAction.DealRiver)
          {
            wasHandState = HandState.River;
            wasRaisedPot = false;
            cards = new Card[] { (Card)(handDetails.tableCard1), (Card)(handDetails.tableCard2), (Card)(handDetails.tableCard3), (Card)(handDetails.tableCard4), (Card)(handDetails.tableCard5) };

            foreach (var player in activePlayers)
            {
              //playersOld[player].UpdateProbsAfterCardDealt((Card)(handDetails.tableCard5));
              playersNew[player].UpdateProbsAfterCardDealt((Card)(handDetails.tableCard5));
            }

            continue;
          }
          else if (handAction.actionType == PokerAction.Fold)
          {
            //if a player has folded set all their probabilities of having cards to zero and reduce number of active players
            //if (playersOld.ContainsKey(handAction.playerId))
            //    playersOld[handAction.playerId].SetAllProbsToZeroOnFold();

            if (playersNew.ContainsKey(handAction.playerId))
              playersNew[handAction.playerId].SetAllProbsToZeroOnFold();

            lastNumberActivePlayers--;
            continue;
          }

          //otherwise we're dealing with a betting action so they should be in active players
          if (activePlayers.Contains(handAction.playerId))
          {
            //Update win percentages and indices in player model and update card probs based on action
            //playersOld[handAction.playerId].UpdateCardsWinPercentages(cards, lastNumberActivePlayers);
            playersNew[handAction.playerId].UpdateCardsWinPercentages(cards, lastNumberActivePlayers);

            decimal ra = 0, ca = 0, pa = 0;
            double dd;

            if (handAction.actionType == PokerAction.Raise)
              ra = cache.getCurrentRoundLastRaiseAmount(handAction.localIndex + 1L);

            if (wasRaisedPot)
              ca = cache.getMinimumPlayAmount(handAction.localIndex) - cache.getPlayerCurrentRoundBetAmount(handAction.playerId, handAction.localIndex);

            pa = cache.getPotUpToActionId(handAction.localIndex);
            dd = (cache.getActivePlayerDistanceToDealer(handAction.playerId, handAction.localIndex) - 1.0) / (cache.getActivePositions(handAction.localIndex).Length - 1.0);

            //playersOld[handAction.playerId].UpdateCardProbsBasedOnAction(handAction.actionType, wasHandState, ca, ra, pa, wasRaisedPot, dd < 0.5);
            playersNew[handAction.playerId].UpdateCardProbsBasedOnAction(handAction.actionType, wasHandState, ca, ra, pa, wasRaisedPot, dd < 0.5);
          }

          //Finally if the action was a raise we now have a raised pot
          if (handAction.actionType == PokerAction.Raise)
            wasRaisedPot = true;
        }

        //for (int i = 0; i < activePlayers.Length; i++)
        //    playersOld[activePlayers[i]].UpdateCardsWinPercentages(cards, activePlayers.Length);

        for (int i = 0; i < activePlayers.Length; i++)
          playersNew[activePlayers[i]].UpdateCardsWinPercentages(cards, activePlayers.Length);

        //finally update last action considered and last number players
        if (handActions.Count() > 0)
          lastActionIdConsidered = handActions.Last().localIndex;

        lastNumberActivePlayers = activePlayers.Length;
      }

      //public double GetProbAnyPlayerHasBetterHandOld(databaseCache cache, long playerId, ushort winPercentage)
      //{
      //    //return 0;

      //    double prob = 1.0;
      //    var playerCards = cache.getPlayerHoleCards(playerId);

      //    //for each player that isn't us find out probability that we have better cards than them and multiply together
      //    foreach (var player in playersOld)
      //    {
      //        if (player.Key != playerId)
      //            prob *= (1.0 - player.Value.GetProbHaveBetterWinPercentageThan(winPercentage, (Card)playerCards.holeCard1, (Card)playerCards.holeCard2));
      //    }

      //    //return 1 - prob that we have better cards than all players
      //    prob = Math.Round(1.0 - prob, 4);

      //    if (prob >= 0 && prob <= 1)
      //        return prob;

      //    if (prob > 1.0 && Math.Round(prob, 4) == 1)
      //        return 1;

      //    if (prob < 0 && Math.Round(prob, 4) == 0)
      //        return 0;

      //    throw new Exception("Cannot have probability that gets us here!!!");
      //}

      public double GetProbAnyPlayerHasBetterHandNew(databaseCache cache, long playerId, ushort winPercentage)
      {
        //return 0;

        double prob = 1.0;
        var playerCards = cache.getPlayerHoleCards(playerId);

        //for each player that isn't us find out probability that we have better cards than them and multiply together
        foreach (var player in playersNew)
        {
          if (player.Key != playerId)
            prob *= (1.0 - player.Value.GetProbHaveBetterWinPercentageThan(winPercentage, (Card)playerCards.holeCard1, (Card)playerCards.holeCard2));
        }

        //return 1 - prob that we have better cards than all players
        prob = Math.Round(1.0 - prob, 4);

        if (prob >= 0 && prob <= 1)
          return prob;

        if (prob > 1.0 && Math.Round(prob, 4) == 1)
          return 1;

        if (prob < 0 && Math.Round(prob, 4) == 0)
          return 0;

        throw new Exception("Cannot have probability that gets us here!!!");
      }

      public void GetRaiseCallStealAmounts(databaseCache cache, long playerId, out decimal call, out decimal steel, out double probStealSuccess, out double probCallSuccess)
      {
        var playersInHand = cache.getActivePlayerIds();
        call = decimal.MinValue;
        steel = decimal.MinValue;
        decimal callTemp, stealTemp;
        double stealProbTemp, callProbTemp;
        probStealSuccess = 0;
        probCallSuccess = 0;

        HandState stage;
        var details = cache.getCurrentHandDetails();
        if (details.tableCard1 == 0)
          stage = HandState.PreFlop;
        else if (details.tableCard4 == 0)
          stage = HandState.Flop;
        else if (details.tableCard5 == 0)
          stage = HandState.Turn;
        else
          stage = HandState.River;

        var playerCards = cache.getPlayerHoleCards(playerId);
        decimal minExtraRaise = (cache.getCurrentRoundLastRaiseAmount() == 0 ? cache.BigBlind : cache.getCurrentRoundLastRaiseAmount());
        decimal maxExtraRaise = cache.getPlayerStack(playerId) - cache.getMinimumPlayAmount() + cache.getPlayerCurrentRoundBetAmount(playerId);

        double numberOpponents = cache.getActivePositions().Length - cache.getAllInPositions().Length - 1;

        if (playersInHand.Length > cache.getAllInPositions().Length + 1 && maxExtraRaise > 0)
        {

          foreach (var player in playersNew)
          {
            if (player.Key != playerId && playersInHand.Contains(player.Key))
            {
              if (cache.getAllInPositions().Contains(cache.getPlayerPosition(player.Key)))
                continue;

              var dd = (cache.getActivePlayerDistanceToDealer(player.Key) - 1.0) / (cache.getActivePositions().Length - 1.0);

              player.Value.GetRaiseCallSteal(stage, (Card)playerCards.holeCard1, (Card)playerCards.holeCard2, dd < 0.5,
                  details.potValue, minExtraRaise, maxExtraRaise, Math.Pow(0.5, 1.0 / numberOpponents), Math.Pow(0.75, 1.0 / numberOpponents), out callTemp, out stealTemp, out stealProbTemp, out callProbTemp);

              if (callTemp > call)
              {
                call = callTemp;
                probCallSuccess = callProbTemp;
              }

              if (stealTemp > steel)
              {
                steel = stealTemp;
                probStealSuccess = stealProbTemp;
              }
            }
          }

          probStealSuccess = Math.Pow(probStealSuccess, numberOpponents);
          probCallSuccess = Math.Pow(probCallSuccess, numberOpponents);
        }
        else
        {
          call = minExtraRaise;
          steel = minExtraRaise;
          probStealSuccess = 0;
          probCallSuccess = 0;
        }

        decimal currentMinPlayAmount = cache.getMinimumPlayAmount();
        decimal minRaise = minExtraRaise + currentMinPlayAmount;
        decimal maxRaise = maxExtraRaise + currentMinPlayAmount;

        call += currentMinPlayAmount;
        steel += currentMinPlayAmount;

        decimal callChange = call * 0.2m * (decimal)(wrProv.randomGen.NextDouble() - 0.5);
        decimal stealChange = steel * 0.2m * (decimal)(wrProv.randomGen.NextDouble() - 0.5);

        if (Math.Abs(callChange) < cache.LittleBlind)
          callChange = cache.LittleBlind * callChange / Math.Abs(callChange);

        if (Math.Abs(stealChange) < cache.LittleBlind)
          stealChange = cache.LittleBlind * stealChange / Math.Abs(stealChange);

        call += callChange;
        call = Math.Round(call / cache.LittleBlind, 0, MidpointRounding.AwayFromZero) * cache.LittleBlind;

        if (call < minRaise)
          call = minRaise;
        if (call > maxRaise)
          call = maxRaise;

        steel += stealChange;
        steel = Math.Round(steel / cache.LittleBlind, 0, MidpointRounding.AwayFromZero) * cache.LittleBlind;

        if (steel < minRaise)
          steel = minRaise;
        if (steel > maxRaise)
          steel = maxRaise;
      }

      //public double GetAveragePercievedProbPlayerHasBetterHand(long playerId, databaseCache cache)
      //{
      //    var botPlayer = playersOld[playerId];

      //    var handDetails = cache.getCurrentHandDetails();
      //    var activePlayers = cache.getActivePlayerIds();
      //    List<Card> tableCards = new List<Card>();

      //    if (handDetails.tableCard1 != 0)
      //    {
      //        tableCards.Add((Card)handDetails.tableCard1);
      //        tableCards.Add((Card)handDetails.tableCard2);
      //        tableCards.Add((Card)handDetails.tableCard3);

      //        if (handDetails.tableCard4 != 0)
      //        {
      //            tableCards.Add((Card)handDetails.tableCard4);

      //            if (handDetails.tableCard5 != 0)
      //            {
      //                tableCards.Add((Card)handDetails.tableCard5);
      //            }
      //        }
      //    }

      //    var tableCardsArray = tableCards.ToArray();

      //    botPlayer.UpdateCardsWinPercentages(tableCardsArray, activePlayers.Length);
      //    double prob = 0;
      //    for (int i = 0; i < activePlayers.Length; i++)
      //    {
      //        if (activePlayers[i] == playerId)
      //            continue;

      //        prob += botPlayer.GetPerceivedChanceHasBetterHandThanOtherPlayer(playersOld[activePlayers[i]]) / (activePlayers.Length - 1);
      //    }

      //    if (prob >= 0 && prob <= 1)
      //        return prob;

      //    if (prob > 1.0 && Math.Round(prob, 4) == 1)
      //        return 1;

      //    if (prob < 0 && Math.Round(prob, 4) == 0)
      //        return 0;

      //    throw new Exception("Cannot have probability that gets us here!!!");
      //}
    }
  }
}
