using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using PokerBot.Definitions;
using PokerBot.AI.InfoProviders;
using System.Windows.Forms;
using System.Diagnostics;
using PokerBot.AI.Neural;
using PokerBot.AI.Neural.Version1;
using Encog;

namespace PokerBot.AI.Nerual.Version1
{

  internal class NeuralAIv1 : NeuralAIBase
  {
    NeuralAINNModelV1 neuralPokerAI = new NeuralAINNModelV1();

    public NeuralAIv1(AIRandomControl aiRandomControl)
        : base(aiRandomControl)
    {
      aiType = AIGeneration.NeuralV1;

      specificUpdateKey = new RequestedInfoKey(false);

      specificUpdateKey.SetInfoTypeRequired(InfoType.CP_HoleCardsAAPair_Bool);
      specificUpdateKey.SetInfoTypeRequired(InfoType.CP_HoleCardsKKPair_Bool);
      specificUpdateKey.SetInfoTypeRequired(InfoType.CP_HoleCardsAK_Bool);
      specificUpdateKey.SetInfoTypeRequired(InfoType.CP_HoleCardsOtherHighPair_Bool);
      specificUpdateKey.SetInfoTypeRequired(InfoType.CP_HoleCardsOtherLowPair_Bool);
      specificUpdateKey.SetInfoTypeRequired(InfoType.CP_HoleCardsTroubleHand_Bool);
      specificUpdateKey.SetInfoTypeRequired(InfoType.CP_HoleCardsMidConnector_Bool);
      specificUpdateKey.SetInfoTypeRequired(InfoType.CP_HoleCardsLowConnector_Bool);
      specificUpdateKey.SetInfoTypeRequired(InfoType.CP_HoleCardsSuited_Bool);
      specificUpdateKey.SetInfoTypeRequired(InfoType.CP_HoleCardsFlushDraw_Bool);

      specificUpdateKey.SetInfoTypeRequired(InfoType.CP_HoleCardsOuterStraightDrawWithHC_Bool);
      specificUpdateKey.SetInfoTypeRequired(InfoType.CP_HoleCardsInnerStraightDrawWithHC_Bool);
      specificUpdateKey.SetInfoTypeRequired(InfoType.CP_HoleCardsTopOrTwoPair_Bool);
      specificUpdateKey.SetInfoTypeRequired(InfoType.CP_HoleCardsAOrKInHand_Bool);
      specificUpdateKey.SetInfoTypeRequired(InfoType.CP_HoleCards3KindOrBetterMadeWithHC_Bool);
      specificUpdateKey.SetInfoTypeRequired(InfoType.CP_AOnBoard_Bool);
      specificUpdateKey.SetInfoTypeRequired(InfoType.CP_KOnBoard_Bool);
      specificUpdateKey.SetInfoTypeRequired(InfoType.CP_AKQToBoardRatio_Real);
      specificUpdateKey.SetInfoTypeRequired(InfoType.CP_FlushPossible_Bool);
      specificUpdateKey.SetInfoTypeRequired(InfoType.CP_StraightPossible_Bool);

      specificUpdateKey.SetInfoTypeRequired(InfoType.CP_TableStraightDraw_Bool);
      specificUpdateKey.SetInfoTypeRequired(InfoType.CP_TableFlushDraw_Bool);
      specificUpdateKey.SetInfoTypeRequired(InfoType.GP_NumPlayersDealtIn_Byte);
      specificUpdateKey.SetInfoTypeRequired(InfoType.GP_NumTableSeats_Byte);
      specificUpdateKey.SetInfoTypeRequired(InfoType.GP_NumActivePlayers_Byte);
      specificUpdateKey.SetInfoTypeRequired(InfoType.GP_NumUnactedPlayers_Byte);
      specificUpdateKey.SetInfoTypeRequired(InfoType.GP_GameStage_Byte);
      specificUpdateKey.SetInfoTypeRequired(InfoType.GP_DealerDistance_Byte);
      specificUpdateKey.SetInfoTypeRequired(InfoType.BP_ImmediatePotOdds_Double);
      specificUpdateKey.SetInfoTypeRequired(InfoType.BP_PlayerMoneyInPot_Decimal);

      specificUpdateKey.SetInfoTypeRequired(InfoType.BP_PlayerHandStartingStackAmount_Decimal);
      specificUpdateKey.SetInfoTypeRequired(InfoType.BP_TotalPotAmount_Decimal);
      specificUpdateKey.SetInfoTypeRequired(InfoType.BP_TotalNumRaises_Byte);
      specificUpdateKey.SetInfoTypeRequired(InfoType.BP_TotalNumCalls_Byte);
      specificUpdateKey.SetInfoTypeRequired(InfoType.BP_TotalNumChecks_Byte);
      specificUpdateKey.SetInfoTypeRequired(InfoType.BP_LastRoundBetsToCall_Byte);
      specificUpdateKey.SetInfoTypeRequired(InfoType.BP_CurrentCallAmountLarger4BB);
      specificUpdateKey.SetInfoTypeRequired(InfoType.BP_RaisedLastRound_Bool);
      specificUpdateKey.SetInfoTypeRequired(InfoType.BP_PlayerLastAction_Short);
      specificUpdateKey.SetInfoTypeRequired(InfoType.WR_CardsOnlyWinPercentage);

      specificUpdateKey.SetInfoTypeRequired(InfoType.WR_CardsOnlyOpponentWinPercentage);
      specificUpdateKey.SetInfoTypeRequired(InfoType.WR_CardsOnlyWinPercentageLastRoundChange);
      specificUpdateKey.SetInfoTypeRequired(InfoType.WR_CardsOnlyOpponentWinPercentage);
      specificUpdateKey.SetInfoTypeRequired(InfoType.PAP_RaiseToBotCheck_Prob);
      specificUpdateKey.SetInfoTypeRequired(InfoType.PAP_RaiseToBotCall_Prob);
      specificUpdateKey.SetInfoTypeRequired(InfoType.PAP_FoldToBotCall_Prob);
      specificUpdateKey.SetInfoTypeRequired(InfoType.PAP_RaiseToStealSuccess_Prob);
      specificUpdateKey.SetInfoTypeRequired(InfoType.PAP_RaiseToStealAmount_Amount);
      specificUpdateKey.SetInfoTypeRequired(InfoType.PAP_RaiseToCallAmount_Amount);

      specificUpdateKey.SetInfoTypeRequired(InfoType.BP_MinimumPlayAmount_Decimal);
      specificUpdateKey.SetInfoTypeRequired(InfoType.BP_PlayerBetAmountCurrentRound_Decimal);
      //specificUpdateKey.SetInfoTypeRequired(InfoType.BP_MinimumPlayAmount_Decimal);
      //specificUpdateKey.SetInfoTypeRequired(InfoType.BP_MinimumPlayAmount_Decimal);

      //specificUpdateKey.SetInfoTypeRequired(InfoType.BP_MinimumPlayAmount_Decimal);
      //specificUpdateKey.SetInfoTypeRequired(InfoType.BP_MinimumPlayAmount_Decimal);
      //specificUpdateKey.SetInfoTypeRequired(InfoType.BP_MinimumPlayAmount_Decimal);
    }

