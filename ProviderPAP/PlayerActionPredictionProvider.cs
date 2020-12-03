using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using PokerBot.Definitions;
using PokerBot.Database;
using PokerBot.AI.InfoProviders;
using System.Threading;
using System.Diagnostics;
using Encog.Neural.Networks;
using System.Threading.Tasks;


#if logging
using log4net;
using log4net.Repository;
using log4net.Appender;
using System.Reflection;
using log4net.Layout;

using System.Security.Cryptography;
using System.Runtime.Serialization.Formatters.Binary;
using System.IO;
#endif

namespace PokerBot.AI.ProviderPAP
{

  public partial class PlayerActionPredictionProvider : InfoProviderBase
  {
    //The network manager should be static across all instance of player action prediction
    //We don't need two things starting networks!!
    static playerActionPredictionNetworkManager networkManager;

#if logging
        private static readonly ILog logger = LogManager.GetLogger(typeof(PlayerActionPredictionProvider));
        protected static object locker = new object();
        protected static string GenerateMD5(object data)
        {
            lock (locker)
            {
                BinaryFormatter bin = new BinaryFormatter();
                MemoryStream mem = new MemoryStream();
                MD5CryptoServiceProvider md5 = new MD5CryptoServiceProvider();

                bin.Serialize(mem, data);
                return BitConverter.ToString(md5.ComputeHash(mem.ToArray()));
            }
        }

#endif

    public PlayerActionPredictionProvider(InfoCollection information, Dictionary<InfoProviderType, InfoProviderBase> allInformationProviders, AIRandomControl aiRandomControl)
        : base(information, InfoProviderType.PlayerActionPrediction, allInformationProviders, aiRandomControl)
    {
      requiredInfoTypes = new List<InfoType>() { InfoType.CP_AKQToBoardRatio_Real,            //Same for all
                                            InfoType.CP_AOnBoard_Bool,                  //Same for all
                                            InfoType.CP_FlushPossible_Bool,             //Same for all
                                            InfoType.CP_KOnBoard_Bool,                  //Same for all
                                            InfoType.CP_StraightPossible_Bool,          //Same for all
                                            InfoType.GP_GameStage_Byte,                 //Same for all
                                            InfoType.GP_NumActivePlayers_Byte,              //Provided
                                            InfoType.GP_NumPlayersDealtIn_Byte,         //Same for all
                                            InfoType.GP_NumTableSeats_Byte,             //Same for all
                                            InfoType.GP_NumUnactedPlayers_Byte,             //Provided
                                            InfoType.BP_BetsToCall_Byte,                    //Provided
                                            InfoType.BP_LastRoundBetsToCall_Byte,               // ********* From CACHE
                                            InfoType.BP_MinimumPlayAmount_Decimal,          //Provided
                                            InfoType.BP_PlayerLastAction_Short,                 // ********* From CACHE
                                            InfoType.BP_PlayerMoneyInPot_Decimal,               // ********* From CACHE
                                            InfoType.BP_TotalNumCalls_Byte,                 //Provided
                                            InfoType.BP_TotalNumRaises_Byte,                //Provided
                                            InfoType.BP_TotalPotAmount_Decimal,             //Provided
                                            InfoType.BP_LastAdditionalRaiseAmount,
                                            InfoType.GP_DealerDistance_Byte,                     // ********* From CACHE
                                            InfoType.AP_AvgScaledOppPreFlopPlayFreq_Double     //We don't actually need this we just want to make sure the agression provider has been added
            };

      providedInfoTypes = new List<InfoPiece>() {
                                                        new InfoPiece(InfoType.PAP_RaiseToStealAmount_Amount, 0),
                                                        new InfoPiece(InfoType.PAP_RaiseToStealSuccess_Prob, 0),
                                                        new InfoPiece(InfoType.PAP_RaiseToCallAmount_Amount, 0),
                                                        new InfoPiece(InfoType.PAP_RaiseToBotCall_Prob, 1),
                                                        new InfoPiece(InfoType.PAP_FoldToBotCall_Prob, 0),
                                                        new InfoPiece(InfoType.PAP_RaiseToBotCheck_Prob, 1),
                                                        };
      AddProviderInformationTypes();

      //Create the player action prediction network manager if it's has not already been created.
      if (networkManager == null)
        networkManager = new playerActionPredictionNetworkManager();

#if logging
            //If we are logging configure the logger
            ILoggerRepository repository = LogManager.GetRepository(Assembly.GetCallingAssembly());
            IBasicRepositoryConfigurator configurableRepository = repository as IBasicRepositoryConfigurator;

            PatternLayout layout = new PatternLayout();
            layout.ConversionPattern = "%level% [%thread%] - %message%newline";
            layout.ActivateOptions();

            FileAppender appender = new FileAppender();
            appender.Layout = layout;
            appender.File = "aiDecisions_PAP.csv";
            appender.AppendToFile = true;
            appender.ActivateOptions();
            configurableRepository.Configure(appender);
#endif
    }

