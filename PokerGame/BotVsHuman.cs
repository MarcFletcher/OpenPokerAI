using System;
using PokerBot.Definitions;
using PokerBot.Database;
using System.Threading;
using PokerBot.AI;

namespace PokerBot.BotGame
{
  public class BotVsHumanPokerGame : PokerGameBase
  {
    PokerBot.CacheMonitor.CacheMonitor cacheMonitor = null;
    bool useAISetDecisionTime = false;

    public BotVsHumanPokerGame(PokerGameType gameType, databaseCacheClient clientCache, byte minNumTablePlayers, string[] playerNames, decimal startingStack, int maxHandsToPlay, int actionPause, PokerBot.CacheMonitor.CacheMonitor cacheMonitor)
        : base(gameType, clientCache, playerNames, startingStack, minNumTablePlayers, maxHandsToPlay, actionPause)
    {
      this.cacheMonitor = cacheMonitor;
    }

    public BotVsHumanPokerGame(PokerGameType gameType, databaseCacheClient clientCache, byte minNumTablePlayers, string[] playerNames, decimal startingStack, int maxHandsToPlay, bool useAISetDecisionTime)
        : base(gameType, clientCache, playerNames, startingStack, minNumTablePlayers, maxHandsToPlay, 1500)
    {
      this.useAISetDecisionTime = useAISetDecisionTime;
    }

    protected override Play getPlayerDecision()
    {
      Play playerDecision;
      bool isBotPlayer;
      long currentActivePlayerId = clientCache.getPlayerId(currentActionPosition);

      isBotPlayer = clientCache.getPlayerDetails(currentActivePlayerId).isBot;

      if (isBotPlayer)
      {
        playerDecision = aiManager.GetDecision(clientCache.getPlayerId(currentActionPosition), clientCache);


        //We want to implement the correct wait time for this game type.
        if (useAISetDecisionTime)
          Thread.Sleep((int)(playerDecision.DecisionTime * 1000));
        else
          Thread.Sleep(actionPause);
      }
      else
      {
        if (cacheMonitor == null)
          throw new Exception("Cannot accept human decisions without a valid cache monitor.");

        //We set the cache monitor to take a manual decision
        try
        {
          cacheMonitor.Invoke(new CacheMonitor.CacheMonitor.triggerDelegate(cacheMonitor.triggerManualDecision));
        }
        catch (Exception)
        {
          //Do nothing if this fails as we don't need to log and error and if something does go wrong it will be obvious.
        }

        //We wait for the decision to be made
        do
        {
          Thread.Sleep(500);
        } while (!cacheMonitor.ManualDecisionMade && !cacheMonitor.IsDisposed);

        //Validate the manual here
        //If the one provided is invalid we just pass it through our fixer function.
        playerDecision = AIManager.ValidatePlayerDecision(cacheMonitor.ManualDecision, clientCache);
      }

      //Return the decision here.
      return playerDecision;
    }
  }
}
