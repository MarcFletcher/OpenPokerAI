using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using PokerBot.AI.InfoProviders;
using PokerBot.Database;
using PokerBot.Definitions;

namespace ProviderAggression
{
  public class AggressionProvider : InfoProviderBase
  {
    private static object locker = new object();

    //int historicalHandLimit = 500;
    //int recentHandLimit = 100;

    //We will never use more than the max hand limit for aggression information
    static int maxHandLimit = 2000;

    //We will scale any scaled return value if less than this number of hands is available
    static int minHandLimit = 900;

    //We can give more recent hands more weight using this value but default is 1
    static decimal startWeight = 1;

    //Default values from MEDIAN averages of all players with over 1000 hands
    //For some stupid reason these were original hardcoded _ FOOL (now fixed!)
    public const decimal DEFAULT_rFREQ_PREFLOP = 0.094m;
    public const decimal DEFAULT_rFREQ_POSTFLOP = 0.260m;

    public const decimal DEFAULT_cFREQ_PREFLOP = 0.045m;
    public const decimal DEFAULT_cFREQ_POSTFLOP = 0.085m;

    public const decimal DEFAULT_chFREQ_PREFLOP = 0.005m;
    public const decimal DEFAULT_chFREQ_POSTFLOP = 0.430m;

    public const decimal DEFAULT_pFREQ_PREFLOP = 0.140m;
    public const decimal DEFAULT_pFREQ_POSTFLOP = 0.580m;

    //This is a list because it can have multiple entries for the same player
    private static List<PlayerAggression> playerAggressionCache;

    private class PlayerAggression
    {
      long playerId;
      AIGeneration aiType;

      long tableId = -1;

      int maxHands = 0;
      int handsCounted = 0;

      decimal playFreqPreFlop = 0;
      decimal playFreqPostFlop = 0;

      decimal checkFreq_PostFlop = 0;
      decimal checkFreq_PreFlop = 0;

      decimal cFreq_PreFlop = 0;
      decimal cFreq_PostFlop = 0;

      decimal rFreq_PreFlop = 0;
      decimal rFreq_PostFlop = 0;

      /// <summary>
      /// Creates an instance and retrieves data from databaseQueries
      /// </summary>
      /// <param name="playerId"></param>
      /// <param name="maxHands"></param>
      /// <param name="startWeight"></param>
      public PlayerAggression(long playerId, int maxHands, decimal startWeight, Random randomGen = null)
      {
        this.playerId = playerId;

        string config = "";
        CacheTracker.Instance.PlayerAiConfig(playerId, tableId, out aiType, out config);

        //We get aggression information for the most 'maxHands' worth of recent hands
        //databaseQueries.PlayerAgressionMetrics(playerId, maxHands, -1, startWeight, ref handsCounted, ref rFreq_PreFlop, ref rFreq_PostFlop, ref cFreq_PreFlop, ref cFreq_PostFlop, ref checkFreq_PreFlop, ref checkFreq_PostFlop, ref playFreqPreFlop, ref playFreqPostFlop);
        UpdatePlayerStats(maxHands, startWeight, randomGen);
      }

      /// <summary>
      /// Creates an instance using supplied values
      /// </summary>
      /// <param name="playerId"></param>
      /// <param name="maxHands"></param>
      /// <param name="handsCounted"></param>
      /// <param name="rFreq_PreFlop"></param>
      /// <param name="rFreq_PostFlop"></param>
      /// <param name="cFreq_PreFlop"></param>
      /// <param name="cFreq_PostFlop"></param>
      /// <param name="checkFreq_PreFlop"></param>
      /// <param name="checkFreq_PostFlop"></param>
      /// <param name="playFreqPreFlop"></param>
      /// <param name="playFreqPostFlop"></param>
      public PlayerAggression(long playerId, int maxHands, int handsCounted, decimal rFreq_PreFlop,
          decimal rFreq_PostFlop, decimal cFreq_PreFlop, decimal cFreq_PostFlop,
          decimal checkFreq_PreFlop, decimal checkFreq_PostFlop, decimal playFreqPreFlop, decimal playFreqPostFlop, AIGeneration aiType)
      {
        this.playerId = playerId;
        this.maxHands = maxHands;
        this.handsCounted = handsCounted;
        this.rFreq_PreFlop = rFreq_PreFlop;
        this.rFreq_PostFlop = rFreq_PostFlop;

        this.cFreq_PreFlop = cFreq_PreFlop;
        this.cFreq_PostFlop = cFreq_PostFlop;

        this.checkFreq_PreFlop = checkFreq_PreFlop;
        this.checkFreq_PostFlop = checkFreq_PostFlop;

        this.playFreqPreFlop = playFreqPreFlop;
        this.playFreqPostFlop = playFreqPostFlop;

        this.aiType = aiType;
      }

