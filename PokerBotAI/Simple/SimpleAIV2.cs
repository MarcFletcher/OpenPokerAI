using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using PokerBot.Definitions;
using PokerBot.AI.InfoProviders;

namespace PokerBot.AI
{
  internal class SimpleAIV2 : SimpleAIBase
  {
    public SimpleAIV2(AIRandomControl aiRandomControl)
        : base(aiRandomControl)
    {
      aiType = AIGeneration.SimpleV2;

      specificUpdateKey = new RequestedInfoKey(false);

      specificUpdateKey.SetInfoTypeRequired(InfoType.WR_CardsOnlyWinPercentage);
      specificUpdateKey.SetInfoTypeRequired(InfoType.WR_CardsOnlyWinRatio);

      specificUpdateKey.SetInfoTypeRequired(InfoType.BP_TotalPotAmount_Decimal);
    }

    protected override RequestedInfoKey GetUpdateKeyOrPreDecision()
    {
      return specificUpdateKey;
    }

    protected override void GetAIDecision()
    {
      if (bettingRound == 0)
        v2preFlopAIDecision();
      else
        v2postFlopAIDecision();
    }

    protected void v2preFlopAIDecision()
    {
      //We are only going to raise with the top couple of hands, i.e. WR > 2.5, raise 4xBB

      //We want to call any bet upto 20xBB depending on the current WR
      //Max Call Amount = 1 + (WR - PlayWR)*(19/(4.1-PlayWR))
      //This is the maximum amount we can call based upon our current WR
      decimal maxCallAmount = (1 + ((currentWinRatio - botPlayWRThreshold) * (9 / (2.5m - botPlayWRThreshold)))) * currentDecision.Cache.BigBlind;

      //The difference between current winRatio and play threshold.
      //The higher the positive difference the better our hand
      winRatioDifference = currentWinRatio - botPlayWRThreshold;

      //If we have a good hand and 4*BigBlind is greater than 2* currentRoundMinimumPlayAmount we want to raise
      if (currentWinRatio > 2.5m && (4 * currentDecision.Cache.BigBlind > 2 * currentRoundMinimumPlayAmount))
      {
        botAction = PokerAction.Raise;
        //betAmount = 5 * currentDecision.Cache.BigBlind;

        decimal totalPotAmount = infoStore.GetInfoValue(InfoType.BP_TotalPotAmount_Decimal);
        //If the pot is unraised randomly raise either 4 or 3 times the bigblind
        if (currentRoundMinimumPlayAmount == currentDecision.Cache.BigBlind)
        {
          if (randomGen.NextDouble() < 0.75)
            betAmount = 3 * currentDecision.Cache.BigBlind;
          else
            betAmount = 4 * currentDecision.Cache.BigBlind;
        }
        else
        {
          //If the pot is raised then the raise is based on the pot size
          if (randomGen.NextDouble() < 0.75)
            betAmount = currentRoundMinimumPlayAmount + 0.4m * totalPotAmount;
          else
            betAmount = currentRoundMinimumPlayAmount + 0.6m * totalPotAmount;
        }

        //Scale the raise amounts to a little blind
        decimal raiseBlindMultiple = betAmount / currentDecision.Cache.LittleBlind;
        betAmount = Math.Round(raiseBlindMultiple, 0, MidpointRounding.AwayFromZero) * currentDecision.Cache.LittleBlind;
      }
      else
      {
        //If the current play amount is greater than what we want to call but
        //also the max we want to call is greater than what we can put on the table
        if (currentRoundMinimumPlayAmount > maxCallAmount && maxCallAmount > currentDecision.Cache.getPlayerStack(currentDecision.PlayerId) + currentRoundPlayerBetAmount)
        {
          botAction = PokerAction.Call;
          betAmount = currentDecision.Cache.getPlayerStack(currentDecision.PlayerId);
        }
        else if (currentRoundMinimumPlayAmount > maxCallAmount)
          botAction = PokerAction.Fold;
        else
        {
          //If our hand is not great and we can afford the call we decide based on the current action
          if (currentRoundPlayerBetAmount == currentDecision.Cache.BigBlind && currentRoundMinimumPlayAmount == currentDecision.Cache.BigBlind && botAction == PokerAction.Fold)
            botAction = PokerAction.Check;
          else
          {
            botAction = PokerAction.Call;
            betAmount = currentRoundMinimumPlayAmount - currentRoundPlayerBetAmount;
          }
        }
      }
    }

    protected void v2postFlopAIDecision()
    {
      //If WR > RaiseWR we will call anything or raise to 4xBB
      //If WR < RaiseWR we determine the max call amount using the following

      //Max Call Amount = 1 + (WR - PlayWR)*(19/(RaiseWR-PlayWR)
      decimal maxCallAmount;

      if (currentWinRatio > botRaiseWRThreshold)
      {
        //If we are good enough to raise our maximum call amount is our stack
        maxCallAmount = currentDecision.Cache.getPlayerStack(currentDecision.PlayerId);
        winRatioDifference = currentWinRatio - botRaiseWRThreshold;
      }
      else
      {
        //If we are not good enough to raise our maximum call amount is scaled using our winRatio
        maxCallAmount = (decimal)((decimal)(1 + ((currentWinRatio - botPlayWRThreshold) * (19 / (botRaiseWRThreshold - botPlayWRThreshold)))) * currentDecision.Cache.BigBlind);
        winRatioDifference = currentWinRatio - botPlayWRThreshold;
      }

      //If the current bet amount is greater than max call amount but
      //the total amount we can put on the table is less than maxCallAmount we can call
      if (currentRoundMinimumPlayAmount > maxCallAmount && maxCallAmount > currentDecision.Cache.getPlayerStack(currentDecision.PlayerId) + currentRoundPlayerBetAmount)
      {
        botAction = PokerAction.Call;
        betAmount = currentDecision.Cache.getPlayerStack(currentDecision.PlayerId);
      }
      else if (currentRoundMinimumPlayAmount > maxCallAmount)
        botAction = PokerAction.Fold;
      else
      {
        if (currentRoundMinimumPlayAmount == currentRoundPlayerBetAmount)
        {
          botAction = PokerAction.Check;
          betAmount = 0;
        }
        else
        {
          botAction = PokerAction.Call;
          betAmount = currentRoundMinimumPlayAmount - currentRoundPlayerBetAmount;
        }
      }

      //If the max call amount is atleast 2x larger than the current bet amount we will raise
      //If the current bet amount is greater than max call amount then we want to fold
      if (currentWinRatio > (botRaiseWRThreshold / 2) && maxCallAmount > 2 * currentRoundMinimumPlayAmount && currentDecision.Cache.BigBlind * 4 > 2 * currentRoundMinimumPlayAmount)
      {
        botAction = PokerAction.Raise;

        //betAmount = 5 * currentDecision.Cache.BigBlind;
        decimal totalPotAmount = infoStore.GetInfoValue(InfoType.BP_TotalPotAmount_Decimal);
        if (randomGen.NextDouble() < 0.75)
          betAmount = 0.6m * totalPotAmount;
        else
          betAmount = 0.75m * totalPotAmount;

        //Scale the raise amounts to a little blind
        decimal raiseBlindMultiple = betAmount / currentDecision.Cache.LittleBlind;
        betAmount = Math.Round(raiseBlindMultiple, 0, MidpointRounding.AwayFromZero) * currentDecision.Cache.LittleBlind;
      }
    }
  }
}
