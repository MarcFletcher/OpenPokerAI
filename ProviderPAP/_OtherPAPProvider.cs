using System;
using System.Collections.Generic;
using System.Linq;
using PokerBot.Definitions;
using PokerBot.AI.InfoProviders;

namespace PokerBot.AI.ProviderPAP
{
  public partial class PlayerActionPredictionProvider
  {
    /// <summary>
    /// Returns the probability someone left to act will raise if we check
    /// </summary>
    /// <param name="positionsLeftToAct"></param>
    /// <returns></returns>
    protected void probRaiseToBotCheckWorker()
    {
      //Raise To Bot Check Probability
      //Probability a player will raise to our check this round

      //For each player who is left to act determine their action
      //For each player who checks modify the table values, if someone raises store that probability
      //Then assume that player checks and keep going for all players
      //The total probability someone raises is (1 - Prob(Everyone Checking)) 

#if logging
            logger.Debug("Start," + decisionRequest.Cache.getCurrentHandId() + ", " + decisionRequest.Cache.getMostRecentLocalIndex()[1]);
#endif

      try
      {
        CalculateRaiseToBotCheck();
      }
      catch (Exception ex)
      {
        LogPAPError(ex, decisionRequest.Cache);
        raiseToBotCheckProb = 1;
      }

#if logging
            logger.Debug("End," + decisionRequest.Cache.getCurrentHandId() + ", " + decisionRequest.Cache.getMostRecentLocalIndex()[1]);
#endif

    }

    private void CalculateRaiseToBotCheck()
    {
      byte[] positionsLeftToAct = tempLocalHandCache.positionsLeftToAct;
      PokerPlayerNNModelv1 playerActionNN = new PokerPlayerNNModelv1();

      decimal totalProbRequiredRaisersAchieved = 0;

      decimal numActivePlayers = infoStore.GetInfoValue(InfoType.GP_NumActivePlayers_Byte);
      decimal numUnactedPlayers = infoStore.GetInfoValue(InfoType.GP_NumUnactedPlayers_Byte);
      decimal betsToCall = infoStore.GetInfoValue(InfoType.BP_BetsToCall_Byte);
      decimal minimumCallAmount = infoStore.GetInfoValue(InfoType.BP_MinimumPlayAmount_Decimal);
      decimal totalNumCalls = infoStore.GetInfoValue(InfoType.BP_TotalNumCalls_Byte);
      decimal totalNumRaises = infoStore.GetInfoValue(InfoType.BP_TotalNumRaises_Byte);
      decimal totalPotAmount = infoStore.GetInfoValue(InfoType.BP_TotalPotAmount_Decimal);

      if (positionsLeftToAct.Count() > 1 && betsToCall == 0)
      {
        TraverseActionProbTree(ref totalProbRequiredRaisersAchieved, 1, decisionRequest.Cache.getCurrentHandId(), positionsLeftToAct,
            1, PokerAction.Raise, 1, 0, (byte)numActivePlayers, (byte)(numUnactedPlayers - 1), new List<byte> { }, (byte)betsToCall, minimumCallAmount,
            (byte)totalNumCalls, (byte)totalNumRaises, totalPotAmount, playerActionNN);

        raiseToBotCheckProb = totalProbRequiredRaisersAchieved;
      }
      else
        raiseToBotCheckProb = 0; //If we are last to act the prob that someone will raise our call is 0 - i.e. they never get the opportunity too.

      //Need to handle possible crazy values and possible arithmetic overflows.
      if (raiseToBotCheckProb > 1 || raiseToBotCheckProb < 0)
      {
        if (raiseToBotCheckProb > 1 && Math.Round(raiseToBotCheckProb, 4) == 1)
          raiseToBotCheckProb = 1;
        else if (raiseToBotCheckProb < 0 && Math.Round(raiseToBotCheckProb, 4) == 0)
          raiseToBotCheckProb = 0;
        else
          throw new Exception("raiseToBotCheckProb must be between 0 and 1.");
      }
    }

