using System;
using System.Linq;
using PokerBot.Definitions;
using System.IO;

namespace PokerBot.AI.ProviderWinRatio
{
  public partial class WinRatioProvider
  {

    public partial class TableModel
    {

      #region File location Params

      static string fileDir = Environment.GetEnvironmentVariable("WeightedWinRatioDir");

      public const string unraisedPreflopFoldFN = "Preflop\\_fittedUnraisedPreflopFold.csv";
      public const string unraisedPreflopCallFN = "Preflop\\_fittedUnraisedPreflopCall.csv";
      public const string unraisedPreflopRaiseFN = "Preflop\\_fittedUnraisedPreflopRaise.csv";
      public const string unraisedPostflopRaisedEarlyFN = "Postflop\\_fittedUnraisedPostflopRaiseEarly.csv";
      public const string unraisedPostflopRaisedLateFN = "Postflop\\_fittedUnraisedPostflopRaiseLate.csv";
      public const string unraisedPostflopCheckEarlyFN = "Postflop\\_fittedUnraisedPostflopCheckEarly.csv";
      public const string unraisedPostflopCheckLateFN = "Postflop\\_fittedUnraisedPostflopCheckLate.csv";

      public const string raisedPreflopFoldFN = "Preflop\\_fittedNormRaisedPreflopFold.csv";
      public const string raisedPreflopCallFN = "Preflop\\_fittedNormRaisedPreflopCall.csv";
      public const string raisedPreflopRaiseFN = "Preflop\\_fittedNormRaisedPreflopRaise.csv";
      public const string raisedPostFlopFoldEarlyFN = "Postflop\\_fittedNormRaisedPostflopFoldEarly.csv";
      public const string raisedPostFlopFoldLateFN = "Postflop\\_fittedNormRaisedPostflopFoldLate.csv";
      public const string raisedPostFlopCallEarlyFN = "Postflop\\_fittedNormRaisedPostflopCallEarly.csv";
      public const string raisedPostFlopCallLateFN = "Postflop\\_fittedNormRaisedPostflopCallLate.csv";
      public const string raisedPostFlopRaiseEarlyFN = "Postflop\\_fittedNormRaisedPostflopRaiseEarly.csv";
      public const string raisedPostFlopRaiseLateFN = "Postflop\\_fittedNormRaisedPostflopRaiseLate.csv";

      #endregion

      public class PlayerModelFixed
      {
        struct playerCardsProb
        {
          public Card card1, card2;
          public int sortedIndex;
          public ushort winPercentage;
          //public double prob;

          public playerCardsProb(Card card1, Card card2)
          {
            if ((byte)card1 > (byte)card2)
            { this.card1 = card1; this.card2 = card2; }
            else
            { this.card1 = card2; this.card2 = card1; }

            this.sortedIndex = -1;
            this.winPercentage = 0;
          }
        }

        playerCardsProb[] playerWinRatios = new playerCardsProb[52 * 51 / 2];
        double[] playerWinRatioProbs = new double[52 * 51 / 2];

        bool folded = false;
        bool winRatiosSorted = false;
        WinRatioProvider wrProv;
        long playerId, tableId, handId;
        long tableCardsOnLastUpdate = long.MaxValue;
        int numPlayersOnLastUpdate = -1;
        decimal bigBlind;

        static object locker = new object();
        static playerCardsProb[][] storedPreflopPlayerWinRatios;
        static double[][] storedPreflopProbs;
        static long[] holeCardsLongs;

        #region Action percentage data

        static double[] unraisedPreflopFold, unraisedPreflopCall, unraisedPreflopRaise, unraisedPostflopRaisedEarly, unraisedPostflopRaisedLate, unraisedPostflopCheckEarly, unraisedPostflopCheckLate;
        static double[,] raisedPreflopFold, raisedPreflopCall, raisedPreflopRaise, raisedPostFlopFoldEarly, raisedPostFlopFoldLate, raisedPostFlopCallEarly, raisedPostFlopCallLate, raisedPostFlopRaiseEarly, raisedPostFlopRaiseLate;

        #endregion

        #region Action percentage model params

        static double unraisedPreflopA = -0.0848, unraisedPreflopC = 0.000, raisedPreflopA = -1.8405, raisedPreflopC = -1.7008, unraisedPostflopA = -0.1579, unraisedPostflopC = -0.1207, raisedPostflopA = -0.3744, raisedPostflopC = -0.1479;
        static double unraisedPreflopB = 0.2524, unraisedPreflopD = 0.0388, raisedPreflopB = 0.4334, raisedPreflopD = 0.427, unraisedPostflopB = 0.4196, unraisedPostflopD = 0.2002, raisedPostflopB = 0.4603, raisedPostflopD = 0.1608;

        #endregion

        #region Normalisation Constants

        static double[] raisedPreflopNormsByCallAmount = new double[]
        {
                    7.000838168,         //0.0 
                    6.599161832,         //0.1
                    6.658385079,         //0.2
                    7.191716244,         //0.3
                    7.326723766,         //0.4
                    7.969230769,         //0.5
                    8.925,               //0.6
                    9.225,               //0.7
                    9.425,               //0.8
                    9.55,                //0.9
                    9.65                 //1.0                    
        };

        static double[] raisedPostFlopEarlyNormsByCallAmount = new double[]
        {
                    4.422619048     ,    //0.0
                    6.077380952     ,    //0.1
                    7.012608225     ,    //0.2
                    7.086215174     ,    //0.3
                    7.091464091     ,    //0.4
                    8.09047619      ,    //0.5
                    9.075           ,    //0.6
                    9.1             ,    //0.7
                    9.1             ,    //0.8
                    9.15            ,    //0.9
                    9.25                 //1.0
        };

        static double[] raisedPostFlopLateNormsByCallAmount = new double[]
        {
                    3.125           ,    //0.0
                    5.351190476     ,    //0.1
                    6.689664502     ,    //0.2
                    7.198593074     ,    //0.3
                    7.875           ,    //0.4
                    8.133928571     ,    //0.5
                    8.5             ,    //0.6
                    9.025           ,    //0.7
                    9.05            ,    //0.8
                    9.075           ,    //0.9
                    9.125                //1.0
        };

        #endregion

