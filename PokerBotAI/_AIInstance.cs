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
using PokerBot.AI.Nerual.Version2;
using PokerBot.AI.Nerual.Version3;
using System.Collections;
using PokerBot.AI.Nerual.Version4;
using PokerBot.AI.ProviderPAP;
using System.Threading.Tasks;
using ProviderAggression;
using PokerBot.AI.Neural.Version6;
using PokerBot.AI.Neural.Version7;

namespace PokerBot.AI
{
  public partial class AIManager
  {
    private class AIInstance
    {
      public class InfoProviderTaskTree
      {
        RequestedInfoKey keyToMatch;
        RequestedInfoKey actualKey;
        public RequestedInfoKey ActualKey { get { return actualKey; } }

        public Dictionary<InfoProviderType, InfoProviderBase> allProviders;
        public Dictionary<InfoProviderType, List<InfoProviderBase>> dependents;
        public Dictionary<InfoProviderType, int> numberPrerequisites;

        public InfoProviderTaskTree(RequestedInfoKey keyToCreate, Dictionary<InfoProviderType, InfoProviderBase> allAvailableProviders)
        {
          keyToMatch = keyToCreate;
          actualKey = keyToMatch;
          allProviders = new Dictionary<InfoProviderType, InfoProviderBase>();
          dependents = new Dictionary<InfoProviderType, List<InfoProviderBase>>();
          numberPrerequisites = new Dictionary<InfoProviderType, int>();

          //First we need to make sure all types that are required to compute types that we want are going to be computed
          for (int i = 0; i < 256; i++)
          {
            OuterLoopBegin: //Marker so we can restart loop easily

            //Make sure info type is actually defined 
            if (Enum.IsDefined(typeof(InfoType), i))
            {
              InfoType type = (InfoType)i;

              //if the key says we need this value
              if (actualKey.IsInfoTypeRequired(type))
              {
                //Go through all providers looking for the one that provides this type
                foreach (var outerProvider in allAvailableProviders.Values)
                {
                  //does provider set this type
                  if ((from types in outerProvider.GetProvidedInformationTypes()
                       where types == type
                       select types).Count() == 1)
                  {
                    //if provider is not currently being used we better make it so
                    if (!allProviders.ContainsKey(outerProvider.ProviderType))
                    {
                      allProviders.Add(outerProvider.ProviderType, outerProvider);
                      numberPrerequisites.Add(outerProvider.ProviderType, 0);
                    }

                    List<InfoType> requiredTypes = outerProvider.RequiredInfoTypesByInfoType(type);

                    //does provider require any other types to provide this type
                    if (requiredTypes != null && requiredTypes.Count != 0)
                    {
                      //if types are required by this provider we to loop through finding who provides them
                      foreach (var innerProvider in allAvailableProviders.Values)
                      {
                        //checks if inner provider provides a type that is required by outer provider
                        if ((from provided in innerProvider.GetProvidedInformationTypes()
                             where requiredTypes.Contains(provided)
                             select provided).Count() != 0)
                        {
                          //if inner provider currently has no dependents add it to list so it can
                          if (!dependents.ContainsKey(innerProvider.ProviderType))
                            dependents.Add(innerProvider.ProviderType, new List<InfoProviderBase>());

                          //if inner provider alrerady doesn't have outer provider as a dependent make it so
                          if (!dependents[innerProvider.ProviderType].Contains(outerProvider))
                          {
                            dependents[innerProvider.ProviderType].Add(outerProvider);
                            numberPrerequisites[outerProvider.ProviderType]++;
                          }
                        }
                      }

                      //initiate bool to mark if we add any new types through dependencies
                      bool addedNewType = false;

                      //go through required types
                      foreach (var element in requiredTypes)
                      {
                        //if type isn't required already make it so and mark that we've added new types
                        if (!actualKey.IsInfoTypeRequired(element))
                        {
                          actualKey.SetInfoTypeRequired(element);
                          addedNewType = true;
                        }
                      }

                      //if we added new types need to reset loop
                      if (addedNewType)
                      {
                        i = 0;
                        goto OuterLoopBegin;
                      }
                    }
                    i++;
                    goto OuterLoopBegin;
                  }
                }

                throw new Exception("Required type does not seem to be provided by any known provider");
              }
            }
          }
        }
      }

      Dictionary<RequestedInfoKey, InfoProviderTaskTree> taskTrees;

      InfoCollection infoStore;

      Dictionary<AIGeneration, AIBase> aiDictList;
      Dictionary<InfoProviderType, InfoProviderBase> infoProviderDictList;

      Queue decisionRequestQueue;

      InstanceSelector instanceSelector;
      int instanceNumber;

      object decisionInProgress;

