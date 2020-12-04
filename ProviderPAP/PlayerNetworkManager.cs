using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Encog.Neural.Networks;
using Encog.Util;
using System.Threading;
using System.IO;
using PokerBot.Database;
using Encog;
using PokerBot.AI.InfoProviders;
using PokerBot.Definitions;

namespace PokerBot.AI.ProviderPAP
{
  /// <summary>
  /// Handles all player action prediction networks
  /// </summary>
  public class playerActionPredictionNetworkManager
  {
    object locker = new object();

    protected static string FILES_STORE = Environment.GetEnvironmentVariable("PlayerActionPredictionDir");
    protected static string PAP_STORE = Path.Combine(FILES_STORE, "PAPv1\\");
    protected volatile bool closeThread = false;

    protected static int minActionsRequiredForNetwork = 100;
    protected static int maxTrainingActions = 400;

    //CacheTracker cacheTracker;
    //BasicNetwork defaultNetwork;
    NNThreadSafeNetworkPool defaultNetworkPool;
    decimal defaultNetworkAccuracy = 70.0m;

    Thread backgroundWorkerThread;

    Dictionary<long, PAPNetworkContainer> playerNetworksDict = new Dictionary<long, PAPNetworkContainer>();

    protected volatile static bool disableNewNetworkTraining = false;

    /// <summary>
    /// Updated to only store a flat network
    /// </summary>
    [Serializable]
    protected class PAPNetworkContainer : ICloneable
    {
      long playerId;

      [NonSerialized]
      //BasicNetwork playerNetwork;
      NNThreadSafeNetworkPool playerNetworkPool;

      decimal networkAccuracy;
      int numActionsTrained;
      long lastUpdatedHandId;

      DateTime lastUpdatedTime;

      public static PAPNetworkContainer LoadPAPContainer(string pathName, string fileName)
      {
        throw new NotImplementedException();

        //Load the container
        PAPNetworkContainer returnItem = (PAPNetworkContainer)SerializeObject.Load(pathName + fileName + ".PAPc");

        //Load the network in as well
        returnItem.playerNetworkPool = new NNThreadSafeNetworkPool(NNLoadSave.loadNetwork(fileName + ".eNN", pathName), returnItem.playerId.ToString(), NNThreadSafeNetworkPool.DefaultListLength);

        return returnItem;
      }

      public void SavePAPContainer(string pathName, string fileName)
      {
        if (playerNetworkPool == null)
          throw new Exception("Network must be set before the PAP container can be saved out.");

        //Save out the PAPContainer (without the network)
        SerializeObject.Save(pathName + fileName + ".PAPc", this);

        //Save out the network
        NNLoadSave.saveNetwork(playerNetworkPool.BaseNetwork, fileName + ".eNN", pathName);
      }

      #region GetSet
      public long PlayerId
      {
        get { return playerId; }
      }

      /*
      public BasicNetwork PlayerNetwork
      {
          get { return playerNetwork; }
          private set { playerNetwork = value; }
      }
      */

      public NNThreadSafeNetworkPool PlayerNetworkPool
      {
        get { return playerNetworkPool; }
        private set { playerNetworkPool = value; }
      }

      public decimal NetworkAccuracy
      {
        get { return networkAccuracy; }
      }

      public long LastUpdatedHandId
      {
        get { return lastUpdatedHandId; }
      }

      public int NumActionsTrained
      {
        get { return numActionsTrained; }
      }

      public DateTime LastUpdatedTime
      {
        get { return lastUpdatedTime; }
      }
      #endregion

      public void UpdateNetwork(BasicNetwork network, decimal networkAccuracy, int numActionsTrained, long currentHandId)
      {
        this.playerNetworkPool = new NNThreadSafeNetworkPool(network, playerId.ToString(), NNThreadSafeNetworkPool.DefaultListLength);
        this.networkAccuracy = networkAccuracy;
        this.lastUpdatedHandId = currentHandId;
        this.lastUpdatedTime = DateTime.Now;
        this.numActionsTrained = numActionsTrained;
      }

      public void UpdateNetwork(long currentHandId)
      {
        this.lastUpdatedHandId = currentHandId;
        this.lastUpdatedTime = DateTime.Now;
      }

