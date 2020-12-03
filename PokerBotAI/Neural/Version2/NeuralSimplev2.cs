using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using PokerBot.Definitions;
using PokerBot.AI.InfoProviders;
using System.Windows.Forms;
using System.Diagnostics;
using PokerBot.AI.Neural;
using PokerBot.AI.Neural.Version2;
using Encog;

namespace PokerBot.AI.Nerual.Version2
{

  internal class NeuralAIv2 : NeuralAIBase
  {
    NeuralAINNModelV2 neuralPokerAI = new NeuralAINNModelV2();

    public NeuralAIv2(AIRandomControl aiRandomControl)
        : base(aiRandomControl)
    {
      aiType = AIGeneration.NeuralV2;

      specificUpdateKey = new RequestedInfoKey(false);

      specificUpdateKey.SetInfoTypeRequired(InfoType.BP_MinimumPlayAmount_Decimal);
      specificUpdateKey.SetInfoTypeRequired(InfoType.BP_PlayerBetAmountCurrentRound_Decimal);
      specificUpdateKey.SetInfoTypeRequired(InfoType.PAP_RaiseToCallAmount_Amount);
      specificUpdateKey.SetInfoTypeRequired(InfoType.WR_CardsOnlyWinPercentage);
    }

    protected override RequestedInfoKey GetUpdateKeyOrPreDecision()
    {
      return specificUpdateKey;
    }

    protected override Play GetDecision()
    {
      //We need to write to the decision string here!
      Dictionary<string, decimal> networkInputs = new Dictionary<string, decimal>();

      networkInputs.Add("ProbCardsWin", infoStore.GetInfoValue(InfoType.WR_CardsOnlyWinPercentage));
      networkInputs.Add("ProbCardsWinOpponentWin", (1 - infoStore.GetInfoValue(InfoType.WR_CardsOnlyWinPercentage)) / (infoStore.GetInfoValue(InfoType.GP_NumActivePlayers_Byte) - 1));

      decimal currentGameStage = (infoStore.GetInfoValue(InfoType.GP_GameStage_Byte));
      decimal postFlop;
      if (currentGameStage == 0)
        postFlop = 0;
      else
        postFlop = 1;

      networkInputs.Add("PostFlop", postFlop);

      decimal currentRoundBetAmount = currentDecision.Cache.getPlayerCurrentRoundBetAmount(currentDecision.PlayerId);
      decimal minCallAmount = infoStore.GetInfoValue(InfoType.BP_MinimumPlayAmount_Decimal);
      decimal raiseToCallAmount = (infoStore.GetInfoValue(InfoType.PAP_RaiseToCallAmount_Amount));

      decimal scaledCurrentRoundMinimumPlayAmount = (minCallAmount / currentDecision.Cache.MaxStack);
      decimal scaledCurrentRoundPlayerBetAmount = (currentRoundBetAmount / currentDecision.Cache.MaxStack);

      if (scaledCurrentRoundMinimumPlayAmount > 1)
        scaledCurrentRoundMinimumPlayAmount = 1;
      if (scaledCurrentRoundPlayerBetAmount > 1)
        scaledCurrentRoundPlayerBetAmount = 1;

      networkInputs.Add("ScaledCurrentRoundMinimumPlayAmount", scaledCurrentRoundMinimumPlayAmount);
      networkInputs.Add("ScaledCurrentRoundPlayerBetAmount", scaledCurrentRoundPlayerBetAmount);

      //(probCardsWin, probCardsWinOpponentWin, postFlop, scaledCurrentRoundMinimumPlayAmount, scaledCurrentRoundPlayerBetAmount);
      NNDataSource aiDecisionData = new NNDataSource(networkInputs.Values.ToArray(), NeuralAINNModelV2.Input_Neurons);

      //decisionLogStr = aiDecisionData.ToString();

      //Get the network outputs
      double[] networkInputsArray = null;
      aiDecisionData.returnInput(ref networkInputsArray);
      double[] networkOutput = getPlayerNetworkPrediction(currentDecision.AiConfigStr, networkInputsArray);

      NeuralAiDecision decision = new NeuralAiDecision(networkOutput);
      //Debug.Print("AI Decision - [{0}], [{1}], [{2}], [{3}], [{4}]", networkOutput[0], networkOutput[1], networkOutput[2], networkOutput[3], networkOutput[4]);
      Play botAction;
      if (decision.BotAction == 0)
        botAction = new Play(PokerAction.Fold, 0, 0, currentDecision.Cache.getCurrentHandId(), currentDecision.PlayerId, decisionLogStr, 0);
      else if (decision.BotAction == 1)
        botAction = new Play(PokerAction.Call, currentDecision.Cache.getMinimumPlayAmount() - currentRoundBetAmount, 0, currentDecision.Cache.getCurrentHandId(), currentDecision.PlayerId, decisionLogStr, 0);
      else if (decision.BotAction == 2)
        botAction = new Play(PokerAction.Raise, (decimal)raiseToCallAmount, 0, currentDecision.Cache.getCurrentHandId(), currentDecision.PlayerId, decisionLogStr, 0);
      else
        throw new Exception("Something has gone wery wong!");

      //Call if already raised pre flop
      if (infoStore.GetInfoValue(InfoType.GP_GameStage_Byte) == 0 && botAction.Action == PokerAction.Raise)
      {
        //Split down into seperate IF to make sure there is not too much of a performance hit
        if (currentDecision.Cache.getPlayerCurrentRoundActions(currentDecision.PlayerId).Contains(PokerAction.Raise))
        {
          botAction.Action = PokerAction.Call;
          botAction.Amount = (decimal)(minCallAmount - currentRoundBetAmount);
        }
      }

      return botAction;
    }
  }
}