      DecisionRequest currentDecision;
      InfoProviderTaskTree selectedTaskTree;
      Dictionary<InfoProviderType, Task> providerTasks;
      Dictionary<InfoProviderType, Action> providerActions;
      object locker = new object();

      public AIInstance(InstanceSelector instanceSelector, int instanceNumber, Queue decisionRequestQueue, AIRandomControl aiRandomControl)
      {
        taskTrees = new Dictionary<RequestedInfoKey, InfoProviderTaskTree>();

        this.instanceSelector = instanceSelector;
        this.instanceNumber = instanceNumber;
        this.decisionRequestQueue = decisionRequestQueue;
        this.decisionInProgress = new object();

        //Setup the AI's and providers here
        infoStore = new InfoCollection();

        infoProviderDictList = new Dictionary<InfoProviderType, InfoProviderBase>();
        aiDictList = new Dictionary<AIGeneration, AIBase>();

        //Setup the 1st infostore instance
        var GP = new GameProvider(infoStore, infoProviderDictList, aiRandomControl);
        var WRP = new ProviderWinRatio.WinRatioProvider(infoStore, infoProviderDictList, aiRandomControl);
        var BP = new BetsProvider(infoStore, infoProviderDictList, aiRandomControl);
        var CP = new CardsProvider(infoStore, infoProviderDictList, aiRandomControl);
        var AP = new AggressionProvider(infoStore, infoProviderDictList, aiRandomControl);
        var PAPP = new PlayerActionPredictionProvider(infoStore, infoProviderDictList, aiRandomControl);

        //Hard lock the infostore collection preventing any further changes.
        infoStore.HardLockInfoList();

        //Setup the AI list
        //We are going to lock the AI's into random mode.
        aiDictList.Add(AIGeneration.SimpleV1, new SimpleAIV1(aiRandomControl));
        aiDictList.Add(AIGeneration.SimpleV2, new SimpleAIV2(aiRandomControl));
        aiDictList.Add(AIGeneration.NeuralV1, new NeuralAIv1(aiRandomControl));
        aiDictList.Add(AIGeneration.SimpleV3, new SimpleAIV3(aiRandomControl));
        aiDictList.Add(AIGeneration.NeuralV2, new NeuralAIv2(aiRandomControl));
        aiDictList.Add(AIGeneration.SimpleV4AggressionTrack, new SimpleAIV4AggTrack(aiRandomControl));
        aiDictList.Add(AIGeneration.NeuralV3, new NeuralAIv3(aiRandomControl));
        aiDictList.Add(AIGeneration.NeuralV4, new NeuralAIv4(aiRandomControl));
        aiDictList.Add(AIGeneration.SimpleV5, new SimpleAIv5(aiRandomControl));
        aiDictList.Add(AIGeneration.SimpleV6, new SimpleAIv6(aiRandomControl));
        aiDictList.Add(AIGeneration.NeuralV5, new NeuralAIv5(aiRandomControl));
        aiDictList.Add(AIGeneration.NeuralV6, new NeuralAIv6(aiRandomControl));
        aiDictList.Add(AIGeneration.SimpleV7, new SimpleAIv7(aiRandomControl));
        aiDictList.Add(AIGeneration.CheatV1, new CheatV1(aiRandomControl));
        aiDictList.Add(AIGeneration.NeuralV7, new NeuralAIv7(aiRandomControl));

        if (ConcurrencyMode.Concurrency == ConcurrencyMode.ConcurencyModel.MultiCore)
          providerTasks = new Dictionary<InfoProviderType, Task>();
        else
          providerActions = new Dictionary<InfoProviderType, Action>();
      }

