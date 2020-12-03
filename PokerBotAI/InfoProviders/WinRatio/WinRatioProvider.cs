//using System;
//using System.Collections.Generic;
//using System.Linq;
//using System.Text;
//using System.IO;
//using System.Threading;
//using PokerBot.Definitions;
//using GASS.CUDA;
//using GASS.CUDA.Types;
//using System.Runtime.InteropServices;
//using System.Diagnostics;
//using System.Runtime.Serialization.Formatters.Binary;

//namespace PokerBot.AI.InfoProviders
//{
//    public class WinRatioProvider : InfoProviderBase
//    {

//        #region WinRatio result struct

//        /// <summary>
//        /// Stores details of a previous win ratio calculation
//        /// </summary>
//        struct WinRatioResult
//        {
//            /// <summary>
//            /// The table Id in the database of the table the result was created for
//            /// </summary>
//            public long tableId;
//            /// <summary>
//            /// The hand Id of the hand that the result is taken from
//            /// </summary>
//            public long handId;
//            /// <summary>
//            /// The time in ticks that the result was created
//            /// </summary>
//            public long timeCalculatedTicks;

//            /// <summary>
//            /// A card in hand used in the calculation
//            /// </summary>
//            public byte hole1, hole2;
//            /// <summary>
//            /// A card in on the table used in the calculation
//            /// </summary>
//            public byte table1, table2, table3, table4, table5;
//            /// <summary>
//            /// Number of payers in calculation
//            /// </summary>
//            public int numPlayers;
//            /// <summary>
//            /// Whether the calculation used weighted hole cards for opponents
//            /// </summary>
//            public bool weighted;
//            /// <summary>
//            /// The result of the calculation
//            /// </summary>
//            public double winRatio, winPercentage;

//            /// <summary>
//            /// Creates a new win ratio result for storage in previous results
//            /// </summary>
//            /// <param name="tableId">The table Id in the database of the table the result was created for</param>
//            /// <param name="handId">The time in ticks that the result was created</param>
//            /// <param name="numPlayers">Number of payers in calculation</param>
//            /// <param name="hole1"> A card in hand used in the calculation</param>
//            /// <param name="hole2"> A card in hand used in the calculation</param>
//            /// <param name="table1">A card in on the table used in the calculation</param>
//            /// <param name="table2">A card in on the table used in the calculation</param>
//            /// <param name="table3">A card in on the table used in the calculation</param>
//            /// <param name="table4">A card in on the table used in the calculation</param>
//            /// <param name="table5">A card in on the table used in the calculation</param>
//            /// <param name="weighted">Whether the calculation used weighted hole cards for opponents</param>
//            /// <param name="winRatio">The result of the calculation</param>
//            /// <param name="winPercentage"The result of the calculation></param>
//            public WinRatioResult(long tableId, long handId, int numPlayers, byte hole1, byte hole2, byte table1, byte table2, byte table3, byte table4, byte table5, bool weighted, double winRatio, double winPercentage)
//            {                
//                this.tableId = tableId;
//                this.handId = handId;
//                this.hole1 = hole1; this.hole2 = hole2;
//                this.table1 = table1; this.table2 = table2; this.table3 = table3; this.table4 = table4; this.table5 = table5;
//                this.numPlayers = numPlayers;
//                this.weighted = weighted;
//                this.winRatio = winRatio;
//                this.winPercentage = winPercentage;

//                this.timeCalculatedTicks = DateTime.Now.Ticks;
//            }
//        }

//        #endregion

//        #region Stored results

//        static Dictionary<long, uint> allPreviousResults;
//        static object previousResultsLocker = new object();
//        static string savedResultsDefaulDrive;
//        static readonly string savedResultsFileName = "SavedWR.WRdat";
//        static DateTime lastSave = DateTime.Now;

//        double[, , , ,] preFlopWR;        

//        #endregion

//        #region CPU calc params

//        int numThreads = System.Environment.ProcessorCount;
//        static object handRankLocker = new object();
//        static int[] handRank;

//        #endregion

//        #region GPU calc params

//        bool CUDAInitInWorkerThread = true;
//        WinRatioGPU cudaHandEval;
//        WinRatioGPU cudaHandEval2;
//        static object locker = new object();

//        #endregion

//        /// <summary>
//        /// Contructor
//        /// </summary>
//        /// <param name="information"></param>
//        /// <param name="globalRequestedInfoTypes"></param>
//        /// <param name="allInformationProviders"></param>
//        /// <param name="cacheTracker"></param>
//        public WinRatioProvider(InfoCollection information, Dictionary<InfoProviderType, InfoProviderBase> allInformationProviders, CacheTracker cacheTracker)
//            : base(information, InfoProviderType.WinRatio, allInformationProviders, cacheTracker, false)
//        {
//            requiredInfoTypes = new List<InfoType>() { };
//            providedInfoTypes = new List<InfoPiece>() { new InfoPiece(InfoType.WR_CardsOnlyWinRatio, 0),
//                                                        new InfoPiece(InfoType.WR_CardsOnlyWinPercentage, 0),
//                                                        new InfoPiece(InfoType.WR_CardsOnlyWeightedPercentage, 0),
//                                                        new InfoPiece(InfoType.WR_CardsOnlyOpponentWinPercentage, 1),
//                                                        new InfoPiece(InfoType.WR_CardsOnlyWeightedOpponentWinPercentage, 1),
//                                                        new InfoPiece(InfoType.WR_CardsOnlyWinPercentageLastRoundChange, 0.5)
//                                                        };
//            AddProviderInformationTypes();

//            lock (previousResultsLocker)
//            {
//                if (allPreviousResults == null)
//                {
//                    var drives = DriveInfo.GetDrives();
//                    bool yPresent = false;

//                    for (int i = 0; i < drives.Length; i++)
//                    {
//                        if (drives[i].Name.Contains('Y') || drives[i].Name.Contains('y'))
//                            yPresent = true;
//                    }

