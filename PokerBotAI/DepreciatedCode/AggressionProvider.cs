//using System;
//using System.Collections.Generic;
//using System.Linq;
//using System.Text;
//using PokerBot.Definitions;
//using System.Threading;
//using PokerBot.Database;
//using PokerBot.WebManagement;
//using PokerBot.AI.InfoProviders;

//namespace PokerBot.AI.Depreciated
//{
//    public class AggressionProvider : InfoProviderBase
//    {

//        public AggressionProvider(InfoCollection information, Dictionary<InfoProviderType, InfoProviderBase> allInformationProviders, CacheTracker cacheTracker, AIRandomControl aiRandomControl)
//            : base(information, InfoProviderType.AIAggression, allInformationProviders, cacheTracker, aiRandomControl)
//        {
//            requiredInfoTypes = new List<InfoType>() { };
//            providedInfoTypes = new List<InfoPiece> { 
//                                                    new InfoPiece(InfoType.AP_AvgScaledOppRaiseFreq_Double,0),
//                                                    new InfoPiece(InfoType.AP_AvgScaledOppCallFreq_Double,0),
//                                                    new InfoPiece(InfoType.AP_AvgScaledOppPreFlopPlayFreq_Double,0)
//                                                    };

//            AddProviderInformationTypes();

//            //Disable agression provider
//            if (false)
//            {
//                lock (workerStartLock)
//                {
//                    if (updater == null)
//                    {
//                        updater = new Thread(providerUpdateThread);
//                        updater.Priority = ThreadPriority.Lowest;
//                        updater.Name = "AIA_BackgroundWorker";
//                        updater.Start();
//                    }
//                }
//            }
//        }

//        /// <summary>
//        /// Convert a play frequency (normally preflop value) to a winRatio using the determined fit.
//        /// </summary>
//        /// <param name="playFreq"></param>
//        /// <returns></returns>
//        public static double ConvertPlayFreqToWinRatio(double playFreq)
//        {
//            //Determined from simulated playing data using the simple AI's
//            //Using statistical information gets you very close but the fit required is a much higher order
//            //This fit gives a reasonable fit to 'real' data to we are going to use that.
//            return (-4.5523 * Math.Pow(playFreq, 3)) + (8.7179 * Math.Pow(playFreq, 2)) - (6.2106 * playFreq) + 2.5224;
//        }

//        private object locker = new object();

//        int historicalHandLimit = 200;
//        int recentHandLimit = 20;
//        double startWeight = 1;

//        private class PlayerAggression
//        {
//            long playerId;
//            int maxHands=0;

//            int handsCounted = 0;
//            decimal rFreq_PreFlop=0;
//            decimal rFreq_PostFlop=0;
//            decimal cFreq_PreFlop=0;
//            decimal cFreq_PostFlop=0;

//            decimal checkFreq_PreFlop = 0;
//            decimal checkFreq_PostFlop = 0;

//            decimal playFreqPreFlop = 0;

//            public PlayerAggression(long playerId, int maxHands, decimal startWeight)
//            {
//                this.playerId = playerId;
//                this.maxHands = maxHands;

//                //Change to aggression query not added to depreciated code
//                throw new NotImplementedException();
//                //databaseQueries.PlayerAgressionMetrics(playerId, maxHands, -1, startWeight, ref handsCounted, ref rFreq_PreFlop, ref rFreq_PostFlop, ref cFreq_PreFlop, ref cFreq_PostFlop, ref checkFreq_PreFlop, ref checkFreq_PostFlop, ref playFreqPreFlop);
//            }

//            public PlayerAggression(long playerId, int maxHands, int handsCounted, decimal rFreq_PreFlop,
//            decimal rFreq_PostFlop, decimal cFreq_PreFlop, decimal cFreq_PostFlop, decimal playFreqPreFlop)
//            {
//                this.playerId = playerId;
//                this.maxHands = maxHands;
//                this.handsCounted = handsCounted;
//                this.rFreq_PreFlop = rFreq_PreFlop;
//                this.rFreq_PostFlop = rFreq_PostFlop;
//                this.cFreq_PreFlop = cFreq_PreFlop;
//                this.cFreq_PostFlop = cFreq_PostFlop;
//                this.playFreqPreFlop = playFreqPreFlop;
//            }

//            public void Update(int maxHands, decimal startWeight)
//            {
//                this.maxHands = maxHands;

