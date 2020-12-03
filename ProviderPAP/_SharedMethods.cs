using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using PokerBot.Definitions;
using Encog;
using PokerBot.AI.InfoProviders;
using ProviderAggression;

namespace PokerBot.AI.ProviderPAP
{
  public partial class PlayerActionPredictionProvider : InfoProviderBase
  {
    #region sharedMethods

    //protected List<tempPlayerCache> tempLocalPlayerCache;
    protected Dictionary<long, tempPlayerCache> tempLocalPlayerCacheDict;
    protected tempHandCache tempLocalHandCache;

    #region PAP Protected Classes
    [Serializable]
    protected class tempPlayerCache
    {

      //[NonSerialized]
      public long playerId;
      //[NonSerialized]
      public long handId;
      //[NonSerialized]
      public decimal playerStack;
      public byte playerPosition;
      public decimal totalPlayerMoneyInPot;
      public decimal currentRoundPlayerBetAmount;
      public bool playerLastRoundCall;
      public bool playerLastRoundRaise;

      public decimal RFreq_PreFlop;
      public decimal RFreq_PostFlop;
      public decimal CFreq_PreFlop;
      public decimal CFreq_PostFlop;
      public decimal PreFlopPlayFreq;

      public tempPlayerCache(long playerId, long handId, decimal playerStack, byte playerPosition, decimal totalPlayerMoneyInPot, decimal currentRoundPlayerBetAmount, bool playerLastRoundCall, bool playerLastRoundRaise, decimal RFreq_PreFlop,
      decimal RFreq_PostFlop,
      decimal CFreq_PreFlop,
      decimal CFreq_PostFlop,
      decimal PreFlopPlayFreq)
      {
        this.playerId = playerId;
        this.handId = handId;
        this.playerStack = playerStack;
        this.playerPosition = playerPosition;
        this.totalPlayerMoneyInPot = totalPlayerMoneyInPot;
        this.currentRoundPlayerBetAmount = currentRoundPlayerBetAmount;
        this.playerLastRoundCall = playerLastRoundCall;
        this.playerLastRoundRaise = playerLastRoundRaise;

        this.RFreq_PreFlop = RFreq_PreFlop;
        this.RFreq_PostFlop = RFreq_PostFlop;
        this.CFreq_PreFlop = CFreq_PreFlop;
        this.CFreq_PostFlop = CFreq_PostFlop;
        this.PreFlopPlayFreq = PreFlopPlayFreq;
      }
    }

    [Serializable]
    protected class tempHandCache
    {
      //[NonSerialized]
      public long handId;
      public byte gameStage;
      public byte numTableSeats;
      public byte numPlayersDealtIn;
      public bool flushPossible;
      public bool straightPossible;
      public bool aceOnBoard;
      public bool kingOnBoard;
      public decimal aceKingQueenRatio;
      public byte[] positionsLeftToAct;

      public tempHandCache(long handId, byte gameStage, byte numTableSeats, byte numPlayersDealtIn, bool flushPossible, bool straightPossible,
          bool aceOnBoard, bool kingOnBoard, decimal aceKingQueenRatio, byte[] positionsLeftToAct)
      {
        this.handId = handId;
        this.gameStage = gameStage;
        this.numTableSeats = numTableSeats;
        this.numPlayersDealtIn = numPlayersDealtIn;
        this.flushPossible = flushPossible;
        this.straightPossible = straightPossible;
        this.aceOnBoard = aceOnBoard;
        this.kingOnBoard = kingOnBoard;
        this.aceKingQueenRatio = aceKingQueenRatio;
        this.positionsLeftToAct = positionsLeftToAct;
      }
    }

    protected class PlayerActionPrediction
    {
      double probFoldCheck;
      double probCall;
      double probRaise;
      PokerAction predictedAction;
      decimal accuracy;

