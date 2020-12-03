using System;
using System.Collections.Generic;
using System.Linq;
using System.IO;
using PokerBot.Definitions;
using System.Threading;

namespace PokerBot.Database
{
  partial class RAMDatabase
  {
    volatile bool PlayerChangesSinceLoad = true;
    protected object playerLocker = new object();

    #region AdvancedLocking
    int readLockCounter = 0;
    int writeLockCounter = 0;
    Thread currentPlayerWriteThread;

    protected void startPlayerRead()
    {
      Random rand = new Random();
      int loopCount = 1;

      do
      {
        while (currentPlayerWriteThread != Thread.CurrentThread && currentPlayerWriteThread != null)
        {
          Thread.Sleep((int)(50.0 * rand.NextDouble() * loopCount));
          loopCount++;
        }

        lock (playerLocker)
        {
          if (currentPlayerWriteThread == null || currentPlayerWriteThread == Thread.CurrentThread)
          {
            readLockCounter++;
            return;
          }
        }

        loopCount = loopCount / 2;

      } while (currentPlayerWriteThread != null && currentPlayerWriteThread != Thread.CurrentThread);
    }
    protected void endPlayerRead()
    {
      lock (playerLocker)
      {
        if (readLockCounter < 1)
          throw new Exception("ReadLockCounter is less than 1.");

        readLockCounter--;
      }
    }

    protected void startPlayerWrite()
    {
      Random rand = new Random();
      int loopCount = 1;

      do
      {
        while ((currentPlayerWriteThread != Thread.CurrentThread && currentPlayerWriteThread != null) || readLockCounter > 0)
        {
          Thread.Sleep((int)(25.0 * rand.NextDouble() * loopCount));
          loopCount++;
        }

        lock (playerLocker)
        {
          if ((currentPlayerWriteThread == null || currentPlayerWriteThread == Thread.CurrentThread) && readLockCounter == 0)
          {
            currentPlayerWriteThread = Thread.CurrentThread;
            writeLockCounter++;
          }
        }

        loopCount = loopCount / 2;

      } while (currentPlayerWriteThread != Thread.CurrentThread);
    }
    protected void endPlayerWrite()
    {
      lock (playerLocker)
      {
        //Release the write lock
        writeLockCounter--;

        if (writeLockCounter == 0)
          currentPlayerWriteThread = null;
      }
    }
    #endregion

    public void LoadManualPlayersTable(string fileLocation)
    {
      startPlayerWrite();

      //lock (playerLocker)
      //{
      if (File.Exists(fileLocation))
      {
        pokerPlayers = new Dictionary<PokerClients, Dictionary<long, PokerPlayer>>();
        string[] fileLines = File.ReadAllLines(fileLocation);

        for (int i = 1; i < fileLines.Length; i++)
        {
          string[] lineElements = fileLines[i].Split(',');

          long playerId = long.Parse(lineElements[0]);
          PokerClients playerClient = (PokerClients)short.Parse(lineElements[2]);
          var player = new PokerPlayer(playerId, lineElements[1], (short)playerClient, (AIGeneration)Enum.Parse(typeof(AIGeneration), lineElements[3]), lineElements[4]);

          if (!pokerPlayers.ContainsKey(playerClient))
            pokerPlayers.Add(playerClient, new Dictionary<long, PokerPlayer>());

          pokerPlayers[playerClient].Add(playerId, player);
        }
      }

      PlayerChangesSinceLoad = false;
      //}

      endPlayerWrite();
    }

    public void SaveOutManualPlayersTable(string fileLocation)
    {
      startPlayerWrite();

      //lock (playerLocker)
      //{
      //Only need to save if there were changes since load
      if (PlayerChangesSinceLoad)
      {
        using (StreamWriter sw = new StreamWriter(fileLocation, false))
        {
          sw.WriteLine("playerId, playerName, pokerClientId, aiType, aiConfigStr");
          foreach (PokerClients client in pokerPlayers.Keys)
            foreach (PokerPlayer player in pokerPlayers[client].Values)
              sw.WriteLine(player.PlayerId + "," + player.PlayerName + "," + player.PokerClientId + "," + player.AiType + "," + player.AiConfigStr);
        }

        PlayerChangesSinceLoad = false;
      }
      //}

      endPlayerWrite();
    }

