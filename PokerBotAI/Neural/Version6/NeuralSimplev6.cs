using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using PokerBot.Definitions;
using PokerBot.AI.InfoProviders;
using System.Windows.Forms;
using System.Diagnostics;
using PokerBot.AI.Neural;
using Encog;
using ProviderAggression;

namespace PokerBot.AI.Neural.Version6
{
  internal class NeuralAIv6 : NeuralAIBase
  {
    NeuralAINNModelV6 neuralPokerAI = new NeuralAINNModelV6();

#if logging
        //Added for testing, can be removed
        static object locker = new object();
        System.Security.Cryptography.MD5CryptoServiceProvider md5 = new System.Security.Cryptography.MD5CryptoServiceProvider();
        System.Runtime.Serialization.Formatters.Binary.BinaryFormatter bin = new System.Runtime.Serialization.Formatters.Binary.BinaryFormatter();
        System.IO.MemoryStream mem = new System.IO.MemoryStream();
#endif

    public NeuralAIv6(AIRandomControl aiRandomControl)
        : base(aiRandomControl)
    {
      aiType = AIGeneration.NeuralV6;

      //Setup this AI's specific update key
      specificUpdateKey = new RequestedInfoKey(false);

      specificUpdateKey.SetInfoTypeRequired(InfoType.GP_GameStage_Byte);

      specificUpdateKey.SetInfoTypeRequired(InfoType.CP_HoleCardsAAPair_Bool);
      specificUpdateKey.SetInfoTypeRequired(InfoType.CP_HoleCardsKKPair_Bool);
      specificUpdateKey.SetInfoTypeRequired(InfoType.CP_HoleCardsAK_Bool);
      specificUpdateKey.SetInfoTypeRequired(InfoType.CP_HoleCardsOtherHighPair_Bool);
      specificUpdateKey.SetInfoTypeRequired(InfoType.CP_HoleCardsOtherLowPair_Bool);
      specificUpdateKey.SetInfoTypeRequired(InfoType.CP_HoleCardsTroubleHand_Bool);
      specificUpdateKey.SetInfoTypeRequired(InfoType.CP_HoleCardsMidConnector_Bool);
      specificUpdateKey.SetInfoTypeRequired(InfoType.CP_HoleCardsLowConnector_Bool);
      specificUpdateKey.SetInfoTypeRequired(InfoType.CP_HoleCardsSuited_Bool);

      specificUpdateKey.SetInfoTypeRequired(InfoType.GP_DealerDistance_Byte);
      specificUpdateKey.SetInfoTypeRequired(InfoType.GP_NumActivePlayers_Byte);
      specificUpdateKey.SetInfoTypeRequired(InfoType.GP_NumUnactedPlayers_Byte);

      specificUpdateKey.SetInfoTypeRequired(InfoType.BP_PlayerMoneyInPot_Decimal);
      specificUpdateKey.SetInfoTypeRequired(InfoType.BP_PlayerHandStartingStackAmount_Decimal);
      specificUpdateKey.SetInfoTypeRequired(InfoType.BP_PlayerBetAmountCurrentRound_Decimal);
      specificUpdateKey.SetInfoTypeRequired(InfoType.BP_TotalPotAmount_Decimal);
      specificUpdateKey.SetInfoTypeRequired(InfoType.BP_MinimumPlayAmount_Decimal);
      specificUpdateKey.SetInfoTypeRequired(InfoType.BP_LastAdditionalRaiseAmount);

      specificUpdateKey.SetInfoTypeRequired(InfoType.WR_ProbOpponentHasBetterWRFIXED);
      specificUpdateKey.SetInfoTypeRequired(InfoType.WR_RaiseToCallAmount);
      specificUpdateKey.SetInfoTypeRequired(InfoType.WR_RaiseToStealAmount);
      specificUpdateKey.SetInfoTypeRequired(InfoType.WR_RaiseToCallStealSuccessProb);
      specificUpdateKey.SetInfoTypeRequired(InfoType.WR_RaiseToStealSuccessProb);

      //specificUpdateKey.SetInfoTypeRequired(InfoType.PAP_RaiseToStealSuccess_Prob);
      //specificUpdateKey.SetInfoTypeRequired(InfoType.PAP_RaiseToCallAmount_Amount);
      //specificUpdateKey.SetInfoTypeRequired(InfoType.PAP_RaiseToStealAmount_Amount);

      specificUpdateKey.SetInfoTypeRequired(InfoType.AP_AvgLiveOppPreFlopPlayFreq_Double);
      specificUpdateKey.SetInfoTypeRequired(InfoType.AP_AvgLiveOppPostFlopPlayFreq_Double);
      specificUpdateKey.SetInfoTypeRequired(InfoType.AP_AvgLiveOppCurrentRoundAggr_Double);
      specificUpdateKey.SetInfoTypeRequired(InfoType.AP_AvgLiveOppCurrentRoundAggrAcc_Double);

      ////The following mostly used by PAP
      //specificUpdateKey.SetInfoTypeRequired(InfoType.GP_NumTableSeats_Byte);
      //specificUpdateKey.SetInfoTypeRequired(InfoType.CP_FlushPossible_Bool);
      //specificUpdateKey.SetInfoTypeRequired(InfoType.CP_StraightPossible_Bool);
      //specificUpdateKey.SetInfoTypeRequired(InfoType.CP_AOnBoard_Bool);
      //specificUpdateKey.SetInfoTypeRequired(InfoType.CP_KOnBoard_Bool);
      //specificUpdateKey.SetInfoTypeRequired(InfoType.CP_AKQToBoardRatio_Real);
      //specificUpdateKey.SetInfoTypeRequired(InfoType.BP_LastAdditionalRaiseAmount);
      //specificUpdateKey.SetInfoTypeRequired(InfoType.BP_BetsToCall_Byte);
    }

