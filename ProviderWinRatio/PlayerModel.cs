//using System;
//using System.Collections.Generic;
//using System.Linq;
//using System.Text;
//using System.Collections;
//using PokerBot.Definitions;
//using System.IO;
//using Microsoft.Win32;

//namespace PokerBot.AI.ProviderWinRatio
//{
//    public partial class WinRatioProvider
//    {

//        public partial class TableModel
//        {

//            internal class PlayerModel
//            {
//                struct playerCardsProb
//                {
//                    public Card card1, card2;
//                    public int sortedIndex;
//                    public ushort winPercentage;
//                    //public double prob;

//                    public playerCardsProb(Card card1, Card card2)
//                    {
//                        if ((byte)card1 > (byte)card2)
//                        { this.card1 = card1; this.card2 = card2; }
//                        else
//                        { this.card1 = card2; this.card2 = card1; }

//                        this.sortedIndex = -1;
//                        this.winPercentage = 0;                        
//                    }
//                }

//                playerCardsProb[] playerWinRatios = new playerCardsProb[52 * 51 / 2];                
//                double[] playerWinRatioProbs = new double[52 * 51 / 2];              

//                bool folded = false;
//                bool winRatiosSorted = false;
//                WinRatioProvider wrProv;
//                long playerId, tableId, handId;
//                long tableCardsOnLastUpdate = long.MaxValue;
//                int numPlayersOnLastUpdate = -1;

//                static object locker = new object();
//                static playerCardsProb[][] storedPreflopPlayerWinRatios;
//                static double[][] storedPreflopProbs;
//                static long[] holeCardsLongs;

//                #region Action percentage data

//                static double[] unraisedPreflopFold, unraisedPreflopCall, unraisedPreflopRaise, unraisedPostflopRaisedEarly, unraisedPostflopRaisedLate, unraisedPostflopCheckEarly, unraisedPostflopCheckLate;
//                static double[,] raisedPreflopFold, raisedPreflopCall, raisedPreflopRaise, raisedPostFlopFoldEarly, raisedPostFlopFoldLate, raisedPostFlopCallEarly, raisedPostFlopCallLate, raisedPostFlopRaiseEarly, raisedPostFlopRaiseLate;

//                #endregion

//                #region Action percentage model params

//                static double unraisedPreflopA = -0.0848, unraisedPreflopC = 0.000, raisedPreflopA = -1.8405, raisedPreflopC = -1.7008, unraisedPostflopA = -0.1579, unraisedPostflopC = -0.1207, raisedPostflopA = -0.3744, raisedPostflopC = -0.1479;
//                static double unraisedPreflopB = 0.2524, unraisedPreflopD = 0.0388, raisedPreflopB = 0.4334, raisedPreflopD = 0.427, unraisedPostflopB = 0.4196, unraisedPostflopD = 0.2002, raisedPostflopB = 0.4603, raisedPostflopD = 0.1608;

//                #endregion

//                public PlayerModel(long tableId, long handId, long playerId, WinRatioProvider wrProv)
//                {
//                    this.wrProv = wrProv;
//                    this.playerId = playerId;
//                    this.tableId = tableId;
//                    this.handId = handId;
//                    int count = 0;

//                    lock (staticLocker)
//                    {
//                        if (storedPreflopPlayerWinRatios == null)
//                        {
//                            storedPreflopPlayerWinRatios = new playerCardsProb[9][];
//                            storedPreflopProbs = new double[9][];
//                            holeCardsLongs = new long[52 * 51 / 2];
//                            var tableCards = new Card[0];

//                            for (int n = 2; n <= 10; n++)
//                            {
//                                count = 0;
//                                storedPreflopPlayerWinRatios[n - 2] = new playerCardsProb[52 * 51 / 2];
//                                storedPreflopProbs[n - 2] = new double[52 * 51 / 2];

