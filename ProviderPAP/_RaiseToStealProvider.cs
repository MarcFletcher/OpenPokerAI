using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using PokerBot.Definitions;
using System.Diagnostics;
using System.Threading;
using PokerBot.AI.InfoProviders;

namespace PokerBot.AI.ProviderPAP
{
  public partial class PlayerActionPredictionProvider
  {
    //protected volatile bool raiseToStealTriggered;
    //protected volatile bool raiseToStealCompleted;

    /// <summary>
    /// Works out if there is some amount we can raise with a high probability of everyone else folding
    /// </summary>
    /// <param name="positionsLeftToAct"></param>
    /// <param name="activePositions"></param>
    /// <param name="successThreshold"></param>
    /// <param name="raiseToStealAmount"></param>
    /// <returns></returns>
    protected void RaiseToStealWorker()
    {
      //Raise To Steal Possible & Raise To Steal Amount - NOT THE SAME AS RAISE TO CALL DUE TO CONFIDENCE LEVELS
      //Is it possible to raise some amount that makes all players fold this round
      //The amount we have to raise in order to make everyone fold

      //Choose some starting raise amount
      //For each player who is left to act determine their action
      //For each player who folds modify the table values, if someone calls with a high level of confidence then increase the raise amount and start again
      //Keep increasing the raise amount until everyone folds WITH A HIGH LEVEL OF CONFIDENCE.
      //If we have hit our stack amount and there is still a reasonable chance someone will call Raise To Steal is not possible

      //Raise To Call Amount
      //What is the maximum the bot can raise to guarantee 1 or more callers

#if logging
            logger.Debug("Start," + decisionRequest.Cache.getCurrentHandId() + ", " + decisionRequest.Cache.getMostRecentLocalIndex()[1]);
#endif

      decimal minimumCallAmount = infoStore.GetInfoValue(InfoType.BP_MinimumPlayAmount_Decimal);
      decimal lastAdditionalRaiseAmount = infoStore.GetInfoValue(InfoType.BP_LastAdditionalRaiseAmount);
      decimal playerBetAmountCurrentRound = infoStore.GetInfoValue(InfoType.BP_PlayerBetAmountCurrentRound_Decimal);
      decimal playerRemainingStack = decisionRequest.Cache.getPlayerStack(decisionRequest.PlayerId);
      decimal minimumRaiseToAmount = (minimumCallAmount - lastAdditionalRaiseAmount) + (lastAdditionalRaiseAmount * 2);

      try
      {
        //If the minimumRaisetoAMount is greater than or equal to the maximum amount we
        //can put in the pot a raise is not possible.
        if (minimumCallAmount >= playerRemainingStack)
        {
          raiseToStealSuccessProb = 0;
          raiseToStealAmount = minimumRaiseToAmount;
#if logging
                    logger.Debug("RaiseToStealSuccesProb=0 as raising is not possible. Stack.");
#endif
        }
        //If all other players are all in stealing is not possible
        else if (decisionRequest.Cache.getAllInPositions().Length == decisionRequest.Cache.getActivePositions().Length - 1)
        {
          raiseToStealSuccessProb = 0;
          raiseToStealAmount = minimumRaiseToAmount;

#if logging
                    logger.Debug("RaiseToStealSuccesProb=0 as raising is not possible. All In.");
#endif
        }
        else
          CalculateRaiseToSteal();
      }
      catch (Exception ex)
      {
        LogPAPError(ex, decisionRequest.Cache);

        raiseToStealSuccessProb = 0;
        raiseToStealAmount = minimumRaiseToAmount;
      }

#if logging
            logger.Debug("End," + decisionRequest.Cache.getCurrentHandId() + ", " + decisionRequest.Cache.getMostRecentLocalIndex()[1]);
#endif

    }

