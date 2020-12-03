using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

using PokerBot.Definitions;
using PokerBot.Database;
using System.Diagnostics;
using System.Windows.Forms;
using PokerBot.AI.InfoProviders;
using System.Threading;
using PokerBot.AI.Nerual.Version1;
using PokerBot.WebManagement;
using PokerBot.AI.Nerual.Version2;
using PokerBot.AI.Nerual.Version3;
using System.Collections;
using System.IO;

namespace PokerBot.AI
{
  /// <summary>
  /// Manages incoming decision requests.
  /// Responsible for:
  ///     Loading the correct AI (i.e. Simple, Neuralv1 etc.
  ///     Maintaining the cache tracker.
  ///     Maintaining a history of caches per table.
  ///     Returning decisions from the correct AI.
  /// </summary>
  public class AIManagerOld
  {
    int providersTimeOutMilliSeconds;

    protected CacheTracker cacheTracker;

    /// <summary>
    /// These three arrays can be used to multithread the AI manager.
    /// </summary>
    protected int numInstances;
    protected InfoCollection[] infoStoreCollection;
    protected List<InfoProviderBase>[] infoProviderCollection;
    internal List<AIBase>[] aiCollection;
    protected bool runInSafeMode;

    protected object[] instanceLocks;

    //Locker used to prevent multiple decisions colliding
    protected object idlelocker = new object();
    protected int instancesIdle;

    protected List<Control> aiServerGUIControls;

    protected Random randomGen = new Random();

    protected ManualResetEvent aiAvailableEvent = new ManualResetEvent(true);

