using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using Encog;
using PokerBot.AI.InfoProviders;
using PokerBot.Definitions;

namespace PokerBot.AI.Neural.Version7
{
  /// <summary>
  /// Stores the AI NN outputs.
  /// </summary>
  internal class NeuralAiDecisionNeuralv7
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

    public NeuralAiDecisionNeuralv7(double[] networkOutputsIn, bool enableStochasticism, double stochasticDouble)
    {
      this.networkOutputs = networkOutputsIn.ToArray();
      this.stochasticDouble = stochasticDouble;

      if (networkOutputs.Length != 5)
        throw new NotImplementedException("Relative output scaling currently only supported for neuralV4");

      //Only the first 4 outputs make the decision, the last one is raise amount
      double maxValue = networkOutputs.Take(4).Max();

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
          //networkOutputs[4] -= halfMaxValue;

          //Set any negative outputs to zero
          if (networkOutputs[0] < 0)
            networkOutputs[0] = 0;
          if (networkOutputs[1] < 0)
            networkOutputs[1] = 0;
          if (networkOutputs[2] < 0)
            networkOutputs[2] = 0;
          if (networkOutputs[3] < 0)
            networkOutputs[3] = 0;
          //if (networkOutputs[4] < 0) networkOutputs[4] = 0;

          //Rescale networkOutputs so everything adds up to 1
          double arrayScaleMultiplier = 1.0 / networkOutputs.Take(4).Sum();
          networkOutputs[0] *= arrayScaleMultiplier;
          networkOutputs[1] *= arrayScaleMultiplier;
          networkOutputs[2] *= arrayScaleMultiplier;
          networkOutputs[3] *= arrayScaleMultiplier;
          //networkOutputs[4] *= arrayScaleMultiplier;

          //Choose one of the outputs based on the stochastic double
          double runningTotal = 0;
          for (short i = 0; i < networkOutputs.Length - 1; i++)
          {
            runningTotal += networkOutputs[i];
            if (stochasticDouble < runningTotal)
            {
              botAction = i;
              break;
            }
          }

          if (botAction == 3)
          {
            //if (networkOutputs[4] > 1) networkOutputs[4] = 1;
            if (networkOutputs[4] < 0)
              networkOutputs[4] = 0;

            ScaledAdditionalRaiseAmount = networkOutputs[4];
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

              if (botAction == 3)
              {
                //if (networkOutputs[4] > 1) networkOutputs[4] = 1;
                if (networkOutputs[4] < 0)
                  networkOutputs[4] = 0;

                ScaledAdditionalRaiseAmount = networkOutputs[4];
              }

              break;
            }
          }
        }
      }
      else
        //If the network output all zeros then we fold
        botAction = 0;
    }

    public NeuralAiDecisionNeuralv7(double[] networkOutputs)
    {
      throw new NotImplementedException();

      //this.networkOutputs = networkOutputs;

      //short tempAction = 0;
      //double tempMaxValue = networkOutputs[0];

      //for (short i = 0; i < networkOutputs.Length; i++)
      //{
      //    if (networkOutputs[i] > tempMaxValue)
      //    {
      //        tempAction = i;
      //        tempMaxValue = networkOutputs[i];
      //    }
      //}

      //botAction = tempAction;
    }

    public NeuralAiDecisionNeuralv7(double actCheckFold, double actCall, double actRaiseToCall, double actRaiseToSteal, double actAllIn)
    {
      throw new NotImplementedException();
      //this.actCheckFold = actCheckFold;
      //this.actCall = actCall;
      //this.actRaiseToCall = actRaiseToCall;
      //this.actRaiseToSteal = actRaiseToSteal;
      //this.actAllIn = actAllIn;

      ////We need to work out which the largest value is
      //short tempAction = 0;
      //double tempMaxValue = actCheckFold;

      //if (actCall > tempMaxValue)
      //{
      //    tempAction = 1;
      //    tempMaxValue = actCall;
      //}

      //if (actRaiseToCall > tempMaxValue)
      //{
      //    tempAction = 2;
      //    tempMaxValue = actRaiseToCall;
      //}

      //if (actRaiseToSteal > tempMaxValue)
      //{
      //    tempAction = 3;
      //    tempMaxValue = actRaiseToSteal;
      //}

      //if (actAllIn > tempMaxValue)
      //{
      //    tempAction = 4;
      //    tempMaxValue = actAllIn;
      //}

      //botAction = tempAction;
    }

    public double ScaledAdditionalRaiseAmount { get; private set; }
  }

  internal class NeuralAIv7 : NeuralAIBase
  {
    NeuralAINNModelV7 neuralPokerAI = new NeuralAINNModelV7();

#if logging
        //Added for testing, can be removed
        static object locker = new object();
        System.Security.Cryptography.MD5CryptoServiceProvider md5 = new System.Security.Cryptography.MD5CryptoServiceProvider();
        System.Runtime.Serialization.Formatters.Binary.BinaryFormatter bin = new System.Runtime.Serialization.Formatters.Binary.BinaryFormatter();
        System.IO.MemoryStream mem = new System.IO.MemoryStream();
#endif

    RequestedInfoKey postFlopUpdateKey;

    public NeuralAIv7(AIRandomControl aiRandomControl)
        : base(aiRandomControl)
    {
      aiType = AIGeneration.NeuralV7;

      #region preflop update key
      //Setup this AI's specific update key
      specificUpdateKey = new RequestedInfoKey(false);

      specificUpdateKey.SetInfoTypeRequired(InfoType.GP_GameStage_Byte);

      specificUpdateKey.SetInfoTypeRequired(InfoType.CP_HoleCardsAAPair_Bool);
      specificUpdateKey.SetInfoTypeRequired(InfoType.CP_HoleCardsKKPair_Bool);
      specificUpdateKey.SetInfoTypeRequired(InfoType.CP_HoleCardsAK_Bool);
      specificUpdateKey.SetInfoTypeRequired(InfoType.CP_HoleCardsOtherHighPair_Bool);
      specificUpdateKey.SetInfoTypeRequired(InfoType.CP_HoleCardsOtherLowPair_Bool);
      specificUpdateKey.SetInfoTypeRequired(InfoType.CP_HoleCardsTroubleHand_Bool);
      specificUpdateKey.SetInfoTypeRequired(InfoType.CP_HoleCardsMidConnector_Bool);
      specificUpdateKey.SetInfoTypeRequired(InfoType.CP_HoleCardsLowConnector_Bool);
      specificUpdateKey.SetInfoTypeRequired(InfoType.CP_HoleCardsSuited_Bool);

      specificUpdateKey.SetInfoTypeRequired(InfoType.GP_DealerDistance_Byte);
      specificUpdateKey.SetInfoTypeRequired(InfoType.GP_NumActivePlayers_Byte);
      specificUpdateKey.SetInfoTypeRequired(InfoType.GP_NumUnactedPlayers_Byte);

      specificUpdateKey.SetInfoTypeRequired(InfoType.BP_PlayerMoneyInPot_Decimal);
      specificUpdateKey.SetInfoTypeRequired(InfoType.BP_PlayerHandStartingStackAmount_Decimal);
      specificUpdateKey.SetInfoTypeRequired(InfoType.BP_PlayerBetAmountCurrentRound_Decimal);
      specificUpdateKey.SetInfoTypeRequired(InfoType.BP_TotalPotAmount_Decimal);
      specificUpdateKey.SetInfoTypeRequired(InfoType.BP_MinimumPlayAmount_Decimal);

      specificUpdateKey.SetInfoTypeRequired(InfoType.WR_ProbOpponentHasBetterWRFIXED);
      specificUpdateKey.SetInfoTypeRequired(InfoType.WR_RaiseToCallAmount);
      specificUpdateKey.SetInfoTypeRequired(InfoType.WR_RaiseToStealAmount);
      specificUpdateKey.SetInfoTypeRequired(InfoType.WR_RaiseToCallStealSuccessProb);
      specificUpdateKey.SetInfoTypeRequired(InfoType.WR_RaiseToStealSuccessProb);

      specificUpdateKey.SetInfoTypeRequired(InfoType.AP_AvgLiveOppPreFlopPlayFreq_Double);
      specificUpdateKey.SetInfoTypeRequired(InfoType.AP_AvgLiveOppPostFlopPlayFreq_Double);
      specificUpdateKey.SetInfoTypeRequired(InfoType.AP_AvgLiveOppCurrentRoundAggr_Double);
      specificUpdateKey.SetInfoTypeRequired(InfoType.AP_AvgLiveOppCurrentRoundAggrAcc_Double);
      #endregion

      #region postflop update key
      postFlopUpdateKey = new RequestedInfoKey(false);

      postFlopUpdateKey.SetInfoTypeRequired(InfoType.GP_GameStage_Byte);

      postFlopUpdateKey.SetInfoTypeRequired(InfoType.GP_DealerDistance_Byte);
      postFlopUpdateKey.SetInfoTypeRequired(InfoType.GP_NumActivePlayers_Byte);
      postFlopUpdateKey.SetInfoTypeRequired(InfoType.GP_NumUnactedPlayers_Byte);

      postFlopUpdateKey.SetInfoTypeRequired(InfoType.BP_PlayerMoneyInPot_Decimal);
      postFlopUpdateKey.SetInfoTypeRequired(InfoType.BP_PlayerHandStartingStackAmount_Decimal);
      postFlopUpdateKey.SetInfoTypeRequired(InfoType.BP_PlayerBetAmountCurrentRound_Decimal);
      postFlopUpdateKey.SetInfoTypeRequired(InfoType.BP_TotalPotAmount_Decimal);
      postFlopUpdateKey.SetInfoTypeRequired(InfoType.BP_MinimumPlayAmount_Decimal);

      postFlopUpdateKey.SetInfoTypeRequired(InfoType.WR_ProbOpponentHasBetterWRFIXED);
      postFlopUpdateKey.SetInfoTypeRequired(InfoType.WR_RaiseToCallAmount);
      postFlopUpdateKey.SetInfoTypeRequired(InfoType.WR_RaiseToStealAmount);
      postFlopUpdateKey.SetInfoTypeRequired(InfoType.WR_RaiseToCallStealSuccessProb);
      postFlopUpdateKey.SetInfoTypeRequired(InfoType.WR_RaiseToStealSuccessProb);

      postFlopUpdateKey.SetInfoTypeRequired(InfoType.AP_AvgLiveOppPreFlopPlayFreq_Double);
      postFlopUpdateKey.SetInfoTypeRequired(InfoType.AP_AvgLiveOppPostFlopPlayFreq_Double);
      postFlopUpdateKey.SetInfoTypeRequired(InfoType.AP_AvgLiveOppCurrentRoundAggr_Double);
      postFlopUpdateKey.SetInfoTypeRequired(InfoType.AP_AvgLiveOppCurrentRoundAggrAcc_Double);
      #endregion
    }

    protected override Dictionary<InfoType, string> GetInfoUpdateConfigs()
    {
      defaultInfoTypeUpdateConfigs = new Dictionary<InfoType, string>();
      return defaultInfoTypeUpdateConfigs;
    }

    protected override RequestedInfoKey GetUpdateKeyOrPreDecision()
    {
      if (currentDecision.Cache.getBettingRound() == 0)
        return specificUpdateKey;
      else
        return postFlopUpdateKey;
    }

    protected override Play GetDecision()
    {
      Dictionary<string, decimal> networkInputs = new Dictionary<string, decimal>();

      //Some values need to be moved the top because they are used in multiple places
      decimal currentRoundMinPlayCallAmount = currentDecision.Cache.getMinimumPlayAmount();
      decimal additionalCallAmount = currentRoundMinPlayCallAmount - infoStore.GetInfoValue(InfoType.BP_PlayerBetAmountCurrentRound_Decimal);

      //Determine the minimumRaiseToAmount
      decimal lastAdditionalRaiseAmount = currentDecision.Cache.getCurrentRoundLastRaiseAmount();
      decimal minimumRaiseToAmount = (currentRoundMinPlayCallAmount - lastAdditionalRaiseAmount) + (lastAdditionalRaiseAmount * 2);

      decimal currentPotAmount = infoStore.GetInfoValue(InfoType.BP_TotalPotAmount_Decimal);
      decimal remainingPlayerStack = currentDecision.Cache.getPlayerStack(currentDecision.PlayerId);
      long[] activePlayerIds = currentDecision.Cache.getActivePlayerIds();

      //Current Round Actions
      List<PokerAction> currentRoundPlayerActions = (from current in currentDecision.Cache.getPlayerCurrentRoundActions(currentDecision.PlayerId)
                                                     where current == PokerAction.Check ||
                                                     current == PokerAction.Call ||
                                                     current == PokerAction.Raise
                                                     select current).ToList();

      #region RaiseAmounts
      decimal raiseToCallAmountNew, raiseToStealAmountNew;

      //If we are preflop we calculate the raise amounts in a slightly more fixed fashion
      if (currentDecision.Cache.getBettingRound() == 0)
      {
        double randomNumber = randomGen.NextDouble();

        //If the pot is unraised
        if (infoStore.GetInfoValue(InfoType.BP_MinimumPlayAmount_Decimal) == currentDecision.Cache.BigBlind)
        {
          if (randomNumber > 0.8)
            raiseToCallAmountNew = 4m * currentDecision.Cache.BigBlind;
          else if (randomNumber > 0.4)
            raiseToCallAmountNew = 3.5m * currentDecision.Cache.BigBlind;
          else
            raiseToCallAmountNew = 3.0m * currentDecision.Cache.BigBlind;
        }
        else
        {
          //decimal currentRoundLastRaiseAmount = infoStore.GetInfoValue(InfoType.BP_LastAdditionalRaiseAmount);
          decimal additionalNewRaiseAmount;

          if (randomNumber > 0.9)
            additionalNewRaiseAmount = 2 * lastAdditionalRaiseAmount;
          else if (randomNumber > 0.45)
            additionalNewRaiseAmount = 1.5m * lastAdditionalRaiseAmount;
          else
            additionalNewRaiseAmount = 1 * lastAdditionalRaiseAmount;

          raiseToCallAmountNew = additionalNewRaiseAmount + infoStore.GetInfoValue(InfoType.BP_MinimumPlayAmount_Decimal);
        }

        raiseToStealAmountNew = 1.5m * raiseToCallAmountNew;

        //We will only scale the raiseToCallAmount if it is not a bigblind multiple
        if (((int)(raiseToCallAmountNew / currentDecision.Cache.BigBlind)) * currentDecision.Cache.BigBlind != raiseToCallAmountNew)
        {
          //Round the raiseToCallAmount
          decimal raiseToCallAmountBlindMultiple = raiseToCallAmountNew / currentDecision.Cache.LittleBlind;
          raiseToCallAmountNew = Math.Round(raiseToCallAmountBlindMultiple, 0, MidpointRounding.AwayFromZero) * currentDecision.Cache.LittleBlind;
        }

        //Round the raiseToStealAmount
        decimal raiseToStealAmountBlindMultiple = raiseToStealAmountNew / currentDecision.Cache.LittleBlind;
        raiseToStealAmountNew = Math.Round(raiseToStealAmountBlindMultiple, 0, MidpointRounding.AwayFromZero) * currentDecision.Cache.LittleBlind;
      }
      else
      {
        raiseToCallAmountNew = (decimal)infoStore.GetInfoValue(InfoType.WR_RaiseToCallAmount);
        raiseToStealAmountNew = (decimal)infoStore.GetInfoValue(InfoType.WR_RaiseToStealAmount);
      }

      //Some raise validation
      //Validate we can actually raise the selected amounts
      decimal maximumRaiseAmount = remainingPlayerStack + infoStore.GetInfoValue(InfoType.BP_PlayerBetAmountCurrentRound_Decimal);

      //if (raiseToCallAmount > maximumRaiseAmount) raiseToCallAmount = maximumRaiseAmount;
      //if (raiseToStealAmount > maximumRaiseAmount) raiseToStealAmount = maximumRaiseAmount;

      //Check for a big raise amount which would be best as going all in
      if (raiseToCallAmountNew > 0.8m * maximumRaiseAmount)
        raiseToCallAmountNew = maximumRaiseAmount;
      if (raiseToStealAmountNew > 0.8m * maximumRaiseAmount)
        raiseToStealAmountNew = maximumRaiseAmount;

      //If we have already raised twice this round all amounts are set to all in
      bool raisedTwiceOrGreaterCurrentRound = currentRoundPlayerActions.Count(entry => entry == PokerAction.Raise) >= 2;
      if (raisedTwiceOrGreaterCurrentRound)
      {
        raiseToCallAmountNew = maximumRaiseAmount;
        raiseToStealAmountNew = maximumRaiseAmount;
      }
      #endregion RaiseAmounts

      /////////////////////////////////////////
      ///////////ALL NON BOOLEAN INPUTS BETWEEN 0.9 and 0.1
      /////////////////////////////////////////

      #region neuralInputs

      #region gameStage

      decimal gameStagePreFlop = 0;
      decimal gameStageFlop = 0;
      decimal gameStageTurn = 0;
      decimal gameStageRiver = 0;

      decimal currentGameStage = infoStore.GetInfoValue(InfoType.GP_GameStage_Byte);

      if (currentGameStage == 0)
        gameStagePreFlop = 1;
      else if (currentGameStage == 1)
        gameStageFlop = 1;
      else if (currentGameStage == 2)
        gameStageTurn = 1;
      else if (currentGameStage == 3)
        gameStageRiver = 1;
      else
        throw new Exception("What sort of game stage do you call this??");

      networkInputs.Add("GameStagePreFlop", gameStagePreFlop);
      networkInputs.Add("GameStageFlop", gameStageFlop);
      networkInputs.Add("GameStageTurn", gameStageTurn);
      networkInputs.Add("GameStageRiver", gameStageRiver);

      #endregion gameStage
      //4 inputs

      #region preFlop HoleCards
      if (gameStagePreFlop == 1)
      {
        //Card info types
        networkInputs.Add("HoleCardsAAPair", infoStore.GetInfoValue(InfoType.CP_HoleCardsAAPair_Bool));
        networkInputs.Add("HoleCardsKKPair", infoStore.GetInfoValue(InfoType.CP_HoleCardsKKPair_Bool));
        networkInputs.Add("HoleCardsAKPair", infoStore.GetInfoValue(InfoType.CP_HoleCardsAK_Bool));
        networkInputs.Add("HoleCardsOtherHighPair", infoStore.GetInfoValue(InfoType.CP_HoleCardsOtherHighPair_Bool));
        networkInputs.Add("HoleCardsOtherLowPair", infoStore.GetInfoValue(InfoType.CP_HoleCardsOtherLowPair_Bool));
        networkInputs.Add("HoleCardsTroubleHand", infoStore.GetInfoValue(InfoType.CP_HoleCardsTroubleHand_Bool));
        networkInputs.Add("HoleCardsMidConnector", infoStore.GetInfoValue(InfoType.CP_HoleCardsMidConnector_Bool));
        networkInputs.Add("HoleCardsLowConnector", infoStore.GetInfoValue(InfoType.CP_HoleCardsLowConnector_Bool));
        networkInputs.Add("HoleCardsSuited", infoStore.GetInfoValue(InfoType.CP_HoleCardsSuited_Bool));
      }
      else
      {
        networkInputs.Add("HoleCardsAAPair", 0);
        networkInputs.Add("HoleCardsKKPair", 0);
        networkInputs.Add("HoleCardsAKPair", 0);
        networkInputs.Add("HoleCardsOtherHighPair", 0);
        networkInputs.Add("HoleCardsOtherLowPair", 0);
        networkInputs.Add("HoleCardsTroubleHand", 0);
        networkInputs.Add("HoleCardsMidConnector", 0);
        networkInputs.Add("HoleCardsLowConnector", 0);
        networkInputs.Add("HoleCardsSuited", 0);
      }
      #endregion
      //9 inputs

      #region playerPosition and numPlayers
      decimal dealerDistance = ((infoStore.GetInfoValue(InfoType.GP_DealerDistance_Byte) - 1) / (infoStore.GetInfoValue(InfoType.GP_NumActivePlayers_Byte) - 1));
      decimal tablePositionEarly = 0;
      decimal tablePositionMid = 0;
      decimal tablePositionLate = 0;

      if (dealerDistance < (1.0m / 3.0m) && dealerDistance >= 0)
        tablePositionEarly = 1;
      else if (dealerDistance < (2.0m / 3.0m) && dealerDistance >= 0)
        tablePositionMid = 1;
      else if (dealerDistance <= 1.0m && dealerDistance >= 0)
        tablePositionLate = 1;
      else
        throw new Exception("Dealer distance must be between 0 and 1.");

      networkInputs.Add("TablePositionEarly", tablePositionEarly);
      networkInputs.Add("TablePositionMid", tablePositionMid);
      networkInputs.Add("TablePositionLate", tablePositionLate);

      networkInputs.Add("NumActivePlayers4Plus", (infoStore.GetInfoValue(InfoType.GP_NumActivePlayers_Byte) >= 4 ? 1 : 0));
      networkInputs.Add("NumActivePlayers3", (infoStore.GetInfoValue(InfoType.GP_NumActivePlayers_Byte) == 3 ? 1 : 0));
      networkInputs.Add("NumActivePlayers2", (infoStore.GetInfoValue(InfoType.GP_NumActivePlayers_Byte) == 2 ? 1 : 0));

      networkInputs.Add("LastToAct", (infoStore.GetInfoValue(InfoType.GP_NumUnactedPlayers_Byte) == 1 ? 1 : 0));
      #endregion playerPosition and numPlayers
      //7 inputs

      #region potCommitment and liability
      decimal potCommitted = 0;
      if (infoStore.GetInfoValue(InfoType.BP_PlayerMoneyInPot_Decimal) > infoStore.GetInfoValue(InfoType.BP_PlayerHandStartingStackAmount_Decimal) * 0.75m)
        potCommitted = 1;

      networkInputs.Add("PotCommitted", potCommitted);

      //The maximum liability in our current hand, i.e.
      decimal maximumLiability = 0;
      decimal[] allPlayerStacks = new decimal[activePlayerIds.Length];

      //Determine if anyone has a larger stack than us
      bool stackLargerThanOurs = false;
      for (int i = 0; i < activePlayerIds.Length; i++)
      {
        allPlayerStacks[i] = currentDecision.Cache.getPlayerStack(activePlayerIds[i]);

        if (activePlayerIds[i] != currentDecision.PlayerId && allPlayerStacks[i] > remainingPlayerStack)
        {
          stackLargerThanOurs = true;
          break;
        }
      }

      if (!stackLargerThanOurs)
      {
        //Find the nearest largest stack
        decimal nearestLargestStack = 0;
        for (int i = 0; i < activePlayerIds.Length; i++)
        {
          if (activePlayerIds[i] != currentDecision.PlayerId && allPlayerStacks[i] > nearestLargestStack)
            nearestLargestStack = allPlayerStacks[i];
        }

        maximumLiability = ScaleContInput(nearestLargestStack / currentDecision.Cache.MaxStack);
      }
      else
        maximumLiability = ScaleContInput(remainingPlayerStack / currentDecision.Cache.MaxStack);

      networkInputs.Add("MaximumLiability", maximumLiability);
      #endregion
      //2 inputs

      #region Hand and Action History
      List<PokerAction> allPlayerActionsCurrentHand = currentDecision.Cache.getPlayerActionsCurrentHand(currentDecision.PlayerId);
      decimal callCount = (decimal)allPlayerActionsCurrentHand.Count(entry => entry == PokerAction.Call);
      decimal checkCount = (decimal)allPlayerActionsCurrentHand.Count(entry => entry == PokerAction.Check);
      decimal raiseCount = (decimal)allPlayerActionsCurrentHand.Count(entry => entry == PokerAction.Raise);

      decimal currentHandOurAggression = 0;

      currentHandOurAggression = raiseCount - (callCount + checkCount);
      if (currentHandOurAggression < -6)
        currentHandOurAggression = -6;
      else if (currentHandOurAggression > 6)
        currentHandOurAggression = 6;

      networkInputs.Add("CurrentHandOurAggression", currentHandOurAggression / 6.0m);

      networkInputs.Add("CurrentRoundFirstAction", (currentRoundPlayerActions.Count == 0 ? 1 : 0));
      networkInputs.Add("CurrentRoundSecondAction", (currentRoundPlayerActions.Count == 1 ? 1 : 0));
      networkInputs.Add("CurrentRoundThirdPlusAction", (currentRoundPlayerActions.Count >= 2 ? 1 : 0));
      #endregion Hand and Round Actions
      //4 inputs

      #region WinRatio
      decimal probWin = 1.0m - infoStore.GetInfoValue(InfoType.WR_ProbOpponentHasBetterWRFIXED);
      networkInputs.Add("WRProbWin", ScaleContInput(probWin));
      #endregion
      //1 inputs

      #region EV, RaiseAmounts and CheckPossibilitiy

      decimal bigBlindEVScaleAmount = 20;

      #region CallEV
      //The following EV assumes we can actually call the additionaCallAmount
      //We could cap the additionalCallAmount but we would then also not be able to win the total pot amount ;( again a minor error
      decimal actualCallCheckEV = (currentPotAmount * probWin) - ((1.0m - probWin) * additionalCallAmount);
      decimal scaledCallCheckEV = (actualCallCheckEV / currentDecision.Cache.BigBlind) / bigBlindEVScaleAmount;
      if (scaledCallCheckEV > 1)
        networkInputs.Add("ScaledCallCheckEV", 1);
      else if (scaledCallCheckEV < -1)
        networkInputs.Add("ScaledCallCheckEV", -1);
      else
        networkInputs.Add("ScaledCallCheckEV", scaledCallCheckEV);
      #endregion

      //Current scaled raise amounts
      //Aditional = MaximumRaiseTo - CurrentCallAmount
      decimal maximumAdditionalRaiseAmount = (infoStore.GetInfoValue(InfoType.BP_PlayerBetAmountCurrentRound_Decimal) + remainingPlayerStack) - (currentRoundMinPlayCallAmount);
      bool raisePossible = false;

      if (maximumAdditionalRaiseAmount > 0)
      {
        raisePossible = true;
        if (lastAdditionalRaiseAmount > maximumAdditionalRaiseAmount)
          lastAdditionalRaiseAmount = maximumAdditionalRaiseAmount;

        decimal additionalRaiseToCallAmount = raiseToCallAmountNew - currentRoundMinPlayCallAmount;// -infoStore.GetInfoValue(InfoType.BP_PlayerBetAmountCurrentRound_Decimal);
        decimal additionalRaiseToStealAmount = raiseToStealAmountNew - currentRoundMinPlayCallAmount;// -infoStore.GetInfoValue(InfoType.BP_PlayerBetAmountCurrentRound_Decimal);

        if (additionalRaiseToCallAmount > maximumAdditionalRaiseAmount)
          additionalRaiseToCallAmount = maximumAdditionalRaiseAmount;
        else if (additionalRaiseToCallAmount < currentDecision.Cache.BigBlind)
          additionalRaiseToCallAmount = currentDecision.Cache.BigBlind;

        if (additionalRaiseToStealAmount > maximumAdditionalRaiseAmount)
          additionalRaiseToStealAmount = maximumAdditionalRaiseAmount;
        else if (additionalRaiseToStealAmount < currentDecision.Cache.BigBlind)
          additionalRaiseToStealAmount = currentDecision.Cache.BigBlind;

        networkInputs.Add("ScaledAdditionalRaiseToCallAmount", ScaleContInput((decimal)RaiseAmountsHelper.ScaleAdditionalRaiseAmount(currentPotAmount, currentDecision.Cache.BigBlind, lastAdditionalRaiseAmount, maximumAdditionalRaiseAmount, additionalRaiseToCallAmount)));
        networkInputs.Add("ScaledAdditionalRaiseToStealAmount", ScaleContInput((decimal)RaiseAmountsHelper.ScaleAdditionalRaiseAmount(currentPotAmount, currentDecision.Cache.BigBlind, lastAdditionalRaiseAmount, maximumAdditionalRaiseAmount, additionalRaiseToStealAmount)));
      }
      else
      {
        //No raise possible, just use defaults
        networkInputs.Add("ScaledAdditionalRaiseToCallAmount", ScaleContInput(0));
        networkInputs.Add("ScaledAdditionalRaiseToStealAmount", ScaleContInput(0));
      }

      decimal probAllOpponentFoldRaiseToCall = infoStore.GetInfoValue(InfoType.WR_RaiseToCallStealSuccessProb);
      decimal probAllOpponentFoldRaiseToSteal = (gameStagePreFlop == 1 ? 0 : infoStore.GetInfoValue(InfoType.WR_RaiseToStealSuccessProb));

      #region RaiseToCallEV
      if (raisePossible)
      {
        decimal potAmountAssumingAllCall = currentPotAmount;

        //If we are preflop then it would be unrealistic to expect everyone to call
        //For Call we assume that if there are 8 players to act after the raise we get 2 calls
        //If there are 4 players to act after the raise we get 1 call
        //This is the same as dividing the number of activePlayers by 4
        if (gameStagePreFlop == 1)
        {
          //We assume anyone who has already called or raised will call again
          long[] calledRaisedPlayerIds = (from current in currentDecision.Cache.getAllHandActions()
                                          where current.actionType == PokerAction.Call || current.actionType == PokerAction.Raise
                                          where current.playerId != currentDecision.PlayerId
                                          select current.playerId).ToArray();

          for (int i = 0; i < calledRaisedPlayerIds.Length; i++)
            potAmountAssumingAllCall += raiseToCallAmountNew - currentDecision.Cache.getPlayerCurrentRoundBetAmount(calledRaisedPlayerIds[i]);

          potAmountAssumingAllCall += ((decimal)(activePlayerIds.Length - 1 - calledRaisedPlayerIds.Length) / 4) * raiseToCallAmountNew;
        }
        else
        {
          for (int i = 0; i < activePlayerIds.Length; i++)
          {
            if (activePlayerIds[i] != currentDecision.PlayerId)
              potAmountAssumingAllCall += raiseToCallAmountNew - currentDecision.Cache.getPlayerCurrentRoundBetAmount(activePlayerIds[i]);
          }
        }

        decimal extraDollarRiskAmount = raiseToCallAmountNew - infoStore.GetInfoValue(InfoType.BP_PlayerBetAmountCurrentRound_Decimal);

        decimal actualRaiseToCallEV = (currentPotAmount * probAllOpponentFoldRaiseToCall) + (probWin * potAmountAssumingAllCall * (1.0m - probAllOpponentFoldRaiseToCall)) - ((1.0m - probWin) * extraDollarRiskAmount * (1.0m - probAllOpponentFoldRaiseToCall));

        decimal scaledRaiseToCallEV = (actualRaiseToCallEV / currentDecision.Cache.BigBlind) / bigBlindEVScaleAmount;
        if (scaledRaiseToCallEV > 1)
          networkInputs.Add("ScaledRaiseToCallEV", 1);
        else if (scaledRaiseToCallEV < -1)
          networkInputs.Add("ScaledRaiseToCallEV", -1);
        else
          networkInputs.Add("ScaledRaiseToCallEV", scaledRaiseToCallEV);
      }
      else
        networkInputs.Add("ScaledRaiseToCallEV", networkInputs["ScaledCallCheckEV"]);
      #endregion

      #region RaiseToStealEV
      if (raisePossible)
      {
        decimal potAmountAssumingAllCall = currentPotAmount;

        //If we are preflop then it would be unrealistic to expect everyone to call
        //For steal we assume that if there are 8 players to act after the raise we get 1 call
        //If there are 4 players to act after the raise we get 0.5 call
        //This is the same as dividing the number of activePlayers by 8
        if (gameStagePreFlop == 1)
        {
          //We assume anyone who has already raised will call again
          long[] raisedPlayerIds = (from current in currentDecision.Cache.getAllHandActions()
                                    where current.actionType == PokerAction.Raise
                                    where current.playerId != currentDecision.PlayerId
                                    select current.playerId).ToArray();

          for (int i = 0; i < raisedPlayerIds.Length; i++)
            potAmountAssumingAllCall += raiseToStealAmountNew - currentDecision.Cache.getPlayerCurrentRoundBetAmount(raisedPlayerIds[i]);

          potAmountAssumingAllCall += ((decimal)(activePlayerIds.Length - 1 - raisedPlayerIds.Length) / 8) * raiseToStealAmountNew;
        }
        else
        {
          for (int i = 0; i < activePlayerIds.Length; i++)
          {
            if (activePlayerIds[i] != currentDecision.PlayerId)
              potAmountAssumingAllCall += raiseToStealAmountNew - currentDecision.Cache.getPlayerCurrentRoundBetAmount(activePlayerIds[i]);
          }
        }

        decimal extraDollarRiskAmount = raiseToStealAmountNew - infoStore.GetInfoValue(InfoType.BP_PlayerBetAmountCurrentRound_Decimal);

        decimal actualRaiseToStealEV = (currentPotAmount * probAllOpponentFoldRaiseToSteal) + (probWin * potAmountAssumingAllCall * (1.0m - probAllOpponentFoldRaiseToSteal)) - ((1.0m - probWin) * extraDollarRiskAmount * (1.0m - probAllOpponentFoldRaiseToSteal));
        decimal scaledRaiseToStealEV = (actualRaiseToStealEV / currentDecision.Cache.BigBlind) / bigBlindEVScaleAmount;
        if (scaledRaiseToStealEV > 1)
          networkInputs.Add("ScaledRaiseToStealEV", 1);
        else if (scaledRaiseToStealEV < -1)
          networkInputs.Add("ScaledRaiseToStealEV", -1);
        else
          networkInputs.Add("ScaledRaiseToStealEV", scaledRaiseToStealEV);
      }
      else
        networkInputs.Add("ScaledRaiseToStealEV", networkInputs["ScaledCallCheckEV"]);
      #endregion

      bool checkPossible = (infoStore.GetInfoValue(InfoType.BP_MinimumPlayAmount_Decimal) == infoStore.GetInfoValue(InfoType.BP_PlayerBetAmountCurrentRound_Decimal));
      networkInputs.Add("CheckPossible", Convert.ToDecimal(checkPossible));

      networkInputs.Add("CallAmount1BBOrLess", !checkPossible && ((decimal)infoStore.GetInfoValue(InfoType.BP_MinimumPlayAmount_Decimal) - (decimal)infoStore.GetInfoValue(InfoType.BP_PlayerBetAmountCurrentRound_Decimal)) <= currentDecision.Cache.BigBlind ? 1 : 0);
      #endregion
      //7 inputs

      #region PAP
      networkInputs.Add("ProbRaiseToStealSuccess", ScaleContInput(probAllOpponentFoldRaiseToSteal));
      #endregion
      //1 inputs

      #region PlayerAggression
      networkInputs.Add("AvgLiveOppPreFlopPlayFreq", ScaleContInput(infoStore.GetInfoValue(InfoType.AP_AvgLiveOppPreFlopPlayFreq_Double)));
      networkInputs.Add("AvgLiveOppPostFlopPlayFreq", ScaleContInput(infoStore.GetInfoValue(InfoType.AP_AvgLiveOppPostFlopPlayFreq_Double)));
      networkInputs.Add("AvgLiveOppCurrentRoundAggr", ScaleContInput(infoStore.GetInfoValue(InfoType.AP_AvgLiveOppCurrentRoundAggr_Double)));
      networkInputs.Add("AvgLiveOppCurrentRoundAggrAcc", ScaleContInput(infoStore.GetInfoValue(InfoType.AP_AvgLiveOppCurrentRoundAggrAcc_Double)));
      #endregion
      //4 inputs

      #region BigOrSmallPot
      byte isBigPot = 0;
      if (currentPotAmount > RaiseAmountsHelper.SmallPotBBMultiplierLimit * currentDecision.Cache.BigBlind)
        isBigPot = 1;
      networkInputs.Add("BigPot", ScaleContInput(isBigPot));
      #endregion
      //1 input

      #region RandomInputs
      //We have 2 random inputs with different periods, per hand and per decision

      //Check the randomNumbers are not -1
      if (currentDecision.Cache.CurrentHandRandomNumber() < 0)
        throw new Exception("currentDecision.Cache.CurrentHandRandomNumber() should not be less than 0");

      //We disable these random numbers for the ailwyn blank network
      if (currentDecision.Cache.PokerClientId == (short)PokerClients.GeneticPokerPlayer_PlaySimpleAI_1a_FixedStart || currentDecision.Cache.PokerClientId == (short)PokerClients.GeneticPokerPlayer_PlayerSimpleAI_1b_RandomInitialisation || currentDecision.Cache.PokerClientId == (short)PokerClients.GeneticPokerPlay_PlaySimpleAI_2a || (currentDecision.AiConfigStr.StartsWith("F") && currentDecision.AiConfigStr == "FixedNeuralPlayers\\neuralV7_Ailwyn_NoRandomInput.eNN"))
      {
        networkInputs.Add("HandRandomNumber", 0);
        networkInputs.Add("DecisionRandomNumber", 0);
      }
      else
      {
        networkInputs.Add("HandRandomNumber", ScaleContInput((decimal)currentDecision.Cache.CurrentHandRandomNumber() / (decimal)int.MaxValue));
        networkInputs.Add("DecisionRandomNumber", ScaleContInput((decimal)randomGen.NextDouble()));
      }
      #endregion
      //2 inputs

      #endregion neuralInputs

      NNDataSource aiDecisionData = new NNDataSource(networkInputs.Values.ToArray(), NeuralAINNModelV7.Input_Neurons, true);

      if (true)
      {
        //Get the network outputs
        double[] networkInputsArray = null;
        aiDecisionData.returnInput(ref networkInputsArray);
        double[] networkOutput = getPlayerNetworkPrediction(currentDecision.AiConfigStr, networkInputsArray);

        //Blank the non possible actions
        if (checkPossible)
        {
          //Blank fold
          networkOutput[0] = 0;
          //Blank call
          networkOutput[2] = 0;
        }
        else
          //Blank check
          networkOutput[1] = 0;

        //Get the bot decision
        NeuralAiDecisionNeuralv7 decision = new NeuralAiDecisionNeuralv7(networkOutput, !Convert.ToBoolean(gameStageRiver), randomGen.NextDouble());

        Debug.Print("Decision");

        //Can now check the fold decision for aces
        //if (decision.BotAction == 0 && networkInputs["HoleCardsAAPair"] == 1 && gameStagePreFlop == 1 && currentDecision.AiConfigStr.StartsWith("GeneticPokerPlayers"))
        //{
        //    currentDecision.Cache.SaveToDisk("", currentDecision.PlayerId + "-" + InfoProviderBase.CurrentJob.JobId + "-" + currentDecision.Cache.getCurrentHandId() + "-" + currentDecision.Cache.getMostRecentLocalIndex()[1]);
        //}

        if (decision.BotAction == 0)
          return new Play(PokerAction.Fold, 0, 0, currentDecision.Cache.getCurrentHandId(), currentDecision.PlayerId, decisionLogStr, 0);
        else if (decision.BotAction == 1)
          return new Play(PokerAction.Check, 0, 0, currentDecision.Cache.getCurrentHandId(), currentDecision.PlayerId, decisionLogStr, 0);
        else if (decision.BotAction == 2)
          return new Play(PokerAction.Call, additionalCallAmount, 0, currentDecision.Cache.getCurrentHandId(), currentDecision.PlayerId, decisionLogStr, 0);
        else if (decision.BotAction == 3)
        {
          bool isSmallPot = currentPotAmount < RaiseAmountsHelper.SmallPotBBMultiplierLimit * currentDecision.Cache.BigBlind * ((decimal)(randomGen.NextDouble() * 0.2) + 0.80m);

          //////////////////////////////////////////////////////////
          ///////////////// START VANAHEIM FIX /////////////////////
          //////////////////////////////////////////////////////////

          //We add a temporary fix here for vanaheim which is playing mid pocket pairs a little strongly
          //If we have pocket pair lower than (not including KK) have already raised and are trying to raise again we fold
          //if (currentDecision.AiConfigStr.StartsWith("F") && !isSmallPot && gameStagePreFlop == 1 && currentDecision.AiConfigStr == "FixedNeuralPlayers\\neuralV7_Vanaheim.eNN")
          //{
          //    bool highPair = infoStore.GetInfoValue(InfoType.CP_HoleCardsOtherHighPair_Bool) == 1.0m;
          //    bool midPair = infoStore.GetInfoValue(InfoType.CP_HoleCardsOtherLowPair_Bool) == 1.0m;
          //    if (highPair || midPair)
          //    {
          //        if (currentRoundPlayerActions.Count(entry => entry == PokerAction.Raise) >= 1)
          //        {
          //            if (actualCallCheckEV > 0 && (tablePositionLate == 1 || tablePositionMid == 1) && currentRoundPlayerActions.Count(entry => entry == PokerAction.Call) == 0)
          //                return new Play(PokerAction.Call, additionalCallAmount, 0, currentDecision.Cache.getCurrentHandId(), currentDecision.PlayerId, decisionLogStr, 0);
          //            else
          //                return new Play(PokerAction.Fold, 0, 0, currentDecision.Cache.getCurrentHandId(), currentDecision.PlayerId, decisionLogStr, 0);
          //        }
          //    }
          //}

          ////////////////////////////////////////////////////////////////////
          /////////////////// END VANAHEIM FIX /////////////////////
          ////////////////////////////////////////////////////////////////////

          //Make sure raiseAmount is multiple of small blind
          if (raisePossible)
          {
            decimal raiseToAmount, raiseAmountBlindMultiple;
            /////////////////////
            //GENETIC OR VANAHEIM 2 PLAYER FIX
            ////////////////////
            if (raisedTwiceOrGreaterCurrentRound && gameStagePreFlop == 1 && (currentDecision.AiConfigStr.StartsWith("G") || currentDecision.AiConfigStr == "FixedNeuralPlayers\\neuralV7_Vanaheim.eNN"))
              //Genetic players will now go all in on their third raise preflop
              raiseToAmount = maximumRaiseAmount;
            else
            {
              decimal additionalRaiseAmount = RaiseAmountsHelper.UnscaleAdditionalRaiseAmount(currentPotAmount, currentDecision.Cache.BigBlind, lastAdditionalRaiseAmount, maximumAdditionalRaiseAmount, decision.ScaledAdditionalRaiseAmount);

              //If the pot is currently small we raise in increments of big blind instead of little blind
              //We add a little randomness into this limit so that it's not always exact, random number is between 0.8 and 1.0
              if (isSmallPot)
              {
                ///////////////////////////////////////
                ///// START VANAHEIM FIX //////////////
                ///////////////////////////////////////
                //If we are preflop with a small pot playing as vanaheim
                //And we do not have AA or KK we can only raise to a set maximum
                //if (currentDecision.AiConfigStr.StartsWith("F") && gameStagePreFlop == 1 && currentDecision.AiConfigStr == "FixedNeuralPlayers\\neuralV7_Vanaheim.eNN")
                //{
                //    bool AAPair = infoStore.GetInfoValue(InfoType.CP_HoleCardsAAPair_Bool) == 1.0m;
                //    bool KKPair = infoStore.GetInfoValue(InfoType.CP_HoleCardsKKPair_Bool) == 1.0m;

                //    decimal maxDesiredRaise = RaiseAmountsHelper.SmallPotBBMultiplierLimit * currentDecision.Cache.BigBlind;
                //    if (!(AAPair || KKPair) && additionalRaiseAmount > maxDesiredRaise)
                //        additionalRaiseAmount = maxDesiredRaise;
                //}
                ///////////////////////////////////////
                ///// START VANAHEIM FIX //////////////
                ///////////////////////////////////////

                raiseAmountBlindMultiple = (currentRoundMinPlayCallAmount + additionalRaiseAmount) / currentDecision.Cache.BigBlind;
                raiseToAmount = Math.Round(raiseAmountBlindMultiple, 0, MidpointRounding.AwayFromZero) * currentDecision.Cache.BigBlind;
              }
              else
              {
                raiseAmountBlindMultiple = (currentRoundMinPlayCallAmount + additionalRaiseAmount) / currentDecision.Cache.LittleBlind;
                raiseToAmount = Math.Round(raiseAmountBlindMultiple, 0, MidpointRounding.AwayFromZero) * currentDecision.Cache.LittleBlind;
              }

              //If we are going to raise almost maximum then it might as well be max
              if (raiseToAmount > maximumRaiseAmount * 0.9m)
                raiseToAmount = maximumRaiseAmount;
            }

            return new Play(PokerAction.Raise, raiseToAmount, 0, currentDecision.Cache.getCurrentHandId(), currentDecision.PlayerId, decisionLogStr, 0);
          }
          else
            return new Play(PokerAction.Call, additionalCallAmount, 0, currentDecision.Cache.getCurrentHandId(), currentDecision.PlayerId, decisionLogStr, 0);
        }
        else
          throw new Exception("Something has gone wery wong!");
      }

      decisionLogStr = aiDecisionData.ToStringAdv(true);
      return new Play(PokerAction.Fold, 0, 0, currentDecision.Cache.getCurrentHandId(), currentDecision.PlayerId, decisionLogStr, 0);
    }

    /// <summary>
    /// Scales a continous input to between 0.1 and 0.9
    /// </summary>
    /// <param name="input"></param>
    /// <returns></returns>
    private decimal ScaleContInput(decimal input)
    {
      if (input > 1)
        input = 1;
      if (input < 0)
        input = 0;

      return (input * 0.8m) + 0.1m;
    }
  }
}