    private void CalculateRaiseToSteal()
    {
      decimal totalProbRequiredCallersAchieved;
      decimal probRaiseToStealSuccess;
      PokerPlayerNNModelv1 playerActionNN = new PokerPlayerNNModelv1();

      //byte[] activePositions = cache.getActivePositions(cache.getCurrentHandDetails().dealerPosition);
      byte[] activePositions = (from local in tempLocalPlayerCacheDict.Values select local.playerPosition).ToArray();
      byte[] positionsLeftToAct = tempLocalHandCache.positionsLeftToAct;

      decimal raiserStackAmount = decisionRequest.Cache.getPlayerStack(decisionRequest.PlayerId);

      List<byte> positionsRequiredToActAfterRaise = new List<byte>();
      positionsRequiredToActAfterRaise.AddRange(positionsLeftToAct);
      positionsRequiredToActAfterRaise.AddRange(activePositions.Except(positionsLeftToAct));
      positionsRequiredToActAfterRaise.Remove(decisionRequest.Cache.getPlayerPosition(decisionRequest.PlayerId));

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
        maximumRaiseToAmount = 2 * totalPotAmount;

#if logging
            logger.Debug("maximumRaiseToAmount (PreScale) =" + maximumRaiseToAmount);
#endif

      decimal maxRaiseAmountBlindMultiple = maximumRaiseToAmount / littleBlind;
      maximumRaiseToAmount = Math.Round(maxRaiseAmountBlindMultiple, 0, MidpointRounding.AwayFromZero) * littleBlind;

#if logging
            logger.Debug("maximumRaiseToAmount (PostScale) =" + maximumRaiseToAmount);
#endif

      #endregion

      #region MinimumRaiseAmount
      decimal minimumRaiseToAmount = (2 * lastRaiseAmount) + (minimumCallAmount - lastRaiseAmount);
      //If the minimumRaiseAmount is less than or equal to 2 blind and the pot is less than 4BB
      if (minimumRaiseToAmount <= 2 * bigBlind && totalPotAmount <= 4 * bigBlind)
      {
        if (randomNumbers[0] > 0.5)
          minimumRaiseToAmount = 3 * bigBlind;
        else
          minimumRaiseToAmount = 4 * bigBlind;
      }
      else if (minimumRaiseToAmount <= 2 * bigBlind && totalPotAmount > 4 * bigBlind)
        minimumRaiseToAmount = 0.6m * totalPotAmount;

#if logging
            logger.Debug("minimumRaiseToAmount (PreScale) =" + minimumRaiseToAmount);
#endif

      //Round the minimumRaiseToAmount UPTO (+1) the nearest blind multitple
      decimal minRaiseAmountBlindMultiple = minimumRaiseToAmount / littleBlind;
      minimumRaiseToAmount = (Math.Round(minRaiseAmountBlindMultiple, 0, MidpointRounding.AwayFromZero) + 1) * littleBlind;

#if logging
            logger.Debug("minimumRaiseToAmount (PostScale) =" + minimumRaiseToAmount);
#endif

      #endregion

      //Start the raiseToCallAmount off at the calculated minimumRaiseAmount
      decimal raiseToCallAmount0Callers = minimumRaiseToAmount;
      List<decimal> raiseAmountsTried = new List<decimal>();
      bool nextRaiseAmountGreaterThanPrevious = false;

      do
      {
        totalProbRequiredCallersAchieved = 0;
        raiseToCallAmount0Callers = NextRaiseAmount(minimumRaiseToAmount, raiseAmountsTried, nextRaiseAmountGreaterThanPrevious);
        raiseAmountsTried.Add(raiseToCallAmount0Callers);

#if logging
                logger.Debug("Testing raise amount " + raiseToCallAmount0Callers);
#endif

        if (raiseToCallAmount0Callers > raiserStackAmount)
        {
          raiseToCallAmount0Callers = raiserStackAmount;
          break;
        }
        else if (raiseToCallAmount0Callers < minimumRaiseToAmount)
        {
          raiseToCallAmount0Callers = minimumRaiseToAmount;
          break;
        }

        TraverseActionProbTree(ref totalProbRequiredCallersAchieved, 1, decisionRequest.Cache.getCurrentHandId(), positionsRequiredToActAfterRaise.ToArray(),
            0, PokerAction.Call, 1, 0, (byte)numActivePlayers, (byte)numUnactedPlayers, new List<byte> { }, (byte)(betsToCall + 1), raiseToCallAmount0Callers,
            (byte)totalNumCalls, (byte)(totalNumRaises + 1), totalPotAmount + raiseToCallAmount0Callers, playerActionNN);

#if logging
                logger.Debug("    totalProbRequiredCallersAchieved = " + totalProbRequiredCallersAchieved);
#endif

        if (totalProbRequiredCallersAchieved > raiseToStealThreshold)
          nextRaiseAmountGreaterThanPrevious = true;
        else
          nextRaiseAmountGreaterThanPrevious = false;

      } while (raiseAmountsTried.Count() < 8);

      raiseToCallAmount0Callers *= 1.2m;


#if logging
            logger.Debug("Selected raise amount * 1.2 (PreScale) = " + raiseToCallAmount0Callers);
#endif

      //We now want to round up the raiseToStealAmount to nearest blind multiple
      decimal raiseToStealAmountBlindMultiple = raiseToCallAmount0Callers / littleBlind;
      raiseToCallAmount0Callers = Math.Round(raiseToStealAmountBlindMultiple, 0, MidpointRounding.AwayFromZero) * littleBlind;

#if logging
            logger.Debug("Selected raise amount * 1.2 (PostScale) = " + raiseToStealAmountBlindMultiple);
#endif

      if (raiseToCallAmount0Callers > maximumRaiseToAmount)
        raiseToCallAmount0Callers = maximumRaiseToAmount;

      decimal playerAlreadyBetAmount = infoStore.GetInfoValue(InfoType.BP_PlayerBetAmountCurrentRound_Decimal);

      if (raiseToCallAmount0Callers > (decisionRequest.Cache.getPlayerStack(decisionRequest.PlayerId) + playerAlreadyBetAmount) * 0.75m)
        raiseToCallAmount0Callers = decisionRequest.Cache.getPlayerStack(decisionRequest.PlayerId) + playerAlreadyBetAmount;

#if logging
            logger.Debug("Getting final steal success prob using = " + raiseToCallAmount0Callers);
#endif

      //Work out the final probability of having atleast 1 caller
      totalProbRequiredCallersAchieved = 0;
      TraverseActionProbTree(ref totalProbRequiredCallersAchieved, 1, decisionRequest.Cache.getCurrentHandId(), positionsRequiredToActAfterRaise.ToArray(),
          0, PokerAction.Call, 1, 0, (byte)numActivePlayers, (byte)numUnactedPlayers, new List<byte> { }, (byte)(betsToCall + 1), raiseToCallAmount0Callers,
          (byte)totalNumCalls, (byte)(totalNumRaises + 1), totalPotAmount + raiseToCallAmount0Callers, playerActionNN);

      probRaiseToStealSuccess = (1 - totalProbRequiredCallersAchieved);

#if logging
            logger.Debug("probRaiseToStealSuccess = " + probRaiseToStealSuccess + ", raiseToStealAmount = " + raiseToCallAmount0Callers);
#endif

      //Set the output values
      raiseToStealAmount = raiseToCallAmount0Callers;
      raiseToStealSuccessProb = probRaiseToStealSuccess;

      if (raiseToStealSuccessProb > 1 || raiseToStealSuccessProb < 0)
      {
        if (raiseToStealSuccessProb > 1 && Math.Round(raiseToStealSuccessProb, 4) == 1)
          raiseToStealSuccessProb = 1;
        else if (raiseToStealSuccessProb < 0 && Math.Round(raiseToStealSuccessProb, 4) == 0)
          raiseToStealSuccessProb = 0;
        else
          throw new Exception("raiseToStealSuccessProb must be between 0 and 1.");
      }
    }
  }
}