        public PlayerModelFixed(long tableId, decimal bigBlind, long handId, long playerId, WinRatioProvider wrProv)
        {
          this.wrProv = wrProv;
          this.playerId = playerId;
          this.tableId = tableId;
          this.handId = handId;
          int count = 0;
          this.bigBlind = bigBlind;

          lock (staticLocker)
          {
            if (storedPreflopPlayerWinRatios == null)
            {
              storedPreflopPlayerWinRatios = new playerCardsProb[9][];
              storedPreflopProbs = new double[9][];
              holeCardsLongs = new long[52 * 51 / 2];
              var tableCards = new Card[0];

              for (int n = 2; n <= 10; n++)
              {
                count = 0;
                storedPreflopPlayerWinRatios[n - 2] = new playerCardsProb[52 * 51 / 2];
                storedPreflopProbs[n - 2] = new double[52 * 51 / 2];

                for (byte i = 1; i <= 52; i++)
                {
                  for (byte j = 1; j < i; j++)
                  {
                    storedPreflopPlayerWinRatios[n - 2][count] = new playerCardsProb((Card)i, (Card)j);
                    storedPreflopPlayerWinRatios[n - 2][count].sortedIndex = wrProv.GetWinPercentageIndex((Card)i, (Card)j, tableCards, n, tableId, handId);
                    storedPreflopPlayerWinRatios[n - 2][count].winPercentage = wrProv.GetWinPercentageShort((Card)i, (Card)j, tableCards, n, tableId, handId);
                    storedPreflopProbs[n - 2][count] = 1 / 1326.0;
                    holeCardsLongs[count] = ((1L << (i - 1)) | (1L << (j - 1)));
                    count++;
                  }
                }
              }
            }
          }
          Array.Copy(storedPreflopPlayerWinRatios[8], playerWinRatios, playerWinRatios.Length);
          tableCardsOnLastUpdate = 0;
          numPlayersOnLastUpdate = 10;
          Array.Copy(storedPreflopProbs[8], playerWinRatioProbs, playerWinRatioProbs.Length);

          LoadDataIfRequired();
        }

        private static void LoadDataIfRequired()
        {
          lock (locker)
          {
            if (unraisedPreflopFold == null)
            {
              if (CurrentJob == null)
              {
                LoadData(Path.Combine(fileDir, unraisedPreflopFoldFN), out unraisedPreflopFold, false);
                LoadData(Path.Combine(fileDir, unraisedPreflopCallFN), out unraisedPreflopCall, false);
                LoadData(Path.Combine(fileDir, unraisedPreflopRaiseFN), out unraisedPreflopRaise, false);
                LoadData(Path.Combine(fileDir, unraisedPostflopRaisedEarlyFN), out unraisedPostflopRaisedEarly, false);
                LoadData(Path.Combine(fileDir, unraisedPostflopRaisedLateFN), out unraisedPostflopRaisedLate, false);
                LoadData(Path.Combine(fileDir, unraisedPostflopCheckEarlyFN), out unraisedPostflopCheckEarly, false);
                LoadData(Path.Combine(fileDir, unraisedPostflopCheckLateFN), out unraisedPostflopCheckLate, false);

                LoadData(Path.Combine(fileDir, raisedPostFlopCallEarlyFN), out raisedPostFlopCallEarly, false);
                LoadData(Path.Combine(fileDir, raisedPostFlopCallLateFN), out raisedPostFlopCallLate, false);
                LoadData(Path.Combine(fileDir, raisedPostFlopFoldEarlyFN), out raisedPostFlopFoldEarly, false);
                LoadData(Path.Combine(fileDir, raisedPostFlopFoldLateFN), out raisedPostFlopFoldLate, false);
                LoadData(Path.Combine(fileDir, raisedPostFlopRaiseEarlyFN), out raisedPostFlopRaiseEarly, false);
                LoadData(Path.Combine(fileDir, raisedPostFlopRaiseLateFN), out raisedPostFlopRaiseLate, false);

                LoadData(Path.Combine(fileDir, raisedPreflopCallFN), out raisedPreflopCall, false);
                LoadData(Path.Combine(fileDir, raisedPreflopFoldFN), out raisedPreflopFold, false);
                LoadData(Path.Combine(fileDir, raisedPreflopRaiseFN), out raisedPreflopRaise, false);
              }
              else
              {
                LoadData(unraisedPreflopFoldFN, out unraisedPreflopFold, true);
                LoadData(unraisedPreflopCallFN, out unraisedPreflopCall, true);
                LoadData(unraisedPreflopRaiseFN, out unraisedPreflopRaise, true);
                LoadData(unraisedPostflopRaisedEarlyFN, out unraisedPostflopRaisedEarly, true);
                LoadData(unraisedPostflopRaisedLateFN, out unraisedPostflopRaisedLate, true);
                LoadData(unraisedPostflopCheckEarlyFN, out unraisedPostflopCheckEarly, true);
                LoadData(unraisedPostflopCheckLateFN, out unraisedPostflopCheckLate, true);

                LoadData(raisedPostFlopCallEarlyFN, out raisedPostFlopCallEarly, true);
                LoadData(raisedPostFlopCallLateFN, out raisedPostFlopCallLate, true);
                LoadData(raisedPostFlopFoldEarlyFN, out raisedPostFlopFoldEarly, true);
                LoadData(raisedPostFlopFoldLateFN, out raisedPostFlopFoldLate, true);
                LoadData(raisedPostFlopRaiseEarlyFN, out raisedPostFlopRaiseEarly, true);
                LoadData(raisedPostFlopRaiseLateFN, out raisedPostFlopRaiseLate, true);

                LoadData(raisedPreflopCallFN, out raisedPreflopCall, true);
                LoadData(raisedPreflopFoldFN, out raisedPreflopFold, true);
                LoadData(raisedPreflopRaiseFN, out raisedPreflopRaise, true);
              }

            }
          }
        }

        private static void LoadData(string fileName, out double[] data, bool fromJob)
        {
          if (!File.Exists(fileName))
          {
            throw new ArgumentException("Provided data does not exist " + fileName);
          }

          string[] lines = fromJob ? null : File.ReadAllLines(fileName);

          if (lines.Length != 101)
            throw new Exception();

          data = new double[101];

          for (int i = 0; i < 101; i++)
            data[i] = double.Parse(lines[i]);
        }