    protected override RequestedInfoKey GetUpdateKeyOrPreDecision()
    {
      return specificUpdateKey;
    }

    protected override Play GetDecision()
    {
      //List<double> networkInputs = new List<double>();
      Dictionary<string, decimal> networkInputs = new Dictionary<string, decimal>();
      //We need to write to the decision string here!

      #region neuralInputs

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

      //DealtInRatio, ActiveRatio and UnactedRatio
      networkInputs.Add("DealtInRatio", infoStore.GetInfoValue(InfoType.GP_NumPlayersDealtIn_Byte) / infoStore.GetInfoValue(InfoType.GP_NumTableSeats_Byte));
      networkInputs.Add("ActiveRatio", infoStore.GetInfoValue(InfoType.GP_NumActivePlayers_Byte) / infoStore.GetInfoValue(InfoType.GP_NumPlayersDealtIn_Byte));
      networkInputs.Add("UnactedRatio", infoStore.GetInfoValue(InfoType.GP_NumUnactedPlayers_Byte) / infoStore.GetInfoValue(InfoType.GP_NumActivePlayers_Byte));

      #region gameStage

      decimal gameStagePreFlop = 0;
      decimal gameStageFlop = 0;
      decimal gameStageTurnRiver = 0;

      if (infoStore.GetInfoValue(InfoType.GP_GameStage_Byte) == 0)
        gameStagePreFlop = 1;
      else if (infoStore.GetInfoValue(InfoType.GP_GameStage_Byte) == 1)
        gameStageFlop = 1;
      else
        gameStageTurnRiver = 1;

      networkInputs.Add("GameStagePreFlop", gameStagePreFlop);
      networkInputs.Add("GameStageFlop", gameStageFlop);
      networkInputs.Add("GameStageTurnRiver", gameStageTurnRiver);

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
      networkInputs.Add("TablePositionMid", tablePositionMid);
      networkInputs.Add("TablePositionLate", tablePositionLate);

      #endregion dealerDistance

      networkInputs.Add("ImmediatePotOdds", infoStore.GetInfoValue(InfoType.BP_ImmediatePotOdds_Double));
      networkInputs.Add("ImpliedPotOdds", 0); //(infoStore.GetInfoValue(InfoType.IO_ImpliedPotOdds_Double))

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

      decimal betsToCall = infoStore.GetInfoValue(InfoType.BP_BetsToCall_Byte);
      decimal betsToCall0 = 0;
      decimal betsToCall1 = 0;
      decimal betsToCall2Greater = 0;

      if (betsToCall >= 2m)
        betsToCall2Greater = 1;
      else if (betsToCall == 1m)
        betsToCall1 = 1;
      else if (betsToCall == 0)
        betsToCall0 = 1;
      else
        throw new Exception("This is impossible!!");

      networkInputs.Add("BetsToCall0", betsToCall0);
      networkInputs.Add("BetsToCall1", betsToCall1);
      networkInputs.Add("BetsToCall2Greater", betsToCall2Greater);

      decimal betsLastRound1Greater = 0;
      if (infoStore.GetInfoValue(InfoType.BP_LastRoundBetsToCall_Byte) > 0)
        betsLastRound1Greater = 1;

      networkInputs.Add("BetsLastRound1Greater", betsLastRound1Greater);
      networkInputs.Add("CurrentCallAmountLarger4BB", infoStore.GetInfoValue(InfoType.BP_CurrentCallAmountLarger4BB));

      decimal raisedLastRound = 0;
      if (infoStore.GetInfoValue(InfoType.BP_RaisedLastRound_Bool) == 1)
        raisedLastRound = 1;

      networkInputs.Add("RaisedLastRound", raisedLastRound);

      decimal lastActionRaise = 0;
      if (infoStore.GetInfoValue(InfoType.BP_PlayerLastAction_Short) == 9)
        lastActionRaise = 1;

      networkInputs.Add("LastActionRaise", lastActionRaise);

      #region winPercentage

      networkInputs.Add("probCardsWin", infoStore.GetInfoValue(InfoType.WR_CardsOnlyWinPercentage));
      networkInputs.Add("probCardsWeightedWin", infoStore.GetInfoValue(InfoType.WR_CardsOnlyWinPercentage));

      //networkInputs.Add("probCardsWinOpponentWin",infoStore.GetInfoValue(InfoType.WR_CardsOnlyOpponentWinPercentage));
      networkInputs.Add("probCardsWinOpponentWin", (1 - infoStore.GetInfoValue(InfoType.WR_CardsOnlyWinPercentage)) / (infoStore.GetInfoValue(InfoType.GP_NumActivePlayers_Byte) - 1));

      //networkInputs.Add("probCardsOpponentWeightedWin",infoStore.GetInfoValue(InfoType.WR_CardsOnlyOpponentWinPercentage));
      networkInputs.Add("probCardsOpponentWeightedWin", (1 - infoStore.GetInfoValue(InfoType.WR_CardsOnlyWinPercentage)) / (infoStore.GetInfoValue(InfoType.GP_NumActivePlayers_Byte) - 1));

      networkInputs.Add("probCardsOnlyWinPercentageLastRoundChange", infoStore.GetInfoValue(InfoType.WR_CardsOnlyWinPercentageLastRoundChange));

      #endregion winPercentage

      #region PAP

      networkInputs.Add("ProbRaiseToBotCheck", infoStore.GetInfoValue(InfoType.PAP_RaiseToBotCheck_Prob));
      networkInputs.Add("ProbRaiseToBotCall", infoStore.GetInfoValue(InfoType.PAP_RaiseToBotCall_Prob));
      networkInputs.Add("ProbFoldToBotCall", infoStore.GetInfoValue(InfoType.PAP_FoldToBotCall_Prob));
      networkInputs.Add("ProbRaiseToStealSuccess", infoStore.GetInfoValue(InfoType.PAP_RaiseToStealSuccess_Prob));

      #endregion PAP

      #endregion neuralInputs

      //We have 53 Inputs
      /* 
          AAPocketPair, KKPocketPair,AKPocket,OtherHighPocketPair,OtherLowPocketPair,HoleCardTrouble,midConnectorPocket,lowConnectorPocket,suitedPocket,holeCardsFlushDraw;
          holeCardsOuterStraightDraw,holeCardsInnerStraightDraw,holeCardsTopOrTwoPair,holeCardsAorK,holeCards3KindOrBetter, aceOnBoard,kingOnBoard,tableAKQRatio;
          tableFlushPossible, tableStraightPossible,tableStraightDraw, tableFlushDraw // 22 Card Types

          dealtInRatio, activeRatio,unactedRatio,stagePreFlop,stageFlop,stageTurnRiver,tablePositionEarly,tablePositionMid, tablePositionLate // 9 Game Types

          imPotOdds,impliedPotOdds, potCommitted, potRatio, raiseRatio, checkRatio,betsToCall0,betsToCall1,betsToCall2Greater,betsLastRound1Greater,callAmountGreaterThan4BB
          raisedLastRound, lastActionRaise // 13 Bet Types

          probCardsWin,probCardsWeightedWin,probCardsOpponentWin,probCardsWeightedOpponentWin,cardsWinPercentageLastRoundChange; // 5 Monto Carlo Types

          probRaiseToBotCheck,probRaiseToBotCall,probFoldToBotCall,probRaiseToStealSuccess // 4 Prediction Types
      */

      NNDataSource aiDecisionData = new NNDataSource(networkInputs.Values.ToArray(), NeuralAINNModelV1.Input_Neurons);

      //decisionLogStr = aiDecisionData.ToString();

      //Get the network outputs
      double[] networkInputsArray = null;
      aiDecisionData.returnInput(ref networkInputsArray);
      double[] networkOutput = getPlayerNetworkPrediction(currentDecision.AiConfigStr, networkInputsArray);

      NeuralAiDecision decision = new NeuralAiDecision(networkOutput[0], networkOutput[1], networkOutput[2], networkOutput[3], networkOutput[4]);
      //Debug.Print("AI Decision - [{0}], [{1}], [{2}], [{3}], [{4}]", networkOutput[0], networkOutput[1], networkOutput[2], networkOutput[3], networkOutput[4]);

      decimal raiseToCallAmount = (infoStore.GetInfoValue(InfoType.PAP_RaiseToCallAmount_Amount));
      decimal raiseToStealAmount = (infoStore.GetInfoValue(InfoType.PAP_RaiseToStealAmount_Amount));
      decimal minCallAmount = infoStore.GetInfoValue(InfoType.BP_MinimumPlayAmount_Decimal);
      decimal currentRoundBetAmount = currentDecision.Cache.getPlayerCurrentRoundBetAmount(currentDecision.PlayerId);
      decimal playerRemainingStackAmount = currentDecision.Cache.getPlayerStack(currentDecision.PlayerId);

      if (decision.BotAction == 0)
        return new Play(PokerAction.Fold, 0, 0, currentDecision.Cache.getCurrentHandId(), currentDecision.PlayerId, decisionLogStr, 0);
      else if (decision.BotAction == 1)
        return new Play(PokerAction.Call, currentDecision.Cache.getMinimumPlayAmount() - currentRoundBetAmount, 0, currentDecision.Cache.getCurrentHandId(), currentDecision.PlayerId, decisionLogStr, 0);
      else if (decision.BotAction == 2)
        return new Play(PokerAction.Raise, (decimal)raiseToCallAmount, 0, currentDecision.Cache.getCurrentHandId(), currentDecision.PlayerId, decisionLogStr, 0);
      else if (decision.BotAction == 3)
        return new Play(PokerAction.Raise, (decimal)raiseToStealAmount, 0, currentDecision.Cache.getCurrentHandId(), currentDecision.PlayerId, decisionLogStr, 0);
      else if (decision.BotAction == 4)
        return new Play(PokerAction.Raise, playerRemainingStackAmount + currentRoundBetAmount, 0, currentDecision.Cache.getCurrentHandId(), currentDecision.PlayerId, decisionLogStr, 0);
      else
        throw new Exception("Something has gone wery wong!");
    }
  }
}