//                                for (byte i = 1; i <= 52; i++)
//                                {
//                                    for (byte j = 1; j < i; j++)
//                                    {
//                                        storedPreflopPlayerWinRatios[n - 2][count] = new playerCardsProb((Card)i, (Card)j);
//                                        storedPreflopPlayerWinRatios[n - 2][count].sortedIndex = wrProv.GetWinPercentageIndex((Card)i, (Card)j, tableCards, n, tableId, handId);
//                                        storedPreflopPlayerWinRatios[n - 2][count].winPercentage = wrProv.GetWinPercentageShort((Card)i, (Card)j, tableCards, n, tableId, handId);
//                                        storedPreflopProbs[n - 2][count] = 1 / 1326.0;
//                                        holeCardsLongs[count] = ((1L << (i - 1)) | (1L << (j - 1)));
//                                        count++;
//                                    }
//                                }
//                            }
//                        }
//                    }
//                    Array.Copy(storedPreflopPlayerWinRatios[8], playerWinRatios, playerWinRatios.Length); tableCardsOnLastUpdate = 0; numPlayersOnLastUpdate = 10;
//                    Array.Copy(storedPreflopProbs[8], playerWinRatioProbs, playerWinRatioProbs.Length);

//                    LoadDataIfRequired();
//                }

//                private static void LoadDataIfRequired()
//                {
//                    lock (locker)
//                    {
//                        if (unraisedPreflopFold == null)
//                        {
//                            if (CurrentJob == null || CurrentJob.PlayerModels == null || CurrentJob.PlayerModels.Count == 0)
//                            {
//                                //Legacy moved functionality to file locations
//                                //RegistryKey key = Registry.LocalMachine.OpenSubKey("Software\\FullBotPoker");
//                                //try
//                                //{
//                                //    if (key != null)
//                                //    {
//                                //        fileDir = key.GetValue("WRFitsDir") as string;

//                                //        if (fileDir == null)
//                                //            throw new Exception("WR fits value does not exist");
//                                //    }
//                                //    else
//                                //    {
//                                //        throw new Exception("Full bot poker key not found");
//                                //    }
//                                //}
//                                //catch (Exception ex)
//                                //{
//                                //    throw ex;
//                                //}

//                                fileDir = FileLocations.WinRatioPlayerModelsDir;

//                                LoadData(Path.Combine(fileDir, unraisedPreflopFoldFN), out unraisedPreflopFold, false);
//                                LoadData(Path.Combine(fileDir, unraisedPreflopCallFN), out unraisedPreflopCall, false);
//                                LoadData(Path.Combine(fileDir, unraisedPreflopRaiseFN), out unraisedPreflopRaise, false);
//                                LoadData(Path.Combine(fileDir, unraisedPostflopRaisedEarlyFN), out unraisedPostflopRaisedEarly, false);
//                                LoadData(Path.Combine(fileDir, unraisedPostflopRaisedLateFN), out unraisedPostflopRaisedLate, false);
//                                LoadData(Path.Combine(fileDir, unraisedPostflopCheckEarlyFN), out unraisedPostflopCheckEarly, false);
//                                LoadData(Path.Combine(fileDir, unraisedPostflopCheckLateFN), out unraisedPostflopCheckLate, false);

//                                LoadData(Path.Combine(fileDir, raisedPostFlopCallEarlyFN), out raisedPostFlopCallEarly, false);
//                                LoadData(Path.Combine(fileDir, raisedPostFlopCallLateFN), out raisedPostFlopCallLate, false);
//                                LoadData(Path.Combine(fileDir, raisedPostFlopFoldEarlyFN), out raisedPostFlopFoldEarly, false);
//                                LoadData(Path.Combine(fileDir, raisedPostFlopFoldLateFN), out raisedPostFlopFoldLate, false);
//                                LoadData(Path.Combine(fileDir, raisedPostFlopRaiseEarlyFN), out raisedPostFlopRaiseEarly, false);
//                                LoadData(Path.Combine(fileDir, raisedPostFlopRaiseLateFN), out raisedPostFlopRaiseLate, false);

