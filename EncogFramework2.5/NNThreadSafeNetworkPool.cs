using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Encog.Neural.Networks;

namespace Encog
{
  /// <summary>
  /// Maintains an available list of networks and only provides access via getNetworkPrediction.
  /// Will work in the same way as the BasicNetwork object but is threadsafe when calling compute
  /// </summary>
  public class NNThreadSafeNetworkPool
  {
    public const int DefaultListLength = 1;

    /// <summary>
    /// This network is copied if we require more networks in the queue
    /// </summary>
    BasicNetwork baseNetwork;
    string networkConfigStr;

    object baseLocker = new object();

    /// <summary>
    /// The index of the network which should be available
    /// </summary>
    int nextExpectedAvailableNetworkIndex = 0;

    /// <summary>
    /// The bit flagger which gives available instances
    /// </summary>
    int idleNetworksBitFlags;

    /// <summary>
    /// The thread safe networks
    /// </summary>
    List<ThreadSafeNetwork> threadSafeNetworksList = new List<ThreadSafeNetwork>();

    /// <summary>
    /// Create a new NNThreadSafeNetworkPool
    /// </summary>
    /// <param name="baseNetwork">The network to use for this object</param>
    /// <param name="listStartLength">The default length to start the network list at.</param>
    public NNThreadSafeNetworkPool(BasicNetwork baseNetwork, string networkConfigStr, int listStartLength)
    {
      this.baseNetwork = (BasicNetwork)baseNetwork.Clone();
      this.networkConfigStr = networkConfigStr;

      for (int i = 0; i < listStartLength; i++)
        threadSafeNetworksList.Add(new ThreadSafeNetwork((BasicNetwork)baseNetwork.Clone()));

      //Initialise idleNetworksBitFlags
      for (int i = 0; i < listStartLength; i++)
        idleNetworksBitFlags = idleNetworksBitFlags | (1 << i);
    }

    /// <summary>
    /// Get a string which can be used to identify this network pool
    /// </summary>
    public string NetworkConfigStr
    {
      get { return networkConfigStr; }
    }

    public BasicNetwork BaseNetwork
    {
      get { return (BasicNetwork)baseNetwork.Clone(); }
    }

    /// <summary>
    /// Get a prediction from the network pool using the provided inputs
    /// </summary>
    /// <param name="inputs"></param>
    /// <returns></returns>
    public double[] getNetworkPrediction(double[] inputs)
    {
      int networkInstance = DetermineAvailableInstanceIndex();

      if (networkInstance == -1)
      {
        lock (baseLocker)
        {
          threadSafeNetworksList.Add(new ThreadSafeNetwork((BasicNetwork)baseNetwork.Clone()));
          networkInstance = threadSafeNetworksList.Count - 1;
        }
      }

      double[] outputs = threadSafeNetworksList[networkInstance].getNetworkPrediction(inputs);

      //Now set instance available again
      lock (baseLocker)
        idleNetworksBitFlags = idleNetworksBitFlags | (1 << networkInstance);

      //Get next available network in list
      //If one is not avaialble add one to the list and then use that
      return outputs;
    }

    /// <summary>
    /// Returns the index of an available network. If no networks are available returns -1
    /// </summary>
    /// <returns></returns>
    private int DetermineAvailableInstanceIndex()
    {
      int selectedInstanceIndex = -1;
      int availableInstances;
      int indexAvailableMask;

      lock (baseLocker)
      {
        //Work out if any instances are available
        availableInstances = idleNetworksBitFlags & int.MaxValue;

        //Only continue if instances are available
        //If there are no instances we just return -1
        if (availableInstances > 0)
        {
          //Initialise the mask at the expected position on an available instance
          indexAvailableMask = 1 << nextExpectedAvailableNetworkIndex;

          //Initialise the return value at this as well
          selectedInstanceIndex = nextExpectedAvailableNetworkIndex;

          while (true)
          {
            if ((availableInstances & indexAvailableMask) > 0)
            {
              //Once we have found an available instance change the bit flags
              idleNetworksBitFlags = idleNetworksBitFlags ^ indexAvailableMask;
              break;
            }

            selectedInstanceIndex++;
            indexAvailableMask = indexAvailableMask << 1;

            if (selectedInstanceIndex >= threadSafeNetworksList.Count)
            {
              selectedInstanceIndex = 0;
              indexAvailableMask = 1;
            }
          }

          nextExpectedAvailableNetworkIndex = selectedInstanceIndex + 1;

          if (nextExpectedAvailableNetworkIndex >= threadSafeNetworksList.Count)
            nextExpectedAvailableNetworkIndex = 0;
        }
      }

      return selectedInstanceIndex;
    }
  }

  class ThreadSafeNetwork
  {
    BasicNetwork network;
    object networkLocker = new object();

    public ThreadSafeNetwork(BasicNetwork network)
    {
      this.network = network;
      this.network.Structure.FinalizeStructure();
    }

    public double[] getNetworkPrediction(double[] inputs)
    {
      double[] networkOuputs = new double[network.OutputCount];

      lock (networkLocker)
        network.Compute(inputs, networkOuputs);

      return networkOuputs;
    }
  }
}
