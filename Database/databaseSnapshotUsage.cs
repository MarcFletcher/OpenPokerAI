namespace PokerBot.Database
{
  /// <summary>
  /// Used to tell database queries how they can use the database snapshots.
  /// If a positive number (secs) is provided a recent snapshot will be used
  /// if it's age is less than the number of seconds provided.
  /// </summary>
  public enum databaseSnapshotUsage
  {
    /// <summary>
    /// Use the most recent snapshot regardless of when it happeened.
    /// </summary>
    UseMostRecent = -1,

    /// <summary>
    /// Force a new snapshot creation for the query.
    /// </summary>
    CreateNew = 0,
  }
}
