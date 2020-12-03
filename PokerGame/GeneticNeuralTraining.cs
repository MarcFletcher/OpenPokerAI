using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using PokerBot.AI;
using PokerBot.Database;
using PokerBot.Definitions;
using System.Threading;

namespace PokerBot.BotGame
{
  public class GeneticNeuralTrainingPokerGame : BotVsBotPokerGame
  {
    //Used for total random seating orientation
    List<long> geneticPlayerIds;
    List<long> nonGeneticPlayerIds;
    byte numGeneticTablePlayers;

    CardsManager cardsManager;

    //Used for static seating orientation
    List<long[]> positionPlayerIds;

    int lastAggressionProviderUpdateHandNum = 0;

    /// <summary>
    /// Genetic Neural Training With Random Positions, Players and Cards
    /// </summary>
    /// <param name="gameType"></param>
    /// <param name="gameCache"></param>
    /// <param name="playerNames"></param>
    /// <param name="startingStack"></param>
    /// <param name="minNumTablePlayers"></param>
    /// <param name="maxHandsToPlay"></param>
    /// <param name="actionPause"></param>
    /// <param name="aiManager"></param>
    /// <param name="geneticPlayerIds"></param>
    /// <param name="nonGeneticPlayerIds"></param>
    /// <param name="numGeneticTablePlayers"></param>
    public GeneticNeuralTrainingPokerGame(PokerGameType gameType, databaseCacheClient gameCache, string[] playerNames, decimal startingStack, byte minNumTablePlayers, int maxHandsToPlay, int actionPause, AIManager aiManager, List<long> geneticPlayerIds, List<long> nonGeneticPlayerIds, byte numGeneticTablePlayers)
        : base(gameType, gameCache, minNumTablePlayers, aiManager, playerNames, startingStack, maxHandsToPlay, actionPause)
    {
      this.geneticPlayerIds = geneticPlayerIds;
      this.nonGeneticPlayerIds = nonGeneticPlayerIds;
      this.numGeneticTablePlayers = numGeneticTablePlayers;

      this.autoRemovePlayersWithLargeStackInBB = 200;
    }

    /// <summary>
    /// Genetic Neural Training With Fixed Players And Random Cards
    /// </summary>
    /// <param name="gameType"></param>
    /// <param name="gameCache"></param>
    /// <param name="playerNames"></param>
    /// <param name="startingStack"></param>
    /// <param name="minNumTablePlayers"></param>
    /// <param name="maxHandsToPlay"></param>
    /// <param name="actionPause"></param>
    /// <param name="aiManager"></param>
    /// <param name="positionPlayerIds"></param>
    public GeneticNeuralTrainingPokerGame(PokerGameType gameType, databaseCacheClient gameCache, string[] playerNames, decimal startingStack, byte minNumTablePlayers, int maxHandsToPlay, int actionPause, AIManager aiManager, List<long[]> positionPlayerIds)
        : base(gameType, gameCache, minNumTablePlayers, aiManager, playerNames, startingStack, maxHandsToPlay, actionPause)
    {
      this.positionPlayerIds = positionPlayerIds;
      this.autoRemovePlayersWithLargeStackInBB = 200;
    }

    /// <summary>
    /// Genetic Neural Training With Fixed Positions and Fixed Cards
    /// </summary>
    /// <param name="gameType"></param>
    /// <param name="gameCache"></param>
    /// <param name="playerNames"></param>
    /// <param name="startingStack"></param>
    /// <param name="minNumTablePlayers"></param>
    /// <param name="maxHandsToPlay"></param>
    /// <param name="actionPause"></param>
    /// <param name="aiManager"></param>
    /// <param name="positionPlayerIds"></param>
    /// <param name="cardsManager"></param>
    public GeneticNeuralTrainingPokerGame(PokerGameType gameType, databaseCacheClient gameCache, string[] playerNames, decimal startingStack, byte minNumTablePlayers, int maxHandsBeforeTableReset, int maxHandsToPlay, int actionPause, AIManager aiManager, List<long[]> positionPlayerIds, CardsManager cardsManager)
        : base(gameType, gameCache, minNumTablePlayers, aiManager, playerNames, startingStack, maxHandsToPlay, actionPause)
    {
      this.positionPlayerIds = positionPlayerIds;
      this.cardsManager = cardsManager;

      //Set the correct dealerIndex based on currentHandIndex
      dealerIndex = (byte)(cardsManager.CurrentHandIndex % gameCache.NumSeats);
      this.autoRemovePlayersWithLargeStackInBB = 200;
      this.maxHandsBeforeTableReset = maxHandsBeforeTableReset;
    }