      public PlayerActionPrediction(double probFoldCheck, double probCall, double probRaise, decimal accuracy)
      {
        this.probFoldCheck = probFoldCheck;
        this.probCall = probCall;
        this.probRaise = probRaise;

        double[] probabilities = new double[] { probFoldCheck, probCall, probRaise };

        probabilities = (from
                     predictions in probabilities
                         orderby predictions descending
                         select predictions).ToArray();

        this.accuracy = accuracy;

        double maxProbability = probFoldCheck;
        predictedAction = PokerAction.Check;

        if (probCall > maxProbability)
        {
          maxProbability = probCall;
          predictedAction = PokerAction.Call;
        }

        if (probRaise > maxProbability)
          predictedAction = PokerAction.Raise;

      }

      public double ProbFoldCheck
      {
        get { return probFoldCheck; }
      }

      public double ProbCall
      {
        get { return probCall; }
      }

      public double ProbRaise
      {
        get { return probRaise; }
      }

      public PokerAction PredictedAction
      {
        get { return predictedAction; }
      }

      public decimal Accuracy
      {
        get { return accuracy; }
      }
    }
    #endregion PAP Protected Classes

    /// <summary>
    /// Predict a players next action using the current cache reference.
    /// </summary>
    /// <param name="handId"></param>
    /// <param name="playerId"></param>
    /// <returns></returns>
    protected PlayerActionPrediction PredictPlayerAction(long handId, long predictPlayerId, decimal numActivePlayers,
        decimal numUnactedPlayers, List<byte> simulatedFoldPositions, decimal betsToCall, decimal minCallAmount, decimal totalNumCalls, decimal totalNumRaises, decimal totalPotAmount, bool logNetworkInputs, PokerPlayerNNModelv1 playerActionNN)
    {
      //Load the network.
      decimal networkAccuracy = 0;
      //playerActionNN.Network = networkManager.getPlayerNetwork(predictPlayerId, ref networkAccuracy);

      if (tempLocalHandCache.handId != handId)
        throw new Exception("The tempLocalHandCache has not been updated.");

      Dictionary<string, decimal> networkInputs = new Dictionary<string, decimal>();

      #region compileNetworkInputs
      decimal playerCurrentRoundBetAmount;
      decimal totalPlayerMoneyInPot;
      decimal playerTotalStack;
      bool playerLastRoundCall;
      bool playerLastRoundRaise;
      decimal RFreq_PostFlop, CFreq_PostFlop, PreFlopPlayFreq;

      if (tempLocalPlayerCacheDict.Count != 0)
      {
        tempPlayerCache playerDetails;

        if (tempLocalPlayerCacheDict.ContainsKey(predictPlayerId))
          playerDetails = tempLocalPlayerCacheDict[predictPlayerId];
        else
          throw new Exception("Played not found - The tempLocalPlayerCache should be built before PredictPlayerAction is called.");

        playerCurrentRoundBetAmount = playerDetails.currentRoundPlayerBetAmount;
        totalPlayerMoneyInPot = playerDetails.totalPlayerMoneyInPot;
        playerLastRoundCall = playerDetails.playerLastRoundCall;
        playerLastRoundRaise = playerDetails.playerLastRoundRaise;
        playerTotalStack = playerDetails.playerStack;
        RFreq_PostFlop = playerDetails.RFreq_PostFlop;
        CFreq_PostFlop = playerDetails.CFreq_PostFlop;
        PreFlopPlayFreq = playerDetails.PreFlopPlayFreq;
      }
      else
        throw new Exception("No entries - The tempLocalPlayerCache should be built before PredictPlayerAction is called.");

      decimal imPotOdds;
      if (minCallAmount > 0)
      {
        //The immediatePotOdds will always be larger than 1 al the way to 10,000 (assuming $100 pot size and a $0.01 call amount)
        //Most immedaitePotOdds seem to fall between 1 and 10 (i.e. if a pot already has $5 in it the minimum expected call will be around $0.50)
        //A @imPotOdds = 1 means the pot odds are very good i.e. better than 10:1 
        //A @imPotOdds = 0 means the pot odds are crap i.e. 1:1
        //cache.getPlayerCurrentRoundBetAmount(predictPlayerId, out playerCurrentRounBetAmount);
        decimal useThisCallAmount;
        if (minCallAmount > playerTotalStack + playerCurrentRoundBetAmount)
          useThisCallAmount = playerTotalStack + playerCurrentRoundBetAmount;
        else
          useThisCallAmount = minCallAmount;

        imPotOdds = ((totalPotAmount / (useThisCallAmount - playerCurrentRoundBetAmount)) / 10.0m) - 0.1m;
        if (imPotOdds > 1)
          imPotOdds = 1;
        else if (imPotOdds < 0)
          throw new Exception("Error in predictPlayerAction. ImmediatePotOdds should never be negative. TotalPot: " + totalPotAmount.ToString() + ", MinCallAmount: " + minCallAmount.ToString() + ", PlayerCurrentRoundBetAmount:" + playerCurrentRoundBetAmount.ToString());
      }
      else
        imPotOdds = 1;

      networkInputs.Add("ImPotOdds", imPotOdds);

      //Compile all inputs
      decimal raiseRatio = 0;
      if (totalNumRaises + totalNumCalls > 0)
        raiseRatio = totalNumRaises / (totalNumRaises + totalNumCalls);

      networkInputs.Add("RaiseRatio", raiseRatio);
      networkInputs.Add("PotRatio", totalPlayerMoneyInPot / totalPotAmount); // Own Money In Pot / Total Amount In Pot

      //Bets to Call
      decimal betsToCall0 = 0, betsToCall1 = 0, betsToCall2Greater = 0;
      if (betsToCall == 0)
        betsToCall0 = 1;
      else if (betsToCall == 1)
        betsToCall1 = 1;
      else if (betsToCall > 1)
        betsToCall2Greater = 1;
      else
        throw new Exception("Meh!");

      networkInputs.Add("BetsToCall0", betsToCall0);
      networkInputs.Add("BetsToCall1", betsToCall1);
      networkInputs.Add("BetsToCall2Greater", betsToCall2Greater);

      //Gamestage
      byte gameStage = tempLocalHandCache.gameStage;
      decimal gameStage0 = 0, gameStage1 = 0, gameStage2 = 0, gameStage3 = 0;
      if (gameStage == 0)
        gameStage0 = 1;
      else if (gameStage == 1)
        gameStage1 = 1;
      else if (gameStage == 2)
        gameStage2 = 1;
      else if (gameStage == 3)
        gameStage3 = 1;
      else
        throw new Exception("Meh!");

      networkInputs.Add("GameStage0", gameStage0);
      networkInputs.Add("GameStage1", gameStage1);
      networkInputs.Add("GameStage2", gameStage2);
      networkInputs.Add("GameStage3", gameStage3);

      networkInputs.Add("PlayerLastRoundCall", Convert.ToDecimal(playerLastRoundCall));
      networkInputs.Add("PlayerLastRoundRaise", Convert.ToDecimal(playerLastRoundRaise));

      networkInputs.Add("DealtInPlayersRatio", tempLocalHandCache.numPlayersDealtIn / tempLocalHandCache.numTableSeats);
      networkInputs.Add("ActivePlayersRatio", numActivePlayers / tempLocalHandCache.numPlayersDealtIn);
      networkInputs.Add("UnactedPlayersRatio", numUnactedPlayers / numActivePlayers);

      networkInputs.Add("FlushPossible", Convert.ToDecimal(tempLocalHandCache.flushPossible));
      networkInputs.Add("StraightPossible", Convert.ToDecimal(tempLocalHandCache.straightPossible));
      networkInputs.Add("AceOnBoard", Convert.ToDecimal(tempLocalHandCache.aceOnBoard));
      networkInputs.Add("KingOnBoard", Convert.ToDecimal(tempLocalHandCache.kingOnBoard));
      networkInputs.Add("AceKingQueenRatio", tempLocalHandCache.aceKingQueenRatio);

      ////This is wrong and not going to work as we don't know where the simulated folded players sit!!
      //float dealerDistance = (cache.getActivePlayerDistanceToDealer(predictPlayerId) - (float)numSimulatedFoldPlayers) - 1  / ((float)numActivePlayers - 1); 
      //Distance to dealer for this player
      networkInputs.Add("DealerDistance", 0);

      //Some aggression information
      networkInputs.Add("PreFlopPlayFreq", PreFlopPlayFreq);
      networkInputs.Add("CFreq_PostFlop", CFreq_PostFlop);
      networkInputs.Add("RFreq_PostFlop", RFreq_PostFlop);

      #endregion

      NNDataSource networkInputSource = new NNDataSource(networkInputs.Values.ToArray(), PokerPlayerNNModelv1.Input_Neurons);

      //Get the network output
      double[] networkInput = null;
      networkInputSource.returnInput(ref networkInput);
      double[] networkOutput = networkManager.getPlayerNetworkPrediction(predictPlayerId, networkInput, ref networkAccuracy);
      PlayerActionPrediction predictedAction = new PlayerActionPrediction(networkOutput[0], networkOutput[1], networkOutput[2], networkAccuracy);

#if logging
            logger.Debug("PPA," + handId + ", " + predictPlayerId + ",, " + networkInputSource.ToString()+ ",, " + GenerateMD5(networkManager.GetDefaultNetwork()) + ", ," + networkOutput[0] + ", ," + networkOutput[1] + ", ," + networkOutput[2]);
#endif

      //Return as PlayerActionPrediction struct
      return predictedAction;
    }

