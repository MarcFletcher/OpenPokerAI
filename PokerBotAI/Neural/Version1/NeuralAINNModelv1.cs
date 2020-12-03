using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using PokerBot.Database;
using System.IO;
using Encog;
using Encog.Neural.Networks;
using Encog.Util;

namespace PokerBot.AI.Neural.Version1
{
  public class NeuralAINNModelV1 : FBPNNMain<NNDataSource>
  {
    protected const int inputNeurons = 53;
    protected const int outputNeurons = 5;

    public static int Input_Neurons
    {
      get { return inputNeurons; }
    }

    public static int Output_Neurons
    {
      get { return outputNeurons; }
    }

    public NeuralAINNModelV1()
    {
      OtherSetup();
    }

    private void OtherSetup()
    {
      INPUT_NEURONS = inputNeurons;
      OUTPUT_NEURONS = outputNeurons;

      HIDDENLAYER1_NEURONS = 30;
      HIDDENLAYER2_NEURONS = 20;

      ANNEAL_STARTTEMP = 150;
      ANNEAL_ENDTEMP = 2;
      ANNEAL_ITERATIONS = 50;
      ANNEAL_MIN_ERROR = 0.05;

      ANNEAL_ATTEMPTS = 10;
      MAX_ANNEAL_START_ERROR = 0.0001;
      MIN_BACKPROP_ITERATIONS_ANNEAL_START = 150;

      ACTIVIATION_FUNCTION = 1;

      //MAX_RMS_ITERATION_NETWORK_ERROR = 0.000002; //TANH
      MAX_RMS_ITERATION_NETWORK_ERROR = 0.0000001; //SIG
      MAX_RMS_TOTAL_NETWORK_ERROR = 0.02;

      TRAIN_DATA_PERCENT = 1;

      neuralNetDataSource = new List<NNDataSource>();
    }

    public void getBotAiLogFileData(string logFileString)
    {
      string[] allLines = File.ReadAllLines(logFileString);

      foreach (string inputString in allLines)
      {
        //Remove the actionId from the beginning of the string
        string trimedString = inputString.Substring(inputString.IndexOf(',') + 1, inputString.Length - inputString.IndexOf(',') - 1);

        //Split the string into input and output
        string[] strValues = trimedString.Split(',');
        string inputStr = "";
        string idealOutputStr = "";

        for (int i = 0; i < strValues.Length - 5; i++)
          inputStr += strValues[i] + ",";

        for (int i = strValues.Length - 5; i < strValues.Length; i++)
          idealOutputStr += strValues[i] + ",";

        neuralNetDataSource.Add(new NNDataSource(inputStr, idealOutputStr, Input_Neurons, Output_Neurons));
      }
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
       */

      /*
      string[][] aiTrainingLogStr = databaseSP.aiPlayerNNData(pokerClientId);

      neuralNetDataSource.AddRange(
          from log in aiTrainingLogStr
          select new AIActionV1(log[1])
          );

      neuralNetDataSource = shuffleList(neuralNetDataSource);
       */
      throw new NotImplementedException();
    }

  }
}
