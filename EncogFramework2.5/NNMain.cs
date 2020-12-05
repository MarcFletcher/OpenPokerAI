// Encog(tm) Artificial Intelligence Framework v2.3
// .Net Version
// http://www.heatonresearch.com/encog/
// http://code.google.com/p/encog-java/
// 
// Contributed to Encog By M.Fletcher
// University of Cambridge, Dept. of Physics, UK

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

using Encog.Neural.Networks;
using Encog.Neural.Networks.Layers;
using Encog.Util;
using Encog.Neural.Data.Basic;
using Encog.Neural.NeuralData;
using Encog.Neural.Networks.Training;
using Encog.Neural.Data;
using Encog.Neural.Networks.Training.Propagation.Resilient;
using Encog.Neural.Networks.Training.Propagation.Back;
using Encog.Neural.Networks.Training.Anneal;
using Encog.Neural.Networks.Training.Propagation.SCG;
using Encog.Persist;
using System.IO;
using Encog.Neural.Networks.Pattern;
using Encog.Neural.Networks.Prune;
using Encog.Engine.Network.Flat;
using Encog.Engine.Network.Activation;
using Encog.Engine.Util;
using System.Threading;

namespace Encog
{
#pragma warning disable 1591
  public class NNDataSource
  {
    double expectedNumInputs;
    double expectedNumOutputs;

    double[] inputs;
    double[] idealOutputs;

    public NNDataSource()
    {
      throw new Exception("This should not be called.");
    }

    /// <summary>
    /// Strings will be split based on comma seperation.
    /// </summary>
    /// <param name="inputString"></param>
    /// <param name="idealOutputString"></param>
    public NNDataSource(string inputString, string idealOutputString, double expectedNumInputs, double expectedNumOutputs, bool allowValidateToNegativeOne = false)
    {
      this.expectedNumInputs = expectedNumInputs;
      this.expectedNumOutputs = expectedNumOutputs;

      //Make sure that are no trailing commas on either strings
      if (inputString.Trim()[inputString.Trim().Length - 1] == ',')
        inputString = inputString.Trim().Substring(0, inputString.Trim().Length - 1);

      if (idealOutputString.Trim()[idealOutputString.Trim().Length - 1] == ',')
        idealOutputString = idealOutputString.Trim().Substring(0, idealOutputString.Trim().Length - 1);

      string[] inputStrValues = inputString.Split(',');
      string[] idealOutputStrValues = idealOutputString.Split(',');

      this.inputs = new double[inputStrValues.Length];
      this.idealOutputs = new double[idealOutputStrValues.Length];

      for (int i = 0; i < this.inputs.Length; i++)
        this.inputs[i] = Convert.ToDouble(inputStrValues[i]);

      for (int i = 0; i < this.idealOutputs.Length; i++)
        this.idealOutputs[i] = Convert.ToDouble(idealOutputStrValues[i]);

      Validate(allowValidateToNegativeOne);
    }

    public NNDataSource(string inputString, int expectedNumInputs, int expectedNumOutputs)
    {
      this.expectedNumInputs = expectedNumInputs;
      this.expectedNumOutputs = expectedNumOutputs;

      //Make sure that are no trailing commas on either strings
      if (inputString.Trim()[inputString.Trim().Length - 1] == ',')
        inputString = inputString.Trim().Substring(0, inputString.Trim().Length - 1);

      string[] inputStrValues = inputString.Split(',');

      //If the inputString length is different from the total of input and output and also not equal to inputs we have a bad string
      if (inputStrValues.Length != expectedNumInputs + expectedNumOutputs && inputStrValues.Length != expectedNumInputs)
        throw new Exception("Invalid inputString length");

      this.inputs = new double[expectedNumInputs];
      this.idealOutputs = new double[expectedNumOutputs];

      //First get the inputs
      for (int i = 0; i < expectedNumInputs; i++)
        this.inputs[i] = Convert.ToDouble(inputStrValues[i]);

      if (inputStrValues.Length != expectedNumInputs)
      {
        for (int i = 0; i < expectedNumOutputs; i++)
          this.idealOutputs[i] = Convert.ToDouble(inputStrValues[expectedNumInputs + i]);
      }

      Validate();
    }

