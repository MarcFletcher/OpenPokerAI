using System;
using System.Collections.Generic;
using System.Linq;
using Encog;
using PokerBot.Database;

namespace PokerBot.AI.ProviderPAP
{
  // Will implement the NNProvider interface <>
  public class PokerPlayerNNModelv1 : FBPNNMain<NNDataSource>
  {
    //protected const int inputNeurons = 21;
    protected const int inputNeurons = 24;
    protected const int outputNeurons = 3;

    public static int Input_Neurons
    {
      get { return inputNeurons; }
    }

    public static int Output_Neurons
    {
      get { return outputNeurons; }
    }

    public PokerPlayerNNModelv1()
    {
      OtherSetup();
    }

    private void OtherSetup()
    {
      INPUT_NEURONS = inputNeurons;
      OUTPUT_NEURONS = outputNeurons;

      HIDDENLAYER1_NEURONS = 50;
      HIDDENLAYER2_NEURONS = 30;

      ANNEAL_STARTTEMP = 50;
      ANNEAL_ENDTEMP = 1;
      ANNEAL_ITERATIONS = 50;
      ANNEAL_MIN_ERROR = 0.03;

      ANNEAL_ATTEMPTS = 10;
      MAX_ANNEAL_START_ERROR = 0.00001;
      MIN_BACKPROP_ITERATIONS_ANNEAL_START = 150;

      ACTIVIATION_FUNCTION = 1;

      MAX_ITERATIONS = 100000;

      //MAX_RMS_ITERATION_NETWORK_ERROR = 0.000002; //TANH
      //MAX_RMS_ITERATION_NETWORK_ERROR = 0.000000002; //SIG
      MAX_RMS_ITERATION_NETWORK_ERROR = 0.000001; //SIG
      MAX_RMS_TOTAL_NETWORK_ERROR = 0.02;

      TRAIN_DATA_PERCENT = 0.75;

      neuralNetDataSource = new List<NNDataSource>();
    }

    public void populatePlayActions(List<long> playerIds)
    {
      throw new NotImplementedException();
    }

    public void populatePlayActions(long playerId, int maxActions, long startingHandId)
    {
      //We need to get the agression information here
      int numHandsCounted = 0;
      decimal RFreq_PreFlop = 0, RFreq_PostFlop = 0, CFreq_PreFlop = 0, CFreq_PostFlop = 0, PreFlopPlayFreq = 0, PostFlopPlayFreq = 0, checkFreq_PreFlop = 0, checkFreq_PostFlop = 0;

      databaseQueries.PlayerAgressionMetrics(playerId, 10000, 1, 1, ref numHandsCounted, ref RFreq_PreFlop, ref RFreq_PostFlop, ref CFreq_PreFlop, ref CFreq_PostFlop, ref checkFreq_PreFlop, ref checkFreq_PostFlop, ref PreFlopPlayFreq, ref PostFlopPlayFreq);

      List<NNDataSource> playerData = (from
          results in databaseQueries.playerActionPredictionData(playerId, maxActions, startingHandId)
                                       select new NNDataSource(new decimal[] { (decimal)results.imPotOdds, (decimal)results.raiseRatio,
                                                 (decimal)results.potRatio, (results.betsToCall==0 ? 1 : 0), (results.betsToCall==1 ? 1 : 0), (results.betsToCall>1 ? 1 : 0),
                                                 (results.gameStage==0 ? 1 : 0), (results.gameStage==1 ? 1 : 0), (results.gameStage==2 ? 1 : 0), (results.gameStage==3 ? 1 : 0),
                                                 Convert.ToDecimal(results.calledLastRoundBets), Convert.ToDecimal(results.raisedLastRound), (decimal)results.dealtInPlayers,
                                                 (decimal)results.activePlayers, (decimal)results.unactedPlayers, Convert.ToDecimal(results.flushPossible), Convert.ToDecimal(results.straightPossible),
                                                 Convert.ToDecimal(results.aceOnBoard), Convert.ToDecimal(results.kingOnBoard), (decimal)results.aceKingQueenRatio, (decimal)results.dealerDistance, PreFlopPlayFreq, CFreq_PostFlop, RFreq_PostFlop},
                                           new double[] { (results.actionTypeId == 6 || results.actionTypeId == 7 ? 1 : 0), (results.actionTypeId == 8 ? 1 : 0), (results.actionTypeId == 9 ? 1 : 0) }, Input_Neurons, Output_Neurons)).ToList();

      neuralNetDataSource.AddRange(playerData.ToArray());
    }

    public void SuffleDataSource()
    {
      neuralNetDataSource = shuffleList(neuralNetDataSource);
    }

    public void populateTestPlayActions()
    {
      decimal[] testInput1 = new decimal[] { 0, 0, 0, 9, 0.1m, 0.5m, 1, 3, 0, 1, 1, 0.1m, 0.1m, 0.1m, 1, 0, 1, 0, 0.2m, 0.3m };
      decimal[] testInput2 = new decimal[] { 0, 0, 0, 8, 0.1m, 0.5m, 1, 3, 0, 1, 1, 0.1m, 0.1m, 0.1m, 1, 0, 1, 0, 0.2m, 0.3m };

      //Create the test actions
      for (int i = 0; i < 500; i++)
        neuralNetDataSource.Add(new NNDataSource(testInput1, Input_Neurons));

      for (int i = 0; i < 500; i++)
        neuralNetDataSource.Add(new NNDataSource(testInput2, Input_Neurons));
    }
  }
}