    public long[] ClientOpponentPlayerIds(PokerClients pokerClient, List<int> excludeAIGenerations, int currentTrainingGenerationType)
    {
      long[] playerIds = new long[0];
      startPlayerRead();

      //lock (playerLocker)
      //{
      if (pokerPlayers.ContainsKey(pokerClient))
      {
        playerIds = (from current in pokerPlayers[pokerClient].Values
                     where !excludeAIGenerations.Contains((int)current.AiType) && (((int)current.AiType == currentTrainingGenerationType && !current.AiConfigStr.StartsWith("Genetic")) || (int)current.AiType != currentTrainingGenerationType)
                     where current.PlayerName != ""
                     select current.PlayerId).ToArray();
      }

      //}

      endPlayerRead();

      return playerIds;
    }

    public void AIPlayerConfig(long playerId, out int aiType, out string aiConfigStr)
    {
      startPlayerRead();

      aiType = 0;
      aiConfigStr = "";

      //lock (playerLocker)
      //{
      foreach (PokerClients client in pokerPlayers.Keys)
      {
        if (pokerPlayers[client].ContainsKey(playerId))
        {
          aiType = (int)pokerPlayers[client][playerId].AiType;
          aiConfigStr = pokerPlayers[client][playerId].AiConfigStr;

          break;
        }
      }
      //}

      endPlayerRead();
    }

    /// <summary>
    /// Converts the aiConfigStr provided into playerIds, sorted in the same order.
    /// </summary>
    /// <param name="aiConfigStr"></param>
    /// <param name="pokerClientId"></param>
    /// <returns></returns>
    public long[] PlayerIdsFromConfigStr(string[] aiConfigStr, PokerClients pokerClientId)
    {
      long[] playerMatches;

      startPlayerRead();

      //lock (playerLocker)
      //{
      playerMatches = (from current in pokerPlayers[pokerClientId].Values
                       where aiConfigStr.Contains(current.AiConfigStr) && current.PlayerName != ""
                       select current.PlayerId).ToArray();
      //}

      endPlayerRead();

      if (playerMatches.Count() == 0)
        throw new Exception("The aiConfigStr provided did not produce any playerIds. aiConfigStr.Length=" + aiConfigStr.Length + ", aiConfigStr[0]=" + aiConfigStr[0]);
      else
        return playerMatches;
    }

    /// <summary>
    /// Converts the aiConfigStr provided into playerIds, sorted in the same order.
    /// </summary>
    /// <param name="aiConfigStr"></param>
    /// <param name="pokerClientId"></param>
    /// <returns></returns>
    public long[] PlayerIdsFromAiType(AIGeneration aiType, PokerClients pokerClientId)
    {
      long[] playerMatches;

      startPlayerRead();

      playerMatches = (from current in pokerPlayers[pokerClientId].Values
                       where current.AiType == aiType && current.PlayerName != ""
                       select current.PlayerId).ToArray();

      endPlayerRead();

      if (playerMatches.Count() == 0)
        throw new Exception("The aiType provided did not produce any playerIds. aiType=" + aiType + ", pokerClientId=" + pokerClientId);
      else
        return playerMatches;
    }

    public PokerPlayer PlayerDetails(string playerName, PokerClients pokerClientId)
    {
      PokerPlayer[] result;

      startPlayerRead();

      //lock (playerLocker)
      //{
      if (pokerPlayers.Count == 0 || !pokerPlayers.ContainsKey(pokerClientId))
      {
        endPlayerRead();
        return null;
      }

      result = (from current in pokerPlayers[pokerClientId].Values
                where current.PlayerName == playerName
                select current).ToArray();
      //}

      endPlayerRead();

      if (result.Length == 0)
        return null;
      else if (result.Length == 1)
        return result[0];
      else
        throw new Exception("More than a single player selected from database when there should only every be 0 or 1.");
    }

