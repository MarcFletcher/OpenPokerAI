namespace PokerBot.Database
{
  public class PlayerAverageAdditionalRaiseAmountResult
  {

    private long _handId;

    private short _seqIndex;

    private byte _gameStage;

    private decimal _currentPotAmount;

    private decimal _bigBlind;

    private decimal _minAdditionalRaiseAmount;

    private decimal _maxAdditionalRaiseAmount;

    private decimal _additionalRaiseAmountToScale;

    public PlayerAverageAdditionalRaiseAmountResult()
    {
    }

    public PlayerAverageAdditionalRaiseAmountResult(long _handId, short _seqIndex, byte _gameStage, decimal _currentPotAmount, decimal _bigBlind, decimal _minAdditionalRaiseAmount,
        decimal _maxAdditionalRaiseAmount, decimal _additionalRaiseAmountToScale)
    {
      this._handId = _handId;
      this._seqIndex = _seqIndex;
      this._gameStage = _gameStage;
      this._currentPotAmount = _currentPotAmount;
      this._bigBlind = _bigBlind;
      this._minAdditionalRaiseAmount = _minAdditionalRaiseAmount;
      this._maxAdditionalRaiseAmount = _maxAdditionalRaiseAmount;
      this._additionalRaiseAmountToScale = _additionalRaiseAmountToScale;
    }

    public long handId
    {
      get
      {
        return this._handId;
      }
      set
      {
        if ((this._handId != value))
        {
          this._handId = value;
        }
      }
    }

    public short seqIndex
    {
      get
      {
        return this._seqIndex;
      }
      set
      {
        if ((this._seqIndex != value))
        {
          this._seqIndex = value;
        }
      }
    }

    public byte gameStage
    {
      get
      {
        return this._gameStage;
      }
      set
      {
        if ((this._gameStage != value))
        {
          this._gameStage = value;
        }
      }
    }

    public decimal currentPotAmount
    {
      get
      {
        return this._currentPotAmount;
      }
      set
      {
        if ((this._currentPotAmount != value))
        {
          this._currentPotAmount = value;
        }
      }
    }

    public decimal bigBlind
    {
      get
      {
        return this._bigBlind;
      }
      set
      {
        if ((this._bigBlind != value))
        {
          this._bigBlind = value;
        }
      }
    }

    public decimal minAdditionalRaiseAmount
    {
      get
      {
        return this._minAdditionalRaiseAmount;
      }
      set
      {
        if ((this._minAdditionalRaiseAmount != value))
        {
          this._minAdditionalRaiseAmount = value;
        }
      }
    }

    public decimal maxAdditionalRaiseAmount
    {
      get
      {
        return this._maxAdditionalRaiseAmount;
      }
      set
      {
        if ((this._maxAdditionalRaiseAmount != value))
        {
          this._maxAdditionalRaiseAmount = value;
        }
      }
    }

    public decimal additionalRaiseAmountToScale
    {
      get
      {
        return this._additionalRaiseAmountToScale;
      }
      set
      {
        if ((this._additionalRaiseAmountToScale != value))
        {
          this._additionalRaiseAmountToScale = value;
        }
      }
    }
  }
}
