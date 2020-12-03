using System;
using System.Collections.Generic;
using System.Linq;
using PokerBot.Definitions;
using System.Threading;
using System.Diagnostics;
using System.Threading.Tasks;
using PokerBot.Database;
using PokerBot.AI;

namespace PokerBot.BotGame
{

  public class PokerGameAIError : System.ApplicationException
  {
    public PokerGameAIError(string msg)
        : base(msg)
    {

    }
  }

  /// <summary>
  /// Class which is used to playing bot vs bot and bot vs. real
  /// </summary>
  public abstract class PokerGameBase
  {
    protected static Random initialisationSeedGenerator = new Random();
    protected static object initialisationSeedLocker = new object();

    //protected Thread gameThread;
    protected Task gameTask;
    protected bool useLongRunning;
    protected PokerGameType gameType;

    protected volatile bool pauseGame;
    protected volatile bool isPaused;
    protected volatile bool endGame;
    protected volatile bool gamedFinished;

    protected volatile bool shutdownAIOnGameFinish = false;

    //protected bool endOnFirstDeath;
    protected byte minNumTablePlayers;

    protected databaseCacheClient clientCache;
    protected AIManager aiManager;

    //protected GPAJob GPAJob;

    protected CacheError cacheError;
    protected Deck currentDeck;

    protected byte currentActionPosition;
    protected long currentHandId;
    protected byte dealerIndex;
    protected int maxHandsToPlay;
    protected string[] playerNames;
    protected decimal startingStack;
    protected int actionPause;

    protected int autoRemovePlayersWithLargeStackInBB = -1;
    protected int maxHandsBeforeTableReset = -1;
    protected int handCountSinceReset = 0;

    protected int numCompletedHands = 0;

    protected byte[] activePositions;
    //protected List<byte> allInPositions = new List<byte>();

    protected databaseCacheClient.playerDetails[] playerDetails;
    protected databaseCacheClient.handDetails currentHandDetails;
    protected List<Card[]> weightedHoleCards;

    protected Random randomGen;

    bool logEveryDecision = false;

    private PokerHelper.PokerRakeDelegate RakeDelegate;

    protected bool purgeCacheOnHandEnd = true;
    protected int maxHandsToKeepInCache = 10;

    /// <summary>
    /// playerHandValue struct used for determining pot Winners
    /// </summary>
    protected class playerHandValue : IComparable
    {
      public string playerName;
      public int handValue;
      public decimal playerMoneyInPot;
      public long playerId;
      public decimal maximumWinableAmount;
      public bool hasFolded;

      public playerHandValue(int handValue, long playerId, string playerName, decimal playerMoneyInPot, decimal maxWinableAmount, bool hasFolded)
      {
        this.handValue = handValue;
        this.playerId = playerId;
        this.playerMoneyInPot = playerMoneyInPot;
        this.maximumWinableAmount = maxWinableAmount;
        this.hasFolded = hasFolded;
      }

      //Implement the compareTo method so that we can sort
      public int CompareTo(object obj)
      {
        playerHandValue otherHandValue = (playerHandValue)obj;

        //First compare handValue
        if (this.handValue > otherHandValue.handValue)
          return -1;
        else if (this.handValue < otherHandValue.handValue)
          return 1;
        else
        {
          //Compare the playerMoneyInPot
          if (this.playerMoneyInPot > otherHandValue.playerMoneyInPot)
            return -1;
          else if (this.playerMoneyInPot < otherHandValue.playerMoneyInPot)
            return 1;
          else
            return 0;
        }
      }
    }

    public PokerGameBase(PokerGameType gameType, databaseCacheClient gameCache, string[] playerNames, decimal startingStack, byte minNumTablePlayers, int maxHandsToPlay, int actionPause)
    {
      this.gameType = gameType;
      this.startingStack = startingStack;
      this.playerNames = playerNames;
      pauseGame = false;
      endGame = false;
      isPaused = false;
      this.maxHandsToPlay = maxHandsToPlay;
      this.clientCache = gameCache;
      this.minNumTablePlayers = minNumTablePlayers;
      this.actionPause = actionPause;

      lock (initialisationSeedLocker)
      {
        randomGen = new Random((int)(initialisationSeedGenerator.NextDouble() * int.MaxValue));
        currentDeck = new Deck((int)(initialisationSeedGenerator.NextDouble() * int.MaxValue));
      }

      setupWeightedHoleCards();

      aiManager = new AIManager(3600000, 1, new AIRandomControl(), true);

      //Get the rake delegate for full tilt poker 
      RakeDelegate = RakeDelegates.GetRakeDelegate(10);
    }

    public PokerGameBase(PokerGameType gameType, databaseCacheClient gameCache, string[] playerNames, decimal startingStack, byte minNumTablePlayers, int maxHandsToPlay, int actionPause, AIManager aiManager)
    {
      this.gameType = gameType;
      this.startingStack = startingStack;
      this.playerNames = playerNames;
      pauseGame = false;
      endGame = false;
      isPaused = false;
      this.maxHandsToPlay = maxHandsToPlay;
      this.clientCache = gameCache;
      this.minNumTablePlayers = minNumTablePlayers;
      this.actionPause = actionPause;
      this.aiManager = aiManager;

      //this.GPAJob = GPAJob;

      lock (initialisationSeedLocker)
      {
        randomGen = new Random((int)(initialisationSeedGenerator.NextDouble() * int.MaxValue));
        currentDeck = new Deck((int)(initialisationSeedGenerator.NextDouble() * int.MaxValue));
      }

      setupWeightedHoleCards();

      //Get the rake delegate for full tilt poker 
      RakeDelegate = RakeDelegates.GetRakeDelegate(10);
    }

    public static class RakeDelegates
    {

      public static PokerHelper.PokerRakeDelegate GetRakeDelegate(int clientId)
      {
        switch (clientId)
        {
          case 1:
            return CalculateZeroRake;
          case 10:
            return CalculateFTPRake;
          case 4:
            return CalculatePSDollarRake;
          case 25:
            return CalculatePSEuroRake;
          case 27:
            return CalculatePSPoundRake;
          case 3:
            return CalculatePPRake;
          case 2:
            return CalculateFTPRake;
          default:
            throw new Exception("No delegate written for specified client number " + clientId);
        }
      }

      public static PokerHelper.PokerRakeDelegate GetRakeDelegate(PokerClients client)
      {
        return GetRakeDelegate((int)client);
      }

      #region RakeDelegates
      private static decimal CalculateZeroRake(databaseCacheClient clientCache, List<decimal> potValues, int potIndex)
      {
        return 0;
      }

      private static decimal CalculateFTPRake(databaseCacheClient clientCache, List<decimal> potValues, int potIndex)
      {
        decimal totalPotToIndex = 0;
        decimal totalPotToBeforeIndex = 0;

        for (int i = 0; i < potIndex; i++)
        {
          totalPotToBeforeIndex += potValues[i];
        }

        totalPotToIndex = totalPotToBeforeIndex + potValues[potIndex];

        decimal rakeBefore = CalculateFTPRake(clientCache, totalPotToBeforeIndex);
        decimal rakeTotal = CalculateFTPRake(clientCache, totalPotToIndex);

        return rakeTotal - rakeBefore;
      }

