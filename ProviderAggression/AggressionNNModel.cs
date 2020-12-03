using System;
using System.Collections.Generic;
using System.Linq;
using System.IO;
using Encog;

namespace ProviderAggression
{
  public class AggressionNNModel : FBPNNMain<NNDataSource>
  {
    protected const int inputNeurons = 61;
    protected const int outputNeurons = 4;

    public static int Input_Neurons
    {
      get { return inputNeurons; }
    }

    public static int Output_Neurons
    {
      get { return outputNeurons; }
    }

    public AggressionNNModel()
    {
      OtherSetup();
    }

    private void OtherSetup()
    {
      INPUT_NEURONS = inputNeurons;
      OUTPUT_NEURONS = outputNeurons;
      HIDDENLAYER1_NEURONS = 30;
      HIDDENLAYER2_NEURONS = 15;

      ANNEAL_STARTTEMP = 100;
      ANNEAL_ENDTEMP = 2;
      ANNEAL_ITERATIONS = 50;
      ANNEAL_MIN_ERROR = 0.0;

      ANNEAL_ATTEMPTS = 5;
      MAX_ANNEAL_START_ERROR = 0.0001;
      MIN_BACKPROP_ITERATIONS_ANNEAL_START = 20;

      ACTIVIATION_FUNCTION = 1;

      //MAX_RMS_ITERATION_NETWORK_ERROR = 0.000002; //TANH
      MAX_RMS_ITERATION_NETWORK_ERROR = 0.0000001; //SIG
      MAX_RMS_TOTAL_NETWORK_ERROR = 0.02;

      TRAIN_DATA_PERCENT = 0.8;

      neuralNetDataSource = new List<NNDataSource>();
    }

    public void getBotAiLogFileData(string logFileString, Dictionary<long, List<byte>> actionsToIgnore = null)
    {
      string[] allLines = File.ReadAllLines(logFileString).Skip(1).ToArray();
      //long[] handIds = new long[allLines.Length];

      for (int i = 0; i < allLines.Length; i++)
      {
        var elements = (from items in allLines[i].Split(',').Skip(1).Select((obj, index) => { return new { obj, index }; })
                        select new { input = items.index < (int)inputNeurons ? items.obj + ',' : "", output = items.index >= (int)inputNeurons ? items.obj + ',' : "" });

        var inputs = elements.Aggregate((input, element) => { return new { input = input.input + element.input, output = "" }; }).input.TrimEnd(',').Replace("TRUE", "1").Replace("FALSE", "0");
        var outputs = elements.Aggregate((input, element) => { return new { input = "", output = input.output + element.output }; }).output.TrimEnd(',').Replace("TRUE", "1").Replace("FALSE", "0");

        neuralNetDataSource.Add(new NNDataSource(inputs, outputs, inputNeurons, outputNeurons, true));
      }
    }

    public void getBotAiNNDatabaseData(short pokerClientId)
    {
      throw new NotImplementedException();
    }

    private struct VectorDistanceElement
    {
      public double totalVectorDistance;
      public NNDataSource element;

      public VectorDistanceElement(double distance, NNDataSource element)
      {
        this.totalVectorDistance = distance;
        this.element = element;
      }
    }

    /// <summary>
    /// Using the currently loaded NNDataSource list calculates vector distance for all and then returns the closest matches
    /// </summary>
    /// <param name="dataSourceToMatch"></param>
    /// <param name="numReturns"></param>
    /// <returns></returns>
    public NNDataSource[] ReturnBestNNDataSourceMatch(NNDataSource dataSourceToMatch, int numReturns)
    {
      throw new NotImplementedException("Not yet implmented in v5. Check v4 for similar.");
    }
  }
}
