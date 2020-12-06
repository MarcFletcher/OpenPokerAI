using System;
using System.Collections;
using System.Collections.Generic;
using System.Threading.Tasks;
using PokerBot.AI.InfoProviders;
using PokerBot.AI.Neural;
using PokerBot.AI.ProviderPAP;
using PokerBot.Database;
using PokerBot.Definitions;

namespace PokerBot.AI
{
  public partial class AIManager
  {
    /// <summary>
    /// Used to remove superflous checking if integrity is guaranteed.
    /// If not running in safe mode providers will also die when errors are thrown instead of just returning defaults
    /// </summary>
    private static volatile bool runInSafeMode;

    public static bool RunInSafeMode
    {
      get { return runInSafeMode; }
      private set { runInSafeMode = value; }
    }

    protected Queue decisionRequestQueue; //Maintains the queue of decision requests

    private List<AIInstance> aiInstanceList;

    protected InstanceSelector instanceSelector;
    protected class InstanceSelector
    {
      public object locker = new object();
      public int instancesIdle;
    }

    public AIManager(int providersTimeOutMilliSeconds, int numInstances, AIRandomControl aiRandomControl, bool runInSafeMode)
    {
      AIManagerConstructor(numInstances, aiRandomControl, runInSafeMode, null);
    }

    private void AIManagerConstructor(int numInstances, AIRandomControl aiRandomControl, bool runInSafeMode, Object jobToRun)
    {
      RunInSafeMode = runInSafeMode;

      InfoProviderBase.CurrentJob = jobToRun;
      this.decisionRequestQueue = new Queue();

      instanceSelector = new InstanceSelector();
      aiInstanceList = new List<AIInstance>();

      //Setup the instances
      for (int i = 0; i < numInstances; i++)
        aiInstanceList.Add(new AIInstance(instanceSelector, i, decisionRequestQueue, aiRandomControl));

      for (int i = 0; i < numInstances; i++)
        instanceSelector.instancesIdle = instanceSelector.instancesIdle | (1 << i);
    }

    public void ResetCacheTrackerAndAINetworks()
    {
      CacheTracker.Instance.ResetCacheTracker();
      NeuralAIBase.ResetThreadSafeNetworkDict();
    }

    /// <summary>
    /// Using this to disable new networking training in PAP
    /// </summary>
    public void DisableAnnoyingThings()
    {
      playerActionPredictionNetworkManager.DisableNewNetworkTraining();
    }

    #region ProviderSlowUpdateTask
    /// <summary>
    /// Creates a task that will start all infoProvider slow update queues
    /// Method returns as soon as the task is started, i.e. not necessary once it's completed
    /// Use WaitForSlowProviderUpdateToComplete()
    /// </summary>
    public void BeginSlowProviderUpdate(List<InfoProviderType> providersToUpdate)
    {
      InfoProviderBase.InitiateSlowTasks(providersToUpdate);
    }

    /// <summary>
    /// Can be used to determine if the slow provider update has completed.
    /// </summary>
    /// <returns></returns>
    public bool AllSlowUpdateTasksCompleted()
    {
      return InfoProviderBase.AllSlowUpdateTasksCompleted();
    }

    /// <summary>
    /// If the slowupdate task is running this will return once it is complete.
    /// If the task is not running this will return immediately.
    /// </summary>
    public void WaitForSlowProviderUpdateToComplete()
    {
      InfoProviderBase.WaitForSlowTasksToComplete();
    }
    #endregion

    public void Shutdown()
    {
      CacheTracker.Instance.Shutdown();

      foreach (var instance in aiInstanceList)
        instance.ShutdownInstance();
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
        //Sort out the cacheTracker
        CacheTracker.Instance.Update(playerId, genericGameCache);
        if (serverType == AIGeneration.Undefined || aiConfigStr == null)
          CacheTracker.Instance.PlayerAiConfig(playerId, genericGameCache.TableId, out serverType, out aiConfigStr);

        DecisionRequest newRequest = new DecisionRequest(playerId, genericGameCache, serverType, aiConfigStr, AIManager.RunInSafeMode);
        PreDecisionErrorChecking(newRequest.PlayerId, newRequest.Cache);

        //For now we will just use the old method
        //Determine an available AIInstance
        int selectedInstanceIndex = DetermineAvailableInstance(newRequest);

        //This could be done in a seperate thread
        if (selectedInstanceIndex == -1)
          //There were no available instances so we join the queue and wait
          newRequest.WaitForDecision();
        else
        {
          if (ConcurrencyMode.Concurrency == ConcurrencyMode.ConcurencyModel.MultiCore)
          {
            Task t = Task.Factory.StartNew(aiInstanceList[selectedInstanceIndex].HandleDecisionRequest, newRequest);
            t.Wait();
          }
          else
          {
            aiInstanceList[selectedInstanceIndex].HandleDecisionRequest(newRequest);
          }
        }

        Play aiAction = newRequest.DecisionPlay;
        aiAction = SetDecisionTime(ValidatePlayerDecision(aiAction, newRequest.Cache));

        return aiAction;
      }
      catch (Exception ex)
      {
        instanceSelector.instancesIdle = 0;

        for (int i = 0; i < aiInstanceList.Count; i++)
          instanceSelector.instancesIdle = instanceSelector.instancesIdle | (1 << i);

        genericGameCache.readLockCounter = 0;

        string fileName = LogError.Log(ex, "PokerAIError");
        genericGameCache.SaveToDisk("", fileName);

        return new Play(PokerAction.GeneralAIError, 0, 0, genericGameCache.getCurrentHandId(), playerId, ex.ToString(), 3);
      }