      private static decimal CalculateFTPRake(databaseCacheClient clientCache, decimal potValue)
      {
        var handDetails = clientCache.getCurrentHandDetails();

        int numPlayers = handDetails.numStartPlayers;
        decimal bigBlind = clientCache.BigBlind;
        decimal rake, maxRake;

        if (numPlayers < 2)
          numPlayers = 2;


        if (handDetails.tableCard1 == (byte)Card.NoCard)
          return 0;

        if (bigBlind < 0.25m)
        {
          rake = (int)(potValue / 0.15m) * 0.01m;

          if (numPlayers == 2)
            maxRake = 0.50m;
          else if (numPlayers <= 4)
            maxRake = 1.00m;
          else if (numPlayers <= 10)
            maxRake = 2.00m;
          else
            throw new Exception();
        }
        else if (bigBlind == 0.25m)
        {
          rake = (int)(potValue / 0.20m) * 0.01m;

          if (numPlayers == 2)
            maxRake = 0.50m;
          else if (numPlayers == 3)
            maxRake = 1.00m;
          else if (numPlayers == 4)
            maxRake = 2.00m;
          else if (numPlayers <= 10)
            maxRake = 3.00m;
          else
            throw new Exception();
        }
        else
        {
          rake = (int)(potValue / 1.00m) * 0.05m;

          if (numPlayers == 2)
            maxRake = 0.50m;
          else if (numPlayers == 3)
            maxRake = 1.00m;
          else if (numPlayers == 4)
            maxRake = 2.00m;
          else if (numPlayers <= 10)
            maxRake = 3.00m;
          else
            throw new Exception();
        }

        if (rake > maxRake)
          rake = maxRake;

        return rake;
      }

      private static decimal CalculatePSDollarRake(databaseCacheClient clientCache, List<decimal> potValues, int potIndex)
      {
        decimal totalPotToIndex = 0;
        decimal totalPotToBeforeIndex = 0;

        for (int i = 0; i < potIndex; i++)
        {
          totalPotToBeforeIndex += potValues[i];
        }

        totalPotToIndex = totalPotToBeforeIndex + potValues[potIndex];

        decimal rakeBefore = CalculatePSDollarRakeNew(clientCache, totalPotToBeforeIndex);
        decimal rakeTotal = CalculatePSDollarRakeNew(clientCache, totalPotToIndex);

        return rakeTotal - rakeBefore;
      }

      [Obsolete("Poker stars rake function used before 01/02/12.  For later tables use CalculatePSDollarRakeNew")]
      private static decimal CalculatePSDollarRake(databaseCacheClient clientCache, decimal potValue)
      {
        var handDetails = clientCache.getCurrentHandDetails();

        int numPlayers = handDetails.numStartPlayers;
        decimal bigBlind = clientCache.BigBlind;
        decimal rake, maxRake;

        if (numPlayers < 2)
          numPlayers = 2;


        if (handDetails.tableCard1 == (byte)Card.NoCard)
          return 0;

        if (bigBlind <= 0.25m)
        {
          rake = 0.01m * ((int)(1000 * (0.01m * potValue / 0.2m)) / 10);

          if (numPlayers == 2)
            maxRake = 0.50m;
          else if (numPlayers <= 4)
            maxRake = 1.00m;
          else if (numPlayers <= 10)
            maxRake = 2.00m;
          else
            throw new Exception();
        }
        else if (bigBlind <= 100.00m)
        {
          rake = 0.01m * ((int)(1000 * (0.05m * potValue / 1.0m)) / 10);

          if (numPlayers == 2)
            maxRake = 0.50m;
          else if (numPlayers == 3)
            maxRake = 1.00m;
          else if (numPlayers <= 5)
            maxRake = 2.00m;
          else if (numPlayers <= 10)
            maxRake = 3.00m;
          else
            throw new Exception();
        }
        else
        {
          rake = 0.01m * ((int)(1000 * (1.0m * potValue / 100.0m)) / 10);

          if (numPlayers <= 3)
            maxRake = 2.00m;
          else if (numPlayers <= 10)
            maxRake = 5.00m;
          else
            throw new Exception();
        }

        if (rake > maxRake)
          rake = maxRake;

        return rake;
      }

      private static decimal CalculatePSDollarRakeNew(databaseCacheClient clientCache, decimal potValue)
      {
        var handDetails = clientCache.getCurrentHandDetails();

        int numPlayers = handDetails.numStartPlayers;
        decimal bigBlind = clientCache.BigBlind;
        decimal rake, maxRake;

        if (numPlayers < 2)
          numPlayers = 2;

        if (handDetails.tableCard1 == (byte)Card.NoCard)
          return 0;

        if (bigBlind == 0.02m)
        {
          rake = Math.Round(0.035m * potValue, 2, MidpointRounding.ToEven);
          maxRake = 0.3m;
        }
        else if (bigBlind == 0.05m)
        {
          rake = Math.Round(0.0415m * potValue, 2, MidpointRounding.ToEven);

          if (numPlayers <= 4)
            maxRake = 0.5m;
          else
            maxRake = 1;
        }
        else
        {
          rake = Math.Round(0.045m * potValue, 2, MidpointRounding.ToEven);

          if (numPlayers == 2)
          {
            if (bigBlind <= 50)
              maxRake = 0.5m;
            else
              maxRake = 2;
          }
          else if (numPlayers <= 4)
          {
            if (bigBlind <= 0.25m)
              maxRake = 1;
            else if (bigBlind <= 6)
              maxRake = 1.5m;
            else if (bigBlind == 50)
              maxRake = 2;
            else if (bigBlind == 100)
              maxRake = 3;
            else
              maxRake = 5;

          }
          else
          {
            if (bigBlind <= 0.16m)
              maxRake = 1.5m;
            else if (bigBlind == 0.25m)
              maxRake = 2;
            else if (bigBlind == 0.5m)
              maxRake = 2.5m;
            else if (bigBlind <= 6)
              maxRake = 2.8m;
            else if (bigBlind <= 50)
              maxRake = 3;
            else
              maxRake = 5;
          }
        }


        if (rake > maxRake)
          rake = maxRake;

        return rake;
      }

      private static decimal CalculatePSEuroRake(databaseCacheClient clientCache, List<decimal> potValues, int potIndex)
      {
        decimal totalPotToIndex = 0;
        decimal totalPotToBeforeIndex = 0;

        for (int i = 0; i < potIndex; i++)
        {
          totalPotToBeforeIndex += potValues[i];
        }

        totalPotToIndex = totalPotToBeforeIndex + potValues[potIndex];

        decimal rakeBefore = CalculatePSEuroRakeNew(clientCache, totalPotToBeforeIndex);
        decimal rakeTotal = CalculatePSEuroRakeNew(clientCache, totalPotToIndex);

        return rakeTotal - rakeBefore;
      }