    public override void Close()
    {
      base.Close();

      networkManager.Close();
    }

    //The values writen to by each of the threads.
    protected decimal raiseToCallTheshold = 0.75m;
    protected decimal raiseToCallAmount1Players = 0;
    protected decimal raiseToCallAmount2Players = 0;

    protected decimal raiseToStealThreshold = 0.4m;
    protected decimal raiseToStealAmount = 0;
    protected decimal raiseToStealSuccessProb = 0;

    protected decimal raiseToBotCallProb = 0.5m;
    protected decimal foldToBotCallProb = 0;
    protected decimal raiseToBotCheckProb = 0;

    protected double[] randomNumbers;

    /// <summary>
    /// Get all of the necessary player action prediction information.
    /// </summary>
    protected override void updateInfo()
    {
      decimal gameStage = infoStore.GetInfoValue(InfoType.GP_GameStage_Byte);
      decimal raiseToCallAmount = 0;
      raiseToCallAmount1Players = 0;
      raiseToCallAmount2Players = 0;
      raiseToStealAmount = 0;
      raiseToStealSuccessProb = 0;
      foldToBotCallProb = 0;
      raiseToBotCallProb = 0.5m;
      raiseToBotCheckProb = 0;

      if (gameStage > 0)
      {
        BuildTempCache();
        BuildTempPlayerCache();

        //Generate the required random numbers
        randomNumbers = new double[2];
        for (int i = 0; i < randomNumbers.Length; i++)
          randomNumbers[i] = randomGen.NextDouble();

        //LOG4NET - Log TempCache, TempPlayerCache and RandomNumbers MD5
#if logging
                logger.Debug("PRE," + decisionRequest.Cache.getCurrentHandId() + ", " + decisionRequest.Cache.getMostRecentLocalIndex()[1] + "," + GenerateMD5(tempLocalHandCache) + "," + GenerateMD5(tempLocalPlayerCacheDict) + "," + GenerateMD5(randomNumbers));
#endif

        triggerPostFlopCalculations();
      }
      else
        triggerPreFlopCalculations();

      //Work out which raiseToCallToUse
      if (raiseToCallAmount1Players > raiseToCallAmount2Players * 2)
        raiseToCallAmount = raiseToCallAmount1Players;
      else
        raiseToCallAmount = raiseToCallAmount2Players;

      //If raiseToSteal has come out lower than raiseToCall then an error has likely happenned
      //In this particular case we should default to the lower of the two values
      if (raiseToStealAmount < raiseToCallAmount)
        raiseToCallAmount = raiseToStealAmount;

      infoStore.SetInformationValue(InfoType.PAP_RaiseToStealAmount_Amount, raiseToStealAmount);
      infoStore.SetInformationValue(InfoType.PAP_RaiseToStealSuccess_Prob, raiseToStealSuccessProb);
      infoStore.SetInformationValue(InfoType.PAP_RaiseToCallAmount_Amount, raiseToCallAmount);
      infoStore.SetInformationValue(InfoType.PAP_FoldToBotCall_Prob, foldToBotCallProb);
      infoStore.SetInformationValue(InfoType.PAP_RaiseToBotCall_Prob, raiseToBotCallProb);
      infoStore.SetInformationValue(InfoType.PAP_RaiseToBotCheck_Prob, raiseToBotCheckProb);

#if logging
            logger.Debug("POST," + decisionRequest.Cache.getCurrentHandId() + ", " + decisionRequest.Cache.getMostRecentLocalIndex()[1] + "," + raiseToStealAmount + "," + raiseToStealSuccessProb + "," + raiseToCallAmount + "," + foldToBotCallProb + "," + raiseToBotCallProb + "," + raiseToBotCheckProb);
#endif
    }