//                                LoadData(Path.Combine(fileDir, raisedPreflopCallFN), out raisedPreflopCall, false);
//                                LoadData(Path.Combine(fileDir, raisedPreflopFoldFN), out raisedPreflopFold, false);
//                                LoadData(Path.Combine(fileDir, raisedPreflopRaiseFN), out raisedPreflopRaise, false);
//                            }
//                            else
//                            {
//                                LoadData(unraisedPreflopFoldFN, out unraisedPreflopFold, true);
//                                LoadData(unraisedPreflopCallFN, out unraisedPreflopCall, true);
//                                LoadData(unraisedPreflopRaiseFN, out unraisedPreflopRaise, true);
//                                LoadData(unraisedPostflopRaisedEarlyFN, out unraisedPostflopRaisedEarly, true);
//                                LoadData(unraisedPostflopRaisedLateFN, out unraisedPostflopRaisedLate, true);
//                                LoadData(unraisedPostflopCheckEarlyFN, out unraisedPostflopCheckEarly, true);
//                                LoadData(unraisedPostflopCheckLateFN, out unraisedPostflopCheckLate, true);

//                                LoadData(raisedPostFlopCallEarlyFN, out raisedPostFlopCallEarly, true);
//                                LoadData(raisedPostFlopCallLateFN, out raisedPostFlopCallLate, true);
//                                LoadData(raisedPostFlopFoldEarlyFN, out raisedPostFlopFoldEarly, true);
//                                LoadData(raisedPostFlopFoldLateFN, out raisedPostFlopFoldLate, true);
//                                LoadData(raisedPostFlopRaiseEarlyFN, out raisedPostFlopRaiseEarly, true);
//                                LoadData(raisedPostFlopRaiseLateFN, out raisedPostFlopRaiseLate, true);

//                                LoadData(raisedPreflopCallFN, out raisedPreflopCall, true);
//                                LoadData(raisedPreflopFoldFN, out raisedPreflopFold, true);
//                                LoadData(raisedPreflopRaiseFN, out raisedPreflopRaise, true);
//                            }

//                        }
//                    }
//                }

//                private static void LoadData(string fileName, out double[] data, bool fromJob)
//                {
//                    string[] lines = fromJob ? CurrentJob.PlayerModels[fileName] : File.ReadAllLines(fileName);

//                    if (lines.Length != 101)
//                        throw new Exception();

//                    data = new double[101];

//                    for (int i = 0; i < 101; i++)
//                        data[i] = double.Parse(lines[i]);
//                }

//                private static void LoadData(string fileName, out double[,] data, bool fromJob)
//                {
//                    string[] lines = fromJob ? CurrentJob.PlayerModels[fileName] : File.ReadAllLines(fileName);

//                    if (lines.Length != 101 * 101)
//                        throw new Exception();

//                    data = new double[101, 101];

//                    for (int i = 0; i < 101; i++)
//                    {
//                        for (int j = 0; j < 101; j++)
//                            data[i, j] = double.Parse(lines[i * 101 + j]);
//                    }
//                }

//                public void ResetProbsToDefault(long handId)
//                {
//                    Array.Copy(storedPreflopPlayerWinRatios[8], playerWinRatios, playerWinRatios.Length); tableCardsOnLastUpdate = 0; numPlayersOnLastUpdate = 10;
//                    Array.Copy(storedPreflopProbs[8], playerWinRatioProbs, playerWinRatioProbs.Length);

//                    folded = false; winRatiosSorted = false; this.handId = handId;                    
//                }

//                public void UpdateCardsWinPercentages(Card[] tableCards, int numPlayers)
//                {
//                    if (folded)
//                        return;

//                    long tableCardsL = 0;

//                    for (int i = 0; i < tableCards.Length; i++)
//                        tableCardsL |= 1L << ((byte)(tableCards[i]) - 1);

//                    if (tableCardsL == tableCardsOnLastUpdate && numPlayersOnLastUpdate == numPlayers)
//                        return;

//                    tableCardsOnLastUpdate = tableCardsL; numPlayersOnLastUpdate = numPlayers;

//                    long handCards = 0;
//                    int count = playerWinRatios.Length;                    
//                    winRatiosSorted = false;