      [Obsolete("Poker stars rake function used before 01/02/12.  For later tables use CalculatePSEuroRakeNew")]
      private static decimal CalculatePSEuroRake(databaseCacheClient clientCache, decimal potValue)
      {
        var handDetails = clientCache.getCurrentHandDetails();

        int numPlayers = handDetails.numStartPlayers;
        decimal bigBlind = clientCache.BigBlind;
        decimal rake, maxRake;

        if (numPlayers < 2)
          numPlayers = 2;


        if (handDetails.tableCard1 == (byte)Card.NoCard)
          return 0;

        if (bigBlind <= 0.25m)
        {
          rake = 0.01m * ((int)(1000 * (0.01m * potValue / 0.2m)) / 10);

          if (numPlayers == 2)
            maxRake = 0.50m;
          else if (numPlayers <= 4)
            maxRake = 1.00m;
          else if (numPlayers <= 10)
            maxRake = 2.00m;
          else
            throw new Exception();
        }
        else
        {
          rake = 0.01m * ((int)(1000 * (0.05m * potValue / 1.0m)) / 10);

          if (numPlayers == 2)
            maxRake = 0.50m;
          else if (numPlayers <= 4)
            maxRake = 1.00m;
          else if (numPlayers <= 10)
            maxRake = 2.00m;
          else
            throw new Exception();
        }

        if (rake > maxRake)
          rake = maxRake;

        return rake;
      }

      private static decimal CalculatePSEuroRakeNew(databaseCacheClient clientCache, decimal potValue)
      {
        var handDetails = clientCache.getCurrentHandDetails();

        int numPlayers = handDetails.numStartPlayers;
        decimal bigBlind = clientCache.BigBlind;
        decimal rake, maxRake;

        if (numPlayers < 2)
          numPlayers = 2;

        if (handDetails.tableCard1 == (byte)Card.NoCard)
          return 0;

        if (bigBlind == 0.02m)
        {
          rake = Math.Round(0.035m * potValue, 2, MidpointRounding.ToEven);
          maxRake = 0.25m;
        }
        else if (bigBlind == 0.05m)
        {
          rake = Math.Round(0.0415m * potValue, 2, MidpointRounding.ToEven);

          if (numPlayers <= 4)
            maxRake = 0.5m;
          else
            maxRake = 0.75m;
        }
        else
        {
          rake = Math.Round(0.045m * potValue, 2, MidpointRounding.ToEven);

          if (numPlayers == 2)
            maxRake = 0.5m;
          else if (numPlayers <= 4)
          {
            if (bigBlind == 0.1m || bigBlind == 0.25m)
              maxRake = 0.75m;
            else
              maxRake = 1.25m;
          }
          else
          {
            if (bigBlind == 0.1m)
              maxRake = 1.25m;
            else if (bigBlind == 0.25m)
              maxRake = 1.5m;
            else if (bigBlind == 0.5m)
              maxRake = 2;
            else if (bigBlind <= 6)
              maxRake = 2.15m;
            else
              maxRake = 2.25m;
          }
        }


        if (rake > maxRake)
          rake = maxRake;

        return rake;
      }

      private static decimal CalculatePSPoundRake(databaseCacheClient clientCache, List<decimal> potValues, int potIndex)
      {
        decimal totalPotToIndex = 0;
        decimal totalPotToBeforeIndex = 0;

        for (int i = 0; i < potIndex; i++)
        {
          totalPotToBeforeIndex += potValues[i];
        }

        totalPotToIndex = totalPotToBeforeIndex + potValues[potIndex];

        decimal rakeBefore = CalculatePSPoundRake(clientCache, totalPotToBeforeIndex);
        decimal rakeTotal = CalculatePSPoundRake(clientCache, totalPotToIndex);

        return rakeTotal - rakeBefore;
      }

      private static decimal CalculatePSPoundRake(databaseCacheClient clientCache, decimal potValue)
      {
        var handDetails = clientCache.getCurrentHandDetails();

        int numPlayers = handDetails.numStartPlayers;
        decimal bigBlind = clientCache.BigBlind;
        decimal rake, maxRake;

        if (numPlayers < 2)
          numPlayers = 2;


        if (handDetails.tableCard1 == (byte)Card.NoCard)
          return 0;

        if (bigBlind <= 0.25m)
        {
          rake = 0.01m * ((int)(1000 * (0.01m * potValue / 0.2m)) / 10);

          if (numPlayers == 2)
            maxRake = 0.50m;
          else if (numPlayers <= 4)
            maxRake = 1.00m;
          else if (numPlayers <= 10)
            maxRake = 2.00m;
          else
            throw new Exception();
        }
        else
        {
          rake = 0.01m * ((int)(1000 * (0.05m * potValue / 1.0m)) / 10);

          if (numPlayers == 2)
            maxRake = 0.50m;
          else if (numPlayers <= 4)
            maxRake = 1.00m;
          else if (numPlayers <= 10)
            maxRake = 2.00m;
          else
            throw new Exception();
        }

        if (rake > maxRake)
          rake = maxRake;

        return rake;
      }

      private static decimal CalculatePPRake(databaseCacheClient clientCache, List<decimal> potValues, int potIndex)
      {
        decimal totalPotToIndex = 0;
        decimal totalPotToBeforeIndex = 0;

        for (int i = 0; i < potIndex; i++)
        {
          totalPotToBeforeIndex += potValues[i];
        }

        totalPotToIndex = totalPotToBeforeIndex + potValues[potIndex];

        decimal rakeBefore = CalculatePPRake(clientCache, totalPotToBeforeIndex);
        decimal rakeTotal = CalculatePPRake(clientCache, totalPotToIndex);

        return rakeTotal - rakeBefore;
      }

