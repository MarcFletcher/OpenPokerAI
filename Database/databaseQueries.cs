using System;
using System.Collections.Generic;
using System.Linq;
using PokerBot.Definitions;

namespace PokerBot.Database
{
  public static class databaseQueries
  {

    static bool databaseOffline = false;
    static string manualPlayersTableFileLocation = "";

    public static string ManualPlayersTableFileLocation
    {
      get { return manualPlayersTableFileLocation; }
    }

    /// <summary>
    /// Allows us to the use the cache completely independantly from the database.
    /// </summary>
    /// <param name="ManualPlayersTableFileLocation"></param>
    public static void SetDatabaseOffline(string ManualPlayersTableFileLocation)
    {
      databaseOffline = true;
      manualPlayersTableFileLocation = ManualPlayersTableFileLocation;
    }

    public static void LoadManualPlayersTableFromDisk()
    {
      if (databaseOffline && databaseCache.databaseRAM != null)
        databaseCache.databaseRAM.LoadManualPlayersTable(manualPlayersTableFileLocation);
    }

    public static void SaveManualPlayersTableToDisk()
    {
      if (databaseOffline && databaseCache.databaseRAM != null)
        databaseCache.databaseRAM.SaveOutManualPlayersTable(manualPlayersTableFileLocation);
    }

    /// <summary>
    /// A testing query.
    /// </summary>
    public static void TestQuery()
    {
      //if (databaseCache.databaseRAM.UseRAMOnly)
      //    databaseCache.databaseRAM.
    }

    public static long[] ClientOpponentPlayerIds(short pokerClientId, List<int> excludeAIGenerations, int currentTrainingGenerationType)
    {
      if (databaseOffline && databaseCache.databaseRAM != null)
        return databaseCache.databaseRAM.ClientOpponentPlayerIds((PokerClients)pokerClientId, excludeAIGenerations, currentTrainingGenerationType);
      else
      {
        throw new NotImplementedException();
      }
    }

    /// <summary>
    /// Returns the player agression metric from the database
    /// </summary>
    /// <param name="playerId"></param>
    /// <param name="handsRange">The maximum number of hands to count.</param>
    /// <param name="handsStartIndex">-1 means from most recent backwards.</param>
    /// <param name="startWeight">The weight at which the scaling starts. 1 means no scaling.</param>
    /// <param name="numHandsCounted">Returns the number of hands counted which may be less than handsRange</param>
    /// <param name="RFreq_PreFlop"></param>
    /// <param name="RFreq_PostFlop"></param>
    /// <param name="CFreq_PreFlop"></param>
    /// <param name="CFreq_PostFlop"></param>
    /// <param name="PreFlopPlayFreq"></param>
    public static void PlayerAgressionMetrics(long playerId, long handsRange, long handsStartIndex, decimal startWeight, ref int numHandsCounted,
        ref decimal RFreq_PreFlop, ref decimal RFreq_PostFlop,
        ref decimal CFreq_PreFlop, ref decimal CFreq_PostFlop,
        ref decimal CheckFreq_PreFlop, ref decimal CheckFreq_PostFlop,
        ref decimal PreFlopPlayFreq, ref decimal PostFlopPlayFreq)
    {
      if (databaseCache.databaseRAM != null && databaseCache.databaseRAM.UseRAMOnly)
      {
        databaseCache.databaseRAM.csp_PlayerAgressionMetrics(playerId, handsRange, handsStartIndex, startWeight, ref numHandsCounted, ref RFreq_PreFlop,
            ref RFreq_PostFlop, ref CFreq_PreFlop, ref CFreq_PostFlop, ref CheckFreq_PreFlop, ref CheckFreq_PostFlop, ref PreFlopPlayFreq, ref PostFlopPlayFreq);
      }
      else
      {
        throw new NotImplementedException();
      }

      //Value check
      if (RFreq_PreFlop == null || //decimal.IsNaN(RFreq_PreFlop) ||
      RFreq_PostFlop == null || //decimal.IsNaN(RFreq_PostFlop) ||
      CFreq_PreFlop == null || //decimal.IsNaN(CFreq_PreFlop) ||
      CFreq_PostFlop == null || //decimal.IsNaN(CFreq_PostFlop) ||
      CheckFreq_PreFlop == null || //decimal.IsNaN(CheckFreq_PreFlop) ||
      CheckFreq_PostFlop == null || //decimal.IsNaN(CheckFreq_PostFlop) ||
      numHandsCounted == null || //decimal.IsNaN(numHandsCounted) ||
      PreFlopPlayFreq == null || //decimal.IsNaN(PreFlopPlayFreq) ||
      PostFlopPlayFreq == null) //|| decimal.IsNaN(PostFlopPlayFreq))
        throw new Exception("Aggression provider tried to return NaN or null for playerId " + playerId + ". " +
           numHandsCounted + ", " + PreFlopPlayFreq + ", " + CheckFreq_PreFlop + ", " + CFreq_PreFlop + ", " + RFreq_PreFlop +
           ", " + PostFlopPlayFreq + ", " + CheckFreq_PostFlop + ", " + CFreq_PostFlop + ", " + RFreq_PostFlop);
    }