        private static void LoadData(string fileName, out double[,] data, bool fromJob)
        {
          string[] lines = fromJob ? null : File.ReadAllLines(fileName);

          if (lines.Length != 101 * 101)
            throw new Exception();

          data = new double[101, 101];

          for (int i = 0; i < 101; i++)
          {
            for (int j = 0; j < 101; j++)
              data[j, i] = double.Parse(lines[i * 101 + j]);
          }
        }

        public void ResetProbsToDefault(long handId)
        {
          Array.Copy(storedPreflopPlayerWinRatios[8], playerWinRatios, playerWinRatios.Length);
          tableCardsOnLastUpdate = 0;
          numPlayersOnLastUpdate = 10;
          Array.Copy(storedPreflopProbs[8], playerWinRatioProbs, playerWinRatioProbs.Length);

          folded = false;
          winRatiosSorted = false;
          this.handId = handId;
        }

        public void UpdateCardsWinPercentages(Card[] tableCards, int numPlayers)
        {
          if (folded)
            return;

          long tableCardsL = 0;

          for (int i = 0; i < tableCards.Length; i++)
            tableCardsL |= 1L << ((byte)(tableCards[i]) - 1);

          if (tableCardsL == tableCardsOnLastUpdate && numPlayersOnLastUpdate == numPlayers)
            return;

          tableCardsOnLastUpdate = tableCardsL;
          numPlayersOnLastUpdate = numPlayers;

          long handCards = 0;
          int count = playerWinRatios.Length;
          winRatiosSorted = false;

          if (tableCards.Length != 0)
          {
            var wrEntry = wrProv.GetWinPercentageEntry(tableCards, numPlayers, tableId, handId);
            wrEntry.PrepForLargeNumberGets((HandState)(tableCards.Length), numPlayers);

            for (int i = 0; i < count; i++)
            {
              handCards = holeCardsLongs[i];// ((1L << ((byte)(playerWinRatios[i].card1) - 1)) | (1L << ((byte)(playerWinRatios[i].card2) - 1)));

              if ((handCards & tableCardsL) == 0)
              {
                playerWinRatios[i].sortedIndex = wrEntry.GetPrepSortedIndex(playerWinRatios[i].card1, playerWinRatios[i].card2);
                playerWinRatios[i].winPercentage = wrEntry.GetPrepWinRatio(playerWinRatios[i].card1, playerWinRatios[i].card2);
              }
              else
              {
                playerWinRatios[i].sortedIndex = -1;
                playerWinRatios[i].winPercentage = 0;
              }
            }
          }
          else
          {
            Array.Copy(storedPreflopPlayerWinRatios[numPlayers - 2], playerWinRatios, playerWinRatios.Length);
          }
        }