    /// <summary>
    /// Sit down everyone at random. Introduces large table variance.
    /// </summary>
    protected void performFullRandomSitDown()
    {
      long selectedPlayerId;
      byte selectedPosition;
      string playerName;
      List<byte> emptySeatPositions = clientCache.getEmptyPositions().ToList();

      //Only sit down players if there are empty seats
      if (emptySeatPositions.Count > 0)
      {
        long[] allSatInPlayerIds = clientCache.getSatInPlayerIds();

        //Sit in new players
        //Count the numebr of genetic players
        long[] currentlySeatedGeneticPlayerIds =
            (from current in allSatInPlayerIds
             join genetic in geneticPlayerIds on current equals genetic
             select current).ToArray();

        long[] currentlySeatedNonGeneticPlayers =
            (from current in allSatInPlayerIds
             join other in nonGeneticPlayerIds on current equals other
             select current).ToArray();

        //If there is less than there is supposed to be add a random genetic player who is not already sat at the table
        if (currentlySeatedGeneticPlayerIds.Length < numGeneticTablePlayers)
        {
          //For each player we need to add, lets sit down a genetic player
          for (int i = 0; i < numGeneticTablePlayers - currentlySeatedGeneticPlayerIds.Length; i++)
          {
            do
            {
              selectedPlayerId = geneticPlayerIds[(int)(randomGen.NextDouble() * geneticPlayerIds.Count)];
            } while (clientCache.getSatInPlayerIds().Contains(selectedPlayerId));

            playerName = clientCache.getPlayerName(selectedPlayerId);
            selectedPosition = emptySeatPositions[(int)(randomGen.NextDouble() * emptySeatPositions.Count)];
            emptySeatPositions.Remove(selectedPosition);

            //Sit down the selected player
            cacheError = clientCache.newTablePlayer(playerName, 0, selectedPosition, true, ref selectedPlayerId);
            if (cacheError != CacheError.noError)
              throw new Exception("Error!");

            cacheError = clientCache.newHandAction(selectedPlayerId, PokerAction.JoinTable, selectedPosition);
            if (cacheError != CacheError.noError)
              throw new Exception("Error!");

            cacheError = clientCache.newHandAction(selectedPlayerId, PokerAction.AddStackCash, startingStack);
            if (cacheError != CacheError.noError)
              throw new Exception("Error!");

            cacheError = clientCache.newHandAction(selectedPlayerId, PokerAction.SitIn, selectedPosition);
            if (cacheError != CacheError.noError)
              throw new Exception("Error!");
          }
        }

        //We can now fill all remaning seats with non genetic players (who are not already playing)
        int remaningEmptySeats = emptySeatPositions.Count;

        for (int i = 0; i < remaningEmptySeats; i++)
        {
          do
          {
            selectedPlayerId = nonGeneticPlayerIds[(int)(randomGen.NextDouble() * nonGeneticPlayerIds.Count)];
          } while (clientCache.getSatInPlayerIds().Contains(selectedPlayerId));

          playerName = clientCache.getPlayerName(selectedPlayerId);
          selectedPosition = emptySeatPositions[(int)(randomGen.NextDouble() * emptySeatPositions.Count)];
          emptySeatPositions.Remove(selectedPosition);

          //Sit down the selected player
          cacheError = clientCache.newTablePlayer(playerName, 0, selectedPosition, true, ref selectedPlayerId);
          if (cacheError != CacheError.noError)
            throw new Exception("Error!");

          cacheError = clientCache.newHandAction(selectedPlayerId, PokerAction.JoinTable, selectedPosition);
          if (cacheError != CacheError.noError)
            throw new Exception("Error!");

          cacheError = clientCache.newHandAction(selectedPlayerId, PokerAction.AddStackCash, startingStack);
          if (cacheError != CacheError.noError)
            throw new Exception("Error!");

          cacheError = clientCache.newHandAction(selectedPlayerId, PokerAction.SitIn, selectedPosition);
          if (cacheError != CacheError.noError)
            throw new Exception("Error!");
        }
      }
    }