    /// <summary>
    /// Returns the number of hands played by the provided playerId
    /// </summary>
    /// <param name="playerId"></param>
    /// <param name="countUniqueHandsOnly">If this is true hands are calculated differently and will potentially take much longer.</param>
    /// <returns></returns>
    public static int NumHandsPlayed(long playerId, bool countUniqueHandsOnly)
    {
      int? handsPlayed = 0;

      if (databaseCache.databaseRAM != null && databaseCache.databaseRAM.UseRAMOnly)
        handsPlayed = databaseCache.databaseRAM.csp_NumHandsPlayed(playerId, countUniqueHandsOnly);
      else
      {
        throw new NotImplementedException();
      }
      if (handsPlayed == null)
        throw new Exception("Handsplayed returned null.");

      return (int)handsPlayed;
    }

    /// <summary>
    /// Returns the number of hands played by the provided clientId
    /// </summary>
    /// <param name="pokerClientId"></param>
    /// <returns></returns>
    public static int NumHandsPlayed(short pokerClientId)
    {
      long? handsPlayed = 0;

      if (databaseCache.databaseRAM != null && databaseCache.databaseRAM.UseRAMOnly)
        handsPlayed = databaseCache.databaseRAM.csp_NumHandsPlayed(pokerClientId);
      else
      {
        throw new NotImplementedException();
      }

      if (handsPlayed == null)
        throw new Exception("Handsplayed returned null.");

      return (int)handsPlayed;
    }

    /// <summary>
    /// Returns the handIds of the hands played by playerId
    /// </summary>
    /// <param name="playerId"></param>
    /// <returns></returns>
    public static long[] HandIdsPlayedByPlayerId(long playerId)
    {
      if (databaseCache.databaseRAM != null && databaseCache.databaseRAM.UseRAMOnly)
        return databaseCache.databaseRAM.csp_HandIdsPlayedByPlayerId(playerId);
      else
      {
        throw new NotImplementedException();
      }
    }

    public static string GenerateNewPlayerName(string startingName, short pokerClientId, bool obfuscate)
    {
      string newPlayerName = "";

      if (databaseOffline)
      {
        List<string> obfuscatedNames = new List<string>() { "Bob", "Alice", "Charlene", "Eve", "Tak", "Marc", "Matt", "Ailwyn", "Edmund", "Lucifer", "Denis", "Kent", "Clark", "Bruce", "Peter", "Nike", "Steve", "Rio", "Tony", "Stan" };
        List<string> characters = new List<string>() { "a", "b", "c", "d", "e", "f", "g", "h", "i", "j", "k", "l", "m", "n", "o", "p", "q", "r", "s", "t", "u", "v", "w", "x", "y", "z", "A", "B", "C", "D", "E", "F", "G", "H", "I", "J", "K", "L", "M", "N", "O", "P", "Q", "R", "S", "T", "U", "V", "W", "X", "Y", "Z", "0", "1", "2", "3", "4", "5", "6", "7", "8", "9" };

        //Check to see if we have created a player with this name before
        do
        {
          string tryName;
          if (obfuscate)
            tryName = ShuffleList.Shuffle(obfuscatedNames)[0] + "-" + String.Join("", ShuffleList.Shuffle(characters).Take(3).ToArray());
          else
            tryName = startingName + "-" + String.Join("", ShuffleList.Shuffle(characters).Take(3).ToArray());

          //Unique name check
          PokerPlayer playerDetails = playerDetailsByPlayerName(tryName, pokerClientId);
          if (playerDetails == null)
          {
            newPlayerName = tryName;
            break;
          }

        } while (true);
      }
      else
      {
        throw new NotImplementedException();
      }

      return newPlayerName;
    }