    /// <summary>
    /// Build the local provider player cache copy so that references to the main cache are required only once.
    /// DealerIndex position is at the beginning
    /// </summary>
    private void BuildTempPlayerCache()
    {
      long currentPlayerId;
      byte currentPlayerPosition;
      decimal currentPlayerStack;
      decimal currentRoundBetAmount;
      decimal currentPlayerTotalMoneyInPot;
      byte[] activePositions = decisionRequest.Cache.getActivePositions(decisionRequest.Cache.getCurrentHandDetails().dealerPosition);
      List<PokerAction> lastRoundPlayerActions;

      tempLocalPlayerCacheDict = new Dictionary<long, tempPlayerCache>();

      //Need access to the agression provider
      //This should never fail because the aggressionProvider has to be included in order to add PAP
      //AggressionProvider aggressionProvider = (AggressionProvider)allInformationProviders[InfoProviderType.AIAggression];

      //For each player we need to build the localcache
      for (int i = 0; i < activePositions.Length; i++)
      {
        currentPlayerPosition = activePositions[i];
        currentPlayerId = decisionRequest.Cache.getPlayerId(currentPlayerPosition);
        currentRoundBetAmount = decisionRequest.Cache.getPlayerCurrentRoundBetAmount(currentPlayerId);
        currentPlayerTotalMoneyInPot = decisionRequest.Cache.getTotalPlayerMoneyInPot(currentPlayerId);
        currentPlayerStack = decisionRequest.Cache.getPlayerStack(currentPlayerId);
        lastRoundPlayerActions = decisionRequest.Cache.getPlayerLastRoundActions(currentPlayerId);

        decimal RFreq_PreFlop = 0, RFreq_PostFlop = 0, CFreq_PreFlop = 0, CFreq_PostFlop = 0, CheckFreq_PreFlop = 0, CheckFreq_PostFlop = 0, PreFlopPlayFreq = 0, PostFlopPlayFreq = 0;
        byte aggressionDataLevel = AggressionProvider.PlayerAggressionMetricFromCache(currentPlayerId, ref RFreq_PreFlop, ref RFreq_PostFlop, ref CFreq_PreFlop, ref CFreq_PostFlop, ref CheckFreq_PreFlop, ref CheckFreq_PostFlop, ref PreFlopPlayFreq, ref PostFlopPlayFreq);

        tempLocalPlayerCacheDict.Add(currentPlayerId, new tempPlayerCache(currentPlayerId, decisionRequest.Cache.getCurrentHandId(), currentPlayerStack,
            currentPlayerPosition, currentPlayerTotalMoneyInPot, currentRoundBetAmount,
            lastRoundPlayerActions.Contains(PokerAction.Call), lastRoundPlayerActions.Contains(PokerAction.Raise), RFreq_PreFlop, RFreq_PostFlop, CFreq_PreFlop, CFreq_PostFlop, PreFlopPlayFreq));
        //AggressionProvider.DEFAULT_rFREQ_PREFLOP,
        //AggressionProvider.DEFAULT_rFREQ_POSTFLOP,
        //AggressionProvider.DEFAULT_cFREQ_PREFLOP, 
        //AggressionProvider.DEFAULT_cFREQ_POSTFLOP,
        //AggressionProvider.DEFAULT_pFREQ_PREFLOP));
      }
    }

