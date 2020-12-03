using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows.Forms;
using PokerBot.Definitions;
using System.Threading;
using PokerBot.Database;
using PokerBot.AI.InfoProviders;

namespace PokerBot.BotGame
{

  public class ManualNeuralTrainingPokerGame : PokerGameBase
  {
    volatile bool manualDecisionMade;
    Play manualDecision;
    byte raiseType;
    protected long lastPlayerActionId;

    protected InfoCollection AIInfoStore;
    List<Control> neuralTrainingOutputFields;
    List<Control> neuralPlayerActionLog;

    public ManualNeuralTrainingPokerGame(PokerGameType gameType, databaseCacheClient clientCache, byte minNumTablePlayers, string[] playerNames, decimal startingStack, int maxHandsToPlay, int actionPause, List<Control> neuralTrainingOutputFields, List<Control> neuralPlayerActionLog)
        : base(gameType, clientCache, playerNames, startingStack, minNumTablePlayers, maxHandsToPlay, actionPause)
    {
      this.neuralTrainingOutputFields = neuralTrainingOutputFields;
      this.neuralPlayerActionLog = neuralPlayerActionLog;
    }

    public delegate void updateGUI(Control GUIControl, string message);
    public void updateGUIControl(Control GUIControl, string message)
    {
      GUIControl.Text = message;
    }

    //raiseType = 0 (raise to call), =1 (raise to steal), =2 (raise all in)
    public void SetManualDecision(Play decision, byte raiseType)
    {
      this.raiseType = raiseType;
      manualDecision = decision;
      manualDecisionMade = true;
    }

    protected override Play getPlayerDecision()
    {
      Play playerDecision;
      bool isBotPlayer;
      string aiLogDecisionAppendStr;

      neuralTrainingOutputFields.ElementAt(16).Invoke(new updateGUI(updateGUIControl), new object[] { neuralTrainingOutputFields.ElementAt(17), "" });
      aiLogDecisionAppendStr = "";

      isBotPlayer = clientCache.getPlayerDetails(clientCache.getPlayerId(currentActionPosition)).isBot;

      #region neuralTraining
      manualDecisionMade = false;

      DateTime startTime = DateTime.Now;
      playerDecision = aiManager.GetDecision(clientCache.getPlayerId(currentActionPosition), clientCache);
      String endTime = (DateTime.Now - startTime).TotalSeconds.ToString();

      neuralTrainingOutputFields.ElementAt(0).Invoke(new updateGUI(updateGUIControl), new object[] { neuralTrainingOutputFields.ElementAt(0), playerDecision.ToString() });
      //AIInfoStore = aiManager.GetPrimaryInfoStore;

      //Update all of the winform values
      updateNerualOutputFields(clientCache.getPlayerId(currentActionPosition));

      //Wait for the actual action, then do that!
      do
      {
        Thread.Sleep(100);
      } while (!manualDecisionMade);

      //We could get the AI to log the decision here
      //We need to get the actionId of this players action
      if (manualDecision.Action == PokerAction.Fold || manualDecision.Action == PokerAction.Check)
        aiLogDecisionAppendStr = playerDecision.AiDecisionStr + ", 1, 0, 0, 0, 0";
      else if (manualDecision.Action == PokerAction.Call)
        aiLogDecisionAppendStr = playerDecision.AiDecisionStr + ", 0, 1, 0, 0, 0";
      else if (manualDecision.Action == PokerAction.Raise && raiseType == 0)
        aiLogDecisionAppendStr = playerDecision.AiDecisionStr + ", 0, 0, 1, 0, 0";
      else if (manualDecision.Action == PokerAction.Raise && raiseType == 1)
        aiLogDecisionAppendStr = playerDecision.AiDecisionStr + ", 0, 0, 0, 1, 0";
      else if (manualDecision.Action == PokerAction.Raise && raiseType == 2)
        aiLogDecisionAppendStr = playerDecision.AiDecisionStr + ", 0, 0, 0, 0, 1";
      else
        throw new Exception("Something has fucked up!");

      decimal playerAlreadyBetAmount;
      clientCache.getPlayerCurrentRoundBetAmount(clientCache.getPlayerId(currentActionPosition), out playerAlreadyBetAmount);

      //We now need to take care of the raise problem when training.
      if (manualDecision.Amount >= clientCache.getPlayerStack(clientCache.getPlayerId(currentActionPosition)) + playerAlreadyBetAmount && manualDecision.Action == PokerAction.Raise && clientCache.getMinimumPlayAmount() >= manualDecision.Amount)
        playerDecision = new Play(PokerAction.Call, manualDecision.Amount - playerAlreadyBetAmount, manualDecision.DecisionTime, manualDecision.HandId, manualDecision.PlayerId);
      else
        playerDecision = manualDecision;

      #endregion

      //If this player is to be logged
      //Now get the last actionId for that player
      if (!(((CheckBox)neuralPlayerActionLog.ElementAt(currentActionPosition)).Checked))
      {
        //lastPlayerActionId = clientCache.getPlayerLastActionId(clientCache.getPlayerId(currentActionPosition));
        //databaseQueries.logAiDecision(clientCache.getCurrentHandId(), clientCache.getCurrentHandSeqIndex(), aiLogDecisionAppendStr, 1);
      }

      return playerDecision;
    }