        public void UpdateCardProbsBasedOnAction(PokerAction action, HandState handStage, decimal callAmount, decimal raiseAmount, decimal potAmount, bool raisedPot, bool earlyPosition)
        {
          //If the player has folded then this method should not be being called
          if (folded)
            throw new Exception("A folded player cannot act");

          #region Convert call and raise amount to scaled values

          double pa = (double)potAmount;

          double a, b, c;

          pa -= (double)callAmount;

          if (pa > (double)(50 * bigBlind))
            pa = (double)(50 * bigBlind);
          if (pa < (double)(10 * bigBlind))
            pa = (double)(10 * bigBlind);

          b = ((double)(101 * bigBlind) - 2 * pa) / Math.Pow((double)bigBlind - pa, 2);
          c = 1 - (double)bigBlind * b;
          a = 1 / Math.Log((double)(100 * bigBlind) * b + c, Math.E);

          double ca = a * Math.Log(b * (double)callAmount + c, Math.E);

          if (ca > 1)
            ca = 1;
          if (ca < 0)
            ca = 0;

          pa = (double)(potAmount);

          if (pa > (double)(50 * bigBlind))
            pa = (double)(50 * bigBlind);
          if (pa < (double)(10 * bigBlind))
            pa = (double)(10 * bigBlind);

          b = ((double)(101 * bigBlind) - 2 * pa) / Math.Pow((double)bigBlind - pa, 2);
          c = 1 - (double)bigBlind * b;
          a = 1 / Math.Log((double)(100 * bigBlind) * b + c, Math.E);

          double ra = a * Math.Log(b * (double)raiseAmount + c, Math.E);

          if (ra > 1)
            ra = 1;
          if (ra < 0)
            ra = 0;

          #endregion

          //Going to loop through all possible cards and total up the percentage of time we do that action
          double totalActionPercentage = 0;

          //Function derived from player "surprising" fits as of 22/09/10                   

          //div is 1 / number of possible hand combinations for this handstage
          double div = 1.0 / (handStage == HandState.PreFlop ? 168 : (0.5 * (52 - (int)handStage) * (51 - (int)handStage) - 1));

          //Different data arrays based on raised/unraised
          if (!raisedPot)
          {
            //Select the data array based on hand stage and action
            double[] dataUnRaised = null;

            if (handStage == HandState.PreFlop)
            {
              switch (action)
              {
                case PokerAction.Fold:
                  dataUnRaised = unraisedPreflopFold;
                  break;
                case PokerAction.Check:
                  dataUnRaised = unraisedPreflopFold;
                  break;
                case PokerAction.Call:
                  dataUnRaised = unraisedPreflopCall;
                  break;
                case PokerAction.Raise:
                  dataUnRaised = unraisedPreflopRaise;
                  break;
              }
            }
            else
            {
              if (action == PokerAction.Raise)
              {
                if (earlyPosition)
                  dataUnRaised = unraisedPostflopRaisedEarly;
                else
                  dataUnRaised = unraisedPostflopRaisedLate;
              }
              else if (action == PokerAction.Check)
              {
                if (earlyPosition)
                  dataUnRaised = unraisedPostflopCheckEarly;
                else
                  dataUnRaised = unraisedPostflopCheckLate;
              }
              else
                throw new Exception();
            }

            //if we raise we need to do a slightly different loop
            if (action == PokerAction.Raise && ra != double.MaxValue)
            {
              //Prepare params for raise % calculation
              //we will resuse a, b and c but need another param
              double d;

              if (raisedPot)
              {
                if (handStage == HandState.PreFlop)
                { a = raisedPreflopA; b = raisedPreflopB; c = raisedPreflopC; d = raisedPreflopD; }
                else
                { a = raisedPostflopA; b = raisedPostflopB; c = raisedPostflopC; d = raisedPostflopD; }
              }
              else
              {
                if (handStage == HandState.PreFlop)
                { a = unraisedPreflopA; b = unraisedPreflopB; c = unraisedPreflopC; d = unraisedPreflopD; }
                else
                { a = unraisedPostflopA; b = unraisedPostflopB; c = unraisedPostflopC; d = unraisedPostflopD; }
              }

              //Loop through each card set
              for (int i = 0; i < playerWinRatios.Length; i++)
              {
                //if sor index < 0 cannot have card so prob have those cards is 0.  Hence 
                if (playerWinRatios[i].sortedIndex < 0)
                {
                  playerWinRatioProbs[i] = 0;
                  continue;
                }

                double wp = playerWinRatios[i].sortedIndex * div;

                int lowIndex = (int)Math.Round(wp * 100);

                double temp = dataUnRaised[lowIndex];

                //This account for the raise
                double x = ra - a * wp - b, y = (c * wp + d < 0.05) ? 0.05 : (c * wp + d);
                y = y * y;
                x = x * x;
                temp *= Math.Exp(-x / (2 * y)) / Math.Sqrt(2 * Math.PI * y);

                //Update prob and add to total
                playerWinRatioProbs[i] = playerWinRatioProbs[i] * temp;
                totalActionPercentage += playerWinRatioProbs[i];
              }
            }
            else
            {
              //Loop through each card set
              for (int i = 0; i < playerWinRatios.Length; i++)
              {
                //if sor index < 0 cannot have card so prob have those cards is 0.  Hence 
                if (playerWinRatios[i].sortedIndex < 0)
                {
                  playerWinRatioProbs[i] = 0;
                  continue;
                }

                double wp = playerWinRatios[i].sortedIndex * div;

                int lowIndex = (int)Math.Round(wp * 100);

                double temp = dataUnRaised[lowIndex];

                //Update prob and add to total
                playerWinRatioProbs[i] = playerWinRatioProbs[i] * temp;
                totalActionPercentage += playerWinRatioProbs[i];
              }
            }
          }
          else
          {
            //Select the data array based on hand stage and action
            double[,] dataRaised = null;

            if (handStage == HandState.PreFlop)
            {
              switch (action)
              {
                case PokerAction.Fold:
                  dataRaised = raisedPreflopFold;
                  break;
                case PokerAction.Call:
                  dataRaised = raisedPreflopCall;
                  break;
                case PokerAction.Raise:
                  dataRaised = raisedPreflopRaise;
                  break;
              }
            }
            else
            {
              if (action == PokerAction.Raise)
              {
                if (earlyPosition)
                  dataRaised = raisedPostFlopRaiseEarly;
                else
                  dataRaised = raisedPostFlopRaiseLate;
              }
              else if (action == PokerAction.Call)
              {
                if (earlyPosition)
                  dataRaised = raisedPostFlopCallEarly;
                else
                  dataRaised = raisedPostFlopCallLate;
              }
              else if (action == PokerAction.Fold)
              {
                if (earlyPosition)
                  dataRaised = raisedPostFlopFoldEarly;
                else
                  dataRaised = raisedPostFlopFoldLate;
              }
              else
                throw new Exception();
            }

            //Set Y index for prob look up
            int lowIndexY = (int)Math.Round(ca * 100);

            //if we raise we need to do a slightly different loop
            if (action == PokerAction.Raise && ra != double.MaxValue)
            {
              //Prepare params for raise % calculation
              //we will resuse a, b and c but need another param
              double d;

              if (raisedPot)
              {
                if (handStage == HandState.PreFlop)
                { a = raisedPreflopA; b = raisedPreflopB; c = raisedPreflopC; d = raisedPreflopD; }
                else
                { a = raisedPostflopA; b = raisedPostflopB; c = raisedPostflopC; d = raisedPostflopD; }
              }
              else
              {
                if (handStage == HandState.PreFlop)
                { a = unraisedPreflopA; b = unraisedPreflopB; c = unraisedPreflopC; d = unraisedPreflopD; }
                else
                { a = unraisedPostflopA; b = unraisedPostflopB; c = unraisedPostflopC; d = unraisedPostflopD; }
              }

              //Loop through and update prob for each card set
              for (int i = 0; i < playerWinRatios.Length; i++)
              {
                //if sor index < 0 cannot have card so prob have those cards is 0.  Hence 
                if (playerWinRatios[i].sortedIndex < 0)
                {
                  playerWinRatioProbs[i] = 0;
                  continue;
                }

                double wp = playerWinRatios[i].sortedIndex * div;

                int lowIndexX = (int)Math.Round(wp * 100);

                double temp = dataRaised[lowIndexY, lowIndexX];

                if (temp < 0.005)
                  temp = 0.005;

                //This account for the raise
                double x = ra - a * wp - b, y = (c * wp + d < 0.05) ? 0.05 : (c * wp + d);
                y = y * y;
                x = x * x;
                temp *= Math.Exp(-x / (2 * y)) / Math.Sqrt(2 * Math.PI * y);

                playerWinRatioProbs[i] = playerWinRatioProbs[i] * temp;
                totalActionPercentage += playerWinRatioProbs[i];
              }
            }
            else
            {
              //Loop through and update prob for each card set
              for (int i = 0; i < playerWinRatios.Length; i++)
              {
                //if sor index < 0 cannot have card so prob have those cards is 0.  Hence 
                if (playerWinRatios[i].sortedIndex < 0)
                {
                  playerWinRatioProbs[i] = 0;
                  continue;
                }

                double wp = playerWinRatios[i].sortedIndex * div;

                int lowIndexX = (int)Math.Round(wp * 100);

                double temp = dataRaised[lowIndexY, lowIndexX];

                if (temp < 0.005)
                  temp = 0.005;

                playerWinRatioProbs[i] = playerWinRatioProbs[i] * temp;
                totalActionPercentage += playerWinRatioProbs[i];
              }
            }
          }

          //renormalise all the probs based on summed total action percentage
          totalActionPercentage = 1.0 / totalActionPercentage;

          for (int i = 0; i < playerWinRatios.Length; i++)
            playerWinRatioProbs[i] = playerWinRatioProbs[i] * totalActionPercentage;
        }