    protected override Dictionary<InfoType, string> GetInfoUpdateConfigs()
    {
      defaultInfoTypeUpdateConfigs = new Dictionary<InfoType, string>();
      return defaultInfoTypeUpdateConfigs;
    }

    protected override RequestedInfoKey GetUpdateKeyOrPreDecision()
    {
      return specificUpdateKey;
    }

    protected override Play GetDecision()
    {
      Dictionary<string, decimal> networkInputs = new Dictionary<string, decimal>();

      //Some values need to be moved the top because they are used in multiple places
      decimal currentRoundMinPlayCallAmount = currentDecision.Cache.getMinimumPlayAmount();
      decimal additionalCallAmount = currentRoundMinPlayCallAmount - infoStore.GetInfoValue(InfoType.BP_PlayerBetAmountCurrentRound_Decimal);
      decimal currentPotAmount = infoStore.GetInfoValue(InfoType.BP_TotalPotAmount_Decimal);
      decimal remainingPlayerStack = currentDecision.Cache.getPlayerStack(currentDecision.PlayerId);

      //Current Round Actions
      List<PokerAction> currentRoundPlayerActions = (from current in currentDecision.Cache.getPlayerCurrentRoundActions(currentDecision.PlayerId)
                                                     where current == PokerAction.Check ||
                                                     current == PokerAction.Call ||
                                                     current == PokerAction.Raise
                                                     select current).ToList();

      #region RaiseAmounts
      decimal raiseToCallAmountNew, raiseToStealAmountNew;

      //If we are preflop we calculate the raise amounts in a slightly more fixed fashion
      if (currentDecision.Cache.getBettingRound() == 0)
      {
        double randomNumber = randomGen.NextDouble();

        //If the pot is unraised
        if (infoStore.GetInfoValue(InfoType.BP_MinimumPlayAmount_Decimal) == currentDecision.Cache.BigBlind)
        {
          if (randomNumber > 0.8)
            raiseToCallAmountNew = 4m * currentDecision.Cache.BigBlind;
          else if (randomNumber > 0.4)
            raiseToCallAmountNew = 3.5m * currentDecision.Cache.BigBlind;
          else
            raiseToCallAmountNew = 3.0m * currentDecision.Cache.BigBlind;
        }
        else
        {
          decimal currentRoundLastRaiseAmount = infoStore.GetInfoValue(InfoType.BP_LastAdditionalRaiseAmount);
          decimal additionalNewRaiseAmount;

          if (randomNumber > 0.9)
            additionalNewRaiseAmount = 2 * currentRoundLastRaiseAmount;
          else if (randomNumber > 0.45)
            additionalNewRaiseAmount = 1.5m * currentRoundLastRaiseAmount;
          else
            additionalNewRaiseAmount = 1 * currentRoundLastRaiseAmount;

          raiseToCallAmountNew = additionalNewRaiseAmount + infoStore.GetInfoValue(InfoType.BP_MinimumPlayAmount_Decimal);
        }

        raiseToStealAmountNew = 1.5m * raiseToCallAmountNew;

        //We will only scale the raiseToCallAmount if it is not a bigblind multiple
        if (((int)(raiseToCallAmountNew / currentDecision.Cache.BigBlind)) * currentDecision.Cache.BigBlind != raiseToCallAmountNew)
        {
          //Round the raiseToCallAmount
          decimal raiseToCallAmountBlindMultiple = raiseToCallAmountNew / currentDecision.Cache.LittleBlind;
          raiseToCallAmountNew = Math.Round(raiseToCallAmountBlindMultiple, 0, MidpointRounding.AwayFromZero) * currentDecision.Cache.LittleBlind;
        }

        //Round the raiseToStealAmount
        decimal raiseToStealAmountBlindMultiple = raiseToStealAmountNew / currentDecision.Cache.LittleBlind;
        raiseToStealAmountNew = Math.Round(raiseToStealAmountBlindMultiple, 0, MidpointRounding.AwayFromZero) * currentDecision.Cache.LittleBlind;
      }
      else
      {
        raiseToCallAmountNew = (decimal)infoStore.GetInfoValue(InfoType.WR_RaiseToCallAmount);
        raiseToStealAmountNew = (decimal)infoStore.GetInfoValue(InfoType.WR_RaiseToStealAmount);
      }

      //Some raise validation
      //Validate we can actually raise the selected amounts
      decimal maximumRaiseAmount = remainingPlayerStack + (decimal)infoStore.GetInfoValue(InfoType.BP_PlayerBetAmountCurrentRound_Decimal);

      //if (raiseToCallAmount > maximumRaiseAmount) raiseToCallAmount = maximumRaiseAmount;
      //if (raiseToStealAmount > maximumRaiseAmount) raiseToStealAmount = maximumRaiseAmount;

      //Check for a big raise amount which would be best as going all in
      if (raiseToCallAmountNew > 0.8m * maximumRaiseAmount)
        raiseToCallAmountNew = maximumRaiseAmount;
      if (raiseToStealAmountNew > 0.8m * maximumRaiseAmount)
        raiseToStealAmountNew = maximumRaiseAmount;

      //If we have already raised twice this round all amounts are set to all in
      if (currentRoundPlayerActions.Count(entry => entry == PokerAction.Raise) >= 2)
      {
        raiseToCallAmountNew = maximumRaiseAmount;
        raiseToStealAmountNew = maximumRaiseAmount;
      }
      #endregion RaiseAmounts

      /////////////////////////////////////////
      ///////////ALL NON BOOLEAN INPUTS BETWEEN 0.9 and 0.1
      /////////////////////////////////////////

      #region neuralInputs

      #region gameStage

      decimal gameStagePreFlop = 0;
      decimal gameStagePostFlop = 0;
      decimal gameStageRiver = 0;

      if (infoStore.GetInfoValue(InfoType.GP_GameStage_Byte) == 0)
        gameStagePreFlop = 1;
      else
        gameStagePostFlop = 1;

      if (infoStore.GetInfoValue(InfoType.GP_GameStage_Byte) == 3)
        gameStageRiver = 1;

      networkInputs.Add("GameStagePreFlop", gameStagePreFlop);
      networkInputs.Add("GameStagePostFlop", gameStagePostFlop);
      networkInputs.Add("GameStageRiver", gameStageRiver);

      #endregion gameStage
      //3 inputs

      #region preFlop HoleCards
      if (gameStagePreFlop == 1)
      {
        //Card info types
        networkInputs.Add("HoleCardsAAPair", infoStore.GetInfoValue(InfoType.CP_HoleCardsAAPair_Bool));
        networkInputs.Add("HoleCardsKKPair", infoStore.GetInfoValue(InfoType.CP_HoleCardsKKPair_Bool));
        networkInputs.Add("HoleCardsAKPair", infoStore.GetInfoValue(InfoType.CP_HoleCardsAK_Bool));
        networkInputs.Add("HoleCardsOtherHighPair", infoStore.GetInfoValue(InfoType.CP_HoleCardsOtherHighPair_Bool));
        networkInputs.Add("HoleCardsOtherLowPair", infoStore.GetInfoValue(InfoType.CP_HoleCardsOtherLowPair_Bool));
        networkInputs.Add("HoleCardsTroubleHand", infoStore.GetInfoValue(InfoType.CP_HoleCardsTroubleHand_Bool));
        networkInputs.Add("HoleCardsMidConnector", infoStore.GetInfoValue(InfoType.CP_HoleCardsMidConnector_Bool));
        networkInputs.Add("HoleCardsLowConnector", infoStore.GetInfoValue(InfoType.CP_HoleCardsLowConnector_Bool));
        networkInputs.Add("HoleCardsSuited", infoStore.GetInfoValue(InfoType.CP_HoleCardsSuited_Bool));
      }
      else
      {
        networkInputs.Add("HoleCardsAAPair", 0);
        networkInputs.Add("HoleCardsKKPair", 0);
        networkInputs.Add("HoleCardsAKPair", 0);
        networkInputs.Add("HoleCardsOtherHighPair", 0);
        networkInputs.Add("HoleCardsOtherLowPair", 0);
        networkInputs.Add("HoleCardsTroubleHand", 0);
        networkInputs.Add("HoleCardsMidConnector", 0);
        networkInputs.Add("HoleCardsLowConnector", 0);
        networkInputs.Add("HoleCardsSuited", 0);
      }
      #endregion
      //12 inputs

      #region playerPosition and numPlayers
      decimal dealerDistance = ((infoStore.GetInfoValue(InfoType.GP_DealerDistance_Byte) - 1) / (infoStore.GetInfoValue(InfoType.GP_NumActivePlayers_Byte) - 1));
      decimal tablePositionEarly = 0;
      decimal tablePositionMid = 0;
      decimal tablePositionLate = 0;

      if (dealerDistance < (1.0m / 3.0m) && dealerDistance >= 0)
        tablePositionEarly = 1;
      else if (dealerDistance < (2.0m / 3.0m) && dealerDistance >= 0)
        tablePositionMid = 1;
      else if (dealerDistance <= 1.0m && dealerDistance >= 0)
        tablePositionLate = 1;
      else
        throw new Exception("Dealer distance must be between 0 and 1.");

      networkInputs.Add("TablePositionEarly", tablePositionEarly);
      networkInputs.Add("TablePositionMid", tablePositionMid);
      networkInputs.Add("TablePositionLate", tablePositionLate);

      networkInputs.Add("NumActivePlayers4Plus", (infoStore.GetInfoValue(InfoType.GP_NumActivePlayers_Byte) >= 4 ? 1 : 0));
      networkInputs.Add("NumActivePlayers3", (infoStore.GetInfoValue(InfoType.GP_NumActivePlayers_Byte) == 3 ? 1 : 0));
      networkInputs.Add("NumActivePlayers2", (infoStore.GetInfoValue(InfoType.GP_NumActivePlayers_Byte) == 2 ? 1 : 0));

      networkInputs.Add("LastToAct", (infoStore.GetInfoValue(InfoType.GP_NumUnactedPlayers_Byte) == 1 ? 1 : 0));
      #endregion playerPosition and numPlayers
      //19 inputs

      #region potCommitment
      decimal potCommitted = 0;
      if (infoStore.GetInfoValue(InfoType.BP_PlayerMoneyInPot_Decimal) > infoStore.GetInfoValue(InfoType.BP_PlayerHandStartingStackAmount_Decimal) * 0.75m)
        potCommitted = 1;

      networkInputs.Add("PotCommitted", potCommitted);
      #endregion
      //20 inputs

      #region Hand and Action History
      List<PokerAction> allPlayerActionsCurrentHand = currentDecision.Cache.getPlayerActionsCurrentHand(currentDecision.PlayerId);
      decimal callCount = (decimal)allPlayerActionsCurrentHand.Count(entry => entry == PokerAction.Call);
      decimal checkCount = (decimal)allPlayerActionsCurrentHand.Count(entry => entry == PokerAction.Check);
      decimal raiseCount = (decimal)allPlayerActionsCurrentHand.Count(entry => entry == PokerAction.Raise);

      decimal currentHandOurAggression = 0;

      currentHandOurAggression = raiseCount - (callCount + checkCount);
      if (currentHandOurAggression < -6)
        currentHandOurAggression = -6;
      else if (currentHandOurAggression > 6)
        currentHandOurAggression = 6;

      networkInputs.Add("CurrentHandOurAggression", currentHandOurAggression / 6.0m);

      networkInputs.Add("CurrentRoundFirstAction", (currentRoundPlayerActions.Count == 0 ? 1 : 0));
      networkInputs.Add("CurrentRoundSecondAction", (currentRoundPlayerActions.Count == 1 ? 1 : 0));
      networkInputs.Add("CurrentRoundThirdPlusAction", (currentRoundPlayerActions.Count >= 2 ? 1 : 0));
      #endregion Hand and Round Actions
      //24 inputs

      #region WinRatio
      decimal probWin = 1.0m - infoStore.GetInfoValue(InfoType.WR_ProbOpponentHasBetterWRFIXED);
      networkInputs.Add("WRProbWin", ScaleContInput(probWin));
      #endregion
      //25 inputs

      #region EV and CheckPossibilitiy

      decimal bigBlindEVScaleAmount = 20;

      #region CallEV
      //The following EV assumes we can actually call the additionaCallAmount
      //We could cap the additionalCallAmount but we would then also not be able to win the total pot amount ;( again a minor error
      decimal actualCallCheckEV = (currentPotAmount * probWin) - ((1.0m - probWin) * additionalCallAmount);
      decimal scaledCallCheckEV = (actualCallCheckEV / currentDecision.Cache.BigBlind) / bigBlindEVScaleAmount;
      if (scaledCallCheckEV > 1)
        networkInputs.Add("ScaledCallCheckEV", 1);
      else if (scaledCallCheckEV < -1)
        networkInputs.Add("ScaledCallCheckEV", -1);
      else
        networkInputs.Add("ScaledCallCheckEV", scaledCallCheckEV);
      #endregion

      //We can raise if we have more stack than the current additional call amount
      //If we can't raise the raiseEV's get set to the callEV (which may be negative)
      bool raisePossible = (remainingPlayerStack > additionalCallAmount);

      decimal probAllOpponentFoldRaiseToCall = infoStore.GetInfoValue(InfoType.WR_RaiseToCallStealSuccessProb);
      decimal probAllOpponentFoldRaiseToSteal = (gameStagePreFlop == 1 ? 0 : infoStore.GetInfoValue(InfoType.WR_RaiseToStealSuccessProb));
      long[] activePlayerIds = currentDecision.Cache.getActivePlayerIds();

      #region RaiseToCallEV
      if (raisePossible)
      {
        decimal potAmountAssumingAllCall = currentPotAmount;

        //If we are preflop then it would be unrealistic to expect everyone to call
        //For Call we assume that if there are 8 players to act after the raise we get 2 calls
        //If there are 4 players to act after the raise we get 1 call
        //This is the same as dividing the number of activePlayers by 4
        if (gameStagePreFlop == 1)
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

        decimal scaledRaiseToCallEV = (actualRaiseToCallEV / currentDecision.Cache.BigBlind) / bigBlindEVScaleAmount;
        if (scaledRaiseToCallEV > 1)
          networkInputs.Add("ScaledRaiseToCallEV", 1);
        else if (scaledRaiseToCallEV < -1)
          networkInputs.Add("ScaledRaiseToCallEV", -1);
        else
          networkInputs.Add("ScaledRaiseToCallEV", scaledRaiseToCallEV);
      }
      else
        networkInputs.Add("ScaledRaiseToCallEV", networkInputs["ScaledCallCheckEV"]);
      #endregion

      #region RaiseToStealEV
      if (raisePossible)
      {
        decimal potAmountAssumingAllCall = currentPotAmount;

        //If we are preflop then it would be unrealistic to expect everyone to call
        //For steal we assume that if there are 8 players to act after the raise we get 1 call
        //If there are 4 players to act after the raise we get 0.5 call
        //This is the same as dividing the number of activePlayers by 8
        if (gameStagePreFlop == 1)
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
        decimal scaledRaiseToStealEV = (actualRaiseToStealEV / currentDecision.Cache.BigBlind) / bigBlindEVScaleAmount;
        if (scaledRaiseToStealEV > 1)
          networkInputs.Add("ScaledRaiseToStealEV", 1);
        else if (scaledRaiseToStealEV < -1)
          networkInputs.Add("ScaledRaiseToStealEV", -1);
        else
          networkInputs.Add("ScaledRaiseToStealEV", scaledRaiseToStealEV);
      }
      else
        networkInputs.Add("ScaledRaiseToStealEV", networkInputs["ScaledCallCheckEV"]);
      #endregion

      bool checkPossible = (infoStore.GetInfoValue(InfoType.BP_MinimumPlayAmount_Decimal) == infoStore.GetInfoValue(InfoType.BP_PlayerBetAmountCurrentRound_Decimal));
      networkInputs.Add("CheckPossible", Convert.ToDecimal(checkPossible));

      networkInputs.Add("CallAmount1BBOrLess", !checkPossible && ((decimal)infoStore.GetInfoValue(InfoType.BP_MinimumPlayAmount_Decimal) - (decimal)infoStore.GetInfoValue(InfoType.BP_PlayerBetAmountCurrentRound_Decimal)) <= currentDecision.Cache.BigBlind ? 1 : 0);
      #endregion
      //28 inputs

      #region PAP
      //probAllOpponentFoldRaiseToSteal == 0 preflop
      networkInputs.Add("ProbRaiseToStealSuccess", ScaleContInput(probAllOpponentFoldRaiseToSteal));
      #endregion
      //29 inputs

      #region PlayerAggression
      //All of V4 players get aggression defaults
      networkInputs.Add("AvgLiveOppPreFlopPlayFreq", ScaleContInput(infoStore.GetInfoValue(InfoType.AP_AvgLiveOppPreFlopPlayFreq_Double)));
      networkInputs.Add("AvgLiveOppPostFlopPlayFreq", ScaleContInput(infoStore.GetInfoValue(InfoType.AP_AvgLiveOppPostFlopPlayFreq_Double)));
      networkInputs.Add("AvgLiveOppCurrentRoundAggr", ScaleContInput(infoStore.GetInfoValue(InfoType.AP_AvgLiveOppCurrentRoundAggr_Double)));
      networkInputs.Add("AvgLiveOppCurrentRoundAggrAcc", ScaleContInput(infoStore.GetInfoValue(InfoType.AP_AvgLiveOppCurrentRoundAggrAcc_Double)));
      #endregion
      //33 inputs

      #endregion neuralInputs

      NNDataSource aiDecisionData = new NNDataSource(networkInputs.Values.ToArray(), NeuralAINNModelV6.Input_Neurons, true);
      //decisionLogStr = aiDecisionData.ToStringAdv(true);

      if (true)
      {
        //Get the network outputs
        double[] networkInputsArray = null;
        aiDecisionData.returnInput(ref networkInputsArray);
        double[] networkOutput = getPlayerNetworkPrediction(currentDecision.AiConfigStr, networkInputsArray);

        //Blank the non possible actions
        if (checkPossible)
        {
          //Blank fold
          networkOutput[0] = 0;
          //Blank call
          networkOutput[2] = 0;
        }
        else
          //Blank check
          networkOutput[1] = 0;

        //Get the bot decision
        NeuralAiDecision decision = new NeuralAiDecision(networkOutput, !Convert.ToBoolean(gameStageRiver) /*|| checkPossible*/, randomGen.NextDouble(), true);

        Debug.Print("Decision");

        //Can now check the fold decision for aces
        //if (decision.BotAction == 0 && networkInputs["HoleCardsAAPair"] == 1 && gameStagePreFlop == 1 && currentDecision.AiConfigStr.StartsWith("GeneticPokerPlayers"))
        //    currentDecision.Cache.SaveToDisk("", currentDecision.PlayerId + "-" + InfoProviderBase.CurrentJob.JobId + "-" + currentDecision.Cache.getCurrentHandId() + "-" + currentDecision.Cache.getMostRecentLocalIndex()[1]);

        if (decision.BotAction == 0)
          return new Play(PokerAction.Fold, 0, 0, currentDecision.Cache.getCurrentHandId(), currentDecision.PlayerId, decisionLogStr, 0);
        else if (decision.BotAction == 1)
          return new Play(PokerAction.Check, 0, 0, currentDecision.Cache.getCurrentHandId(), currentDecision.PlayerId, decisionLogStr, 0);
        else if (decision.BotAction == 2)
          return new Play(PokerAction.Call, additionalCallAmount, 0, currentDecision.Cache.getCurrentHandId(), currentDecision.PlayerId, decisionLogStr, 0);
        else if (decision.BotAction == 3)
          return new Play(PokerAction.Raise, raiseToCallAmountNew, 0, currentDecision.Cache.getCurrentHandId(), currentDecision.PlayerId, decisionLogStr, 0);
        else if (decision.BotAction == 4)
          return new Play(PokerAction.Raise, raiseToStealAmountNew, 0, currentDecision.Cache.getCurrentHandId(), currentDecision.PlayerId, decisionLogStr, 0);
        else
          throw new Exception("Something has gone wery wong!");
      }
    }

    private decimal ScaleContInput(decimal input)
    {
      return (input * 0.8m) + 0.1m;
    }
  }
}