      private static decimal CalculatePPRake(databaseCacheClient clientCache, decimal potValue)
      {
        var handDetails = clientCache.getCurrentHandDetails();

        int numPlayers = handDetails.numStartPlayers;
        decimal bigBlind = clientCache.BigBlind;
        decimal rake, maxRake;

        if (numPlayers < 2)
          numPlayers = 2;

        if (handDetails.tableCard1 == (byte)Card.NoCard)
          return 0;

        if (bigBlind < 0.10m)
        {
          rake = (int)(potValue / 0.10m) * 0.01m;
          maxRake = 2.00m;
        }
        else if (bigBlind <= 0.30m)
        {
          rake = (int)(potValue / 0.20m) * 0.01m;
          maxRake = 0.50m;
        }
        else if (bigBlind <= 2.00m)
        {
          rake = (int)(potValue / 1.00m) * 0.05m;
          maxRake = 1.00m;
        }
        else if (bigBlind == 4.00m)
        {
          rake = (int)(potValue / 5.00m) * 0.25m;

          if (numPlayers == 2)
            maxRake = 1.00m;
          else if (numPlayers <= 4)
            maxRake = 2.00m;
          else
            maxRake = 3.00m;
        }
        else if (bigBlind <= 20.00m)
        {
          rake = (int)(potValue / 10.00m) * 0.50m;

          if (numPlayers == 2)
            maxRake = 1.00m;
          else if (numPlayers <= 4)
            maxRake = 2.00m;
          else
            maxRake = 3.00m;
        }
        else if (bigBlind <= 60.00m)
        {
          if (numPlayers == 2)
            rake = (int)(potValue / 20.00m) * 0.50m;
          else
            rake = (int)(potValue / 20.00m) * 1.00m;

          if (numPlayers == 2)
            maxRake = 1.00m;
          else if (numPlayers <= 4)
            maxRake = 2.00m;
          else
            maxRake = 3.00m;
        }
        else
        {
          rake = (int)(potValue / 100.00m) * 1.00m;

          if (numPlayers <= 3)
            maxRake = 2.00m;
          else
            maxRake = 5.00m;
        }

        if (rake > maxRake)
          rake = maxRake;

        return rake;
      }

      #endregion

    }

    #region GetSet
    /// <summary>
    /// Get and set parameters for pauseGame which is used to control thread execution
    /// </summary>
    public bool PauseGame
    {
      set { pauseGame = value; }
      get { return pauseGame; }
    }

    public bool PurgeCacheOnHandEnd
    {
      set { purgeCacheOnHandEnd = value; }
      get { return purgeCacheOnHandEnd; }
    }

    /// <summary>
    /// Get and set parameters for holdGame which is used to control thread execution
    /// </summary>
    public bool EndGame
    {
      set { endGame = value; }
      get { return endGame; }
    }

    /// <summary>
    /// Returns true once the poker game has finished.
    /// </summary>
    public bool GameFinished
    {
      get { return gamedFinished; }
    }

    public bool IsPaused
    {
      get { return isPaused; }
    }

    public int CompletedHands
    {
      get { return numCompletedHands; }
    }

    #endregion

    /// <summary>
    /// Shuts down the AI Manager used by the bot game as a reference may not exist outside of this class
    /// </summary>
    public void ShutdownAIOnFinish()
    {
      shutdownAIOnGameFinish = true;
    }

    /// <summary>
    /// Sets up the weighted hole card list.
    /// </summary>
    public void setupWeightedHoleCards()
    {
      weightedHoleCards = new List<Card[]>();

      //Tight Agressive Hole Cards
      //tightAggressiveHoleCards.Add(new Card[] { Card.ClubsA, Card.DiamondsA });
      //tightAggressiveHoleCards.Add(new Card[] { Card.ClubsK, Card.DiamondsK });
      //tightAggressiveHoleCards.Add(new Card[] { Card.ClubsQ, Card.DiamondsQ });
      //tightAggressiveHoleCards.Add(new Card[] { Card.ClubsJ, Card.DiamondsJ });
      //tightAggressiveHoleCards.Add(new Card[] { Card.Clubs10, Card.Diamonds10 });
      //tightAggressiveHoleCards.Add(new Card[] { Card.ClubsA, Card.DiamondsK });


      weightedHoleCards.Add(new Card[] { Card.ClubsA, Card.ClubsK });
      weightedHoleCards.Add(new Card[] { Card.ClubsA, Card.ClubsQ });
      weightedHoleCards.Add(new Card[] { Card.ClubsA, Card.ClubsJ });
      weightedHoleCards.Add(new Card[] { Card.ClubsA, Card.Clubs10 });
      weightedHoleCards.Add(new Card[] { Card.ClubsA, Card.DiamondsJ });
      weightedHoleCards.Add(new Card[] { Card.ClubsA, Card.DiamondsQ });
      weightedHoleCards.Add(new Card[] { Card.ClubsA, Card.Diamonds10 });
      //tightAggressiveHoleCards.Add(new Card[] { Card.Clubs7, Card.Clubs6 });
      /*
      tightAggressiveHoleCards.Add(new Card[] { Card.ClubsA, Card.ClubsQ });

      tightAggressiveHoleCards.Add(new Card[] { Card.Clubs9, Card.Clubs7 });
      tightAggressiveHoleCards.Add(new Card[] { Card.Clubs8, Card.Clubs7 });
      tightAggressiveHoleCards.Add(new Card[] { Card.Clubs8, Card.Clubs6 });

      tightAggressiveHoleCards.Add(new Card[] { Card.Clubs7, Card.Clubs5 });
      tightAggressiveHoleCards.Add(new Card[] { Card.Clubs6, Card.Clubs5 });
      tightAggressiveHoleCards.Add(new Card[] { Card.Clubs6, Card.Clubs4 });
      /*
      tightAggressiveHoleCards.Add(new Card[] { Card.Clubs5, Card.Clubs4 });
      tightAggressiveHoleCards.Add(new Card[] { Card.Clubs5, Card.Clubs3 });
      tightAggressiveHoleCards.Add(new Card[] { Card.Clubs4, Card.Clubs3 });
      tightAggressiveHoleCards.Add(new Card[] { Card.Clubs4, Card.Clubs2 });
      tightAggressiveHoleCards.Add(new Card[] { Card.Clubs3, Card.Clubs2 });
      */
      //Loose Aggressive Hole Cards

      /*
      tightAggressiveHoleCards.Add(new Card[] { Card.Clubs8, Card.Diamonds8 });
      tightAggressiveHoleCards.Add(new Card[] { Card.Clubs7, Card.Diamonds7 });
      tightAggressiveHoleCards.Add(new Card[] { Card.Clubs5, Card.Diamonds5 });
      tightAggressiveHoleCards.Add(new Card[] { Card.Clubs3, Card.Diamonds3 });
       */
    }

    /// <summary>
    /// Start the game within a task. Returns immediately.
    /// </summary>
    public void startGameTask()
    {
      gamedFinished = false;

      //if (ConcurrencyMode.Concurrency == ConcurrencyMode.ConcurencyModel.MultiCore)
      gameTask = Task.Factory.StartNew(playGame, useLongRunning ? TaskCreationOptions.LongRunning : TaskCreationOptions.None);
      //else
      //playGame();

      //gameThread = new Thread(playGame);
      //gameThread.Name = "FBPGameSimulator";            
      //gameThread.Start();
    }