      public override string ToString()
      {
        return "[" + playerId + " - " + maxHands + " - " + handsCounted + "] " + cFreq_PreFlop + ", " + cFreq_PostFlop + " | " + rFreq_PreFlop + ", " + rFreq_PostFlop + " | " + playFreqPreFlop;
      }

      public string CSVString()
      {
        return playerId + ", " + maxHands + ", " + handsCounted + ", " + cFreq_PreFlop + ", " + cFreq_PostFlop + ", " + rFreq_PreFlop + ", " + rFreq_PostFlop + ", " + playFreqPreFlop;
      }

      /// <summary>
      /// Resets local values with those from database queries for the most 'maxHands' worth of recent hands
      /// </summary>
      /// <param name="maxHands"></param>
      /// <param name="startWeight"></param>
      public void Update(int maxHands, decimal startWeight, Random randomGen = null)
      {
        UpdatePlayerStats(maxHands, startWeight, randomGen);
      }

      private void UpdatePlayerStats(int maxHands, decimal startWeight, Random randomGen)
      {
        this.maxHands = maxHands;

        //If this is a cheater then we FIX the values ;)
        if (aiType == AIGeneration.CheatV1)
        {
          //We can set the handsCounted by using the hands player by this player
          handsCounted = Math.Min(maxHands, databaseQueries.NumHandsPlayed(playerId, false));

          //Noise is a percentage between 20%-100%. Lowest threshold is based on the that from the aggression analysis.
          double noiseLevel;
          if (handsCounted >= 900)
            noiseLevel = 0.2;
          else
            noiseLevel = (900 - handsCounted) * (0.8 / 900.0) + 0.2;

          //DEFAULT_rFREQ_PREFLOP = 0.094m;
          //DEFAULT_rFREQ_POSTFLOP = 0.260m;

          //DEFAULT_cFREQ_PREFLOP = 0.045m;
          //DEFAULT_cFREQ_POSTFLOP = 0.085m;

          //DEFAULT_chFREQ_PREFLOP = 0.005m;
          //DEFAULT_chFREQ_POSTFLOP = 0.430m;

          //DEFAULT_pFREQ_PREFLOP = 0.140m;
          //DEFAULT_pFREQ_POSTFLOP = 0.580m;

          //Cheaters just get the default values
          playFreqPreFlop = AggressionProvider.DEFAULT_pFREQ_PREFLOP + (decimal)((randomGen.NextDouble() * 2 - 1) * noiseLevel * (double)AggressionProvider.DEFAULT_pFREQ_PREFLOP);
          playFreqPostFlop = AggressionProvider.DEFAULT_pFREQ_POSTFLOP + (decimal)((randomGen.NextDouble() * 2 - 1) * noiseLevel * (double)AggressionProvider.DEFAULT_pFREQ_POSTFLOP);

          rFreq_PreFlop = AggressionProvider.DEFAULT_rFREQ_PREFLOP + (decimal)((randomGen.NextDouble() * 2 - 1) * noiseLevel * (double)AggressionProvider.DEFAULT_rFREQ_PREFLOP);
          cFreq_PreFlop = AggressionProvider.DEFAULT_cFREQ_PREFLOP + (decimal)((randomGen.NextDouble() * 2 - 1) * noiseLevel * (double)AggressionProvider.DEFAULT_cFREQ_PREFLOP);
          checkFreq_PreFlop = AggressionProvider.DEFAULT_chFREQ_PREFLOP + (decimal)((randomGen.NextDouble() * 2 - 1) * noiseLevel * (double)AggressionProvider.DEFAULT_chFREQ_PREFLOP);

          rFreq_PostFlop = AggressionProvider.DEFAULT_rFREQ_POSTFLOP + (decimal)((randomGen.NextDouble() * 2 - 1) * noiseLevel * (double)AggressionProvider.DEFAULT_rFREQ_POSTFLOP);
          cFreq_PostFlop = AggressionProvider.DEFAULT_cFREQ_POSTFLOP + (decimal)((randomGen.NextDouble() * 2 - 1) * noiseLevel * (double)AggressionProvider.DEFAULT_cFREQ_POSTFLOP);
          checkFreq_PostFlop = AggressionProvider.DEFAULT_chFREQ_POSTFLOP + (decimal)((randomGen.NextDouble() * 2 - 1) * noiseLevel * (double)AggressionProvider.DEFAULT_chFREQ_POSTFLOP);

          //>1 checks
          if (playFreqPreFlop > 1)
            playFreqPreFlop = 1;
          if (playFreqPostFlop > 1)
            playFreqPostFlop = 1;

          if (rFreq_PreFlop > 1)
            rFreq_PreFlop = 1;
          if (cFreq_PreFlop > 1)
            cFreq_PreFlop = 1;
          if (checkFreq_PreFlop > 1)
            checkFreq_PreFlop = 1;

          if (rFreq_PostFlop > 1)
            rFreq_PostFlop = 1;
          if (cFreq_PostFlop > 1)
            cFreq_PostFlop = 1;
          if (checkFreq_PostFlop > 1)
            checkFreq_PostFlop = 1;
        }
        else
          databaseQueries.PlayerAgressionMetrics(playerId, maxHands, -1, startWeight, ref handsCounted, ref rFreq_PreFlop, ref rFreq_PostFlop, ref cFreq_PreFlop, ref cFreq_PostFlop, ref checkFreq_PreFlop, ref checkFreq_PostFlop, ref playFreqPreFlop, ref playFreqPostFlop);
      }
      #region Get & Set