//                    if (yPresent)
//                        savedResultsDefaulDrive = "Y:\\";
//                    else
//                        savedResultsDefaulDrive = "C:\\";

//                    try
//                    {
//                        //byte[] data = null;

//                        if (File.Exists(Path.Combine(savedResultsDefaulDrive, savedResultsFileName)))
//                        {
//                            /*
//                            //data = File.ReadAllBytes(Path.Combine(savedResultsDefaulDrive, savedResultsFileName));
//                            BinaryFormatter bin = new BinaryFormatter();
//                            Stream stream = File.Open(Path.Combine(savedResultsDefaulDrive, savedResultsFileName), FileMode.Open);
//                            //mem.Write(data, 0, data.Length);
//                            //mem.Seek(0, 0);
//                            //allPreviousResults = bin.Deserialize(mem) as Dictionary<long, uint>;
//                            allPreviousResults = (Dictionary<long, uint>)bin.Deserialize(stream);
//                            stream.Close();
//                             */
//                            allPreviousResults = new Dictionary<long, uint>();
//                            using (var stream = File.OpenRead(Path.Combine(savedResultsDefaulDrive, savedResultsFileName)))
//                            using (var reader = new BinaryReader(stream))
//                            {
//                                while (stream.Position < stream.Length)
//                                {
//                                    var key = reader.ReadInt64();
//                                    var value = reader.ReadUInt32();
//                                    allPreviousResults.Add(key, value);
//                                }
//                            }

//                        }
//                    }
//                    finally
//                    {
//                        if (allPreviousResults == null)
//                            allPreviousResults = new Dictionary<long, uint>();
//                    }
//                }
//            }

//            //If all input objects are null we are using provider outside of AI structure.  Need to init cuda here
//            if (information == null && allInformationProviders == null)
//                CUDAInitInWorkerThread = false;

//            if (!CUDAInitInWorkerThread)
//            {
//                cudaHandEval = WinRatioGPU.Instance(0);
//                if (cudaHandEval != null)
//                {
//                    cudaHandEval2 = WinRatioGPU.Instance(1);
//                }                
//            }

//            lock (handRankLocker)
//                if (handRank == null)
//                    LoadHandRank();

//            //Load pre calculated win ratio for pre flop
//            if (File.Exists(".\\PreFlopWR.dat"))
//            {
//                preFlopWR = new double[2, 9, 52, 52, 2];

//                BinaryReader br = new BinaryReader(File.OpenRead(".\\PreFlopWR.dat"));

//                for (int w = 0; w < 2; w++)
//                {
//                    for (int n = 0; n < 9; n++)
//                    {
//                        for (int i = 0; i < 52; i++)
//                        {
//                            for (int j = 0; j < 52; j++)
//                            {
//                                preFlopWR[w, n, i, j, 0] = br.ReadDouble();
//                                preFlopWR[w, n, i, j, 1] = br.ReadDouble();
//                            }
//                        }
//                    }
//                }

//                br.Close();
//            }

//        }

//        /// <summary>
//        /// Gets the win ratio when for a set of cards.  Use when not using provider as part of AI
//        /// </summary>
//        /// <param name="maxIterations">Number of hands simulated.  Not used for GPU calculation where maxIterations = 512 * 1024</param>
//        /// <param name="numPlayers">Number of players to simulate</param>
//        /// <param name="holeCard1">First hole card</param>
//        /// <param name="holeCard2">Second hole card</param>
//        /// <param name="tableCard1">First table card</param>
//        /// <param name="tableCard2">Second table card</param>
//        /// <param name="tableCard3">Third table card</param>
//        /// <param name="tableCard4">Fourth table card</param>
//        /// <param name="tableCard5">Fifth table card</param>
//        /// <param name="weightedWR">Do calculation with weighted opponent hole cards</param>
//        /// <param name="winRatio">Outputs resultant win ratio</param>
//        /// <param name="winPercentage">Outputs resultant win percentage</param>
//        public void GetWinRatio(int maxIterations, int numPlayers, int holeCard1, int holeCard2, int tableCard1, int tableCard2, int tableCard3, int tableCard4, int tableCard5, bool weightedWR, out double winRatio, out double winPercentage)
//        {
//            if (tableCard1 != 0)
//                Monitor.Enter(locker);

//            CalculateWinRatio(maxIterations, numPlayers, holeCard1, holeCard2,
//                                tableCard1, tableCard2, tableCard3, tableCard4, tableCard5, weightedWR, out winRatio, out winPercentage, cudaHandEval, true);

//            if (cudaHandEval != null && winRatio < 0)
//                cudaHandEval.WaitForThreadSignalAndOutputResult(out winRatio, out winPercentage);

//            if (tableCard1 != 0)
//                Monitor.Exit(locker);
//        }

//        public void GetWinRatio(int maxIterations, int numPlayers, int holeCard1, int holeCard2, int tableCard1, int tableCard2, int tableCard3, int tableCard4, int tableCard5, out double winRatio, out double winPercentage, out double weightedWR, out double weightedWP)
//        {
//            if (tableCard1 != 0)
//                Monitor.Enter(locker);

//            CalculateWinRatio(maxIterations, numPlayers, holeCard1, holeCard2,
//                                tableCard1, tableCard2, tableCard3, tableCard4, tableCard5, false, out winRatio, out winPercentage, cudaHandEval, true);

//            CalculateWinRatio(maxIterations, numPlayers, holeCard1, holeCard2,
//                                            tableCard1, tableCard2, tableCard3, tableCard4, tableCard5, true, out weightedWR, out weightedWP, cudaHandEval2, true);

//            bool needSetInDictionary = false;

//            if (cudaHandEval != null && winRatio < 0)
//            {
//                needSetInDictionary = true;
//                cudaHandEval.WaitForThreadSignalAndOutputResult(out winRatio, out winPercentage);
//            }

