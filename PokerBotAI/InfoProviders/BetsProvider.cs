using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using PokerBot.Definitions;
using PokerBot.AI.InfoProviders;

namespace PokerBot.AI.InfoProviders
{
  class BetsProvider : InfoProviderBase
  {
    public BetsProvider(InfoCollection information, Dictionary<InfoProviderType, InfoProviderBase> allInformationProviders, AIRandomControl aiRandomControl)
        : base(information, InfoProviderType.Bets, allInformationProviders, aiRandomControl)
    {
      providedInfoTypes = new List<InfoPiece>() { new InfoPiece(InfoType.BP_BetsToCall_Byte,5),
                                                        new InfoPiece(InfoType.BP_LastRoundBetsToCall_Byte,0),
                                                        new InfoPiece(InfoType.BP_MinimumPlayAmount_Decimal,0),
                                                        new InfoPiece(InfoType.BP_PlayerHandStartingStackAmount_Decimal,0),
                                                        new InfoPiece(InfoType.BP_PlayerLastAction_Short,(byte)Definitions.PokerAction.CatastrophicError),
                                                        new InfoPiece(InfoType.BP_PlayerMoneyInPot_Decimal,0),
                                                        new InfoPiece(InfoType.BP_TotalNumCalls_Byte,5),
                                                        new InfoPiece(InfoType.BP_TotalNumRaises_Byte,5),
                                                        new InfoPiece(InfoType.BP_TotalPotAmount_Decimal,1),
                                                        new InfoPiece(InfoType.BP_CalledLastRound_Bool,0),
                                                        new InfoPiece(InfoType.BP_RaisedLastRound_Bool,0),
                                                        new InfoPiece(InfoType.BP_PlayerBetAmountCurrentRound_Decimal,0),
                                                        new InfoPiece(InfoType.BP_ImmediatePotOdds_Double, 0),
                                                        new InfoPiece(InfoType.BP_TotalNumChecks_Byte, 0),
                                                        new InfoPiece(InfoType.BP_CurrentCallAmountLarger4BB, 1),
                                                        new InfoPiece(InfoType.BP_LastAdditionalRaiseAmount, 0),
                                                        new InfoPiece(InfoType.BP_ScaledCallAmount_Double, 1)
                                                    };

      AddProviderInformationTypes();
    }

