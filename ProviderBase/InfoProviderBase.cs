using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using PokerBot.Definitions;

namespace PokerBot.AI.InfoProviders
{
  public enum InfoProviderType
  {
    WinRatio,
    Bets,
    Cards,
    Game,
    ImpliedOdds,
    PlayerActionPrediction,
    AIAggression
  }

  public abstract class InfoProviderBase
  {
    public int PrerequesitesLeft = 5;
    public object PrerequesitesLocker = new object();

    protected InfoProviderType providerType;
    protected DecisionRequest decisionRequest;

    protected List<InfoPiece> providedInfoTypes; //The infotypes provided by this infotype        
    protected List<InfoProviderType> requiredInfoProviders; //The infoproviders required so that this provider operates
    protected Dictionary<InfoProviderType, InfoProviderBase> allInformationProviders;
    protected volatile bool closeThread = false;

    protected List<InfoType> requiredInfoTypes; //The infotypes required before this provider can output
    protected Dictionary<InfoType, List<InfoType>> requiredInfoTypesByInfoType = new Dictionary<InfoType, List<InfoType>>();
    public List<InfoType> RequiredInfoTypesByInfoType(InfoType type)
    {
      if (requiredInfoTypesByInfoType.ContainsKey(type))
        return requiredInfoTypesByInfoType[type];
      else if (requiredInfoTypes != null && requiredInfoTypes.Count != 0)
        return requiredInfoTypes;
      else
        return null;
    }

    /// <summary>
    /// Maintains active lists which providers may use for caching.
    /// </summary>
    //protected CacheTracker cacheTracker;
    protected InfoCollection infoStore;

    /// <summary>
    /// These values are used to control the repoduceable randomness, important for comparing generations
    /// </summary>
    protected AIRandomControl aiRandomControl;
    protected int providerInitialisationSequenceNum;
    protected Random randomGen;

    /// <summary>
    /// Used by the slowUpdateMethods
    /// </summary>
    protected static object slowUpdateLocker = new object();
    protected static Dictionary<InfoProviderType, Action> slowUpdateTaskDelegates = new Dictionary<InfoProviderType, Action>();
    protected static List<Task> slowUpdateTasksList;

    public InfoProviderBase(InfoCollection information, InfoProviderType providerType, Dictionary<InfoProviderType, InfoProviderBase> allInformationProviders, AIRandomControl aiRandomControl)
    {
      //this.disableTrueRandomness = disableTrueRandomness;
      this.aiRandomControl = aiRandomControl;

      if (allInformationProviders == null)
        this.providerInitialisationSequenceNum = 1;
      else
        this.providerInitialisationSequenceNum = allInformationProviders.Count + 1;

      if (aiRandomControl.InfoProviderRandomPerHandSeedEnabled)
        this.randomGen = new CMWCRandom(aiRandomControl.InfoProviderRandomPerHandSeed);
      else
        this.randomGen = new CMWCRandom(DateTime.Now.Ticks + providerInitialisationSequenceNum);

      this.providerType = providerType;
      this.allInformationProviders = allInformationProviders;

      if (this.allInformationProviders != null)
        this.allInformationProviders.Add(providerType, this);

      //We need to check that dependant providers have already been added
      //This code is here but it does not seem to be useable
      if (requiredInfoProviders != null)
      {
        int numRequiredProviders = requiredInfoProviders.Count;

        int requiredInfoProvidersPresent = (from
                                        availableProviders in allInformationProviders.Values
                                            join requiredProviders in requiredInfoProviders on availableProviders.ProviderType equals requiredProviders
                                            select availableProviders).Count();

        if (numRequiredProviders != requiredInfoProvidersPresent)
          throw new Exception("Required information providers for this provider are not yet present.");
      }

      if (information != null)
      {
        this.infoStore = information;
      }
      else if (requiredInfoTypes != null)
      {
        throw new Exception("InfoStore and GlobalRequestedInfo Types must both be provided or neither, not one or the other.");
      }

      //We need to check that the requiredInfoTypes are already in the infoStore
      if (requiredInfoTypes != null && information != null)
      {
        int numRequiredInfoTypes = requiredInfoTypes.Count;

        int requiredInfoTypesPresent = (from
                                    availableInformation in infoStore.GetInformationStore()
                                        join requiredTypes in requiredInfoTypes on availableInformation.InformationType equals requiredTypes
                                        select infoStore).Count();

        if (numRequiredInfoTypes != requiredInfoTypesPresent)
          throw new Exception("Required information types for this provider are not yet present in the information store.");
      }
      else if (requiredInfoTypes != null && information == null)
        throw new Exception("If requiredInfoTypes is not null an infoStore MUST be used!");

      //Setup the slow update function delegates
      lock (slowUpdateLocker)
      {
        if (!slowUpdateTaskDelegates.ContainsKey(providerType))
          slowUpdateTaskDelegates.Add(providerType, ProviderSlowUpdateTask);
      }
    }

    public virtual void ResetProvider()
    {
      throw new NotImplementedException();
    }

    public InfoProviderType ProviderType
    {
      get { return this.providerType; }
    }

