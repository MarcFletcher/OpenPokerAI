using System;
using PokerBot.Definitions;
using PokerBot.Database;
using PokerBot.AI;
using System.Threading;

namespace PokerBot.BotGame
{
  public class BotVsBotPokerGame : PokerGameBase
  {

    public BotVsBotPokerGame(PokerGameType gameType, databaseCacheClient clientCache, byte minNumTablePlayers, string[] playerNames, decimal startingStack, int maxHandsToPlay, int actionPause)
        : base(gameType, clientCache, playerNames, startingStack, minNumTablePlayers, maxHandsToPlay, actionPause)
    {
      //Nothing special happens here
    }

    public BotVsBotPokerGame(PokerGameType gameType, databaseCacheClient clientCache, byte minNumTablePlayers, AIManager aiManager, string[] playerNames, decimal startingStack, int maxHandsToPlay, int actionPause)
        : base(gameType, clientCache, playerNames, startingStack, minNumTablePlayers, maxHandsToPlay, actionPause, aiManager)
    {
      //Nothing special happens here
    }

    protected override Play getPlayerDecision()
    {
      Play playerDecision;
      bool isBotPlayer;

      //Commented out as this class IS ONLY ever used when it's JUST bots
      //This call also adds an extra 0.8ms / 6% to the overall decision time.
      //isBotPlayer = clientCache.getPlayerDetails(clientCache.getPlayerId(currentActionPosition)).isBot;
      isBotPlayer = true;

      if (aiManager == null)
        throw new Exception("aiManager must be created in order to use it!");

      playerDecision = aiManager.GetDecision(clientCache.getPlayerId(currentActionPosition), clientCache);

      if (actionPause > 0)
        Thread.Sleep(actionPause);

      return playerDecision;
    }

  }
}