    /// <summary>
    /// Initialise the AI Manager
    /// </summary>
    public AIManagerOld(int providersTimeOutMilliSeconds, int numInstances, bool disableRandomness, bool runInSafeMode)
    {
      this.numInstances = numInstances;
      cacheTracker = new CacheTracker(false);
      this.providersTimeOutMilliSeconds = providersTimeOutMilliSeconds;

      //Safe mode increases the amount of error checking on the decision
      this.runInSafeMode = runInSafeMode;

      //All instances will use the same required info types
      #region requiredInfoTypes

      List<InfoType> requiredInfoTypes = new List<InfoType>() {
                                                InfoType.WR_CardsOnlyWinPercentage,
                                                InfoType.WR_CardsOnlyWeightedPercentage,
                                                InfoType.WR_CardsOnlyOpponentWinPercentage,
                                                InfoType.WR_CardsOnlyWeightedOpponentWinPercentage,
                                                InfoType.WR_CardsOnlyWinPercentageLastRoundChange,
                                                InfoType.WR_CardsOnlyWinRatio,

                                                //Player Action Prediction Provider - PAP
                                                InfoType.PAP_RaiseToBotCheck_Prob,
                                                InfoType.PAP_RaiseToBotCall_Prob,
                                                InfoType.PAP_FoldToBotCall_Prob,
                                                InfoType.PAP_RaiseToCallAmount_Amount,
                                                InfoType.PAP_RaiseToStealSuccess_Prob,
                                                InfoType.PAP_RaiseToStealAmount_Amount,

                                                //Card Provider
                                                InfoType.CP_AOnBoard_Bool,
                                                InfoType.CP_KOnBoard_Bool,
                                                InfoType.CP_FlushPossible_Bool,
                                                InfoType.CP_StraightPossible_Bool,
                                                InfoType.CP_AKQToBoardRatio_Real,
                                                InfoType.CP_TableFlushDraw_Bool,
                                                InfoType.CP_TableStraightDraw_Bool,

                                                InfoType.CP_HoleCardsAAPair_Bool,
                                                InfoType.CP_HoleCardsKKPair_Bool,

                                                InfoType.CP_HoleCardsOtherHighPair_Bool,
                                                InfoType.CP_HoleCardsOtherLowPair_Bool,
                                                InfoType.CP_HoleCardsTroubleHand_Bool,
                                                InfoType.CP_HoleCardsOuterStraightDrawWithHC_Bool,
                                                InfoType.CP_HoleCardsInnerStraightDrawWithHC_Bool,
                                                InfoType.CP_HoleCards3KindOrBetterMadeWithHC_Bool,
                                                InfoType.CP_HoleCardsTopOrTwoPair_Bool,
                                                InfoType.CP_HoleCardsAOrKInHand_Bool,

                                                InfoType.CP_HoleCardsAK_Bool,
                                                InfoType.CP_HoleCardsMidConnector_Bool,
                                                InfoType.CP_HoleCardsLowConnector_Bool,
                                                InfoType.CP_HoleCardsSuited_Bool,
                                                InfoType.CP_HoleCardsFlushDraw_Bool,
                                                InfoType.CP_HoleCardsStraightDraw_Bool,

                                                //Bets Provider
                                                InfoType.BP_TotalPotAmount_Decimal,
                                                InfoType.BP_MinimumCallAmount_Decimal,
                                                InfoType.BP_BetsToCall_Byte,
                                                InfoType.BP_LastRoundBetsToCall_Byte,
                                                InfoType.BP_PlayerMoneyInPot_Decimal,
                                                InfoType.BP_TotalNumRaises_Byte,
                                                InfoType.BP_TotalNumCalls_Byte,
                                                InfoType.BP_TotalNumChecks_Byte,
                                                InfoType.BP_PlayerHandStartingStackAmount_Decimal,
                                                InfoType.BP_PlayerLastAction_Short,
                                                InfoType.BP_RaisedLastRound_Bool,
                                                InfoType.BP_CalledLastRound_Bool,
                                                InfoType.BP_PlayerBetAmountCurrentRound_Decimal,
                                                InfoType.BP_ImmediatePotOdds_Double,
                                                InfoType.BP_CurrentCallAmountLarger4BB,
                                                InfoType.BP_LastAdditionalRaiseAmount,

                                                //Game Provider
                                                InfoType.GP_NumTableSeats_Byte,
                                                InfoType.GP_NumPlayersDealtIn_Byte,
                                                InfoType.GP_NumActivePlayers_Byte,
                                                InfoType.GP_NumUnactedPlayers_Byte,
                                                InfoType.GP_GameStage_Byte,
                                                InfoType.GP_DealerDistance_Byte,

                                                //ImpliedOdds Provider
                                                InfoType.IO_ImpliedPotOdds_Double,

                                                //Aggression Provider
                                                InfoType.AP_AvgScaledOppRaiseFreq_Double,
                                                InfoType.AP_AvgScaledOppCallFreq_Double,
                                                InfoType.AP_AvgScaledOppPreFlopPlayFreq_Double
                                                };
      #endregion

      //Setup the infostore arrays
      infoStoreCollection = new InfoCollection[numInstances];
      infoProviderCollection = new List<InfoProviderBase>[numInstances];
      aiCollection = new List<AIBase>[numInstances];
      instanceLocks = new object[numInstances];

      for (int i = 0; i < numInstances; i++)
        instancesIdle = instancesIdle | (1 << i);

      //Setup the instances
      for (int i = 0; i < numInstances; i++)
      {
        infoStoreCollection[i] = new InfoCollection();
        infoProviderCollection[i] = new List<InfoProviderBase>();
        aiCollection[i] = new List<AIBase>();
        instanceLocks[i] = new object();

        //Setup the 1st infostore instance
        (new GameProvider(infoStoreCollection[i], requiredInfoTypes, infoProviderCollection[i], cacheTracker)).StartWorkerThread();
        (new WinRatioProvider(infoStoreCollection[i], requiredInfoTypes, infoProviderCollection[i], cacheTracker)).StartWorkerThread();
        (new BetsProvider(infoStoreCollection[i], requiredInfoTypes, infoProviderCollection[i], cacheTracker)).StartWorkerThread();
        (new CardsProvider(infoStoreCollection[i], requiredInfoTypes, infoProviderCollection[i], cacheTracker)).StartWorkerThread();
        (new AggressionProvider(infoStoreCollection[i], requiredInfoTypes, infoProviderCollection[i], cacheTracker)).StartWorkerThread();
        (new PlayerActionPredictionProvider(infoStoreCollection[i], requiredInfoTypes, infoProviderCollection[i], cacheTracker, disableRandomness)).StartWorkerThread();
        (new ImpliedOddsProvider(infoStoreCollection[i], requiredInfoTypes, infoProviderCollection[i], cacheTracker)).StartWorkerThread();

        //Hard lock the infostore collection preventing any further changes.
        infoStoreCollection[i].HardLockInfoList();

        //Setup the AI list
        aiCollection[i].Add(new SimpleAIV1());
        aiCollection[i].Add(new SimpleAIV2());
        aiCollection[i].Add(new NeuralAIv1());
        aiCollection[i].Add(new SimpleAIV1Adv());
        aiCollection[i].Add(new NeuralAIv2());
        aiCollection[i].Add(new SimpleAIV4AggTrack(disableRandomness));
        aiCollection[i].Add(new NeuralAIv3());
      }

    }