//                //No need to add check frequencies to code which has been depreciated
//                throw new NotImplementedException();
//                //databaseQueries.PlayerAgressionMetrics(playerId, maxHands, -1, startWeight, ref handsCounted, ref rFreq_PreFlop, ref rFreq_PostFlop, ref cFreq_PreFlop, ref cFreq_PostFlop, ref playFreqPreFlop);
//            }

//            #region Get & Set

//            public long PlayerId
//            {
//                get { return playerId; }
//            }

//            public int MaxHands
//            {
//                get { return maxHands; }
//            }

//            public int HandsCounted
//            {
//                get { return handsCounted; }
//            }

//            public decimal RFreq_PreFlop
//            {
//                get { return rFreq_PreFlop; }
//            }

//            public decimal RFreq_PostFlop
//            {
//                get { return rFreq_PostFlop; }
//            }

//            public decimal CFreq_PreFlop
//            {
//                get { return cFreq_PreFlop; }
//            }

//            public decimal CFreq_PostFlop
//            {
//                get { return cFreq_PostFlop; }
//            }

//            public decimal PlayFreqPreFlop
//            {
//                get { return playFreqPreFlop; }
//            }

//            #endregion
//        }
//        private static List<PlayerAggression> playerAggressionCache = new List<PlayerAggression>();

//        //The background thread which will do the heavy lifting
//        private static Thread updater;
//        private static object workerStartLock = new object();

//        protected override void updateInfo()
//        {
//            decimal avgScaledOppRaiseFreq=0;
//            decimal avgScaledOppCallFreq=0;
//            decimal avgScaledOppPreFlopPlayFreq=0;
//            decimal tempRaiseFreq, tempCallFreq, tempPreFlopPlayFreq;

//            long[] activePlayerIds = decisionRequest.Cache.getActivePlayerIds();
//            byte gameStage = decisionRequest.Cache.getBettingRound();

//            //Retreive the necessary values.
//            lock (locker)
//            {
//                #region Get Values From playerAggressionCache
//                //If the player has less than recenthands we are going to use a 50/50 split with defaults using limits
//                //PreFlop Raise Default - 0.08 (MAX 0.18, MIN 0)
//                //PreFlop Call Default - 0.08 (MAX 0.2, MIN 0)
//                //PostFlop Raise Default - 0.20 (MAX 0.32,MIN 0.08)
//                //PostFlop Call Default - 0.08 (MAX 0.16, MIN 0)

//                //PreFlopPlayFreq Default - 0.15 (MAX 0.2, MIN 0.1)

//                //If the player has more than recent hands we are going to take a 50/50 split using recent and historical

//                //For each player
//                for (int i = 0; i < activePlayerIds.Length; i++)
//                {
//                    //We don't want to include ourself in this calculuation
//                    if (activePlayerIds[i] != decisionRequest.PlayerId)
//                    {
//                        var playerStats =
//                            (from stats in playerAggressionCache
//                             where stats.PlayerId == activePlayerIds[i]
//                             select stats).ToList();

//                        if (playerStats.Count == 2)
//                        {
//                            #region 2StatPoints
//                            avgScaledOppPreFlopPlayFreq += playerStats[0].PlayFreqPreFlop * 0.5m + playerStats[1].PlayFreqPreFlop * 0.5m;

//                            if (gameStage == 0)
//                            {
//                                avgScaledOppRaiseFreq += playerStats[0].RFreq_PreFlop * 0.5m + playerStats[1].RFreq_PreFlop * 0.5m;
//                                avgScaledOppCallFreq += playerStats[0].CFreq_PreFlop * 0.5m + playerStats[1].CFreq_PreFlop * 0.5m;
//                            }
//                            else
//                            {
//                                avgScaledOppRaiseFreq += playerStats[0].RFreq_PostFlop * 0.5m + playerStats[1].RFreq_PostFlop * 0.5m;
//                                avgScaledOppCallFreq += playerStats[0].CFreq_PostFlop * 0.5m + playerStats[1].CFreq_PostFlop * 0.5m;
//                            }
//                            #endregion
//                        }
//                        else if (playerStats.Count == 1)
//                        {
//                            #region 1StatPoint - Use defaults scaling
//                            tempPreFlopPlayFreq = playerStats[0].PlayFreqPreFlop;
//                            //if (tempPreFlopPlayFreq > 0.2) tempPreFlopPlayFreq = 0.2;
//                            //if (tempPreFlopPlayFreq < 0.1) tempPreFlopPlayFreq = 0.1;

