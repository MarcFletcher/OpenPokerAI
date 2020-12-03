//using System;
//using System.Collections.Generic;
//using System.Linq;
//using System.Text;
//using System.Threading;
//using PokerBot.Definitions;

//namespace PokerBot.AI.InfoProviders
//{
//    public class WinRatioCPU
//    {
//        static object locker = new object();
//        static int sharedGameSuccess;

//        int totalNumGames;
//        int numGamesPerThread;
//        int numPlayers;

//        int[] HandRank;

//        int holeCard1;
//        int holeCard2;
//        int tableCard1;
//        int tableCard2;
//        int tableCard3;
//        int tableCard4;
//        int tableCard5;
//        //int seed;

//        bool weightedWR;

//        Thread[] workerThreads;
//        List<int> seedValues;

//        /// <summary>
//        /// Initiate WinRatioCPU.
//        /// </summary>
//        /// <param name="HandRank">The handRank value array.</param>
//        /// <param name="numThreads">The number of threads to use when calculating the gameSuccess.</param>
//        /// <param name="numGames">THe total number of games which should be simulated in order to calculate gameSuccess.</param>
//        public WinRatioCPU(int[] HandRank, int numThreads, int totalNumGames)
//        {
//            this.HandRank = HandRank;
//            this.workerThreads = new Thread[numThreads];
//            this.totalNumGames = totalNumGames;
//            this.numGamesPerThread = (totalNumGames / numThreads);
//        }

//        /// <summary>
//        /// Calculates the total number of succesfull (won) hands, the max possible win equals the value of numGames specified on construction.
//        /// </summary>
//        /// <param name="holeCard1"></param>
//        /// <param name="holeCard2"></param>
//        /// <param name="tableCard1"></param>
//        /// <param name="tableCard2"></param>
//        /// <param name="tableCard3"></param>
//        /// <param name="tableCard4"></param>
//        /// <param name="tableCard5"></param>
//        /// <param name="numPlayers">The total numbers to simulate (Minimum is 2).</param>
//        /// <param name="weightedWR">False - Return Non-weighted, True - Return Weighted</param>
//        /// <returns></returns>
//        public int GetGameSuccess(int holeCard1, int holeCard2, int tableCard1, int tableCard2, int tableCard3, int tableCard4, int tableCard5, int numPlayers, bool weightedWR)
//        {
//            sharedGameSuccess = 0;
//            seedValues = new List<int>();

//            this.holeCard1 = holeCard1;
//            this.holeCard2 = holeCard2;
//            this.tableCard1 = tableCard1;
//            this.tableCard2 = tableCard2;
//            this.tableCard3 = tableCard3;
//            this.tableCard4 = tableCard4;
//            this.tableCard5 = tableCard5;
//            this.numPlayers = numPlayers;
//            this.weightedWR = weightedWR;

//            //Start threads of simulateGame
//            for (int i = 0; i < workerThreads.Length; i++)
//            {
//                workerThreads[i] = new Thread(SimulateGame);
//                workerThreads[i].Name = "thread" + i.ToString();
//                seedValues.Add((int)(DateTime.Now.Ticks * i));
//            }

//            for (int i = 0; i < workerThreads.Length; i++) workerThreads[i].Start();
//            for (int i = 0; i < workerThreads.Length; i++) workerThreads[i].Join();

//            return sharedGameSuccess;
//        }

//        /// <summary>
//        /// Determines the value of a 7 card hand using the "Two Plus Two Evaluator" 
//        /// (http://www.codingthewheel.com/archives/poker-hand-evaluator-roundup#2p2)
//        /// (http://archives1.twoplustwo.com/showflat.php?Cat=0&Number=8513906&page=0&fpart=1&vc=1)
//        /// (Source code from XPokerEval Library - http://www.codingthewheel.com/file.axd?file=XPokerEval.zip)
//        /// </summary>
//        /// <param name="card1">Card 1 (Hole1)</param>
//        /// <param name="card2">Card 2 (Hole2)</param>
//        /// <param name="card3">TableCard 1 (Flop)</param>
//        /// <param name="card4">TableCard 2 (Flop)</param>
//        /// <param name="card5">TableCard 3 (Flop)</param>
//        /// <param name="card6">TableCard 4 (Turn)</param>
//        /// <param name="card7">TableCard 5 (River)</param>
//        /// <returns></returns>
//        private int GetHandValue(int card1, int card2, int card3, int card4, int card5, int card6, int card7)
//        {