//            if (cudaHandEval2 != null && weightedWR < 0)
//                cudaHandEval2.WaitForThreadSignalAndOutputResult(out weightedWR, out weightedWP);

//            lock (previousResultsLocker)
//            {
//                if (needSetInDictionary)
//                {
//                    uint storedResult = getResultForStorage(winPercentage, weightedWP);

//                    long key = generateKeyForCards((Card)holeCard1, (Card)holeCard2,
//                            (Card)tableCard1, (Card)tableCard2, (Card)tableCard3, (Card)tableCard4, (Card)tableCard5,
//                            numPlayers);

//                    if (!allPreviousResults.ContainsKey(key))
//                        allPreviousResults.Add(key, storedResult);

//                    winPercentage = getWinPercentages(storedResult, false);
//                    weightedWP = getWinPercentages(storedResult, true);

//                    double avgOpponentWin = ((100 - winPercentage) / (double)(numPlayers - 1));

//                    if (avgOpponentWin == 0)
//                        winRatio = 1000;
//                    else
//                        winRatio = winPercentage / avgOpponentWin;

//                    avgOpponentWin = ((100 - weightedWP) / (double)(numPlayers - 1));

//                    if (avgOpponentWin == 0)
//                        weightedWR = 1000;
//                    else
//                        weightedWR = weightedWP / avgOpponentWin;

//                }
//            }

//            if (tableCard1 != 0)
//                Monitor.Exit(locker);
//        }

//        public static int GetHandRank(Card card1, Card card2, Card card3, Card card4, Card card5, Card card6, Card card7)
//        {
//            lock (handRankLocker)
//                if (handRank == null)
//                    LoadHandRank();

//            int returnValue = handRank[53 + (byte)card1];
//            returnValue = handRank[returnValue + (byte)card2];
//            returnValue = handRank[returnValue + (byte)card3];
//            returnValue = handRank[returnValue + (byte)card4];
//            returnValue = handRank[returnValue + (byte)card5];
//            returnValue = handRank[returnValue + (byte)card6];
//            returnValue = handRank[returnValue + (byte)card7];

//            return returnValue;
//        }

//        private static void LoadHandRank()
//        {
//            //Load hand rank for any CPU calculations
//            handRank = new int[32487834];

//            if (File.Exists(FileLocations.HandRanksDOTDAT))
//            {
//                BinaryReader HandRankFile = new BinaryReader(File.OpenRead(FileLocations.HandRanksDOTDAT));

//                for (int i = 0; i < handRank.Length; i++)
//                {
//                    handRank[i] = HandRankFile.ReadInt32();
//                }

//                HandRankFile.Close();
//            }
//            else
//            {
//                throw new Exception("Unable to find HandRanks.dat file");
//            }
//        }

//        protected /*override*/ void updateWorkerThread()
//        {
//            lock (locker)
//            {
//                if (CUDAInitInWorkerThread)
//                {
//                    cudaHandEval = WinRatioGPU.Instance(0);
//                    if (cudaHandEval != null)
//                    {
//                        cudaHandEval2 = WinRatioGPU.Instance(1);
//                    }
//                }
//            }

//            //base.updateWorkerThread();
//        }

//        protected override void updateInfo()
//        {
//            //DateTime startTime = DateTime.Now;

//            var handDetails = decisionRequest.Cache.getCurrentHandDetails();
//            var numActivePlayers = decisionRequest.Cache.getActivePositions().Length;
//            var botHoleCards = decisionRequest.Cache.getPlayerHoleCards(decisionRequest.PlayerId);

//            if(botHoleCards.holeCard1 == 0 || botHoleCards.holeCard2 == 0)
//                throw new Exception("Bot hole cards not defined");

//            double winRatio, winPercentage, weightedWinRatio, weightedWinPercentage, winRatioOld, winPercentageOld;
//            double winPercentageDifference, scaledWinPercentageChange;

//            //if (handDetails.tableCard1 != 0)
//                //Monitor.Enter(locker);

//            #region normalWR

//            CalculateWinRatio(500000, numActivePlayers,
//                botHoleCards.holeCard1, botHoleCards.holeCard2,
//                handDetails.tableCard1, handDetails.tableCard2, handDetails.tableCard3,
//                handDetails.tableCard4, handDetails.tableCard5, false, out winRatio, out winPercentage, cudaHandEval, true);

//            #endregion normalWR

//            #region weightedWR

//            CalculateWinRatio(500000, numActivePlayers,
//                botHoleCards.holeCard1, botHoleCards.holeCard2,
//                handDetails.tableCard1, handDetails.tableCard2, handDetails.tableCard3,
//                handDetails.tableCard4, handDetails.tableCard5, true, out weightedWinRatio, out weightedWinPercentage, cudaHandEval2, true);

//            #endregion weightedWR

//            #region WRdifference
//            //WinRatio Difference

//            bool needSetInDictionary = false;

//            if (handDetails.tableCard3 == 0)
//            {

//                if (cudaHandEval != null && winRatio < 0)
//                {
//                    needSetInDictionary = true;
//                    cudaHandEval.WaitForThreadSignalAndOutputResult(out winRatio, out winPercentage);
//                    Monitor.Exit(locker);
//                }

//                if (cudaHandEval2 != null && weightedWinRatio < 0)
//                {
//                    cudaHandEval2.WaitForThreadSignalAndOutputResult(out weightedWinRatio, out weightedWinPercentage);
//                    Monitor.Exit(locker);
//                }

//                lock (previousResultsLocker)
//                {
//                    if (needSetInDictionary)
//                    {
//                        uint storedResult = getResultForStorage(winPercentage, weightedWinPercentage);

//                        long key = generateKeyForCards((Card)botHoleCards.holeCard1, (Card)botHoleCards.holeCard2,
//                                (Card)handDetails.tableCard1, (Card)handDetails.tableCard2, (Card)handDetails.tableCard3, (Card)handDetails.tableCard4, (Card)handDetails.tableCard5,
//                                numActivePlayers);

