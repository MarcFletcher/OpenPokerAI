using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using PokerBot.Definitions;
using PokerBot.AI.InfoProviders;
using System.Windows.Forms;
using System.Diagnostics;
using PokerBot.AI.Neural;
using PokerBot.AI.Neural.Version3;
using Encog;

namespace PokerBot.AI.Nerual.Version3
{

  internal class NeuralAIv3 : NeuralAIBase
  {
    NeuralAINNModelV3 neuralPokerAI = new NeuralAINNModelV3();

    public NeuralAIv3(AIRandomControl aiRandomControl)
        : base(aiRandomControl)
    {
      aiType = AIGeneration.NeuralV3;

      specificUpdateKey = new RequestedInfoKey(false);

      specificUpdateKey.SetInfoTypeRequired(InfoType.WR_CardsOnlyWinPercentage);

      specificUpdateKey.SetInfoTypeRequired(InfoType.GP_NumActivePlayers_Byte);
      specificUpdateKey.SetInfoTypeRequired(InfoType.GP_NumUnactedPlayers_Byte);
      specificUpdateKey.SetInfoTypeRequired(InfoType.GP_NumTableSeats_Byte);

      specificUpdateKey.SetInfoTypeRequired(InfoType.GP_GameStage_Byte);

      specificUpdateKey.SetInfoTypeRequired(InfoType.BP_MinimumPlayAmount_Decimal);
      specificUpdateKey.SetInfoTypeRequired(InfoType.BP_PlayerBetAmountCurrentRound_Decimal);

      specificUpdateKey.SetInfoTypeRequired(InfoType.PAP_RaiseToCallAmount_Amount);
      specificUpdateKey.SetInfoTypeRequired(InfoType.PAP_RaiseToStealAmount_Amount);
      specificUpdateKey.SetInfoTypeRequired(InfoType.PAP_RaiseToBotCheck_Prob);
      specificUpdateKey.SetInfoTypeRequired(InfoType.PAP_RaiseToBotCall_Prob);
      specificUpdateKey.SetInfoTypeRequired(InfoType.PAP_FoldToBotCall_Prob);
      specificUpdateKey.SetInfoTypeRequired(InfoType.PAP_RaiseToStealSuccess_Prob);

      specificUpdateKey.SetInfoTypeRequired(InfoType.BP_TotalNumRaises_Byte);
      specificUpdateKey.SetInfoTypeRequired(InfoType.BP_TotalNumCalls_Byte);
      specificUpdateKey.SetInfoTypeRequired(InfoType.BP_TotalNumChecks_Byte);

      specificUpdateKey.SetInfoTypeRequired(InfoType.BP_ImmediatePotOdds_Double);

      specificUpdateKey.SetInfoTypeRequired(InfoType.AP_AvgScaledOppRaiseFreq_Double);
      specificUpdateKey.SetInfoTypeRequired(InfoType.AP_AvgScaledOppCallFreq_Double);
      specificUpdateKey.SetInfoTypeRequired(InfoType.AP_AvgScaledOppPreFlopPlayFreq_Double);
    }

    protected override RequestedInfoKey GetUpdateKeyOrPreDecision()
    {
      return specificUpdateKey;
    }