    /// <summary>
    /// Sit's players down in fixed positions to remove a degree of freedom from the simulation.
    /// </summary>
    protected void performFixedSitDown()
    {
      //positionPlayerIds gives the playerIds which can be placed in which positions
      long selectedPlayerId;
      string playerName;
      List<byte> emptySeatPositions = clientCache.getEmptyPositions().ToList();

      int loopSafety = 0;

      for (int i = 0; i < emptySeatPositions.Count; i++)
      {
        do
        {
          selectedPlayerId = positionPlayerIds[emptySeatPositions[i]][(int)(randomGen.NextDouble() * positionPlayerIds[emptySeatPositions[i]].Length)];
          loopSafety++;

          if (loopSafety > 1000)
            throw new Exception("Error - All available players for this position are already seated at the table.");

        } while (clientCache.getSatInPlayerIds().Contains(selectedPlayerId));

        playerName = clientCache.getPlayerName(selectedPlayerId);

        //Sit down the selected player
        cacheError = clientCache.newTablePlayer(playerName, 0, (byte)emptySeatPositions[i], true, ref selectedPlayerId);
        if (cacheError != CacheError.noError)
          throw new Exception("Error!");

        cacheError = clientCache.newHandAction(selectedPlayerId, PokerAction.JoinTable, emptySeatPositions[i]);
        if (cacheError != CacheError.noError)
          throw new Exception("Error!");

        //decimal stackToAdd = startingStack;
        //if (playerName.StartsWith("geneticPokerAI")) stackToAdd = 10.0m;

        cacheError = clientCache.newHandAction(selectedPlayerId, PokerAction.AddStackCash, startingStack);
        if (cacheError != CacheError.noError)
          throw new Exception("Error!");

        cacheError = clientCache.newHandAction(selectedPlayerId, PokerAction.SitIn, emptySeatPositions[i]);
        if (cacheError != CacheError.noError)
          throw new Exception("Error!");
      }
    }

    protected override void SitDownTableStartingPlayers(string[] playerNames)
    {
      //Sit down the blank players
      base.SitDownTableStartingPlayers(playerNames);

      //Now sit out any dead players.
      base.SitOutInPlayers();
    }

    protected override void SitOutInPlayers()
    {
      //First sitout the defaults (0) stacks and anything larger than the autoRemove
      base.SitOutInPlayers();

      //Now sit down the correct players.
      if (geneticPlayerIds != null || nonGeneticPlayerIds != null)
        performFullRandomSitDown();
      else if (positionPlayerIds != null)
        performFixedSitDown();
      else
        throw new Exception("Filling empty seats has not been specified or atleast the code the confused!!");
    }

    protected override void dealPlayerCards(bool dealWeightedCards, double percentPlayersWeightedCards)
    {
      if (cardsManager == null)
        base.dealPlayerCards(dealWeightedCards, percentPlayersWeightedCards);
      else
      {
        //If the cards manager is not null we want to deal player cards from there
        activePositions = clientCache.getActivePositions(dealerIndex);
        byte[] playerHoleCards;
        for (int i = 0; i < activePositions.Length; i++)
        {
          playerHoleCards = cardsManager.PlayerHoleCards(activePositions[i]);
          cacheError = clientCache.newHoleCards(clientCache.getPlayerId(activePositions[i]), playerHoleCards[0], playerHoleCards[1]);
          if (cacheError != CacheError.noError)
            throw new Exception("Cache error dealing out hole cards.");
        }
      }
    }