//                    if (tableCards.Length != 0)
//                    {
//                        var wrEntry = wrProv.GetWinPercentageEntry(tableCards, numPlayers, tableId, handId);
//                        wrEntry.PrepForLargeNumberGets((HandState)(tableCards.Length), numPlayers);

//                        for (int i = 0; i < count; i++)
//                        {
//                            handCards = holeCardsLongs[i];// ((1L << ((byte)(playerWinRatios[i].card1) - 1)) | (1L << ((byte)(playerWinRatios[i].card2) - 1)));

//                            if ((handCards & tableCardsL) == 0)
//                            {
//                                playerWinRatios[i].sortedIndex = wrEntry.GetPrepSortedIndex(playerWinRatios[i].card1, playerWinRatios[i].card2);
//                                playerWinRatios[i].winPercentage = wrEntry.GetPrepWinRatio(playerWinRatios[i].card1, playerWinRatios[i].card2);
//                            }
//                            else
//                            {
//                                playerWinRatios[i].sortedIndex = -1;
//                                playerWinRatios[i].winPercentage = 0;
//                            }
//                        }
//                    }
//                    else
//                    {
//                        Array.Copy(storedPreflopPlayerWinRatios[numPlayers - 2], playerWinRatios, playerWinRatios.Length); 
//                    }
//                }

//                public void UpdateCardProbsBasedOnAction(PokerAction action, HandState handStage, decimal callAmount, decimal raiseAmount, decimal potAmount, bool raisedPot, bool earlyPosition)
//                {
//                    if (folded)
//                        throw new Exception("A folded player cannot act");

//                    double pa = (double)potAmount;

//                    double a, b, c;

//                    pa -= (double)callAmount;

//                    if (pa > 5) pa = 5;
//                    if (pa < 1) pa = 1;

//                    b = (10.1 - 2 * pa) / Math.Pow(0.1 - pa, 2);
//                    c = 1 - 0.1 * b;
//                    a = 1 / Math.Log(10 * b + c, Math.E);

//                    double ca = a * Math.Log(b * (double)callAmount + c, Math.E);

//                    if (ca > 1) ca = 1; if (ca < 0) ca = 0;

//                    pa = (double)(potAmount);

//                    if (pa > 5) pa = 5;
//                    if (pa < 1) pa = 1;

//                    b = (10.1 - 2 * pa) / Math.Pow(0.1 - pa, 2);
//                    c = 1 - 0.1 * b;
//                    a = 1 / Math.Log(10 * b + c, Math.E);

//                    double ra = a * Math.Log(b * (double)raiseAmount + c, Math.E);

//                    if (ra > 1) ra = 1; if (ra < 0) ra = 0;

//                    double totalActionPercentage = 0;

//                    for (int i = 0; i < playerWinRatios.Length; i++)
//                    {
//                        playerWinRatioProbs[i] = playerWinRatioProbs[i] * GetActionPercentage(playerWinRatios[i].sortedIndex, action, handStage, ca, ra, earlyPosition, raisedPot);
//                        totalActionPercentage += playerWinRatioProbs[i];
//                    }

//                    totalActionPercentage = 1.0 / totalActionPercentage;

//                    for (int i = 0; i < playerWinRatios.Length; i++)
//                        playerWinRatioProbs[i] = playerWinRatioProbs[i] * totalActionPercentage;
//                }

//                public void UpdateProbsAfterCardDealt(Card dealtCard)
//                {
//                    if (folded)
//                        return;

//                    double probHadCard = 0.0;
//                    long dealtCardLong = (1L << ((int)dealtCard - 1));

//                    for (int i = 0; i < playerWinRatios.Length; i++)
//                    {
//                        if ((holeCardsLongs[i] & dealtCardLong) != 0)
//                        {
//                            probHadCard += playerWinRatioProbs[i];
//                            playerWinRatioProbs[i] = 0;
//                        }
//                    }

//                    probHadCard = 1.0 / (1.0 - probHadCard);

//                    for (int i = 0; i < playerWinRatioProbs.Length; i++)
//                        playerWinRatioProbs[i] = playerWinRatioProbs[i] * probHadCard;
//                }

