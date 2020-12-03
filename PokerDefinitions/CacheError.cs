using System;

namespace PokerBot.Definitions
{
  public struct CacheError
  {
    public static readonly CacheError noError = new CacheError(ErrorType.NoError, null, null, null, "NoError");

    public enum ErrorType
    {
      IdNumberInvalid,
      PlayerNameInvalid,
      AmountInvalid,
      ActionError,
      CardInvalid,
      tableDoesNotExist,
      handDoesNotExist,
      HandStillOpen,
      NoError,
      CommsError,
    }

    ErrorType error;
    public ErrorType Error { get { return error; } }
    long tableId;
    public long TableID { get { return tableId; } }
    long handId;
    public long HandID { get { return handId; } }
    long playerId;
    public long PlayerID { get { return playerId; } }
    object errorValue;
    public object ErrorValue { get { return errorValue; } }

    public CacheError(ErrorType error, Nullable<long> tableId, Nullable<long> handId, Nullable<long> playerId,
        object errorValue)
    {
      this.error = error;

      if (tableId == null)
        this.tableId = -1;
      else
        this.tableId = (long)tableId;

      if (handId == null)
        this.handId = -1;
      else
        this.handId = (long)handId;

      if (playerId == null)
        this.playerId = -1;
      else
        this.playerId = (long)playerId;

      this.errorValue = errorValue;
    }

    public override string ToString()
    {
      string errorType = Enum.GetName(typeof(ErrorType), error);

      string result = "Error type : " + errorType + "\n";
      if (tableId != -1)
        result = result + " Table ID : " + tableId.ToString() + "\n";
      if (handId != -1)
        result = result + " Hand ID : " + handId.ToString() + "\n";
      if (playerId != -1)
        result = result + " Player ID : " + playerId.ToString() + "\n";
      result = result + " Error value : " + errorValue.ToString();

      return result;
    }

    public static bool operator ==(CacheError a, CacheError b)
    {
      if (a.error == b.error && a.errorValue == b.errorValue && a.handId == b.handId && a.playerId == b.playerId && a.tableId == b.tableId)
        return true;
      else
        return false;
    }

    public static bool operator !=(CacheError a, CacheError b)
    {
      if (a.error == b.error && a.errorValue == b.errorValue && a.handId == b.handId && a.playerId == b.playerId && a.tableId == b.tableId)
        return false;
      else
        return true;
    }

    public override int GetHashCode()
    {
      string errorType = Enum.GetName(typeof(ErrorType), error);

      return (errorType + tableId.ToString() + handId.ToString() + playerId.ToString() + errorValue.ToString()).GetHashCode();
    }

    public override bool Equals(object obj)
    {
      if (obj == null || obj.GetType() != GetType())
        return false;

      CacheError b = (CacheError)obj;

      if (this == b)
        return true;
      else
        return false;
    }
  }
}