//                        if (!allPreviousResults.ContainsKey(key))
//                            allPreviousResults.Add(key, storedResult);
//                        else
//                            storedResult = allPreviousResults[key];

//                        winPercentage = getWinPercentages(storedResult, false);
//                        weightedWinPercentage = getWinPercentages(storedResult, true);

//                        double avgOpponentWin = ((100 - winPercentage) / (double)(numActivePlayers - 1));

//                        if (avgOpponentWin == 0)
//                            winRatio = 1000;
//                        else
//                            winRatio = winPercentage / avgOpponentWin;

//                        avgOpponentWin = ((100 - weightedWinPercentage) / (double)(numActivePlayers - 1));

//                        if (avgOpponentWin == 0)
//                            weightedWinRatio = 1000;
//                        else
//                            weightedWinRatio = weightedWinPercentage / avgOpponentWin;

//                    }
//                }

//                winPercentageOld = winPercentage * 100;
//            }
//            else
//            {

//                if (handDetails.tableCard5 > 0)
//                {
//                    CalculateWinRatio(500000, numActivePlayers,
//                        botHoleCards.holeCard1, botHoleCards.holeCard2,
//                        handDetails.tableCard1, handDetails.tableCard2, handDetails.tableCard3,
//                        handDetails.tableCard4, 0, true, out winRatioOld, out winPercentageOld, null, false);
//                }
//                else if (handDetails.tableCard4 > 0)
//                {
//                    CalculateWinRatio(500000, numActivePlayers,
//                        botHoleCards.holeCard1, botHoleCards.holeCard2,
//                        handDetails.tableCard1, handDetails.tableCard2, handDetails.tableCard3,
//                        0, 0, true, out winRatioOld, out winPercentageOld, null, false);
//                }
//                else if (handDetails.tableCard3 > 0)
//                {
//                    CalculateWinRatio(500000, numActivePlayers,
//                        botHoleCards.holeCard1, botHoleCards.holeCard2,
//                        0, 0, 0,
//                        0, 0, true, out winRatioOld, out winPercentageOld, null, false);
//                }
//                else
//                    throw new Exception();

//                if (cudaHandEval != null && winRatio < 0)
//                {
//                    needSetInDictionary = true;
//                    cudaHandEval.WaitForThreadSignalAndOutputResult(out winRatio, out winPercentage);
//                    Monitor.Exit(locker);
//                }

//                if (cudaHandEval2 != null && weightedWinRatio < 0)
//                {
//                    cudaHandEval2.WaitForThreadSignalAndOutputResult(out weightedWinRatio, out weightedWinPercentage);
//                    Monitor.Exit(locker);
//                }

//                lock (previousResultsLocker)
//                {
//                    if (needSetInDictionary)
//                    {
//                        uint storedResult = getResultForStorage(winPercentage, weightedWinPercentage);

//                        long key = generateKeyForCards((Card)botHoleCards.holeCard1, (Card)botHoleCards.holeCard2,
//                                (Card)handDetails.tableCard1, (Card)handDetails.tableCard2, (Card)handDetails.tableCard3, (Card)handDetails.tableCard4, (Card)handDetails.tableCard5,
//                                numActivePlayers);

//                        if (!allPreviousResults.ContainsKey(key))
//                            allPreviousResults.Add(key, storedResult);

//                        winPercentage = getWinPercentages(storedResult, false);
//                        weightedWinPercentage = getWinPercentages(storedResult, true);

//                        double avgOpponentWin = ((100 - winPercentage) / (double)(numActivePlayers - 1));

//                        if (avgOpponentWin == 0)
//                            winRatio = 1000;
//                        else
//                            winRatio = winPercentage / avgOpponentWin;

//                        avgOpponentWin = ((100 - weightedWinPercentage) / (double)(numActivePlayers - 1));

//                        if (avgOpponentWin == 0)
//                            weightedWinRatio = 1000;
//                        else
//                            weightedWinRatio = weightedWinPercentage / avgOpponentWin;
//                    }
//                }
//            }

//            //Difference is between +1 and -1
//            winPercentageDifference = (winPercentage*100) - winPercentageOld;
//            scaledWinPercentageChange = ((winPercentageDifference * 2) + 100) / 2;

//            if (scaledWinPercentageChange > 100)
//                scaledWinPercentageChange = 1;
//            else if (scaledWinPercentageChange < 0)
//                scaledWinPercentageChange = 0;

//            //normal win ratio sets
//            infoStore.SetInformationValue(InfoType.WR_CardsOnlyWinRatio, winRatio);
//            infoStore.SetInformationValue(InfoType.WR_CardsOnlyWinPercentage, winPercentage / 100);
//            infoStore.SetInformationValue(InfoType.WR_CardsOnlyOpponentWinPercentage, (1 - (winPercentage / 100)) / (numActivePlayers - 1));

//            //weighted win ratio
//            infoStore.SetInformationValue(InfoType.WR_CardsOnlyWeightedPercentage, weightedWinPercentage / 100);
//            infoStore.SetInformationValue(InfoType.WR_CardsOnlyWeightedOpponentWinPercentage, (1 - (weightedWinPercentage / 100)) / (numActivePlayers - 1));

//            //last round win ratio
//            infoStore.SetInformationValue(InfoType.WR_CardsOnlyWinPercentageLastRoundChange, scaledWinPercentageChange/100);

//            #endregion WRdifference            

//            lock (previousResultsLocker)
//            {
//                if (DateTime.Now - lastSave > TimeSpan.FromHours(24))
//                {
//                    var drives = DriveInfo.GetDrives();
//                    bool yPresent = false;

//                    for (int i = 0; i < drives.Length; i++)
//                    {
//                        if (drives[i].Name.Contains('Y') || drives[i].Name.Contains('y'))
//                            yPresent = true;
//                    }

