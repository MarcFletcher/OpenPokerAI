namespace PokerBot.Definitions
{
  /// <summary>
  /// Enum describing the different actions that can be taken at a turn
  /// </summary>
  public enum PokerAction : byte
  {
    //GeneralAIError = -6,
    //TotalPotError = -5,
    //CommsTimeoutError = -4,
    //CommsSerialisationError = -3,
    //CatastrophicError = -2,
    //StackErrorAdjustment = -1,
    JoinTable,
    LeaveTable,
    SitOut,
    SitIn,
    LittleBlind,
    BigBlind,
    Fold,
    Check,
    Call,
    Raise,
    WinPot,
    AddStackCash,
    DeadBlind,
    DealFlop,
    DealTurn,
    DealRiver,
    ReturnBet,
    TableRake,
    NoAction,

    Ante,

    //Error are always greater than 200
    GeneralAIError = 201,
    StackErrorAdjustment = 202,
    TotalPotError = 203,
    CommsTimeoutError = 204,
    CommsSerialisationError = 205,
    CatastrophicError = 206,
  }
}