    /// <summary>C
    /// Shutdown the AI manger, all providers and AIs
    /// </summary>
    public void Shutdown()
    {
      //Shut down the AI's
      for (int i = 0; i < numInstances; i++)
        foreach (var provider in infoProviderCollection[i])
          provider.Close();
    }

    public void EndTablePlayer(long tableId, long playerId)
    {
      cacheTracker.removeTablePlayer(tableId, playerId);
    }

    /// <summary>
    /// Default GetDecision which gets the correct player config from the database.
    /// </summary>
    /// <param name="playerId"></param>
    /// <param name="genericGameCache"></param>
    /// <returns></returns>
    public Play GetDecision(long playerId, databaseCache genericGameCache)
    {
      return GetDecision(playerId, genericGameCache, AIGeneration.Undefined, null);
    }

    /// <summary>
    /// Initiaties a decision but overrides the player config using the specified serverType and configStr
    /// </summary>
    /// <param name="handId"></param>
    /// <param name="playerId"></param>
    /// <param name="genericGameCache"></param>
    /// <returns></returns>
    public Play GetDecision(long playerId, databaseCache genericGameCache, AIGeneration serverType, string aiConfigStr)
    {
      try
      {
        //AIGeneration serverType;
        //string aiConfigStr;
        int selectedInstanceIndex;

        //Check for any simple errors
        PreDecisionErrorChecking(playerId, genericGameCache);

        //Determine which instance to use
        selectedInstanceIndex = DetermineAvailableInstance();

        //Enter the correct lock
        lock (instanceLocks[selectedInstanceIndex])
        {
          //Update the infostore
          //If anything in the InfoStore throws any error it will be caught by the catch
          //in this method, and thus logged correctly.
          UpdateInfoStore(playerId, genericGameCache, infoStoreCollection[selectedInstanceIndex], infoProviderCollection[selectedInstanceIndex]);

          //Get the correct AI version and configuration for this player
          if (serverType == AIGeneration.Undefined || aiConfigStr == null)
            cacheTracker.playerAiConfig(playerId, genericGameCache.TableId, out serverType, out aiConfigStr);

          //Pick that aiServer from the available list
          var aiServer =
              from server in aiCollection[selectedInstanceIndex]
              where server.AiType == serverType
              select server;

          if (aiServer.Count() != 1)
            throw new Exception("Unable to select correct AI from those avaialble.");

          //Get the ai decision
          Play aiAction = aiServer.First().GetDecision(playerId, aiConfigStr, genericGameCache, infoStoreCollection[selectedInstanceIndex]);

          aiAction = SetDecisionTime(ValidatePlayerDecision(aiAction, genericGameCache));

          //Signal this decision as finished and let another one through.
          lock (idlelocker)
          {
            instancesIdle = instancesIdle | (1 << selectedInstanceIndex);
            //aiAvailableEvent.Set();
          }

          return aiAction;
        }

      }
      catch (Exception ex)
      {
        //Reset idle instances (the locks will protected and potential multithreading problems).
        instancesIdle = 0;
        for (int i = 0; i < numInstances; i++)
          instancesIdle = instancesIdle | (1 << i);

        genericGameCache.readLockCounter = 0;

        LogAIError(ex, genericGameCache);

        return new Play(PokerAction.GeneralAIError, 0, 0, genericGameCache.getCurrentHandId(), playerId, ex.ToString(), 3);
      }

      throw new Exception("Should never get here!");
    }

    #region Update InfoStore

    /// <summary>
    /// Trigger an update of all info providers in 1st instance
    /// </summary>
    /// <returns></returns>
    private void UpdateInfoStore(long playerId, databaseCache currentCache, InfoCollection localInfoStore, List<InfoProviderBase> localInfoProviders)
    {
      //Update the shared cache tracker for the current cache
      cacheTracker.Update(playerId, currentCache);

      //Reset all update flags to false
      localInfoStore.ResetAllUpdateFlags();

      foreach (var provider in localInfoProviders)
        provider.TriggerUpdateInfo(playerId, currentCache, cacheTracker);

      foreach (var provider in localInfoProviders)
        provider.UpdateFinished(providersTimeOutMilliSeconds);
    }

    public InfoCollection GetPrimaryInfoStore
    {
      get { return infoStoreCollection[0]; }
    }

    public List<InfoProviderBase> PrimaryInfoProviders
    {
      get { return infoProviderCollection[0]; }
    }

    #endregion