    /// <summary>
    /// Returns the aiType and configId for a given playerId. 
    /// If a matching config is not found returns 0 - NoAi_Human and blank config string.
    /// </summary>
    /// <param name="playerId"></param>
    /// <param name="aiType"></param>
    /// <param name="aiConfigId"></param>
    public static void aiPlayerConfig(long playerId, out int aiType, out string aiConfigStr)
    {
      if (databaseOffline && databaseCache.databaseRAM != null)
        databaseCache.databaseRAM.AIPlayerConfig(playerId, out aiType, out aiConfigStr);
      else
      {
        throw new NotImplementedException();
      }
    }

    public static void DeleteOldPlayerHands(long[] playerIds, int numberHandsToDelete)
    {
      if (databaseCache.databaseRAM.UseRAMOnly)
        databaseCache.databaseRAM.csp_DeleteOldPlayerHands(playerIds, numberHandsToDelete);
      else
      {
        throw new NotImplementedException();
      }
    }

    public static void DeleteDatabasePokerHand(long handId)
    {
      if (databaseCache.databaseRAM.UseRAMOnly)
        throw new NotImplementedException();
      else
      {
        throw new NotImplementedException();
      }
    }

    /// <summary>
    /// Returns the total amount won by a player Id, defined as summing all 'Win Pot' actions.
    /// To work out total amount won or lost you need to minus 'amountsGambledByPlayerId'.
    /// StartingIndex can be used to do incremental counts, default is 1. Hands counted start at the startingIndex. MaxHands is the max number of hands to be counted. i.e. start at handIndex 2 and count two hands (hands counted are 2 and 3).
    /// Set maxHands = -1 for all available hands.
    /// </summary>
    /// <param name="playerId"></param>
    /// <returns></returns>
    public static decimal amountsWonByPlayerId(long playerId, int startingIndex, int maxHands)
    {
      if (databaseCache.databaseRAM != null && databaseCache.databaseRAM.UseRAMOnly)
        throw new Exception("Use amountsGambledWonByPlayerId() for RAMDatabase");

      decimal? amountsWon = 0;

      if (databaseCache.databaseRAM != null && databaseCache.databaseRAM.UseRAMOnly)
        databaseCache.databaseRAM.csp_amountsWonByPlayerId(playerId, startingIndex, maxHands);
      else
      {
        throw new NotImplementedException();
      }

      if (amountsWon == null)
        throw new Exception("Amounts won returned null.");

      return (decimal)amountsWon;
    }

    /// <summary>
    /// Returns the amounts a player has gambled, i.e. during playing poker hands.
    /// To work out total amount won or lost use this method in conjunction with 'amountsWonByPlayerId'.
    /// StartingIndex can be used to do incremental counts, default is 1. Hands counted start at the startingIndex. MaxHands is the max number of hands to be counted. i.e. start at handIndex 2 and count two hands (hands counted are 2 and 3).
    /// Set maxHands = - 1 for all available hands.
    /// </summary>
    /// <param name="playerId"></param>
    /// <returns></returns>
    public static decimal amountsGambledByPlayerId(long playerId, int startingIndex, int maxHands)
    {
      if (databaseCache.databaseRAM != null && databaseCache.databaseRAM.UseRAMOnly)
        throw new Exception("Use amountsGambledWonByPlayerId() for RAMDatabase");

      decimal? amountsGambled = 0;

      if (databaseCache.databaseRAM != null && databaseCache.databaseRAM.UseRAMOnly)
        databaseCache.databaseRAM.csp_amountsGambledByPlayerId(playerId, startingIndex, maxHands);
      else
      {
        throw new NotImplementedException();
      }

      if (amountsGambled == null)
        throw new Exception("Amounts gambled returned null");

      return (decimal)amountsGambled;
    }

