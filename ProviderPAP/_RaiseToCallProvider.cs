using System;
using System.Collections.Generic;
using System.Linq;
using PokerBot.Definitions;
using PokerBot.AI.InfoProviders;

namespace PokerBot.AI.ProviderPAP
{
  public partial class PlayerActionPredictionProvider
  {
    //protected volatile bool raiseToCall1PlayersTriggered;
    //protected volatile bool raiseToCall1PlayersCompleted;

    //protected volatile bool raiseToCall2PlayersTriggered;
    //protected volatile bool raiseToCall2PlayersCompleted;

    protected void raiseToCallAmount1PlayersWorker()
    {



#if logging
            logger.Debug("Start," + decisionRequest.Cache.getCurrentHandId() + ", " + decisionRequest.Cache.getMostRecentLocalIndex()[1]);
#endif

      try
      {
        CalculateRaiseToCallAmount1Players();


      }
      catch (Exception ex)
      {
        LogPAPError(ex, decisionRequest.Cache);

        decimal minimumCallAmount = infoStore.GetInfoValue(InfoType.BP_MinimumPlayAmount_Decimal);
        decimal lastAdditionalRaiseAmount = infoStore.GetInfoValue(InfoType.BP_LastAdditionalRaiseAmount);
        decimal minimumRaiseToAmount = (minimumCallAmount - lastAdditionalRaiseAmount) + (lastAdditionalRaiseAmount * 2);

        raiseToCallAmount1Players = minimumRaiseToAmount;
      }

#if logging
            logger.Debug("End," + decisionRequest.Cache.getCurrentHandId() + ", " + decisionRequest.Cache.getMostRecentLocalIndex()[1]);
#endif

    }