    protected override void updateInfo()
    {
      if (decisionRequest.RequiredInfoTypeUpdateKey.IsInfoTypeRequired(InfoType.BP_LastAdditionalRaiseAmount) ||
          decisionRequest.RequiredInfoTypeUpdateKey.IsInfoTypeRequired(InfoType.BP_PlayerMoneyInPot_Decimal) ||
          decisionRequest.RequiredInfoTypeUpdateKey.IsInfoTypeRequired(InfoType.BP_PlayerHandStartingStackAmount_Decimal) ||
          decisionRequest.RequiredInfoTypeUpdateKey.IsInfoTypeRequired(InfoType.BP_PlayerBetAmountCurrentRound_Decimal) ||
          decisionRequest.RequiredInfoTypeUpdateKey.IsInfoTypeRequired(InfoType.BP_MinimumPlayAmount_Decimal) ||
          decisionRequest.RequiredInfoTypeUpdateKey.IsInfoTypeRequired(InfoType.BP_ScaledCallAmount_Double) ||
          decisionRequest.RequiredInfoTypeUpdateKey.IsInfoTypeRequired(InfoType.BP_TotalPotAmount_Decimal) ||
          decisionRequest.RequiredInfoTypeUpdateKey.IsInfoTypeRequired(InfoType.BP_CurrentCallAmountLarger4BB) ||
          decisionRequest.RequiredInfoTypeUpdateKey.IsInfoTypeRequired(InfoType.BP_TotalNumCalls_Byte) ||
          decisionRequest.RequiredInfoTypeUpdateKey.IsInfoTypeRequired(InfoType.BP_TotalNumRaises_Byte) ||
          decisionRequest.RequiredInfoTypeUpdateKey.IsInfoTypeRequired(InfoType.BP_TotalNumChecks_Byte) ||
          decisionRequest.RequiredInfoTypeUpdateKey.IsInfoTypeRequired(InfoType.BP_PlayerLastAction_Short) ||
          decisionRequest.RequiredInfoTypeUpdateKey.IsInfoTypeRequired(InfoType.BP_CalledLastRound_Bool) ||
          decisionRequest.RequiredInfoTypeUpdateKey.IsInfoTypeRequired(InfoType.BP_RaisedLastRound_Bool) ||
          decisionRequest.RequiredInfoTypeUpdateKey.IsInfoTypeRequired(InfoType.BP_BetsToCall_Byte) ||
          decisionRequest.RequiredInfoTypeUpdateKey.IsInfoTypeRequired(InfoType.BP_LastRoundBetsToCall_Byte) ||
          decisionRequest.RequiredInfoTypeUpdateKey.IsInfoTypeRequired(InfoType.BP_ImmediatePotOdds_Double))
      {
        if (decisionRequest.RequiredInfoTypeUpdateKey.IsInfoTypeRequired(InfoType.BP_LastAdditionalRaiseAmount))
          infoStore.SetInformationValue(InfoType.BP_LastAdditionalRaiseAmount, decisionRequest.Cache.getCurrentRoundLastRaiseAmount());

        if (decisionRequest.RequiredInfoTypeUpdateKey.IsInfoTypeRequired(InfoType.BP_PlayerBetAmountCurrentRound_Decimal) ||
            decisionRequest.RequiredInfoTypeUpdateKey.IsInfoTypeRequired(InfoType.BP_PlayerMoneyInPot_Decimal) ||
            decisionRequest.RequiredInfoTypeUpdateKey.IsInfoTypeRequired(InfoType.BP_PlayerHandStartingStackAmount_Decimal) ||
            decisionRequest.RequiredInfoTypeUpdateKey.IsInfoTypeRequired(InfoType.BP_MinimumPlayAmount_Decimal) ||
            decisionRequest.RequiredInfoTypeUpdateKey.IsInfoTypeRequired(InfoType.BP_ScaledCallAmount_Double) ||
            decisionRequest.RequiredInfoTypeUpdateKey.IsInfoTypeRequired(InfoType.BP_TotalPotAmount_Decimal) ||
            decisionRequest.RequiredInfoTypeUpdateKey.IsInfoTypeRequired(InfoType.BP_ImmediatePotOdds_Double) ||
            decisionRequest.RequiredInfoTypeUpdateKey.IsInfoTypeRequired(InfoType.BP_CurrentCallAmountLarger4BB))
        {
          decimal playerMoneyBetCurrentRound = decisionRequest.Cache.getPlayerCurrentRoundBetAmount(decisionRequest.PlayerId);
          if (decisionRequest.RequiredInfoTypeUpdateKey.IsInfoTypeRequired(InfoType.BP_PlayerBetAmountCurrentRound_Decimal))
            infoStore.SetInformationValue(InfoType.BP_PlayerBetAmountCurrentRound_Decimal, playerMoneyBetCurrentRound);

          decimal playerMoneyInPot = decisionRequest.Cache.getTotalPlayerMoneyInPot(decisionRequest.PlayerId);
          if (decisionRequest.RequiredInfoTypeUpdateKey.IsInfoTypeRequired(InfoType.BP_PlayerMoneyInPot_Decimal))
            infoStore.SetInformationValue(InfoType.BP_PlayerMoneyInPot_Decimal, playerMoneyInPot);

          decimal playerStackAmount = decisionRequest.Cache.getPlayerStack(decisionRequest.PlayerId);
          if (decisionRequest.RequiredInfoTypeUpdateKey.IsInfoTypeRequired(InfoType.BP_PlayerHandStartingStackAmount_Decimal))
            infoStore.SetInformationValue(InfoType.BP_PlayerHandStartingStackAmount_Decimal, playerStackAmount + playerMoneyInPot);

          if (decisionRequest.RequiredInfoTypeUpdateKey.IsInfoTypeRequired(InfoType.BP_MinimumPlayAmount_Decimal) ||
              decisionRequest.RequiredInfoTypeUpdateKey.IsInfoTypeRequired(InfoType.BP_ScaledCallAmount_Double) ||
              decisionRequest.RequiredInfoTypeUpdateKey.IsInfoTypeRequired(InfoType.BP_TotalPotAmount_Decimal) ||
              decisionRequest.RequiredInfoTypeUpdateKey.IsInfoTypeRequired(InfoType.BP_ImmediatePotOdds_Double))
          {
            decimal minCallToTotalAmount = decisionRequest.Cache.getMinimumPlayAmount();
            if (minCallToTotalAmount > playerStackAmount + playerMoneyBetCurrentRound)
              minCallToTotalAmount = playerStackAmount + playerMoneyBetCurrentRound;

            if (decisionRequest.RequiredInfoTypeUpdateKey.IsInfoTypeRequired(InfoType.BP_MinimumPlayAmount_Decimal))
              infoStore.SetInformationValue(InfoType.BP_MinimumPlayAmount_Decimal, minCallToTotalAmount);

            decimal potAmount = decisionRequest.Cache.getCurrentHandDetails().potValue;
            if (decisionRequest.RequiredInfoTypeUpdateKey.IsInfoTypeRequired(InfoType.BP_ScaledCallAmount_Double))
              infoStore.SetInformationValue(InfoType.BP_ScaledCallAmount_Double, ScaleCallAmount(decisionRequest.Cache.BigBlind, minCallToTotalAmount - playerMoneyBetCurrentRound, potAmount));

            //Need to take into account table rake here
            //We should be doing this with a rake delegate but this will do for now
            decimal rakedPotAmount;
            if (decisionRequest.Cache.getBettingRound() > 0)
            {
              int rakeMultiples = (int)(potAmount / 0.15m);
              rakedPotAmount = potAmount - (0.01m * rakeMultiples);
            }
            else
              rakedPotAmount = potAmount;

            if (decisionRequest.RequiredInfoTypeUpdateKey.IsInfoTypeRequired(InfoType.BP_TotalPotAmount_Decimal))
              infoStore.SetInformationValue(InfoType.BP_TotalPotAmount_Decimal, rakedPotAmount);

            if (decisionRequest.RequiredInfoTypeUpdateKey.IsInfoTypeRequired(InfoType.BP_ImmediatePotOdds_Double))
            {
              decimal immediatePotOdds;
              if (minCallToTotalAmount - playerMoneyBetCurrentRound > 0)
              {
                immediatePotOdds = ((rakedPotAmount / (minCallToTotalAmount - playerMoneyBetCurrentRound)) / 10.0m) - 0.1m;
                if (immediatePotOdds > 1.0m)
                  immediatePotOdds = 1.0m;
                else if (immediatePotOdds < 0)
                  immediatePotOdds = 0;
              }
              else
                immediatePotOdds = 1.0m;

              infoStore.SetInformationValue(InfoType.BP_ImmediatePotOdds_Double, immediatePotOdds);
            }
          }

          if (decisionRequest.RequiredInfoTypeUpdateKey.IsInfoTypeRequired(InfoType.BP_CurrentCallAmountLarger4BB))
          {
            bool currentCallAmountLarger4BB;
            if (decisionRequest.Cache.getMinimumPlayAmount() - playerMoneyBetCurrentRound > (4 * decisionRequest.Cache.BigBlind))
              currentCallAmountLarger4BB = true;
            else
              currentCallAmountLarger4BB = false;
            infoStore.SetInformationValue(InfoType.BP_CurrentCallAmountLarger4BB, Convert.ToDecimal(currentCallAmountLarger4BB));
          }
        }

        var handActionsThisHand =
            (from ha in decisionRequest.Cache.getAllHandActions()
             where ha.handId == decisionRequest.Cache.getCurrentHandId()
             select ha).ToArray();

        if (decisionRequest.RequiredInfoTypeUpdateKey.IsInfoTypeRequired(InfoType.BP_TotalNumCalls_Byte))
        {
          int callsCount =
              (from ha in handActionsThisHand
               where ha.actionType == PokerAction.Call
               select ha).Count();
          infoStore.SetInformationValue(InfoType.BP_TotalNumCalls_Byte, callsCount);
        }

        if (decisionRequest.RequiredInfoTypeUpdateKey.IsInfoTypeRequired(InfoType.BP_TotalNumRaises_Byte))
        {
          int raiseCount =
              (from ha in handActionsThisHand
               where ha.actionType == PokerAction.Raise //|| ha.actionType == PokerAction.BigBlind
               select ha).Count();
          infoStore.SetInformationValue(InfoType.BP_TotalNumRaises_Byte, raiseCount);
        }

        if (decisionRequest.RequiredInfoTypeUpdateKey.IsInfoTypeRequired(InfoType.BP_TotalNumChecks_Byte))
        {
          int checkCount =
              (from ha in handActionsThisHand
               where ha.actionType == PokerAction.Check
               select ha).Count();
          infoStore.SetInformationValue(InfoType.BP_TotalNumChecks_Byte, checkCount);
        }

        if (decisionRequest.RequiredInfoTypeUpdateKey.IsInfoTypeRequired(InfoType.BP_PlayerLastAction_Short))
        {
          var lastPlayerAction =
             (from ha in handActionsThisHand
              where ha.playerId == decisionRequest.PlayerId
              where ha.actionType == PokerAction.Fold || ha.actionType == PokerAction.Check || ha.actionType == PokerAction.Call || ha.actionType == PokerAction.Raise
              orderby ha.localIndex descending
              select ha).ToArray();
          infoStore.SetInformationValue(InfoType.BP_PlayerLastAction_Short, (lastPlayerAction.Count() == 0 ? (byte)PokerAction.NoAction : (short)lastPlayerAction.First().actionType));
        }

        //If we want the calls last round we probably want the raises as well so we will just do them together
        if (decisionRequest.RequiredInfoTypeUpdateKey.IsInfoTypeRequired(InfoType.BP_CalledLastRound_Bool) || decisionRequest.RequiredInfoTypeUpdateKey.IsInfoTypeRequired(InfoType.BP_RaisedLastRound_Bool))
        {
          var lastRoundPlayerActions = decisionRequest.Cache.getPlayerLastRoundActions(decisionRequest.PlayerId);
          bool calledLastRoundBets = lastRoundPlayerActions.Contains(PokerAction.Call); //Did this player call last round?
          infoStore.SetInformationValue(InfoType.BP_CalledLastRound_Bool, Convert.ToDecimal(calledLastRoundBets));

          bool raisedLastRound = lastRoundPlayerActions.Contains(PokerAction.Raise); // Did this player raise last round?
          infoStore.SetInformationValue(InfoType.BP_RaisedLastRound_Bool, Convert.ToDecimal(raisedLastRound));
        }

        if (decisionRequest.RequiredInfoTypeUpdateKey.IsInfoTypeRequired(InfoType.BP_BetsToCall_Byte) || decisionRequest.RequiredInfoTypeUpdateKey.IsInfoTypeRequired(InfoType.BP_LastRoundBetsToCall_Byte))
        {
          IEnumerable<PokerBot.Database.databaseCache.playActions> actionsInBettingRound;
          int betsToCall = 0;
          int betsToCallLastRound = 0;

          var dealerActions =
              (from ha in handActionsThisHand
               where ha.actionType == PokerAction.DealFlop || ha.actionType == PokerAction.DealRiver || ha.actionType == PokerAction.DealTurn
               orderby ha.localIndex ascending
               select ha).ToArray();

          for (int i = 0; i < dealerActions.Count() + 1; i++)
          {
            actionsInBettingRound =
                (from ha in handActionsThisHand
                 where ha.localIndex < (i == dealerActions.Length ? long.MaxValue : dealerActions[i].localIndex) &&
                        ha.localIndex > (i == 0 ? -1 : dealerActions[i - 1].localIndex)
                 select ha).ToArray();

            if (i == dealerActions.Length)
            {
              var bets =
                  from ha in actionsInBettingRound
                  where ha.actionType == PokerAction.Call || ha.actionType == PokerAction.Raise //|| ha.actionType ==PokerAction.BigBlind
                  select ha;

              betsToCall = bets.Count();

            }
            else if (i == dealerActions.Length - 1)
            {
              var bets =
                  from ha in actionsInBettingRound
                  where ha.actionType == PokerAction.Call || ha.actionType == PokerAction.Raise //|| ha.actionType == PokerAction.BigBlind
                  select ha;

              betsToCallLastRound = bets.Count();
            }
          }

          infoStore.SetInformationValue(InfoType.BP_BetsToCall_Byte, betsToCall);
          infoStore.SetInformationValue(InfoType.BP_LastRoundBetsToCall_Byte, betsToCallLastRound);
        }
      }
    }