    /// <summary>
    /// Returns the amounts a player has gambled and won as references
    /// StartingIndex can be used to do incremental counts, default is 1. Hands counted start at the startingIndex. MaxHands is the max number of hands to be counted. i.e. start at handIndex 2 and count two hands (hands counted are 2 and 3).
    /// Set maxHands = - 1 for all available hands.
    /// </summary>
    /// <param name="playerId"></param>
    /// <param name="startingIndex"></param>
    /// <param name="maxHands"></param>
    /// <param name="amountsWon"></param>
    /// <param name="amountsGambled"></param>
    public static void amountsGambledWonByPlayerId(long playerId, int startingIndex, int maxHands, ref decimal amountsWon, ref decimal amountsGambled)
    {
      if (databaseCache.databaseRAM.UseRAMOnly)
      {
        //Make sure they are starting at 0.
        amountsWon = 0;
        amountsGambled = 0;
        databaseCache.databaseRAM.csp_amountsGambledWonByPlayerId(playerId, startingIndex, maxHands, ref amountsWon, ref amountsGambled);
      }
      else
      {
        amountsWon = amountsWonByPlayerId(playerId, startingIndex, maxHands);
        amountsGambled = amountsGambledByPlayerId(playerId, startingIndex, maxHands);
      }
    }

    /// <summary>
    /// Deletes database data for the provided pokerClientId
    /// </summary>
    /// <param name="pokerClientId"></param>
    /// <param name="deletePlayers"></param>
    public static void deleteDatabaseData(short pokerClientId, bool deletePlayers)
    {
      if (databaseOffline && databaseCache.databaseRAM != null && deletePlayers)
      {
        //All we really need to delete at this point is the players
        databaseCache.databaseRAM.DeleteAllClientPlayersFromManualPlayersTable((PokerClients)pokerClientId);
      }
      else
      {
        throw new NotImplementedException();
      }
    }

    public static void DeleteLegacyTrainingPlayers(short pokerClientId, int generationNumToKeep)
    {
      if (databaseOffline && databaseCache.databaseRAM != null)
        databaseCache.databaseRAM.DeleteLegacyTrainingPlayersFromManualPlayersTable((PokerClients)pokerClientId, generationNumToKeep);
      else
      {
        throw new NotImplementedException();
      }
    }

    public class PlayerAverageAdditionalRaiseAmountResultScaled
    {
      private long _handId;

      private short _seqIndex;

      private byte _gameStage;

      private decimal _scaledRaiseAmount;

      public PlayerAverageAdditionalRaiseAmountResultScaled()
      {
      }

      public PlayerAverageAdditionalRaiseAmountResultScaled(long handId, short seqIndex, byte gameStage, double scaledRaiseAmount)
      {
        this._handId = handId;
        this._seqIndex = seqIndex;
        this._gameStage = gameStage;
        this._scaledRaiseAmount = (decimal)scaledRaiseAmount;
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

      public decimal scaledRaiseAmount
      {
        get
        {
          return this._scaledRaiseAmount;
        }

        set
        {
          if ((this._scaledRaiseAmount != value))
          {
            this._scaledRaiseAmount = value;
          }
        }
      }
    }

    public static PlayerAverageAdditionalRaiseAmountResultScaled[] PlayerAdditionalRaiseAmountsScaled(long playerId)
    {
      if (databaseCache.databaseRAM != null && databaseCache.databaseRAM.UseRAMOnly)
        return databaseCache.databaseRAM.csp_playerAdditionalRaiseAmounts(playerId);
      else
        throw new NotImplementedException("The scaled version of this query is not implemented for the SQL database");
    }

    public static PlayerAverageAdditionalRaiseAmountResult[] PlayerAdditionalRaiseAmountsRaw(long playerId)
    {
      if (databaseCache.databaseRAM != null && databaseCache.databaseRAM.UseRAMOnly)
        return databaseCache.databaseRAM.csp_playerAdditionalRaiseAmountsRaw(playerId);
      else
      {
        throw new NotImplementedException();
      }
    }