    /// <summary>
    /// Start the game. Returns once the game has been competed.
    /// </summary>
    public void playGame()
    {
      try
      {
        //DateTime startTime = DateTime.Now;

        //Initialise the first hand.
        initialiseHand();
        SitDownTableStartingPlayers(playerNames);

        //We just sat down a load of blank players, we now need to make sure we sit down real players
        //where possible
        SitOutInPlayers();

        do
        {
          //Start a new hand if one does not exist
          if (clientCache.getCurrentHandId() == -1)
            initialiseHand();

          if (clientCache.getActivePositions().Length > 1)
          {
            placeBlinds();
            dealPlayerCards(false, 0);
            bettingRounds();

            if (PokerHelper.ReturnUncalledBets(clientCache) != CacheError.noError)
              throw new Exception("Error while trying to return uncalled bets.");

            if (PokerHelper.AwardPot(clientCache, RakeDelegate, numCompletedHands) != CacheError.noError)
              throw new Exception("Error while trying to award pot.");

            //Just slow the endgame down so that we get a chance to see if in the UI
            if (actionPause > 0)
            {
              if (clientCache.getActivePositions().Length > 1)
                Thread.Sleep(actionPause * 6);
              else
                Thread.Sleep(actionPause * 2);
            }
          }

          //Sitout and remove dead players
          //In geneticNeuralTraining this all removes players with big stacks and sits down new players
          SitOutInPlayers();

          finishHand();

          //Debugging Section
          //Game is slowed down exceptionally by the increasing length of pokerhands, holecards and handactions
          //Because of this we need to to be able to purge all previous hands, holecards and actions
          if (purgeCacheOnHandEnd)
          {
            clientCache.purgeHoleCards();
            clientCache.purgePokerActions();
            clientCache.purgePokerHands();
          }
          else
          {
            //This is seperate because of the performance of clientCache.HandIdsInCache()
            if (clientCache.HandIdsInCache().Length > maxHandsToKeepInCache)
            {
              clientCache.purgeHoleCards();
              clientCache.purgePokerActions();
              clientCache.purgePokerHands();
            }
          }

          //endGame = true;
          Debug.Print(clientCache.getNumHandsPlayed() + " Hands Completed.");

        } while (!endGame);

        //Debug.Print((DateTime.Now - startTime).TotalMilliseconds.ToString() + "ms for table to complete "+numCompletedHands+" hands.");
        //Do any remaining commits.
        //clientCache.CommitDatabase();
        //Debug.Print("Bot game finished.");
      }
      catch (PokerGameAIError)
      {
        //Do nothing for AI errors as it is already logged by AI Manager
        //We just end the game.
      }
      catch (Exception ex)
      {
        string fileName = LogError.Log(ex, "PokerGameError");
        clientCache.SaveToDisk("", fileName);
      }

      if (shutdownAIOnGameFinish && aiManager != null)
        aiManager.Shutdown();

      gamedFinished = true;
      endGame = true;

    }

    /// <summary>
    /// Sits down all of the nessesary players.
    /// </summary>
    protected virtual void SitDownTableStartingPlayers(string[] playerNames)
    {
      if (numCompletedHands == 0)
      {
        long playerId = 0;
        for (byte i = 0; i < playerNames.Count(); i++)
        {

          cacheError = clientCache.newTablePlayer(playerNames[i], 0, i, false, ref playerId);
          if (cacheError != CacheError.noError)
            throw new Exception("Error!");

          if (playerNames[i] != "")
          {

            cacheError = clientCache.newHandAction(playerId, PokerAction.JoinTable, i);
            if (cacheError != CacheError.noError)
              throw new Exception("Error!");

            cacheError = clientCache.newHandAction(playerId, PokerAction.AddStackCash, startingStack);
            if (cacheError != CacheError.noError)
              throw new Exception("Error!");
          }
        }
      }

    }

    /// <summary>
    /// Starts a new hand, shuffles the card deck and sets the currentActionPosition
    /// </summary>
    protected void initialiseHand()
    {
      #region initialiseHand
      //Start a new hand
      cacheError = clientCache.newHand(dealerIndex, ref currentHandId);
      if (cacheError != CacheError.noError)
        throw new Exception("Cache error starting new hand.");

      currentDeck.Shuffle();
      currentActionPosition = dealerIndex;

      //Get all of the player details from the cache
      playerDetails = clientCache.getPlayerDetails();

      #endregion initialiseHand
    }

    /// <summary>
    /// Place the compulsory blinds
    /// </summary>
    protected void placeBlinds()
    {
      ///////////////////////////////////////////////////
      // 1. Post little and big blind from dealer index//
      ///////////////////////////////////////////////////
      //Need to allow people to play even if their stack amount is less than littleBlind & bigBlind

      //Recording the action updates potvalue and player stack ;)
      currentActionPosition = clientCache.getNextActiveTablePosition(currentActionPosition);
      if (clientCache.getPlayerStack(clientCache.getPlayerId(currentActionPosition)) - clientCache.LittleBlind < 0)
        throw new Exception("This should no longer happen.");
      //cacheError = clientCache.newHandAction(clientCache.getPlayerId(currentActionPosition), PokerAction.LittleBlind, clientCache.getPlayerStack(clientCache.getPlayerId(currentActionPosition)));
      else
        cacheError = clientCache.newHandAction(clientCache.getPlayerId(currentActionPosition), PokerAction.LittleBlind, clientCache.LittleBlind);

      if (cacheError != CacheError.noError)
        throw new Exception("Cache error posting little blind.");

      currentActionPosition = clientCache.getNextActiveTablePosition(currentActionPosition);
      if (clientCache.getPlayerStack(clientCache.getPlayerId(currentActionPosition)) - clientCache.BigBlind < 0)
        cacheError = clientCache.newHandAction(clientCache.getPlayerId(currentActionPosition), PokerAction.BigBlind, clientCache.getPlayerStack(clientCache.getPlayerId(currentActionPosition)));
      else
        cacheError = clientCache.newHandAction(clientCache.getPlayerId(currentActionPosition), PokerAction.BigBlind, clientCache.BigBlind);

      if (cacheError != CacheError.noError)
        throw new Exception("Cache error posting big blind.");
    }

