using System;
using System.Collections.Generic;
using System.Linq;
using PokerBot.Definitions;
using System.IO;
using PokerBot.Database;

namespace PokerBot.BotGame
{
  public static class PokerHelper
  {
    /// <summary>
    /// playerHandValue struct used for determining pot Winners
    /// </summary>
    internal class playerHandValue : IComparable
    {
      public int handValue;
      public decimal playerMoneyInPot;
      public long playerId;
      public decimal maximumWinableAmount;
      public bool hasFolded;

      public playerHandValue(int handValue, long playerId, string playerName, decimal playerMoneyInPot, decimal maxWinableAmount, bool hasFolded)
      {
        this.handValue = handValue;
        this.playerId = playerId;
        this.playerMoneyInPot = playerMoneyInPot;
        this.maximumWinableAmount = maxWinableAmount;
        this.hasFolded = hasFolded;
      }

      //Implement the compareTo method so that we can sort on maximum winable amount ascending
      public int CompareTo(object obj)
      {
        playerHandValue otherHandValue = obj as playerHandValue;

        if (otherHandValue == null)
          throw new ArgumentException();

        //Use default comparer for decimal to make sure we get this right. This will still sort ascending
        return this.maximumWinableAmount.CompareTo(otherHandValue.maximumWinableAmount);

        //Old custom compare that is incorrect due to not returning zero for equal cases
        /*
        //First compare handValue
        if (this.maximumWinableAmount > otherHandValue.maximumWinableAmount)
            return 1;
        else
            return -1;                
        */
      }

      public override string ToString()
      {
        return handValue.ToString() + ",\t" +
                playerId.ToString() + ",\t" +
                playerMoneyInPot.ToString() + ",\t" +
                maximumWinableAmount.ToString() + ",\t" +
                hasFolded.ToString();
      }
    }

    public delegate decimal PokerRakeDelegate(databaseCacheClient clientCache, List<decimal> allPots, int potToCalcIndex);

    public static CacheError ReturnUncalledBets(databaseCacheClient clientCache)
    {
      if (clientCache.getActivePositionsLeftToAct().Length != 0)
        return new CacheError(CacheError.ErrorType.ActionError, clientCache.TableId, clientCache.getCurrentHandId(), null, "Cannot end hand with players still to act");

      long[] playerIdsInHand =
          (from ha in clientCache.getAllHandActions()
           where ha.handId == clientCache.getCurrentHandId()
           where ha.actionType == PokerAction.Fold || ha.actionType == PokerAction.Call ||
                 ha.actionType == PokerAction.Raise || ha.actionType == PokerAction.BigBlind
           select ha.playerId).Distinct().ToArray();

      decimal max1 = 0, max2 = 0, temp;
      long max1ID = -1;
      for (int i = 0; i < playerIdsInHand.Count(); i++)
      {
        temp = clientCache.getTotalPlayerMoneyInPot(playerIdsInHand[i]);

        if (temp > max1)
        {
          max2 = max1;
          max1 = temp;
          max1ID = playerIdsInHand[i];
        }
        else if (temp > max2)
        {
          max2 = temp;
        }
      }

      if (max1 > max2)
      {
        var result = clientCache.newHandAction(max1ID, PokerAction.ReturnBet, max1 - max2);

        if (result != CacheError.noError)
          return result;
      }

      return CacheError.noError;
    }

    public static CacheError AwardPot(databaseCacheClient clientCache, PokerRakeDelegate rakeCalculator, int numCompletedHands)
    {
      if (clientCache.getActivePositionsLeftToAct().Length != 0)
        return new CacheError(CacheError.ErrorType.ActionError, clientCache.TableId, clientCache.getCurrentHandId(), null, "Cannot end hand with players still to act");

      return AwardPot(clientCache, rakeCalculator, DateTime.Now, numCompletedHands);
    }

    public static CacheError AwardPot(databaseCacheClient clientCache, PokerRakeDelegate rakeCalculator)
    {
      if (clientCache.getActivePositionsLeftToAct().Length != 0)
        return new CacheError(CacheError.ErrorType.ActionError, clientCache.TableId, clientCache.getCurrentHandId(), null, "Cannot end hand with players still to act");

      return AwardPot(clientCache, rakeCalculator, DateTime.Now, 0);
    }