    /// <summary>
    /// Trigger the preflop player action predictions
    /// </summary>
    protected void triggerPreFlopCalculations()
    {

      ////////////////////////////////////////////////////////////
      ////////////// No selective updating can be done here as everything sort of depends on everything else
      ///////////////////////////////////////////////////////////

      //We need to decide a raiseToCallAmount here
      //For now we will randomly choose an amount between 3 and 4 times the big blind
      decimal bigBlind = decisionRequest.Cache.BigBlind;
      decimal littleBlind = decisionRequest.Cache.LittleBlind;
      decimal minimumCallAmount = infoStore.GetInfoValue(InfoType.BP_MinimumPlayAmount_Decimal);
      decimal playerBetAmountCurrentRound = infoStore.GetInfoValue(InfoType.BP_PlayerBetAmountCurrentRound_Decimal);
      decimal playerStack = decisionRequest.Cache.getPlayerStack(decisionRequest.PlayerId);
      double random = randomGen.NextDouble();

#if preFlopLogging || logging
            if (decisionRequest.Cache.getCurrentHandId() == 880)
            {
                using (System.IO.StreamWriter sw = new System.IO.StreamWriter("innerPAPLogging.csv", true))
                {
                    sw.WriteLine("\nStarting HandId:" + decisionRequest.Cache.getCurrentHandId() + ", SeqIndex:" + decisionRequest.Cache.getMostRecentLocalIndex()[1]);
                    sw.WriteLine("\tminimumCallAmount:" + minimumCallAmount + ", playerBetAmountCurrentRound:" + playerBetAmountCurrentRound + ", playerStack:" + playerStack + ", random:" + random);
                }
            }
#endif

      if (minimumCallAmount == bigBlind)
      {
        //Raise multipliers include 0.1 because there seems to be some rounding issue here
        if (random > 0.5)
          raiseToCallAmount1Players = 3.5m * bigBlind;
        else
          raiseToCallAmount1Players = 3 * bigBlind;
      }
      else
      {
        decimal currentRoundLastRaiseAmount = decisionRequest.Cache.getCurrentRoundLastRaiseAmount();
        if (random > 0.6666)
          raiseToCallAmount1Players = 2 * currentRoundLastRaiseAmount;
        else if (random > 0.3333)
          raiseToCallAmount1Players = 1.5m * currentRoundLastRaiseAmount;
        else
          raiseToCallAmount1Players = 1 * currentRoundLastRaiseAmount;

        raiseToCallAmount1Players = raiseToCallAmount1Players + playerBetAmountCurrentRound + minimumCallAmount;
      }

#if preFlopLogging || logging
            if (decisionRequest.Cache.getCurrentHandId() == 880)
            {
                using (System.IO.StreamWriter sw = new System.IO.StreamWriter("innerPAPLogging.csv", true))
                    sw.WriteLine("\raiseToCallAmount1Players:" + raiseToCallAmount1Players);
            }
#endif

      //Need to make raiseToCallAmount a big blind multiple
      decimal raiseToCallAmountBlindMultiple = raiseToCallAmount1Players / littleBlind;
      raiseToCallAmount1Players = Math.Round(raiseToCallAmountBlindMultiple, 0, MidpointRounding.AwayFromZero) * littleBlind;

#if preFlopLogging || logging
            if (decisionRequest.Cache.getCurrentHandId() == 880)
            {
                using (System.IO.StreamWriter sw = new System.IO.StreamWriter("innerPAPLogging.csv", true))
                    sw.WriteLine("\raiseToCallAmount1Players:" + raiseToCallAmount1Players);
            }
#endif

      if (raiseToCallAmount1Players > (playerStack + playerBetAmountCurrentRound) * 0.75m)
        raiseToCallAmount1Players = playerStack + playerBetAmountCurrentRound;

#if preFlopLogging || logging
            if (decisionRequest.Cache.getCurrentHandId() == 880)
            {
                using (System.IO.StreamWriter sw = new System.IO.StreamWriter("innerPAPLogging.csv", true))
                    sw.WriteLine("\raiseToCallAmount1Players:" + raiseToCallAmount1Players);
            }
#endif

      raiseToStealAmount = raiseToCallAmount1Players * 1.5m;
      raiseToStealSuccessProb = 0;

#if preFlopLogging || logging
            if (decisionRequest.Cache.getCurrentHandId() == 880)
            {
                using (System.IO.StreamWriter sw = new System.IO.StreamWriter("innerPAPLogging.csv", true))
                    sw.WriteLine("\raiseToStealAmount:" + raiseToStealAmount);
            }
#endif

      decimal raiseToStealAmountBlindMultitple = raiseToStealAmount / littleBlind;
      raiseToStealAmount = Math.Round(raiseToStealAmountBlindMultitple, 0, MidpointRounding.AwayFromZero) * littleBlind;

#if preFlopLogging || logging
            if (decisionRequest.Cache.getCurrentHandId() == 880)
            {
                using (System.IO.StreamWriter sw = new System.IO.StreamWriter("innerPAPLogging.csv", true))
                    sw.WriteLine("\raiseToStealAmount:" + raiseToStealAmount);
            }
#endif

      if (raiseToStealAmount > (playerStack + playerBetAmountCurrentRound))
        raiseToStealAmount = playerStack + playerBetAmountCurrentRound;

#if preFlopLogging || logging
            if (decisionRequest.Cache.getCurrentHandId() == 880)
            {
                using (System.IO.StreamWriter sw = new System.IO.StreamWriter("innerPAPLogging.csv", true))
                    sw.WriteLine("\raiseToStealAmount:" + raiseToStealAmount);
            }
#endif

    }