    /// <summary>
    /// Returns a players additional raise amounts in a binned format between 0 and 1 using the provided binWidth. Anything greater than 1 will be added to an additional bin at the end.
    /// </summary>
    /// <param name="playerId"></param>
    /// <param name="binWidth"></param>
    /// <returns></returns>
    public static void PlayerAdditionalRaiseAmountsBinned(long playerId, decimal binWidth, out int[] preFlopBinsSmall, out int[] flopBinsSmall, out int[] turnBinsSmall, out int[] riverBinsSmall,
        out int[] preFlopBinsBig, out int[] flopBinsBig, out int[] turnBinsBig, out int[] riverBinsBig)
    {
      PlayerAverageAdditionalRaiseAmountResult[] playerData = PlayerAdditionalRaiseAmountsRaw(playerId);

      //Setup bins
      int numBins = (int)(1.0m / binWidth) + 1;
      preFlopBinsSmall = new int[numBins];
      flopBinsSmall = new int[numBins];
      turnBinsSmall = new int[numBins];
      riverBinsSmall = new int[numBins];

      preFlopBinsBig = new int[numBins];
      flopBinsBig = new int[numBins];
      turnBinsBig = new int[numBins];
      riverBinsBig = new int[numBins];

      //Fill bins
      for (int i = 0; i < playerData.Length; i++)
      {
        //Calculate the info we need to know about the scaled amount
        double scaledRaiseAmount = RaiseAmountsHelper.ScaleAdditionalRaiseAmount(playerData[i].currentPotAmount, playerData[i].bigBlind, playerData[i].minAdditionalRaiseAmount, playerData[i].maxAdditionalRaiseAmount, playerData[i].additionalRaiseAmountToScale);
        bool smallPot = playerData[i].currentPotAmount <= RaiseAmountsHelper.SmallPotBBMultiplierLimit * playerData[i].bigBlind;
        int binIndex = (int)(scaledRaiseAmount / (double)binWidth);

        //Anything larger than the last bin just gets put there
        if (binIndex > numBins - 1)
          binIndex = numBins - 1;

        if (smallPot)
        {
          if (playerData[i].gameStage == 0)
            preFlopBinsSmall[binIndex]++;
          else if (playerData[i].gameStage == 1)
            flopBinsSmall[binIndex]++;
          else if (playerData[i].gameStage == 2)
            turnBinsSmall[binIndex]++;
          else if (playerData[i].gameStage == 3)
            riverBinsSmall[binIndex]++;
          else
            throw new Exception("Impossible gameStage provided.");
        }
        else
        {
          if (playerData[i].gameStage == 0)
            preFlopBinsBig[binIndex]++;
          else if (playerData[i].gameStage == 1)
            flopBinsBig[binIndex]++;
          else if (playerData[i].gameStage == 2)
            turnBinsBig[binIndex]++;
          else if (playerData[i].gameStage == 3)
            riverBinsBig[binIndex]++;
          else
            throw new Exception("Impossible gameStage provided.");
        }
      }
    }

    /// <summary>
    /// Returns the playerActionPrediction data from the database.
    /// </summary>
    /// <param name="playerId"></param>
    /// <returns></returns>
    public static NNPlayerModellingDataResult[] playerActionPredictionData(long playerId, int maxActions, long startingHandId)
    {
      throw new NotImplementedException();
    }

    public static long[] CreateNewBotPlayers(short pokerClientId, int aiType, string[] playerNames, string[] aiConfigStrs)
    {
      if (databaseOffline && databaseCache.databaseRAM != null)
        return databaseCache.databaseRAM.AddPlayersToManualPlayersTable((PokerClients)pokerClientId, aiType, playerNames, aiConfigStrs);
      else
        throw new NotImplementedException("CreateNewBotPlayers only implemented for offline RAM database.");
    }

    /// <summary>
    /// Creates a new bot player in the database
    /// </summary>
    /// <param name="playerName"></param>
    /// <param name="pokerClientId"></param>
    /// <param name="aiType"></param>
    /// <param name="aiConfigStr"></param>
    /// <returns>The new player name</returns>
    public static long CreateNewBotPlayer(string playerName, short pokerClientId, int aiType, string aiConfigStr)
    {
      long newPlayerId;

      if (databaseOffline && databaseCache.databaseRAM != null)
        newPlayerId = databaseCache.databaseRAM.AddPlayerToManualPlayersTable(playerName, (PokerClients)pokerClientId, aiType, aiConfigStr);
      else
      {
        throw new NotImplementedException();
      }

      return newPlayerId;
    }

    /// <summary>
    /// Creates a new non bot player
    /// </summary>
    /// <param name="playerName"></param>
    /// <param name="pokerClientId"></param>
    /// <returns>The new playerId</returns>
    public static long CreateNewNonBotPlayer(string playerName, short pokerClientId)
    {
      if (databaseCache.databaseRAM.UseRAMOnly && playerName != "")
        throw new NotImplementedException("Can not yet create new 'NON BLANK' players when using RAM database.");

      throw new NotImplementedException();
    }