    private void CalculateRaiseToCallAmount1Players()
    {
      //byte[] activePositions = cache.getActivePositions(cache.getCurrentHandDetails().dealerPosition);
      byte[] activePositions = (from local in tempLocalPlayerCacheDict.Values select local.playerPosition).ToArray();
      byte[] positionsLeftToAct = tempLocalHandCache.positionsLeftToAct;
      PokerPlayerNNModelv1 playerActionNN = new PokerPlayerNNModelv1();

      List<byte> positionsRequiredToActAfterRaise = new List<byte>();
      positionsRequiredToActAfterRaise.AddRange(positionsLeftToAct);
      positionsRequiredToActAfterRaise.AddRange(activePositions.Except(positionsLeftToAct));
      positionsRequiredToActAfterRaise.Remove(decisionRequest.Cache.getPlayerPosition(decisionRequest.PlayerId));

      //Raise To Call Amount
      //What is the maximum the bot can raise to guarantee 1 or more callers
      decimal totalProbRequiredCallersAchieved;

      decimal numActivePlayers = infoStore.GetInfoValue(InfoType.GP_NumActivePlayers_Byte);
      decimal numUnactedPlayers = positionsRequiredToActAfterRaise.Count();
      decimal betsToCall = infoStore.GetInfoValue(InfoType.BP_BetsToCall_Byte);
      decimal minimumCallAmount = infoStore.GetInfoValue(InfoType.BP_MinimumPlayAmount_Decimal);
      decimal totalNumCalls = infoStore.GetInfoValue(InfoType.BP_TotalNumCalls_Byte);
      decimal totalNumRaises = infoStore.GetInfoValue(InfoType.BP_TotalNumRaises_Byte);
      decimal totalPotAmount = infoStore.GetInfoValue(InfoType.BP_TotalPotAmount_Decimal);
      decimal lastRaiseAmount = infoStore.GetInfoValue(InfoType.BP_LastAdditionalRaiseAmount);

      decimal bigBlind = decisionRequest.Cache.BigBlind;
      decimal littleBlind = decisionRequest.Cache.LittleBlind;

      #region MaximumRaiseAmount
      //Initialise the maximum raiseAmount to sensible values
      //These are more of a safety net than anything else.
      decimal maximumRaiseToAmount;
      if (totalPotAmount < bigBlind * 4)
        maximumRaiseToAmount = bigBlind * 8;
      else
        maximumRaiseToAmount = totalPotAmount;

      decimal maxRaiseAmountBlindMultiple = maximumRaiseToAmount / littleBlind;
      maximumRaiseToAmount = Math.Round(maxRaiseAmountBlindMultiple, 0, MidpointRounding.AwayFromZero) * littleBlind;
      #endregion

      #region MinimumRaiseAmount
      decimal minimumRaiseToAmount = (2 * lastRaiseAmount) + (minimumCallAmount - lastRaiseAmount);
      //If the minimumRaiseAmount is less than or equal to 2 blind and the pot is less than 4BB
      if (minimumRaiseToAmount <= 2 * bigBlind && totalPotAmount <= 4 * bigBlind)
      {
        if (randomNumbers[1] > 0.5)
          minimumRaiseToAmount = 3 * bigBlind;
        else
          minimumRaiseToAmount = 3.5m * bigBlind;
      }
      else if (minimumRaiseToAmount <= 2 * bigBlind && totalPotAmount > 4 * bigBlind)
        minimumRaiseToAmount = 0.6m * totalPotAmount;

      //Round the minimumRaiseToAmount UP TO (+1) the nearest blind multitple
      decimal minRaiseAmountBlindMultiple = minimumRaiseToAmount / littleBlind;
      minimumRaiseToAmount = (Math.Round(minRaiseAmountBlindMultiple, 0, MidpointRounding.AwayFromZero) + 1) * littleBlind;
      #endregion

      #region Calculation
      decimal raiseToCallAmount1Callers = minimumRaiseToAmount;
      decimal raiserStackAmount = decisionRequest.Cache.getPlayerStack(decisionRequest.PlayerId);

      List<decimal> raiseAmountsTried = new List<decimal>();
      bool nextRaiseAmountGreaterThanPrevious = false;

      do
      {
        totalProbRequiredCallersAchieved = 0;
        raiseToCallAmount1Callers = NextRaiseAmount(minimumRaiseToAmount, raiseAmountsTried, nextRaiseAmountGreaterThanPrevious);
        raiseAmountsTried.Add(raiseToCallAmount1Callers);

        if (raiseToCallAmount1Callers > raiserStackAmount)
        {
          raiseToCallAmount1Callers = raiserStackAmount;
          break;
        }
        else if (raiseToCallAmount1Callers < minimumRaiseToAmount)
        {
          raiseToCallAmount1Callers = minimumRaiseToAmount;
          break;
        }

        TraverseActionProbTree(ref totalProbRequiredCallersAchieved, 1, decisionRequest.Cache.getCurrentHandId(), positionsRequiredToActAfterRaise.ToArray(),
            0, PokerAction.Call, 1, 0, (byte)numActivePlayers, (byte)numUnactedPlayers, new List<byte> { }, (byte)(betsToCall + 1), raiseToCallAmount1Callers,
            (byte)totalNumCalls, (byte)(totalNumRaises + 1), totalPotAmount + raiseToCallAmount1Callers, playerActionNN);

        if (totalProbRequiredCallersAchieved > raiseToCallTheshold)
          nextRaiseAmountGreaterThanPrevious = true;
        else
          nextRaiseAmountGreaterThanPrevious = false;

      } while (raiseAmountsTried.Count() < 8);

      if (raiseToCallAmount1Callers > maximumRaiseToAmount)
        raiseToCallAmount1Callers = maximumRaiseToAmount;

      if (raiseToCallAmount1Callers < minimumRaiseToAmount)
        raiseToCallAmount1Callers = minimumRaiseToAmount;

      decimal raiseToCallAmountBlindMultiple = raiseToCallAmount1Callers / littleBlind;
      raiseToCallAmount1Callers = Math.Round(raiseToCallAmountBlindMultiple, 0, MidpointRounding.AwayFromZero) * littleBlind;

      decimal playerAlreadyBetAmount = infoStore.GetInfoValue(InfoType.BP_PlayerBetAmountCurrentRound_Decimal);

      if (raiseToCallAmount1Callers > (raiserStackAmount + playerAlreadyBetAmount) * 0.75m)
        raiseToCallAmount1Callers = raiserStackAmount + playerAlreadyBetAmount;

      raiseToCallAmount1Players = raiseToCallAmount1Callers;

      #endregion
    }