    protected override void dealTableCards(int bettingRound)
    {
      if (cardsManager == null)
        base.dealTableCards(bettingRound);
      else
      {
        //Prevents the game from running too quickly and cards being drawn on the same tick as the last game action
        //Thread.Sleep(1);

        byte[] newTableCards = cardsManager.TableCards();
        var currentHand = clientCache.getCurrentHandDetails();

        switch (bettingRound)
        {
          case 1:
            //Record deal flop
            cacheError = clientCache.newHandAction(clientCache.getPlayerId(dealerIndex), PokerAction.DealFlop, 0);
            if (cacheError != CacheError.noError)
              throw new Exception("Cache error recording deal flop.");
            cacheError = clientCache.updateTableCards(newTableCards[0], newTableCards[1], newTableCards[2], 0, 0);
            if (cacheError != CacheError.noError)
              throw new Exception("Cache error adding flop cards.");
            break;
          case 2:
            //Record deal turn
            if (currentHand.tableCard1 != newTableCards[0] || currentHand.tableCard2 != newTableCards[1] || currentHand.tableCard3 != newTableCards[2])
              throw new Exception("The previous cards do not correlate with the current ones.!!.");

            cacheError = clientCache.newHandAction(clientCache.getPlayerId(dealerIndex), PokerAction.DealTurn, 0);
            if (cacheError != CacheError.noError)
              throw new Exception("Cache error recording deal turn.");
            cacheError = clientCache.updateTableCards(0, 0, 0, newTableCards[3], 0);
            if (cacheError != CacheError.noError)
              throw new Exception("Cache error adding turn card.");
            break;
          case 3:
            //Record deal river
            if (currentHand.tableCard1 != newTableCards[0] || currentHand.tableCard2 != newTableCards[1] || currentHand.tableCard3 != newTableCards[2] || currentHand.tableCard4 != newTableCards[3])
              throw new Exception("The previous cards do not correlate with the current ones.!!.");

            cacheError = clientCache.newHandAction(clientCache.getPlayerId(dealerIndex), PokerAction.DealRiver, 0);
            if (cacheError != CacheError.noError)
              throw new Exception("Cache error recording deal river.");
            cacheError = clientCache.updateTableCards(0, 0, 0, 0, newTableCards[4]);
            if (cacheError != CacheError.noError)
              throw new Exception("Cache error adding river card.");
            break;
          default:
            //Pre flop action is after big blind
            currentActionPosition = clientCache.getNextActiveTablePosition(currentActionPosition);
            currentActionPosition = clientCache.getNextActiveTablePosition(currentActionPosition);
            break;
        }
      }
    }

    protected override void finishHand()
    {
      base.finishHand();

      if (cardsManager != null)
        cardsManager.IncrementHandIndex();

      //Check to see if an update is necessary
      /*if (GPAJob != null && GPAJob.AggressionProviderNumHandUpdateInterval > 0 && GPAJob.NumHandsPerJob > numCompletedHands)
      {
          //We only update every AggressionProviderNumHandUpdateInterval
          if (numCompletedHands - lastAggressionProviderUpdateHandNum >= GPAJob.AggressionProviderNumHandUpdateInterval)
          {
              lastAggressionProviderUpdateHandNum = numCompletedHands;

              //Trigger the aggression provider update
              List<InfoProviderType> providersToUpdate = new List<InfoProviderType>() { InfoProviderType.AIAggression };
              aiManager.BeginSlowProviderUpdate(providersToUpdate);
              aiManager.WaitForSlowProviderUpdateToComplete();
          }
      }*/
    }
  }
}