      public PAPNetworkContainer(long playerId, decimal networkAccuracy, long currentHandId, int numActionsTrained, BasicNetwork network)
      {
        this.playerId = playerId;
        this.playerNetworkPool = new NNThreadSafeNetworkPool(network, playerId.ToString(), NNThreadSafeNetworkPool.DefaultListLength);
        this.lastUpdatedHandId = currentHandId;
        this.lastUpdatedTime = DateTime.Now;
        this.networkAccuracy = networkAccuracy;
        this.numActionsTrained = numActionsTrained;
      }

      public PAPNetworkContainer(long playerId, decimal networkAccuracy, long currentHandId, int numActionsTrained, BasicNetwork network, DateTime lastUpdatedTime)
      {
        this.playerId = playerId;
        this.playerNetworkPool = new NNThreadSafeNetworkPool(network, playerId.ToString(), NNThreadSafeNetworkPool.DefaultListLength);
        this.lastUpdatedHandId = currentHandId;
        this.lastUpdatedTime = lastUpdatedTime;
        this.networkAccuracy = networkAccuracy;
        this.numActionsTrained = numActionsTrained;
      }

      #region ICloneable Members

      public object Clone()
      {
        return new PAPNetworkContainer(this.playerId, this.networkAccuracy, this.lastUpdatedHandId, this.numActionsTrained, (BasicNetwork)this.playerNetworkPool.BaseNetwork.Clone(), this.lastUpdatedTime);
      }

      #endregion
    }

    public playerActionPredictionNetworkManager()
    {
      if (InfoProviderBase.CurrentJob == null)
      {
        defaultNetworkPool = new NNThreadSafeNetworkPool(NNLoadSave.loadNetwork("generalPlayer.eNN", PAP_STORE), "default", NNThreadSafeNetworkPool.DefaultListLength);
      }
      else
      {
        //    defaultNetworkPool = new NNThreadSafeNetworkPool(NNLoadSave.loadNetwork(InfoProviders.InfoProviderBase.CurrentJob.JobData.NeuralNetworkBytes("PAP_generalPlayer.eNN")), "default", NNThreadSafeNetworkPool.DefaultListLength);
      }

      if (backgroundWorkerThread != null)
        throw new Exception("The network manager worker thread is already running!");

      //Start the monitor thread
      //backgroundWorkerThread = new Thread(networkBackgroundWorker);
      //backgroundWorkerThread.Name = "PAP_BackgroundWorker";
      //backgroundWorkerThread.Priority = ThreadPriority.BelowNormal;

      //We are not going to start the worker thread at the moment.
      //backgroundWorkerThread.Start();
    }

    public static void DisableNewNetworkTraining()
    {
      disableNewNetworkTraining = true;
    }

    public static void EnableNewNetworkTraining()
    {
      disableNewNetworkTraining = false;
    }

    /// <summary>
    /// Close the network manager
    /// </summary>
    public void Close()
    {
      closeThread = true;
    }

    public BasicNetwork GetDefaultNetwork()
    {
      return defaultNetworkPool.BaseNetwork;
    }

    public double[] getPlayerNetworkPrediction(long playerId, double[] networkInputs, ref decimal accuracy)
    {
      try
      {
        //Check to see if the network has already been retreived
        if (playerNetworksDict.ContainsKey(playerId))
        {
          accuracy = playerNetworksDict[playerId].NetworkAccuracy / 100m;
          return playerNetworksDict[playerId].PlayerNetworkPool.getNetworkPrediction(networkInputs);
        }
        else
        {
          accuracy = defaultNetworkAccuracy / 100m;
          return defaultNetworkPool.getNetworkPrediction(networkInputs);
        }
      }
      catch (Exception ex)
      {
        throw new Exception("Exception getting player prediction within PAP.");
      }
    }

