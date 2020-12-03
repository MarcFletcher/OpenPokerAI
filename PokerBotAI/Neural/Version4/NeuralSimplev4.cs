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
using PokerBot.AI.Neural.Version4;
using ProviderAggression;

namespace PokerBot.AI.Nerual.Version4
{

  internal class NeuralAIv4 : NeuralAIBase
  {
    NeuralAINNModelV4 neuralPokerAI = new NeuralAINNModelV4();

#if logging
        //Added for testing, can be removed
        static object locker = new object();
        System.Security.Cryptography.MD5CryptoServiceProvider md5 = new System.Security.Cryptography.MD5CryptoServiceProvider();
        System.Runtime.Serialization.Formatters.Binary.BinaryFormatter bin = new System.Runtime.Serialization.Formatters.Binary.BinaryFormatter();
        System.IO.MemoryStream mem = new System.IO.MemoryStream();
#endif

    public NeuralAIv4(AIRandomControl aiRandomControl)
        : base(aiRandomControl)
    {
      aiType = AIGeneration.NeuralV4;

      //Setup this AI's specific update key
      specificUpdateKey = new RequestedInfoKey(false);
      specificUpdateKey.SetInfoTypeRequired(InfoType.CP_HoleCardsMatchedPlayability);
      specificUpdateKey.SetInfoTypeRequired(InfoType.GP_GameStage_Byte);
      specificUpdateKey.SetInfoTypeRequired(InfoType.GP_DealerDistance_Byte);
      specificUpdateKey.SetInfoTypeRequired(InfoType.GP_NumActivePlayers_Byte);
      specificUpdateKey.SetInfoTypeRequired(InfoType.GP_NumUnactedPlayers_Byte);
      specificUpdateKey.SetInfoTypeRequired(InfoType.BP_ImmediatePotOdds_Double);
      specificUpdateKey.SetInfoTypeRequired(InfoType.BP_PlayerMoneyInPot_Decimal);
      specificUpdateKey.SetInfoTypeRequired(InfoType.BP_PlayerHandStartingStackAmount_Decimal);
      specificUpdateKey.SetInfoTypeRequired(InfoType.BP_TotalPotAmount_Decimal);
      specificUpdateKey.SetInfoTypeRequired(InfoType.BP_TotalNumRaises_Byte);
      specificUpdateKey.SetInfoTypeRequired(InfoType.BP_TotalNumCalls_Byte);
      specificUpdateKey.SetInfoTypeRequired(InfoType.BP_TotalNumChecks_Byte);
      specificUpdateKey.SetInfoTypeRequired(InfoType.BP_MinimumPlayAmount_Decimal);
      specificUpdateKey.SetInfoTypeRequired(InfoType.BP_PlayerBetAmountCurrentRound_Decimal);
      specificUpdateKey.SetInfoTypeRequired(InfoType.BP_ScaledCallAmount_Double);

      specificUpdateKey.SetInfoTypeRequired(InfoType.WR_ProbOpponentHasBetterWRFIXED);
      specificUpdateKey.SetInfoTypeRequired(InfoType.WR_CardsOnlyWinPercentage);
      specificUpdateKey.SetInfoTypeRequired(InfoType.WR_CardsOnlyWinPercentageIndex);

      specificUpdateKey.SetInfoTypeRequired(InfoType.PAP_RaiseToStealSuccess_Prob);
      //specificUpdateKey.SetInfoTypeRequired(InfoType.AP_AvgScaledOppRaiseFreq_Double);
      //specificUpdateKey.SetInfoTypeRequired(InfoType.AP_AvgScaledOppCallFreq_Double);
      //specificUpdateKey.SetInfoTypeRequired(InfoType.AP_AvgScaledOppPreFlopPlayFreq_Double);
      specificUpdateKey.SetInfoTypeRequired(InfoType.BP_PlayerBetAmountCurrentRound_Decimal);
      specificUpdateKey.SetInfoTypeRequired(InfoType.PAP_RaiseToCallAmount_Amount);
      specificUpdateKey.SetInfoTypeRequired(InfoType.PAP_RaiseToStealAmount_Amount);

      //The following mostly used by PAP
      //specificUpdateKey.SetInfoTypeRequired(InfoType.GP_NumTableSeats_Byte);
      //specificUpdateKey.SetInfoTypeRequired(InfoType.CP_FlushPossible_Bool);
      //specificUpdateKey.SetInfoTypeRequired(InfoType.CP_StraightPossible_Bool);
      //specificUpdateKey.SetInfoTypeRequired(InfoType.CP_AOnBoard_Bool);
      //specificUpdateKey.SetInfoTypeRequired(InfoType.CP_KOnBoard_Bool);
      //specificUpdateKey.SetInfoTypeRequired(InfoType.CP_AKQToBoardRatio_Real);
      //specificUpdateKey.SetInfoTypeRequired(InfoType.BP_LastAdditionalRaiseAmount);
      //specificUpdateKey.SetInfoTypeRequired(InfoType.BP_BetsToCall_Byte);

      //The fixing metrics
      specificUpdateKey.SetInfoTypeRequired(InfoType.WR_ProbOpponentHasBetterWRFIXED);
      specificUpdateKey.SetInfoTypeRequired(InfoType.WR_RaiseToCallAmount);
      specificUpdateKey.SetInfoTypeRequired(InfoType.WR_RaiseToStealAmount);
      specificUpdateKey.SetInfoTypeRequired(InfoType.WR_RaiseToCallStealSuccessProb);
      specificUpdateKey.SetInfoTypeRequired(InfoType.WR_RaiseToStealSuccessProb);
    }