    /// <summary>
    /// Build the local provider cache copy so that references to the main cache are required only once (also builds temp playerCache).
    /// </summary>
    protected void BuildTempCache()
    {
      byte gameStage = (byte)infoStore.GetInfoValue(InfoType.GP_GameStage_Byte);
      byte numTableSeats = (byte)infoStore.GetInfoValue(InfoType.GP_NumTableSeats_Byte);
      byte numPlayersDealtIn = (byte)infoStore.GetInfoValue(InfoType.GP_NumPlayersDealtIn_Byte);
      bool flushPossible = Convert.ToBoolean(infoStore.GetInfoValue(InfoType.CP_FlushPossible_Bool));
      bool straightPossible = Convert.ToBoolean(infoStore.GetInfoValue(InfoType.CP_StraightPossible_Bool));
      bool aceOnBoard = Convert.ToBoolean(infoStore.GetInfoValue(InfoType.CP_AOnBoard_Bool));
      bool kingOnBoard = Convert.ToBoolean(infoStore.GetInfoValue(InfoType.CP_KOnBoard_Bool));
      decimal aceKingQueenRatio = infoStore.GetInfoValue(InfoType.CP_AKQToBoardRatio_Real);
      byte[] positionsLeftToAct = decisionRequest.Cache.getActivePositionsLeftToAct();

      tempLocalHandCache = new tempHandCache(decisionRequest.Cache.getCurrentHandId(), gameStage, numTableSeats, numPlayersDealtIn, flushPossible, straightPossible, aceOnBoard,
          kingOnBoard, aceKingQueenRatio, positionsLeftToAct);

    }