    /// <summary>
    /// Returns the playerId associated with the aiConfigStr
    /// </summary>
    /// <param name="aiConfigStr"></param>
    /// <returns></returns>
    public static long[] PlayerIdsFromConfigStr(string[] aiConfigStr, short pokerClientId)
    {
      long[] playerIds = new long[aiConfigStr.Length];

      if (databaseOffline && databaseCache.databaseRAM != null)
        playerIds = databaseCache.databaseRAM.PlayerIdsFromConfigStr(aiConfigStr, (PokerClients)pokerClientId);
      else
      {
        throw new NotImplementedException();
      }

      return (from ids in playerIds orderby ids ascending select ids).ToArray();
    }

    /// <summary>
    /// Returns the playerId associated with the aiConfigStr
    /// </summary>
    /// <param name="aiConfigStr"></param>
    /// <returns></returns>
    public static long[] getPlayerIdsFromAiType(AIGeneration aiType, short pokerClientId)
    {
      long[] playerIds;

      if (databaseOffline && databaseCache.databaseRAM != null)
        playerIds = databaseCache.databaseRAM.PlayerIdsFromAiType(aiType, (PokerClients)pokerClientId);
      else
      {
        throw new NotImplementedException();
      }

      return (from ids in playerIds orderby ids ascending select ids).ToArray();
    }

    public static int getNumPlayerHandActions(long playerId, bool postFlopOnly, int maxActions, ref long startId)
    {
      throw new NotImplementedException();
    }

    public static string convertToPlayerNameFromId(long playerId)
    {
      return playerDetailsByPlayerId(playerId).PlayerName;
    }

    /// <summary>
    /// Returns the player details from the database. Returns null if the player is not located.
    /// </summary>
    /// <param name="playerName"></param>
    /// <param name="pokerClientId"></param>
    /// <returns></returns>
    public static PokerPlayer playerDetailsByPlayerName(string playerName, short pokerClientId)
    {
      if (databaseOffline && databaseCache.databaseRAM != null)
        return databaseCache.databaseRAM.PlayerDetails(playerName, (PokerClients)pokerClientId);
      else
      {
        throw new NotImplementedException();
      }
    }

    /// <summary>
    /// Returns the player details from the database
    /// </summary>
    /// <param name="playerId"></param>
    /// <returns></returns>
    public static PokerPlayer playerDetailsByPlayerId(long playerId)
    {
      if (databaseOffline && databaseCache.databaseRAM != null)
        return databaseCache.databaseRAM.PlayerDetails(playerId);
      else
      {
        throw new NotImplementedException();
      }
    }

    /// <summary>
    /// Save out any known data in typical hand history format
    /// </summary>
    /// <param name="tableId"></param>
    /// <param name="fileName"></param>
    public static void SaveoutHandHistory(long tableId, string fileName)
    {
      if (databaseCache.databaseRAM != null)
        databaseCache.databaseRAM.csp_SaveOutRamDataHandHistory(tableId, fileName);
      else
        throw new NotImplementedException("Method only implemented for RAMDatabase");
    }

    /// <summary>
    /// Returns an array which encapsulates a players hole card usage.
    /// Card 1 is always larger than card 2.
    /// Suited cards are all changed to clubs.
    /// Unsuited cards are clubs and diamods.
    /// </summary>
    /// <param name="playerId"></param>
    /// <returns></returns>
    public static CardUsage[] PlayerPreFlopCardUsage(long playerId)
    {
      if (!databaseCache.databaseRAM.UseRAMOnly)
        throw new NotImplementedException("PlayerCardUsage only implemented in RAM database.");

      List<CardUsage> result = new List<CardUsage>();
      var resultDict = databaseCache.databaseRAM.csp_PlayerPreflopCardUsage(playerId);

      foreach (KeyValuePair<Card, Dictionary<Card, CardUsage>> index1 in resultDict)
        result.AddRange(index1.Value.Values);

      var returnResult = (from current in result orderby current.handValue descending, current.card1 descending select current).ToArray();
      return returnResult;
    }
  }
}
