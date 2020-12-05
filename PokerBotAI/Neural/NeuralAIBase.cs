using System;
using System.Collections.Generic;
using System.Linq;
using Encog;
using Encog.Neural.Networks;
using PokerBot.Definitions;

namespace PokerBot.AI.Neural
{
  /// <summary>
  /// Stores the AI NN outputs.
  /// </summary>
  internal class NeuralAiDecision
  {
    double actCheckFold;
    double actCall;
    double actRaiseToCall;
    double actRaiseToSteal;
    double actAllIn;

    double[] networkOutputs;

    short botAction;
    double stochasticDouble;

    /// <summary>
    /// Returns the action the bot should perform.
    /// </summary>
    public short BotAction
    {
      get { return botAction; }
      set { botAction = value; }
    }

    public double StochasticDouble
    {
      get { return this.stochasticDouble; }
    }

    public double[] NetworkOutputs
    {
      get { return this.networkOutputs; }
    }

    public NeuralAiDecision(double[] networkOutputsIn, bool enableStochasticism, double stochasticDouble, bool useNewRelativeScale)
    {
      this.networkOutputs = networkOutputsIn.ToArray();
      this.stochasticDouble = stochasticDouble;

      if (useNewRelativeScale)
      {
        if (networkOutputs.Length != 5)
          throw new NotImplementedException("Relative output scaling currently only supported for neuralV4");

        double maxValue = networkOutputs.Max();

        if (maxValue > 0)
        {
          //We only need to scale things if we are getting a random value
          if (enableStochasticism)
          {
            double halfMaxValue = 0.5 * maxValue;

            //We take away half the max value from everything
            networkOutputs[0] -= halfMaxValue;
            networkOutputs[1] -= halfMaxValue;
            networkOutputs[2] -= halfMaxValue;
            networkOutputs[3] -= halfMaxValue;
            networkOutputs[4] -= halfMaxValue;

            //Set any negative outputs to zero
            if (networkOutputs[0] < 0)
              networkOutputs[0] = 0;
            if (networkOutputs[1] < 0)
              networkOutputs[1] = 0;
            if (networkOutputs[2] < 0)
              networkOutputs[2] = 0;
            if (networkOutputs[3] < 0)
              networkOutputs[3] = 0;
            if (networkOutputs[4] < 0)
              networkOutputs[4] = 0;

            //Rescale networkOutputs so everything adds up to 1
            double arrayScaleMultiplier = 1.0 / networkOutputs.Sum();
            networkOutputs[0] *= arrayScaleMultiplier;
            networkOutputs[1] *= arrayScaleMultiplier;
            networkOutputs[2] *= arrayScaleMultiplier;
            networkOutputs[3] *= arrayScaleMultiplier;
            networkOutputs[4] *= arrayScaleMultiplier;

            //Choose one of the outputs based on the stochastic double
            double runningTotal = 0;
            for (short i = 0; i < networkOutputs.Length; i++)
            {
              runningTotal += networkOutputs[i];
              if (stochasticDouble < runningTotal)
              {
                botAction = i;
                break;
              }
            }
          }
          else
          {
            //If there is no random decision we just take the maximum
            for (short i = 0; i < networkOutputs.Length; i++)
            {
              if (networkOutputs[i] == maxValue)
              {
                botAction = i;
                break;
              }
            }
          }
        }
        else
          //If the network output all zeros then we fold
          botAction = 0;
      }
      else
      {
        #region old Fixed Scaling
        short tempAction = 0;
        double tempMaxValue = networkOutputs[0];

        double outputTotal = 0;

        for (short i = 0; i < networkOutputs.Length; i++)
        {
          if (enableStochasticism && networkOutputs[i] < 0.15)
            networkOutputs[i] = 0;

          outputTotal += networkOutputs[i];

          if (networkOutputs[i] > tempMaxValue)
          {
            tempAction = i;
            tempMaxValue = networkOutputs[i];
          }
        }

        if (enableStochasticism)
        {
          //double randomNum = stochasticDouble;
          double runningTotal = 0;

          //Rescale networkOutputs so everything adds up to 1
          for (short i = 0; i < networkOutputs.Length; i++)
          {
            networkOutputs[i] = networkOutputs[i] * (1 / outputTotal);
            runningTotal += networkOutputs[i];

            if (stochasticDouble < runningTotal)
            {
              tempAction = i;
              break;
              //stochasticDouble = double.MaxValue;
            }
          }
        }

        botAction = tempAction;
        #endregion
      }
    }

    public NeuralAiDecision(double[] networkOutputs)
    {
      this.networkOutputs = networkOutputs;

      short tempAction = 0;
      double tempMaxValue = networkOutputs[0];

      for (short i = 0; i < networkOutputs.Length; i++)
      {
        if (networkOutputs[i] > tempMaxValue)
        {
          tempAction = i;
          tempMaxValue = networkOutputs[i];
        }
      }

      botAction = tempAction;
    }

