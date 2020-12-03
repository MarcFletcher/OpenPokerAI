using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using PokerBot.AI.InfoProviders;
using PokerBot.Database;
//using Encog.Neural.Networks;
using Encog.Util;
using System.Reflection;
using Encog.Neural.Networks;
using PokerBot.AI.ProviderPAP;
using Encog;
using System.Diagnostics;

namespace PokerBot.AI.ProviderPAP
{
  class TrainPAP
  {
    static void Main(string[] args)
    {
      TrainPAP test = new TrainPAP();
      test.Go();
    }

    public void Go()
    {
      double[][] networkPredictedOutputs;

      byte idealColumn;
      double outputMaxValue1st = -1;
      byte outputColumn = byte.MaxValue;
      int correctPredictions = 0;

      Console.WriteLine("Let's get this show on the road!!");

      long[] allPlayerIds = new long[0];

      //Todo - Add players to allPlayerIds

      //Before we do PAP we need select specific players based on agression
      //int numHandsCounted=0;
      //double RFreq_PreFlop=0, RFreq_PostFlop=0, CFreq_PreFlop=0, CFreq_PostFlop=0, PreFlopPlayFreq=0;
      //using (System.IO.StreamWriter sw = new System.IO.StreamWriter("playerAggression.csv", false))
      //{
      //    for (int i = 0; i < allPlayerIds.Length; i++)
      //    {
      //        //Break the stats down for the top player to find out what is the needed number of hands for a good answer
      //        //for (int j = 0; j < 353; j++)
      //        //{
      //            numHandsCounted = 0; RFreq_PreFlop = 0; RFreq_PostFlop = 0; CFreq_PreFlop = 0; CFreq_PostFlop = 0; PreFlopPlayFreq = 0;
      //            databaseQueries.PlayerAgressionMetrics(allPlayerIds[i],10000, 1, 1, ref numHandsCounted, ref RFreq_PreFlop, ref RFreq_PostFlop, ref CFreq_PreFlop, ref CFreq_PostFlop, ref PreFlopPlayFreq);
      //            //sw.WriteLine("{0},{1},{2},{3},{4},{5},{6}", allPlayerIds[i], numHandsCounted, RFreq_PreFlop, RFreq_PostFlop, CFreq_PreFlop, CFreq_PostFlop, PreFlopPlayFreq);
      //            sw.WriteLine("{0},{1},{2},{3},{4},{5}", allPlayerIds[i], RFreq_PreFlop, RFreq_PostFlop, CFreq_PreFlop, CFreq_PostFlop, PreFlopPlayFreq);    
      //            //Console.WriteLine(j);
      //        //}
      //        sw.Flush();
      //        Console.WriteLine(i);
      //    }
      //}

      //return;
      PokerPlayerNNModelv1 playerPrediction = new PokerPlayerNNModelv1();

      //Get all of the playerActions
      if (true)
      {
        Console.WriteLine("Loading PAP data for {0} players.", allPlayerIds.Length);
        for (int i = 0; i < allPlayerIds.Length; i++)
        {
          try
          {
            playerPrediction.populatePlayActions(allPlayerIds[i], 10000, 0);
            Console.WriteLine("... data loaded for player index {0} ({1}). {2} actions added.", i, allPlayerIds[i], playerPrediction.NeuralNetDataSource.Count);
            playerPrediction.SaveNNDataSource("allPlayerPAPData.csv");
          }
          catch (Exception ex)
          {
            Console.WriteLine("Meh - Error on playerId {0}.", allPlayerIds[i]);
            using (System.IO.StreamWriter sw = new System.IO.StreamWriter("errors.txt", true))
              sw.WriteLine(ex.ToString());
          }
        }
      }
      else
      {
        Console.WriteLine("Loading NNData...");
        playerPrediction.LoadNNDatasource("allPlayerPAPData.csv", PokerPlayerNNModelv1.Input_Neurons, PokerPlayerNNModelv1.Output_Neurons);
        Console.WriteLine("... complete.");
      }

      Console.WriteLine("Shuffling NNData...");
      playerPrediction.SuffleDataSource();
      Console.WriteLine("... complete.");

      Console.WriteLine("Initiating training ...");

      //playerPrediction.createNetwork();
      playerPrediction.Network = NNLoadSave.loadNetwork("generalPlayer.eNN", "");
      //neuralPokerAI.getBotAiNN(false);
      playerPrediction.createTrainingSets();
      playerPrediction.trainNetwork();
      playerPrediction.createTestingSets();

      networkPredictedOutputs = new double[playerPrediction.NetworkInput.Length][];
      ;

      //Create the output array
      for (int i = 0; i < networkPredictedOutputs.Length; i++)
        networkPredictedOutputs[i] = new double[PokerPlayerNNModelv1.Output_Neurons];

      Stopwatch timer = new Stopwatch();
      timer.Start();
      playerPrediction.testNetwork(networkPredictedOutputs);
      timer.Stop();

      for (int i = 0; i < networkPredictedOutputs.GetLength(0); i++)
      {
        outputMaxValue1st = -1;

        //Determine if the output match the ideal
        if (playerPrediction.NetworkIdealOutput[i][0] == 1)
          idealColumn = 0;
        else if (playerPrediction.NetworkIdealOutput[i][1] == 1)
          idealColumn = 1;
        else if (playerPrediction.NetworkIdealOutput[i][2] == 1)
          idealColumn = 2;
        else
          throw new Exception("This should never happen");

        if (networkPredictedOutputs[i][0] > outputMaxValue1st)
        {
          outputMaxValue1st = networkPredictedOutputs[i][0];
          outputColumn = 0;
        }

        if (networkPredictedOutputs[i][1] > outputMaxValue1st)
        {
          outputMaxValue1st = networkPredictedOutputs[i][1];
          outputColumn = 1;
        }

        if (networkPredictedOutputs[i][2] > outputMaxValue1st)
        {
          outputMaxValue1st = networkPredictedOutputs[i][2];
          outputColumn = 2;
        }

        if (outputColumn == byte.MaxValue)
          throw new Exception("This should not happen!");

        if (outputColumn != idealColumn)
        {
          Console.WriteLine("****  Actual-{0},Predicted-{1} - [{2}, {3}, {4}]", idealColumn, outputColumn, String.Format("{0:0.00}", networkPredictedOutputs[i][0]), String.Format("{0:0.00}", networkPredictedOutputs[i][1]), String.Format("{0:0.00}", networkPredictedOutputs[i][2]));
        }
        else
        {
          //if (idealColumn == 4)
          //Console.WriteLine("Actual-{0}, Predicted-{1} - [{2}, {3}, {4}, {5}, {6}]", idealColumn, outputColumn, String.Format("{0:0.00}", networkPredictedOutputs[i][0]), String.Format("{0:0.00}", networkPredictedOutputs[i][1]), String.Format("{0:0.00}", networkPredictedOutputs[i][2]), String.Format("{0:0.00}", networkPredictedOutputs[i][3]), String.Format("{0:0.00}", networkPredictedOutputs[i][4]));
          //Console.WriteLine("Actual-{0},Predicted-{1} - [{2}, {3}, {4}]", idealColumn, outputColumn, String.Format("{0:0.00}", networkPredictedOutputs[i][0]), String.Format("{0:0.00}", networkPredictedOutputs[i][1]), String.Format("{0:0.00}", networkPredictedOutputs[i][2]));
          correctPredictions++;
        }
      }

      Console.WriteLine("Predicted {0}% Of Actions Correctly.", ((double)correctPredictions / (double)(int)(playerPrediction.DataSourceCount - (int)(playerPrediction.DataSourceCount * playerPrediction.Train_Data_Percent))) * 100);
      Console.WriteLine("Per network compute - {0}ms.", (double)timer.ElapsedMilliseconds / ((double)(playerPrediction.DataSourceCount - (int)(playerPrediction.DataSourceCount * playerPrediction.Train_Data_Percent))));
      //Console.WriteLine("Predicted {0}% Of Actions Correctly.", ((double)correctPredictions / (double)(int)(playerPrediction.DataSourceCount)) * 100);

      NNLoadSave.saveNetwork(playerPrediction.Network, "generalPlayerNew.eNN", "");

      Console.ReadKey();
    }