    protected override Dictionary<InfoType, string> GetInfoUpdateConfigs()
    {
      defaultInfoTypeUpdateConfigs = new Dictionary<InfoType, string>();

      if (currentDecision.AiConfigStr.StartsWith("FixedNeuralPlayers\\ChouDist") || currentDecision.AiConfigStr == "FixedNeuralPlayers\\neuralV4_Bob.eNN")
        defaultInfoTypeUpdateConfigs.Add(InfoType.IS_CURRENT_GENETIC, "TRUE");
      //Quick high performance match to see if it might be a fixed neural player
      else if (currentDecision.AiConfigStr.Substring(0, 1) == "F")
      {
        if (!currentDecision.AiConfigStr.StartsWith("FixedNeuralPlayers\\ShangDist"))
        {
          if (currentDecision.AiConfigStr == "FixedNeuralPlayers\\neuralV4_Chavin.eNN")
            defaultInfoTypeUpdateConfigs.Add(InfoType.CP_HoleCardsMatchedPlayability, "Chavin");
          else if (currentDecision.AiConfigStr == "FixedNeuralPlayers\\neuralV4_Marc.eNN")
            defaultInfoTypeUpdateConfigs.Add(InfoType.CP_HoleCardsMatchedPlayability, "Marc");
          else if (currentDecision.AiConfigStr == "FixedNeuralPlayers\\neuralV4_Ailwyn.eNN")
            defaultInfoTypeUpdateConfigs.Add(InfoType.CP_HoleCardsMatchedPlayability, "Ailwyn");
          else if (currentDecision.AiConfigStr == "FixedNeuralPlayers\\neuralV4_Aztec.eNN")
            defaultInfoTypeUpdateConfigs.Add(InfoType.CP_HoleCardsMatchedPlayability, "Aztec");
          else if (currentDecision.AiConfigStr == "FixedNeuralPlayers\\neuralV4_Mixtec.eNN")
            defaultInfoTypeUpdateConfigs.Add(InfoType.CP_HoleCardsMatchedPlayability, "Mixtec");
          else if (currentDecision.AiConfigStr == "FixedNeuralPlayers\\neuralV4_Barra.eNN")
            defaultInfoTypeUpdateConfigs.Add(InfoType.CP_HoleCardsMatchedPlayability, "Barra");
          else if (currentDecision.AiConfigStr == "FixedNeuralPlayers\\neuralV4_Olmec.eNN")
            defaultInfoTypeUpdateConfigs.Add(InfoType.CP_HoleCardsMatchedPlayability, "Olmec");
          else if (currentDecision.AiConfigStr == "FixedNeuralPlayers\\neuralV4_Moche.eNN")
            defaultInfoTypeUpdateConfigs.Add(InfoType.CP_HoleCardsMatchedPlayability, "Moche");
        }
      }
      else if (currentDecision.AiConfigStr.Substring(0, 1) == "G")
      {
        //We add a genetic flag to the raiseToCall so that we go all in on any third raise in any round
        defaultInfoTypeUpdateConfigs.Add(InfoType.IS_CURRENT_GENETIC, "TRUE");
      }

      return defaultInfoTypeUpdateConfigs;
    }

