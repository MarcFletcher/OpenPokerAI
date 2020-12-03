using System;
using System.Threading;

namespace PokerBot.AI.InfoProviders
{
  public enum InfoType
  {
    //General usage
    IS_CURRENT_GENETIC = -1,

    //WinRatio Provider - WR
    WR_CardsOnlyWinRatio = 0,
    WR_CardsOnlyWinPercentage,
    WR_CardsOnlyWeightedPercentage,
    WR_CardsOnlyOpponentWinPercentage,
    WR_CardsOnlyWeightedOpponentWinPercentage,
    WR_CardsOnlyWinPercentageLastRoundChange,
    WR_ProbOpponentHasBetterWR,
    WR_ProbOpponentHasBetterWRFIXED,
    WR_AveragePercievedProbBotHasBetterHand,
    WR_CardsOnlyWinPercentageIndex,
    WR_ModelAction,
    WR_ModelActionAmount,
    WR_RaiseToCallAmount,
    WR_RaiseToStealAmount,
    WR_RaiseToStealSuccessProb,
    WR_RaiseToCallStealSuccessProb,

    //Player Action Prediction Provider - PAP
    PAP_RaiseToBotCheck_Prob,
    PAP_RaiseToBotCall_Prob,
    PAP_FoldToBotCall_Prob,
    PAP_RaiseToBotRaise_Prob,
    PAP_RaiseToCallAmount_Amount,
    PAP_RaiseToStealSuccess_Prob,
    PAP_RaiseToStealAmount_Amount,

    //Card Provider
    CP_AOnBoard_Bool,
    CP_KOnBoard_Bool,
    CP_FlushPossible_Bool,
    CP_StraightPossible_Bool,
    CP_AKQToBoardRatio_Real,
    CP_TableFlushDraw_Bool,
    CP_TableStraightDraw_Bool,

    CP_HoleCardsAAPair_Bool,
    CP_HoleCardsKKPair_Bool,
    CP_HoleCardsOtherHighPair_Bool,
    CP_HoleCardsOtherLowPair_Bool,
    CP_HoleCardsOtherPair_Bool,
    CP_HoleCardsAK_Bool,
    CP_HoleCardsTroubleHand_Bool,
    CP_HoleCardsMidConnector_Bool,
    CP_HoleCardsLowConnector_Bool,
    CP_HoleCardsSuited_Bool,
    CP_HoleCardsFlushDraw_Bool,
    CP_HoleCardsStraightDraw_Bool,
    CP_HoleCardsOuterStraightDrawWithHC_Bool,
    CP_HoleCardsInnerStraightDrawWithHC_Bool,
    CP_HoleCards3KindOrBetterMadeWithHC_Bool,
    CP_HoleCardsTopOrTwoPair_Bool,
    CP_HoleCardsAOrKInHand_Bool,
    CP_HoleCardsMatchedPlayability,

    //Bets Provider
    BP_TotalPotAmount_Decimal,
    BP_MinimumPlayAmount_Decimal,
    BP_BetsToCall_Byte,
    BP_LastRoundBetsToCall_Byte,
    BP_LastAdditionalRaiseAmount,
    BP_PlayerBetAmountCurrentRound_Decimal,
    BP_PlayerMoneyInPot_Decimal,
    BP_TotalNumRaises_Byte,
    BP_TotalNumCalls_Byte,
    BP_TotalNumChecks_Byte,
    BP_CurrentCallAmountLarger4BB,
    BP_PlayerHandStartingStackAmount_Decimal,
    BP_PlayerLastAction_Short,
    BP_CalledLastRound_Bool,
    BP_RaisedLastRound_Bool,
    BP_ImmediatePotOdds_Double,
    BP_ScaledCallAmount_Double,

    //Game Provider
    GP_NumTableSeats_Byte,
    GP_NumPlayersDealtIn_Byte,
    GP_NumActivePlayers_Byte,
    GP_NumUnactedPlayers_Byte,
    GP_GameStage_Byte,
    GP_DealerDistance_Byte,

    //ImpliedOdds Provider
    IO_ImpliedPotOdds_Double,

    //AI Aggression Provider
    //Scaling is 50% from 200 hands and 50% from 20 recent hands.
    AP_AvgScaledOppRaiseFreq_Double,
    AP_AvgScaledOppCallFreq_Double,
    AP_AvgScaledOppPreFlopPlayFreq_Double,

    //The new live aggression methods. Raw values as calculated from available stats.
    //Accuracy is from 0 to 1 where 1 is >1000 hands
    AP_AvgLiveOppPreFlopPlayFreq_Double,
    AP_AvgLiveOppPostFlopPlayFreq_Double,
    AP_AvgLiveOppCurrentRoundAggr_Double,
    AP_AvgLiveOppCurrentRoundAggrAcc_Double,
  }

  public class InfoPiece
  {
    InfoType type;
    decimal value;
    decimal defaultValue;
    bool updated;

    ManualResetEvent infoPieceUpdatedSignal;

    object infoPieceLocker = new object();

    public InfoType InformationType { get { return type; } }

    public decimal Value
    {
      get
      {
        lock (infoPieceLocker)
        {
          if (!updated)
            throw new Exception("Attempting to get value from infoPiece which has not been recorded as updated.");

          return value;
        }
      }
      set
      {
        lock (infoPieceLocker)
        {
          this.value = value;
          infoPieceUpdatedSignal.Set();
          updated = true;
        }
      }
    }

    public void ResetUpdateFlag()
    {
      lock (infoPieceLocker)
      {
        updated = false;
        infoPieceUpdatedSignal.Reset();
      }
    }

    public void WaitForUpdate()
    {
      infoPieceUpdatedSignal.WaitOne();
    }

    public bool Updated
    {
      get { return updated; }
    }

    public decimal DefaultValue
    {
      get { lock (infoPieceLocker) { return defaultValue; } }
    }

    public InfoPiece(InfoType infoType, decimal defaultValue)
    {
      lock (infoPieceLocker)
      {
        this.type = infoType;
        this.defaultValue = defaultValue;
        this.value = defaultValue;
        infoPieceUpdatedSignal = new ManualResetEvent(false);
        updated = false;
      }
    }

    public InfoPiece(InfoType infoType, decimal value, decimal defaultValue, bool startingUpdatedStatus = false)
    {
      lock (infoPieceLocker)
      {
        this.value = value;
        this.type = infoType;
        this.defaultValue = defaultValue;
        infoPieceUpdatedSignal = new ManualResetEvent(false);
        updated = startingUpdatedStatus;
      }
    }

    /// <summary>
    /// Set this infoPiece to it's default value. If updatedStatus is true then this piece will be marked as updated
    /// </summary>
    /// <param name="updatedStatus"></param>
    public void SetToDefault(bool updatedStatus)
    {
      lock (infoPieceLocker)
      {
        this.value = this.defaultValue;
        infoPieceUpdatedSignal.Set();

        if (updatedStatus)
          updated = true;
      }
    }
  }
}