    public void Go2()
    {
      Console.WriteLine("Let's get this show on the road!!");

      //Get all of the playerActions
      for (int i = 0; i < 20; i++)
      {
        PokerPlayerNNModelv1 playerPrediction = new PokerPlayerNNModelv1();
        playerPrediction.LoadNNDatasource("allPlayerPAPData.csv", PokerPlayerNNModelv1.Input_Neurons, PokerPlayerNNModelv1.Output_Neurons);

        playerPrediction.SuffleDataSource();
        playerPrediction.createNetwork();
        playerPrediction.createTrainingSets();
        playerPrediction.trainNetwork();
        playerPrediction.createTestingSets();
        decimal accuracy = playerPrediction.getNetworkAccuracy();

        Console.WriteLine("Achieved {0}% accuracy.", accuracy);
        NNLoadSave.saveNetwork(playerPrediction.Network, accuracy.ToString() + "_generalPlayer.eNN", "");
      }

      Console.WriteLine("... completed.");
      Console.ReadKey();
    }

    public void Go3()
    {
      Console.WriteLine("Let's get this show on the road!!");

      long[] allPlayerIds = new long[0];

      //Todo - Add players to allPlayerIds

      //Get all of the playerActions
      Console.WriteLine("Testing new network accuracy for {0} players.", allPlayerIds.Length);

      for (int i = 0; i < allPlayerIds.Length; i++)
      {
        try
        {
          PokerPlayerNNModelv1 playerPrediction = new PokerPlayerNNModelv1();

          playerPrediction.populatePlayActions(allPlayerIds[i], 10000, 0);
          playerPrediction.SuffleDataSource();

          //playerPrediction.createNetwork();
          //playerPrediction.createTrainingSets();
          //playerPrediction.trainNetwork();
          playerPrediction.Network = NNLoadSave.loadNetwork("generalPlayer.eNN", "");

          Console.WriteLine("... data loaded for player index {0} ({1}). {2} actions added.", i, allPlayerIds[i], playerPrediction.NeuralNetDataSource.Count);
          //playerPrediction.SaveNNDataSource("allPlayerPAPData.csv");

          playerPrediction.createTestingSets();
          decimal accuracy = playerPrediction.getNetworkAccuracy();
          Console.WriteLine("PlayerId:{0} Predicted {1}% Of Actions Correctly.", allPlayerIds[i], Math.Round(accuracy, 1));

          using (System.IO.StreamWriter sw = new System.IO.StreamWriter("accuracy.csv", true))
            sw.WriteLine(allPlayerIds[i] + ", " + Math.Round(accuracy, 1));
        }
        catch (Exception ex)
        {
          Console.WriteLine("Meh - Error on playerId {0}.", allPlayerIds[i]);
          using (System.IO.StreamWriter sw = new System.IO.StreamWriter("errors.txt", true))
            sw.WriteLine(ex.ToString());
        }
      }

      //NNLoadSave.saveNetwork(playerPrediction.Network, "generalPlayer.eNN", "");
      Console.WriteLine("... completed.");
      Console.ReadKey();
    }
  }
}
