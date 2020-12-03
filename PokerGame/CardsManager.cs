using System;
using System.Collections.Generic;
using System.Linq;
using PokerBot.Definitions;
using System.IO;
using NetworkCommsDotNet.DPSBase;

namespace PokerBot.BotGame
{
  /// <summary>
  /// Handles the fixed cards which will be dealt to player as part of the GPA
  /// </summary>
  public class CardsManager
  {
    byte[][] cardsArray;
    long currentHandIndex;

    static DataProcessor cardsCompressor = DPSManager.GetDataProcessor<SharpZipLibCompressor.SharpZipLibGzipCompressor>();

    /// <summary>
    /// Cards Manager
    /// </summary>
    /// <param name="loadCardsArray">If set to true the cardsmanger will load the current cards array.</param>
    /// <param name="startingHandIndex">The index at which the first set of cards should be drawn from.</param>
    public CardsManager(string fileLocation, long startingHandIndex)
    {
      currentHandIndex = startingHandIndex;
      LoadCardsArray(fileLocation);
    }

    /// <summary>
    /// Populates the cards manager. Allows for card recreation any any point in gamecards array.
    /// </summary>
    /// <param name="seed">Value used to initialise the random number generator</param>
    /// <param name="jumpToHandNum">Number of hands to jump before adding data to cardManager. For the first hand in the manager to be hand 100, starting from hand 0, enter 100</param>
    /// <param name="numPlayers">Number of players to generate cards for</param>
    /// <param name="numHandsToCreate">Hands to create in cards manager</param>
    public CardsManager(long seed, int jumpToHandNum, byte numPlayers, int numHandsToCreate)
    {
      cardsArray = new byte[numHandsToCreate][];
      Deck cardDeck = new Deck(seed);

      //Move forwards by the necessary random numbers
      for (int i = 0; i < jumpToHandNum; i++)
      {
        cardDeck.Shuffle();
        int cardsToDraw = 5 + (2 * numPlayers);
        for (int j = 0; j < cardsToDraw; j++)
          cardDeck.GetNextCard();
      }

      for (int i = 0; i < numHandsToCreate; i++)
      {
        cardDeck.Shuffle();
        cardsArray[i] = new byte[5 + (2 * numPlayers)];

        for (int j = 0; j < cardsArray[i].Length; j++)
          cardsArray[i][j] = cardDeck.GetNextCard();
      }
    }

    public CardsManager(byte[][] cardsArray, long startingHandIndex)
    {
      this.cardsArray = cardsArray;
      this.currentHandIndex = startingHandIndex;
    }

    /// <summary>
    /// Creates an array containing the cards required for the specified number of hands. File is saved as 'gameCards.cards'
    /// </summary>
    /// <param name="numHands"></param>
    /// <param name="numPlayers"></param>
    /// <param name="saveFileName"></param>
    /// <param name="optionalCardSeed">DateTime.Now is default.</param>
    public static void CreateCardsArray(int numHands, byte numPlayers, string saveFileName, long optionalCardSeed = 0)
    {
      if (optionalCardSeed == 0)
        optionalCardSeed = DateTime.Now.Ticks;

      CardsManager cards = new CardsManager(optionalCardSeed, 0, numPlayers, numHands);

      //Save the array out
      //File.WriteAllBytes(saveFileName, FBPSerialiser.SerialiseDataObject(cards.CardsArray));
      File.WriteAllBytes(saveFileName, DPSManager.GetDataSerializer<ProtobufSerializer>().SerialiseDataObject<byte[][]>(cards.CardsArray, new List<DataProcessor>() { cardsCompressor }, new Dictionary<string, string>()).ThreadSafeStream.ToArray());
    }

    /// <summary>
    /// Get and set the currentHandIndex. The index is the point at which cards are dealt from.
    /// </summary>
    public long CurrentHandIndex
    {
      get { return currentHandIndex; }
    }

    public byte[][] CardsArray
    {
      get { return cardsArray; }
    }

    public void IncrementHandIndex()
    {
      currentHandIndex++;
    }

    public void LoadCardsArray(string fileLocation)
    {
      //cardsArray = (byte[][])SerializeObject.Load(fileLocation);
      //cardsArray = FBPSerialiser.DeserialiseDataObject<byte[][]>(File.ReadAllBytes(fileLocation));
      cardsArray = DPSManager.GetDataSerializer<ProtobufSerializer>().DeserialiseDataObject<byte[][]>(File.ReadAllBytes(fileLocation), new List<DataProcessor>() { cardsCompressor }, new Dictionary<string, string>());
    }