//                    if (yPresent)
//                        savedResultsDefaulDrive = "Y:\\";
//                    else
//                        savedResultsDefaulDrive = "C:\\";

//                    if (File.Exists(Path.Combine(savedResultsDefaulDrive, savedResultsFileName)))
//                        File.Delete(Path.Combine(savedResultsDefaulDrive, savedResultsFileName));

//                    if (savedResultsDefaulDrive == "Y:\\")
//                    {
//                        //File.WriteAllBytes(Path.Combine(savedResultsDefaulDrive, savedResultsFileName), data);

//                        //Write out winRatio using the new version
//                        using (var stream = File.OpenWrite(Path.Combine(savedResultsDefaulDrive, savedResultsFileName)))
//                        using (var writer = new BinaryWriter(stream))
//                        {
//                            foreach (var key in allPreviousResults.Keys)
//                            {
//                                writer.Write(key);
//                                writer.Write(allPreviousResults[key]);
//                            }
//                        }
//                    }

//                    lastSave = DateTime.Now;
//                }
//            }
//        }

//        private void CalculateWinRatio(int numGames, int numPlayers, int holeCard1, int holeCard2, int tableCard1, int tableCard2, int tableCard3, int tableCard4, int tableCard5, bool weightedWR, out double winRatio, out double winPercentage, WinRatioGPU evaluator, bool careAboutNumPlayersInLookup)
//        {
//            winRatio = -1;
//            winPercentage = -1;

//            //First check if pre flop and use lookup tabke if possible
//            if (tableCard1 == 0 && preFlopWR != null)
//            {
//                winRatio = preFlopWR[weightedWR ? 0 : 1, numPlayers - 2, (int)holeCard1 - 1, (int)holeCard2 - 1, 0];
//                winPercentage = preFlopWR[weightedWR ? 0 : 1, numPlayers - 2, (int)holeCard1 - 1, (int)holeCard2 - 1, 1];
//            }
//            else
//            {
//                if (decisionRequest.Cache != null || careAboutNumPlayersInLookup)
//                {
//                    if (evaluator != null)
//                        Monitor.Enter(locker);

//                    lock (previousResultsLocker)
//                    {
//                        if (careAboutNumPlayersInLookup)
//                        {
//                            uint result;
//                            long key = generateKeyForCards((Card)holeCard1, (Card)holeCard2, (Card)tableCard1, (Card)tableCard2, (Card)tableCard3, (Card)tableCard4, (Card)tableCard5, numPlayers);

//                            if (getresultFromDictionary(key, out result))
//                            {
//                                winPercentage = getWinPercentages(result, weightedWR);

//                                double avgOpponentWin = ((100 - winPercentage) / (double)(numPlayers - 1));

//                                if (avgOpponentWin == 0)
//                                    winRatio = 1000;
//                                else
//                                    winRatio = winPercentage / avgOpponentWin;

//                                if (evaluator != null)
//                                    Monitor.Exit(locker);

//                                return;
//                            }
//                        }
//                        else
//                        {
//                            uint result;
//                            for (int i = numPlayers; i <= decisionRequest.Cache.getCurrentHandDetails().numStartPlayers; i++)
//                            {
//                                long key = generateKeyForCards((Card)holeCard1, (Card)holeCard2, (Card)tableCard1, (Card)tableCard2, (Card)tableCard3, (Card)tableCard4, (Card)tableCard5, i);

//                                if (getresultFromDictionary(key, out result))
//                                {
//                                    winPercentage = getWinPercentages(result, weightedWR);

//                                    double avgOpponentWin = ((100 - winPercentage) / (double)(numPlayers - 1));

//                                    if (avgOpponentWin == 0)
//                                        winRatio = 1000;
//                                    else
//                                        winRatio = winPercentage / avgOpponentWin;

//                                    if (evaluator != null)
//                                        Monitor.Exit(locker);

//                                    return;
//                                }
//                            }
//                        }
//                    }
//                }


//                if (evaluator != null)
//                {
//                    if (weightedWR)
//                        evaluator.GetWinRatio((byte)holeCard1, (byte)holeCard2, (byte)tableCard1, (byte)tableCard2, (byte)tableCard3, (byte)tableCard4, (byte)tableCard5, (byte)numPlayers, true);
//                    else
//                        evaluator.GetWinRatio((byte)holeCard1, (byte)holeCard2, (byte)tableCard1, (byte)tableCard2, (byte)tableCard3, (byte)tableCard4, (byte)tableCard5, (byte)numPlayers, false);
//                }
//                else
//                {
//                    int gameSuccess = 0;

//                    //GameSimulationThreadManagment goSimulation = new GameSimulationThreadManagment(numThreads, numPlayers, (numGames / numThreads), holeCard1, holeCard2, tableCard1, tableCard2, tableCard3, tableCard4, tableCard5, HandRank, weightedWR);
//                    //gameSuccess = goSimulation.getGameSuccess();
//                    WinRatioCPU winRatioCalculation = new WinRatioCPU(handRank, numThreads, numGames);
//                    gameSuccess = winRatioCalculation.GetGameSuccess(holeCard1, holeCard2, tableCard1, tableCard2, tableCard3, tableCard4, tableCard5, numPlayers, weightedWR);

//                    //Calculate win ratio
//                    winPercentage = ((double)(gameSuccess + (numGames)) / (double)(2 * (numGames))) * 100;
//                    double avgOpponentWin = ((100 - winPercentage) / (double)(numPlayers - 1));

//                    if (avgOpponentWin == 0)
//                        winRatio = 1000;
//                    else
//                        winRatio = winPercentage / avgOpponentWin;

//                    if (winRatio == Double.NaN)
//                        throw new Exception("Error in ProbabilityWinRatio.cs. winRatio cannot equal NaN");
//                }
//            }            
//        }

//        private bool getresultFromDictionary(long key, out uint result)
//        {
//            return allPreviousResults.TryGetValue(key, out result);
//        }