    public static decimal ScaleCallAmount(decimal bigBlind, decimal callAmount, decimal potAmount)
    {
      if (callAmount == 0.0m)
        return 0;

      decimal pa = potAmount;

      decimal a, b, c;

      pa -= callAmount;

      if (pa > 50.0m * bigBlind)
        pa = 50.0m * bigBlind;
      if (pa < 10.0m * bigBlind)
        pa = 10.0m * bigBlind;

      //b = (101 * bigBlind - 2 * pa) / Math.Pow(bigBlind - pa, 2);
      b = (101 * bigBlind - 2 * pa) / ((bigBlind - pa) * (bigBlind - pa));
      c = 1 - bigBlind * b;
      a = 1.0m / (decimal)Math.Log((double)(100.0m * bigBlind * b + c), Math.E);

      decimal ca = a * (decimal)Math.Log((double)(b * callAmount + c), Math.E);

      if (ca > 1)
        ca = 1;
      if (ca < 0)
        ca = 0;

      //if (double.IsNaN(ca))
      //    throw new Exception("ScaleCallAmount should never return NaN");

      return ca;
    }

    public static decimal ScaleRaiseAmount(decimal bigBlind, decimal additionalRaiseAmount, decimal potAmount)
    {
      if (additionalRaiseAmount == 0.0m)
        return 0;

      decimal pa = potAmount;

      decimal a, b, c;

      if (pa > 50.0m * bigBlind)
        pa = 50.0m * bigBlind;
      if (pa < 10.0m * bigBlind)
        pa = 10.0m * bigBlind;

      //b = (101 * bigBlind - 2 * pa) / Math.Pow((bigBlind - pa), 2);
      b = (101 * bigBlind - 2 * pa) / ((bigBlind - pa) * (bigBlind - pa));

      c = 1 - bigBlind * b;
      a = 1.0m / (decimal)Math.Log((double)(100.0m * bigBlind * b + c), Math.E);

      decimal ra = a * (decimal)Math.Log((double)(b * additionalRaiseAmount + c), Math.E);

      if (ra > 1)
        ra = 1;
      if (ra < 0)
        ra = 0;

      //if (double.IsNaN(ra))
      //    throw new Exception("ScaleRaiseAmount should never return NaN");

      return ra;
    }

  }
}