//                public double GetProbHaveBetterWinPercentageThan(ushort winPercentage, Card hc1, Card hc2)
//                {
//                    if (folded)
//                        return 0;

//                    int numPWR = playerWinRatios.Length;
//                    double result = 0.0, total = 0.0;
//                    long hc = ((1L << ((int)hc1 - 1)) | ((1L << ((int)hc2 - 1))));

//                    for (int i = 0; i < numPWR; i++)
//                    {
//                        if ((holeCardsLongs[i] & hc) == 0)
//                        {
//                            if (playerWinRatios[i].winPercentage > winPercentage)
//                                result += playerWinRatioProbs[i];

//                            total += playerWinRatioProbs[i];
//                        }                        
//                    }

//                    return result / total;
//                }

//                public void SetAllProbsToZeroOnFold()
//                {
//                    folded = true;

//                    for (int i = 0; i < playerWinRatios.Length; i++)
//                        playerWinRatioProbs[i] = 0;                    
//                }

//                //Function derived from player "surprising" fits as of 22/09/10                
//                private static double GetActionPercentage(int sortIndex, PokerAction action, HandState handStage, double callAmount, double raiseAmount, bool earlyPosition, bool raisedPot)
//                {
//                    if (sortIndex < 0)
//                        return 0;

//                    if (!raisedPot)
//                    {
//                        double[] data = null;
//                        if (handStage == HandState.PreFlop)
//                        {
//                            switch (action)
//                            {
//                                case PokerAction.Fold:
//                                    data = unraisedPreflopFold;
//                                    break;
//                                case PokerAction.Check:
//                                    data = unraisedPreflopFold;
//                                    break;
//                                case PokerAction.Call:
//                                    data = unraisedPreflopCall;
//                                    break;
//                                case PokerAction.Raise:
//                                    data = unraisedPreflopRaise;
//                                    break;
//                            }
//                        }
//                        else
//                        {
//                            if (action == PokerAction.Raise)
//                            {
//                                if (earlyPosition)
//                                    data = unraisedPostflopRaisedEarly;
//                                else
//                                    data = unraisedPostflopRaisedLate;
//                            }
//                            else if (action == PokerAction.Check)
//                            {
//                                if (earlyPosition)
//                                    data = unraisedPostflopCheckEarly;
//                                else
//                                    data = unraisedPostflopCheckLate;
//                            }
//                            else throw new Exception();
//                        }

//                        double wp = sortIndex / (handStage == HandState.PreFlop ? 168 : (0.5 * (52 - (int)handStage) * (51 - (int)handStage) - 1));

//                        int lowIndex = (int)Math.Round(wp * 100);

//                        double result = data[lowIndex];

//                        if (action == PokerAction.Raise && raiseAmount != double.MaxValue)
//                            result *= GetRaiseProbForAmount(wp, raiseAmount, handStage, raisedPot);

//                        return result;
//                    }
//                    else
//                    {
//                        double[,] data = null;

//                        if (handStage == HandState.PreFlop)
//                        {
//                            switch (action)
//                            {
//                                case PokerAction.Fold:
//                                    data = raisedPreflopFold;
//                                    break;
//                                case PokerAction.Call:
//                                    data = raisedPreflopCall;
//                                    break;
//                                case PokerAction.Raise:
//                                    data = raisedPreflopRaise;
//                                    break;
//                            }
//                        }
//                        else
//                        {
//                            if (action == PokerAction.Raise)
//                            {
//                                if (earlyPosition)
//                                    data = raisedPostFlopRaiseEarly;
//                                else
//                                    data = raisedPostFlopRaiseLate;
//                            }
//                            else if (action == PokerAction.Call)
//                            {
//                                if (earlyPosition)
//                                    data = raisedPostFlopCallEarly;
//                                else
//                                    data = raisedPostFlopCallLate;
//                            }
//                            else if (action == PokerAction.Fold)
//                            {
//                                if (earlyPosition)
//                                    data = raisedPostFlopFoldEarly;
//                                else
//                                    data = raisedPostFlopFoldLate;
//                            }
//                            else throw new Exception();
//                        }