    public NeuralAiDecision(double actCheckFold, double actCall, double actRaiseToCall, double actRaiseToSteal, double actAllIn)
    {
      this.actCheckFold = actCheckFold;
      this.actCall = actCall;
      this.actRaiseToCall = actRaiseToCall;
      this.actRaiseToSteal = actRaiseToSteal;
      this.actAllIn = actAllIn;

      //We need to work out which the largest value is
      short tempAction = 0;
      double tempMaxValue = actCheckFold;

      if (actCall > tempMaxValue)
      {
        tempAction = 1;
        tempMaxValue = actCall;
      }

      if (actRaiseToCall > tempMaxValue)
      {
        tempAction = 2;
        tempMaxValue = actRaiseToCall;
      }

      if (actRaiseToSteal > tempMaxValue)
      {
        tempAction = 3;
        tempMaxValue = actRaiseToSteal;
      }

      if (actAllIn > tempMaxValue)
      {
        tempAction = 4;
        tempMaxValue = actAllIn;
      }

      botAction = tempAction;
    }
  }

  internal abstract class NeuralAIBase : AIBase
  {
    protected static object locker = new object();
    protected static Dictionary<string, NNThreadSafeNetworkPool> threadSafeNetworkDict;

    public NeuralAIBase(AIRandomControl aiRandomControl)
        : base(aiRandomControl)
    {
      threadSafeNetworkDict = new Dictionary<string, NNThreadSafeNetworkPool>();
    }

    public static void ResetThreadSafeNetworkDict()
    {
      lock (locker)
        threadSafeNetworkDict = new Dictionary<string, NNThreadSafeNetworkPool>();
    }

    protected BasicNetwork PlayerNetworkCopy(string aiConfigStr)
    {
      if (threadSafeNetworkDict.ContainsKey(aiConfigStr))
        return threadSafeNetworkDict[aiConfigStr].BaseNetwork;
      else
        throw new Exception("No known network for that aiConfigStr exists.");
    }

    protected double[] getPlayerNetworkPrediction(string aiConfigStr, double[] networkInputs)
    {
      try
      {
        lock (locker)
        {
          //Check to see if the network has already been retreived
          if (threadSafeNetworkDict.ContainsKey(aiConfigStr))
            return threadSafeNetworkDict[aiConfigStr].getNetworkPrediction(networkInputs);
          else
          {
            BasicNetwork network = null;
            //We need to load the network
            if (InfoProviders.InfoProviderBase.CurrentJob == null)
              network = NNLoadSave.loadNetwork(aiConfigStr, AI_FILES_STORE);
            else
            {
              //network = NNLoadSave.loadNetwork(InfoProviders.InfoProviderBase.CurrentJob.JobData.NeuralNetworkBytes(aiConfigStr));
              /*
              string[] directories = aiConfigStr.Split('\\');
              //Check to see if the network needs to be created
              if (!File.Exists(FileLocations.ConvertWinFileReferenceToLocal("LocalNetworkStore\\" + aiConfigStr)))
              {
                  string networkStr = InfoProviders.InfoProviderBase.CurrentJob.JobData.NeuralNetwork(aiConfigStr);

                  if (!Directory.Exists("LocalNetworkStore"))
                      Directory.CreateDirectory("LocalNetworkStore");

                  //Make sure all of the necessary directories exist for the network
                  //string[] directories = aiConfigStr.Split('\\');
                  string currentDir = "LocalNetworkStore";
                  for (int i = 0; i < directories.Length - 1; i++)
                  {
                      if (!Directory.Exists(Path.Combine(currentDir, directories[i])))
                          Directory.CreateDirectory(Path.Combine(currentDir, directories[i]));

                      currentDir = Path.Combine(currentDir, directories[i]);
                  }

                  File.WriteAllBytes(FileLocations.ConvertWinFileReferenceToLocal("LocalNetworkStore\\" + aiConfigStr), (from current in networkStr.Split('-') select Convert.ToByte(current, 16)).ToArray());
              }

              if (InfoProviders.InfoProviderBase.CurrentJob.JobData.ContainsNeuralNetwork(aiConfigStr))
                  //We can now delete the entry in the job file otherwise it just takes up alot of memory
                  InfoProviders.InfoProviderBase.CurrentJob.JobData.RemoveNeuralNetwork(aiConfigStr);

              //Console.WriteLine("Loading network from LocalNetworkStore combine with {0}", FileLocations.ConvertWinFileReferenceToLocal(aiConfigStr));
              network = NNLoadSave.loadNetwork(FileLocations.ConvertWinFileReferenceToLocal(aiConfigStr), "LocalNetworkStore");
              */
            }

            if (!threadSafeNetworkDict.ContainsKey(aiConfigStr))
              threadSafeNetworkDict.Add(aiConfigStr, new NNThreadSafeNetworkPool(network, aiConfigStr, NNThreadSafeNetworkPool.DefaultListLength));

            return threadSafeNetworkDict[aiConfigStr].getNetworkPrediction(networkInputs);
          }
        }
      }
      catch (Exception ex)
      {
        throw new Exception("aiConfigStr was not formatted correctly for the current AI type.", ex);
      }
    }
  }
}