    /// <summary>
    /// Returns the probability someone left to act will raise if we call
    /// </summary>
    /// <param name="positionsLeftToAct"></param>
    /// <returns></returns>
    protected void probRaiseToBotCallWorker()
    {
      //Raise To Bot Call Probability 
      //Probability a player will raise to our call this round

      //For each player who is left to act determine their action
      //For each player who calls modify the table values, if someone raises store that probability
      //Assume that player calls and keep going for all players
      //The total probability someone raises is (1 - Prob(Everyone Calling))



#if logging
            logger.Debug("Start," + decisionRequest.Cache.getCurrentHandId() + ", " + decisionRequest.Cache.getMostRecentLocalIndex()[1]);
#endif

      try
      {
        CalculateRaiseToBotCall();
      }
      catch (Exception ex)
      {
        LogPAPError(ex, decisionRequest.Cache);
        raiseToBotCallProb = 1;
      }

#if logging
            logger.Debug("End," + decisionRequest.Cache.getCurrentHandId() + ", " + decisionRequest.Cache.getMostRecentLocalIndex()[1]);
#endif

    }

    private void CalculateRaiseToBotCall()
    {

      byte[] positionsLeftToAct = tempLocalHandCache.positionsLeftToAct;
      decimal totalProbRequiredRaisersAchieved = 0;
      PokerPlayerNNModelv1 playerActionNN = new PokerPlayerNNModelv1();

      decimal numActivePlayers = infoStore.GetInfoValue(InfoType.GP_NumActivePlayers_Byte);
      decimal numUnactedPlayers = infoStore.GetInfoValue(InfoType.GP_NumUnactedPlayers_Byte);
      decimal betsToCall = infoStore.GetInfoValue(InfoType.BP_BetsToCall_Byte);
      decimal minimumCallAmount = decisionRequest.Cache.getMinimumPlayAmount();
      decimal totalNumCalls = infoStore.GetInfoValue(InfoType.BP_TotalNumCalls_Byte);
      decimal totalNumRaises = infoStore.GetInfoValue(InfoType.BP_TotalNumRaises_Byte);
      decimal totalPotAmount = infoStore.GetInfoValue(InfoType.BP_TotalPotAmount_Decimal);

      if (positionsLeftToAct.Count() > 1 && minimumCallAmount > 0)
      {
        TraverseActionProbTree(ref totalProbRequiredRaisersAchieved, 1, decisionRequest.Cache.getCurrentHandId(), positionsLeftToAct,
            1, PokerAction.Raise, 1, 0, (byte)numActivePlayers, (byte)(numUnactedPlayers - 1), new List<byte> { }, (byte)(betsToCall + 1), minimumCallAmount,
            (byte)(totalNumCalls + 1), (byte)totalNumRaises, totalPotAmount + minimumCallAmount, playerActionNN);

        raiseToBotCallProb = totalProbRequiredRaisersAchieved;
      }
      else
        raiseToBotCallProb = 0; //If we are last to act the prob that someone will raise our call is 0 - i.e. they never get the opportunity too.

      if (raiseToBotCallProb > 1 || raiseToBotCallProb < 0)
      {
        if (raiseToBotCallProb > 1 && Math.Round(raiseToBotCallProb, 4) == 1)
          raiseToBotCallProb = 1;
        else if (raiseToBotCallProb < 0 && Math.Round(raiseToBotCallProb, 4) == 0)
          raiseToBotCallProb = 0;
        else
          throw new Exception("raiseToBotCallProb must be between 0 and 1.");
      }
    }

    /// <summary>
    /// Works out the probability everyone left to act will fold if we call
    /// </summary>
    /// <param name="positionsLeftToAct"></param>
    /// <returns></returns>
    protected void probFoldToBotCallWorker()
    {

      //Fold To Bot Call Probability
      //Probability all other players left to act will fold if bot calls

      //For each player who is left to act determine their action
      //For each player who folds modify the table values, if someone calls store that probability
      //Then assume that player folds and keep going for all players
      //The total probability everyone folds is (1 - Prob(People Calling))

#if logging
            logger.Debug("Start," + decisionRequest.Cache.getCurrentHandId() + ", " + decisionRequest.Cache.getMostRecentLocalIndex()[1]);
#endif

      try
      {
        CalculateFoldToBotCall();
      }
      catch (Exception ex)
      {
        LogPAPError(ex, decisionRequest.Cache);
        foldToBotCallProb = 0;
      }

#if logging
            logger.Debug("End," + decisionRequest.Cache.getCurrentHandId() + ", " + decisionRequest.Cache.getMostRecentLocalIndex()[1]);
#endif


    }