    private void PreDecisionErrorChecking(long playerId, databaseCache genericGameCache)
    {
      if (runInSafeMode)
      {
        #region Basic Error Checking
        if (!(genericGameCache.getCurrentHandId() > 0))
          throw new Exception("getCurrentHandId() returns an invalid value.");

        byte[] positionsLeftToAct = genericGameCache.getActivePositionsLeftToAct();
        if (positionsLeftToAct.Length == 0)
          throw new Exception("Positions left to act contains no elements.");

        if (positionsLeftToAct[0] != genericGameCache.getPlayerPosition(playerId))
          throw new Exception("First position left to act should always be the specified player - otherwise why are we trying to determine an action yet.");

        if (!genericGameCache.getPlayerDetails(playerId).isBot)
          throw new Exception("Decision requested for player who is not marked as a bot.");

        if (genericGameCache.TableId == 0)
          throw new Exception("Cache Table Id is 0 which cannot be correct!");
        #endregion
      }
    }

    private int DetermineAvailableInstance()
    {
      int selectedInstanceIndex;
      int availableInstances;
      int indexAvailableMask;

      do
      {
        //Find the first available instance.
        selectedInstanceIndex = -1;
        lock (idlelocker)
        {
          availableInstances = (instancesIdle & int.MaxValue);

          if (availableInstances > 0)
          {
            indexAvailableMask = 1;
            selectedInstanceIndex = 0;

            while (true)
            {
              if ((availableInstances & indexAvailableMask) > 0)
              {
                instancesIdle = instancesIdle ^ indexAvailableMask;
                break;
              }
              else if (selectedInstanceIndex > numInstances)
                throw new Exception("Loop error!");

              selectedInstanceIndex++;
              indexAvailableMask = indexAvailableMask << 1;
            }
          }

          //if (selectedInstanceIndex == -1)
          //    aiAvailableEvent.Reset();
        }

        //If we did not pick up an available instance we wait for one to signal it has finished
        //If we have not received a signal within 50ms the waitone times out and try to pick up a resource regardless.
        if (selectedInstanceIndex == -1)
          WaitForInstance();

      } while (selectedInstanceIndex == -1);

      return selectedInstanceIndex;
    }

    private void WaitForInstance()
    {
      //signalDecisionFinished.WaitOne(50);
      //aiAvailableEvent.WaitOne(1);
      Thread.Sleep(1);
    }

    /// <summary>
    /// Validates the AI action. 
    /// Prevents raises when only calls are possible.
    /// Prevents raising more than allowed.
    /// Prevent raising less than allowed.
    /// </summary>
    /// <param name="aiAction">The AI action to be validated.</param>
    /// <returns>The validated decision.</returns>
    public static Play ValidatePlayerDecision(Play oldAiAction, databaseCache cache)
    {
      //PlayerId passed through because "cache.getCurrentActiveTablePosition()" is a slow method.
      //long playerId = cache.getPlayerId(cache.getCurrentActiveTablePosition());

      long playerId = oldAiAction.PlayerId;
      decimal minCallAmount = cache.getMinimumPlayAmount();
      decimal currentRoundBetAmount = cache.getPlayerCurrentRoundBetAmount(playerId);
      decimal playerRemaningStack = cache.getPlayerStack(playerId);

      //if (oldAiAction.PlayerId != playerId)
      //    throw new Exception("Current active table position does not correspond with bot player position.");

      //Copy the action here so that we can still see the old one for debugging reasons.
      //Once we know this is stable we can get rid of this line.
      Play aiAction = new Play(oldAiAction.Serialise());

      //We need to make sure a raise is possible
      byte[] activePositions = cache.getActivePositions();
      byte numPlayersAllIn = (byte)cache.getAllInPositions().Length;

      //We need to ensure the bot is not calling dead, i.e. if the winPercentage is less than 5% we should not be calling after the river
      if (aiAction.Action == PokerAction.Fold)
      {
        //Check for the free check
        if (minCallAmount - currentRoundBetAmount == 0)
          aiAction.Action = PokerAction.Check;
      }
      else if (aiAction.Action == PokerAction.Call)
      {
        //We must call atleast the minimum amount
        if (minCallAmount - currentRoundBetAmount > aiAction.Amount)
          aiAction.Amount = minCallAmount;

        //We cannot call 0, it should be a check
        if (minCallAmount - currentRoundBetAmount == 0)
        {
          aiAction.Action = PokerAction.Check;
          aiAction.Amount = 0;
        }
        else if (minCallAmount - currentRoundBetAmount > playerRemaningStack)
        {
          //We cannot call more than the stack amount
          aiAction.Amount = playerRemaningStack;
        }

        //Never call a 0 amount!!!
        if (aiAction.Amount == 0 && aiAction.Action == PokerAction.Call)
          aiAction.Action = PokerAction.Check;
      }
      else if (aiAction.Action == PokerAction.Raise)
      {
        //If the raiseToAmount is less than the minimum allowable raise then raise the minimum.
        decimal lastAdditionalRaiseAmount = cache.getCurrentRoundLastRaiseAmount();
        decimal minimumRaiseToAmount = (minCallAmount - lastAdditionalRaiseAmount) + (lastAdditionalRaiseAmount * 2);

        if (aiAction.Amount < minimumRaiseToAmount)
          aiAction.Amount = minimumRaiseToAmount;

        //if (aiAction.Amount > (cache.getCurrentHandDetails().potValue * 2))
        //    aiAction.Amount = cache.getCurrentHandDetails().potValue * 2;

        //If the raiseToAmount is more than we are able to raise then raise the maximum amount possible.
        if (aiAction.Amount > currentRoundBetAmount + playerRemaningStack)
          aiAction.Amount = currentRoundBetAmount + playerRemaningStack;

        if (minCallAmount - currentRoundBetAmount >= playerRemaningStack)
        {
          //If we are trying to raise but we cannot even meet the minimum call then call stack
          aiAction.Action = PokerAction.Call;
          aiAction.Amount = playerRemaningStack;
        }
        else if (numPlayersAllIn + 1 == activePositions.Length)
        {
          //If everyone else is all in then calling is our only option
          aiAction.Action = PokerAction.Call;
          aiAction.Amount = minCallAmount - currentRoundBetAmount;

          //If we already have the right amount in the pot we can only check
          if (aiAction.Amount == 0)
            aiAction.Action = PokerAction.Check;
        }

      }

      //If we are still trying to call a 0 amounts somthing else has gone very wrong
      if (aiAction.Amount == 0 && aiAction.Action == PokerAction.Call)
        throw new Exception("Critical validation error - trying to call 0 amount.");

      /*
      if (oldAiAction.Amount != aiAction.Amount || oldAiAction.Action != aiAction.Action)
          throw new Exception("Validation Error");
      */

      return aiAction;
    }