        public void UpdateProbsAfterCardDealt(Card dealtCard)
        {
          if (folded)
            return;

          double probHadCard = 0.0;
          long dealtCardLong = (1L << ((int)dealtCard - 1));

          for (int i = 0; i < playerWinRatios.Length; i++)
          {
            if ((holeCardsLongs[i] & dealtCardLong) != 0)
            {
              probHadCard += playerWinRatioProbs[i];
              playerWinRatioProbs[i] = 0;
            }
          }

          probHadCard = 1.0 / (1.0 - probHadCard);

          for (int i = 0; i < playerWinRatioProbs.Length; i++)
            playerWinRatioProbs[i] = playerWinRatioProbs[i] * probHadCard;
        }

        public double GetProbHaveBetterWinPercentageThan(ushort winPercentage, Card hc1, Card hc2)
        {
          if (folded)
            return 0;

          int numPWR = playerWinRatios.Length;
          double result = 0.0, total = 0.0;
          long hc = ((1L << ((int)hc1 - 1)) | ((1L << ((int)hc2 - 1))));

          for (int i = 0; i < numPWR; i++)
          {
            if ((holeCardsLongs[i] & hc) == 0)
            {
              if (playerWinRatios[i].winPercentage > winPercentage)
                result += playerWinRatioProbs[i];

              total += playerWinRatioProbs[i];
            }
          }

          return result / total;
        }

        public void GetRaiseCallSteal(HandState stage, Card hc1, Card hc2, bool earlyPosition, decimal potAmount, decimal minAdditionalRaise, decimal maxAdditionalRaise, double raiseToCallThreshold, double raiseToStealThreshold, out decimal raiseCallAmount, out decimal raiseStealAmount, out double raiseStealActualSuccess, out double raiseCallActualSuccess)
        {
          double raiseCallScaled = -1, raiseStealScaled = -1;

          raiseStealActualSuccess = -1;
          raiseCallActualSuccess = -1;

          double pa = (double)(potAmount);

          if (pa > (double)(50 * bigBlind))
            pa = (double)(50 * bigBlind);
          if (pa < (double)(10 * bigBlind))
            pa = (double)(10 * bigBlind);

          double b = ((double)(101 * bigBlind) - 2 * pa) / Math.Pow((double)bigBlind - pa, 2);
          double c = 1 - (double)bigBlind * b;
          double a = 1 / Math.Log((double)(100 * bigBlind) * b + c, Math.E);

          double minRaise = a * Math.Log(b * (double)minAdditionalRaise + c, Math.E);
          if (minRaise < 0)
            minRaise = 0;
          if (minRaise > 0.5)
            minRaise = 0.5;

          double maxRaise = a * Math.Log(b * (double)maxAdditionalRaise + c, Math.E);
          if (maxRaise < 0)
            maxRaise = 0;
          if (maxRaise > 0.5)
            maxRaise = 0.5;

          long hc = ((1L << ((int)hc1 - 1)) | ((1L << ((int)hc2 - 1))));
          double result;
          double total;

          for (double ca = minRaise; ca <= maxRaise; ca += 0.05)
          {
            GetProbFold(stage, earlyPosition, ca, hc, out result, out total);

            result /= total;

            if (ca == minRaise)
              raiseCallActualSuccess = result;

            if (result <= raiseToCallThreshold)
            {
              raiseCallScaled = ca;
              raiseCallActualSuccess = result;
            }

            if (result >= raiseToStealThreshold)
            {
              raiseStealScaled = ca;
              raiseStealActualSuccess = result;
              break;
            }
          }

          if (raiseCallScaled == -1)
            raiseCallScaled = minRaise;

          if (pa > (double)(50 * bigBlind))
            pa = (double)(50 * bigBlind);
          if (pa < (double)(10 * bigBlind))
            pa = (double)(10 * bigBlind);

          b = ((double)(101 * bigBlind) - 2 * pa) / Math.Pow((double)bigBlind - pa, 2);
          c = 1 - (double)bigBlind * b;
          a = 1 / Math.Log((double)(100 * bigBlind) * b + c, Math.E);

          raiseCallScaled = (Math.Exp(raiseCallScaled / a) - c) / b;

          if (raiseStealScaled != -1)
            raiseStealScaled = (Math.Exp(raiseStealScaled / a) - c) / b;
          else
          {
            raiseStealScaled = maxRaise;

            GetProbFold(stage, earlyPosition, raiseStealScaled, hc, out result, out total);

            raiseStealScaled = (Math.Exp(raiseStealScaled / a) - c) / b;

            result /= total;
            raiseStealActualSuccess = result;
          }

          raiseCallAmount = Math.Round((decimal)raiseCallScaled, 2);
          raiseStealAmount = Math.Round((decimal)raiseStealScaled, 2);
        }


        public void SetAllProbsToZeroOnFold()
        {
          folded = true;

          for (int i = 0; i < playerWinRatios.Length; i++)
            playerWinRatioProbs[i] = 0;
        }

        private void GetProbFold(HandState stage, bool earlyPosition, double callAmount, long hc, out double result, out double total)
        {
          int numPWR = playerWinRatios.Length;
          result = 0.0;
          total = 0.0;

          double[,] fold;
          double norm;
          int index = (int)(callAmount * 10);
          if (index == raisedPreflopNormsByCallAmount.Length - 1)
            index--;

          switch (stage)
          {
            case HandState.PreFlop:
              fold = raisedPreflopFold;
              norm = (1 - callAmount * 10 + index) * raisedPreflopNormsByCallAmount[index] +
                  (callAmount * 10 - index) * raisedPreflopNormsByCallAmount[index + 1];
              break;
            default:
              if (earlyPosition)
              {
                fold = raisedPostFlopFoldEarly;
                norm = (1 - callAmount * 10 + index) * raisedPostFlopEarlyNormsByCallAmount[index] +
                    (callAmount * 10 - index) * raisedPostFlopEarlyNormsByCallAmount[index + 1];
              }
              else
              {
                fold = raisedPostFlopFoldLate;
                norm = (1 - callAmount * 10 + index) * raisedPostFlopLateNormsByCallAmount[index] +
                    (callAmount * 10 - index) * raisedPostFlopLateNormsByCallAmount[index + 1];
              }
              break;
          }

          float div = 100.0f / (stage == HandState.PreFlop ? 168 : (0.5f * (52 - (int)stage) * (51 - (int)stage) - 1));
          int lowIndexY = (int)Math.Round(callAmount * 100);

          for (int i = 0; i < numPWR; i++)
          {
            if ((holeCardsLongs[i] & hc) == 0)
            {
              int lowIndexX = (int)(playerWinRatios[i].sortedIndex * div + 0.5f);
              lowIndexX = lowIndexX == 100 ? 99 : lowIndexX;

              double temp = fold[lowIndexY, lowIndexX] * norm;

              temp = Math.Min(0.995, Math.Max(temp, 0.005));

              result += temp * playerWinRatioProbs[i];
              total += playerWinRatioProbs[i];
            }
          }
        }