    //We now use a list to do the disable as it's a little cleaner as we continue to add more and more disabled AI's
    List<string> aggressionInfoDisbleList = new List<string>() { "Ailwyn", "Marc", "Aztec", "Mixtec", "Barra", "Olmec", "Moche", "Chavin" };

    protected override RequestedInfoKey GetUpdateKeyOrPreDecision()
    {
      return specificUpdateKey;
    }

    protected override Play GetDecision()
    {
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

      #region NewRaiseAmounts
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

      #region EV and CheckPossibilitiy

      decimal probWin = 1.0m - infoStore.GetInfoValue(InfoType.WR_ProbOpponentHasBetterWRFIXED);
      decimal bigBlindEVScaleAmount = 20;

      #region CallEV
      //The following EV assumes we can actually call the additionaCallAmount
      //We could cap the additionalCallAmount but we would then also not be able to win the total pot amount ;( again a minor error
      decimal actualCallCheckEV = (currentPotAmount * probWin) - ((1.0m - probWin) * additionalCallAmount);
      decimal scaledCallCheckEV = (actualCallCheckEV / currentDecision.Cache.BigBlind) / bigBlindEVScaleAmount;
      //if (scaledCallCheckEV > 1) networkInputs.Add("ScaledCallCheckEV", 1);
      //else if (scaledCallCheckEV < -1) networkInputs.Add("ScaledCallCheckEV", -1);
      //else networkInputs.Add("ScaledCallCheckEV", scaledCallCheckEV);
      #endregion

      //We can raise if we have more stack than the current additional call amount
      //If we can't raise the raiseEV's get set to the callEV (which may be negative)
      bool raisePossible = (remainingPlayerStack > additionalCallAmount);

      decimal probAllOpponentFoldRaiseToCall = infoStore.GetInfoValue(InfoType.WR_RaiseToCallStealSuccessProb);
      decimal probAllOpponentFoldRaiseToSteal = (currentDecision.Cache.getBettingRound() == 0 ? 0 : infoStore.GetInfoValue(InfoType.WR_RaiseToStealSuccessProb));
      long[] activePlayerIds = currentDecision.Cache.getActivePlayerIds();

      #region RaiseToCallEV
      decimal scaledRaiseToCallEV = 0;
      if (raisePossible)
      {
        decimal potAmountAssumingAllCall = currentPotAmount;

        //If we are preflop then it would be unrealistic to expect everyone to call
        //For Call we assume that if there are 8 players to act after the raise we get 2 calls
        //If there are 4 players to act after the raise we get 1 call
        //This is the same as dividing the number of activePlayers by 4
        if (currentDecision.Cache.getBettingRound() == 0)
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
        //if (scaledRaiseToCallEV > 1) networkInputs.Add("ScaledRaiseToCallEV", 1);
        //else if (scaledRaiseToCallEV < -1) networkInputs.Add("ScaledRaiseToCallEV", -1);
        //else networkInputs.Add("ScaledRaiseToCallEV", scaledRaiseToCallEV);
      }
      #endregion

      #region RaiseToStealEV
      decimal scaledRaiseToStealEV = 0;
      if (raisePossible)
      {
        decimal potAmountAssumingAllCall = currentPotAmount;

        //If we are preflop then it would be unrealistic to expect everyone to call
        //For steal we assume that if there are 8 players to act after the raise we get 1 call
        //If there are 4 players to act after the raise we get 0.5 call
        //This is the same as dividing the number of activePlayers by 8
        if (currentDecision.Cache.getBettingRound() == 0)
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
        //if (scaledRaiseToStealEV > 1) networkInputs.Add("ScaledRaiseToStealEV", 1);
        //else if (scaledRaiseToStealEV < -1) networkInputs.Add("ScaledRaiseToStealEV", -1);
        //else networkInputs.Add("ScaledRaiseToStealEV", scaledRaiseToStealEV);
      }
      #endregion
      #endregion

      Dictionary<string, decimal> networkInputs = new Dictionary<string, decimal>();

      #region neuralInputs

      //Card info types
      //networkInputs.Add("HoleCardsAAPair", infoStore.GetInfoValue(InfoType.CP_HoleCardsAAPair_Bool));
      //networkInputs.Add("HoleCardsKKPair", infoStore.GetInfoValue(InfoType.CP_HoleCardsKKPair_Bool));
      //networkInputs.Add("HoleCardsAKPair", infoStore.GetInfoValue(InfoType.CP_HoleCardsAK_Bool));
      //networkInputs.Add("HoleCardsOtherHighPair", infoStore.GetInfoValue(InfoType.CP_HoleCardsOtherHighPair_Bool));
      //networkInputs.Add("HoleCardsOtherLowPair", infoStore.GetInfoValue(InfoType.CP_HoleCardsOtherLowPair_Bool));
      //networkInputs.Add("HoleCardsTroubleHand", infoStore.GetInfoValue(InfoType.CP_HoleCardsTroubleHand_Bool));
      //networkInputs.Add("HoleCardsSuited", infoStore.GetInfoValue(InfoType.CP_HoleCardsSuited_Bool));

      networkInputs.Add("HoleCardPlayability", infoStore.GetInfoValue(InfoType.CP_HoleCardsMatchedPlayability));

      /*
      networkInputs.Add("HoleCardsMidConnector", infoStore.GetInfoValue(InfoType.CP_HoleCardsMidConnector_Bool));
      networkInputs.Add("HoleCardsLowConnector", infoStore.GetInfoValue(InfoType.CP_HoleCardsLowConnector_Bool));

      networkInputs.Add("HoleCardsFlushDraw", infoStore.GetInfoValue(InfoType.CP_HoleCardsFlushDraw_Bool));
      networkInputs.Add("HoleCardsOuterStraightDraw", infoStore.GetInfoValue(InfoType.CP_HoleCardsOuterStraightDrawWithHC_Bool));
      networkInputs.Add("HoleCardsInnerStraightDraw", infoStore.GetInfoValue(InfoType.CP_HoleCardsInnerStraightDrawWithHC_Bool));
      networkInputs.Add("HoleCardsTopOrTwoPair", infoStore.GetInfoValue(InfoType.CP_HoleCardsTopOrTwoPair_Bool));
      networkInputs.Add("HoleCardsAorKinHand", infoStore.GetInfoValue(InfoType.CP_HoleCardsAOrKInHand_Bool));
      networkInputs.Add("HoleCards3KindOrBetter", infoStore.GetInfoValue(InfoType.CP_HoleCards3KindOrBetterMadeWithHC_Bool));
      networkInputs.Add("AonBoard", infoStore.GetInfoValue(InfoType.CP_AOnBoard_Bool));
      networkInputs.Add("KonBoard", infoStore.GetInfoValue(InfoType.CP_KOnBoard_Bool));
      networkInputs.Add("AKQRatio", infoStore.GetInfoValue(InfoType.CP_AKQToBoardRatio_Real));

      networkInputs.Add("FlushPossible", infoStore.GetInfoValue(InfoType.CP_FlushPossible_Bool));
      networkInputs.Add("StraightPossible", infoStore.GetInfoValue(InfoType.CP_StraightPossible_Bool));
      networkInputs.Add("TableStraightDraw", infoStore.GetInfoValue(InfoType.CP_TableStraightDraw_Bool));
      networkInputs.Add("TableFlushDraw", infoStore.GetInfoValue(InfoType.CP_TableFlushDraw_Bool));
      */

      //DealtInRatio, ActiveRatio and UnactedRatio
      //networkInputs.Add("DealtInRatio", infoStore.GetInfoValue(InfoType.GP_NumPlayersDealtIn_Byte) / infoStore.GetInfoValue(InfoType.GP_NumTableSeats_Byte));
      //networkInputs.Add("ActiveRatio", infoStore.GetInfoValue(InfoType.GP_NumActivePlayers_Byte) / infoStore.GetInfoValue(InfoType.GP_NumPlayersDealtIn_Byte));
      //networkInputs.Add("UnactedRatio", infoStore.GetInfoValue(InfoType.GP_NumUnactedPlayers_Byte) / infoStore.GetInfoValue(InfoType.GP_NumActivePlayers_Byte));

      #region gameStage

      decimal gameStagePreFlop = 0;
      decimal gameStageRiver = 0;

      if (infoStore.GetInfoValue(InfoType.GP_GameStage_Byte) == 0)
        gameStagePreFlop = 1;
      else if (infoStore.GetInfoValue(InfoType.GP_GameStage_Byte) == 3)
        gameStageRiver = 1;

      networkInputs.Add("GameStagePreFlop", gameStagePreFlop);
      networkInputs.Add("GameStageRiver", gameStageRiver);

      #endregion gameStage

      #region dealerDistance

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
      //networkInputs.Add("TablePositionMid", tablePositionMid);
      networkInputs.Add("TablePositionLate", tablePositionLate);


      networkInputs.Add("FirstToAct", (infoStore.GetInfoValue(InfoType.GP_NumActivePlayers_Byte) == infoStore.GetInfoValue(InfoType.GP_NumUnactedPlayers_Byte) ? 1 : 0));
      networkInputs.Add("LastToAct", (infoStore.GetInfoValue(InfoType.GP_NumUnactedPlayers_Byte) == 1 ? 1 : 0));

      #endregion dealerDistance

      networkInputs.Add("ImmediatePotOdds", infoStore.GetInfoValue(InfoType.BP_ImmediatePotOdds_Double));
      //networkInputs.Add("ImpliedPotOdds", 0); //(infoStore.GetInfoValue(InfoType.IO_ImpliedPotOdds_Double))

      decimal potCommitted = 0;
      if (infoStore.GetInfoValue(InfoType.BP_PlayerMoneyInPot_Decimal) > infoStore.GetInfoValue(InfoType.BP_PlayerHandStartingStackAmount_Decimal) / 2)
        potCommitted = 1;

      networkInputs.Add("PotCommitted", potCommitted);
      networkInputs.Add("PotRatio", infoStore.GetInfoValue(InfoType.BP_PlayerMoneyInPot_Decimal) / infoStore.GetInfoValue(InfoType.BP_TotalPotAmount_Decimal));

      #region raise & check ratio

      decimal raiseRatio = 0;
      decimal checkRatio = 0;
      decimal totalNumRaises = infoStore.GetInfoValue(InfoType.BP_TotalNumRaises_Byte);
      decimal totalNumCalls = infoStore.GetInfoValue(InfoType.BP_TotalNumCalls_Byte);
      decimal totalNumChecks = infoStore.GetInfoValue(InfoType.BP_TotalNumChecks_Byte);
      if (totalNumRaises + totalNumCalls + totalNumChecks > 0)
      {
        raiseRatio = totalNumRaises / (totalNumRaises + totalNumCalls + totalNumChecks);
        checkRatio = totalNumChecks / (totalNumRaises + totalNumCalls + totalNumChecks);
      }

      networkInputs.Add("RaiseRatio", raiseRatio);
      networkInputs.Add("CheckRatio", checkRatio);

      #endregion raise & check ratio

      bool checkPossible = (infoStore.GetInfoValue(InfoType.BP_MinimumPlayAmount_Decimal) == infoStore.GetInfoValue(InfoType.BP_PlayerBetAmountCurrentRound_Decimal));
      networkInputs.Add("CheckPossible", Convert.ToDecimal(checkPossible));

      /*
      double betsToCall = infoStore.GetInfoValue(InfoType.BP_BetsToCall_Byte);
      double betsToCall0 = 0;
      double betsToCall1 = 0;
      double betsToCall2Greater = 0;

      if (betsToCall >= 2)
          betsToCall2Greater = 1;
      else if (betsToCall == 1)
          betsToCall1 = 1;
      else if (betsToCall == 0)
          betsToCall0 = 1;
      else
          throw new Exception("This is impossible!!");

      networkInputs.Add("BetsToCall0", betsToCall0);
      networkInputs.Add("BetsToCall1", betsToCall1);
      networkInputs.Add("BetsToCall2Greater", betsToCall2Greater);

      double betsLastRound1Greater = 0;
      if (infoStore.GetInfoValue(InfoType.BP_LastRoundBetsToCall_Byte) > 0)
          betsLastRound1Greater = 1;

      networkInputs.Add("BetsLastRound1Greater", betsLastRound1Greater);
      */

      /*
      double raisedLastRound = 0;
      if (infoStore.GetInfoValue(InfoType.BP_RaisedLastRound_Bool) == 1)
          raisedLastRound = 1;

      networkInputs.Add("RaisedLastRound", raisedLastRound);

      double lastActionRaise = 0;
      if (infoStore.GetInfoValue(InfoType.BP_PlayerLastAction_Short) == 9)
          lastActionRaise = 1;

      networkInputs.Add("LastActionRaise", lastActionRaise);
      */

      //call and raise amounts
      networkInputs.Add("ScaledCallAmount", infoStore.GetInfoValue(InfoType.BP_ScaledCallAmount_Double));
      networkInputs.Add("CallAmountBB", ((decimal)infoStore.GetInfoValue(InfoType.BP_MinimumPlayAmount_Decimal) - (decimal)infoStore.GetInfoValue(InfoType.BP_PlayerBetAmountCurrentRound_Decimal)) == currentDecision.Cache.BigBlind ? 1 : 0);
      //networkInputs.Add("ScaledRaiseToCallAmount", BetsProvider.ScaleRaiseAmount(cache.BigBlind, infoStore.GetInfoValue(InfoType.PAP_RaiseToCallAmount_Amount) - infoStore.GetInfoValue(InfoType.BP_MinimumPlayAmount_Decimal), infoStore.GetInfoValue(InfoType.BP_TotalPotAmount_Decimal)));
      //networkInputs.Add("ScaledRaiseToStealAmount", BetsProvider.ScaleRaiseAmount(cache.BigBlind, infoStore.GetInfoValue(InfoType.PAP_RaiseToStealAmount_Amount) - infoStore.GetInfoValue(InfoType.BP_MinimumPlayAmount_Decimal), infoStore.GetInfoValue(InfoType.BP_TotalPotAmount_Decimal)));

      //WinRatio
      //networkInputs.Add("WRProbCardsWin", infoStore.GetInfoValue(InfoType.WR_CardsOnlyWinPercentage));
      //networkInputs.Add("WRProbCardsOpponentWin", infoStore.GetInfoValue(InfoType.WR_CardsOnlyOpponentWinPercentage));
      networkInputs.Add("WRProbBeat", infoStore.GetInfoValue(InfoType.WR_ProbOpponentHasBetterWRFIXED));
      //networkInputs.Add("WRLastRoundWRChange", infoStore.GetInfoValue(InfoType.WR_CardsOnlyWinPercentageLastRoundChange));
      networkInputs.Add("WRHandIndexTop10Hands", (infoStore.GetInfoValue(InfoType.WR_CardsOnlyWinPercentageIndex) < 0.01m ? 1 : 0));

      //networkInputs.Add("ProbRaiseToBotCheck",infoStore.GetInfoValue(InfoType.PAP_RaiseToBotCheck_Prob));
      //networkInputs.Add("ProbRaiseToBotCall",infoStore.GetInfoValue(InfoType.PAP_RaiseToBotCall_Prob));
      //networkInputs.Add("ProbFoldToBotCall",infoStore.GetInfoValue(InfoType.PAP_FoldToBotCall_Prob));
      networkInputs.Add("ProbRaiseToStealSuccess", infoStore.GetInfoValue(InfoType.PAP_RaiseToStealSuccess_Prob));

      ////////////////////////////////////////////////////////////////////
      ///////// MASSIVE HACK BEGIN ///////////////////////////////////////
      ////////////////////////////////////////////////////////////////////
      //We need to disable the aggression information for fixed marc and ailwyn bots
      //if (currentDecision.RequiredInfoTypeUpdateConfigs.ContainsKey(InfoType.CP_HoleCardsMatchedPlayability))
      //{
      //    string holeCardsMatchedPlayability = currentDecision.RequiredInfoTypeUpdateConfigs[InfoType.CP_HoleCardsMatchedPlayability];
      //    if (aggressionInfoDisbleList.Contains(holeCardsMatchedPlayability))
      //    {
      //        networkInputs.Add("AvgScaledOppPreFlopPlayFreq", AggressionProvider.DEFAULT_pFREQ_PREFLOP);
      //        if (gameStagePreFlop == 1)
      //        {
      //            networkInputs.Add("AvgScaledOppRaiseFreq", AggressionProvider.DEFAULT_rFREQ_PREFLOP);
      //            networkInputs.Add("AvgScaledOppCallFreq", AggressionProvider.DEFAULT_cFREQ_PREFLOP);
      //        }
      //        else
      //        {
      //            networkInputs.Add("AvgScaledOppRaiseFreq", AggressionProvider.DEFAULT_rFREQ_POSTFLOP);
      //            networkInputs.Add("AvgScaledOppCallFreq", AggressionProvider.DEFAULT_cFREQ_POSTFLOP);
      //        }
      //    }
      //    else
      //    {
      //        networkInputs.Add("AvgScaledOppRaiseFreq", infoStore.GetInfoValue(InfoType.AP_AvgScaledOppRaiseFreq_Double));
      //        networkInputs.Add("AvgScaledOppCallFreq", infoStore.GetInfoValue(InfoType.AP_AvgScaledOppCallFreq_Double));
      //        networkInputs.Add("AvgScaledOppPreFlopPlayFreq", infoStore.GetInfoValue(InfoType.AP_AvgScaledOppPreFlopPlayFreq_Double));
      //    }
      //}
      //else
      //{
      //    networkInputs.Add("AvgScaledOppRaiseFreq", infoStore.GetInfoValue(InfoType.AP_AvgScaledOppRaiseFreq_Double));
      //    networkInputs.Add("AvgScaledOppCallFreq", infoStore.GetInfoValue(InfoType.AP_AvgScaledOppCallFreq_Double));
      //    networkInputs.Add("AvgScaledOppPreFlopPlayFreq", infoStore.GetInfoValue(InfoType.AP_AvgScaledOppPreFlopPlayFreq_Double));
      //}

      //All of V4 players get aggression defaults
      networkInputs.Add("AvgScaledOppPreFlopPlayFreq", AggressionProvider.DEFAULT_pFREQ_PREFLOP);
      if (gameStagePreFlop == 1)
      {
        networkInputs.Add("AvgScaledOppRaiseFreq", AggressionProvider.DEFAULT_rFREQ_PREFLOP);
        networkInputs.Add("AvgScaledOppCallFreq", AggressionProvider.DEFAULT_cFREQ_PREFLOP);
      }
      else
      {
        networkInputs.Add("AvgScaledOppRaiseFreq", AggressionProvider.DEFAULT_rFREQ_POSTFLOP);
        networkInputs.Add("AvgScaledOppCallFreq", AggressionProvider.DEFAULT_cFREQ_POSTFLOP);
      }
      ////////////////////////////////////////////////////////////////////
      ///////// MASSIVE HACK END /////////////////////////////////////////
      ////////////////////////////////////////////////////////////////////

      #endregion neuralInputs

      NNDataSource aiDecisionData = new NNDataSource(networkInputs.Values.ToArray(), NeuralAINNModelV4.Input_Neurons);

      //decisionLogStr = aiDecisionData.ToString();

      //decimal playerRemainingStackAmount = currentDecision.Cache.getPlayerStack(currentDecision.PlayerId);
      decimal raiseToCallAmount = (decimal)infoStore.GetInfoValue(InfoType.PAP_RaiseToCallAmount_Amount);
      decimal raiseToStealAmount = (decimal)infoStore.GetInfoValue(InfoType.PAP_RaiseToStealAmount_Amount);

      if (true)
      {
        //Get the network outputs
        double[] networkInputsArray = null;
        aiDecisionData.returnInput(ref networkInputsArray);
        double[] networkOutput = getPlayerNetworkPrediction(currentDecision.AiConfigStr, networkInputsArray);
        //Debug.Print(Math.Round(networkOutput[0], 2) + ", " + Math.Round(networkOutput[1], 2) + ", " + Math.Round(networkOutput[2], 2) + ", " + Math.Round(networkOutput[3], 2) + ", " + Math.Round(networkOutput[4], 2));

        NeuralAiDecision decision;
        //We use the network outputs and do raises amounts slightly differently for the current genetic distribution
        if (currentDecision.RequiredInfoTypeUpdateConfigs.ContainsKey(InfoType.IS_CURRENT_GENETIC))
        {
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
          decision = new NeuralAiDecision(networkOutput, !Convert.ToBoolean(gameStageRiver) || checkPossible, randomGen.NextDouble(), true);

          if (decision.BotAction >= 3)
          {
            //If we have set a config for PAP_RaiseToCallAmount_Amount then we limit raises per round to three
            if (currentDecision.Cache.getPlayerCurrentRoundActions(currentDecision.PlayerId).Count(entry => entry == PokerAction.Raise) >= 2)
            {
              raiseToCallAmount = remainingPlayerStack + (decimal)infoStore.GetInfoValue(InfoType.BP_PlayerBetAmountCurrentRound_Decimal);
              raiseToStealAmount = raiseToCallAmount;
            }
          }
        }
        else
        {
          //We enable stochastisim on all but the river
          decision = new NeuralAiDecision(networkOutput, !Convert.ToBoolean(gameStageRiver), randomGen.NextDouble(), false);
          //Debug.Print("AI Decision - [{0}], [{1}], [{2}], [{3}], [{4}]", networkOutput[0], networkOutput[1], networkOutput[2], networkOutput[3], networkOutput[4]);

          //Lets put a little double check here for the AI outputs
          if (checkPossible && decision.BotAction == 0)
            decision.BotAction = 1;
          else if (!checkPossible && decision.BotAction == 1)
            decision.BotAction = 0;
        }

        //Need to work out WTF is going on!!
#if logging
                lock (locker)
                {
                    if (currentDecision.Cache.getCurrentHandId() == 880)
                    {
                        mem = new System.IO.MemoryStream();
                        bin.Serialize(mem, Encog.Neural.Networks.Structure.NetworkCODEC.NetworkToArray(PlayerNetworkCopy(currentDecision.AiConfigStr)));
                        string networkHash = BitConverter.ToString(md5.ComputeHash(mem.ToArray()));

                        using (System.IO.StreamWriter sw = new System.IO.StreamWriter("aiDecisionsLog.csv", true))
                            sw.WriteLine(currentDecision.Cache.getCurrentHandId() + ", " + currentDecision.Cache.getMostRecentLocalIndex()[1] + ", " + currentDecision.PlayerId + ", " + decision.StochasticDouble + ", ," + aiDecisionData.ToString() + ", ," + Math.Round(networkOutput[0], 2) + ", " + Math.Round(networkOutput[1], 2) + ", " + Math.Round(networkOutput[2], 2) + ", " + Math.Round(networkOutput[3], 2) + ", " + Math.Round(networkOutput[4], 2) + ", " + decision.BotAction + ",," + infoStore.GetInfoValue(InfoType.PAP_RaiseToCallAmount_Amount) + "," + infoStore.GetInfoValue(InfoType.PAP_RaiseToStealAmount_Amount) + ",," + networkHash + ",," + currentDecision.Cache.CurrentHandHash(true));
                        //sw.WriteLine(cache.getCurrentHandId() + ", " + +cache.getMostRecentLocalIndex() + ", " + playerId + ", " + cache.CurrentHandHash(true) + ",," + aiDecisionData.ToString());
                        //sw.WriteLine(currentDecision.Cache.getCurrentHandId() + ", " + currentDecision.Cache.getMostRecentLocalIndex() + ", " + currentDecision.PlayerId + ", " + currentDecision.Cache.CurrentHandHash(true) + ",," + networkInputs["WRProbBeat"]);
                    }
                }
#endif

        //Decision override to prevent horrificly wrong decision
        if (decision.BotAction > 1 && scaledCallCheckEV < -0.2M && scaledRaiseToCallEV < -0.2M && scaledRaiseToStealEV < -0.2M)
        {
          if (checkPossible)
            decision.BotAction = 1;
          else
            decision.BotAction = 0;
        }

        if (decision.BotAction == 0)
          return new Play(PokerAction.Fold, 0, 0, currentDecision.Cache.getCurrentHandId(), currentDecision.PlayerId, decisionLogStr, 0);
        else if (decision.BotAction == 1)
          return new Play(PokerAction.Check, 0, 0, currentDecision.Cache.getCurrentHandId(), currentDecision.PlayerId, decisionLogStr, 0);
        else if (decision.BotAction == 2)
          return new Play(PokerAction.Call, currentDecision.Cache.getMinimumPlayAmount() - (decimal)infoStore.GetInfoValue(InfoType.BP_PlayerBetAmountCurrentRound_Decimal), 0, currentDecision.Cache.getCurrentHandId(), currentDecision.PlayerId, decisionLogStr, 0);
        else if (decision.BotAction == 3)
          return new Play(PokerAction.Raise, raiseToCallAmount, 0, currentDecision.Cache.getCurrentHandId(), currentDecision.PlayerId, decisionLogStr, 0);
        else if (decision.BotAction == 4)
          return new Play(PokerAction.Raise, raiseToStealAmount, 0, currentDecision.Cache.getCurrentHandId(), currentDecision.PlayerId, decisionLogStr, 0);
        //else if (decision.BotAction == 4)
        //    return new Play(PokerAction.Raise, playerRemainingStackAmount + (decimal)infoStore.GetInfoValue(InfoType.BP_PlayerBetAmountCurrentRound_Decimal), 0, cache.getCurrentHandId(), playerId, decisionLogStr, 0);
        else
          throw new Exception("Something has gone wery wong!");
      }

      //return new Play(PokerAction.Fold, 0, 0, cache.getCurrentHandId(), playerId, decisionLogStr, 0);
    }
  }
}