    public NNDataSource(decimal[] inputs, double[] idealOutputs, int expectedNumInputs, int expectedNumOutputs)
    {
      //We need to convert the inputs to a double array for NN procesing
      double[] doubleInputs = new double[inputs.Length];

      for (int i = 0; i < doubleInputs.Length; i++)
        doubleInputs[i] = (double)inputs[i];

      this.inputs = doubleInputs;
      this.idealOutputs = idealOutputs;

      this.expectedNumInputs = expectedNumInputs;
      this.expectedNumOutputs = expectedNumOutputs;

      Validate();
    }

    /// <summary>
    /// Takes the inputs for the NN evaluation as decimals and converts to doubles within this object.
    /// </summary>
    /// <param name="inputs"></param>
    /// <param name="expectedNumInputs"></param>
    public NNDataSource(decimal[] inputs, double expectedNumInputs, bool allowValidateToNegativeOne = false)
    {
      //We need to convert the inputs to a double array for NN procesing
      double[] doubleInputs = new double[inputs.Length];

      for (int i = 0; i < doubleInputs.Length; i++)
        doubleInputs[i] = (double)inputs[i];

      this.inputs = doubleInputs;
      this.expectedNumInputs = expectedNumInputs;

      Validate(allowValidateToNegativeOne);
    }

    public void returnOutput(ref double[] idealOutput)
    {
      //idealOutput = new double[idealOutputs.Length];
      //for (int i = 0; i < idealOutputs.Length; i++)
      //    idealOutput[i] = idealOutputs[i];
      idealOutput = this.idealOutputs;
    }

    public void returnInput(ref double[] input)
    {
      //input = new double[inputs.Length];
      //for (int i = 0; i < inputs.Length; i++)
      //    input[i] = inputs[i];
      input = this.inputs;
    }

    private void Validate(bool allowToNegative1 = false, bool allowGreater1 = true)
    {
      if (inputs.Length != expectedNumInputs)
        throw new Exception("Incorrect number of inputs");

      for (int i = 0; i < inputs.Length; i++)
      {
        if (!allowToNegative1 && inputs[i] < 0 || inputs[i] > 1 || double.IsNaN(inputs[i]))
          throw new Exception("Input value at index " + i.ToString() + "(" + inputs[i].ToString() + ") is outside of allowed range.");
        else if (allowToNegative1 && inputs[i] < -1 || inputs[i] > 1 || double.IsNaN(inputs[i]))
          throw new Exception("Input value at index " + i.ToString() + "(" + inputs[i].ToString() + ") is outside of allowed range.");
      }

      if (idealOutputs != null)
      {
        for (int i = 0; i < idealOutputs.Length; i++)
          if (idealOutputs[i] < 0 || (idealOutputs[i] > 1 && !allowGreater1) || double.IsNaN(idealOutputs[i]))
            throw new Exception("Ideal ouput value at index " + i.ToString() + "(" + idealOutputs[i].ToString() + ") is outside of allowed range.");

        if (idealOutputs.Length != expectedNumOutputs)
          throw new Exception("Incorrect number of ideal outputs");
      }
    }

    public override string ToString()
    {
      return ToStringAdv(true);
    }

    public string ToStringAdv(bool allowToNegative1)
    {
      string outputString = "";
      Validate(allowToNegative1);

      for (int i = 0; i < inputs.Length; i++)
      {
        //If the value is an integer we just save the integer
        if ((double)(int)inputs[i] == inputs[i])
          outputString += ((int)inputs[i]).ToString("0") + ", ";
        else
          outputString += inputs[i].ToString("0.00000") + ", ";
      }

      if (idealOutputs != null)
      {
        for (int i = 0; i < idealOutputs.Length; i++)
        {
          //If the value is an integer we just save the integer
          if ((double)(int)idealOutputs[i] == idealOutputs[i])
            outputString += ((int)idealOutputs[i]).ToString("0") + ", ";
          else
            outputString += idealOutputs[i].ToString("0.00000") + ", ";
        }
      }

      //Remove the trailing comma
      outputString = outputString.Substring(0, outputString.Length - 2);

      return outputString;
    }

