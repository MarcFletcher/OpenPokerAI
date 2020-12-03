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

  public class NNPlayerModellingDataResult
  {

    private long _handId;

    private long _actionId;

    private short _actionTypeId;

    private float _imPotOdds;

    private float _raiseRatio;

    private float _potRatio;

    private byte _betsToCall;

    private byte _gameStage;

    private bool _calledLastRoundBets;

    private bool _raisedLastRound;

    private float _dealtInPlayers;

    private float _activePlayers;

    private float _unactedPlayers;

    private bool _flushPossible;

    private bool _straightPossible;

    private bool _aceOnBoard;

    private bool _kingOnBoard;

    private float _aceKingQueenRatio;

    private float _dealerDistance;

    public NNPlayerModellingDataResult()
    {
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

    public long actionId
    {
      get
      {
        return this._actionId;
      }
      set
      {
        if ((this._actionId != value))
        {
          this._actionId = value;
        }
      }
    }

    public short actionTypeId
    {
      get
      {
        return this._actionTypeId;
      }
      set
      {
        if ((this._actionTypeId != value))
        {
          this._actionTypeId = value;
        }
      }
    }

    public float imPotOdds
    {
      get
      {
        return this._imPotOdds;
      }
      set
      {
        if ((this._imPotOdds != value))
        {
          this._imPotOdds = value;
        }
      }
    }

    public float raiseRatio
    {
      get
      {
        return this._raiseRatio;
      }
      set
      {
        if ((this._raiseRatio != value))
        {
          this._raiseRatio = value;
        }
      }
    }

    public float potRatio
    {
      get
      {
        return this._potRatio;
      }
      set
      {
        if ((this._potRatio != value))
        {
          this._potRatio = value;
        }
      }
    }

    public byte betsToCall
    {
      get
      {
        return this._betsToCall;
      }
      set
      {
        if ((this._betsToCall != value))
        {
          this._betsToCall = value;
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

    public bool calledLastRoundBets
    {
      get
      {
        return this._calledLastRoundBets;
      }
      set
      {
        if ((this._calledLastRoundBets != value))
        {
          this._calledLastRoundBets = value;
        }
      }
    }

    public bool raisedLastRound
    {
      get
      {
        return this._raisedLastRound;
      }
      set
      {
        if ((this._raisedLastRound != value))
        {
          this._raisedLastRound = value;
        }
      }
    }

    public float dealtInPlayers
    {
      get
      {
        return this._dealtInPlayers;
      }
      set
      {
        if ((this._dealtInPlayers != value))
        {
          this._dealtInPlayers = value;
        }
      }
    }

    public float activePlayers
    {
      get
      {
        return this._activePlayers;
      }
      set
      {
        if ((this._activePlayers != value))
        {
          this._activePlayers = value;
        }
      }
    }

    public float unactedPlayers
    {
      get
      {
        return this._unactedPlayers;
      }
      set
      {
        if ((this._unactedPlayers != value))
        {
          this._unactedPlayers = value;
        }
      }
    }

    public bool flushPossible
    {
      get
      {
        return this._flushPossible;
      }
      set
      {
        if ((this._flushPossible != value))
        {
          this._flushPossible = value;
        }
      }
    }

    public bool straightPossible
    {
      get
      {
        return this._straightPossible;
      }
      set
      {
        if ((this._straightPossible != value))
        {
          this._straightPossible = value;
        }
      }
    }

    public bool aceOnBoard
    {
      get
      {
        return this._aceOnBoard;
      }
      set
      {
        if ((this._aceOnBoard != value))
        {
          this._aceOnBoard = value;
        }
      }
    }

    public bool kingOnBoard
    {
      get
      {
        return this._kingOnBoard;
      }
      set
      {
        if ((this._kingOnBoard != value))
        {
          this._kingOnBoard = value;
        }
      }
    }

    public float aceKingQueenRatio
    {
      get
      {
        return this._aceKingQueenRatio;
      }
      set
      {
        if ((this._aceKingQueenRatio != value))
        {
          this._aceKingQueenRatio = value;
        }
      }
    }

    public float dealerDistance
    {
      get
      {
        return this._dealerDistance;
      }
      set
      {
        if ((this._dealerDistance != value))
        {
          this._dealerDistance = value;
        }
      }
    }
  }
}
