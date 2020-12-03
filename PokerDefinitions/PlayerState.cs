namespace PokerBot.Definitions
{
  /// <summary>
  /// Represents the action of a player in the current turn 
  /// </summary>
  public struct Player
  {
    private uint playerID;
    private string name;
    private PokerAction action;
    private float amount;
    private float remainingChipsValue;
    private bool actionEntered;
    private bool sittingOut;
    private Card holeCard1;
    private Card holeCard2;

    public string Name
    {
      get { return name; }
    }

    /// <summary>
    /// Gets the players action
    /// </summary>
    public PokerAction Action
    {
      get { return action; }
    }

    /// <summary>
    /// Gets the amount a player bet this turn
    /// </summary>
    public float Amount
    {
      get { return amount; }
    }

    /// <summary>
    /// Gets the value represented by the players remaining chips
    /// </summary>
    public float RemainingChipsValue
    {
      get { return remainingChipsValue; }
    }

    public bool ActionEntered
    {
      get { return actionEntered; }
    }

    public bool SittingOut
    {
      get { return sittingOut; }
    }

    public Card HoleCard1
    {
      get { return holeCard1; }
    }

    public Card HoleCard2
    {
      get { return holeCard2; }
    }

    public uint PlayerID
    {
      get { return playerID; }
    }

    /// <summary>
    /// Contructor for Player
    /// </summary>
    /// <param name="action">The type of action that the player performed</param>
    /// <param name="amount">The amount the player bet</param>
    /// <param name="decisionTime">The time it took for the player to make their decision</param>
    /// <param name="remainingChipsValue">The remaining value of the player's chips</param>
    public Player(string name, float remainingChipsValue)
    {
      this.name = name;
      this.remainingChipsValue = remainingChipsValue;
      actionEntered = false;
      action = PokerAction.Check;
      amount = 0;
      sittingOut = false;
      holeCard1 = Card.NoCard;
      holeCard2 = Card.NoCard;

      playerID = 0;
    }

    public void ResetHand(float amountWon)
    {
      remainingChipsValue += amountWon;
      actionEntered = false;
    }

    public void SetPlayerAction(PokerAction action, float amount)
    {
      remainingChipsValue += this.amount - amount;
      this.action = action;
      this.amount = amount;
      actionEntered = true;
    }

    public void AddCash(float amount)
    {
      remainingChipsValue += amount;
    }

    public void PlayerSittingOut()
    {
      sittingOut = true;
    }

    public void PlayerRejoins()
    {
      sittingOut = false;
    }

    public void SetHandCards(Card card1, Card card2)
    {
      holeCard1 = card1;
      holeCard2 = card2;
    }

  }
}