    public static CacheError AwardPot(databaseCacheClient clientCache, PokerRakeDelegate rakeCalculator, DateTime actionTime, int numCompletedHands)
    {
      #region awardPot

      if (clientCache.getActivePositions().Length > 1 && clientCache.getBettingRound() != 3)
        return new CacheError(CacheError.ErrorType.ActionError, clientCache.TableId, clientCache.getCurrentHandId(), null, "Cannot return bets or award pot with more than 1 player in hand unless all table cards are known");

      List<playerHandValue> playerHandValues = new List<playerHandValue>();
      //Last thing is to implement at sidepots and returned bets

      //For each player that is all in, work out how much they put in the pot
      //Determine how many individuals were in the pot when that player last bet
      //The sidepot for that player is their money * number players

      //Rank all the remaning players hands in order to correctly dish out the pot
      //We are going to ignore side pots for now ;)
      var activePositions = clientCache.getActivePositions();
      var currentHandDetails = clientCache.getCurrentHandDetails();

      long[] satInPlayerIDs = clientCache.getSatInPlayerIds();

      long[] playerIdsInHand =
          (from ha in clientCache.getAllHandActions()
           where ha.handId == clientCache.getCurrentHandId()
           where ha.actionType == PokerAction.Fold || ha.actionType == PokerAction.Call ||
                 ha.actionType == PokerAction.Raise || ha.actionType == PokerAction.BigBlind
           select ha.playerId).Distinct().ToArray();

      decimal[] allBetAmounts = new decimal[playerIdsInHand.Length];
      for (int i = 0; i < allBetAmounts.Length; i++)
        allBetAmounts[i] = clientCache.getTotalPlayerMoneyInPot(playerIdsInHand[i]);

      //Determine each positions HandValueObject
      for (int i = 0; i < playerIdsInHand.Length; i++)
      {
        long playerId = playerIdsInHand[i];
        string playerName = clientCache.getPlayerName(playerId);
        byte playerPosition = clientCache.getPlayerPosition(playerId);
        decimal playerMoneyInPot = clientCache.getTotalPlayerMoneyInPot(playerId);
        decimal maxWinAmount = 0;
        int playerHandValue = 0;
        bool hasFolded = true;

        if (satInPlayerIDs.Contains(playerId) && activePositions.Contains(playerPosition))
        {
          hasFolded = false;

          if (activePositions.Length == 1)
            playerHandValue = 100;
          else if (clientCache.getPlayerHoleCards(playerId).holeCard1 == (byte)Card.NoCard)
            playerHandValue = 0;
          else
          {
            //if (GPAJob == null)
            playerHandValue = HandRank.GetHandRank((Card)clientCache.getPlayerHoleCards(playerId).holeCard1, (Card)clientCache.getPlayerHoleCards(playerId).holeCard2, (Card)currentHandDetails.tableCard1, (Card)currentHandDetails.tableCard2, (Card)currentHandDetails.tableCard3, (Card)currentHandDetails.tableCard4, (Card)currentHandDetails.tableCard5);
            //else
            //    playerHandValue = GPAJob.HoleCardValues[numCompletedHands][playerPosition];
          }

          //Work out the maximum this player could win
          for (int j = 0; j < allBetAmounts.Length; j++)
          {
            decimal result = allBetAmounts[j] - playerMoneyInPot;
            if (result <= 0)
              maxWinAmount += allBetAmounts[j];
            else
              maxWinAmount += playerMoneyInPot;
          }
        }

        //Add this information to playerHandValues
        playerHandValues.Add(new playerHandValue(playerHandValue, playerId, playerName, playerMoneyInPot, maxWinAmount, hasFolded));
      }

      decimal deadBlindMoney = clientCache.getDeadBlindMoneyInPot();

      foreach (var handValue in playerHandValues)
        if (handValue.maximumWinableAmount > 0)
          handValue.maximumWinableAmount += deadBlindMoney;

      //Sort ascending on maximum winable amount ascending.             
      playerHandValues.Sort();

      //Linq version of sort from when sort didn't work in mono. See comments in CompareTo method in playerHandValue class
      /*
      playerHandValues = (from values in playerHandValues
                          orderby values.maximumWinableAmount ascending
                          select values).ToList();
      */

      List<decimal> potAmounts = new List<decimal>();
      decimal totalPot = 0.0m;

      for (int i = 0; i < playerHandValues.Count; i++)
      {
        if (playerHandValues[i].maximumWinableAmount == 0.0m)
          continue;

        if (playerHandValues[i].maximumWinableAmount > totalPot)
        {
          potAmounts.Add(playerHandValues[i].maximumWinableAmount - totalPot);

          totalPot = playerHandValues[i].maximumWinableAmount;
        }
      }

      int potIndex = 0;

      //Go through each player 
      for (int i = 0; i < playerHandValues.Count; i++)
      {
        //if they cannot win any money continue the loop
        if (playerHandValues[i].maximumWinableAmount == 0.0m)
          continue;

        //if there is a contested pot we need to do some thinking
        if (i < playerHandValues.Count - 1)
        {
          //Work out the pot amount for this stage
          decimal potAmount = playerHandValues[i].maximumWinableAmount;

          //next work out the rake for this pot
          decimal rake = rakeCalculator(clientCache, potAmounts, potIndex);

          //Next go through each player with at least this amount to win and see how many winners of this pot there are
          int winningRank = 0, winnerCount = 0;
          for (int j = i; j < playerHandValues.Count; j++)
          {
            //if hand value of player is greatest than that found so far we have a new possible winner
            if (playerHandValues[j].handValue > winningRank)
            {
              winningRank = playerHandValues[j].handValue;
              winnerCount = 1;
            }//if rank is equal to winning rank so far we have another winnner
            else if (playerHandValues[j].handValue == winningRank && playerHandValues[j].maximumWinableAmount > 0)
              winnerCount++;
          }

          //Now we know how many winners there are calculate amount each should get
          decimal winAmountPerWinner = ((decimal)((int)(100 * (potAmount - rake) / winnerCount))) / 100.0m;

          //work out odd cents number so we can give those to the players closest to dealer
          int numberOddCents = (int)((potAmount - rake - winnerCount * winAmountPerWinner) * 100);

          //Now do the rake if greater than 0
          CacheError result = CacheError.noError;

          if (rake > 0)
          {
            result = clientCache.newHandAction(clientCache.getPlayerId(clientCache.getCurrentHandDetails().dealerPosition), PokerAction.TableRake, rake);
            if (result != CacheError.noError)
              return result;
          }

          playerHandValue[] winners;

          //Get a list of winners and order by dealer distance if we have odd cents
          if (numberOddCents != 0)
            winners =
                (from players in playerHandValues
                 where players.handValue == winningRank && players.maximumWinableAmount > 0
                 orderby clientCache.getActivePlayerDistanceToDealer(players.playerId) ascending
                 select players).ToArray();
          else
            winners =
                (from players in playerHandValues
                 where players.handValue == winningRank && players.maximumWinableAmount > 0
                 select players).ToArray();

          //go through each winner and award them their share of the pot adding a cent if there are still odd
          //cents to dish out
          for (int j = 0; j < winners.Count(); j++)
          {
            if (numberOddCents > 0)
            {
              result = clientCache.newHandAction(winners[j].playerId, PokerAction.WinPot, winAmountPerWinner + 0.01m);

              numberOddCents--;
            }
            else
            {
              result = clientCache.newHandAction(winners[j].playerId, PokerAction.WinPot, winAmountPerWinner);
            }

            if (result != CacheError.noError)
              return result;
          }

          //now go through each player thats left and reduce their maximumWinablePot by this pot value
          for (int j = i; j < playerHandValues.Count; j++)
          {
            playerHandValues[j].maximumWinableAmount -= potAmount;
          }

          potIndex++;
        }
        else
        {
          //if we get here there must be extra pot to hand out to the last player

          decimal potAmount = playerHandValues[i].maximumWinableAmount;
          decimal rake = rakeCalculator(clientCache, potAmounts, potIndex);
          CacheError result;

          result = clientCache.newHandAction(playerHandValues[i].playerId, PokerAction.WinPot, potAmount - rake);
          if (result != CacheError.noError)
            return result;

          if (rake > 0)
          {
            result = clientCache.newHandAction(clientCache.getPlayerId(clientCache.getCurrentHandDetails().dealerPosition), PokerAction.TableRake, rake);
            if (result != CacheError.noError)
              return result;
          }

          playerHandValues[i].maximumWinableAmount -= potAmount;

          potIndex++;
        }
      }

      //At this point the entire point must have been awarded
      //We want to break here instead of cause a cache error so that we can find out what the problem was!!
      decimal totalAwardedAmounts =
          (from wins in clientCache.getAllHandActions()
           where wins.handId == clientCache.getCurrentHandId() && (wins.actionType == PokerAction.WinPot || wins.actionType == PokerAction.TableRake)
           select wins.actionValue).Sum();

      if (totalAwardedAmounts != clientCache.getCurrentHandDetails().potValue)
        return new CacheError(CacheError.ErrorType.AmountInvalid, clientCache.TableId, clientCache.getCurrentHandId(), null, "The full pot amount has not been awarded.");

      #endregion awardPot

      return CacheError.noError;
    }