      public long PlayerId
      {
        get { return playerId; }
      }

      public AIGeneration AiType
      {
        get { return aiType; }
      }

      public int MaxHands
      {
        get { return maxHands; }
      }

      public int HandsCounted
      {
        get { return handsCounted; }
      }

      public decimal RFreq_PreFlop
      {
        get { return rFreq_PreFlop; }
      }

      public decimal RFreq_PostFlop
      {
        get { return rFreq_PostFlop; }
      }

      public decimal CFreq_PreFlop
      {
        get { return cFreq_PreFlop; }
      }

      public decimal CFreq_PostFlop
      {
        get { return cFreq_PostFlop; }
      }

      public decimal PlayFreqPreFlop
      {
        get { return playFreqPreFlop; }
      }

      public decimal PlayFreqPostFlop
      {
        get { return playFreqPostFlop; }
      }

      public decimal CheckFreq_PreFlop
      {
        get { return checkFreq_PreFlop; }
      }

      public decimal CheckFreq_PostFlop
      {
        get { return checkFreq_PostFlop; }
      }

      #endregion
    }

    public AggressionProvider(InfoCollection information, Dictionary<InfoProviderType, InfoProviderBase> allInformationProviders, AIRandomControl aiRandomControl)
        : base(information, InfoProviderType.AIAggression, allInformationProviders, aiRandomControl)
    {
      requiredInfoTypes = new List<InfoType>() { };
      providedInfoTypes = new List<InfoPiece> { 
                                                    //Old scaled values
                                                    new InfoPiece(InfoType.AP_AvgScaledOppRaiseFreq_Double,0),
                                                    new InfoPiece(InfoType.AP_AvgScaledOppCallFreq_Double,0),
                                                    new InfoPiece(InfoType.AP_AvgScaledOppPreFlopPlayFreq_Double,0),

                                                    //New live values including accuracy
                                                    new InfoPiece(InfoType.AP_AvgLiveOppPreFlopPlayFreq_Double,0),
                                                    new InfoPiece(InfoType.AP_AvgLiveOppPostFlopPlayFreq_Double,0),
                                                    new InfoPiece(InfoType.AP_AvgLiveOppCurrentRoundAggr_Double,0),
                                                    new InfoPiece(InfoType.AP_AvgLiveOppCurrentRoundAggrAcc_Double,0),
                                                    };

      AddProviderInformationTypes();

      lock (locker)
      {
        if (CurrentJob == null)
          playerAggressionCache = new List<PlayerAggression>();
        else
          //For now we will just start a blank list but we should be using the serialised data in the job.
          playerAggressionCache = new List<PlayerAggression>();
      }
    }