        #region Unimplemented

        public double GetPerceivedChanceHasBetterHandThanOtherPlayer(PlayerModelFixed otherPlayer)
        {
          throw new NotImplementedException();

          //if (folded || otherPlayer.folded)
          //    throw new Exception("Should not be calling this on folded player");

          //if (!winRatiosSorted)
          //{
          //    //Array.Sort(sortedIndexes, new Comparison<int>((x, y) => { return playerWinRatios[x].winPercentage.CompareTo(playerWinRatios[y].winPercentage); }));
          //    winRatiosSorted = true;
          //}

          //double probBetter = 0, temp = 0;

          //for (int i = 1; i < sortedIndexes.Length; i++)
          //{
          //    temp += playerWinRatios[sortedIndexes[i - 1]].prob;
          //    probBetter += otherPlayer.playerWinRatios[sortedIndexes[i]].prob * temp;
          //}

          //return probBetter;
        }

        public static void GetActionForModelPlayer(Random rand, int sortIndex, HandState handStage, decimal callAmount, decimal potAmount, bool earlyPosition, bool raisedPot, out PokerAction actionToPerform, out decimal amount)
        {
          throw new NotImplementedException();

          //LoadDataIfRequired();

          //double wp = sortIndex / (handStage == HandState.PreFlop ? 168 : (0.5 * (52 - (int)handStage) * (51 - (int)handStage) - 1));
          //double pa = (double)potAmount;

          //double a, b, c;

          //pa -= (double)callAmount;

          //if (pa > 5) pa = 5;
          //if (pa < 1) pa = 1;

          //b = (10.1 - 2 * pa) / Math.Pow(0.1 - pa, 2);
          //c = 1 - 0.1 * b;
          //a = 1 / Math.Log(10 * b + c, Math.E);

          //double ca = a * Math.Log(b * (double)callAmount + c, Math.E);

          //if (ca > 1) ca = 1; if (ca < 0) ca = 0;

          //double foldProb = (raisedPot || (handStage == HandState.PreFlop && callAmount != 0.0m)) ? GetActionPercentage(sortIndex, PokerAction.Fold, handStage, ca, 0, earlyPosition, raisedPot) : 0;
          //double callProb = (raisedPot || (handStage == HandState.PreFlop && callAmount != 0.0m)) ? GetActionPercentage(sortIndex, PokerAction.Call, handStage, ca, 0, earlyPosition, raisedPot) : 0;
          //double checkProb = !(raisedPot || (handStage == HandState.PreFlop && callAmount != 0.0m)) ? GetActionPercentage(sortIndex, PokerAction.Check, handStage, ca, 0, earlyPosition, raisedPot) : 0;

          //double raiseProb = GetActionPercentage(sortIndex, PokerAction.Raise, handStage, ca, double.MaxValue, earlyPosition, raisedPot);

          //var rNum = rand.NextDouble();

          //if (raisedPot || (handStage == HandState.PreFlop && callAmount != 0.0m))
          //{
          //    double norm = foldProb + callProb + raiseProb;
          //    foldProb = foldProb / norm;
          //    callProb = callProb / norm;
          //    raiseProb = raiseProb / norm;

          //    if (rNum <= foldProb)
          //    {
          //        actionToPerform = PokerAction.Fold; amount = 0;
          //        return;
          //    }
          //    else if (rNum <= (foldProb + callProb))
          //    {
          //        actionToPerform = PokerAction.Call; amount = callAmount;
          //        return;
          //    }
          //    else
          //    {

          //        double[] raiseProbs = new double[]{
          //            GetRaiseProbForAmount(wp, 0.0, handStage, raisedPot), 
          //            GetRaiseProbForAmount(wp, 0.1, handStage, raisedPot),
          //            GetRaiseProbForAmount(wp, 0.2, handStage, raisedPot),
          //            GetRaiseProbForAmount(wp, 0.3, handStage, raisedPot),
          //            GetRaiseProbForAmount(wp, 0.4, handStage, raisedPot),
          //            GetRaiseProbForAmount(wp, 0.5, handStage, raisedPot),
          //            GetRaiseProbForAmount(wp, 0.6, handStage, raisedPot),
          //            GetRaiseProbForAmount(wp, 0.7, handStage, raisedPot),
          //            GetRaiseProbForAmount(wp, 0.8, handStage, raisedPot),
          //            GetRaiseProbForAmount(wp, 0.9, handStage, raisedPot),
          //            GetRaiseProbForAmount(wp, 1.0, handStage, raisedPot)
          //        };

          //        double totalRaise = raiseProbs.Sum();

          //        for (int i = 0; i < raiseProbs.Length; i++)
          //            raiseProbs[i] = raiseProbs[i] / totalRaise;
          //        rNum = (rNum - (foldProb + callProb)) / (1.0 - (foldProb + callProb));
          //        double sum = 0;

          //        for (int i = 0; i < raiseProbs.Length; i++)
          //        {
          //            sum += raiseProbs[i];

          //            if (rNum <= sum || i == raiseProbs.Length - 1)
          //            {
          //                actionToPerform = PokerAction.Raise;

          //                pa = (double)(potAmount);

          //                if (pa > 5) pa = 5;
          //                if (pa < 1) pa = 1;

          //                b = (10.1 - 2 * pa) / Math.Pow(0.1 - pa, 2);
          //                c = 1 - 0.1 * b;
          //                a = 1 / Math.Log(10 * b + c, Math.E);

          //                amount = Math.Round((decimal)((Math.Exp((0.1 * i + ((sum - rNum) * 0.1)) / a) - c) / b), 2);

          //                return;
          //            }
          //        }

          //        throw new Exception("Impossible to get here");
          //    }
          //}
          //else
          //{
          //    double norm = checkProb + raiseProb;
          //    checkProb = checkProb / norm;
          //    raiseProb = raiseProb / norm;

          //    if (rNum <= checkProb)
          //    {
          //        actionToPerform = PokerAction.Check; amount = 0;
          //        return;
          //    }
          //    else
          //    {

          //        double[] raiseProbs = new double[]{
          //            GetRaiseProbForAmount(wp, 0.0, handStage, raisedPot), 
          //            GetRaiseProbForAmount(wp, 0.1, handStage, raisedPot),
          //            GetRaiseProbForAmount(wp, 0.2, handStage, raisedPot),
          //            GetRaiseProbForAmount(wp, 0.3, handStage, raisedPot),
          //            GetRaiseProbForAmount(wp, 0.4, handStage, raisedPot),
          //            GetRaiseProbForAmount(wp, 0.5, handStage, raisedPot),
          //            GetRaiseProbForAmount(wp, 0.6, handStage, raisedPot),
          //            GetRaiseProbForAmount(wp, 0.7, handStage, raisedPot),
          //            GetRaiseProbForAmount(wp, 0.8, handStage, raisedPot),
          //            GetRaiseProbForAmount(wp, 0.9, handStage, raisedPot),
          //            GetRaiseProbForAmount(wp, 1.0, handStage, raisedPot)
          //        };

          //        double totalRaise = raiseProbs.Sum();

          //        for (int i = 0; i < raiseProbs.Length; i++)
          //            raiseProbs[i] = raiseProbs[i] / totalRaise;

          //        rNum = (rNum - (checkProb)) / (1.0 - (checkProb));
          //        double sum = 0;

          //        for (int i = 0; i < raiseProbs.Length; i++)
          //        {
          //            sum += raiseProbs[i];

          //            if (rNum <= sum || i == raiseProbs.Length - 1)
          //            {
          //                actionToPerform = PokerAction.Raise;

          //                pa = (double)(potAmount);

          //                if (pa > 5) pa = 5;
          //                if (pa < 1) pa = 1;

          //                b = (10.1 - 2 * pa) / Math.Pow(0.1 - pa, 2);
          //                c = 1 - 0.1 * b;
          //                a = 1 / Math.Log(10 * b + c, Math.E);

          //                amount = Math.Round((decimal)((Math.Exp((0.1 * i + ((sum - rNum) * 0.1)) / a) - c) / b), 2);

          //                return;
          //            }
          //        }

          //        throw new Exception("Impossible to get here");
          //    }
          //}
        }