    public void updateNerualOutputFields(long playerId)
    {

      decimal raiseRatio = 0;
      decimal totalPotAmount = AIInfoStore.GetInfoValue(InfoType.BP_TotalPotAmount_Decimal);
      decimal potRatio = AIInfoStore.GetInfoValue(InfoType.BP_PlayerMoneyInPot_Decimal) / totalPotAmount; // Own Money In Pot / Total Amount In Pot

      decimal immPotOdds = 1;
      decimal minCallAmount = AIInfoStore.GetInfoValue(InfoType.BP_MinimumPlayAmount_Decimal);

      if (minCallAmount > 0)
      {
        decimal playerAlreadyBet;
        clientCache.getPlayerCurrentRoundBetAmount(playerId, out playerAlreadyBet);

        immPotOdds = ((totalPotAmount / (minCallAmount - playerAlreadyBet)) / 10) - 0.1m;
        if (immPotOdds > 1)
          immPotOdds = 1;
        else if (immPotOdds < 0)
          immPotOdds = 0;
      }

      decimal totalNumRaises = AIInfoStore.GetInfoValue(InfoType.BP_TotalNumRaises_Byte);
      decimal totalNumCalls = AIInfoStore.GetInfoValue(InfoType.BP_TotalNumCalls_Byte);
      decimal lastActionRaise = 0;

      if (totalNumRaises + totalNumCalls > 0)
        raiseRatio = totalNumRaises / (totalNumRaises + totalNumCalls);

      PokerAction lastPlayerAction = (PokerAction)AIInfoStore.GetInfoValue(InfoType.BP_PlayerLastAction_Short);

      if (lastPlayerAction == PokerAction.Raise)
        lastActionRaise = 1;

      neuralTrainingOutputFields.ElementAt(1).Invoke(new updateGUI(updateGUIControl), new object[] { neuralTrainingOutputFields.ElementAt(1), AIInfoStore.GetInfoValue(InfoType.WR_CardsOnlyWinPercentage).ToString() });
      neuralTrainingOutputFields.ElementAt(2).Invoke(new updateGUI(updateGUIControl), new object[] { neuralTrainingOutputFields.ElementAt(2), raiseRatio.ToString() });
      neuralTrainingOutputFields.ElementAt(3).Invoke(new updateGUI(updateGUIControl), new object[] { neuralTrainingOutputFields.ElementAt(3), potRatio.ToString() });
      neuralTrainingOutputFields.ElementAt(4).Invoke(new updateGUI(updateGUIControl), new object[] { neuralTrainingOutputFields.ElementAt(4), AIInfoStore.GetInfoValue(InfoType.BP_RaisedLastRound_Bool).ToString() });
      neuralTrainingOutputFields.ElementAt(5).Invoke(new updateGUI(updateGUIControl), new object[] { neuralTrainingOutputFields.ElementAt(5), AIInfoStore.GetInfoValue(InfoType.BP_CalledLastRound_Bool).ToString() });
      neuralTrainingOutputFields.ElementAt(6).Invoke(new updateGUI(updateGUIControl), new object[] { neuralTrainingOutputFields.ElementAt(6), AIInfoStore.GetInfoValue(InfoType.IO_ImpliedPotOdds_Double).ToString() });
      neuralTrainingOutputFields.ElementAt(7).Invoke(new updateGUI(updateGUIControl), new object[] { neuralTrainingOutputFields.ElementAt(7), immPotOdds.ToString() });
      neuralTrainingOutputFields.ElementAt(8).Invoke(new updateGUI(updateGUIControl), new object[] { neuralTrainingOutputFields.ElementAt(8), lastActionRaise.ToString() });
      neuralTrainingOutputFields.ElementAt(9).Invoke(new updateGUI(updateGUIControl), new object[] { neuralTrainingOutputFields.ElementAt(9), AIInfoStore.GetInfoValue(InfoType.BP_LastRoundBetsToCall_Byte).ToString() });
      neuralTrainingOutputFields.ElementAt(10).Invoke(new updateGUI(updateGUIControl), new object[] { neuralTrainingOutputFields.ElementAt(10), AIInfoStore.GetInfoValue(InfoType.BP_PlayerMoneyInPot_Decimal).ToString() });
      neuralTrainingOutputFields.ElementAt(11).Invoke(new updateGUI(updateGUIControl), new object[] { neuralTrainingOutputFields.ElementAt(11), AIInfoStore.GetInfoValue(InfoType.PAP_RaiseToBotCheck_Prob).ToString() });
      neuralTrainingOutputFields.ElementAt(12).Invoke(new updateGUI(updateGUIControl), new object[] { neuralTrainingOutputFields.ElementAt(12), AIInfoStore.GetInfoValue(InfoType.PAP_RaiseToBotCall_Prob).ToString() });
      neuralTrainingOutputFields.ElementAt(13).Invoke(new updateGUI(updateGUIControl), new object[] { neuralTrainingOutputFields.ElementAt(13), AIInfoStore.GetInfoValue(InfoType.PAP_FoldToBotCall_Prob).ToString() });
      neuralTrainingOutputFields.ElementAt(14).Invoke(new updateGUI(updateGUIControl), new object[] { neuralTrainingOutputFields.ElementAt(14), AIInfoStore.GetInfoValue(InfoType.PAP_RaiseToStealSuccess_Prob).ToString() });
      neuralTrainingOutputFields.ElementAt(15).Invoke(new updateGUI(updateGUIControl), new object[] { neuralTrainingOutputFields.ElementAt(15), AIInfoStore.GetInfoValue(InfoType.PAP_RaiseToCallAmount_Amount).ToString() });
      neuralTrainingOutputFields.ElementAt(16).Invoke(new updateGUI(updateGUIControl), new object[] { neuralTrainingOutputFields.ElementAt(16), AIInfoStore.GetInfoValue(InfoType.PAP_RaiseToStealAmount_Amount).ToString() });
      neuralTrainingOutputFields.ElementAt(17).Invoke(new updateGUI(updateGUIControl), new object[] { neuralTrainingOutputFields.ElementAt(17), playerId.ToString() });
      neuralTrainingOutputFields.ElementAt(18).Invoke(new updateGUI(updateGUIControl), new object[] { neuralTrainingOutputFields.ElementAt(18), AIInfoStore.GetInfoValue(InfoType.WR_CardsOnlyWeightedPercentage).ToString() });
    }

  }

}
