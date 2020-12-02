namespace PokerBot.Definitions
{
  /// <summary>
  /// Enum describing the stage in the current hand.  Values correspond to number of cards on the table
  /// </summary>
  public enum HandState
  {
    PreFlop = 0,
    Flop = 3,
    Turn = 4,
    River = 5,
  }
}