    /// <summary>
    /// Does all of the background stuff
    /// </summary>
    protected void networkBackgroundWorker()
    {
      long[] allPlayerIds;
      long[] currentPlayerIds;
      long[] newPlayerIds;
      int currentNumberPokerTables;
      long mostRecentHandId;

      PAPNetworkContainer playerNetwork;
      //List<PAPNetworkContainer> playerNetworksTemp = new List<PAPNetworkContainer>();
      Dictionary<long, PAPNetworkContainer> playersNetworkTempDict = new Dictionary<long, PAPNetworkContainer>();

      do
      {
        try
        {
          //Create our own copy and work with that.
          //lock (locker) playerNetworksTemp = (from current in playerNetworks select new PAPNetworkContainer(current.PlayerId, current.NetworkAccuracy, current.LastUpdatedHandId, current.NumActionsTrained, (BasicNetwork)current.Network.Clone(), current.LastUpdatedTime)).ToList();

          allPlayerIds = CacheTracker.Instance.AllActivePlayers();

          //Sync the temp with cacheTracker (i.e. delete old players).
          playersNetworkTempDict =
              (from temp in playersNetworkTempDict
               where allPlayerIds.Contains(temp.Key)
               select temp).ToDictionary(k => k.Key, k => k.Value);

          //Get all current players
          currentNumberPokerTables = CacheTracker.Instance.AllActiveTableIds().Length;
          mostRecentHandId = CacheTracker.Instance.MostRecentHandId;

          currentPlayerIds = (from current in playersNetworkTempDict select current.Key).ToArray();
          newPlayerIds = (allPlayerIds.Except(currentPlayerIds)).ToArray();

          #region newPlayers
          for (int i = 0; i < newPlayerIds.Length; i++)
          {
            if (File.Exists(PAP_STORE + newPlayerIds[i].ToString() + ".eNN"))
            {
              //Load the network store object
              playerNetwork = PAPNetworkContainer.LoadPAPContainer(PAP_STORE, newPlayerIds[i].ToString());
              playersNetworkTempDict.Add(newPlayerIds[i], playerNetwork);
            }
            else if (!disableNewNetworkTraining)
            {
              //Get the number of actions recorded for this player
              long startingHandId = 0;
              int numPlayerActions = databaseQueries.getNumPlayerHandActions(newPlayerIds[i], true, maxTrainingActions, ref startingHandId);
              if (numPlayerActions > minActionsRequiredForNetwork)
              {
                //Create & train a new network
                PAPNetworkContainer previousNetwork = new PAPNetworkContainer(newPlayerIds[i], 0, mostRecentHandId, 0, defaultNetworkPool.BaseNetwork);

                //Get a new network
                playerNetwork = trainPlayerNetwork(newPlayerIds[i], mostRecentHandId, previousNetwork, startingHandId, maxTrainingActions);
                playersNetworkTempDict.Add(newPlayerIds[i], playerNetwork);
              }
            }
          }
          #endregion

          //For all players in the dictionary check for a network timeout (i.e. the accuracy needs updating).
          #region allPlayers
          for (int i = 0; i < playersNetworkTempDict.Count; i++)
          {
            playerNetwork = playersNetworkTempDict.ElementAt(i).Value;

            if (!disableNewNetworkTraining)
            {
              //If we just created a new network this will be false
              //If we just loaded a network then we will check to see if it's hit our timeout
              if (playerNetwork.NumActionsTrained < maxTrainingActions)
              {
                if (playerNetwork.LastUpdatedHandId < mostRecentHandId - (5 * currentNumberPokerTables * CacheTracker.handsPerTableTimeout) && playerNetwork.LastUpdatedTime < DateTime.Now.AddMinutes(-CacheTracker.activeTimeOutMins))
                {
                  //Has there been a significant increase in training actions?
                  long startingHandId = 0;
                  int numPlayerActions = databaseQueries.getNumPlayerHandActions(playerNetwork.PlayerId, true, maxTrainingActions, ref startingHandId);
                  //if (numPlayerActions - playerNetwork.NumActionsTrained > minActionsRequiredForNetwork)
                  //if (numPlayerActions - playerNetwork.NumActionsTrained > minActionsRequiredForNetwork)
                  //{
                  //Yes - Train a new network and change the list object for that player
                  playerNetwork = trainPlayerNetwork(playerNetwork.PlayerId, mostRecentHandId, playerNetwork, startingHandId, maxTrainingActions);
                  //}
                  //else
                  //No - Just reset lastUpdatedHandId
                  //    playerNetwork.UpdateNetwork(mostRecentHandId);
                }
              }
              else
              {
                //If we have hit the max actions trained we don't want to check for quite some time
                if (playerNetwork.LastUpdatedHandId < mostRecentHandId - (20 * currentNumberPokerTables * CacheTracker.handsPerTableTimeout) && playerNetwork.LastUpdatedTime < DateTime.Now.AddMinutes(-CacheTracker.activeTimeOutMins * 6))
                {
                  //If we are here then we will try to retrain the network on the most recent actions.
                  long startingHandId = 0;
                  int numPlayerActions = databaseQueries.getNumPlayerHandActions(playerNetwork.PlayerId, true, maxTrainingActions, ref startingHandId);
                  playerNetwork = trainPlayerNetwork(playerNetwork.PlayerId, mostRecentHandId, playerNetwork, startingHandId, maxTrainingActions);
                }
              }
            }

            if (closeThread)
              return;
          }
          #endregion

          //Copy the temp network back to the live copy at the end of of every train incase it times out before it gets a chance.
          lock (locker)
            playerNetworksDict = playersNetworkTempDict.ToDictionary(entry => entry.Key, entry => (PAPNetworkContainer)entry.Value.Clone());

          Thread.Sleep(5000);
        }
        catch (TimeoutException)
        {
          //Copy the temp network back to the live copy at the end of of every train incase it times out before it gets a chance.
          lock (locker)
            playerNetworksDict = playersNetworkTempDict.ToDictionary(entry => entry.Key, entry => (PAPNetworkContainer)entry.Value.Clone());
        }
        catch (Exception ex)
        {
          LogError.Log(ex, "AIPAPError");
        }
      } while (!closeThread);
    }