    protected void raiseToCallAmount2PlayersWorker()
    {

#if logging
            logger.Debug("Start," + decisionRequest.Cache.getCurrentHandId() + ", " + decisionRequest.Cache.getMostRecentLocalIndex()[1]);
#endif

      try
      {
        CalculateRaiseToCallAmount2Players();
      }
      catch (Exception ex)
      {
        LogPAPError(ex, decisionRequest.Cache);

        decimal minimumCallAmount = infoStore.GetInfoValue(InfoType.BP_MinimumPlayAmount_Decimal);
        decimal lastAdditionalRaiseAmount = infoStore.GetInfoValue(InfoType.BP_LastAdditionalRaiseAmount);
        decimal minimumRaiseToAmount = (minimumCallAmount - lastAdditionalRaiseAmount) + (lastAdditionalRaiseAmount * 2);

        raiseToCallAmount2Players = minimumRaiseToAmount;
      }

#if logging
            logger.Debug("End," + decisionRequest.Cache.getCurrentHandId() + ", " + decisionRequest.Cache.getMostRecentLocalIndex()[1]);
#endif

    }

    private void CalculateRaiseToCallAmount2Players()
    {
      //byte[] activePositions = cache.getActivePositions(cache.getCurrentHandDetails().dealerPosition);
      byte[] activePositions = (from local in tempLocalPlayerCacheDict.Values select local.playerPosition).ToArray();
      byte[] positionsLeftToAct = tempLocalHandCache.positionsLeftToAct;
      PokerPlayerNNModelv1 playerActionNN = new PokerPlayerNNModelv1();

      List<byte> positionsRequiredToActAfterRaise = new List<byte>();
      positionsRequiredToActAfterRaise.AddRange(positionsLeftToAct);
      positionsRequiredToActAfterRaise.AddRange(activePositions.Except(positionsLeftToAct));
      positionsRequiredToActAfterRaise.Remove(decisionRequest.Cache.getPlayerPosition(decisionRequest.PlayerId));

      //Raise To Call Amount
      //What is the maximum the bot can raise to guarantee 1 or more callers
      decimal totalProbRequiredCallersAchieved;

      decimal numActivePlayers = infoStore.GetInfoValue(InfoType.GP_NumActivePlayers_Byte);
      decimal numUnactedPlayers = positionsRequiredToActAfterRaise.Count();
      decimal betsToCall = infoStore.GetInfoValue(InfoType.BP_BetsToCall_Byte);
      decimal minimumCallAmount = infoStore.GetInfoValue(InfoType.BP_MinimumPlayAmount_Decimal);
      decimal totalNumCalls = infoStore.GetInfoValue(InfoType.BP_TotalNumCalls_Byte);
      decimal totalNumRaises = infoStore.GetInfoValue(InfoType.BP_TotalNumRaises_Byte);
      decimal totalPotAmount = infoStore.GetInfoValue(InfoType.BP_TotalPotAmount_Decimal);
      decimal lastRaiseAmount = infoStore.GetInfoValue(InfoType.BP_LastAdditionalRaiseAmount);

      decimal bigBlind = decisionRequest.Cache.BigBlind;
      decimal littleBlind = decisionRequest.Cache.LittleBlind;

      #region MaximumRaiseAmount
      //Initialise the maximum raiseAmount to sensible values
      //These are more of a safety net than anything else.
      decimal maximumRaiseToAmount;
      if (totalPotAmount < bigBlind * 4)
        maximumRaiseToAmount = bigBlind * 8;
      else
        maximumRaiseToAmount = totalPotAmount;

      decimal maxRaiseAmountBlindMultiple = maximumRaiseToAmount / littleBlind;
      maximumRaiseToAmount = Math.Round(maxRaiseAmountBlindMultiple, 0, MidpointRounding.AwayFromZero) * littleBlind;
      #endregion

      #region MinimumRaiseAmount
      decimal minimumRaiseToAmount = (2 * lastRaiseAmount) + (minimumCallAmount - lastRaiseAmount);
      //If the minimumRaiseAmount is less than or equal to 2 blind and the pot is less than 4BB
      if (minimumRaiseToAmount <= 2 * bigBlind && totalPotAmount <= 4 * bigBlind)
      {
        if (randomNumbers[1] > 0.5)
          minimumRaiseToAmount = 3 * bigBlind;
        else
          minimumRaiseToAmount = 3.5m * bigBlind;
      }
      else if (minimumRaiseToAmount <= 2 * bigBlind && totalPotAmount > 4 * bigBlind)
        minimumRaiseToAmount = 0.6m * totalPotAmount;

      //Round the minimumRaiseToAmount UPTO (+1) the nearest little blind multitple
      decimal minRaiseAmountBlindMultiple = minimumRaiseToAmount / littleBlind;
      minimumRaiseToAmount = (Math.Round(minRaiseAmountBlindMultiple, 0, MidpointRounding.AwayFromZero) + 1) * littleBlind;
      #endregion

      decimal raiseToCallAmount2Callers = minimumRaiseToAmount;
      decimal raiserStackAmount = decisionRequest.Cache.getPlayerStack(decisionRequest.PlayerId);

      List<decimal> raiseAmountsTried = new List<decimal>();
      bool nextRaiseAmountGreaterThanPrevious = false;

      if (numActivePlayers > 2)
      {
        do
        {
          totalProbRequiredCallersAchieved = 0;
          raiseToCallAmount2Callers = NextRaiseAmount(minimumRaiseToAmount, raiseAmountsTried, nextRaiseAmountGreaterThanPrevious);
          raiseAmountsTried.Add(raiseToCallAmount2Callers);

          if (raiseToCallAmount2Callers > raiserStackAmount)
          {
            raiseToCallAmount2Callers = raiserStackAmount;
            break;
          }
          else if (raiseToCallAmount2Callers < minimumRaiseToAmount)
          {
            raiseToCallAmount2Callers = minimumRaiseToAmount;
            break;
          }

          TraverseActionProbTree(ref totalProbRequiredCallersAchieved, 1, decisionRequest.Cache.getCurrentHandId(), positionsRequiredToActAfterRaise.ToArray(),
              0, PokerAction.Call, 2, 0, (byte)numActivePlayers, (byte)numUnactedPlayers, new List<byte> { }, (byte)(betsToCall + 1), raiseToCallAmount2Callers,
              (byte)totalNumCalls, (byte)(totalNumRaises + 1), totalPotAmount + raiseToCallAmount2Callers, playerActionNN);

          if (totalProbRequiredCallersAchieved > raiseToCallTheshold)
            nextRaiseAmountGreaterThanPrevious = true;
          else
            nextRaiseAmountGreaterThanPrevious = false;

        } while (raiseAmountsTried.Count() < 8);

        if (raiseToCallAmount2Callers > maximumRaiseToAmount)
          raiseToCallAmount2Callers = maximumRaiseToAmount;

        if (raiseToCallAmount2Callers < minimumRaiseToAmount)
          raiseToCallAmount2Callers = minimumRaiseToAmount;
      }
      else
        raiseToCallAmount2Callers = 0;

      decimal raiseToCallAmountBlindMultiple = raiseToCallAmount2Callers / littleBlind;
      raiseToCallAmount2Callers = Math.Round(raiseToCallAmountBlindMultiple, 0, MidpointRounding.AwayFromZero) * littleBlind;

      decimal playerAlreadyBetAmount = infoStore.GetInfoValue(InfoType.BP_PlayerBetAmountCurrentRound_Decimal);

      if (raiseToCallAmount2Callers > (raiserStackAmount + playerAlreadyBetAmount) * 0.75m)
        raiseToCallAmount2Callers = raiserStackAmount + playerAlreadyBetAmount;

      raiseToCallAmount2Players = raiseToCallAmount2Callers;
    }
  }
}