//                            //We use this so we can approach real values once we have enough data
//                            decimal scalingFactor = (decimal)playerStats[0].HandsCounted / (decimal)playerStats[0].MaxHands;

//                            avgScaledOppPreFlopPlayFreq += (0.15m * (1-scalingFactor) + tempPreFlopPlayFreq * scalingFactor) * 0.5m + 0.15m * 0.5m;

//                            if (gameStage == 0)
//                            {
//                                tempRaiseFreq = playerStats[0].RFreq_PreFlop;
//                                //if (tempRaiseFreq > 0.18) tempRaiseFreq = 0.18;

//                                tempCallFreq = playerStats[0].CFreq_PreFlop;
//                                //if (tempCallFreq > 0.2) tempCallFreq = 0.2;

//                                avgScaledOppRaiseFreq += (0.08m * (1 - scalingFactor) + tempRaiseFreq * scalingFactor) * 0.5m + (0.08) * 0.5m;
//                                avgScaledOppCallFreq += (0.08m * (1 - scalingFactor) + tempCallFreq * scalingFactor) * 0.5m + (0.08) * 0.5m; 
//                            }
//                            else
//                            {
//                                tempRaiseFreq = playerStats[0].RFreq_PostFlop;
//                                //if (tempRaiseFreq > 0.32) tempRaiseFreq = 0.32;
//                                //if (tempRaiseFreq < 0.08) tempRaiseFreq = 0.08;

//                                tempCallFreq = playerStats[0].CFreq_PostFlop;
//                                //if (tempCallFreq > 0.16) tempCallFreq = 0.16;

//                                avgScaledOppRaiseFreq += (0.20 * (1 - scalingFactor) + tempRaiseFreq * scalingFactor) * 0.5 + (0.20) * 0.5;
//                                avgScaledOppCallFreq += (0.08 * (1 - scalingFactor) + tempCallFreq * scalingFactor) * 0.5 + (0.08) * 0.5;
//                            }
//                            #endregion
//                        }
//                        else
//                        {
//                            #region 0StatPoint - Just defaults
//                            if (gameStage == 0)
//                            {
//                                avgScaledOppRaiseFreq += 0.08;
//                                avgScaledOppCallFreq += 0.08;
//                                avgScaledOppPreFlopPlayFreq += 0.15;
//                            }
//                            else
//                            {
//                                avgScaledOppRaiseFreq += 0.20;
//                                avgScaledOppCallFreq += 0.08;
//                                avgScaledOppPreFlopPlayFreq += 0.15;
//                            }
//                            #endregion
//                        }
//                    }
//                }
//                #endregion
//            }

//            infoStore.SetInformationValue(InfoType.AP_AvgScaledOppRaiseFreq_Double, avgScaledOppRaiseFreq / (decimal)(activePlayerIds.Length-1));
//            infoStore.SetInformationValue(InfoType.AP_AvgScaledOppCallFreq_Double, avgScaledOppCallFreq / (decimal)(activePlayerIds.Length - 1));
//            infoStore.SetInformationValue(InfoType.AP_AvgScaledOppPreFlopPlayFreq_Double, avgScaledOppPreFlopPlayFreq / (decimal)(activePlayerIds.Length - 1));
//        }

//        //Continually updates the agression values for all tables in the list
//        private void providerUpdateThread()
//        {
//            long[] allPlayersIds;
//            long[] currentPlayerIds;
//            long[] newPlayerIds;
//            List<PlayerAggression> tempPlayerAggressionCache;

//            int totalPlayerHands = 0;

//            //Closes this thread when the AI is asked to close
//            while (!closeThread)
//            {
//                try
//                {
//                    DateTime startTime = DateTime.Now;

//                    //Get the temp version and keep working on that
//                    lock (locker)
//                        tempPlayerAggressionCache = (from current in playerAggressionCache
//                                                         select new PlayerAggression(current.PlayerId, current.MaxHands, current.HandsCounted, current.RFreq_PreFlop, current.RFreq_PostFlop, current.CFreq_PreFlop, current.CFreq_PostFlop, current.PlayFreqPreFlop)).ToList();