    public double[] Inputs
    {
      get { return (double[])this.inputs.Clone(); }
    }

    public double[] Outputs
    {
      get { return (double[])this.idealOutputs.Clone(); }
    }
  }

#pragma warning disable 1591
  public abstract class FBPNNMain<T> where T : NNDataSource
  {
    protected int INPUT_NEURONS;
    protected int OUTPUT_NEURONS;

    //Static network constants here
    protected int HIDDENLAYER1_NEURONS;
    protected int HIDDENLAYER2_NEURONS;

    protected double MAX_RMS_TOTAL_NETWORK_ERROR;
    protected double MAX_RMS_ITERATION_NETWORK_ERROR;

    protected double ANNEAL_STARTTEMP;
    protected double ANNEAL_ENDTEMP;
    protected int ANNEAL_ITERATIONS;
    protected double ANNEAL_MIN_ERROR;

    protected double ANNEAL_ATTEMPTS;
    protected double MAX_ANNEAL_START_ERROR;
    protected double MIN_BACKPROP_ITERATIONS_ANNEAL_START;

    protected int MAX_ITERATIONS = 3000;

    // How much of the data in datasource is used to train the network. 0.4 is 40% for training and 60% for testing.
    protected double TRAIN_DATA_PERCENT;

    // 1 - Sigmoid // 2 - TANH
    protected byte ACTIVIATION_FUNCTION;

    //The nerual net datasource, for testing or training
    protected List<T> neuralNetDataSource;

    protected double[][] networkInput;
    protected double[][] networkIdealOutput;

    //The most awesome of awesome feedforward nerual network
    protected BasicNetwork network;
    //protected FlatNetwork flatNetwork;
    //protected bool useFlatNetwork = false;

    #region get & set methods

    public double[][] NetworkInput
    {
      get { return networkInput; }
    }

    public double[][] NetworkIdealOutput
    {
      get { return networkIdealOutput; }
    }

    public int DataSourceCount
    {
      get { return neuralNetDataSource.Count; }
    }

    public double Train_Data_Percent
    {
      get { return TRAIN_DATA_PERCENT; }
    }

    public BasicNetwork Network
    {
      get { return network; }
      set { network = value; }
    }

    public List<T> NeuralNetDataSource
    {
      get { return neuralNetDataSource; }
      set { neuralNetDataSource = value; }
    }

    public int HiddenLayer1_Neurons
    {
      get { return HIDDENLAYER1_NEURONS; }
      set { HIDDENLAYER1_NEURONS = value; }
    }

    public int HiddenLayer2_Neurons
    {
      get { return HIDDENLAYER2_NEURONS; }
      set { HIDDENLAYER2_NEURONS = value; }
    }
    #endregion get & set methods

    /// <summary>
    /// Creates a feedforward NN
    /// </summary>
    public virtual void createNetwork()
    {
      IActivationFunction threshold;

      if (ACTIVIATION_FUNCTION == 1)
        threshold = new ActivationSigmoid();
      else if (ACTIVIATION_FUNCTION == 2)
        threshold = new ActivationTANH();
      else
        throw new System.Exception("Only 2 activation functions have been impletemented.");

      network = new BasicNetwork();
      network.AddLayer(new BasicLayer(threshold, true, INPUT_NEURONS));
      network.AddLayer(new BasicLayer(threshold, true, HIDDENLAYER1_NEURONS));

      if (HIDDENLAYER2_NEURONS > 0)
      {
        network.AddLayer(new BasicLayer(threshold, true, HIDDENLAYER2_NEURONS));
      }

      network.AddLayer(new BasicLayer(threshold, true, OUTPUT_NEURONS));
      network.Structure.FinalizeStructure();
      network.Reset();
    }