//            /*
//            Card values
//            2c = 1 2d = 2 2h = 3 2s = 4
//            3c = 5 3d = 6 3h = 7 3s = 8
//            4c = 9 4d = 10 4h = 11 4s = 12
//            5c = 13 5d = 14 5h = 15 5s = 16
//            6c = 17 6d = 18 6h = 19 6s = 20
//            7c = 21 7d = 22 7h = 23 7s = 24
//            8c = 25 8d = 26 8h = 27 8s = 28
//            9c = 29 9d = 30 9h = 31 9s = 32
//            Tc = 33 Td = 34 Th = 35 Ts = 36
//            Jc = 37 Jd = 38 Jh = 39 Js = 40
//            Qc = 41 Qd = 42 Qh = 43 Qs = 44
//            Kc = 45 Kd = 46 Kh = 47 Ks = 48
//            Ac = 49 Ad = 50 Ah = 51 As = 52 
//            */

//            int returnValue = HandRank[53 + card1];
//            returnValue = HandRank[returnValue + card2];
//            returnValue = HandRank[returnValue + card3];
//            returnValue = HandRank[returnValue + card4];
//            returnValue = HandRank[returnValue + card5];
//            returnValue = HandRank[returnValue + card6];
//            returnValue = HandRank[returnValue + card7];

//            return returnValue;

//        }

//        /// <summary>
//        /// Returns a hand value using hole cards and tableOffset.
//        /// </summary>
//        /// <param name="hole1"></param>
//        /// <param name="hole2"></param>
//        /// <param name="tableCardOffset"></param>
//        /// <returns></returns>
//        private int GetHandValue(int hole1, int hole2, int tableCardOffset)
//        {
//            int returnValue = HandRank[tableCardOffset + hole1];
//            return HandRank[returnValue + hole2];
//        }

//        /// <summary>
//        /// Used to precalculate the offset for the current table cards. Increases speed when comparing cards since everyone shares these.
//        /// </summary>
//        /// <param name="table1"></param>
//        /// <param name="table2"></param>
//        /// <param name="table3"></param>
//        /// <param name="table4"></param>
//        /// <param name="table5"></param>
//        /// <returns></returns>
//        private int GetHandRankTableCardsOffset(int table1, int table2, int table3, int table4, int table5)
//        {
//            int returnValue = HandRank[53 + table1];
//            returnValue = HandRank[returnValue + table2];
//            returnValue = HandRank[returnValue + table3];
//            returnValue = HandRank[returnValue + table4];
//            return HandRank[returnValue + table5];
//        }

//        /// <summary>
//        /// Simulates a single game using passed variables
//        /// Only index 0 of playerCards should be populated with our cards
//        /// Any number of table cards may be specified (the method randomises the others)
//        /// </summary>
//        /// <param name="numPlayers">Number of players to simulate</param>
//        /// <param name="playerCards">Array of player hole cards (Index 0 are known cards)</param>
//        /// <param name="tableCards">Array of any cards on the table</param>
//        /// <returns>Win=1, Lose=-1, Tie=1</returns>
//        public void SimulateGame()
//        {
//            int myHandValue;
//            int enemyHandValue;
//            int gameSuccess;
//            int threadGameSuccess = 0;

//            string threadName = Thread.CurrentThread.Name;

//            //Local variables
//            int[,] playerCards = new int[numPlayers, 2];
//            int table1;
//            int table2;
//            int table3;
//            int table4;
//            int table5;
//            int q, j;
//            int numberPlayers = numPlayers;
//            bool preFlop;
//            bool preTurn;
//            bool preRiver;
//            long initialDeckState;
//            int offset;
//            int seedValue;

//            //Get the seed value
//            lock (locker)
//            {
//                seedValue = seedValues[0];
//                seedValues.Remove(seedValue);
//            }