    /// <summary>
    /// Creates the neccessary oppoenent players and passes back their new playerIds
    /// </summary>
    public static long[] CreateOpponentPlayers(AISelection[] AISelection, bool obfuscate, short pokerClientId)
    {
      long[] newPlayerIds = new long[AISelection.Length];

      for (int i = 0; i < AISelection.Length; i++)
      {
        string newPlayerName;

        //Catch human players and correct the name
        if (AISelection[i].AiGeneration == AIGeneration.NoAi_Human)
          newPlayerName = "Human" + i.ToString();
        else
          newPlayerName = AISelection[i].AiGeneration.ToString();

        newPlayerName = databaseQueries.GenerateNewPlayerName(newPlayerName, pokerClientId, obfuscate);

        //Catch genetic players and get a real configStr
        //This first if is required incase the configStr is less than 8 chars, SubString will throw an error if it is
        if (AISelection[i].ConfigStr.Length > 8)
        {
          if (AISelection[i].ConfigStr.Substring(0, 8) == "genetic=")
          {
            string speciesName = AISelection[i].ConfigStr.Substring(8, AISelection[i].ConfigStr.Length - 8);
            string fbpBaseDir = Environment.GetEnvironmentVariable("FBPNetworkStore");

            string speciesLocation = "GeneticPokerPlayers\\" + speciesName + "\\";
            DirectoryInfo d = new DirectoryInfo(fbpBaseDir + speciesLocation);

            int CurrentGeneticGeneration = d.GetDirectories().Count();

            string[] fullFileNames = Directory.GetFiles(fbpBaseDir + speciesLocation + CurrentGeneticGeneration, "*.eNN");
            string[] networkNames = new string[fullFileNames.Length];

            for (int k = 0; k < networkNames.Length; k++)
              networkNames[k] = fullFileNames[k].Replace(fbpBaseDir, "");

            //Now select the highest network possible
            networkNames = ReOrderNetworkNames(networkNames);

            for (int k = 0; k < networkNames.Length; k++)
            {
              int networkAddedCount =
                  (from alreadyAdded in AISelection
                   where alreadyAdded.ConfigStr == networkNames[k]
                   select alreadyAdded).Count();

              if (networkAddedCount == 0)
              {
                AISelection[i].ConfigStr = networkNames[k];
                break;
              }
              else if (k >= networkNames.Length - 1)
                throw new Exception("Error setting correct configStr.");
            }
          }
        }

        newPlayerIds[i] = databaseQueries.CreateNewBotPlayer(newPlayerName, pokerClientId, (int)AISelection[i].AiGeneration, AISelection[i].ConfigStr);
      }

      return newPlayerIds;
    }

    private static string[] ReOrderNetworkNames(string[] names)
    {
      string[][] arraySort = new string[names.Length][];

      for (int i = 0; i < names.Length; i++)
      {
        arraySort[i] = new string[2];
        arraySort[i][0] = names[i];

        int networkNumber;
        if (int.TryParse(names[i].Substring(names[i].Length - 6, 2), out networkNumber))
          arraySort[i][1] = networkNumber.ToString();
        else if (int.TryParse(names[i].Substring(names[i].Length - 5, 1), out networkNumber))
          arraySort[i][1] = networkNumber.ToString();
        else
          throw new Exception("Some error reordering network names.");
      }

      return (from temp in arraySort
              orderby int.Parse(temp[1]) ascending
              select temp[0]).ToArray();
    }
  }
}