    /// <summary>
    /// Deal out all player cards.
    /// </summary>
    /// <param name="dealWeightedCards">True = deal out weighted cards.</param>
    /// <param name="percentPlayersWeightedCards">Between 0 and 1 - the percent of players to whom weighted cards are dealt.</param>
    protected virtual void dealPlayerCards(bool dealWeightedCards, double percentPlayersWeightedCards)
    {
      double randomNum;

      ///////////////////////////////////////////////////////////////////////////
      //2. Deal cards to all active players (starting at currentActionPosition)//
      ///////////////////////////////////////////////////////////////////////////
      activePositions = clientCache.getActivePositions(dealerIndex);
      for (int i = 0; i < activePositions.Length; i++)
      {
        ///*
        randomNum = randomGen.NextDouble();
        //I have disabled the weighted card dealing by setting the expression < -1 (which is impossible for r.NextDouble()
        //Normally use 0.08
        if (dealWeightedCards && randomNum < percentPlayersWeightedCards)
        {
          #region dealWeightedCards
          int loop = 0;
          while (true)
          {
            //We may want to deal out cards of our choosing
            randomNum = randomGen.NextDouble();
            int handCardIndex = (int)(randomNum * weightedHoleCards.Count());

            //Loop safety
            if (loop > weightedHoleCards.Count() * 3)
            {
              cacheError = clientCache.newHoleCards(clientCache.getPlayerId(activePositions.ElementAt(i)), currentDeck.GetNextCard(), currentDeck.GetNextCard());
              if (cacheError != CacheError.noError)
                throw new Exception("Cache error dealing out hole cards.");
              break;
            }

            if (!currentDeck.CardAlreadyDealt(weightedHoleCards.ElementAt(handCardIndex)[0]) && !currentDeck.CardAlreadyDealt(weightedHoleCards.ElementAt(handCardIndex)[1]))
            {
              //Remove the cards
              currentDeck.RemoveCard((int)weightedHoleCards.ElementAt(handCardIndex)[0]);
              currentDeck.RemoveCard((int)weightedHoleCards.ElementAt(handCardIndex)[1]);

              cacheError = clientCache.newHoleCards(clientCache.getPlayerId(activePositions.ElementAt(i)), (byte)weightedHoleCards.ElementAt(handCardIndex)[0], (byte)weightedHoleCards.ElementAt(handCardIndex)[1]);
              if (cacheError != CacheError.noError)
                throw new Exception("Cache error dealing out hole cards.");
              break;
            }

            loop++;

          }
          #endregion
        }
        else
        {
          cacheError = clientCache.newHoleCards(clientCache.getPlayerId(activePositions.ElementAt(i)), currentDeck.GetNextCard(), currentDeck.GetNextCard());
          if (cacheError != CacheError.noError)
            throw new Exception("Cache error dealing out hole cards.");
        }
        //*/
        //cacheError = clientCache.newHoleCards(clientCache.getPlayerId(activePositions.ElementAt(i)), currentDeck.GetNextCard(), currentDeck.GetNextCard());
        //if (cacheError != CacheError.noError) throw new Exception("Cache error dealing out hole cards.");
      }
    }

    /// <summary>
    /// Part of play game.
    /// </summary>
    protected void bettingRounds()
    {

      bool repeatBettingRound;
      bool actionThisBettingRound;
      Play playerDecision;
      //allInPositions = new List<byte>();

      //We need to check for all ins here because of the all in on blind problem
      activePositions = clientCache.getActivePositions();
      /*
      for(int i=0; i<activePositions.Length; i++)
          if (clientCache.getPlayerStack(clientCache.getPlayerId(activePositions[i])) == 0)
              allInPositions.Add(currentActionPosition);
      */
      List<byte> allInPositions = clientCache.getAllInPositions().ToList();

      for (int bettingRound = 0; bettingRound < 4; bettingRound++)
      {

        playerDetails = clientCache.getPlayerDetails();

        //currentActionPosition = clientCache.getNextActiveTablePosition(currentActionPosition);

        //If there is only a single player left then they win by default
        if (clientCache.getActivePositions().Length == 1)
        {
          bettingRound = 4;
          break;
        }

        //Update table cards if neccessary
        dealTableCards(bettingRound);

        ///////////////////////////////////////////////
        // Get decisions from all (recording action) //
        ///////////////////////////////////////////////

        //We need to keep getting decisions until everyone has put in the same amount of dosh
        //i.e. we have been round once without any reraises (repeatBettingRound=false)
        int loopSafety = 0;
        int repeatBettingRoundUptoPosition = 250;

        //We only get additional decisions if more than 2 players are still involved.
        if (clientCache.getActivePositions().Count() - allInPositions.Count() > 1)
        {
          //If we only have one player left who is not all in we do not need a decision from them however we should keep drawing cards
          do
          {
            //Decreased from 300 to 50 to make sure we see all possible bugs 
            //Increased from 50 to 100 as 50 was too small (legitimate hands were dying)
            if (loopSafety > 2000 || (autoRemovePlayersWithLargeStackInBB > 1 && loopSafety > autoRemovePlayersWithLargeStackInBB))
              throw new Exception("Betting loop safety triggered.");

            activePositions = clientCache.getActivePositions();
            repeatBettingRound = false;
            actionThisBettingRound = false;

            for (int i = 0; i < activePositions.Length; i++)
            {

              //There are some conditions which result in a round termination
              //This is deliberately clientCache.getActivePositions() because it needs to check each loop through the for!!
              if (clientCache.getActivePositions().Length == 1)
              {
                //If there is only a single player left then they win by default, go straight to the end of the game.
                bettingRound = 4;
                break;
              }

              //If there is no-one left to act then we can move onto the next round.
              if (clientCache.getActivePositionsLeftToAct().Length == 0)
                //If there is only one player who is not all in break this round.
                break;

              //Increment the activeposition
              currentActionPosition = clientCache.getCurrentActiveTablePosition();

              /*
              if (currentActionPosition == repeatBettingRoundUptoPosition)
                  //If the current position is back to the repeat betting position and no-one else has reraised this round of betting is fininished
                  break;
              */

              //Get a decision if this player is not all in
              if (!allInPositions.Contains(currentActionPosition))
              {
                if (clientCache.getActivePositions().Length == 1)
                  throw new Exception("This should be impossible at this point!!");

                #region get decision and decision logging
                playerDecision = getPlayerDecision();

                //If player decision is AIGeneralError just throw an exception (it's already been logged)
                //An AI error at this point should end the game.
                if (playerDecision.Action < 0)
                  throw new PokerGameAIError("AI error occured.");

                //We only record the raise for the purpose of going round again if this is not the first player
                //And if there was a previous betting action
                if (playerDecision.Action == PokerAction.Call || playerDecision.Action == PokerAction.Raise || playerDecision.Action == PokerAction.Check)
                  actionThisBettingRound = true;

                if (playerDecision.Action == PokerAction.Raise && actionThisBettingRound)
                {
                  repeatBettingRound = true;
                  repeatBettingRoundUptoPosition = currentActionPosition;
                }

                cacheError = clientCache.newHandAction(clientCache.getPlayerId(currentActionPosition), playerDecision.Action, playerDecision.Amount);
                if (cacheError != CacheError.noError)
                  throw new Exception("Cache error while recording player play action (" + playerDecision.Action + ":" + playerDecision.Amount + ")." + cacheError.Error + "." + cacheError.ErrorValue);

                //Any logging has to happen here
                //if (logEveryDecision && playerDecision.AiDecisionStr != "")
                //    databaseQueries.logAiDecision(clientCache.getCurrentHandId(), clientCache.getCurrentHandSeqIndex(), playerDecision.AiDecisionStr, 1);

                #endregion

                //Check to see if this player is allIn, if he is then add him to all in positions
                //If player is now all in add them to the all in list
                if (clientCache.getPlayerStack(clientCache.getPlayerId(currentActionPosition)) == 0)
                  allInPositions.Add(currentActionPosition);
              }
            }

            loopSafety++;

          } while (repeatBettingRound == true);
        }
      }

    }

