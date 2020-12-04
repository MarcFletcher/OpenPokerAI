using System;
using PokerBot.Definitions;
using PokerBot.AI.InfoProviders;

namespace PokerBot.AI
{
  internal class SimpleAIV1 : SimpleAIBase
  {
    public SimpleAIV1(AIRandomControl aiRandomControl)
        : base(aiRandomControl)
    {
      aiType = AIGeneration.SimpleV1;

      specificUpdateKey = new RequestedInfoKey(false);

      specificUpdateKey.SetInfoTypeRequired(InfoType.WR_CardsOnlyWinPercentage);
      specificUpdateKey.SetInfoTypeRequired(InfoType.WR_CardsOnlyWinRatio);

      specificUpdateKey.SetInfoTypeRequired(InfoType.BP_TotalPotAmount_Decimal);
    }

    protected override RequestedInfoKey GetUpdateKeyOrPreDecision()
    {
      return specificUpdateKey;
    }

    /// <summary>
    /// Sets botAction and betAmount
    /// </summary>
    protected override void GetAIDecision()
    {
      byte bettingRound = currentDecision.Cache.getBettingRound();
      decimal totalPotAmount = infoStore.GetInfoValue(InfoType.BP_TotalPotAmount_Decimal);

      if (currentWinRatio >= botRaiseWRThreshold)
      {
        //If WR is larger than raiseLevel then set action to raise 8xBigBlind
        //If minimum call amount is larger than 8xBigBlind call
        #region code

        winRatioDifference = currentWinRatio - botRaiseWRThreshold;
        //betAmount = 8 * currentDecision.Cache.BigBlind;

        if (bettingRound == 0)
        {
          if (currentRoundMinimumPlayAmount == currentDecision.Cache.BigBlind)
          {
            if (randomGen.NextDouble() < 0.75)
              betAmount = 3 * currentDecision.Cache.BigBlind;
            else
              betAmount = 4 * currentDecision.Cache.BigBlind;
          }
          else
          {
            if (randomGen.NextDouble() < 0.75)
              betAmount = 0.6m * totalPotAmount;
            else
              betAmount = 0.75m * totalPotAmount;
          }
        }
        else
        {
          if (randomGen.NextDouble() < 0.75)
            betAmount = 0.6m * totalPotAmount;
          else
            betAmount = 0.75m * totalPotAmount;
        }

        //Scale the raise amounts to a little blind
        decimal raiseBlindMultiple = betAmount / currentDecision.Cache.LittleBlind;
        betAmount = Math.Round(raiseBlindMultiple, 0, MidpointRounding.AwayFromZero) * currentDecision.Cache.LittleBlind;

        if (currentRoundMinimumPlayAmount >= (8 * currentDecision.Cache.BigBlind) || currentRoundMinimumPlayAmount + currentRoundLastRaiseAmount > betAmount)
        {
          botAction = PokerAction.Call;
          betAmount = currentRoundMinimumPlayAmount - currentRoundPlayerBetAmount;
        }
        else
          botAction = PokerAction.Raise;

        #endregion code
      }
      else if (currentWinRatio >= botPlayWRThreshold)
      {
        //If WR is larger than playLevel then set action to raise 4xBigBlind
        //If minimum call amount is up to 8xbigblind call
        //If minimum call amount is larger than 8xbigblind fold
        #region code

        winRatioDifference = currentWinRatio - botPlayWRThreshold;
        //betAmount = 3 * currentDecision.Cache.BigBlind;

        if (bettingRound == 0)
        {
          if (currentRoundMinimumPlayAmount == currentDecision.Cache.BigBlind)
          {
            if (randomGen.NextDouble() < 0.75)
              betAmount = 3 * currentDecision.Cache.BigBlind;
            else
              betAmount = 4 * currentDecision.Cache.BigBlind;
          }
          else
          {
            if (randomGen.NextDouble() < 0.75)
              betAmount = 0.6m * totalPotAmount;
            else
              betAmount = 0.75m * totalPotAmount;
          }
        }
        else
        {
          if (randomGen.NextDouble() < 0.75)
            betAmount = 0.6m * totalPotAmount;
          else
            betAmount = 0.75m * totalPotAmount;
        }

        //Scale the raise amounts to a little blind
        decimal raiseBlindMultiple = betAmount / currentDecision.Cache.LittleBlind;
        betAmount = Math.Round(raiseBlindMultiple, 0, MidpointRounding.AwayFromZero) * currentDecision.Cache.LittleBlind;

        if (currentRoundMinimumPlayAmount > (8 * currentDecision.Cache.BigBlind))
        {
          betAmount = 0;
          botAction = PokerAction.Fold;
        }
        else if (currentRoundMinimumPlayAmount >= (4 * currentDecision.Cache.BigBlind) || currentRoundMinimumPlayAmount + currentRoundLastRaiseAmount > betAmount)
        {
          botAction = PokerAction.Call;
          betAmount = currentRoundMinimumPlayAmount - currentRoundPlayerBetAmount;
        }
        else
          botAction = PokerAction.Raise;

        #endregion code
      }

      //If we are preflop and we are one of the blinds
      if (bettingRound == 0 && (currentRoundPlayerBetAmount == currentDecision.Cache.BigBlind || currentRoundPlayerBetAmount == currentDecision.Cache.LittleBlind))
      {
        #region code

        if (currentRoundPlayerBetAmount == currentDecision.Cache.BigBlind && currentRoundMinimumPlayAmount == currentDecision.Cache.BigBlind && botAction == PokerAction.Fold)
        {
          //If action is currently fold and minimum bet amount is big blind then check
          botAction = PokerAction.Check;
        }
        else if (currentRoundPlayerBetAmount == currentDecision.Cache.LittleBlind && currentRoundMinimumPlayAmount == currentDecision.Cache.BigBlind && botAction == PokerAction.Fold)
        {
          //Actions for little blind
          //If action is currently fold ....
          //If AlwaysPlayLittleBlind is true and minimum call amount is big blind then call big blind
          if (botAlwaysPlayLB)
          {
            botAction = PokerAction.Call;
            betAmount = currentDecision.Cache.BigBlind - currentRoundPlayerBetAmount;
          }
          else
          {
            botAction = PokerAction.Fold;
          }
        }
        //If nothing has changed we want to fold

        #endregion code
      }
      else
      {
        //If we have reached here, have decided not to raise but can get a free check, lets take it.
        if (currentRoundMinimumPlayAmount == currentRoundPlayerBetAmount && botAction == PokerAction.Fold)
          botAction = PokerAction.Check;
      }

      //Call if already raised pre flop
      if (bettingRound == 0 && botAction == PokerAction.Raise)
      {
        //Split down into seperate IF to make sure there is not too much of a performance hit
        if (currentDecision.Cache.getPlayerCurrentRoundActions(currentDecision.PlayerId).Contains(PokerAction.Raise))
        {
          botAction = PokerAction.Call;
          betAmount = currentRoundMinimumPlayAmount - currentRoundPlayerBetAmount;
        }
      }
    }
  }
}
