using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using PokerBot.Definitions;
using PokerBot.AI.InfoProviders;
using System.IO;
using System.Diagnostics;

namespace PokerBot.AI
{
  internal class CheatV1 : AIBase
  {
    bool allOpponentsCheaters = false;
    RequestedInfoKey fastUpdateKey;

    internal CheatV1(AIRandomControl aiRandomControl)
        : base(aiRandomControl)
    {
      aiType = AIGeneration.CheatV1;

      #region normal key
      //Setup this AI's specific update key
      specificUpdateKey = new RequestedInfoKey(false);
      specificUpdateKey.SetInfoTypeRequired(InfoType.CP_HoleCardsMatchedPlayability);

      specificUpdateKey.SetInfoTypeRequired(InfoType.GP_DealerDistance_Byte);
      specificUpdateKey.SetInfoTypeRequired(InfoType.GP_NumActivePlayers_Byte);
      specificUpdateKey.SetInfoTypeRequired(InfoType.GP_NumUnactedPlayers_Byte);

      specificUpdateKey.SetInfoTypeRequired(InfoType.BP_PlayerMoneyInPot_Decimal);
      specificUpdateKey.SetInfoTypeRequired(InfoType.BP_PlayerHandStartingStackAmount_Decimal);
      specificUpdateKey.SetInfoTypeRequired(InfoType.BP_PlayerBetAmountCurrentRound_Decimal);
      specificUpdateKey.SetInfoTypeRequired(InfoType.BP_TotalPotAmount_Decimal);
      specificUpdateKey.SetInfoTypeRequired(InfoType.BP_MinimumPlayAmount_Decimal);
      specificUpdateKey.SetInfoTypeRequired(InfoType.BP_TotalNumCalls_Byte);

      specificUpdateKey.SetInfoTypeRequired(InfoType.WR_RaiseToCallStealSuccessProb);
      specificUpdateKey.SetInfoTypeRequired(InfoType.WR_RaiseToStealSuccessProb);

      specificUpdateKey.SetInfoTypeRequired(InfoType.AP_AvgLiveOppCurrentRoundAggr_Double);
      #endregion

      #region fast key
      fastUpdateKey = new RequestedInfoKey(false);

      fastUpdateKey.SetInfoTypeRequired(InfoType.CP_HoleCardsMatchedPlayability);

      fastUpdateKey.SetInfoTypeRequired(InfoType.GP_DealerDistance_Byte);
      fastUpdateKey.SetInfoTypeRequired(InfoType.GP_NumActivePlayers_Byte);
      fastUpdateKey.SetInfoTypeRequired(InfoType.GP_NumUnactedPlayers_Byte);

      fastUpdateKey.SetInfoTypeRequired(InfoType.BP_PlayerMoneyInPot_Decimal);
      fastUpdateKey.SetInfoTypeRequired(InfoType.BP_PlayerHandStartingStackAmount_Decimal);
      fastUpdateKey.SetInfoTypeRequired(InfoType.BP_PlayerBetAmountCurrentRound_Decimal);
      fastUpdateKey.SetInfoTypeRequired(InfoType.BP_TotalPotAmount_Decimal);
      fastUpdateKey.SetInfoTypeRequired(InfoType.BP_MinimumPlayAmount_Decimal);
      fastUpdateKey.SetInfoTypeRequired(InfoType.BP_TotalNumCalls_Byte);

      fastUpdateKey.SetInfoTypeRequired(InfoType.AP_AvgLiveOppCurrentRoundAggr_Double);
      #endregion

      //THis AI requires the HandRankData
      //We need to confirm we have the handRank data ready
      if (!HandRank.HandRankLoaded)
      {
        try
        {
          //If we have not already loaded the handRanks data then we check to make sure the file exists
          if (!File.Exists(Environment.GetEnvironmentVariable("HandRanksLocation")))
            throw new Exception("Unable to locate handRank file.");

          //If the file existed it will be loaded the first time we try to use it
        }
        catch (Exception ex)
        {
          throw new Exception("Unable to start CheatV1 AI. HandRanks file must be loaded before starting AI manager or be located from the registry. " + ex.ToString());
        }
      }
    }

    protected override RequestedInfoKey GetUpdateKeyOrPreDecision()
    {
      //If we are preflop and we have a hole card playability of 0 then we need go no further
      if (currentDecision.Cache.getBettingRound() == 0 && CardsProvider.GetPlayerHoleCardPlayability(currentDecision.Cache.getPlayerHoleCards(currentDecision.PlayerId), "CheatV1") == 0)
        currentDecision.SetDecision(new Play(PokerAction.Fold, 0, 0, currentDecision.Cache.getCurrentHandId(), currentDecision.PlayerId, decisionLogStr, 0));
      else
      {
        //If all of our opponents are cheaters then we return the fast update key, otherwise just normal
        long[] activePlayerIds = currentDecision.Cache.getActivePlayerIds();
        string aiConfigStr;
        AIGeneration serverType;

        for (int i = 0; i < activePlayerIds.Length; i++)
        {
          CacheTracker.Instance.PlayerAiConfig(activePlayerIds[i], currentDecision.Cache.TableId, out serverType, out aiConfigStr);
          if (serverType != AIGeneration.CheatV1)
          {
            //We have detected a non cheater so continue with the normal decision process
            allOpponentsCheaters = false;
            return specificUpdateKey;
          }
        }

        allOpponentsCheaters = true;

        if (currentDecision.Cache.getMinimumPlayAmount() > 0)
          currentDecision.SetDecision(new Play(PokerAction.Fold, 0, 0, currentDecision.Cache.getCurrentHandId(), currentDecision.PlayerId, decisionLogStr, 0));
        else
          currentDecision.SetDecision(new Play(PokerAction.Raise, currentDecision.Cache.BigBlind, 0, currentDecision.Cache.getCurrentHandId(), currentDecision.PlayerId, decisionLogStr, 0));
      }

      return fastUpdateKey;
    }

    protected override Dictionary<InfoType, string> GetInfoUpdateConfigs()
    {
      defaultInfoTypeUpdateConfigs = new Dictionary<InfoType, string>();
      defaultInfoTypeUpdateConfigs.Add(InfoType.CP_HoleCardsMatchedPlayability, "CheatV1");

      return defaultInfoTypeUpdateConfigs;
    }