//        public static long generateKeyForCards(Card hole1, Card hole2, Card table1, Card table2, Card table3, Card table4, Card table5, int numPlayers)
//        {
//            if(hole1 == Card.NoCard || hole2 == Card.NoCard)
//                throw new Exception("Must specify hole cards");

//            if (numPlayers < 2 || numPlayers > 10)
//                throw new Exception("Number of players must be between 2 and 9 inclusive");

//            int numtc = 0;
//            if(table1 != Card.NoCard)
//                if(table4 != Card.NoCard)
//                    if(table5!= Card.NoCard)
//                        numtc = 5;
//                    else
//                        numtc = 4;
//                else
//                    numtc = 3;

//            int suithc1 = ((byte)hole1 - 1) % 4; int numhc1 = ((byte)hole1 - 1) / 4;
//            int suithc2 = ((byte)hole2 - 1) % 4; int numhc2 = ((byte)hole2 - 1) / 4;

//            int[] tcsuits = new int[numtc];
//            int[] tcnums = new int[numtc];

//            if (numtc > 0)
//            {
//                tcsuits[0] = ((byte)table1 - 1) % 4; tcnums[0] = ((byte)table1 - 1) / 4;
//                tcsuits[1] = ((byte)table2 - 1) % 4; tcnums[1] = ((byte)table2 - 1) / 4;
//                tcsuits[2] = ((byte)table3 - 1) % 4; tcnums[2] = ((byte)table3 - 1) / 4;

//                if (numtc > 3)
//                {
//                    tcsuits[3] = ((byte)table4 - 1) % 4;
//                    tcnums[3] = ((byte)table4 - 1) / 4;

//                    if (numtc > 4)
//                    {
//                        tcsuits[4] = ((byte)table5 - 1) % 4; tcnums[4] = ((byte)table5 - 1) / 4;
//                    }
//                }
//            }

//            int[] numsuits = new int[4];
//            int[] suitSwapTemp = new int[4];
//            int temp;

//            if (numhc1 < numhc2)
//            {
//                temp = numhc2;
//                numhc2 = numhc1;
//                numhc1 = temp;

//                temp = suithc1;
//                suithc1 = suithc2;
//                suithc2 = temp;
//            }
//            else if (numhc1 == numhc2 && suithc2 < suithc1)
//            {
//                temp = numhc2;
//                numhc2 = numhc1;
//                numhc1 = temp;

//                temp = suithc1;
//                suithc1 = suithc2;
//                suithc2 = temp;
//            }

//            if (suithc1 == suithc2)
//            {
//                for (int i = 0; i < numtc; i++)
//                {
//                    if (tcsuits[i] == suithc1)
//                        tcsuits[i] = 0;
//                    else if (tcsuits[i] == 0)
//                        tcsuits[i] = suithc1;

//                    numsuits[tcsuits[i]]++;
//                    numsuits[0] = 10;
//                }

//                suithc1 = 0; suithc2 = 0;

//                #region relable suits on tcs

//                if (numsuits[1] > numsuits[2])
//                {
//                    if (numsuits[1] > numsuits[3])
//                    {
//                        if (numsuits[2] > numsuits[3])
//                        {
//                            //1, 2, 3 excellent do nothing
//                        }
//                        else
//                        {
//                            //1, 3, 2

//                            for (int i = 0; i < numtc; i++)
//                            {
//                                if (tcsuits[i] == 2)
//                                    tcsuits[i] = 3;
//                                else if (tcsuits[i] == 3)
//                                    tcsuits[i] = 2;
//                            }
//                        }
//                    }
//                    else
//                    {
//                        //3, 1, 2

//                        for (int i = 0; i < numtc; i++)
//                        {
//                            if (tcsuits[i] == 1)
//                                tcsuits[i] = 2;
//                            else if (tcsuits[i] == 2)
//                                tcsuits[i] = 3;
//                            else if (tcsuits[i] == 3)
//                                tcsuits[i] = 1;
//                        }
//                    }
//                }
//                else
//                {
//                    if (numsuits[2] > numsuits[3])
//                    {
//                        if (numsuits[1] > numsuits[3])
//                        {
//                            //2, 1, 3

//                            for (int i = 0; i < numtc; i++)
//                            {
//                                if (tcsuits[i] == 1)
//                                    tcsuits[i] = 2;
//                                else if (tcsuits[i] == 2)
//                                    tcsuits[i] = 1;                                
//                            }
//                        }
//                        else
//                        {
//                            //2, 3, 1

//                            for (int i = 0; i < numtc; i++)
//                            {
//                                if (tcsuits[i] == 1)
//                                    tcsuits[i] = 3;
//                                else if (tcsuits[i] == 2)
//                                    tcsuits[i] = 1;
//                                else if (tcsuits[i] == 3)
//                                    tcsuits[i] = 2;
//                            }
//                        }
//                    }
//                    else
//                    {
//                        //3, 2, 1

//                        for (int i = 0; i < numtc; i++)
//                        {
//                            if (tcsuits[i] == 1)
//                                tcsuits[i] = 3;                            
//                            else if (tcsuits[i] == 3)
//                                tcsuits[i] = 1;
//                        }
//                    }
//                }

//                #endregion                
//            }
//            else
//            {
//                for (int i = 0; i < numtc; i++)
//                {
//                    if (tcsuits[i] == suithc1)
//                        tcsuits[i] = 0;
//                    else if (tcsuits[i] == suithc2)
//                        tcsuits[i] = 1;
//                    else if (tcsuits[i] == 0)
//                        tcsuits[i] = 2;
//                    else if (tcsuits[i] == 1)
//                        tcsuits[i] = 3;

//                    numsuits[tcsuits[i]]++;
//                    numsuits[0] = 10;
//                    numsuits[1] = 9;
//                }

//                suithc1 = 0; suithc2 = 1;