    protected override void updateInfo()
    {
      //Might as well tally values for all if either is required, no real difference there
      if (decisionRequest.RequiredInfoTypeUpdateKey.IsInfoTypeRequired(InfoType.AP_AvgScaledOppRaiseFreq_Double) ||
          decisionRequest.RequiredInfoTypeUpdateKey.IsInfoTypeRequired(InfoType.AP_AvgScaledOppCallFreq_Double) ||
          decisionRequest.RequiredInfoTypeUpdateKey.IsInfoTypeRequired(InfoType.AP_AvgScaledOppPreFlopPlayFreq_Double))
      {
        decimal avgScaledOppRaiseFreq = 0;
        decimal avgScaledOppCallFreq = 0;
        decimal avgScaledOppPreFlopPlayFreq = 0;
        decimal tempRaiseFreq, tempCallFreq;

        long[] activePlayerIds = decisionRequest.Cache.getActivePlayerIds();
        byte gameStage = decisionRequest.Cache.getBettingRound();

        //Retreive the necessary values.
        lock (locker)
        {
          #region Get Values From playerAggressionCache
          //If the player has less than recenthands we are going to use a 50/50 split with defaults using limits
          //If the player has more than recent hands we are going to take a 50/50 split using recent and historical

          //For each player
          for (int i = 0; i < activePlayerIds.Length; i++)
          {
            //We don't want to include ourself in this calculuation
            if (activePlayerIds[i] != decisionRequest.PlayerId)
            {
              var playerStats =
                  (from stats in playerAggressionCache
                   where stats.PlayerId == activePlayerIds[i]
                   select stats).ToList();

              if (playerStats.Count == 1)
              {
                #region 1StatPoint

                //We use this so we can approach real values once we have enough data
                bool scalingRequired = (playerStats[0].HandsCounted < minHandLimit);
                if (scalingRequired)
                {
                  decimal scalingFactor = (decimal)playerStats[0].HandsCounted / (decimal)minHandLimit;

                  avgScaledOppPreFlopPlayFreq += (DEFAULT_pFREQ_PREFLOP * (1 - scalingFactor) + playerStats[0].PlayFreqPreFlop * scalingFactor);

                  if (gameStage == 0)
                  {
                    tempRaiseFreq = playerStats[0].RFreq_PreFlop;
                    tempCallFreq = playerStats[0].CFreq_PreFlop;

                    avgScaledOppRaiseFreq += (DEFAULT_rFREQ_PREFLOP * (1 - scalingFactor) + tempRaiseFreq * scalingFactor);
                    avgScaledOppCallFreq += (DEFAULT_cFREQ_PREFLOP * (1 - scalingFactor) + tempCallFreq * scalingFactor);
                  }
                  else
                  {
                    tempRaiseFreq = playerStats[0].RFreq_PostFlop;
                    tempCallFreq = playerStats[0].CFreq_PostFlop;

                    avgScaledOppRaiseFreq += (DEFAULT_rFREQ_POSTFLOP * (1 - scalingFactor) + tempRaiseFreq * scalingFactor);
                    avgScaledOppCallFreq += (DEFAULT_cFREQ_POSTFLOP * (1 - scalingFactor) + tempCallFreq * scalingFactor);
                  }
                }
                else
                {
                  avgScaledOppPreFlopPlayFreq += playerStats[0].PlayFreqPreFlop;

                  if (gameStage == 0)
                  {
                    avgScaledOppRaiseFreq += playerStats[0].RFreq_PreFlop;
                    avgScaledOppCallFreq += playerStats[0].CFreq_PreFlop;
                  }
                  else
                  {
                    avgScaledOppRaiseFreq += playerStats[0].RFreq_PostFlop;
                    avgScaledOppCallFreq += playerStats[0].CFreq_PostFlop;
                  }
                }
                #endregion
              }
              else
              {
                #region 0StatPoint - Just defaults
                if (gameStage == 0)
                {
                  avgScaledOppRaiseFreq += DEFAULT_rFREQ_PREFLOP;
                  avgScaledOppCallFreq += DEFAULT_cFREQ_PREFLOP;
                  avgScaledOppPreFlopPlayFreq += DEFAULT_pFREQ_PREFLOP;
                }
                else
                {
                  avgScaledOppRaiseFreq += DEFAULT_rFREQ_POSTFLOP;
                  avgScaledOppCallFreq += DEFAULT_cFREQ_POSTFLOP;
                  avgScaledOppPreFlopPlayFreq += DEFAULT_pFREQ_PREFLOP;
                }
                #endregion
              }
            }
          }
          #endregion
        }

        if (decisionRequest.RequiredInfoTypeUpdateKey.IsInfoTypeRequired(InfoType.AP_AvgScaledOppRaiseFreq_Double))
          infoStore.SetInformationValue(InfoType.AP_AvgScaledOppRaiseFreq_Double, avgScaledOppRaiseFreq / (activePlayerIds.Length - 1));

        if (decisionRequest.RequiredInfoTypeUpdateKey.IsInfoTypeRequired(InfoType.AP_AvgScaledOppCallFreq_Double))
          infoStore.SetInformationValue(InfoType.AP_AvgScaledOppCallFreq_Double, avgScaledOppCallFreq / (activePlayerIds.Length - 1));

        if (decisionRequest.RequiredInfoTypeUpdateKey.IsInfoTypeRequired(InfoType.AP_AvgScaledOppPreFlopPlayFreq_Double))
          infoStore.SetInformationValue(InfoType.AP_AvgScaledOppPreFlopPlayFreq_Double, avgScaledOppPreFlopPlayFreq / (activePlayerIds.Length - 1));
      }

      if (decisionRequest.RequiredInfoTypeUpdateKey.IsInfoTypeRequired(InfoType.AP_AvgLiveOppPreFlopPlayFreq_Double) ||
          decisionRequest.RequiredInfoTypeUpdateKey.IsInfoTypeRequired(InfoType.AP_AvgLiveOppPostFlopPlayFreq_Double) ||
          decisionRequest.RequiredInfoTypeUpdateKey.IsInfoTypeRequired(InfoType.AP_AvgLiveOppCurrentRoundAggr_Double) ||
          decisionRequest.RequiredInfoTypeUpdateKey.IsInfoTypeRequired(InfoType.AP_AvgLiveOppCurrentRoundAggrAcc_Double))
      {
        decimal avgLiveOppPreFlopPlayFreq = 0;
        decimal avgLiveOppPostFlopPlayFreq = 0;

        decimal avgLiveOppCurrentRoundAggr = 0;
        decimal avgLiveOppCurrentRoundAggrAcc = 0;

        long[] activePlayerIds = decisionRequest.Cache.getActivePlayerIds();
        byte gameStage = decisionRequest.Cache.getBettingRound();

        //We scale our aggression to be between 1 and 0, 1 being very aggressive, 0 being very passive
        decimal defaultPreflopAggression = (decimal)((Math.Log10((double)(DEFAULT_rFREQ_PREFLOP / (DEFAULT_cFREQ_PREFLOP + DEFAULT_chFREQ_PREFLOP))) + 2.0) / 3.0);
        decimal defaultPostflopAggression = (decimal)((Math.Log10((double)(DEFAULT_rFREQ_POSTFLOP / (DEFAULT_cFREQ_POSTFLOP + DEFAULT_chFREQ_POSTFLOP))) + 2.0) / 3.0);

        //Quick check
        if (defaultPreflopAggression > 1 || defaultPreflopAggression < 0)
          throw new Exception("defaultPreflopAggression should be between 0 and 1");
        if (defaultPostflopAggression > 1 || defaultPostflopAggression < 0)
          throw new Exception("defaultPostflopAggression should be between 0 and 1");

        //Retreive the necessary values.
        lock (locker)
        {
          #region Get Values From playerAggressionCache
          //If the player has less than recenthands we are going to use a 50/50 split with defaults using limits
          //If the player has more than recent hands we are going to take a 50/50 split using recent and historical

          //For each player
          for (int i = 0; i < activePlayerIds.Length; i++)
          {
            //We don't want to include ourself in this calculuation
            if (activePlayerIds[i] != decisionRequest.PlayerId)
            {
              var playerStats =
                  (from stats in playerAggressionCache
                   where stats.PlayerId == activePlayerIds[i]
                   select stats).ToList();

              if (playerStats.Count == 1)
              {
                #region 1StatPoint
                avgLiveOppPreFlopPlayFreq += playerStats[0].PlayFreqPreFlop;
                avgLiveOppPostFlopPlayFreq += playerStats[0].PlayFreqPostFlop;

                //Now the accuracy and aggression values
                if (playerStats[0].HandsCounted < minHandLimit)
                  avgLiveOppCurrentRoundAggrAcc += (decimal)playerStats[0].HandsCounted / (decimal)minHandLimit;
                else
                  avgLiveOppCurrentRoundAggrAcc += 1;

                decimal rFreq;
                decimal cFreq;
                decimal chFreq;
                if (gameStage == 0)
                {
                  rFreq = playerStats[0].RFreq_PreFlop;
                  cFreq = playerStats[0].CFreq_PreFlop;
                  chFreq = playerStats[0].CheckFreq_PreFlop;
                }
                else
                {
                  rFreq = playerStats[0].RFreq_PostFlop;
                  cFreq = playerStats[0].CFreq_PostFlop;
                  chFreq = playerStats[0].CheckFreq_PostFlop;
                }

                decimal playerAggression = 0;
                if (cFreq + chFreq > 0 && rFreq > 0)
                  //If we know the .Log10 won't die
                  playerAggression = (decimal)((Math.Log10((double)(rFreq / (cFreq + chFreq))) + 2.0) / 3.0);
                else if (cFreq + chFreq == 0 && rFreq > 0)
                  //If there have never been any calls or checks only raises then aggression defaults to 1
                  playerAggression = 1;

                if (playerAggression > 1)
                  playerAggression = 1;
                if (playerAggression < 0)
                  playerAggression = 0;

                avgLiveOppCurrentRoundAggr += playerAggression;
                #endregion
              }
              else
              {
                #region 0StatPoint - Just defaults

                avgLiveOppPreFlopPlayFreq += DEFAULT_pFREQ_PREFLOP;
                avgLiveOppPostFlopPlayFreq += DEFAULT_pFREQ_POSTFLOP;

                //We have no data so accuracy is zero
                avgLiveOppCurrentRoundAggrAcc += 0;

                if (gameStage == 0)
                  avgLiveOppCurrentRoundAggr += defaultPreflopAggression;
                else
                  avgLiveOppCurrentRoundAggr += defaultPostflopAggression;
                #endregion
              }
            }
          }
          #endregion
        }

        if (decisionRequest.RequiredInfoTypeUpdateKey.IsInfoTypeRequired(InfoType.AP_AvgLiveOppPreFlopPlayFreq_Double))
          infoStore.SetInformationValue(InfoType.AP_AvgLiveOppPreFlopPlayFreq_Double, avgLiveOppPreFlopPlayFreq / (activePlayerIds.Length - 1));

        if (decisionRequest.RequiredInfoTypeUpdateKey.IsInfoTypeRequired(InfoType.AP_AvgLiveOppPostFlopPlayFreq_Double))
          infoStore.SetInformationValue(InfoType.AP_AvgLiveOppPostFlopPlayFreq_Double, avgLiveOppPostFlopPlayFreq / (activePlayerIds.Length - 1));

        if (decisionRequest.RequiredInfoTypeUpdateKey.IsInfoTypeRequired(InfoType.AP_AvgLiveOppCurrentRoundAggr_Double))
          infoStore.SetInformationValue(InfoType.AP_AvgLiveOppCurrentRoundAggr_Double, avgLiveOppCurrentRoundAggr / (activePlayerIds.Length - 1));

        if (decisionRequest.RequiredInfoTypeUpdateKey.IsInfoTypeRequired(InfoType.AP_AvgLiveOppCurrentRoundAggrAcc_Double))
          infoStore.SetInformationValue(InfoType.AP_AvgLiveOppCurrentRoundAggrAcc_Double, avgLiveOppCurrentRoundAggrAcc / (activePlayerIds.Length - 1));
      }
    }