    public byte[][] CardsBetweenIndex(long startIndexInclusive, long endIndexInclusive, byte numPlayers)
    {
      byte[][] returnArray = new byte[endIndexInclusive - startIndexInclusive + 1][];

      if (cardsArray == null)
        throw new Exception("The cardsArray has not been loaded yet.");

      for (int i = 0; i < returnArray.Length; i++)
      {
        returnArray[i] = new byte[5 + (2 * numPlayers)];

        for (int j = 0; j < returnArray[i].Length; j++)
          returnArray[i][j] = cardsArray[i + startIndexInclusive][j];
      }

      return returnArray;
    }

    /// <summary>
    /// Goes through the provided handCards and for each hand return the handValues as bytes for all players
    /// </summary>
    /// <param name="handCards"></param>
    /// <returns></returns>
    public static byte[][] CardHandValues(byte[][] handCards)
    {
      //playerHandValue = HandRank.GetHandRank((Card)clientCache.getPlayerHoleCards(playerId).holeCard1, (Card)clientCache.getPlayerHoleCards(playerId).holeCard2, (Card)currentHandDetails.tableCard1, (Card)currentHandDetails.tableCard2, (Card)currentHandDetails.tableCard3, (Card)currentHandDetails.tableCard4, (Card)currentHandDetails.tableCard5);
      byte[][] returnArray = new byte[handCards.Length][];

      for (int i = 0; i < handCards.Length; i++)
      {
        returnArray[i] = new byte[(int)((double)(handCards[i].Length - 5) / 2.0)];

        List<PokerBot.BotGame.PokerHelper.playerHandValue> positionHandValues = new List<PokerBot.BotGame.PokerHelper.playerHandValue>();

        //For each player get their handValue
        for (byte j = 0; j < returnArray[i].Length; j++)
          positionHandValues.Add(new PokerBot.BotGame.PokerHelper.playerHandValue(HandRank.GetHandRank((Card)handCards[i][0], (Card)handCards[i][1], (Card)handCards[i][2], (Card)handCards[i][3], (Card)handCards[i][4], (Card)handCards[i][5 + (2 * j)], (Card)handCards[i][6 + (2 * j)]), j, "", 0, 0, false));

        //Now sort by handValue
        positionHandValues = (from current in positionHandValues
                              orderby current.handValue descending
                              select current).ToList();

        //Now write out the correct result in position order
        //for (byte j = 0; j < returnArray[i].Length; j++)
        //    returnArray[i][positionHandValues[j].playerId] = (byte)(returnArray[i].Length-j);

        byte scoreIndex = (byte)returnArray[i].Length;
        int currentHandValue = positionHandValues[0].handValue;
        byte currentPlayerCount = 0;
        do
        {
          //We need to watch out for matching handValues
          if (positionHandValues[currentPlayerCount].handValue == currentHandValue)
            returnArray[i][positionHandValues[currentPlayerCount].playerId] = scoreIndex;
          else
          {
            scoreIndex--;
            currentHandValue = positionHandValues[currentPlayerCount].handValue;
            returnArray[i][positionHandValues[currentPlayerCount].playerId] = scoreIndex;
          }

          currentPlayerCount++;

        } while (currentPlayerCount < returnArray[i].Length);
      }

      return returnArray;
    }

    /// <summary>
    /// Returns the table cards for the currentHandIndex. Table cards are the first 5 cards at the current row.
    /// </summary>
    /// <returns></returns>
    public byte[] TableCards()
    {
      if (cardsArray == null)
        throw new Exception("The cardsArray has not been loaded yet.");

      byte[] returnArray = new byte[5];

      for (int i = 0; i < 5; i++)
        returnArray[i] = cardsArray[currentHandIndex][i];

      return returnArray;
    }

    /// <summary>
    /// Returns the hole cards for the player at positionIndex (starts at position 0)
    /// </summary>
    /// <param name="playerPosition"></param>
    /// <returns></returns>
    public byte[] PlayerHoleCards(byte playerPosition)
    {
      if (cardsArray == null)
        throw new Exception("The cardsArray has not been loaded yet.");

      if (5 + (2 * (playerPosition + 1)) > cardsArray[0].Length)
        throw new Exception("Enough cards have not been pre dealt to hand out cards for the provided position.");

      byte[] returnArray = new byte[2];

      returnArray[0] = cardsArray[currentHandIndex][5 + (2 * playerPosition)];
      returnArray[1] = cardsArray[currentHandIndex][6 + (2 * playerPosition)];

      if (playerPosition == 3)
        playerPosition = 3;

      return returnArray;
    }
  }
}
