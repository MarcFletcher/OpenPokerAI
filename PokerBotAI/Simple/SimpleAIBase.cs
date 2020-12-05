using System;
using System.Collections.Generic;
using System.Linq;
using PokerBot.AI.InfoProviders;
using PokerBot.Definitions;

namespace PokerBot.AI
{
  internal abstract class SimpleAIBase : AIBase
  {
    protected class playerConfig
    {
      string aiConfigStr;
      decimal botPlayWRThreshold;
      decimal botRaiseWRThreshold;

      public playerConfig(string aiConfigStr, decimal playWR, decimal raiseWR)
      {
        this.aiConfigStr = aiConfigStr;
        this.botPlayWRThreshold = playWR;
        this.botRaiseWRThreshold = raiseWR;
      }

      public string AiConfigStr
      {
        get { return aiConfigStr; }
      }

      public decimal BotPlayWRThreshold
      {
        get { return botPlayWRThreshold; }
      }

      public decimal BotRaiseWRThreshold
      {
        get { return botRaiseWRThreshold; }
      }
    }
    protected List<playerConfig> playerConfigs = new List<playerConfig>();

    //protected CacheError cacheError;

    protected PokerAction botAction;
    protected float decisionTime;
    protected decimal betAmount;

    protected decimal currentRoundPlayerBetAmount;
    protected decimal currentRoundMinimumPlayAmount;

    protected byte bettingRound;

    //The amount which was last added to the pot in a raise
    //Any raise the AI makes must be atleast (currentRoundMinimumPlayAmount+currentRoundLastRaiseAmount)
    protected decimal currentRoundLastRaiseAmount;

    protected decimal currentWinPercentage;
    protected decimal currentWinRatio;

    protected decimal winRatioDifference;

    protected decimal botPlayWRThreshold;
    protected decimal botRaiseWRThreshold;
    protected bool botAlwaysPlayLB;

    //protected databaseCache.AIConfig botConfig;

    public SimpleAIBase(AIRandomControl aiRandomControl)
        : base(aiRandomControl)
    {
    }

    protected decimal getPlayWR(string aiConfigStr)
    {
      //Check to see if the network has already been retreived
      decimal playWR;

      try
      {

        var configs =
            from local in playerConfigs
            where local.AiConfigStr == aiConfigStr
            select local;

        if (configs.Count() == 1)
          playWR = configs.First().BotPlayWRThreshold;
        else if (configs.Count() == 0)
        {
          //We need to load the network
          string[] configValues = aiConfigStr.Split('-');
          string playWRStr = configValues[0].Replace("playWR=", "");
          string raiseWRStr = configValues[1].Replace("raiseWR=", "");

          playerConfigs.Add(new playerConfig(aiConfigStr, decimal.Parse(playWRStr), decimal.Parse(raiseWRStr)));
          playWR = decimal.Parse(playWRStr);
        }
        else
          throw new Exception("Why is there more than one entry for this aiConfigStr?");
      }
      catch (Exception ex)
      {
        throw new Exception("aiConfigStr was not formatted correctly for the current AI type.", ex);
      }

      return playWR;
    }

    protected decimal getRaiseWR(string aiConfigStr)
    {
      //Check to see if the network has already been retreived
      decimal raiseWR;

      try
      {

        var configs =
            from local in playerConfigs
            where local.AiConfigStr == aiConfigStr
            select local;

        if (configs.Count() == 1)
          raiseWR = configs.First().BotRaiseWRThreshold;
        else if (configs.Count() == 0)
        {
          //We need to load the network
          string[] configValues = aiConfigStr.Split('-');
          string playWRStr = configValues[0].Replace("playWR=", "");
          string raiseWRStr = configValues[1].Replace("raiseWR=", "");

          playerConfigs.Add(new playerConfig(aiConfigStr, decimal.Parse(playWRStr), decimal.Parse(raiseWRStr)));
          raiseWR = decimal.Parse(raiseWRStr);
        }
        else
          throw new Exception("Why is there more than one entry for this aiConfigStr?");
      }
      catch (Exception ex)
      {
        throw new Exception("aiConfigStr was not formatted correctly for the current AI type.", ex);
      }

      return raiseWR;
    }

    /// <summary>
    /// Returns an AI decision where the gameCache is already populated.
    /// </summary>
    /// <param name="handId">Current Hand Id</param>
    /// <param name="playerId">Player for which decision is required.</param>
    /// <param name="gameCache">The current gamecache</param>
    /// <returns></returns>
    protected override Play GetDecision()
    {
      //Initialise playDecision as fold which is returned if it is not modified anywhere
      botAction = PokerAction.Fold;
      betAmount = 0;
      decisionTime = 0;

      botPlayWRThreshold = getPlayWR(currentDecision.AiConfigStr);
      botRaiseWRThreshold = getRaiseWR(currentDecision.AiConfigStr);
      botAlwaysPlayLB = true;

      winRatioDifference = (botPlayWRThreshold / 2);
      bettingRound = currentDecision.Cache.getBettingRound();
      currentRoundMinimumPlayAmount = currentDecision.Cache.getMinimumPlayAmount();
      currentRoundLastRaiseAmount = currentDecision.Cache.getCurrentRoundLastRaiseAmount();

      currentRoundPlayerBetAmount = currentDecision.Cache.getPlayerCurrentRoundBetAmount(currentDecision.PlayerId);

      currentWinPercentage = infoStore.GetInfoValue(InfoType.WR_CardsOnlyWinPercentage);
      currentWinRatio = infoStore.GetInfoValue(InfoType.WR_CardsOnlyWinRatio);

      GetAIDecision();

      return new Play(botAction, betAmount, decisionTime, currentDecision.Cache.getCurrentHandId(), currentDecision.PlayerId, currentWinRatio.ToString(), 2);
    }

    protected abstract void GetAIDecision();
  }
}