    /// <summary>
    /// Trigger the postflop player action predictions
    /// </summary>
    protected void triggerPostFlopCalculations()
    {
      if (ConcurrencyMode.Concurrency == ConcurrencyMode.ConcurencyModel.MultiCore)
      {
        //We can selectively update here
        List<Task> tasksToStart = new List<Task>();

        if (decisionRequest.RequiredInfoTypeUpdateKey.IsInfoTypeRequired(InfoType.PAP_RaiseToStealAmount_Amount) || decisionRequest.RequiredInfoTypeUpdateKey.IsInfoTypeRequired(InfoType.PAP_RaiseToStealSuccess_Prob))
          tasksToStart.Add(Task.Factory.StartNew(RaiseToStealWorker));

        if (decisionRequest.RequiredInfoTypeUpdateKey.IsInfoTypeRequired(InfoType.PAP_RaiseToCallAmount_Amount))
        {
          tasksToStart.Add(Task.Factory.StartNew(raiseToCallAmount1PlayersWorker));
          tasksToStart.Add(Task.Factory.StartNew(raiseToCallAmount2PlayersWorker));
        }

        if (decisionRequest.RequiredInfoTypeUpdateKey.IsInfoTypeRequired(InfoType.PAP_FoldToBotCall_Prob))
          tasksToStart.Add(Task.Factory.StartNew(probFoldToBotCallWorker));

        if (decisionRequest.RequiredInfoTypeUpdateKey.IsInfoTypeRequired(InfoType.PAP_RaiseToBotCall_Prob))
          tasksToStart.Add(Task.Factory.StartNew(probRaiseToBotCallWorker));

        if (decisionRequest.RequiredInfoTypeUpdateKey.IsInfoTypeRequired(InfoType.PAP_RaiseToBotCheck_Prob))
          tasksToStart.Add(Task.Factory.StartNew(probRaiseToBotCheckWorker));

        Task.WaitAll(tasksToStart.ToArray());
      }
      else
      {
        if (decisionRequest.RequiredInfoTypeUpdateKey.IsInfoTypeRequired(InfoType.PAP_RaiseToCallAmount_Amount) || decisionRequest.RequiredInfoTypeUpdateKey.IsInfoTypeRequired(InfoType.PAP_RaiseToStealAmount_Amount) || decisionRequest.RequiredInfoTypeUpdateKey.IsInfoTypeRequired(InfoType.PAP_RaiseToStealSuccess_Prob))
          RaiseToStealWorker();

        if (decisionRequest.RequiredInfoTypeUpdateKey.IsInfoTypeRequired(InfoType.PAP_RaiseToCallAmount_Amount))
        {
          raiseToCallAmount1PlayersWorker();
          raiseToCallAmount2PlayersWorker();
        }

        if (decisionRequest.RequiredInfoTypeUpdateKey.IsInfoTypeRequired(InfoType.PAP_FoldToBotCall_Prob))
          probFoldToBotCallWorker();

        if (decisionRequest.RequiredInfoTypeUpdateKey.IsInfoTypeRequired(InfoType.PAP_RaiseToBotCall_Prob))
          probRaiseToBotCallWorker();

        if (decisionRequest.RequiredInfoTypeUpdateKey.IsInfoTypeRequired(InfoType.PAP_RaiseToBotCheck_Prob))
          probRaiseToBotCheckWorker();
      }
    }

    protected static void LogPAPError(Exception ex, databaseCache cache)
    {
      //Save the cache for later checking and try again
      string fileName = LogError.Log(ex, "AIError_PAP");
      cache.SaveToDisk("", fileName);
    }
  }
}