        public static double GetActionAggression(int sortIndex, HandState handStage, decimal callAmount, decimal bigBlind, decimal potAmount, bool raisedPot, bool earlyPosition, PokerAction performedAction, decimal raiseAmount)
        {
          lock (staticLocker)
            LoadDataIfRequired();

          double wp = sortIndex / (handStage == HandState.PreFlop ? 168 : (0.5 * (52 - (int)handStage) * (51 - (int)handStage) - 1));
          double pa = (double)(potAmount), a, b, c;

          pa -= (double)callAmount;

          if (pa > (double)(50 * bigBlind))
            pa = (double)(50 * bigBlind);
          if (pa < (double)(10 * bigBlind))
            pa = (double)(10 * bigBlind);

          b = ((double)(101 * bigBlind) - 2 * pa) / Math.Pow((double)bigBlind - pa, 2);
          c = 1 - (double)bigBlind * b;
          a = 1 / Math.Log((double)(100 * bigBlind) * b + c, Math.E);

          double ca = a * Math.Log(b * (double)callAmount + c, Math.E);

          pa = (double)(potAmount);

          if (pa > (double)(50 * bigBlind))
            pa = (double)(50 * bigBlind);
          if (pa < (double)(10 * bigBlind))
            pa = (double)(10 * bigBlind);

          b = ((double)(101 * bigBlind) - 2 * pa) / Math.Pow((double)bigBlind - pa, 2);
          c = 1 - (double)bigBlind * b;
          a = 1 / Math.Log((double)(100 * bigBlind) * b + c, Math.E);

          double ra = a * Math.Log(b * (double)raiseAmount + c, Math.E);

          if (ca > 1)
            ca = 1;
          if (ca < 0)
            ca = 0;
          if (ra > 0.999)
            ra = 0.999;
          if (ra < 0)
            ra = 0;

          double foldProb = (raisedPot || (handStage == HandState.PreFlop && callAmount != 0.0m)) ? GetActionPercentage(sortIndex, PokerAction.Fold, handStage, ca, earlyPosition, raisedPot) : 0;
          double callProb = (raisedPot || (handStage == HandState.PreFlop && callAmount != 0.0m)) ? GetActionPercentage(sortIndex, PokerAction.Call, handStage, ca, earlyPosition, raisedPot) : 0;
          double checkProb = !(raisedPot || (handStage == HandState.PreFlop && callAmount != 0.0m)) ? GetActionPercentage(sortIndex, PokerAction.Check, handStage, ca, earlyPosition, raisedPot) : 0;

          double raiseProb = GetActionPercentage(sortIndex, PokerAction.Raise, handStage, ca, earlyPosition, raisedPot);

          if (raisedPot || (handStage == HandState.PreFlop && callAmount != 0.0m))
          {
            double norm = foldProb + callProb + raiseProb;
            foldProb = foldProb / norm;
            callProb = callProb / norm;
            raiseProb = raiseProb / norm;

            if (performedAction == PokerAction.Fold || performedAction == PokerAction.Call)
            {
              return 0.0;// 5 * (foldProb + callProb);
            }
            else
            {

              double[] raiseProbs = new double[]{
                                GetRaiseProbForAmount(wp, 0.0, handStage, raisedPot),
                                GetRaiseProbForAmount(wp, 0.1, handStage, raisedPot),
                                GetRaiseProbForAmount(wp, 0.2, handStage, raisedPot),
                                GetRaiseProbForAmount(wp, 0.3, handStage, raisedPot),
                                GetRaiseProbForAmount(wp, 0.4, handStage, raisedPot),
                                GetRaiseProbForAmount(wp, 0.5, handStage, raisedPot),
                                GetRaiseProbForAmount(wp, 0.6, handStage, raisedPot),
                                GetRaiseProbForAmount(wp, 0.7, handStage, raisedPot),
                                GetRaiseProbForAmount(wp, 0.8, handStage, raisedPot),
                                GetRaiseProbForAmount(wp, 0.9, handStage, raisedPot),
                                GetRaiseProbForAmount(wp, 1.0, handStage, raisedPot)
                            };

              double totalRaise = raiseProbs.Sum();

              for (int i = 0; i < raiseProbs.Length; i++)
                raiseProbs[i] = raiseProbs[i] / totalRaise;

              double sum = 0;

              for (int i = 0; i < raiseProbs.Length; i++)
              {
                sum += raiseProbs[i];
                raiseProbs[i] = sum;
              }

              return foldProb + callProb + raiseProb * ((1.0 - 10.0 * (ra % 0.1)) * raiseProbs[(int)(ra / 0.1)] + 10.0 * (ra % 0.1) * raiseProbs[1 + (int)(ra / 0.1)]);
            }
          }
          else
          {
            double norm = checkProb + raiseProb;
            checkProb = checkProb / norm;
            raiseProb = raiseProb / norm;

            if (performedAction == PokerAction.Check)
            {
              return 0.0;// 5 * checkProb;
            }
            else
            {

              double[] raiseProbs = new double[]{
                                GetRaiseProbForAmount(wp, 0.0, handStage, raisedPot),
                                GetRaiseProbForAmount(wp, 0.1, handStage, raisedPot),
                                GetRaiseProbForAmount(wp, 0.2, handStage, raisedPot),
                                GetRaiseProbForAmount(wp, 0.3, handStage, raisedPot),
                                GetRaiseProbForAmount(wp, 0.4, handStage, raisedPot),
                                GetRaiseProbForAmount(wp, 0.5, handStage, raisedPot),
                                GetRaiseProbForAmount(wp, 0.6, handStage, raisedPot),
                                GetRaiseProbForAmount(wp, 0.7, handStage, raisedPot),
                                GetRaiseProbForAmount(wp, 0.8, handStage, raisedPot),
                                GetRaiseProbForAmount(wp, 0.9, handStage, raisedPot),
                                GetRaiseProbForAmount(wp, 1.0, handStage, raisedPot)
                            };

              double totalRaise = raiseProbs.Sum();

              for (int i = 0; i < raiseProbs.Length; i++)
                raiseProbs[i] = raiseProbs[i] / totalRaise;

              double sum = 0;

              for (int i = 0; i < raiseProbs.Length; i++)
              {
                sum += raiseProbs[i];
                raiseProbs[i] = sum;
              }

              return checkProb + raiseProb * ((1.0 - 10.0 * (ra % 0.1)) * raiseProbs[(int)(ra / 0.1)] + 10.0 * (ra % 0.1) * raiseProbs[1 + (int)(ra / 0.1)]);
            }
          }
        }