    /// <summary>
    /// Works through the probability tree of possible player actions and sums the paths where 
    /// the number of callers or raisers in that path is equal to or greater than numCallersRequired
    /// </summary>
    /// <param name="numCallersRequired"></param>
    /// <param name="?"></param>
    protected void TraverseActionProbTree(ref decimal totalProbRequiredActionAchieved,
                                        decimal pathProb,
                                        long handId,
                                        byte[] positionsLeftToAct,
                                        byte playerPositionIndex,
                                        PokerAction actionToCount,
                                        byte minActionCountRequired,
                                        byte currentActionCount,
                                        byte numActivePlayers,
                                        byte numUnactedPlayers,
                                        List<byte> simulatedFoldPositions,
                                        byte betsToCall,
                                        decimal callAmount,
                                        byte totalNumCalls,
                                        byte totalNumRaises,
                                        decimal totalPotAmount,
                                        PokerPlayerNNModelv1 playerActionNN)
    {
      decimal tempPathProb;
      PlayerActionPrediction predictedAction;

      long currentPlayerId = decisionRequest.Cache.getPlayerId(positionsLeftToAct[playerPositionIndex]);

      if (!tempLocalPlayerCacheDict.ContainsKey(currentPlayerId))
        throw new Exception("Player information not stored in local cache. Make sure to call BuildTempPlayerCache().");

      //We need to check to see if this player is all in
      //If this player is all in we will just ignore them for now and try to move on
      if (tempLocalPlayerCacheDict[currentPlayerId].playerStack == 0)
      {
        do
        {
          playerPositionIndex++;

          if (playerPositionIndex >= positionsLeftToAct.Count())
          {
            playerPositionIndex--;
            break;
          }

          currentPlayerId = decisionRequest.Cache.getPlayerId(positionsLeftToAct[playerPositionIndex]);
          if (!tempLocalPlayerCacheDict.ContainsKey(currentPlayerId))
            throw new Exception("Player information not stored in local cache. Make sure to call BuildTempPlayerCache().");

        } while (tempLocalPlayerCacheDict[currentPlayerId].playerStack == 0);

        //If the last player is all in just return as it wont affect the probabilities at this section of the tree.
        if (tempLocalPlayerCacheDict[currentPlayerId].playerStack == 0)
        {
          if (currentActionCount >= minActionCountRequired)
            totalProbRequiredActionAchieved += pathProb;

          return;
        }
      }

      predictedAction = PredictPlayerAction(handId,
          currentPlayerId,
          numActivePlayers,
          numUnactedPlayers,
          simulatedFoldPositions,
          betsToCall,
          callAmount,
          totalNumCalls,
          totalNumRaises,
          totalPotAmount,
          false,
          playerActionNN);

      //If we predict a raise we just return this path now
      if (predictedAction.PredictedAction == PokerAction.Raise)
      {
        if (currentActionCount + 1 >= minActionCountRequired)
          totalProbRequiredActionAchieved += pathProb * predictedAction.Accuracy;
        else
          //This line will rarely be used as we will only ever be looking for 1 raiser or more!
          totalProbRequiredActionAchieved += pathProb * ((1 - predictedAction.Accuracy));

        return;
      }

      //If we have reached the end of the tree
      if (positionsLeftToAct.Count() == playerPositionIndex + 1)
      {
        if (actionToCount == PokerAction.Call)
        {

          //If we have already achieved the correct number of action counts at this point
          //We don't care about the current prediction action
          if (currentActionCount >= minActionCountRequired)
            totalProbRequiredActionAchieved += pathProb;
          else
          {
            //If we predicted a call then increment the current action count.
            if (predictedAction.PredictedAction == PokerAction.Call)
              currentActionCount++;

            //Right, have we now hit the necessary minimum??
            if (currentActionCount >= minActionCountRequired)
              totalProbRequiredActionAchieved += pathProb * predictedAction.Accuracy;
            else if (positionsLeftToAct.Count() == 1)
            {
              //The only exception is if we are predicting for a single player
              if (predictedAction.PredictedAction == PokerAction.Call)
                totalProbRequiredActionAchieved += pathProb * predictedAction.Accuracy;
              else
                totalProbRequiredActionAchieved += pathProb * (1 - predictedAction.Accuracy);
            }
            else if (currentActionCount + 1 >= minActionCountRequired)
              totalProbRequiredActionAchieved += pathProb * (1 - predictedAction.Accuracy);
          }

        }
        else if (actionToCount == PokerAction.Raise)
        {
          //If a raise was predicted it would have been returned above
          //If we have reached this point we have not predicted a raise anywhere along the line
          totalProbRequiredActionAchieved += pathProb * (1 - predictedAction.Accuracy);
        }
        else
          throw new Exception("We should not be counting any other action at the moment.");

      }
      else
      {
        //We are in the middle of the tree and need to get REALLY FUCKING FANCY!!!

        //Call down the tree as if this player called
        //If we predicted a call then we modify the pathProb with *= confidence
        //If we predicted a fold then we modify the pathProb with *= 1- confidence
        decimal newPathMultipler;

        if (actionToCount == PokerAction.Raise)
          newPathMultipler = (1 - predictedAction.Accuracy);
        else
          newPathMultipler = (predictedAction.PredictedAction == PokerAction.Call ? predictedAction.Accuracy : (1 - predictedAction.Accuracy));

        tempPathProb = pathProb * newPathMultipler;

        TraverseActionProbTree(ref totalProbRequiredActionAchieved,
            tempPathProb,
            handId,
            positionsLeftToAct,
            (byte)(playerPositionIndex + 1),
            actionToCount,
            minActionCountRequired,
            (byte)(currentActionCount + 1),
            numActivePlayers,
            (byte)(numUnactedPlayers - 1),
            simulatedFoldPositions,
            (byte)(betsToCall + 1),
            callAmount,
            (byte)(totalNumCalls + 1),
            totalNumRaises,
            totalPotAmount + callAmount,
            playerActionNN);

        newPathMultipler = (predictedAction.PredictedAction == PokerAction.Check ? predictedAction.Accuracy : (1 - predictedAction.Accuracy));
        tempPathProb = pathProb * newPathMultipler;
        simulatedFoldPositions.Add(positionsLeftToAct.ElementAt(playerPositionIndex));

        //Call down the tree as if this player folded
        TraverseActionProbTree(ref totalProbRequiredActionAchieved,
            tempPathProb,
            handId,
            positionsLeftToAct,
            (byte)(playerPositionIndex + 1),
            actionToCount,
            minActionCountRequired,
            currentActionCount,
            (byte)(numActivePlayers - 1),
            (byte)(numUnactedPlayers - 1),
            simulatedFoldPositions,
            betsToCall,
            callAmount,
            totalNumCalls,
            totalNumRaises,
            totalPotAmount,
            playerActionNN);

        simulatedFoldPositions.Remove(positionsLeftToAct.ElementAt(playerPositionIndex));
      }

    }