    public override void ProviderSlowUpdateTask()
    {
      //It's possible this can get called more than once for multiple instances
      closeThread = false;
      providerUpdateThread(true);
    }

    public override void ResetProvider()
    {
      lock (locker)
        playerAggressionCache = new List<PlayerAggression>();
    }

    //Continually updates the agression values for all tables in the list
    private void providerUpdateThread(object runOnce)
    {
      long[] allPlayersIds;
      long[] currentPlayerIds;
      long[] newPlayerIds;
      List<PlayerAggression> tempPlayerAggressionCache;

      //Closes this thread when the AI is asked to close
      while (!closeThread)
      {
        try
        {
          DateTime startTime = DateTime.Now;

          //Get the temp version and keep working on that
          lock (locker)
            tempPlayerAggressionCache = (from current in playerAggressionCache
                                         select new PlayerAggression(current.PlayerId, current.MaxHands, current.HandsCounted, current.RFreq_PreFlop, current.RFreq_PostFlop, current.CFreq_PreFlop, current.CFreq_PostFlop, current.CheckFreq_PreFlop, current.CheckFreq_PostFlop, current.PlayFreqPreFlop, current.PlayFreqPostFlop, current.AiType)).ToList();

          //Get all players current in cacheTracker
          allPlayersIds = CacheTracker.Instance.AllActivePlayers();

          //Make sure we are only calculating for players that still exist
          tempPlayerAggressionCache = (from current in tempPlayerAggressionCache where allPlayersIds.Contains(current.PlayerId) select current).ToList();

          currentPlayerIds = (from current in tempPlayerAggressionCache select current.PlayerId).Distinct().ToArray();
          newPlayerIds = (allPlayersIds.Except(currentPlayerIds)).ToArray();

          //We need to create tasks that we can start in one go and then wait on all
          List<Task> aggressionUpdateTasks = new List<Task>();

          //Go through the currentPlayerIds
          #region currentPlayers
          Action<long> currentPlayersAction = new Action<long>((playerId) =>
          {
            int totalPlayerHands = databaseQueries.NumHandsPlayed(playerId, true);

            PlayerAggression[] playerAggressionRows =
              (from temp in tempPlayerAggressionCache
               where temp.PlayerId == playerId
               orderby temp.MaxHands ascending
               select temp).ToArray();

            if (playerAggressionRows.Length == 1)
            {
              if (totalPlayerHands > minHandLimit)
                playerAggressionRows[0].Update(maxHandLimit, startWeight, randomGen);
              else
                playerAggressionRows[0].Update(minHandLimit, startWeight, randomGen);
            }
            else
              throw new Exception("This should not be possible!");
          });

          foreach (long outerPlayerId in currentPlayerIds)
          {
            #region MultiThreadSection
            long playerId = outerPlayerId;

            if (ConcurrencyMode.Concurrency == ConcurrencyMode.ConcurencyModel.MultiCore)
              aggressionUpdateTasks.Add(Task.Factory.StartNew(new Action(() => { currentPlayersAction(playerId); })));
            else
              currentPlayersAction(playerId);

            #endregion
          }
          #endregion

          //Now any new players
          #region newPlayers
          Action<long> newPlayersAction = new Action<long>((playerId) =>
          {
            int totalPlayerHands = databaseQueries.NumHandsPlayed(playerId, true);

            if (totalPlayerHands > 0)
            {
              PlayerAggression tempObject;
              if (totalPlayerHands > minHandLimit)
                tempObject = new PlayerAggression(playerId, maxHandLimit, startWeight, randomGen);
              else
                tempObject = new PlayerAggression(playerId, minHandLimit, startWeight, randomGen);

              lock (locker)
                tempPlayerAggressionCache.Add(tempObject);
            }
          });

          foreach (long outerPlayerId in newPlayerIds)
          {
            #region MultiThreadSection
            long playerId = outerPlayerId;

            //Lamba functions fucking ROCK!!
            if (ConcurrencyMode.Concurrency == ConcurrencyMode.ConcurencyModel.MultiCore)
              aggressionUpdateTasks.Add(Task.Factory.StartNew(new Action(() => { newPlayersAction(playerId); })));
            else
              newPlayersAction(playerId);

            #endregion
          }
          #endregion

          if (ConcurrencyMode.Concurrency == ConcurrencyMode.ConcurencyModel.MultiCore)
            Task.WaitAll(aggressionUpdateTasks.ToArray());

          //Now copy the cache back.
          lock (locker)
            playerAggressionCache = tempPlayerAggressionCache;

          //If this method is used in single shot mode it should return here.
          if (runOnce != null)
            if ((bool)runOnce)
              return;

          Thread.Sleep(500);
        }
        catch (TimeoutException ex)
        {
          //"Aggression Provider Worker SP Timeout."
        }
        catch (Exception ex)
        {
          LogError.Log(ex, "AIPAPError");
        }
      }
    }