    protected override Play GetDecision()
    {
      Dictionary<string, decimal> networkInputs = new Dictionary<string, decimal>();

      networkInputs.Add("ProbCardsWin", infoStore.GetInfoValue(InfoType.WR_CardsOnlyWinPercentage));
      networkInputs.Add("ProbCardsWinOpponentWin", (1 - infoStore.GetInfoValue(InfoType.WR_CardsOnlyWinPercentage)) / (infoStore.GetInfoValue(InfoType.GP_NumActivePlayers_Byte) - 1));

      decimal currentGameStage = (infoStore.GetInfoValue(InfoType.GP_GameStage_Byte));
      decimal preFlop;
      if (currentGameStage == 0)
        preFlop = 1;
      else
        preFlop = 0;

      networkInputs.Add("PreFlop", preFlop);

      decimal currentRoundBetAmount = currentDecision.Cache.getPlayerCurrentRoundBetAmount(currentDecision.PlayerId);
      decimal minCallAmount = infoStore.GetInfoValue(InfoType.BP_MinimumPlayAmount_Decimal);
      decimal scaledCallAmount = (minCallAmount - currentRoundBetAmount) / currentDecision.Cache.MaxStack;
      if (scaledCallAmount > 1)
        scaledCallAmount = 1;
      else if (scaledCallAmount < 0)
        scaledCallAmount = 0;

      networkInputs.Add("ScaledCallAmount", scaledCallAmount);

      decimal raiseToCallAmount = infoStore.GetInfoValue(InfoType.PAP_RaiseToCallAmount_Amount);
      decimal raiseToStealAmount = infoStore.GetInfoValue(InfoType.PAP_RaiseToStealAmount_Amount);

      decimal scaledRaiseToStealAmount = raiseToStealAmount / currentDecision.Cache.MaxStack;
      if (scaledRaiseToStealAmount > 1)
        scaledRaiseToStealAmount = 1;

      networkInputs.Add("ScaledRaiseToStealAmount", scaledRaiseToStealAmount);

      decimal raiseRatio = 0;
      decimal totalNumRaises = infoStore.GetInfoValue(InfoType.BP_TotalNumRaises_Byte);
      decimal totalNumCalls = infoStore.GetInfoValue(InfoType.BP_TotalNumCalls_Byte);
      decimal totalNumChecks = infoStore.GetInfoValue(InfoType.BP_TotalNumChecks_Byte);
      if (totalNumRaises + totalNumCalls + totalNumChecks > 0)
        raiseRatio = totalNumRaises / (totalNumRaises + totalNumCalls + totalNumChecks);

      networkInputs.Add("RaiseRatio", raiseRatio);
      networkInputs.Add("ImmediatePotOdds", infoStore.GetInfoValue(InfoType.BP_ImmediatePotOdds_Double));
      networkInputs.Add("UnactedRatio", infoStore.GetInfoValue(InfoType.GP_NumUnactedPlayers_Byte) / infoStore.GetInfoValue(InfoType.GP_NumActivePlayers_Byte));
      networkInputs.Add("ActiveRatio", infoStore.GetInfoValue(InfoType.GP_NumActivePlayers_Byte) / infoStore.GetInfoValue(InfoType.GP_NumTableSeats_Byte));

      networkInputs.Add("ProbRaiseToBotCheck", infoStore.GetInfoValue(InfoType.PAP_RaiseToBotCheck_Prob));
      networkInputs.Add("ProbRaiseToBotCall", infoStore.GetInfoValue(InfoType.PAP_RaiseToBotCall_Prob));
      networkInputs.Add("ProbFoldToBotCall", infoStore.GetInfoValue(InfoType.PAP_FoldToBotCall_Prob));
      networkInputs.Add("ProbRaiseToStealSuccess", infoStore.GetInfoValue(InfoType.PAP_RaiseToStealSuccess_Prob));

      networkInputs.Add("AvgScaledOppRaiseFreq", infoStore.GetInfoValue(InfoType.AP_AvgScaledOppRaiseFreq_Double));
      networkInputs.Add("AvgScaledOppCallFreq", infoStore.GetInfoValue(InfoType.AP_AvgScaledOppCallFreq_Double));
      networkInputs.Add("AvgScaledOppPreFlopPlayFreq", infoStore.GetInfoValue(InfoType.AP_AvgScaledOppPreFlopPlayFreq_Double));

      //probCardsWin, probCardsWinOpponentWin, preFlop, scaledCallAmount, scaledRaiseToStealAmount, 
      //raiseRatio, immediatePotOdds, unactedRatio, activeRatio, probRaiseToBotCheck, probRaiseToBotCall, 
      //probFoldToBotCall, probRaiseToStealSuccess, avgScaledOppRaiseFreq, avgScaledOppCallFreq, 
      //avgScaledOppPreFlopPlayFreq;

      NNDataSource aiDecisionData = new NNDataSource(networkInputs.Values.ToArray(), NeuralAINNModelV3.Input_Neurons);

      //decisionLogStr = aiDecisionData.ToString();

      //Get the network outputs
      double[] networkInputsArray = null;
      aiDecisionData.returnInput(ref networkInputsArray);
      double[] networkOutput = getPlayerNetworkPrediction(currentDecision.AiConfigStr, networkInputsArray);

      NeuralAiDecision decision = new NeuralAiDecision(networkOutput[0], networkOutput[1], networkOutput[2], networkOutput[3], networkOutput[4]);
      //Debug.Print("AI Decision - [{0}], [{1}], [{2}], [{3}], [{4}]", networkOutput[0], networkOutput[1], networkOutput[2], networkOutput[3], networkOutput[4]);

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

      //return new Play(PokerAction.NoAction, 0, 0, 0, playerId, decisionLogStr, aiDecisionData.AIDecisionStringType);
    }
  }
}