//                        double wp = sortIndex / (handStage == HandState.PreFlop ? 168 : (0.5 * (52 - (int)handStage) * (51 - (int)handStage) - 1));

//                        int lowIndexX = (int)Math.Round(wp * 100);
//                        int lowIndexY = (int)Math.Round(callAmount * 100);

//                        double result;
//                        result = data[lowIndexX, lowIndexY];

//                        if (result < 0.005)
//                            result = 0.005;

//                        if (action == PokerAction.Raise)
//                            result *= GetRaiseProbForAmount(wp, raiseAmount, handStage, raisedPot);

//                        return result;
//                    }

//                    throw new Exception();
//                }

//                private static double GetProbFold(int sortIndex, HandState handStage, double callAmount, bool earlyPosition)
//                {
//                    double[,] fold, call, raise;

//                    switch (handStage)
//                    {
//                        case HandState.PreFlop:
//                            fold =  raisedPreflopFold;
//                            call = raisedPreflopCall;
//                            raise = raisedPreflopRaise;
//                            break;
//                        default:
//                            if (earlyPosition)
//                            {
//                                fold = raisedPostFlopFoldEarly;
//                                call = raisedPostFlopCallEarly;
//                                raise = raisedPostFlopRaiseEarly;
//                            }
//                            else
//                            {
//                                fold = raisedPostFlopFoldLate;
//                                call = raisedPostFlopCallLate;
//                                raise = raisedPostFlopRaiseLate;
//                            }
//                            break;
//                    }

//                    double wp = sortIndex / (handStage == HandState.PreFlop ? 168 : (0.5 * (52 - (int)handStage) * (51 - (int)handStage) - 1));

//                    int lowIndexX = (int)Math.Round(wp * 100);
//                    int lowIndexY = (int)Math.Round(callAmount * 100);

//                    double result = fold[lowIndexX, lowIndexY] / (fold[lowIndexX, lowIndexY] + call[lowIndexX, lowIndexY] + raise[lowIndexX, lowIndexY]);

//                    return result;
//                }


//                //public static void GetActionForModelPlayer(Random rand, int sortIndex, HandState handStage, decimal callAmount, decimal potAmount, bool earlyPosition, bool raisedPot, out PokerAction actionToPerform, out decimal amount)
//                //{
//                //    LoadDataIfRequired();

//                //    double wp = sortIndex / (handStage == HandState.PreFlop ? 168 : (0.5 * (52 - (int)handStage) * (51 - (int)handStage) - 1));
//                //    double pa = (double)potAmount;

//                //    double a, b, c;

//                //    pa -= (double)callAmount;

//                //    if (pa > 5) pa = 5;
//                //    if (pa < 1) pa = 1;

//                //    b = (10.1 - 2 * pa) / Math.Pow(0.1 - pa, 2);
//                //    c = 1 - 0.1 * b;
//                //    a = 1 / Math.Log(10 * b + c, Math.E);

//                //    double ca = a * Math.Log(b * (double)callAmount + c, Math.E);

//                //    if (ca > 1) ca = 1; if (ca < 0) ca = 0;

//                //    double foldProb = (raisedPot || (handStage == HandState.PreFlop && callAmount != 0.0m)) ? GetActionPercentage(sortIndex, PokerAction.Fold, handStage, ca, 0, earlyPosition, raisedPot) : 0;
//                //    double callProb = (raisedPot || (handStage == HandState.PreFlop && callAmount != 0.0m)) ? GetActionPercentage(sortIndex, PokerAction.Call, handStage, ca, 0, earlyPosition, raisedPot) : 0;
//                //    double checkProb = !(raisedPot || (handStage == HandState.PreFlop && callAmount != 0.0m)) ? GetActionPercentage(sortIndex, PokerAction.Check, handStage, ca, 0, earlyPosition, raisedPot) : 0;

