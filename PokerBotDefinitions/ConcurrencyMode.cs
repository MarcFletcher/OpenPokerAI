namespace PokerBot.Definitions
{
  public static class ConcurrencyMode
  {
    public enum ConcurencyModel
    {
      MultiCore,
      Single,
    }

    public static ConcurencyModel Concurrency = ConcurencyModel.Single;
  }
}
