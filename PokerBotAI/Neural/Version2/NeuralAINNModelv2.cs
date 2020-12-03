using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using PokerBot.Database;
using System.IO;
using Encog;
using Encog.Neural.Networks;
using Encog.Util;

namespace PokerBot.AI.Neural.Version2
{
  public class NeuralAINNModelV2 : FBPNNMain<NNDataSource>
  {
    protected const int inputNeurons = 5;
    protected const int outputNeurons = 3;

    public static int Input_Neurons
    {
      get { return inputNeurons; }
    }

    public static int Output_Neurons
    {
      get { return outputNeurons; }
    }

    public NeuralAINNModelV2()
    {
      OtherSetup();
    }

    private void OtherSetup()
    {
      INPUT_NEURONS = inputNeurons;
      OUTPUT_NEURONS = outputNeurons;

      HIDDENLAYER1_NEURONS = 8;
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

      foreach (string inputString in allLines)
      {
        //neuralNetDataSource.Add(new AIActionV2(inputString));
      }
      throw new NotImplementedException();
    }

    public void getBotAiNNDatabaseData(short pokerClientId)
    {

      //databaseLinqDataContext database = new databaseLinqDataContext();

      /*
      List<long> deleteLogIds = new List<long>();
      var aiLogs =
                   from log in database.tbl_aiDecisionLogs
                   join actions in database.tbl_handActions on log.handActionId equals actions.id
                   join hands in database.tbl_hands on actions.handId equals hands.id
                   join tables in database.tbl_tables on hands.tableId equals tables.id
                   where tables.pokerClientId == pokerClientId
                   select log;

      foreach(var log in aiLogs)
      {
          if (Byte.Parse(log.logString.Substring(9, 1)) == 1)
              deleteLogIds.Add(log.id);
      }
      */
      /*
      var badLogs =
          from log in database.tbl_aiDecisionLogs
          where deleteLogIds.Contains(log.id)
          select log;

      database.tbl_aiDecisionLogs.DeleteAllOnSubmit(badLogs);
      database.SubmitChanges();


      string[][] aiTrainingLogStr = databaseCache.aiPlayerNNData(pokerClientId);

      neuralNetDataSource.AddRange(
          from log in aiTrainingLogStr
          select new AIActionV2(log[1])
          );

      neuralNetDataSource = shuffleList(neuralNetDataSource);
       */
      throw new NotImplementedException();
    }

  }
}