//                #region relable suits on tcs

//                if (numsuits[2] < numsuits[3])                
//                {
//                    //3, 2
//                    for (int i = 0; i < numtc; i++)
//                    {
//                        if (tcsuits[i] == 2)
//                            tcsuits[i] = 3;
//                        else if (tcsuits[i] == 3)
//                            tcsuits[i] = 2;
//                    }
//                }                

//                #endregion                
//            }

//            //create six bit value for each tc and hc
//            for (int i = 0; i < numtc; i++)
//                tcnums[i] = (tcsuits[i] << 4) | (tcnums[i] + 1);

//            numhc1 = (suithc1 << 4) | (numhc1 + 1);
//            numhc2 = (suithc2 << 4) | (numhc2 + 1);

//            //now reorder tcs by suit then by number
//            for (int i = 0; i < numtc; i++)
//            {
//                for (int j = i + 1; j < numtc; j++)
//                {
//                    if (tcnums[j] < tcnums[i])
//                    {
//                        temp = tcnums[i];
//                        tcnums[i] = tcnums[j];
//                        tcnums[j] = temp;
//                    }
//                }
//            }

//            //finally create the long result
//            long result = 0;
//            result |= ((long)numhc1) << 6 * 6 + 4;
//            result |= ((long)numhc2) << 6 * 5 + 4;
//            for (int i = 0; i < numtc; i++)
//                result |= ((long)tcnums[i]) << 6 * (4 - i) + 4;

//            result |= numPlayers;

//            return result;
//        }

//        public static void ReOrderCards(byte[] holeCards, byte[] tableCards)
//        {
//            if (holeCards.Length != 2 || tableCards.Length != 5)
//                throw new Exception("Must have 2 hole cards and 5 table cards");

//            Card[] tempHCs =
//                (from hcs in holeCards
//                 select (Card)hcs).ToArray();

//            Card[] temptcs =
//                (from tcs in tableCards
//                 select (Card)tcs).ToArray();

//            ReOrderCards(tempHCs, temptcs);

//            holeCards[0] = (byte)tempHCs[0];
//            holeCards[1] = (byte)tempHCs[1];

//            for (int i = 0; i < 5; i++)
//                tableCards[i] = (byte)temptcs[i];
//        }

//        public static void ReOrderCards(Card[] holeCards, Card[] tableCards)
//        {
//            Card hole1 = holeCards[0];
//            Card hole2 = holeCards[1];
//            Card table1 = tableCards[0];
//            Card table2 = tableCards[1];
//            Card table3 = tableCards[2];
//            Card table4 = tableCards[3];
//            Card table5 = tableCards[4];

//            if (hole1 == Card.NoCard || hole2 == Card.NoCard)
//                throw new Exception("Must specify hole cards");

//            int numtc = 0;
//            if (table1 != Card.NoCard)
//                if (table4 != Card.NoCard)
//                    if (table5 != Card.NoCard)
//                        numtc = 5;
//                    else
//                        numtc = 4;
//                else
//                    numtc = 3;

//            int suithc1 = ((byte)hole1 - 1) % 4; int numhc1 = ((byte)hole1 - 1) / 4;
//            int suithc2 = ((byte)hole2 - 1) % 4; int numhc2 = ((byte)hole2 - 1) / 4;

//            int[] tcsuits = new int[numtc];
//            int[] tcnums = new int[numtc];

//            if (numtc > 0)
//            {
//                tcsuits[0] = ((byte)table1 - 1) % 4; tcnums[0] = ((byte)table1 - 1) / 4;
//                tcsuits[1] = ((byte)table2 - 1) % 4; tcnums[1] = ((byte)table2 - 1) / 4;
//                tcsuits[2] = ((byte)table3 - 1) % 4; tcnums[2] = ((byte)table3 - 1) / 4;

//                if (numtc > 3)
//                {
//                    tcsuits[3] = ((byte)table4 - 1) % 4;
//                    tcnums[3] = ((byte)table4 - 1) / 4;

//                    if (numtc > 4)
//                    {
//                        tcsuits[4] = ((byte)table5 - 1) % 4; tcnums[4] = ((byte)table5 - 1) / 4;
//                    }
//                }
//            }

//            int[] numsuits = new int[4];
//            int[] suitSwapTemp = new int[4];
//            int temp;

//            if (numhc1 < numhc2)
//            {
//                temp = numhc2;
//                numhc2 = numhc1;
//                numhc1 = temp;

//                temp = suithc1;
//                suithc1 = suithc2;
//                suithc2 = temp;
//            }
//            else if (numhc1 == numhc2 && suithc2 < suithc1)
//            {
//                temp = numhc2;
//                numhc2 = numhc1;
//                numhc1 = temp;

//                temp = suithc1;
//                suithc1 = suithc2;
//                suithc2 = temp;
//            }

//            if (suithc1 == suithc2)
//            {
//                for (int i = 0; i < numtc; i++)
//                {
//                    if (tcsuits[i] == suithc1)
//                        tcsuits[i] = 0;
//                    else if (tcsuits[i] == 0)
//                        tcsuits[i] = suithc1;

//                    numsuits[tcsuits[i]]++;
//                    numsuits[0] = 10;
//                }

//                suithc1 = 0; suithc2 = 0;

//                #region relable suits on tcs

//                if (numsuits[1] > numsuits[2])
//                {
//                    if (numsuits[1] > numsuits[3])
//                    {
//                        if (numsuits[2] > numsuits[3])
//                        {
//                            //1, 2, 3 excellent do nothing
//                        }
//                        else
//                        {
//                            //1, 3, 2

//                            for (int i = 0; i < numtc; i++)
//                            {
//                                if (tcsuits[i] == 2)
//                                    tcsuits[i] = 3;
//                                else if (tcsuits[i] == 3)
//                                    tcsuits[i] = 2;
//                            }
//                        }
//                    }
//                    else
//                    {
//                        //3, 1, 2