    //Returns the nextRaiseAmount which should be tried for the purposes of raiseToCall and raiseToSteal
    protected decimal NextRaiseAmount(decimal minimumRaiseAmount, List<decimal> lastRaiseAmounts, bool nextAmountGreater)
    {
      decimal maximumRaiseAmount = decisionRequest.Cache.getPlayerStack(decisionRequest.PlayerId);

      decimal lastRaiseAmount = 0;

      if (lastRaiseAmounts.Count() > 0)
        lastRaiseAmount = lastRaiseAmounts.Last();

      if (lastRaiseAmount == maximumRaiseAmount && nextAmountGreater)
        return maximumRaiseAmount + 0.01m;

      if (lastRaiseAmount == minimumRaiseAmount && !nextAmountGreater)
        return minimumRaiseAmount - 0.01m;

      if (lastRaiseAmounts.Count() == 0)
        return maximumRaiseAmount;
      else if (lastRaiseAmounts.Count() == 1)
        return minimumRaiseAmount;
      else if (lastRaiseAmounts.Count() == 2)
        return (maximumRaiseAmount - minimumRaiseAmount) / 2;
      else
      {
        decimal previousRelevantAmount = 0;

        if (lastRaiseAmount == minimumRaiseAmount && !nextAmountGreater)
          return minimumRaiseAmount - 0.01m;

        if (nextAmountGreater)
        {
          //If higher we need to work out what was greater than the lastRaiseAmount
          for (int i = (lastRaiseAmounts.Count() - 1); i >= 0; i--)
            if (lastRaiseAmounts.ElementAt(i) > lastRaiseAmount)
            {
              previousRelevantAmount = lastRaiseAmounts[i];
              break;
            }

          return ((previousRelevantAmount - lastRaiseAmount) / 2) + previousRelevantAmount;
        }
        else
        {
          //If lower we need to work out what was the lower than the currentRaiseAmount
          for (int i = (lastRaiseAmounts.Count() - 1); i >= 0; i--)
            if (lastRaiseAmounts[i] < lastRaiseAmount)
            {
              previousRelevantAmount = lastRaiseAmounts[i];
              break;
            }

          return ((lastRaiseAmount - previousRelevantAmount) / 2) + previousRelevantAmount;
        }

      }
    }

    #endregion sharedMethods
  }
}