    /// <summary>
    /// Each game type must override this method to determine how it arrives at it's decision.
    /// </summary>
    /// <returns></returns>
    protected abstract Play getPlayerDecision();

    /// <summary>
    /// Deals out the necessary table cards
    /// </summary>
    /// <param name="bettingRound"></param>
    protected virtual void dealTableCards(int bettingRound)
    {
      #region tableCards

      //Sleep at the beginning so that we can see that action from the player who acted just before the dealer
      if (actionPause > 0)
        Thread.Sleep(actionPause);

      switch (bettingRound)
      {
        case 1:
          //Record deal flop
          cacheError = clientCache.newHandAction(clientCache.getPlayerId(dealerIndex), PokerAction.DealFlop, 0);
          if (cacheError != CacheError.noError)
            throw new Exception("Cache error recording deal flop.");
          cacheError = clientCache.updateTableCards(currentDeck.GetNextCard(), currentDeck.GetNextCard(), currentDeck.GetNextCard(), 0, 0);
          if (cacheError != CacheError.noError)
            throw new Exception("Cache error adding flop cards.");
          break;
        case 2:
          //Record deal turn
          cacheError = clientCache.newHandAction(clientCache.getPlayerId(dealerIndex), PokerAction.DealTurn, 0);
          if (cacheError != CacheError.noError)
            throw new Exception("Cache error recording deal turn.");
          cacheError = clientCache.updateTableCards(0, 0, 0, currentDeck.GetNextCard(), 0);
          if (cacheError != CacheError.noError)
            throw new Exception("Cache error adding turn card.");
          break;
        case 3:
          //Record deal river
          cacheError = clientCache.newHandAction(clientCache.getPlayerId(dealerIndex), PokerAction.DealRiver, 0);
          if (cacheError != CacheError.noError)
            throw new Exception("Cache error recording deal river.");
          cacheError = clientCache.updateTableCards(0, 0, 0, 0, currentDeck.GetNextCard());
          if (cacheError != CacheError.noError)
            throw new Exception("Cache error adding river card.");
          break;
      }
      #endregion tableCards
    }

    /// <summary>
    /// Part of play game.
    /// CURRENTLY UNUSED!!!
    /// </summary>
    protected void awardPot()
    {
      #region awardPot

      List<playerHandValue> playerHandValues = new List<playerHandValue>();
      //Last thing is to implement at sidepots and returned bets

      //For each player that is all in, work out how much they put in the pot
      //Determine how many individuals were in the pot when that player last bet
      //The sidepot for that player is their money * number players

      //Rank all the remaning players hands in order to correctly dish out the pot
      //We are going to ignore side pots for now ;)
      activePositions = clientCache.getActivePositions();
      currentHandDetails = clientCache.getCurrentHandDetails();

      //Determine the amounts in pot from everyone in order to calculate maximum winable amount
      byte[] satInPositions = clientCache.getSatInPositions();
      decimal[] allBetAmounts = new decimal[satInPositions.Length];
      for (int i = 0; i < allBetAmounts.Length; i++)
        allBetAmounts[i] = clientCache.getTotalPlayerMoneyInPot(clientCache.getPlayerId(satInPositions[i]));

      //Determine each positions HandValueObject
      for (int i = 0; i < satInPositions.Length; i++)
      {
        string playerName = clientCache.getPlayerName(clientCache.getPlayerId(satInPositions[i]));
        long playerId = clientCache.getPlayerId(satInPositions[i]);
        decimal playerMoneyInPot = clientCache.getTotalPlayerMoneyInPot(clientCache.getPlayerId(satInPositions[i]));
        decimal maxWinAmount = 0;
        int playerHandValue = 0;
        bool hasFolded = true;

        if (activePositions.Contains(satInPositions[i]))
        {
          hasFolded = false;

          //If we are in a GPAJob we get handRank in a slightly different way here
          //if (GPAJob == null)
          playerHandValue = HandRank.GetHandRank((Card)clientCache.getPlayerHoleCards(playerId).holeCard1, (Card)clientCache.getPlayerHoleCards(playerId).holeCard2, (Card)currentHandDetails.tableCard1, (Card)currentHandDetails.tableCard2, (Card)currentHandDetails.tableCard3, (Card)currentHandDetails.tableCard4, (Card)currentHandDetails.tableCard5);
          //else
          //    playerHandValue = GPAJob.HoleCardValues[numCompletedHands][satInPositions[i]];

          //Work out the maximum this player could win
          for (int j = 0; j < allBetAmounts.Length; j++)
          {
            decimal result = allBetAmounts[j] - playerMoneyInPot;
            if (result <= 0)
              maxWinAmount += allBetAmounts[j];
            else
              maxWinAmount += playerMoneyInPot;
          }
        }

        //Add this information to playerHandValues
        playerHandValues.Add(new playerHandValue(playerHandValue, playerId, playerName, playerMoneyInPot, maxWinAmount, hasFolded));
      }

      //Sort descending by hand value
      //If two hand values are identical then sort descending by moneyInPot
      playerHandValues.Sort();

      #region return Bets
      //We need to make sure that two people share the most amount put in the put, any difference is returned
      //We need to compare with people who have folded incase they folded after everyone else went all in
      var topTwoAmountsInPot =
          (from players in playerHandValues
           orderby players.playerMoneyInPot descending
           select players).Take(2).ToArray();

      if (topTwoAmountsInPot.Count() > 1)
      {
        if (topTwoAmountsInPot[0].playerMoneyInPot != topTwoAmountsInPot[1].playerMoneyInPot)
        {
          decimal returnBet = topTwoAmountsInPot[0].playerMoneyInPot - topTwoAmountsInPot[1].playerMoneyInPot;
          cacheError = clientCache.newHandAction(topTwoAmountsInPot[0].playerId, PokerAction.ReturnBet, returnBet);
          if (cacheError != CacheError.noError)
            throw new Exception("Cache error while sitting out player.");

          //Reduce the maximum amount that player can win
          //We can't use topTwoAmountsInPot as this has now been disconnected from the original list
          var returnBetPlayer =
              (from players in playerHandValues
               where players.playerId == topTwoAmountsInPot[0].playerId
               select players);

          if (returnBetPlayer.Count() != 1)
            throw new Exception("Why are there more than one players in this list????");

          returnBetPlayer.First().maximumWinableAmount -= returnBet;
        }
      }
      #endregion

      //Now we have sorted return bet we can finish awarding the pot.
      decimal runningPotAmount = clientCache.getCurrentHandDetails().potValue;
      decimal runningAwardedAmount = 0;

      //We now need to make sure two non folded players share the same maximum winable amount
      //If they don't instead of being a returned bet now it is a win pot
      #region match maximum winable amounts
      var topTwoWinableAmounts =
          (from players in playerHandValues
           where players.hasFolded == false
           orderby players.maximumWinableAmount descending
           select players).Take(2).ToArray();

      if (topTwoWinableAmounts.Count() > 1)
      {
        if (topTwoWinableAmounts[0].maximumWinableAmount != topTwoWinableAmounts[1].maximumWinableAmount)
        {
          decimal winPotAmount = topTwoWinableAmounts[0].maximumWinableAmount - topTwoWinableAmounts[1].maximumWinableAmount;
          cacheError = clientCache.newHandAction(topTwoWinableAmounts[0].playerId, PokerAction.WinPot, winPotAmount);
          if (cacheError != CacheError.noError)
            throw new Exception("Cache error while awarding pot.");

          //Reduce the maximum amount that player can win
          //We can't use topTwoAmountsInPot as this has now been disconnected from the original list
          var winDifferencePlayer =
              (from players in playerHandValues
               where players.playerId == topTwoWinableAmounts[0].playerId
               select players);

          if (winDifferencePlayer.Count() != 1)
            throw new Exception("Why are there more than one players in this list????");

          winDifferencePlayer.First().maximumWinableAmount -= winPotAmount;
          runningPotAmount -= winPotAmount;
          //runningAwardedAmount += winPotAmount;
        }
      }
      #endregion

      //Now continue with the normal award pot section
      #region award pot based on hand value
      int playerIndex = 0;
      int loopSafety = 0;
      while (playerIndex < playerHandValues.Count && runningPotAmount > 0)
      {
        if (loopSafety > 100)
          throw new Exception("Loop safety triggered in award pot. Possible localIndex duplication bug!");

        var winners =
            (from player in playerHandValues
             where player.handValue == playerHandValues[playerIndex].handValue && player.maximumWinableAmount > 0
             orderby player.maximumWinableAmount ascending
             select player).ToArray();

        int winnersLeftToPay = winners.Count();

        for (int i = 0; i < winners.Count(); i++)
        {
          if (runningPotAmount == 0)
            throw new Exception("If a group of people are matched for cards everyone should win something.");

          //we deal out the pot starting with the person who has the least in the pot
          decimal winAmount = (winners[i].maximumWinableAmount - runningAwardedAmount) / winnersLeftToPay;

          //We need to round the winAmount down to the nearest pence
          winAmount = Math.Round(winAmount, 2, MidpointRounding.AwayFromZero);

          if (winAmount > runningPotAmount)
            winAmount = runningPotAmount;

          //If the winAmount is negative this player cannot win anything!
          if (winAmount > 0)
          {
            cacheError = clientCache.newHandAction(winners[i].playerId, PokerAction.WinPot, winAmount);
            //Debug.Print("Awarded {0} to {1} (id-{2})", winAmount, clientCache.getPlayerName(winners[i].playerId), winners[i].playerId);

            if (cacheError != CacheError.noError)
              throw new Exception("Cache error while awarding pot.");

            runningAwardedAmount += winAmount;
            runningPotAmount -= winAmount;
          }

          winnersLeftToPay--;
        }

        playerIndex += winners.Length;
        loopSafety++;
      }
      #endregion

      //At this point the entire point must have been awarded
      //We want to break here instead of cause a cache error so that we can find out what the problem was!!
      decimal totalAwardedAmounts =
          (from wins in clientCache.getAllHandActions()
           where wins.handId == clientCache.getCurrentHandId() && wins.actionType == PokerAction.WinPot
           select wins.actionValue).Sum();

      if (totalAwardedAmounts != clientCache.getCurrentHandDetails().potValue)
        throw new Exception("The full pot amount has not been awarded.");

      #endregion awardPot
    }