      public void HandleDecisionRequest(object decisionRequestObject)
      {
        currentDecision = decisionRequestObject as DecisionRequest;

        //Pick that aiServer from the available list
        //Multiple threads can be accessing this list as it is only returning an object
        //If the AIserver is not found we catch in the below exception and all hell breaks loose with errors!!

        if (AIManager.RunInSafeMode)
          if (!aiDictList.ContainsKey(currentDecision.ServerType))
            throw new Exception("Required AI not added to AI list.");

        //This gives the AI the starting config information as well as sets the first level 
        aiDictList[currentDecision.ServerType].PrepareAIForDecision(currentDecision);

        //It's possible we have set the decision already in PrepareAIForDecision()
        if (currentDecision.DecisionPlay == null)
        {
          if (!taskTrees.ContainsKey(currentDecision.RequiredInfoTypeUpdateKey))
            taskTrees.Add(currentDecision.RequiredInfoTypeUpdateKey, new InfoProviderTaskTree(currentDecision.RequiredInfoTypeUpdateKey, infoProviderDictList));

          selectedTaskTree = taskTrees[currentDecision.RequiredInfoTypeUpdateKey];
          currentDecision.RequiredInfoTypeUpdateKey = selectedTaskTree.ActualKey;

          UpdateProviders();

          //In the future when we move to decision levels this area of the AIInstance is going to change to 
          //Get required info types
          //Update Providers
          //Get decision
          //If decision is noAction go to the next step
          //Get required info types
          //Update providers
          //Get decision etc etc etc
          Play aiAction = aiDictList[currentDecision.ServerType].GetDecision(currentDecision, infoStore);
          currentDecision.SetDecision(aiAction);
        }

        lock (instanceSelector.locker)
        {
          lock (decisionRequestQueue.SyncRoot)
          {
            if (decisionRequestQueue.Count == 0)
            {
              instanceSelector.instancesIdle = instanceSelector.instancesIdle | (1 << instanceNumber);
              return;
            }
            else
            {
              this.currentDecision = decisionRequestQueue.Dequeue() as DecisionRequest;
              Task.Factory.StartNew(HandleDecisionRequest, currentDecision);
              return;
            }
          }
        }
      }

      public void UpdateProviders()
      {
        //Before we start the update we need to make sure to reset all update flags
        infoStore.ResetAllUpdateFlags();

        var allProviders = selectedTaskTree.allProviders;
        var numberPrerequisites = selectedTaskTree.numberPrerequisites;
        var dependents = selectedTaskTree.dependents;

        //Go through the providers and set their required number of prerequisites to that for this tree
        foreach (var provider in allProviders)
          provider.Value.PrerequesitesLeft = numberPrerequisites[provider.Key];

        //Loop through all providers and add them to the providerTask dictionary
        foreach (var provider in allProviders)
        {
          object provObj = provider.Value as object;

          if (ConcurrencyMode.Concurrency == ConcurrencyMode.ConcurencyModel.MultiCore)
          {
            Task p = new Task(TaskMethod, provObj);

            if (!providerTasks.ContainsKey(provider.Key))
              providerTasks.Add(provider.Key, p);
            else
              providerTasks[provider.Key] = p;
          }
          else
          {
            Action p = new Action(() => { TaskMethod(provObj); });

            if (!providerActions.ContainsKey(provider.Key))
              providerActions.Add(provider.Key, p);
            else
              providerActions[provider.Key] = p;
          }
        }

        //Start the providers which have no prerequisites
        foreach (var provider in allProviders)
        {
          if (ConcurrencyMode.Concurrency == ConcurrencyMode.ConcurencyModel.MultiCore)
          {
            if (numberPrerequisites[provider.Key] == 0)
              providerTasks[provider.Key].Start();
          }
          else
          {
            if (numberPrerequisites[provider.Key] == 0)
              providerActions[provider.Key]();
          }
        }

        //Wait for ALL providers to finish
        //We may not have started them all here but they are started as other providers finish
        //Once ALL providers have finished this method can return
        if (ConcurrencyMode.Concurrency == ConcurrencyMode.ConcurencyModel.MultiCore)
          Task.WaitAll(providerTasks.Values.ToArray());
      }

      void TaskMethod(object toRun)
      {
        InfoProviderBase provider = toRun as InfoProviderBase;

        provider.DoUpdateInfo(currentDecision);//currentDecision.PlayerId, currentDecision.Cache, cacheTracker, selectedTaskTree.ActualKey);

        if (selectedTaskTree.dependents.ContainsKey(provider.ProviderType))
        {
          foreach (var dependent in selectedTaskTree.dependents[provider.ProviderType])
          {
            lock (dependent.PrerequesitesLocker)
            {
              if (dependent.PrerequesitesLeft > 0)
              {
                dependent.PrerequesitesLeft--;
                if (dependent.PrerequesitesLeft == 0)
                {
                  if (ConcurrencyMode.Concurrency == ConcurrencyMode.ConcurencyModel.MultiCore)
                    providerTasks[dependent.ProviderType].Start();
                  else
                    providerActions[dependent.ProviderType]();
                }
              }
            }
          }
        }
      }

      public void ShutdownInstance()
      {
        //Now close all of the providers
        foreach (var provider in infoProviderDictList.Values)
          provider.Close();
      }

      public Dictionary<InfoType, InfoPiece> GetInfoStoreValues()
      {
        return infoStore.GetInformationStore();
      }

      public void ResetInfoProvider(List<InfoProviderType> infoProvidersToReset)
      {
        foreach (InfoProviderType providerType in infoProvidersToReset)
        {
          var provider = (from current in infoProviderDictList.Values
                          where current.ProviderType == providerType
                          select current).First();

          provider.ResetProvider();
        }
      }
    }
  }
}
