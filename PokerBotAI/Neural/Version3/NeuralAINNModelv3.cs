using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using PokerBot.Database;
using System.IO;
using Encog;
using Encog.Neural.Networks;
using Encog.Util;

namespace PokerBot.AI.Neural.Version3
{
  public class NeuralAINNModelV3 : FBPNNMain<NNDataSource>
  {
    protected const int inputNeurons = 16;
    protected const int outputNeurons = 5;

    public static int Input_Neurons
    {
      get { return inputNeurons; }
    }

    public static int Output_Neurons
    {
      get { return outputNeurons; }
    }

    public NeuralAINNModelV3()
    {
      OtherSetup();
    }

    private void OtherSetup()
    {
      INPUT_NEURONS = inputNeurons;
      OUTPUT_NEURONS = outputNeurons;
      HIDDENLAYER1_NEURONS = 30;
      HIDDENLAYER2_NEURONS = 0;

      ANNEAL_STARTTEMP = 150;
      ANNEAL_ENDTEMP = 2;
      ANNEAL_ITERATIONS = 50;
      ANNEAL_MIN_ERROR = 0.05;

      ANNEAL_ATTEMPTS = 10;
      MAX_ANNEAL_START_ERROR = 0.0001;
      MIN_BACKPROP_ITERATIONS_ANNEAL_START = 30;

      ACTIVIATION_FUNCTION = 1;

      //MAX_RMS_ITERATION_NETWORK_ERROR = 0.000002; //TANH
      MAX_RMS_ITERATION_NETWORK_ERROR = 0.0000001; //SIG
      MAX_RMS_TOTAL_NETWORK_ERROR = 0.10;

      TRAIN_DATA_PERCENT = 1;

      neuralNetDataSource = new List<NNDataSource>();
    }

    public void getBotAiLogFileData(string logFileString)
    {
      //throw new NotImplementedException();
      string[] allLines = File.ReadAllLines(logFileString);

      /*
      foreach(string inputString in allLines)
      {
          string trimedString = inputString.Substring(inputString.IndexOf(',') + 1, inputString.Length - inputString.IndexOf(',') - 1);
          neuralNetDataSource.Add(new AIActionV3(trimedString));
      }
      */

      throw new NotImplementedException();
    }

    public void getBotAiNNDatabaseData(short pokerClientId)
    {
      throw new NotImplementedException();
    }

  }
}