    /// <summary>
    /// Convert a play frequency (normally preflop value) to a winRatio using the determined fit.
    /// </summary>
    /// <param name="playFreq"></param>
    /// <returns></returns>
    public static double ConvertPlayFreqToWinRatio(double playFreq)
    {
      //Determined from simulated playing data using the simple AI's
      //Using statistical information gets you very close but the fit required is a much higher order
      //This fit gives a reasonable fit to 'real' data to we are going to use that.
      return (-4.5523 * Math.Pow(playFreq, 3)) + (8.7179 * Math.Pow(playFreq, 2)) - (6.2106 * playFreq) + 2.5224;
    }

    /// <summary>
    /// Can be used to retreieve player aggression information the the provider cache (i.e. faster than running the equivalent databaseQuery)
    /// </summary>
    /// <param name="playerId"></param>
    /// <param name="RFreq_PreFlop"></param>
    /// <param name="RFreq_PostFlop"></param>
    /// <param name="CFreq_PreFlop"></param>
    /// <param name="CFreq_PostFlop"></param>
    /// <param name="PreFlopPlayFreq"></param>
    public static byte PlayerAggressionMetricFromCache(long playerId, ref decimal RFreq_PreFlop, ref decimal RFreq_PostFlop, ref decimal CFreq_PreFlop, ref decimal CFreq_PostFlop,
        ref decimal CheckFreq_PreFlop, ref decimal CheckFreq_PostFlop, ref decimal PreFlopPlayFreq, ref decimal PostFlopPlayFreq)
    {
      byte returnValue = 0;

      //Retreive the necessary values.
      lock (locker)
      {
        #region Get Values From playerAggressionCache
        //If the player has less than recenthands we are going to use a 50/50 split with defaults using limits
        var playerStats =
            (from stats in playerAggressionCache
             where stats.PlayerId == playerId
             select stats).ToList();

        if (playerStats.Count == 1)
        {
          #region 1StatPoint

          bool scalingRequired = (playerStats[0].HandsCounted < minHandLimit);
          if (scalingRequired)
          {
            returnValue = 1;
            decimal scalingFactor = (decimal)playerStats[0].HandsCounted / (decimal)minHandLimit;

            PreFlopPlayFreq = (DEFAULT_pFREQ_PREFLOP * (1 - scalingFactor) + playerStats[0].PlayFreqPreFlop * scalingFactor);
            PostFlopPlayFreq = (DEFAULT_pFREQ_POSTFLOP * (1 - scalingFactor) + playerStats[0].PlayFreqPostFlop * scalingFactor);

            RFreq_PreFlop = (DEFAULT_rFREQ_PREFLOP * (1 - scalingFactor) + playerStats[0].RFreq_PreFlop * scalingFactor);
            CFreq_PreFlop = (DEFAULT_cFREQ_PREFLOP * (1 - scalingFactor) + playerStats[0].CFreq_PreFlop * scalingFactor);
            CheckFreq_PreFlop = (DEFAULT_chFREQ_PREFLOP * (1 - scalingFactor) + playerStats[0].CheckFreq_PreFlop * scalingFactor);

            RFreq_PostFlop = (DEFAULT_rFREQ_POSTFLOP * (1 - scalingFactor) + playerStats[0].RFreq_PostFlop * scalingFactor);
            CFreq_PostFlop = (DEFAULT_cFREQ_POSTFLOP * (1 - scalingFactor) + playerStats[0].CFreq_PostFlop * scalingFactor);
            CheckFreq_PostFlop = (DEFAULT_chFREQ_POSTFLOP * (1 - scalingFactor) + playerStats[0].CheckFreq_PostFlop * scalingFactor);
          }
          else
          {
            returnValue = 2;
            PreFlopPlayFreq = (playerStats[0].PlayFreqPreFlop);
            PostFlopPlayFreq = (playerStats[0].PlayFreqPostFlop);

            RFreq_PreFlop = (playerStats[0].RFreq_PreFlop);
            CFreq_PreFlop = (playerStats[0].CFreq_PreFlop);
            CheckFreq_PreFlop = (playerStats[0].CheckFreq_PreFlop);

            RFreq_PostFlop = (playerStats[0].RFreq_PostFlop);
            CFreq_PostFlop = (playerStats[0].CFreq_PostFlop);
            CheckFreq_PostFlop = (playerStats[0].CheckFreq_PostFlop);
          }

          #endregion
        }
        else
        {
          #region 0StatPoint - Just defaults
          PreFlopPlayFreq = DEFAULT_pFREQ_PREFLOP;
          PostFlopPlayFreq = DEFAULT_pFREQ_POSTFLOP;

          RFreq_PreFlop = DEFAULT_rFREQ_PREFLOP;
          CFreq_PreFlop = DEFAULT_cFREQ_PREFLOP;
          CheckFreq_PreFlop = DEFAULT_chFREQ_PREFLOP;

          RFreq_PostFlop = DEFAULT_rFREQ_POSTFLOP;
          CFreq_PostFlop = DEFAULT_cFREQ_POSTFLOP;
          CheckFreq_PostFlop = DEFAULT_chFREQ_POSTFLOP;

          #endregion
        }

        #endregion
      }

      return returnValue;
    }
  }
}
