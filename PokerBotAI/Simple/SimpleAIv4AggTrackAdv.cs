using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using PokerBot.Definitions;
using PokerBot.AI.InfoProviders;
using System.Diagnostics;
using ProviderAggression;

namespace PokerBot.AI
{
  internal class SimpleAIV4AggTrack : SimpleAIBase
  {
    public SimpleAIV4AggTrack(AIRandomControl aiRandomControl)
        : base(aiRandomControl)
    {
      aiType = AIGeneration.SimpleV4AggressionTrack;

      specificUpdateKey = new RequestedInfoKey(false);
      specificUpdateKey.SetInfoTypeRequired(InfoType.BP_MinimumPlayAmount_Decimal);
      specificUpdateKey.SetInfoTypeRequired(InfoType.BP_PlayerBetAmountCurrentRound_Decimal);

      specificUpdateKey.SetInfoTypeRequired(InfoType.WR_CardsOnlyWinPercentage);
      specificUpdateKey.SetInfoTypeRequired(InfoType.WR_CardsOnlyWinRatio);
      specificUpdateKey.SetInfoTypeRequired(InfoType.PAP_RaiseToCallAmount_Amount);
      //specificUpdateKey.SetInfoTypeRequired(InfoType.PAP_RaiseToStealAmount_Amount);  

      specificUpdateKey.SetInfoTypeRequired(InfoType.AP_AvgScaledOppPreFlopPlayFreq_Double);
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
      botPlayWRThreshold = (decimal)AggressionProvider.ConvertPlayFreqToWinRatio((double)infoStore.GetInfoValue(InfoType.AP_AvgScaledOppPreFlopPlayFreq_Double)) * 1.10m;

      //We don't need this AI going absolutly nuts!!
      if (botPlayWRThreshold < 1.1m)
        botPlayWRThreshold = 1.1m;

      botRaiseWRThreshold = botPlayWRThreshold * 1.2m;

      decimal distanceToDealer = ((infoStore.GetInfoValue(InfoType.GP_DealerDistance_Byte) - 1) / (infoStore.GetInfoValue(InfoType.GP_NumActivePlayers_Byte) - 1));
      decimal currentPotValue = infoStore.GetInfoValue(InfoType.BP_TotalPotAmount_Decimal);

      //Debug.Print("");
      //Debug.Print("AP_AvgScaledOppPreFlopPlayFreq=" + infoStore.GetInfoValue(InfoType.AP_AvgScaledOppPreFlopPlayFreq_Double).ToString());
      //Debug.Print("botPlayWRThreshold=" + botPlayWRThreshold.ToString());

      double randomNum = randomGen.NextDouble();

      if (currentWinRatio >= botRaiseWRThreshold)
      {
        //If WR is larger than raiseLevel then set action to raise 8xBigBlind
        //  If minimum call amount is larger than 8xBigBlind call
        #region code

        //Possibly consider a check raise here
        if (randomNum > 0.9 && currentRoundMinimumPlayAmount == 0 && bettingRound != 0 && distanceToDealer != 1)
          botAction = PokerAction.Check;
        else if (randomNum > 0.2 || currentRoundMinimumPlayAmount == 0)
        {
          betAmount = (decimal)infoStore.GetInfoValue(InfoType.PAP_RaiseToCallAmount_Amount);
          botAction = PokerAction.Raise;
        }
        else
        {
          botAction = PokerAction.Call;
          betAmount = currentRoundMinimumPlayAmount - currentRoundPlayerBetAmount;
        }

        #endregion code
      }
      else if (currentWinRatio >= botPlayWRThreshold)
      {
        //If WR is larger than playLevel then set action to raise 4xBigBlind
        //  If minimum call amount is up to 8xbigblind call
        //  If minimum call amount is larger than 8xbigblind fold
        #region code

        if (randomNum > 0.2 && currentRoundMinimumPlayAmount > 0)
        {
          botAction = PokerAction.Call;
          betAmount = currentRoundMinimumPlayAmount - currentRoundPlayerBetAmount;
        }
        else
        {
          betAmount = (decimal)infoStore.GetInfoValue(InfoType.PAP_RaiseToCallAmount_Amount);
          botAction = PokerAction.Raise;
        }

        #endregion code
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