    /// <summary>
    /// Set the decision time for this play if it is currently unset.
    /// </summary>
    /// <param name="aiAction"></param>
    /// <returns>The play with the new decision time set.</returns>
    protected Play SetDecisionTime(Play aiAction)
    {
      float decisionTimeSecs = 0;
      Random r = new Random();
      double random = r.NextDouble();

      if (aiAction.DecisionTime == 0)
      {

        if (aiAction.Action == PokerAction.Fold)
        {
          decisionTimeSecs = (float)((random * 1.5) + 1);
        }
        else if (aiAction.Action == PokerAction.Check)
        {
          decisionTimeSecs = (float)((random * 3) + 1);
        }
        else if (aiAction.Action == PokerAction.Call)
        {
          decisionTimeSecs = (float)((random * 4) + 3);
        }
        else if (aiAction.Action == PokerAction.Raise)
        {
          decisionTimeSecs = (float)((random * 5) + 3);
        }

        aiAction.DecisionTime = decisionTimeSecs;
      }

      return aiAction;
    }

    public static void LogAIError(Exception ex, databaseCache cache)
    {
      //Save the cache for later checking and try again
      WebLogging.AddLog("GPA", WebLogging.LogCategory.AIError, "AI error logged - " + ex.ToString());

      string errorFileName = "pokerAIError " + DateTime.Now.Hour.ToString() + "." + DateTime.Now.Minute.ToString() + "." + DateTime.Now.Second.ToString() + "." + DateTime.Now.Millisecond.ToString() + " " + DateTime.Now.ToString("dd-MM-yyyy");

      using (System.IO.StreamWriter sw = new System.IO.StreamWriter(errorFileName + ".log", false))
      {
        sw.WriteLine("Hand Id: " + cache.getCurrentHandId().ToString() + " Player Id: " + cache.getPlayerId(cache.getCurrentActiveTablePosition()).ToString());

        if (ex.GetBaseException() != null)
          sw.WriteLine("Base Exception Type: " + ex.GetBaseException().ToString());

        if (ex.InnerException != null)
          sw.WriteLine("Inner Exception Type: " + ex.InnerException.ToString());

        if (ex.StackTrace != null)
        {
          sw.WriteLine("");
          sw.WriteLine("Stack Trace: " + ex.StackTrace.ToString());
        }

        File.WriteAllBytes(errorFileName + ".FBPcache", cache.Serialise());

        sw.Close();
      }
    }
  }
}