        //Function derived from player "surprising" fits as of 22/09/10                
        private static double GetActionPercentage(int sortIndex, PokerAction action, HandState handStage, double callAmount, bool earlyPosition, bool raisedPot)
        {
          if (sortIndex < 0)
            return 0;

          if (!raisedPot)
          {
            double[] data = null;
            if (handStage == HandState.PreFlop)
            {
              switch (action)
              {
                case PokerAction.Fold:
                  data = unraisedPreflopFold;
                  break;
                case PokerAction.Check:
                  data = unraisedPreflopFold;
                  break;
                case PokerAction.Call:
                  data = unraisedPreflopCall;
                  break;
                case PokerAction.Raise:
                  data = unraisedPreflopRaise;
                  break;
              }
            }
            else
            {
              if (action == PokerAction.Raise)
              {
                if (earlyPosition)
                  data = unraisedPostflopRaisedEarly;
                else
                  data = unraisedPostflopRaisedLate;
              }
              else if (action == PokerAction.Check)
              {
                if (earlyPosition)
                  data = unraisedPostflopCheckEarly;
                else
                  data = unraisedPostflopCheckLate;
              }
              else
                throw new Exception();
            }

            double wp = sortIndex / (handStage == HandState.PreFlop ? 168 : (0.5 * (52 - (int)handStage) * (51 - (int)handStage) - 1));

            int lowIndex = (int)Math.Round(wp * 100);

            return data[lowIndex];
          }
          else
          {
            double[,] data = null;

            if (handStage == HandState.PreFlop)
            {
              switch (action)
              {
                case PokerAction.Fold:
                  data = raisedPreflopFold;
                  break;
                case PokerAction.Call:
                  data = raisedPreflopCall;
                  break;
                case PokerAction.Raise:
                  data = raisedPreflopRaise;
                  break;
              }
            }
            else
            {
              if (action == PokerAction.Raise)
              {
                if (earlyPosition)
                  data = raisedPostFlopRaiseEarly;
                else
                  data = raisedPostFlopRaiseLate;
              }
              else if (action == PokerAction.Call)
              {
                if (earlyPosition)
                  data = raisedPostFlopCallEarly;
                else
                  data = raisedPostFlopCallLate;
              }
              else if (action == PokerAction.Fold)
              {
                if (earlyPosition)
                  data = raisedPostFlopFoldEarly;
                else
                  data = raisedPostFlopFoldLate;
              }
              else
                throw new Exception();
            }

            double wp = sortIndex / (handStage == HandState.PreFlop ? 168 : (0.5 * (52 - (int)handStage) * (51 - (int)handStage) - 1));

            int lowIndexX = (int)Math.Round(wp * 100);
            int lowIndexY = (int)Math.Round(callAmount * 100);

            double result;
            result = data[lowIndexY, lowIndexX];

            if (result < 0.005)
              result = 0.005;

            return result;
          }

          throw new Exception();
        }

        private static double GetRaiseProbForAmount(double wp, double ra, HandState handStage, bool raisedPot)
        {
          double a, b, c, d;

          if (raisedPot)
          {
            if (handStage == HandState.PreFlop)
            { a = raisedPreflopA; b = raisedPreflopB; c = raisedPreflopC; d = raisedPreflopD; }
            else
            { a = raisedPostflopA; b = raisedPostflopB; c = raisedPostflopC; d = raisedPostflopD; }
          }
          else
          {
            if (handStage == HandState.PreFlop)
            { a = unraisedPreflopA; b = unraisedPreflopB; c = unraisedPreflopC; d = unraisedPreflopD; }
            else
            { a = unraisedPostflopA; b = unraisedPostflopB; c = unraisedPostflopC; d = unraisedPostflopD; }
          }

          double x = ra - a * wp - b, y = (c * wp + d < 0.05) ? 0.05 : (c * wp + d);
          y = y * y;
          x = x * x;

          return Math.Exp(-x / (2 * y)) / Math.Sqrt(2 * Math.PI * y);

          throw new Exception();
        }

        #endregion
      }
    }
  }
}