    /// <summary>
    /// Used to randomise the order of inputs should it be so desired!
    /// </summary>
    /// <param name="inputList"></param>
    /// <returns></returns>
    public List<T> shuffleList(List<T> inputList)
    {
      List<T> randomList = new List<T>();
      if (inputList.Count == 0)
        return randomList;

      Random r = new Random();
      int randomIndex = 0;
      while (inputList.Count > 0)
      {
        randomIndex = r.Next(0, inputList.Count); //Choose a random object in the list
        randomList.Add(inputList[randomIndex]); //add it to the new, random list<
        inputList.RemoveAt(randomIndex); //remove to avoid duplicates
      }

      //clean up
      inputList.Clear();
      inputList = null;
      r = null;

      return randomList; //return the new random list
    }

    public void SaveNNDataSource(string fileName)
    {
      //To write out the NNDataSource we just have to write everything out as strings
      using (System.IO.StreamWriter sw = new System.IO.StreamWriter(fileName, false))
      {
        for (int i = 0; i < neuralNetDataSource.Count; i++)
          sw.WriteLine(neuralNetDataSource[i].ToString());
      }
    }

    public void LoadNNDatasource(string fileName, int expectedNumInputs, int expectedOutputs)
    {
      string[] allLines = File.ReadAllLines(fileName);

      for (int i = 0; i < allLines.Length; i++)
      {
        NNDataSource item = new NNDataSource(allLines[i], expectedNumInputs, expectedOutputs);
        neuralNetDataSource.Add((T)item);
      }
    }

    #region NetworkUsage

    /// <summary>
    /// Returns the prediction of the network for a given input.
    /// </summary>
    /// <param name="dataSource"></param>
    /// <returns></returns>
    public double[] getNetworkPrediction(NNDataSource dataSource)
    {
      double[] networkOutputs = new double[OUTPUT_NEURONS];
      double[] networkInputs = new double[INPUT_NEURONS];
      dataSource.returnInput(ref networkInputs);

      network.Compute(networkInputs, networkOutputs);

      if (networkOutputs.Length != OUTPUT_NEURONS)
        throw new Exception("Unexpected number of network ouputs.");

      return networkOutputs;
    }

    #endregion NetworkUsage

    #region NetworkTesting

    /// <summary>
    /// Tests the network using defined inputs and ideal outputs. Returns a 2D matrix of the predicted
    /// values, which can then somewhere else be compared against the idealoutputs.
    /// </summary>
    /// <returns></returns>
    public double[][] testNetwork()
    {
      double[][] networkOutputs = new double[networkInput.Length][];

      for (int i = 0; i < networkOutputs.Length; i++)
      {
        INeuralData data = new BasicNeuralData(networkInput[i]);
        networkOutputs[i] = (double[])network.Compute(data).Data.Clone();
      }

      return networkOutputs;
    }

    public void testNetwork(double[][] networkOutputs)
    {
      for (int i = 0; i < networkOutputs.Length; i++)
      {
        //INeuralData data = new BasicNeuralData(networkInput[i]);
        //networkOutputs[i] = (double[])network.Compute(data).Data.Clone();

        BasicNeuralData inputs = new BasicNeuralData(networkInput[i]);
        INeuralData output = network.Compute(inputs);
        EngineArray.ArrayCopy(output.Data, networkOutputs[i]);
      }
    }

    /// <summary>
    /// Use the playerActions to set networkInput and networkIdealOutput for testing
    /// </summary>
    public void createTestingSets()
    {
      int testSamples = (int)(neuralNetDataSource.Count - (int)(neuralNetDataSource.Count * TRAIN_DATA_PERCENT));

      networkInput = new double[testSamples][];
      networkIdealOutput = new double[testSamples][];

      for (int i = 0; i < testSamples; i++)
      {
        networkInput[i] = new double[INPUT_NEURONS];
        networkIdealOutput[i] = new double[OUTPUT_NEURONS];

        neuralNetDataSource[i + (neuralNetDataSource.Count - testSamples)].returnInput(ref networkInput[i]);
        neuralNetDataSource[i + (neuralNetDataSource.Count - testSamples)].returnOutput(ref networkIdealOutput[i]);
      }
    }