    protected override Play GetDecision()
    {
      //If we are only playing cheaters we can just fold here
      if (allOpponentsCheaters)
        throw new Exception("This should have been handled by the predecision step in GetUpdateKey().");

      //If we are on the river then probWin can be 1 or 0
      decimal probWin = 0;
      bool topHand = false;
      byte currentBettingRound = currentDecision.Cache.getBettingRound();

      //Some values need to be moved the top because they are used in multiple places
      decimal currentRoundMinPlayCallAmount = infoStore.GetInfoValue(InfoType.BP_MinimumPlayAmount_Decimal);
      decimal additionalCallAmount = currentRoundMinPlayCallAmount - infoStore.GetInfoValue(InfoType.BP_PlayerBetAmountCurrentRound_Decimal);
      decimal currentPotAmount = infoStore.GetInfoValue(InfoType.BP_TotalPotAmount_Decimal);
      decimal remainingPlayerStack = currentDecision.Cache.getPlayerStack(currentDecision.PlayerId);
      long[] activePlayerIds = currentDecision.Cache.getActivePlayerIds();

      var ourCards = currentDecision.Cache.getPlayerHoleCards(currentDecision.PlayerId);

      #region Special ProbWin
      //Preflop we don't really need to cheat
      if (currentBettingRound == 0)
      {
        //probWin = 1.0m - infoStore.GetInfoValue(InfoType.WR_ProbOpponentHasBetterWRFIXED);
        //Alternative preflop probWin
        double winRatioNotUsed, winPercentageUs, winPercentageOpponent, bestWinPercentage = 0, totalWinPercentages = 0;

        ProviderWinRatio.WinRatioProvider.FirstInstance.GetWinRatioExt((int)infoStore.GetInfoValue(InfoType.GP_NumActivePlayers_Byte), ourCards.holeCard1, ourCards.holeCard2, 0, 0, 0, 0, 0, out winRatioNotUsed, out winPercentageUs);

        for (int i = 0; i < activePlayerIds.Length; i++)
        {
          var playerCards = currentDecision.Cache.getPlayerHoleCards(activePlayerIds[i]);
          ProviderWinRatio.WinRatioProvider.FirstInstance.GetWinRatioExt((int)infoStore.GetInfoValue(InfoType.GP_NumActivePlayers_Byte), playerCards.holeCard1, playerCards.holeCard2, 0, 0, 0, 0, 0, out winRatioNotUsed, out winPercentageOpponent);

          totalWinPercentages += winPercentageOpponent;

          if (winPercentageOpponent > bestWinPercentage)
            bestWinPercentage = winPercentageOpponent;
        }

        probWin = (decimal)(winPercentageUs * (100 / totalWinPercentages)) / 100.0m;
        topHand = (winPercentageUs >= bestWinPercentage * 0.9);
      }
      else
      {
        #region postFlop Calculation
        long[] opponentPlayerIds = activePlayerIds.Except(new long[] { currentDecision.PlayerId }).ToArray();
        var currentHandDetails = currentDecision.Cache.getCurrentHandDetails();

        //If we are at the river we just determine if we have won or not
        if (currentBettingRound == 3)
        {
          #region River
          //if (InfoProviderBase.CurrentJob == null)
          int ourHandRank = HandRank.GetHandRank((Card)ourCards.holeCard1, (Card)ourCards.holeCard2, (Card)currentHandDetails.tableCard1, (Card)currentHandDetails.tableCard2, (Card)currentHandDetails.tableCard3, (Card)currentHandDetails.tableCard4, (Card)currentHandDetails.tableCard5);
          //else
          //    ourHandRank = InfoProviderBase.CurrentJob.HoleCardValues[InfoProviderBase.CurrentJob.NumHandsCompleted][currentDecision.Cache.getPlayerPosition(currentDecision.PlayerId)];

          int bestHandRank = -int.MaxValue;
          //long bestHandPlayerId;

          for (int i = 0; i < opponentPlayerIds.Length; i++)
          {
            var playerCards = currentDecision.Cache.getPlayerHoleCards(opponentPlayerIds[i]);

            //Determine which playerhas the best hand rank
            //if (InfoProviderBase.CurrentJob == null)
            int currentHandRank = HandRank.GetHandRank((Card)playerCards.holeCard1, (Card)playerCards.holeCard2, (Card)currentHandDetails.tableCard1, (Card)currentHandDetails.tableCard2, (Card)currentHandDetails.tableCard3, (Card)currentHandDetails.tableCard4, (Card)currentHandDetails.tableCard5);
            //else
            //    currentHandRank = InfoProviderBase.CurrentJob.HoleCardValues[InfoProviderBase.CurrentJob.NumHandsCompleted][currentDecision.Cache.getPlayerPosition(opponentPlayerIds[i])];

            if (currentHandRank > bestHandRank)
            {
              bestHandRank = currentHandRank;
              //bestHandPlayerId = opponentPlayerIds[i];
            }
          }

          if (ourHandRank >= bestHandRank)
            probWin = 1;
          #endregion
        }
        else
        {
          //Get the tableCards
          Card[] tableCards;
          int winCount = 0, looseCount = 0, drawCount = 0;

          switch (currentBettingRound)
          {
            case 1:
              tableCards = new Card[] { (Card)currentHandDetails.tableCard1, (Card)currentHandDetails.tableCard2, (Card)currentHandDetails.tableCard3, Card.NoCard, Card.NoCard };
              break;
            case 2:
              tableCards = new Card[] { (Card)currentHandDetails.tableCard1, (Card)currentHandDetails.tableCard2, (Card)currentHandDetails.tableCard3, (Card)currentHandDetails.tableCard4, Card.NoCard };
              break;
            default:
              throw new Exception("Impossible!!!");
          }

          byte[] knownCards = currentDecision.Cache.KnownCurrentHandCards();
          byte[] allCards = new byte[] { 1, 2, 3, 4, 5, 6, 7, 8, 9, 10, 11, 12, 13, 14, 15, 16, 17, 18, 19, 20, 21, 22, 23, 24, 25, 26, 27, 28, 29, 30, 31, 32, 33, 34, 35, 36, 37, 38, 39, 40, 41, 42, 43, 44, 45, 46, 47, 48, 49, 50, 51, 52 };

          byte[] cardsToTry = allCards.Except(knownCards).ToArray();

          Dictionary<long, PokerBot.Database.databaseCache.playerCards> allPlayerCards = new Dictionary<long, PokerBot.Database.databaseCache.playerCards>();
          for (int k = 0; k < opponentPlayerIds.Length; k++)
            allPlayerCards.Add(opponentPlayerIds[k], currentDecision.Cache.getPlayerHoleCards(opponentPlayerIds[k]));

          if (currentBettingRound == 1)
          {
            #region Flop
            //On the flop we need to calculate wins for the remaining two cards
            for (int i = 0; i < cardsToTry.Length; i++)
            {
              for (int j = 0; j < cardsToTry.Length; j++)
              {
                if (i != j)
                {
                  tableCards[3] = (Card)cardsToTry[i];
                  tableCards[4] = (Card)cardsToTry[j];

                  //Loop over all players and see if we win or loose
                  //int ourWinIndex = ProviderWinRatio.WinRatioProvider.FirstInstance.GetWinPercentageIndexExt((Card)ourCards.holeCard1, (Card)ourCards.holeCard2, tableCards, opponentPlayerIds.Length+1);

                  int ourHandRank = HandRank.GetHandRank((Card)ourCards.holeCard1, (Card)ourCards.holeCard2, tableCards);

                  //First assume we will win then look at the opponents
                  //winCount++;

                  bool drawDetected = false, looseDetected = false;
                  for (int k = 0; k < opponentPlayerIds.Length; k++)
                  {
                    //var playerCards = currentDecision.Cache.getPlayerHoleCards(opponentPlayerIds[k]);
                    //int winPercentageIndex = ProviderWinRatio.WinRatioProvider.FirstInstance.GetWinPercentageIndexExt((Card)playerCards.holeCard1, (Card)playerCards.holeCard2, tableCards, opponentPlayerIds.Length+1);
                    int oppHandRank = HandRank.GetHandRank((Card)allPlayerCards[opponentPlayerIds[k]].holeCard1, (Card)allPlayerCards[opponentPlayerIds[k]].holeCard2, tableCards);

                    if (oppHandRank > ourHandRank)
                    {
                      drawDetected = false;
                      looseDetected = true;

                      //As soon as we are beatn we can correct
                      //winCount--;
                      //looseCount++;
                      break;
                    }
                    else if (oppHandRank == ourHandRank)
                      drawDetected = true;
                  }

                  if (!looseDetected && !drawDetected)
                    winCount++;
                  else if (!drawDetected)
                    looseCount++;
                  else
                    drawCount++;
                }
              }
            }
            #endregion
          }
          else if (currentBettingRound == 2)
          {
            #region Turn
            //On the turn we just need to look at river cards
            for (int i = 0; i < cardsToTry.Length; i++)
            {
              //Pull out the card we are going to try
              tableCards[4] = (Card)cardsToTry[i];

              //Loop over all players and see if we win or loose
              //int ourWinIndex = ProviderWinRatio.WinRatioProvider.FirstInstance.GetWinPercentageIndexExt((Card)ourCards.holeCard1, (Card)ourCards.holeCard2, tableCards, opponentPlayerIds.Length+1);
              int ourHandRank = HandRank.GetHandRank((Card)ourCards.holeCard1, (Card)ourCards.holeCard2, tableCards);

              //First assume we will win then look at the opponents
              //winCount++;

              bool drawDetected = false, looseDetected = false;
              for (int k = 0; k < opponentPlayerIds.Length; k++)
              {
                //var playerCards = currentDecision.Cache.getPlayerHoleCards(opponentPlayerIds[k]);
                //int winPercentageIndex = ProviderWinRatio.WinRatioProvider.FirstInstance.GetWinPercentageIndexExt((Card)playerCards.holeCard1, (Card)playerCards.holeCard2, tableCards, opponentPlayerIds.Length+1);
                int oppHandRank = HandRank.GetHandRank((Card)allPlayerCards[opponentPlayerIds[k]].holeCard1, (Card)allPlayerCards[opponentPlayerIds[k]].holeCard2, tableCards);

                if (oppHandRank > ourHandRank)
                {
                  //As soon as we are beatn we can correct
                  drawDetected = false;
                  looseDetected = true;

                  //winCount--;
                  //looseCount++;
                  break;
                }
                else if (oppHandRank == ourHandRank)
                  drawDetected = true;
              }

              if (!looseDetected && !drawDetected)
                winCount++;
              else if (!drawDetected)
                looseCount++;
              else
                drawCount++;
            }
            #endregion
          }
          else
            throw new Exception("Incorrect betting round for this part of the ai.");

          //Calculate probWin
          probWin = (decimal)(winCount + drawCount) / (decimal)(winCount + drawCount + looseCount);
        }
        #endregion
      }
      #endregion

      int decision = 0;

      //Current Round Actions
      List<PokerAction> currentRoundPlayerActions = (from current in currentDecision.Cache.getPlayerCurrentRoundActions(currentDecision.PlayerId)
                                                     where current == PokerAction.Check ||
                                                     current == PokerAction.Call ||
                                                     current == PokerAction.Raise
                                                     select current).ToList();

      PokerAction playerLastActionCurrentRound = PokerAction.NoAction;
      if (currentRoundPlayerActions.Count > 0)
        playerLastActionCurrentRound = currentRoundPlayerActions.Last();

      #region RaiseAmounts
      decimal raiseToCallAmountNew, raiseToStealAmountNew;

      decimal maximumRaiseToAmount = remainingPlayerStack + (decimal)infoStore.GetInfoValue(InfoType.BP_PlayerBetAmountCurrentRound_Decimal);
      decimal lastAdditionalRaiseAmount = currentDecision.Cache.getCurrentRoundLastRaiseAmount();
      if (lastAdditionalRaiseAmount > remainingPlayerStack)
        lastAdditionalRaiseAmount = remainingPlayerStack;

      bool littlePot = (currentPotAmount < RaiseAmountsHelper.SmallPotBBMultiplierLimit * currentDecision.Cache.BigBlind * ((decimal)(randomGen.NextDouble() * 0.2) + 0.80m));

      double randomNumber1 = randomGen.NextDouble();
      double randomNumber2 = randomGen.NextDouble();
      double raiseToCallRandomNumber = Math.Min(randomNumber1, randomNumber2);
      double raiseToStealRandomNumber = Math.Max(randomNumber1, randomNumber2);

      int raiseToCallIndex = -1, raiseToStealIndex = -1;

      //If we have already raised twice this round all amounts are set to all in
      if (currentRoundPlayerActions.Count(entry => entry == PokerAction.Raise) >= 2)
      {
        raiseToCallAmountNew = maximumRaiseToAmount;
        raiseToStealAmountNew = maximumRaiseToAmount;
      }
      else
      {
        #region Select RaiseIndex
        if (currentBettingRound == 0)
        {
          for (byte raiseIndex = 0; raiseIndex < 10; raiseIndex += 1)
          {
            if (raiseToCallIndex < 0 && raiseToCallRandomNumber < RaiseAmountsHelper.PreFlopRaiseBinIdealCDF[raiseIndex])
              raiseToCallIndex = raiseIndex;
            if (raiseToStealRandomNumber < RaiseAmountsHelper.PreFlopRaiseBinIdealCDF[raiseIndex])
            {
              raiseToStealIndex = raiseIndex;
              break;
            }
          }
        }
        else if (currentBettingRound == 1)
        {
          if (littlePot)
          {
            for (byte raiseIndex = 0; raiseIndex < 10; raiseIndex += 1)
            {
              if (raiseToCallIndex < 0 && raiseToCallRandomNumber < RaiseAmountsHelper.FlopRaiseBinIdealSmallCDF[raiseIndex])
                raiseToCallIndex = raiseIndex;
              if (raiseToStealRandomNumber < RaiseAmountsHelper.FlopRaiseBinIdealSmallCDF[raiseIndex])
              {
                raiseToStealIndex = raiseIndex;
                break;
              }
            }
          }
          else
          {
            for (byte raiseIndex = 0; raiseIndex < 10; raiseIndex += 1)
            {
              if (raiseToCallIndex < 0 && raiseToCallRandomNumber < RaiseAmountsHelper.FlopRaiseBinIdealBigCDF[raiseIndex])
                raiseToCallIndex = raiseIndex;
              if (raiseToStealRandomNumber < RaiseAmountsHelper.FlopRaiseBinIdealBigCDF[raiseIndex])
              {
                raiseToStealIndex = raiseIndex;
                break;
              }
            }
          }
        }
        else if (currentBettingRound == 2)
        {
          if (littlePot)
          {
            for (byte raiseIndex = 0; raiseIndex < 10; raiseIndex += 1)
            {
              if (raiseToCallIndex < 0 && raiseToCallRandomNumber < RaiseAmountsHelper.TurnRaiseBinIdealSmallCDF[raiseIndex])
                raiseToCallIndex = raiseIndex;
              if (raiseToStealRandomNumber < RaiseAmountsHelper.TurnRaiseBinIdealSmallCDF[raiseIndex])
              {
                raiseToStealIndex = raiseIndex;
                break;
              }
            }
          }
          else
          {
            for (byte raiseIndex = 0; raiseIndex < 10; raiseIndex += 1)
            {
              if (raiseToCallIndex < 0 && raiseToCallRandomNumber < RaiseAmountsHelper.TurnRaiseBinIdealBigCDF[raiseIndex])
                raiseToCallIndex = raiseIndex;
              if (raiseToStealRandomNumber < RaiseAmountsHelper.TurnRaiseBinIdealBigCDF[raiseIndex])
              {
                raiseToStealIndex = raiseIndex;
                break;
              }
            }
          }
        }
        else if (currentBettingRound == 3)
        {
          if (littlePot)
          {
            for (byte raiseIndex = 0; raiseIndex < 10; raiseIndex += 1)
            {
              if (raiseToCallIndex < 0 && raiseToCallRandomNumber < RaiseAmountsHelper.RiverRaiseBinIdealSmallCDF[raiseIndex])
                raiseToCallIndex = raiseIndex;
              if (raiseToStealRandomNumber < RaiseAmountsHelper.RiverRaiseBinIdealSmallCDF[raiseIndex])
              {
                raiseToStealIndex = raiseIndex;
                break;
              }
            }
          }
          else
          {
            for (byte raiseIndex = 0; raiseIndex < 10; raiseIndex += 1)
            {
              if (raiseToCallIndex < 0 && raiseToCallRandomNumber < RaiseAmountsHelper.RiverRaiseBinIdealBigCDF[raiseIndex])
                raiseToCallIndex = raiseIndex;
              if (raiseToStealRandomNumber < RaiseAmountsHelper.RiverRaiseBinIdealBigCDF[raiseIndex])
              {
                raiseToStealIndex = raiseIndex;
                break;
              }
            }
          }
        }
        else
          throw new Exception("Impossible!");
        #endregion

        decimal additionalRaiseToCallAmountNew = RaiseAmountsHelper.UnscaleAdditionalRaiseAmount(currentPotAmount, currentDecision.Cache.BigBlind, lastAdditionalRaiseAmount, remainingPlayerStack, raiseToCallIndex / 10.0 + 0.05);
        decimal additionalRaiseToStealAmountNew = RaiseAmountsHelper.UnscaleAdditionalRaiseAmount(currentPotAmount, currentDecision.Cache.BigBlind, lastAdditionalRaiseAmount, remainingPlayerStack, raiseToStealIndex / 10.0 + 0.05);

        if (littlePot)
        {
          decimal raiseAmountBlindMultipleCall = (currentRoundMinPlayCallAmount + additionalRaiseToCallAmountNew) / currentDecision.Cache.BigBlind;
          raiseToCallAmountNew = Math.Round(raiseAmountBlindMultipleCall, 0, MidpointRounding.AwayFromZero) * currentDecision.Cache.BigBlind;

          decimal raiseAmountBlindMultipleSteal = (currentRoundMinPlayCallAmount + additionalRaiseToStealAmountNew) / currentDecision.Cache.BigBlind;
          raiseToStealAmountNew = Math.Round(raiseAmountBlindMultipleSteal, 0, MidpointRounding.AwayFromZero) * currentDecision.Cache.BigBlind;
        }
        else
        {
          decimal raiseAmountBlindMultipleCall = (currentRoundMinPlayCallAmount + additionalRaiseToCallAmountNew) / currentDecision.Cache.LittleBlind;
          raiseToCallAmountNew = Math.Round(raiseAmountBlindMultipleCall, 0, MidpointRounding.AwayFromZero) * currentDecision.Cache.LittleBlind;

          decimal raiseAmountBlindMultipleSteal = (currentRoundMinPlayCallAmount + additionalRaiseToStealAmountNew) / currentDecision.Cache.LittleBlind;
          raiseToStealAmountNew = Math.Round(raiseAmountBlindMultipleSteal, 0, MidpointRounding.AwayFromZero) * currentDecision.Cache.LittleBlind;
        }

        //Check for a big raise amount which would be best as going all in
        if (raiseToCallAmountNew > 0.9m * maximumRaiseToAmount)
          raiseToCallAmountNew = maximumRaiseToAmount;
        if (raiseToStealAmountNew > 0.9m * maximumRaiseToAmount)
          raiseToStealAmountNew = maximumRaiseToAmount;
      }
      #endregion RaiseAmounts

      #region EV and CheckPossibilitiy

      decimal bigBlindEVScaleAmount = 20;

      #region CallEV
      //The following EV assumes we can actually call the additionaCallAmount
      //We could cap the additionalCallAmount but we would then also not be able to win the total pot amount ;( again a minor error
      decimal actualCallCheckEV = (currentPotAmount * probWin) - ((1.0m - probWin) * additionalCallAmount);
      decimal scaledCallCheckEV = (actualCallCheckEV / currentDecision.Cache.BigBlind) / bigBlindEVScaleAmount;
      #endregion

      //We can raise if we have more stack than the current additional call amount
      //If we can't raise the raiseEV's get set to the callEV (which may be negative)
      bool raisePossible = (remainingPlayerStack > additionalCallAmount);
      bool checkPossible = (infoStore.GetInfoValue(InfoType.BP_MinimumPlayAmount_Decimal) == infoStore.GetInfoValue(InfoType.BP_PlayerBetAmountCurrentRound_Decimal));

      decimal probAllOpponentFoldRaiseToCall = (currentBettingRound == 0 ? 0 : (allOpponentsCheaters ? 0.45m : infoStore.GetInfoValue(InfoType.WR_RaiseToCallStealSuccessProb)));
      decimal probAllOpponentFoldRaiseToSteal = (currentBettingRound == 0 ? 0 : (allOpponentsCheaters ? 0.67m : infoStore.GetInfoValue(InfoType.WR_RaiseToStealSuccessProb)));

      #region RaiseToCallEV
      decimal scaledRaiseToCallEV = scaledCallCheckEV;
      if (raisePossible)
      {
        decimal potAmountAssumingAllCall = currentPotAmount;

        //If we are preflop then it would be unrealistic to expect everyone to call
        //For Call we assume that if there are 8 players to act after the raise we get 2 calls
        //If there are 4 players to act after the raise we get 1 call
        //This is the same as dividing the number of activePlayers by 4
        if (currentBettingRound == 0)
        {
          //We assume anyone who has already called or raised will call again
          long[] calledRaisedPlayerIds = (from current in currentDecision.Cache.getAllHandActions()
                                          where current.actionType == PokerAction.Call || current.actionType == PokerAction.Raise
                                          where current.playerId != currentDecision.PlayerId
                                          select current.playerId).ToArray();

          for (int i = 0; i < calledRaisedPlayerIds.Length; i++)
            potAmountAssumingAllCall += raiseToCallAmountNew - currentDecision.Cache.getPlayerCurrentRoundBetAmount(calledRaisedPlayerIds[i]);

          potAmountAssumingAllCall += ((decimal)(activePlayerIds.Length - 1 - calledRaisedPlayerIds.Length) / 4) * raiseToCallAmountNew;
        }
        else
        {
          for (int i = 0; i < activePlayerIds.Length; i++)
          {
            if (activePlayerIds[i] != currentDecision.PlayerId)
              potAmountAssumingAllCall += raiseToCallAmountNew - currentDecision.Cache.getPlayerCurrentRoundBetAmount(activePlayerIds[i]);
          }
        }

        decimal extraDollarRiskAmount = raiseToCallAmountNew - infoStore.GetInfoValue(InfoType.BP_PlayerBetAmountCurrentRound_Decimal);

        decimal actualRaiseToCallEV = (currentPotAmount * probAllOpponentFoldRaiseToCall) + (probWin * potAmountAssumingAllCall * (1.0m - probAllOpponentFoldRaiseToCall)) - ((1.0m - probWin) * extraDollarRiskAmount * (1.0m - probAllOpponentFoldRaiseToCall));

        scaledRaiseToCallEV = (actualRaiseToCallEV / currentDecision.Cache.BigBlind) / bigBlindEVScaleAmount;
      }
      #endregion

      #region RaiseToStealEV
      decimal scaledRaiseToStealEV = scaledCallCheckEV;
      if (raisePossible)
      {
        decimal potAmountAssumingAllCall = currentPotAmount;

        //If we are preflop then it would be unrealistic to expect everyone to call
        //For steal we assume that if there are 8 players to act after the raise we get 1 call
        //If there are 4 players to act after the raise we get 0.5 call
        //This is the same as dividing the number of activePlayers by 8
        if (currentBettingRound == 0)
        {
          //We assume anyone who has already raised will call again
          long[] raisedPlayerIds = (from current in currentDecision.Cache.getAllHandActions()
                                    where current.actionType == PokerAction.Raise
                                    where current.playerId != currentDecision.PlayerId
                                    select current.playerId).ToArray();

          for (int i = 0; i < raisedPlayerIds.Length; i++)
            potAmountAssumingAllCall += raiseToStealAmountNew - currentDecision.Cache.getPlayerCurrentRoundBetAmount(raisedPlayerIds[i]);

          potAmountAssumingAllCall += ((decimal)(activePlayerIds.Length - 1 - raisedPlayerIds.Length) / 8) * raiseToStealAmountNew;
        }
        else
        {
          for (int i = 0; i < activePlayerIds.Length; i++)
          {
            if (activePlayerIds[i] != currentDecision.PlayerId)
              potAmountAssumingAllCall += raiseToStealAmountNew - currentDecision.Cache.getPlayerCurrentRoundBetAmount(activePlayerIds[i]);
          }
        }

        decimal extraDollarRiskAmount = raiseToStealAmountNew - infoStore.GetInfoValue(InfoType.BP_PlayerBetAmountCurrentRound_Decimal);

        decimal actualRaiseToStealEV = (currentPotAmount * probAllOpponentFoldRaiseToSteal) + (probWin * potAmountAssumingAllCall * (1.0m - probAllOpponentFoldRaiseToSteal)) - ((1.0m - probWin) * extraDollarRiskAmount * (1.0m - probAllOpponentFoldRaiseToSteal));
        scaledRaiseToStealEV = (actualRaiseToStealEV / currentDecision.Cache.BigBlind) / bigBlindEVScaleAmount;
      }

      #endregion
      #endregion

      #region playerPosition and numPlayers
      decimal dealerDistance = ((infoStore.GetInfoValue(InfoType.GP_DealerDistance_Byte) - 1) / (infoStore.GetInfoValue(InfoType.GP_NumActivePlayers_Byte) - 1));
      bool tablePositionEarly = false;
      bool tablePositionMid = false;
      bool tablePositionLate = false;

      if (dealerDistance < (1.0m / 3.0m) && dealerDistance >= 0)
        tablePositionEarly = true;
      else if (dealerDistance < (2.0m / 3.0m) && dealerDistance >= 0)
        tablePositionMid = true;
      else if (dealerDistance <= 1.0m && dealerDistance >= 0)
        tablePositionLate = true;
      else
        throw new Exception("Dealer distance must be between 0 and 1.");

      bool lastToAct = infoStore.GetInfoValue(InfoType.GP_NumUnactedPlayers_Byte) == 1;
      #endregion playerPosition and numPlayers

      #region Hand and Action History
      List<PokerAction> allPlayerActionsCurrentHand = currentDecision.Cache.getPlayerActionsCurrentHand(currentDecision.PlayerId);
      decimal callCount = (decimal)allPlayerActionsCurrentHand.Count(entry => entry == PokerAction.Call);
      decimal checkCount = (decimal)allPlayerActionsCurrentHand.Count(entry => entry == PokerAction.Check);
      decimal raiseCount = (decimal)allPlayerActionsCurrentHand.Count(entry => entry == PokerAction.Raise);

      decimal currentHandOurAggression = raiseCount - (callCount + checkCount);
      #endregion

      #region potCommitment
      bool potCommitted = false;
      if (infoStore.GetInfoValue(InfoType.BP_PlayerMoneyInPot_Decimal) > infoStore.GetInfoValue(InfoType.BP_PlayerHandStartingStackAmount_Decimal) * 0.75m)
        potCommitted = true;
      #endregion

      if (currentBettingRound == 0)
      {
        #region Preflop
        decimal holeCardsPlayability = infoStore.GetInfoValue(InfoType.CP_HoleCardsMatchedPlayability);
        byte numCallers = (byte)infoStore.GetInfoValue(InfoType.BP_TotalNumCalls_Byte);

        //Used an odd format to allow multiple fall through all statements
        //i.e. we can use break; to go to the end of the loop
        while (true)
        {
          if (holeCardsPlayability == 1)
          {
            if (playerLastActionCurrentRound == PokerAction.Raise && topHand && randomGen.NextDouble() > 0.8)
            {
              decision = 4;
              raiseToStealAmountNew = currentRoundMinPlayCallAmount + remainingPlayerStack;
              break;
            }
            else
            {
              decision = 3;
              break;
            }
          }

          //If we are required to call an amount greater than 10 times the big blind ($2.50) on 0.25 tables and we dont have AA, KK we fold
          if (additionalCallAmount > currentDecision.Cache.BigBlind * 10 && !topHand)
            break;

          if (holeCardsPlayability >= 0.9m)
          {
            if (playerLastActionCurrentRound == PokerAction.Raise && topHand && randomGen.NextDouble() > 0.9)
            {
              decision = 4;
              raiseToStealAmountNew = currentRoundMinPlayCallAmount + remainingPlayerStack;
              break;
            }
            else if (tablePositionLate &&
                currentRoundMinPlayCallAmount > currentDecision.Cache.BigBlind &&
                scaledCallCheckEV > 0 && scaledRaiseToCallEV > scaledCallCheckEV)
            {
              //Late position, raised pot, 80% reraise
              if (randomGen.NextDouble() > 0.20 && playerLastActionCurrentRound != PokerAction.Raise)
                decision = 3;
              else
                decision = 2;

              break;
            }
            else if (currentRoundMinPlayCallAmount > currentDecision.Cache.BigBlind &&
                (scaledCallCheckEV > 0 || (additionalCallAmount <= 4 * currentDecision.Cache.BigBlind && scaledCallCheckEV > -0.1m) || topHand))
            {
              //Not late, raised pot, call
              decision = 2;

              if (scaledRaiseToCallEV > 0 &&
                  playerLastActionCurrentRound != PokerAction.Raise &&
                  randomGen.NextDouble() > 0.50)
                decision = 3;

              break;
            }
            else if (currentRoundMinPlayCallAmount == currentDecision.Cache.BigBlind)
            {
              //Unraised, there is 90% prob we will raise
              if (randomGen.NextDouble() > 0.1)
                decision = 3;
              else
                decision = 2;

              break;
            }
          }

          if (holeCardsPlayability >= 0.75m)
          {
            //Early position, raised pot
            if (tablePositionEarly && currentRoundMinPlayCallAmount > currentDecision.Cache.BigBlind &&
                (scaledCallCheckEV > 0 || (additionalCallAmount <= 4 * currentDecision.Cache.BigBlind && scaledCallCheckEV > -0.2m) || topHand))
            {
              decision = 2;
              break;
            }
            //Not early, raised pot
            if (!tablePositionEarly && currentRoundMinPlayCallAmount > currentDecision.Cache.BigBlind &&
                (scaledCallCheckEV > 0 || (additionalCallAmount <= 4 * currentDecision.Cache.BigBlind && scaledCallCheckEV > -0.2m) || topHand))
            {
              decision = 2;

              if (raiseToCallAmountNew <= 8 * currentDecision.Cache.BigBlind && randomGen.NextDouble() > 0.5)
                decision = 3;

              break;
            }
            //Unraised pot
            else if (currentRoundMinPlayCallAmount == currentDecision.Cache.BigBlind)
            {
              if (randomGen.NextDouble() > 0.1)
                decision = 3;
              else
                decision = 2;

              break;
            }
          }

          if (holeCardsPlayability >= 0.6m)
          {
            if (tablePositionEarly &&
                (scaledCallCheckEV > 0 || (additionalCallAmount <= 4 * currentDecision.Cache.BigBlind && scaledCallCheckEV > -0.1m) || topHand))
            {
              decision = 2;
              break;
            }
            else if ((scaledCallCheckEV > 0 || (additionalCallAmount <= 4 * currentDecision.Cache.BigBlind && scaledCallCheckEV > -0.15m) || topHand))
            {
              decision = 2;
              break;
            }
          }

          if (holeCardsPlayability >= 0.5m)
          {
            if (tablePositionLate &&
                (scaledCallCheckEV > 0 || (additionalCallAmount <= 4 * currentDecision.Cache.BigBlind && scaledCallCheckEV > -0.15m) || topHand))
            {
              decision = 2;
              break;
            }
            else if ((playerLastActionCurrentRound == PokerAction.Call || playerLastActionCurrentRound == PokerAction.Raise) &&
                (scaledCallCheckEV > 0 || (additionalCallAmount <= 4 * currentDecision.Cache.BigBlind && scaledCallCheckEV > -0.15m) || topHand))
            {
              decision = 2;
              break;
            }
          }

          if (holeCardsPlayability >= 0.3m)
          {
            if (currentRoundMinPlayCallAmount == currentDecision.Cache.BigBlind)
            {
              decision = 2;
              break;
            }
            else if (tablePositionLate && (playerLastActionCurrentRound == PokerAction.Call || playerLastActionCurrentRound == PokerAction.Raise) &&
                (scaledCallCheckEV > 0 || (additionalCallAmount <= 4 * currentDecision.Cache.BigBlind && scaledCallCheckEV > -0.1m) || topHand))
            {
              decision = 2;
              break;
            }
          }

          if (holeCardsPlayability >= 0.25m)
          {
            if (tablePositionLate && currentRoundMinPlayCallAmount == currentDecision.Cache.BigBlind)
            {
              decision = 2;
              break;
            }
          }

          if (holeCardsPlayability >= 0.1m)
          {
            if (tablePositionLate && numCallers >= 2 && currentRoundMinPlayCallAmount == currentDecision.Cache.BigBlind)
            {
              decision = 2;
              break;
            }
            else if (topHand)
            {
              decision = 2;
              break;
            }
          }

          //We only fall through once
          break;
        }

        #endregion
      }
      else
      {
        #region PostFlop Strategy
        //decimal playEVThreshold = 0.05m;
        decimal playEVThreshold = 0;

        //Need some form of opponent aggression factor to use in scaling the slow play
        //Scaled between 0 and 1 for aggression between 0.01 and 10
        //Default value is approx 0.57
        double avgOppAggression = (double)infoStore.GetInfoValue(InfoType.AP_AvgLiveOppCurrentRoundAggr_Double);

        //If we have hit big
        if (probWin > 0.95m)
        {
          #region BigHit
          //River only
          if (currentBettingRound == 3)
          {
            #region River BigHit
            //Has our opponent raised every round? If so we will check again here
            if (tablePositionEarly && checkPossible && playerLastActionCurrentRound == PokerAction.NoAction)
            {
              //What is our opponent aggression this hand?
              double averageOpponentAggression = 0;
              for (int i = 0; i < activePlayerIds.Length; i++)
              {
                if (activePlayerIds[i] != currentDecision.PlayerId)
                {
                  List<PokerAction> allOpponentActions = currentDecision.Cache.getPlayerActionsCurrentHandPostFlop(activePlayerIds[i]);

                  double currentHandOpponentAggression = allOpponentActions.Count(entry => entry == PokerAction.Raise) -
                      (allOpponentActions.Count(entry => entry == PokerAction.Call) + allOpponentActions.Count(entry => entry == PokerAction.Check));

                  averageOpponentAggression += currentHandOpponentAggression;
                }
              }

              averageOpponentAggression /= (double)(activePlayerIds.Length - 1);

              if (averageOpponentAggression >= 2 || randomGen.NextDouble() < (avgOppAggression - 0.8))
              {
                decision = 1;
                Debug.WriteLine("Checking with a whopper on the river hoping you will raise, aggressive SOB!");
              }
              else
              {
                Debug.WriteLine("Raising with my big one on river as you havn't been very aggressive.");

                //When we are on the river we play differently with big hits
                //We will always 'atleast' raise to call with a really good hand
                decision = 3;

                //We may occasionaly raiseToSteal on the river with a good hand to keep opponenents guessing
                if (scaledRaiseToStealEV > scaledRaiseToCallEV && randomGen.NextDouble() > 0.75)
                  decision = 4;
              }
            }
            else
            {
              Debug.WriteLine("Raising with big hit on river as it looks like you have nothing. Pay me!");

              //When we are on the river we play differently with big hits
              //We will always 'atleast' raise to call with a really good hand
              decision = 3;

              //We may occasionaly raiseToSteal on the river with a good hand to keep opponenents guessing
              if (scaledRaiseToStealEV > scaledRaiseToCallEV && randomGen.NextDouble() > 0.75)
                decision = 4;
            }
            #endregion
          }
          else
          {
            if (currentBettingRound == 1 || //We will slow play on the flop
                (playerLastActionCurrentRound == PokerAction.Check && randomGen.NextDouble() < avgOppAggression) || //If we just checked we may continue to slow play this round
                (scaledCallCheckEV > 0.25m && checkPossible && !tablePositionLate && randomGen.NextDouble() < 0.6 * avgOppAggression) || //Sensible pot (flop/turn) we're early and it's not yet raised
                scaledCallCheckEV > 0.75m && !checkPossible && additionalCallAmount > 0.25m * currentPotAmount) //Big pot (flop/turn) and it's been raised sensibly
            {
              Debug.WriteLine("Slow playing with my big hit. Will you take the bait?");

              if (checkPossible)
                decision = 1;
              else
                decision = 2;
            }
            else
            {
              Debug.WriteLine("I'm not going to slow play my hit. Raising so that it gets expensive if you want to continue.");
              //If we are are not going to slow play then we ofcourse default to raise
              decision = 3;
            }
          }
          #endregion
        }
        else if (probWin > 0.5m)
        {
          //On the river we have either won or lost so it should not be possible to have a probWin between 0.5 and 0.95
          if (currentBettingRound == 3)
            throw new Exception("This should be impossible!");

          #region ProbableWin
          //If both raise EV's are positive and better than just calling/checking
          if (scaledRaiseToCallEV > playEVThreshold &&
              scaledRaiseToStealEV > playEVThreshold &&
              scaledRaiseToCallEV > scaledCallCheckEV &&
              scaledRaiseToStealEV > scaledCallCheckEV)
          {
            //Unraised, we've been aggressive, in an early position, then 40% of the time check
            if (checkPossible &&
                currentHandOurAggression > 2 &&
                scaledCallCheckEV > playEVThreshold &&
                tablePositionEarly &&
                randomGen.NextDouble() < avgOppAggression)
            {
              Debug.WriteLine("Early position with OK hand, have already been agressive so checking.");
              decision = 1;
            }

            //Unraised, early position, 15% time we will check
            else if (checkPossible &&
                scaledCallCheckEV > playEVThreshold &&
                randomGen.NextDouble() < avgOppAggression * (double)(1.0m - (probWin * 0.8m))) //higher prob win, less checks
            {
              Debug.WriteLine("Sometimes I want to check with my OK hand.");
              decision = 1;
            }

            //Before river and raised, if we have been aggressive or due to the random number we may call
            else if (!checkPossible &&
                scaledCallCheckEV > playEVThreshold &&
                (randomGen.NextDouble() < avgOppAggression * 0.75 || currentHandOurAggression > 2 || additionalCallAmount > 0.25m * currentPotAmount))
            {
              Debug.WriteLine("I have a better hand than you but sometimes I will just call to be annoying.");
              decision = 2;
            }

            //If we have not checked or called then we will generally be raising
            else if ((randomGen.NextDouble() < 0.8 && scaledRaiseToCallEV > scaledRaiseToStealEV) || //If raiseToCall is best 80% of the time we raiseToCall
                (randomGen.NextDouble() < 0.2 && scaledRaiseToStealEV > scaledRaiseToCallEV) || //If raiseToSteal is best 20% of the time we raiseToCall
                (probAllOpponentFoldRaiseToSteal >= 0.75m) || //Probability everyone will fold if we try to steal is high enough
                currentPotAmount > 40 * currentDecision.Cache.BigBlind) //The pot is already large, a steal is probably pointless
            {
              //Generally the raise will be to call
              Debug.WriteLine("Considered checking/calling with an OK hand but TBH i'm going to raise to call.");
              decision = 3;
            }
            else
            {
              Debug.WriteLine("Considered checking/calling with an OK hand but TBH i'm going to raise to steal.");
              //If we made it to raising but for some reason we didnt raise to call then a steal it is
              decision = 4;
            }
          }
          else if (scaledCallCheckEV > playEVThreshold)
          {
            Debug.WriteLine("Raising doesn't really work for me so i'm just going to call/check.");
            if (checkPossible)
              decision = 1;
            else
              decision = 2;
          }
          #endregion
        }
        else if (probWin > 0.1m)
        {
          #region DangerArea
          //We will consider a steal if it looks favourable
          if (scaledRaiseToStealEV > playEVThreshold &&
          scaledRaiseToStealEV > scaledCallCheckEV &&
          scaledRaiseToStealEV > scaledRaiseToCallEV &&
          probAllOpponentFoldRaiseToSteal > 0.75m &&
          currentPotAmount <= 40 * currentDecision.Cache.BigBlind)
          {
            Debug.WriteLine("My hand is not great but I think I can steal it from you, haha.");
            decision = 4;
          }
          else
          {
            //First action on river with a small pot, finite chance of raising anyway
            if (currentBettingRound == 3 &&
                playerLastActionCurrentRound == PokerAction.NoAction &&
                randomGen.NextDouble() < 0.10 &&
                scaledRaiseToCallEV > playEVThreshold &&
                currentPotAmount < 12 * currentDecision.Cache.BigBlind)
            {
              Debug.WriteLine("My hand is not great but I fancy semi bluffing and faking strength.");
              if (randomGen.NextDouble() > (double)probAllOpponentFoldRaiseToSteal || scaledRaiseToCallEV > scaledRaiseToStealEV)
                decision = 3;
              else
                decision = 4;
            }
            //Tried to check but got raised a small amount
            else if (playerLastActionCurrentRound == PokerAction.Check &&
                randomGen.NextDouble() < avgOppAggression * 0.2 &&
                scaledRaiseToCallEV > playEVThreshold &&
                (currentRoundMinPlayCallAmount * 10.0m <= currentPotAmount || currentRoundMinPlayCallAmount <= currentDecision.Cache.BigBlind * 1.2m))
            {
              decision = 3;
              Debug.WriteLine("Hahahaha, if you're going to raise so little can you handle my check-raise bluff??");
            }
            else if (scaledCallCheckEV > playEVThreshold)
            {
              if (checkPossible)
              {
                Debug.WriteLine("MY hand is not great so i'm checking for some free cards.");
                decision = 1;
              }
              else
              {
                Debug.WriteLine("I'll call but only because i fancy my draws.");
                decision = 2;
              }
            }
            else
            {
              Debug.WriteLine("I just don't fancy my draws, folding is probably best for me.");
              decision = 0;
            }
          }
          #endregion
        }
        else
        {
          #region BigFatMiss Or Big Opponent Hit

          if (currentBettingRound == 3 &&
              (playerLastActionCurrentRound == PokerAction.NoAction || playerLastActionCurrentRound == PokerAction.Check) &&
              currentPotAmount < 12 * currentDecision.Cache.BigBlind &&
              //Depending on the amount raised and the opponent aggression we may try to bluff
              randomGen.NextDouble() < 0.02 * avgOppAggression * (double)(additionalCallAmount > 0 ? (currentPotAmount - additionalCallAmount) / additionalCallAmount : 5))
          {
            Debug.WriteLine("I have so totally missed, but against my better judgement i'm going to bluff once on the river.");
            if (randomGen.NextDouble() > (double)probAllOpponentFoldRaiseToSteal)
              decision = 3;
            else
              decision = 4;
          }
          else
          {
            //We are not going to attempt to steal if we are beat
            if (scaledCallCheckEV > playEVThreshold)
            {
              Debug.WriteLine("I have so totally missed, but my EV says YES!");
              if (checkPossible)
                decision = 1;
              else
                decision = 2;
            }
            else
            {
              Debug.WriteLine("Fair play, you can have this one, AHOLE!");
              decision = 0;
            }
          }
          #endregion
        }
        #endregion
      }

      //If we are pot committed we must call anything from here on out
      if (potCommitted && !checkPossible && probWin > 0.01m)
        decision = 2;

      if (decision == 0)
        return new Play(PokerAction.Fold, 0, 0, currentDecision.Cache.getCurrentHandId(), currentDecision.PlayerId, decisionLogStr, 0);
      else if (decision == 1)
        return new Play(PokerAction.Check, 0, 0, currentDecision.Cache.getCurrentHandId(), currentDecision.PlayerId, decisionLogStr, 0);
      else if (decision == 2)
        return new Play(PokerAction.Call, additionalCallAmount, 0, currentDecision.Cache.getCurrentHandId(), currentDecision.PlayerId, decisionLogStr, 0);
      else if (decision == 3)
        return new Play(PokerAction.Raise, raiseToCallAmountNew, 0, currentDecision.Cache.getCurrentHandId(), currentDecision.PlayerId, decisionLogStr, 0);
      else if (decision == 4)
        return new Play(PokerAction.Raise, raiseToStealAmountNew, 0, currentDecision.Cache.getCurrentHandId(), currentDecision.PlayerId, decisionLogStr, 0);
      else
        throw new Exception("decision value not valid.");
    }
  }
}