//                    //Get all players current in cacheTracker
//                    allPlayersIds = cacheTracker.allActivePlayers();

//                    //Make sure we are only calculating for players that still exist
//                    tempPlayerAggressionCache =
//                        (from temp in tempPlayerAggressionCache
//                         join all in allPlayersIds on temp.PlayerId equals all
//                         select temp).ToList();

//                    currentPlayerIds = (from current in tempPlayerAggressionCache select current.PlayerId).Distinct().ToArray();
//                    newPlayerIds = (allPlayersIds.Except(currentPlayerIds)).ToArray();

//                    //Go through the currentPlayerIds
//                    #region currentPlayers
//                    foreach (long playerId in currentPlayerIds)
//                    {
//                        totalPlayerHands = databaseQueries.NumHandsPlayed(playerId, true);

//                        var playerAggressionRows =
//                            from temp in tempPlayerAggressionCache
//                            where temp.PlayerId == playerId
//                            orderby temp.MaxHands ascending
//                            select temp;

//                        if (playerAggressionRows.Count() == 1)
//                        {
//                            playerAggressionRows.First().Update(recentHandLimit, startWeight);

//                            //If we now have enough to have a historical entry as well lets do that.
//                            if (totalPlayerHands > recentHandLimit)
//                                tempPlayerAggressionCache.Add(new PlayerAggression(playerId, historicalHandLimit, startWeight));
//                        }
//                        else if (playerAggressionRows.Count() == 2)
//                        {
//                            playerAggressionRows.ElementAt(0).Update(recentHandLimit, startWeight);
//                            playerAggressionRows.ElementAt(1).Update(historicalHandLimit, startWeight);
//                        }
//                        else
//                            throw new Exception("This should not be possible!");
//                    }
//                    #endregion

//                    #region newPlayers
//                    //Do the newPlayerIds
//                    foreach (long playerId in newPlayerIds)
//                    {
//                        totalPlayerHands = databaseQueries.NumHandsPlayed(playerId, true);

//                        if (totalPlayerHands > recentHandLimit)
//                        {
//                            tempPlayerAggressionCache.Add(new PlayerAggression(playerId, recentHandLimit, startWeight));
//                            tempPlayerAggressionCache.Add(new PlayerAggression(playerId, historicalHandLimit, startWeight));
//                        }
//                        else if (totalPlayerHands > 0)
//                        {
//                            tempPlayerAggressionCache.Add(new PlayerAggression(playerId, recentHandLimit, startWeight));
//                        }
//                    }
//                    #endregion

//                    //Now copy the cache back.
//                    lock (locker)
//                        playerAggressionCache = tempPlayerAggressionCache;

//                    WebLogging.AddLog("GPA", WebLogging.LogCategory.AP, "Update worker loop completed in " + (DateTime.Now - startTime).TotalSeconds.ToString("00") + " secs now containing " + (from current in tempPlayerAggressionCache select current.PlayerId).Distinct().Count().ToString() + " players.");

//                    Thread.Sleep(500);
//                }
//                catch (TimeoutException ex)
//                {
//                    WebLogging.AddLog("GPA", WebLogging.LogCategory.AP, "Aggression Provider Worker SP Timeout.");
//                }
//                catch (Exception ex)
//                {
//                    WebLogging.AddLog("GPA", WebLogging.LogCategory.AIError, "AI Aggression Worker Error Logged - " + ex.ToString());
//                    using (System.IO.StreamWriter sw = new System.IO.StreamWriter("pokerAIPAPError " + DateTime.Now.Hour.ToString() + "." + DateTime.Now.Minute.ToString() + "." + DateTime.Now.Second.ToString() + "." + DateTime.Now.Millisecond.ToString() + " " + DateTime.Now.ToString("dd-MM-yyyy") + ".log", false))
//                    {
//                        if (ex.GetBaseException() != null)
//                            sw.WriteLine("Base Exception Type: " + ex.GetBaseException().ToString());

//                        if (ex.InnerException != null)
//                            sw.WriteLine("Inner Exception Type: " + ex.InnerException.ToString());

//                        if (ex.StackTrace != null)
//                        {
//                            sw.WriteLine("");
//                            sw.WriteLine("Stack Trace: " + ex.StackTrace.ToString());
//                        }
//                    }
//                }
//            }
//        }
//    }
//}