//                //    double raiseProb = GetActionPercentage(sortIndex, PokerAction.Raise, handStage, ca, double.MaxValue, earlyPosition, raisedPot);

//                //    var rNum = rand.NextDouble();

//                //    if (raisedPot || (handStage == HandState.PreFlop && callAmount != 0.0m))
//                //    {
//                //        double norm = foldProb + callProb + raiseProb;
//                //        foldProb = foldProb / norm;
//                //        callProb = callProb / norm;
//                //        raiseProb = raiseProb / norm;

//                //        if (rNum <= foldProb)
//                //        {
//                //            actionToPerform = PokerAction.Fold; amount = 0;
//                //            return;
//                //        }
//                //        else if (rNum <= (foldProb + callProb))
//                //        {
//                //            actionToPerform = PokerAction.Call; amount = callAmount;
//                //            return;
//                //        }
//                //        else
//                //        {

//                //            double[] raiseProbs = new double[]{
//                //                GetRaiseProbForAmount(wp, 0.0, handStage, raisedPot), 
//                //                GetRaiseProbForAmount(wp, 0.1, handStage, raisedPot),
//                //                GetRaiseProbForAmount(wp, 0.2, handStage, raisedPot),
//                //                GetRaiseProbForAmount(wp, 0.3, handStage, raisedPot),
//                //                GetRaiseProbForAmount(wp, 0.4, handStage, raisedPot),
//                //                GetRaiseProbForAmount(wp, 0.5, handStage, raisedPot),
//                //                GetRaiseProbForAmount(wp, 0.6, handStage, raisedPot),
//                //                GetRaiseProbForAmount(wp, 0.7, handStage, raisedPot),
//                //                GetRaiseProbForAmount(wp, 0.8, handStage, raisedPot),
//                //                GetRaiseProbForAmount(wp, 0.9, handStage, raisedPot),
//                //                GetRaiseProbForAmount(wp, 1.0, handStage, raisedPot)
//                //            };

//                //            double totalRaise = raiseProbs.Sum();

//                //            for (int i = 0; i < raiseProbs.Length; i++)
//                //                raiseProbs[i] = raiseProbs[i] / totalRaise;
//                //            rNum = (rNum - (foldProb + callProb)) / (1.0 - (foldProb + callProb));
//                //            double sum = 0;

//                //            for (int i = 0; i < raiseProbs.Length; i++)
//                //            {
//                //                sum += raiseProbs[i];

//                //                if (rNum <= sum || i == raiseProbs.Length - 1)
//                //                {
//                //                    actionToPerform = PokerAction.Raise;

//                //                    pa = (double)(potAmount);

//                //                    if (pa > 5) pa = 5;
//                //                    if (pa < 1) pa = 1;

//                //                    b = (10.1 - 2 * pa) / Math.Pow(0.1 - pa, 2);
//                //                    c = 1 - 0.1 * b;
//                //                    a = 1 / Math.Log(10 * b + c, Math.E);

//                //                    amount = Math.Round((decimal)((Math.Exp((0.1 * i + ((sum - rNum) * 0.1)) / a) - c) / b), 2);

//                //                    return;
//                //                }
//                //            }

//                //            throw new Exception("Impossible to get here");
//                //        }
//                //    }
//                //    else
//                //    {
//                //        double norm = checkProb + raiseProb;
//                //        checkProb = checkProb / norm;                        
//                //        raiseProb = raiseProb / norm;

//                //        if (rNum <= checkProb)
//                //        {
//                //            actionToPerform = PokerAction.Check; amount = 0;
//                //            return;
//                //        }
//                //        else
//                //        {

