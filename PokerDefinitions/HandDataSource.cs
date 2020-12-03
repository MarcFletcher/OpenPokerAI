namespace PokerBot.Definitions
{
  public enum HandDataSource : byte
  {
    Undefined = 0,

    //These are for interactive play

    PlayingTest = 1,
    PlayingReal = 2,

    // 3 and up are scraping

    Scraping = 3,
    ScrapingTest = 4,

    // 100 and up is simulated data
    SimulationTest = 100,
    GeneticTraining = 101,
    NeuralTraining = 102,

    // 255 and down is hand histories

    HandHistory_HandHQ = 254,
    HandHistory_PTR = 255,
  }
}