      throw new Exception("This point should never be reached.");
    }

    private int DetermineAvailableInstance(DecisionRequest newRequest)
    {
      int selectedInstanceIndex;
      int availableInstances;
      int indexAvailableMask;

      selectedInstanceIndex = -1;
      lock (instanceSelector.locker)
      {
        availableInstances = instanceSelector.instancesIdle & int.MaxValue;

        if (availableInstances > 0)
        {
          indexAvailableMask = 1;
          selectedInstanceIndex = 0;

          while (true)
          {
            if ((availableInstances & indexAvailableMask) > 0)
            {
              instanceSelector.instancesIdle = instanceSelector.instancesIdle ^ indexAvailableMask;
              break;
            }
            else if (selectedInstanceIndex > aiInstanceList.Count)
              throw new Exception("Loop error!");

            selectedInstanceIndex++;
            indexAvailableMask = indexAvailableMask << 1;
          }
        }
        else
        {
          lock (decisionRequestQueue.SyncRoot)
            decisionRequestQueue.Enqueue(newRequest);
        }
      }

      return selectedInstanceIndex;
    }

    public Dictionary<InfoType, InfoPiece> GetInfoStoreValues()
    {
      if (aiInstanceList.Count != 1)
        throw new Exception("This method can only be used if there is a single aiInstance.");

      return aiInstanceList[0].GetInfoStoreValues();
    }

    public void ResetInfoProviders(List<InfoProviderType> infoProvidersToReset)
    {
      foreach (var instance in aiInstanceList)
        instance.ResetInfoProvider(infoProvidersToReset);
    }

    #region Static Checking Methods

    /// <summary>
    /// Validates that the cache is in the correct state to accept a decision from provided playerId.
    /// Disabled if runInSafeMode = false
    /// </summary>
    /// <param name="playerId"></param>
    /// <param name="genericGameCache"></param>
    private static void PreDecisionErrorChecking(long playerId, databaseCache genericGameCache)
    {
      if (AIManager.RunInSafeMode)
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
          throw new Exception("Decision requested for playerId " + playerId + " who is not marked as a bot.");

        if (genericGameCache.TableId == 0)
          throw new Exception("Cache Table Id is 0 which cannot be correct!");

        if (genericGameCache.getPlayerHoleCards(playerId).holeCard1 == 0 || genericGameCache.getPlayerHoleCards(playerId).holeCard1 == 0)
          throw new Exception("Holecards don't exist for the specified player.");
        #endregion
      }
    }

    /// <summary>
    /// Set the decision time for this play if it is currently unset.
    /// </summary>
    /// <param name="aiAction"></param>
    /// <returns>The play with the new decision time set.</returns>
    private static Play SetDecisionTime(Play aiAction)
    {
      float decisionTimeSecs = 0;
      Random r = new Random();
      double random = r.NextDouble();

      if (aiAction.DecisionTime == 0)
      {
        if (aiAction.Action == PokerAction.Fold)
        {
          decisionTimeSecs = (float)((random * 1.5) + 3);
        }
        else if (aiAction.Action == PokerAction.Check)
        {
          decisionTimeSecs = (float)((random * 1.5) + 3);
        }
        else if (aiAction.Action == PokerAction.Call)
        {
          decisionTimeSecs = (float)((random * 2) + 3);
        }
        else if (aiAction.Action == PokerAction.Raise)
        {
          decisionTimeSecs = (float)((random * 2) + 3);
        }

        aiAction.DecisionTime = decisionTimeSecs;
      }

      return aiAction;
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
      //Play aiAction = new Play(oldAiAction.Serialise());
      Play aiAction = oldAiAction;

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
        else if (aiAction.Amount == 0)
          aiAction.Amount = cache.BigBlind;

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

    #endregion Checking
  }
}