//                //            double[] raiseProbs = new double[]{
//                //                GetRaiseProbForAmount(wp, 0.0, handStage, raisedPot), 
//                //                GetRaiseProbForAmount(wp, 0.1, handStage, raisedPot),
//                //                GetRaiseProbForAmount(wp, 0.2, handStage, raisedPot),
//                //                GetRaiseProbForAmount(wp, 0.3, handStage, raisedPot),
//                //                GetRaiseProbForAmount(wp, 0.4, handStage, raisedPot),
//                //                GetRaiseProbForAmount(wp, 0.5, handStage, raisedPot),
//                //                GetRaiseProbForAmount(wp, 0.6, handStage, raisedPot),
//                //                GetRaiseProbForAmount(wp, 0.7, handStage, raisedPot),
//                //                GetRaiseProbForAmount(wp, 0.8, handStage, raisedPot),
//                //                GetRaiseProbForAmount(wp, 0.9, handStage, raisedPot),
//                //                GetRaiseProbForAmount(wp, 1.0, handStage, raisedPot)
//                //            };

//                //            double totalRaise = raiseProbs.Sum();

//                //            for (int i = 0; i < raiseProbs.Length; i++)
//                //                raiseProbs[i] = raiseProbs[i] / totalRaise;

//                //            rNum = (rNum - (checkProb)) / (1.0 - (checkProb));
//                //            double sum = 0;

//                //            for (int i = 0; i < raiseProbs.Length; i++)
//                //            {
//                //                sum += raiseProbs[i];

//                //                if (rNum <= sum || i == raiseProbs.Length - 1)
//                //                {
//                //                    actionToPerform = PokerAction.Raise;

//                //                    pa = (double)(potAmount);

//                //                    if (pa > 5) pa = 5;
//                //                    if (pa < 1) pa = 1;

//                //                    b = (10.1 - 2 * pa) / Math.Pow(0.1 - pa, 2);
//                //                    c = 1 - 0.1 * b;
//                //                    a = 1 / Math.Log(10 * b + c, Math.E);

//                //                    amount = Math.Round((decimal)((Math.Exp((0.1 * i + ((sum - rNum) * 0.1)) / a) - c) / b), 2);

//                //                    return;
//                //                }
//                //            }

//                //            throw new Exception("Impossible to get here");
//                //        }
//                //    }
//                //}


//                private static double GetRaiseProbForAmount(double wp, double ra, HandState handStage, bool raisedPot)
//                {
//                    double a, b, c, d;

//                    if (raisedPot)
//                    {
//                        if (handStage == HandState.PreFlop)
//                        { a = raisedPreflopA; b = raisedPreflopB; c = raisedPreflopC; d = raisedPreflopD; }
//                        else
//                        { a = raisedPostflopA; b = raisedPostflopB; c = raisedPostflopC; d = raisedPostflopD; }
//                    }
//                    else
//                    {
//                        if (handStage == HandState.PreFlop)
//                        { a = unraisedPreflopA; b = unraisedPreflopB; c = unraisedPreflopC; d = unraisedPreflopD; }
//                        else
//                        { a = unraisedPostflopA; b = unraisedPostflopB; c = unraisedPostflopC; d = unraisedPostflopD; }
//                    }

//                    double x = ra - a * wp - b, y = (c * wp + d < 0.05) ? 0.05 : (c * wp + d);
//                    y = y * y;
//                    x = x * x;

//                    return Math.Exp(-x / (2 * y)) / Math.Sqrt(2 * Math.PI * y);

//                    throw new Exception();
//                }

//                public double GetPerceivedChanceHasBetterHandThanOtherPlayer(PlayerModel otherPlayer)
//                {
//                    throw new NotImplementedException();

//                    //if (folded || otherPlayer.folded)
//                    //    throw new Exception("Should not be calling this on folded player");

//                    //if (!winRatiosSorted)
//                    //{
//                    //    //Array.Sort(sortedIndexes, new Comparison<int>((x, y) => { return playerWinRatios[x].winPercentage.CompareTo(playerWinRatios[y].winPercentage); }));
//                    //    winRatiosSorted = true;
//                    //}

//                    //double probBetter = 0, temp = 0;

//                    //for (int i = 1; i < sortedIndexes.Length; i++)
//                    //{
//                    //    temp += playerWinRatios[sortedIndexes[i - 1]].prob;
//                    //    probBetter += otherPlayer.playerWinRatios[sortedIndexes[i]].prob * temp;
//                    //}

//                    //return probBetter;
//                }

//            }
//        }
//    }
//}