    /// <summary>
    /// Trains a new player network based on available database data and returns the most accurate network, previous or new. 
    /// </summary>
    /// <param name="playerId"></param>
    /// <param name="currentHandId"></param>
    /// <param name="currentNetwork"></param>
    /// <returns></returns>
    protected PAPNetworkContainer trainPlayerNetwork(long playerId, long currentHandId, PAPNetworkContainer previousNetwork, long startingHandId, int maxTrainingActions)
    {
      DateTime startTime = DateTime.Now;

      PAPNetworkContainer returnNetworkContainer;
      PokerPlayerNNModelv1 playerPredictionNNModel = new PokerPlayerNNModelv1();

      playerPredictionNNModel.populatePlayActions(playerId, maxTrainingActions, startingHandId);
      playerPredictionNNModel.SuffleDataSource();

      if (playerPredictionNNModel.DataSourceCount < minActionsRequiredForNetwork)
        return previousNetwork;

      playerPredictionNNModel.createNetwork();
      playerPredictionNNModel.createTrainingSets();
      playerPredictionNNModel.trainNetwork();
      playerPredictionNNModel.createTestingSets();

      BasicNetwork newPlayerNetwork = playerPredictionNNModel.Network;
      decimal newNetworkAccuracy = playerPredictionNNModel.getNetworkAccuracy();

      //We need to get the accuracy of the previous network on the new data so that it is a fair comparison
      playerPredictionNNModel.Network = previousNetwork.PlayerNetworkPool.BaseNetwork;
      decimal previousNetworkAccuracy = playerPredictionNNModel.getNetworkAccuracy();

      if (newNetworkAccuracy > previousNetworkAccuracy)
      {
        previousNetwork.UpdateNetwork(newPlayerNetwork, newNetworkAccuracy, playerPredictionNNModel.DataSourceCount, currentHandId);
      }
      else
      {
        //Regardless of whether we replace network/accuracy, we reset the trainingActions and mostRecentHandId
        previousNetwork.UpdateNetwork(previousNetwork.PlayerNetworkPool.BaseNetwork, previousNetworkAccuracy, playerPredictionNNModel.DataSourceCount, currentHandId);
      }

      returnNetworkContainer = previousNetwork;

      //SerializeObject.Save(PAP_STORE + playerId.ToString() + ".PAPdat", returnNetworkContainer);
      returnNetworkContainer.SavePAPContainer(PAP_STORE, playerId.ToString());
      return returnNetworkContainer;
    }
  }

}