//            //Initialise the deck
//            Deck currentDeck = new Deck(seedValue);

//            //Remove the table cards if necessary
//            table1 = tableCard1;
//            if (table1 >= 1) currentDeck.RemoveCard(table1);

//            table2 = tableCard2;
//            if (table2 >= 1) currentDeck.RemoveCard(table2);

//            table3 = tableCard3;
//            if (table3 >= 1) currentDeck.RemoveCard(table3);

//            table4 = tableCard4;
//            if (table4 >= 1) currentDeck.RemoveCard(table4);

//            table5 = tableCard5;
//            if (table5 >= 1) currentDeck.RemoveCard(table5);

//            playerCards[0, 0] = holeCard1;
//            playerCards[0, 1] = holeCard2;

//            //My cards must be removed from the deck
//            currentDeck.RemoveCard(holeCard1);
//            currentDeck.RemoveCard(holeCard2);

//            preFlop = (table1 < 1);
//            preTurn = (table4 < 1);
//            preRiver = (table5 < 1);

//            initialDeckState = currentDeck.GetDealtCards;

//            #region calculate for n Games
//            for (q = 0; q < numGamesPerThread; q++)
//            {
//                gameSuccess = 1;

//                //Shuffle the deck
//                currentDeck.Shuffle(initialDeckState);

//                //Deal out other player cards (all but index 0 as those are already specified)
//                //We could weight the cards here by only handing out one of the top hands ;))
//                for (j = 1; j < numberPlayers; j++)
//                {
//                    if (weightedWR)
//                    {
//                        //We need to make sure there are enough cards to deal out to these players
//                        byte numAKQJ10 = Deck.GetNumCards(currentDeck.GetDealtCards & 0xFFFFF00000000);

//                        if (numAKQJ10 < 19)
//                        {
//                            playerCards[j, 0] = currentDeck.GetNextCard(32);
//                            playerCards[j, 1] = currentDeck.GetNextCard(32);
//                        }
//                        else
//                        {
//                            playerCards[j, 0] = currentDeck.GetNextCard();
//                            playerCards[j, 1] = currentDeck.GetNextCard();
//                        }
//                    }
//                    else
//                    {
//                        playerCards[j, 0] = currentDeck.GetNextCard();
//                        playerCards[j, 1] = currentDeck.GetNextCard();
//                    }
//                }

//                //Deal possible table cards combinations
//                if (preFlop) //We are preFlop and need to select 5 table cards
//                {
//                    table1 = currentDeck.GetNextCard();
//                    table2 = currentDeck.GetNextCard();
//                    table3 = currentDeck.GetNextCard();
//                    table4 = currentDeck.GetNextCard();
//                    table5 = currentDeck.GetNextCard();
//                }
//                else if (preTurn) //We are postFlop and need to select 2 table cards
//                {
//                    table4 = currentDeck.GetNextCard();
//                    table5 = currentDeck.GetNextCard();
//                }
//                else if (preRiver) //We are post turn and need the final table card
//                {
//                    table5 = currentDeck.GetNextCard();
//                }

//                //Get the offset in the handrank array for the table cards which will be fixed for all players
//                offset = GetHandRankTableCardsOffset(table1, table2, table3, table4, table5);

//                //Determine my hand value
//                myHandValue = GetHandValue(playerCards[0, 0], playerCards[0, 1], offset);

//                //Cycle through the other players to see what happens
//                for (j = 1; j < numberPlayers; j++)
//                {
//                    enemyHandValue = GetHandValue(playerCards[j, 0], playerCards[j, 1], offset);// table1, table2, table3, table4, table5);

//                    if (myHandValue < enemyHandValue)
//                    {
//                        gameSuccess = -1;
//                        break;
//                    }
//                }

//                //int handCategory = returnValue >> 12;
//                //int rankWithinCategory = returnValue & 0x00000FFF;

//                //Write the shared gameSucces variable with a lock

//                threadGameSuccess += gameSuccess;
//            }

//            #endregion

//            //Write out the success
//            lock (locker)
//                sharedGameSuccess += threadGameSuccess;

//        }
//    }
//}