//                        for (int i = 0; i < numtc; i++)
//                        {
//                            if (tcsuits[i] == 1)
//                                tcsuits[i] = 2;
//                            else if (tcsuits[i] == 2)
//                                tcsuits[i] = 3;
//                            else if (tcsuits[i] == 3)
//                                tcsuits[i] = 1;
//                        }
//                    }
//                }
//                else
//                {
//                    if (numsuits[2] > numsuits[3])
//                    {
//                        if (numsuits[1] > numsuits[3])
//                        {
//                            //2, 1, 3

//                            for (int i = 0; i < numtc; i++)
//                            {
//                                if (tcsuits[i] == 1)
//                                    tcsuits[i] = 2;
//                                else if (tcsuits[i] == 2)
//                                    tcsuits[i] = 1;
//                            }
//                        }
//                        else
//                        {
//                            //2, 3, 1

//                            for (int i = 0; i < numtc; i++)
//                            {
//                                if (tcsuits[i] == 1)
//                                    tcsuits[i] = 3;
//                                else if (tcsuits[i] == 2)
//                                    tcsuits[i] = 1;
//                                else if (tcsuits[i] == 3)
//                                    tcsuits[i] = 2;
//                            }
//                        }
//                    }
//                    else
//                    {
//                        //3, 2, 1

//                        for (int i = 0; i < numtc; i++)
//                        {
//                            if (tcsuits[i] == 1)
//                                tcsuits[i] = 3;
//                            else if (tcsuits[i] == 3)
//                                tcsuits[i] = 1;
//                        }
//                    }
//                }

//                #endregion
//            }
//            else
//            {
//                for (int i = 0; i < numtc; i++)
//                {
//                    if (tcsuits[i] == suithc1)
//                        tcsuits[i] = 0;
//                    else if (tcsuits[i] == suithc2)
//                        tcsuits[i] = 1;
//                    else if (tcsuits[i] == 0)
//                        tcsuits[i] = 2;
//                    else if (tcsuits[i] == 1)
//                        tcsuits[i] = 3;

//                    numsuits[tcsuits[i]]++;
//                    numsuits[0] = 10;
//                    numsuits[1] = 9;
//                }

//                suithc1 = 0; suithc2 = 1;

//                #region relable suits on tcs

//                if (numsuits[2] < numsuits[3])
//                {
//                    //3, 2
//                    for (int i = 0; i < numtc; i++)
//                    {
//                        if (tcsuits[i] == 2)
//                            tcsuits[i] = 3;
//                        else if (tcsuits[i] == 3)
//                            tcsuits[i] = 2;
//                    }
//                }

//                #endregion
//            }

//            holeCards[0] = (Card)(numhc1 * 4 + suithc1 + 1);
//            holeCards[1] = (Card)(numhc2 * 4 + suithc2 + 1);

//            for (int i = 0; i < 5; i++)
//            {
//                if (i < tcnums.Length)
//                    tableCards[i] = (Card)(tcnums[i] * 4 + tcsuits[i] + 1);
//                else
//                    tableCards[i] = Card.NoCard;
//            }
//        }

//        public static uint getResultForStorage(double winPercentage, double weightedWinPercentage)
//        {
//            ushort normal = (ushort)((winPercentage / 100.0) * ushort.MaxValue);
//            ushort weighted = (ushort)((weightedWinPercentage / 100.0) * ushort.MaxValue);

//            return (uint)(normal << 16) | (uint)weighted;
//        }

//        public static double getWinPercentages(uint valueFromDictionary, bool weighted)
//        {
//            if (weighted)
//                return 100.0 * (double)(valueFromDictionary & 0x0000FFFF) / (double)(ushort.MaxValue);
//            else
//                return 100.0 * (double)((valueFromDictionary & 0xFFFF0000) >> 16) / (double)(ushort.MaxValue);
//        }

//        /// <summary>
//        /// Closes GPU worker threads
//        /// </summary>
//        public override void Close()
//        {
//            lock (previousResultsLocker)
//            {
//                /*
//                byte[] data = null;
//                BinaryFormatter bin = new BinaryFormatter();
//                MemoryStream mem = new MemoryStream();
//                bin.Serialize(mem, allPreviousResults);
//                data = mem.ToArray();
//                */

//                var drives = DriveInfo.GetDrives();
//                bool yPresent = false;

//                for (int i = 0; i < drives.Length; i++)
//                {
//                    if (drives[i].Name.Contains('Y') || drives[i].Name.Contains('y'))
//                        yPresent = true;
//                }

//                if (yPresent)
//                    savedResultsDefaulDrive = "Y:\\";
//                else
//                    savedResultsDefaulDrive = "C:\\";

//                if (File.Exists(Path.Combine(savedResultsDefaulDrive, savedResultsFileName)))
//                    File.Delete(Path.Combine(savedResultsDefaulDrive, savedResultsFileName));

//                if (savedResultsDefaulDrive == "Y:\\")
//                {
//                    //File.WriteAllBytes(Path.Combine(savedResultsDefaulDrive, savedResultsFileName), data);

//                    //Write out winRatio using the new version
//                    using (var stream = File.OpenWrite(Path.Combine(savedResultsDefaulDrive, savedResultsFileName)))
//                    using (var writer = new BinaryWriter(stream))
//                    {
//                        foreach (var key in allPreviousResults.Keys)
//                        {
//                            writer.Write(key);
//                            writer.Write(allPreviousResults[key]);
//                        }
//                    }
//                }
//            }

//            lock (locker)
//            {
//                if (cudaHandEval != null)
//                    cudaHandEval.CloseThread();

//                if (cudaHandEval2 != null)
//                    cudaHandEval2.CloseThread();

//                cudaHandEval = null;
//                cudaHandEval2 = null;

//                WinRatioGPU.CloseInstances();
//            }

//            base.Close();
//        }

//    }

//}