    protected virtual void SitOutInPlayers()
    {
      //////////////////////////////////////////////////
      // Sit out any players which now have a 0 stack //
      //////////////////////////////////////////////////
      activePositions = clientCache.getSatInPositions();
      for (int i = 0; i < activePositions.Length; i++)
      {
        decimal playerStack = clientCache.getPlayerStack(clientCache.getPlayerId(activePositions[i]));

        //Standup players whose stack is:
        //1.Less than a big blind
        //2.If autoRemovePlayersWithLargeStackInBB > 1, then if their stack is larger than this multiple of blinds
        if ((maxHandsBeforeTableReset > 1 && handCountSinceReset >= maxHandsBeforeTableReset) || playerStack < clientCache.BigBlind || (autoRemovePlayersWithLargeStackInBB > 1 && playerStack > (decimal)autoRemovePlayersWithLargeStackInBB * clientCache.BigBlind))
        {
          //Sit the player out
          cacheError = clientCache.newHandAction(clientCache.getPlayerId(activePositions[i]), PokerAction.SitOut, 0);
          if (cacheError != CacheError.noError)
            throw new Exception("Cache error while sitting out player.");

          cacheError = clientCache.newHandAction(clientCache.getPlayerId(activePositions[i]), PokerAction.LeaveTable, 0);
          if (cacheError != CacheError.noError)
            throw new Exception("Cache error while sitting out player.");

          //Stand the player up.
          clientCache.removeTablePlayer(clientCache.getPlayerId(activePositions[i]));
        }
      }
    }

    /// <summary>
    /// Part of play game.
    /// </summary>
    protected virtual void finishHand()
    {
      //If we want to find something going wrong we can add a logging step here
#if logging
            using (System.IO.StreamWriter sw = new System.IO.StreamWriter("pokerTableLogging.csv", true))
                sw.WriteLine(clientCache.getCurrentHandId() + ", " + clientCache.getMostRecentLocalIndex()[1] + ", " + clientCache.CurrentHandHash(true) + "," + clientCache.CurrentHandHandActionsCount() + "," + clientCache.getCurrentHandDetails().potValue);
#endif

      #region cleanupHand

      //if (GPAJob != null) GPAJob.NumHandsCompleted++;

      numCompletedHands++;
      handCountSinceReset++;

      if (maxHandsBeforeTableReset > -1 && handCountSinceReset >= maxHandsBeforeTableReset)
        handCountSinceReset = 0;

      //Increment the dealer index for the next round but not for the very first hand
      dealerIndex = clientCache.getNextSatInPlayerPosition(dealerIndex);

      //End the game if we now have less than our minimum number players.
      if ((maxHandsToPlay > 0 && numCompletedHands >= maxHandsToPlay) || (clientCache.getSatInPositions().Length < minNumTablePlayers))
        endGame = true;

      //Add stack cash for the genetic player
      //if (GPAJob != null)
      //{
      //    decimal currentGeneticStack =clientCache.getPlayerStack(GPAJob.JobPlayerIds[0]);
      //    if (currentGeneticStack < 8.0m)
      //    {
      //        cacheError = clientCache.newHandAction(GPAJob.JobPlayerIds[0], PokerAction.AddStackCash, 10.0m - currentGeneticStack);
      //        if (cacheError != CacheError.noError) throw new Exception("Error!");
      //    }
      //}

      //End the hand
      cacheError = clientCache.endCurrentHand();
      if (cacheError != CacheError.noError)
        throw new Exception("Cache error while ending hand.");

      #endregion cleanupHand
    }
  }
}