    /// <summary>
    /// Uses the current training or testing data to determine the accuracy of the network.
    /// </summary>
    /// <returns>The network accuracy, e.g. 85.0%</returns>
    public decimal getNetworkAccuracy()
    {
      double[][] networkPredictedOutputs;
      decimal networkAccuracy;

      int idealColumn = int.MaxValue;
      int outputColumn = int.MaxValue;
      double outputMaxValue1st = -1 * double.MaxValue;

      int correctPredictions = 0;

      networkPredictedOutputs = testNetwork();

      for (int i = 0; i < networkPredictedOutputs.Length; i++)
      {
        outputMaxValue1st = -1;

        //Work out which column should have been output
        for (int j = 0; j < networkPredictedOutputs[0].Length; j++)
        {
          if (NetworkIdealOutput[i][j] == 1)
          {
            idealColumn = j;
            break;
          }
        }

        if (idealColumn == int.MaxValue)
          throw new Exception("Unable to determine the ideal output neuron.");

        //Work out which column should have been output
        for (int j = 0; j < networkPredictedOutputs[0].Length; j++)
        {
          if (networkPredictedOutputs[i][j] > outputMaxValue1st)
          {
            outputMaxValue1st = networkPredictedOutputs[i][j];
            outputColumn = j;
          }
        }

        if (outputColumn == int.MaxValue)
          throw new Exception("Unable to determine the actual output neuron.");

        if (outputColumn == idealColumn)
          correctPredictions++;

      }

      networkAccuracy = Math.Round(((decimal)correctPredictions / (decimal)networkPredictedOutputs.Length) * 100m, 2);
      return networkAccuracy;
    }

    #endregion NetworkTesting

    #region NetworkTraining

    /// <summary>
    /// Use the playerActions to set networkInput and networkIdealOutput for training
    /// </summary>
    public void createTrainingSets()
    {
      int trainSamples = (int)(neuralNetDataSource.Count * TRAIN_DATA_PERCENT);

      Console.WriteLine("Training Network From {0} Actions.", trainSamples);

      networkInput = new double[trainSamples][];
      networkIdealOutput = new double[trainSamples][];

      for (int i = 0; i < trainSamples; i++)
      {
        networkInput[i] = new double[INPUT_NEURONS];
        networkIdealOutput[i] = new double[OUTPUT_NEURONS];

        neuralNetDataSource[i].returnInput(ref networkInput[i]);
        neuralNetDataSource[i].returnOutput(ref networkIdealOutput[i]);
      }
    }