    /// <summary>
    /// Add the informationTypes to information store which this provider uses
    /// </summary>
    public void AddProviderInformationTypes()
    {
      //We only want to add those types which match those in globalrequestedInfoTypes
      if (infoStore != null)
      {
        if (providedInfoTypes == null)
          throw new Exception("This info provider has not stated which infoTypes it provides!");

        var tmp = (from
                  availableTypes in providedInfoTypes
                   select availableTypes).ToList();

        infoStore.AddInformationTypes(tmp);
      }
    }

    /// <summary>
    /// Returns array of the types of information provided by this provider
    /// </summary>
    public InfoType[] GetProvidedInformationTypes()
    {
      var tmp = (from
          results in providedInfoTypes
                 select results.InformationType).ToArray();

      return tmp;
    }

    public void DoUpdateInfo(DecisionRequest decisionRequest)// long playerId, databaseCache genericGameCache, CacheTracker cacheTracker, RequestedInfoKey updateKey)
    {
      this.decisionRequest = decisionRequest;

      try
      {
        //Can set the random seed value here for all providers if necessary
        if (aiRandomControl.InfoProviderRandomPerHandSeedEnabled)
        {
          //var handDetails = decisionRequest.Cache.getCurrentHandDetails();
          //var holeCards = decisionRequest.Cache.getPlayerHoleCards(decisionRequest.PlayerId);

          var hashFunc = new Func<long, long>((long key) =>
          {
            key = (~key) + (key << 21); // key = (key << 21) - key - 1;
            key = key ^ (key >> 24);
            key = (key + (key << 3)) + (key << 8); // key * 265
            key = key ^ (key >> 14);
            key = (key + (key << 2)) + (key << 4); // key * 21
            key = key ^ (key >> 28);
            key = key + (key << 31);
            return key;
          });

          //long randomSeed = hashFunc(
          //        hashFunc(991 + providerInitialisationSequenceNum) ^
          //        hashFunc(decisionRequest.Cache.getNumHandsPlayed()) ^ 
          //        hashFunc((decisionRequest.Cache.getActivePositions().Length - decisionRequest.Cache.getActivePositionsLeftToAct().Length + 1)) ^ 
          //        hashFunc(handDetails.dealerPosition + 1) ^ 
          //        hashFunc((long)(100 *handDetails.potValue)) ^ 
          //        hashFunc((1L << holeCards.holeCard1) + (1L << holeCards.holeCard2) + (1L << handDetails.tableCard1) +
          //            (1L << handDetails.tableCard2) + (1L << handDetails.tableCard3) + (1L << handDetails.tableCard4) +
          //            (1L << handDetails.tableCard5)));

          //(randomGen as CMWCRandom).ReSeed(randomSeed);

          (randomGen as CMWCRandom).ReSeed((long)(hashFunc(991 + providerInitialisationSequenceNum) ^ hashFunc(decisionRequest.Cache.TableRandomNumber) ^ decisionRequest.Cache.CurrentHandRandomNumber() ^ hashFunc(1 + decisionRequest.Cache.getCurrentHandSeqIndex())));
        }

        updateInfo();
      }
      catch (Exception ex)
      {
        string fileName = LogError.Log(ex, "InfoProviderError");
        decisionRequest.Cache.SaveToDisk("", fileName);

        //If we are running in safe mode then we return defaults
        //If not we re throw the error
        if (decisionRequest.AIManagerInSafeMode)
          SetAllProvidedTypesToDefault((from current in providedInfoTypes select current.InformationType).ToList());
        else
          throw;
      }
    }

    public virtual void Close()
    {
      closeThread = true;
    }

    public void SetAllProvidedTypesToDefault(List<InfoType> infoTypesToSetToDefault)
    {
      for (int i = 0; i < infoTypesToSetToDefault.Count; i++)
      {
        if (infoStore.ContainsInfoType(infoTypesToSetToDefault[i]))
          infoStore.SetInformationValueToDefault(infoTypesToSetToDefault[i]);
      }
    }

    protected abstract void updateInfo();

    /// <summary>
    /// Starts all updateSlowTask methods as tasks
    /// </summary>
    public static void InitiateSlowTasks(List<InfoProviderType> providersToUpdate)
    {
      if (ConcurrencyMode.Concurrency == ConcurrencyMode.ConcurencyModel.MultiCore)
      {
        slowUpdateTasksList = new List<Task>();
        foreach (var updateTask in slowUpdateTaskDelegates)
        {
          if (providersToUpdate.Contains(updateTask.Key))
            slowUpdateTasksList.Add(Task.Factory.StartNew(updateTask.Value));
        }
      }
      else
      {
        foreach (var updateTask in slowUpdateTaskDelegates)
        {
          if (providersToUpdate.Contains(updateTask.Key))
            updateTask.Value();
        }
      }
    }

    public static void WaitForSlowTasksToComplete()
    {
      if (slowUpdateTasksList != null && ConcurrencyMode.Concurrency == ConcurrencyMode.ConcurencyModel.MultiCore)
        Task.WaitAll(slowUpdateTasksList.ToArray());
    }

    public static bool AllSlowUpdateTasksCompleted()
    {
      if (slowUpdateTasksList != null && ConcurrencyMode.Concurrency == ConcurrencyMode.ConcurencyModel.MultiCore)
      {
        foreach (var updateTask in slowUpdateTasksList)
        {
          if (!updateTask.IsCompleted)
            return false;
        }
      }

      return true;
    }

    public virtual void ProviderSlowUpdateTask()
    {
      return;
    }
  }
}