    private void CalculateFoldToBotCall()
    {

      byte[] positionsLeftToAct = tempLocalHandCache.positionsLeftToAct;
      decimal minimumCallAmount = infoStore.GetInfoValue(InfoType.BP_MinimumPlayAmount_Decimal);
      PokerPlayerNNModelv1 playerActionNN = new PokerPlayerNNModelv1();

      if (positionsLeftToAct.Count() > 1 && minimumCallAmount > 0)
      {
        PlayerActionPrediction predictedAction;
        List<byte> simulatedFoldPositions = new List<byte>();
        decimal probFold = 1;

        decimal trueMinCallAmount = decisionRequest.Cache.getMinimumPlayAmount();
        decimal virtualCallAmount;

        //To make this accuracte we need to have some bet amount.
        if (trueMinCallAmount == 0)
          virtualCallAmount = decisionRequest.Cache.BigBlind;
        else
          virtualCallAmount = trueMinCallAmount;


        for (int i = 1; i < positionsLeftToAct.Count(); i++)
        {
          //If the current trueMinCallAmount = 0, then we simulate a raise our end
          if (trueMinCallAmount == 0)
          {
            predictedAction = PredictPlayerAction(decisionRequest.Cache.getCurrentHandId(), decisionRequest.Cache.getPlayerId(positionsLeftToAct.ElementAt(i)), infoStore.GetInfoValue(InfoType.GP_NumActivePlayers_Byte) - i + 1,
                 infoStore.GetInfoValue(InfoType.GP_NumUnactedPlayers_Byte) - i, simulatedFoldPositions, infoStore.GetInfoValue(InfoType.BP_BetsToCall_Byte) + 1, virtualCallAmount,
                 infoStore.GetInfoValue(InfoType.BP_TotalNumCalls_Byte), infoStore.GetInfoValue(InfoType.BP_TotalNumRaises_Byte) + 1, infoStore.GetInfoValue(InfoType.BP_TotalPotAmount_Decimal) + virtualCallAmount, false, playerActionNN);
          }
          else
          {
            predictedAction = PredictPlayerAction(decisionRequest.Cache.getCurrentHandId(), decisionRequest.Cache.getPlayerId(positionsLeftToAct.ElementAt(i)), infoStore.GetInfoValue(InfoType.GP_NumActivePlayers_Byte) - i + 1,
                 infoStore.GetInfoValue(InfoType.GP_NumUnactedPlayers_Byte) - i, simulatedFoldPositions, infoStore.GetInfoValue(InfoType.BP_BetsToCall_Byte) + 1, virtualCallAmount,
                 infoStore.GetInfoValue(InfoType.BP_TotalNumCalls_Byte) + 1, infoStore.GetInfoValue(InfoType.BP_TotalNumRaises_Byte), infoStore.GetInfoValue(InfoType.BP_TotalPotAmount_Decimal) + virtualCallAmount, false, playerActionNN);
          }

          if (predictedAction.PredictedAction == PokerAction.Fold)
            probFold *= 1 * predictedAction.Accuracy;
          else
            probFold *= 1 - (1 * predictedAction.Accuracy);
        }

        foldToBotCallProb = probFold;
      }
      else
        foldToBotCallProb = 0;

      if (foldToBotCallProb > 1 || foldToBotCallProb < 0)
      {
        if (foldToBotCallProb > 1 && Math.Round(foldToBotCallProb, 4) == 1)
          foldToBotCallProb = 1;
        else if (foldToBotCallProb < 0 && Math.Round(foldToBotCallProb, 4) == 0)
          foldToBotCallProb = 0;
        else
          throw new Exception("foldToBotCallProb must be between 0 and 1.");
      }
    }
  }
}