    public PokerPlayer PlayerDetails(long playerId)
    {
      PokerPlayer playerDetails;

      startPlayerRead();

      try
      {
        //lock (playerLocker)
        //{
        foreach (PokerClients client in pokerPlayers.Keys)
        {
          if (pokerPlayers[client].ContainsKey(playerId))
          {
            playerDetails = pokerPlayers[client][playerId];
            return playerDetails;
          }
        }

        throw new Exception("Unable to locate player in database.");
      }
      finally
      {
        endPlayerRead();
      }
    }

    public void DeleteAllClientPlayersFromManualPlayersTable(PokerClients pokerClientId)
    {
      startPlayerWrite();

      //lock (playerLocker)
      //{
      PlayerChangesSinceLoad = true;
      pokerPlayers = (from current in pokerPlayers
                      where current.Key != pokerClientId
                      select current).ToDictionary(dict => dict.Key, dict => dict.Value);
      //}

      endPlayerWrite();
    }

    public void DeleteLegacyTrainingPlayersFromManualPlayersTable(PokerClients pokerClientId, int generationNumToKeep)
    {
      startPlayerWrite();

      //lock (playerLocker)
      //{
      PlayerChangesSinceLoad = true;
      pokerPlayers[pokerClientId] = (from current in pokerPlayers[pokerClientId]
                                     where (current.Value.PlayerName.StartsWith("geneticPokerAI_" + generationNumToKeep) || !current.Value.PlayerName.StartsWith("geneticPokerAI_"))
                                     select current).ToDictionary(dict => dict.Key, dict => dict.Value);
      //}

      endPlayerWrite();
    }

    public long[] AddPlayersToManualPlayersTable(PokerClients pokerClientId, int aiType, string[] playerNames, string[] aiConfigStrs)
    {
      List<long> newPlayerIds = new List<long>();

      if (!pokerPlayers.ContainsKey(pokerClientId))
        pokerPlayers.Add(pokerClientId, new Dictionary<long, PokerPlayer>());

      startPlayerWrite();
      //lock (playerLocker)
      //{
      PlayerChangesSinceLoad = true;

      long nextPlayerId = 1;
      foreach (PokerClients client in pokerPlayers.Keys)
      {
        nextPlayerId = Math.Max(nextPlayerId, (pokerPlayers[client].Count > 0 ? pokerPlayers[client].Keys.Max() : 0) + 1);
      }

      for (int i = 0; i < playerNames.Length; i++)
      {
        if (databaseQueries.playerDetailsByPlayerName(playerNames[i], (short)pokerClientId) != null)
          throw new Exception("A player already exists with that name.");

        newPlayerIds.Add(nextPlayerId);
        pokerPlayers[pokerClientId].Add(nextPlayerId, new PokerPlayer(nextPlayerId, playerNames[i], (short)pokerClientId, (AIGeneration)aiType, aiConfigStrs[i]));

        nextPlayerId++;
      }
      //}

      endPlayerWrite();

      return newPlayerIds.ToArray();
    }

    public long AddPlayerToManualPlayersTable(string playerName, PokerClients pokerClientId, int aiType, string aiConfigStr)
    {
      //First check to see if this player already exists
      if (databaseQueries.playerDetailsByPlayerName(playerName, (short)pokerClientId) != null)
        throw new Exception("A player already exists with that name.");

      long newPlayerId;

      startPlayerWrite();

      if (!pokerPlayers.ContainsKey(pokerClientId))
        pokerPlayers.Add(pokerClientId, new Dictionary<long, PokerPlayer>());

      //lock (playerLocker)
      //{
      PlayerChangesSinceLoad = true;

      newPlayerId = 1;
      foreach (PokerClients client in pokerPlayers.Keys)
      {
        newPlayerId = Math.Max(newPlayerId, (pokerPlayers[client].Count > 0 ? pokerPlayers[client].Keys.Max() : 0) + 1);
      }

      pokerPlayers[pokerClientId].Add(newPlayerId, new PokerPlayer(newPlayerId, playerName, (short)pokerClientId, (AIGeneration)aiType, aiConfigStr));
      //}

      endPlayerWrite();

      return newPlayerId;
    }
  }
}
