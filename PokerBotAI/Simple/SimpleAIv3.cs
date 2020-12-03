using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using PokerBot.Definitions;
using PokerBot.AI.InfoProviders;

namespace PokerBot.AI
{
  internal class SimpleAIV3 : SimpleAIBase
  {
    public SimpleAIV3(AIRandomControl aiRandomControl)
        : base(aiRandomControl)
    {
      aiType = AIGeneration.SimpleV3;

      specificUpdateKey = new RequestedInfoKey(false);
      specificUpdateKey.SetInfoTypeRequired(InfoType.BP_MinimumPlayAmount_Decimal);
      specificUpdateKey.SetInfoTypeRequired(InfoType.BP_PlayerBetAmountCurrentRound_Decimal);

      specificUpdateKey.SetInfoTypeRequired(InfoType.WR_CardsOnlyWinPercentage);
      specificUpdateKey.SetInfoTypeRequired(InfoType.WR_CardsOnlyWinRatio);
      specificUpdateKey.SetInfoTypeRequired(InfoType.PAP_RaiseToCallAmount_Amount);
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
      if (currentWinRatio >= botRaiseWRThreshold)
      {
        //If WR is larger than raiseLevel then set action to raise 8xBigBlind
        //  If minimum call amount is larger than 8xBigBlind call
        #region code

        betAmount = (decimal)infoStore.GetInfoValue(InfoType.PAP_RaiseToCallAmount_Amount);
        botAction = PokerAction.Raise;

        #endregion code
      }
      else if (currentWinRatio >= botPlayWRThreshold)
      {
        //If WR is larger than playLevel then set action to raise 4xBigBlind
        //  If minimum call amount is up to 8xbigblind call
        //  If minimum call amount is larger than 8xbigblind fold
        #region code

        winRatioDifference = currentWinRatio - botPlayWRThreshold;
        betAmount = (decimal)infoStore.GetInfoValue(InfoType.PAP_RaiseToCallAmount_Amount);

        if (currentRoundMinimumPlayAmount > betAmount)
        {
          botAction = PokerAction.Call;
          betAmount = currentRoundMinimumPlayAmount - currentRoundPlayerBetAmount;
        }
        else
          botAction = PokerAction.Raise;

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