    /// <summary>
    /// Trains the network
    /// </summary>
    public virtual void trainNetwork()
    {
      INeuralDataSet trainingSet = new BasicNeuralDataSet(networkInput, networkIdealOutput);
      //ITrain trainBackProp = new Backpropagation(network, trainingSet, BACKPROP_LEARN_RATE, BACKPROP_MOMENTUM);

      ITrain trainBackProp = new ScaledConjugateGradient(network, trainingSet);

      double error = Double.MaxValue;
      double lastError = Double.MaxValue;
      int epoch = 1;

      int lastAnneal = 0;
      int errorExit = 0;

      double errorOnLastAnnealStart = double.MaxValue;
      double sameErrorOnLastAnnealStartCount = 0;

      double currentAnnealInterval = MIN_BACKPROP_ITERATIONS_ANNEAL_START;
      double annealStartError = 0;

      do
      {
        trainBackProp.Iteration();
        error = trainBackProp.Error;

        if (lastError - error < MAX_RMS_ITERATION_NETWORK_ERROR)
          errorExit++;
        else
          errorExit = 0;

        Console.WriteLine("Iteration(SC) #{0} Error: {1}", epoch, error.ToString("0.00000000"));

        if (error > ANNEAL_MIN_ERROR)
        {
          if ((lastAnneal > currentAnnealInterval) && (lastError - error < MAX_ANNEAL_START_ERROR))
          {
            if (error == errorOnLastAnnealStart)
              sameErrorOnLastAnnealStartCount++;
            else if (error < errorOnLastAnnealStart)
            {
              sameErrorOnLastAnnealStartCount = 0;
              errorOnLastAnnealStart = error;
            }

            ICalculateScore score = new TrainingSetScore(trainingSet);
            NeuralSimulatedAnnealing trainAnneal = new NeuralSimulatedAnnealing(network, score, ANNEAL_STARTTEMP, ANNEAL_ENDTEMP, ANNEAL_ITERATIONS);

            for (int i = 1; i <= ANNEAL_ATTEMPTS; i++)
            {
              trainAnneal.Iteration();

              if (i == 1)
                annealStartError = trainAnneal.Error;

              Console.WriteLine("Iteration(Anneal) #{0}-{1} Error: {2}", epoch, i, trainAnneal.Error.ToString("0.00000000"));
              //WebLogging.AddLog("WinRatioNeural", WebLogging.LogCategory.WinRatioNeural, "Iteration(Anneal) #" + i + " Error: " + trainAnneal.Error.ToString("0.00000000"));
            }

            if (annealStartError == trainAnneal.Error)
            {
              if (currentAnnealInterval < 200)
              {
                currentAnnealInterval *= 1.5;
                Console.WriteLine("Iteration(Anneal) # No improvment. Increasing anneal interval to " + currentAnnealInterval);
              }
              else
                Console.WriteLine("Iteration(Anneal) # No improvment. Anneal interval at max.");
            }

            lastAnneal = 0;

            trainBackProp = new ScaledConjugateGradient(network, trainingSet);
            trainBackProp.Iteration();
            error = trainBackProp.Error;
            //saveNetwork(correctPredictions.ToString("##0.0")+ "_" + epoch.ToString() + "_nerualPokerAI_LA.nnDAT");
          }
        }

        //Every 50 epochs we can test the network accuracy
        //#if DEBUG
        //if (epoch % 50 == 0)
        //{
        //    //We want to switch to the testing set if we are not using all data for training
        //    if (TRAIN_DATA_PERCENT < 1.0) createTestingSets();

        //    Console.WriteLine("    Network accuracy is currently {0}%",getNetworkAccuracy());

        //    //Wait for 1 second so that we can read the output
        //    Thread.Sleep(1000);

        //    //Likewise we want to switch back before continuing
        //    if (TRAIN_DATA_PERCENT < 1.0) createTrainingSets();
        //}
        //#endif

        lastError = trainBackProp.Error;
        epoch++;
        lastAnneal++;

        //} while (error > MAX_RMS_TOTAL_NETWORK_ERROR && errorExit < 10 && epoch < MAX_ITERATIONS);
      } while (trainBackProp.Error > MAX_RMS_TOTAL_NETWORK_ERROR && epoch < MAX_ITERATIONS && sameErrorOnLastAnnealStartCount < 2);
    }

    public void Prune()
    {
      INeuralDataSet trainingSet = new BasicNeuralDataSet(networkInput, networkIdealOutput);
      FeedForwardPattern pattern = new FeedForwardPattern();
      pattern.InputNeurons = INPUT_NEURONS;
      pattern.OutputNeurons = OUTPUT_NEURONS;

      if (ACTIVIATION_FUNCTION == 1)
        pattern.ActivationFunction = new ActivationSigmoid();
      else if (ACTIVIATION_FUNCTION == 2)
        pattern.ActivationFunction = new ActivationTANH();
      else
        throw new System.Exception("Only 2 activation functions have been impletemented.");

      PruneIncremental prune = new PruneIncremental(trainingSet, pattern, 200, new ConsoleStatusReportable());

      prune.AddHiddenLayer(10, 40);
      prune.AddHiddenLayer(0, 30);

      prune.Process();

      network = prune.BestNetwork;

      Console.WriteLine("Prune process complete.");
    }

    #endregion NetworkTraining

  }
}
