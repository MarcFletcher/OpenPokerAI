//using System;
//using System.Collections.Generic;
//using System.Linq;
//using System.Text;
//using System.Threading;
//using System.IO;
//using GASS.CUDA;
//using GASS.CUDA.Types;
//using System.Runtime.InteropServices;
//using PokerBot.Definitions;

//namespace PokerBot.AI.InfoProviders
//{
//    internal class WinRatioGPU
//    {
//        static WinRatioGPU instance1 = null;
//        static WinRatioGPU instance2 = null;
//        static WinRatioGPU instance3 = null;

//        public static WinRatioGPU Instance(int i)
//        {
//            if (i == 0)
//            {
//                if (instance1 == null)
//                {
//                    instance1 = new WinRatioGPU(i);
//                    if (!instance1.initialized)
//                    {
//                        instance1 = null;
//                        return null;
//                    }
//                }

//                return instance1;
//            }
//            else if (i == 1)
//            {
//                if (instance2 == null)
//                {
//                    instance2 = new WinRatioGPU(i);
//                    if (!instance2.initialized)
//                    {
//                        instance2 = null;
//                        return null;
//                    }
//                }

//                return instance2;
//            }
//            else if (i == 2)
//            {
//                if (instance3 == null)
//                {
//                    instance3 = new WinRatioGPU(i);
//                    if (!instance3.initialized)
//                    {
//                        instance3 = null;
//                        return null;
//                    }
//                }

//                return instance3;
//            }

//            return null;
//        }

//        struct mt_struct_stripped
//        {
//            uint matrix_a;
//            uint mask_b;
//            uint mask_c;
//            uint seed;

//            public mt_struct_stripped(uint matrix_a, uint mask_b, uint mask_c, uint seed)
//            {
//                this.matrix_a = matrix_a;
//                this.mask_c = mask_c;
//                this.mask_b = mask_b;
//                this.seed = seed;
//            }
//        }

//        public static void CloseInstances()
//        {
//            if (instance1 != null)
//            {                
//                instance1.DealocateMemory();
//                instance1 = null;
//            }
//            if (instance2 != null)
//            {
//                instance2.DealocateMemory();
//                instance2 = null;
//            }
//            if (instance3 != null)
//            {
//                instance3.DealocateMemory();
//                instance3 = null;
//            }
//        }

//        #region Helper Functions


//        ///////////////////////////////////////////////////////////////////////////////
//        // Common host and device function 
//        ///////////////////////////////////////////////////////////////////////////////
//        //ceil(a / b)
//        static uint iDivUp(uint a, uint b)
//        {
//            return ((a % b) != 0) ? (a / b + 1) : (a / b);
//        }

//        //floor(a / b)
//        static uint iDivDown(uint a, uint b)
//        {
//            return a / b;
//        }

//        //Align a to nearest higher multiple of b
//        static uint iAlignUp(uint a, uint b)
//        {
//            return ((a % b) != 0) ? (a - a % b + b) : a;
//        }

//        //Align a to nearest lower multiple of b
//        static uint iAlignDown(uint a, uint b)
//        {
//            return a - a % b;
//        }

//        #endregion

//        #region Threading stuff

//        Thread cudaThread;

//        Thread[] sumThreads;
//        AutoResetEvent[] sumEvents;
//        AutoResetEvent[] sumEvents2;
//        int[] successCount;

//        AutoResetEvent workerThreadSignal;
//        ManualResetEvent mainThreadSignal;

//        byte[] cards = new byte[7];
//        volatile byte numPlayers = 0;
//        volatile bool initialized = false;
//        volatile bool weighted = false;
//        double[] winRatio = new double[] { 0 };
//        double[] winPercentage = new double[] { 0 };

//        public void WaitForThreadSignalAndOutputResult(out double winRatio, out double winPercentage)
//        {
//            mainThreadSignal.WaitOne();
//            winRatio = this.winRatio[0]; winPercentage = this.winPercentage[0];
//        }

//        void CudaThread(object i)
//        {
//            Nullable<int> ordinal = i as Nullable<int>;

//            if (ordinal != null)
//            {
//                if (InitialiseCUDA((int)ordinal))
//                {
//                    mainThreadSignal.Set();
//                    InitializeSumCounterObjects();

//                    while (true)
//                    {
//                        workerThreadSignal.WaitOne();

//                        if (!initialized)
//                            break;

//                        double wr, wp;

//                        if (weighted)
//                            GetWeightedWinRatio(cards[0], cards[1], cards[2], cards[3], cards[4], cards[5], cards[6], numPlayers, out wr, out wp);
//                        else
//                            GetWinRatio(cards[0], cards[1], cards[2], cards[3], cards[4], cards[5], cards[6], numPlayers, out wr, out wp);

//                        winRatio[0] = wr;
//                        winPercentage[0] = wp;
//                        mainThreadSignal.Set();
//                    }
//                }
//                else
//                    mainThreadSignal.Set();
//            }
//        }

//        void SumThread(object i)
//        {
//            int index = (int)(i);
//            int startIndex = index * h_finalResult.Length / sumThreads.Length;
//            int endIndex = (index + 1) * h_finalResult.Length / sumThreads.Length;
//            int successCountLocal = 0;

//            while (true)
//            {
//                sumEvents[index].WaitOne();                
//                successCountLocal = 0;

//                if (!initialized)
//                    break;

//                for (int j = startIndex; j < endIndex; j++)
//                    successCountLocal += h_finalResult[j];


//                successCount[index] = successCountLocal;
//                sumEvents2[index].Set();
//            }
//        }

//        public void CloseThread()
//        {
//            initialized = false;

//            if (cudaThread != null &&
//                (cudaThread.ThreadState == ThreadState.WaitSleepJoin ||
//                 cudaThread.ThreadState == ThreadState.Running))
//            {

//                workerThreadSignal.Set();

//                cudaThread.Join(1000);

//                if (cudaThread.ThreadState != System.Threading.ThreadState.Stopped)
//                    cudaThread.Abort();

//                cudaThread = null;

//                for (int i = 0; i < sumThreads.Length; i++)
//                {
//                    sumEvents[i].Set();
//                    sumThreads[i].Join();
//                }

//                sumEvents = null;
//                sumEvents2 = null;
//                sumThreads = null;
//                successCount = null;
//            }
//        }

//        #endregion

//        #region RandomNumberParams

//        ///////////////////////////////////////////////////////////////////////////////
//        // Data configuration for mersenne twister
//        ///////////////////////////////////////////////////////////////////////////////
//        uint PATH_N;
//        uint N_PER_RNG;
//        uint RAND_N;
//        uint SEED;

//        uint MT_BLOCK_DIM = 64;
//        uint MT_GRID_DIM = 64;
//        uint MT_RNG_COUNT = 4096;
//        Random rand = new Random();

//        #endregion

//        #region HandEvaluatorParams

//        int BLOCK_DIM = 8;
//        int size_x = 1024;
//        int size_y = 512;
//        int rands_per_thread = 25;

//        #endregion

//        #region CUDADeviceStuff

//        CUDA cuda;
//        CUfunction weightedHandEvaluator;
//        CUfunction handEvaluator;
//        CUfunction randomGPU;
//        CUdeviceptr d_MT;
//        CUdeviceptr d_randoms;
//        CUdeviceptr d_handRank;
//        CUdeviceptr d_finalResult;

//        #endregion

//        #region Results params

//        int[] h_finalResult;

//        #endregion

//        private WinRatioGPU(int ordinal)
//        {
//            workerThreadSignal = new AutoResetEvent(false);
//            mainThreadSignal = new ManualResetEvent(false);
//            cudaThread = new Thread(new ParameterizedThreadStart(CudaThread));

//            cudaThread.Start(ordinal);
//            mainThreadSignal.WaitOne();
//        }

//        ~WinRatioGPU()
//        {
//            if (initialized)
//                DealocateMemory();
//        }

//        void initCUDAParams()
//        {
//            PATH_N = (uint)(size_x * size_y * rands_per_thread);
//            N_PER_RNG = iAlignUp(iDivUp(PATH_N, MT_BLOCK_DIM * MT_GRID_DIM), 2);
//            RAND_N = MT_BLOCK_DIM * MT_GRID_DIM * N_PER_RNG;
//        }

//        bool InitialiseCUDA(int i)
//        {
//            try
//            {
//                // Init and select device with ordinal i.
//                cuda = new CUDA(i, true);

//                cuda.LoadModule(Path.Combine(Environment.CurrentDirectory, "InfoProviders\\WinRatio\\CUDA\\handEvaluator.cubin"));
//                handEvaluator = cuda.GetModuleFunction("Evaluate_Hand");

//                // load module
//                cuda.LoadModule(Path.Combine(Environment.CurrentDirectory, "InfoProviders\\WinRatio\\CUDA\\weightedHandEvaluator.cubin"));
//                weightedHandEvaluator = cuda.GetModuleFunction("Evaluate_Hand_Weighted");

//                cuda.LoadModule(Path.Combine(Environment.CurrentDirectory, "InfoProviders\\WinRatio\\CUDA\\MersenneTwister_kernel.cubin"));
//                randomGPU = cuda.GetModuleFunction("RandomGPU");

//                initCUDAParams();

//                SEED = (uint)(rand.NextDouble() * uint.MaxValue);

//                byte[] mt_data = loadMTGPU("InfoProviders\\WinRatio\\CUDA\\MersenneTwister.dat");

//                d_MT = cuda.CopyHostToDevice<byte>(mt_data);
//                d_randoms = cuda.Allocate<float>(new float[RAND_N]);

//                cuda.SetFunctionBlockShape(randomGPU, (int)MT_BLOCK_DIM, 1, 1);
//                cuda.SetParameter(randomGPU, 0, (uint)d_randoms);
//                cuda.SetParameter(randomGPU, IntPtr.Size, (uint)d_MT);
//                cuda.SetParameter(randomGPU, IntPtr.Size * 2, N_PER_RNG);
//                cuda.SetParameter(randomGPU, IntPtr.Size * 2 + sizeof(uint), (uint)SEED);

//                cuda.SetParameterSize(randomGPU, (uint)(2 * IntPtr.Size + 2 * sizeof(uint)));

//                int[] h_handRank = GetHandRank();
//                h_finalResult = new int[size_x * size_y];

//                // allocate device memory
//                // copy host memory to device
//                d_handRank = cuda.CopyHostToDevice<int>(h_handRank);
//                d_finalResult = cuda.CopyHostToDevice<int>(h_finalResult);

//                cuda.SetFunctionBlockShape(handEvaluator, BLOCK_DIM, BLOCK_DIM, 1);
//                cuda.SetParameter(handEvaluator, 0, (uint)d_handRank.Pointer);
//                cuda.SetParameter(handEvaluator, IntPtr.Size, (uint)d_randoms.Pointer);
//                cuda.SetParameter(handEvaluator, IntPtr.Size * 2 + 8 * sizeof(uint), (uint)d_finalResult.Pointer);

//                cuda.SetParameterSize(handEvaluator, (uint)(IntPtr.Size * 3 + sizeof(uint) * 8));

//                cuda.SetFunctionBlockShape(weightedHandEvaluator, BLOCK_DIM, BLOCK_DIM, 1);
//                cuda.SetParameter(weightedHandEvaluator, 0, (uint)d_handRank.Pointer);
//                cuda.SetParameter(weightedHandEvaluator, IntPtr.Size, (uint)d_randoms.Pointer);
//                cuda.SetParameter(weightedHandEvaluator, IntPtr.Size * 2 + 8 * sizeof(uint), (uint)d_finalResult.Pointer);

//                cuda.SetParameterSize(weightedHandEvaluator, (uint)(IntPtr.Size * 3 + sizeof(uint) * 8));

//                initialized = true;
//            }
//            catch (Exception)
//            {
//                return false;
//            }

//            return true;
//        }

//        void InitializeSumCounterObjects()
//        {
//            sumThreads = new Thread[8];
//            sumEvents = new AutoResetEvent[8];
//            sumEvents2 = new AutoResetEvent[8];
//            successCount = new int[8];

//            for (int i = 0; i < 8; i++)
//            {
//                sumEvents[i] = new AutoResetEvent(false);
//                sumEvents2[i] = new AutoResetEvent(false);
//                sumThreads[i] = new Thread(SumThread);
//                sumThreads[i].Name = "GPU WR Provider Sum - " + i.ToString();
//                sumThreads[i].Start((object)i);
//            }
//        }

//        public void GetWinRatio(byte hand1, byte hand2, byte table1, byte table2, byte table3, byte table4, byte table5, byte numberPlayers, bool weighted)
//        {
//            mainThreadSignal.Reset();

//            cards[0] = hand1; cards[1] = hand2;
//            cards[2] = table1; cards[3] = table2; cards[4] = table3; cards[5] = table4; cards[6] = table5;
//            this.winRatio[0] = 0.0;
//            this.winPercentage[0] = 0.0;
//            this.weighted = weighted;
//            this.numPlayers = numberPlayers;

//            workerThreadSignal.Set();
//        }

//        private void GetWinRatio(byte hand1, byte hand2, byte table1, byte table2, byte table3, byte table4, byte table5, byte numberPlayers, out double winRatio, out double winPercentage)
//        {
//            winRatio = 0; winPercentage = 0;

//            cuda.SetParameter(handEvaluator, IntPtr.Size * 2, (uint)hand1);
//            cuda.SetParameter(handEvaluator, IntPtr.Size * 2 + 1 * sizeof(uint), (uint)hand2);
//            cuda.SetParameter(handEvaluator, IntPtr.Size * 2 + 2 * sizeof(uint), (uint)table1);
//            cuda.SetParameter(handEvaluator, IntPtr.Size * 2 + 3 * sizeof(uint), (uint)table2);
//            cuda.SetParameter(handEvaluator, IntPtr.Size * 2 + 4 * sizeof(uint), (uint)table3);
//            cuda.SetParameter(handEvaluator, IntPtr.Size * 2 + 5 * sizeof(uint), (uint)table4);
//            cuda.SetParameter(handEvaluator, IntPtr.Size * 2 + 6 * sizeof(uint), (uint)table5);
//            cuda.SetParameter(handEvaluator, IntPtr.Size * 2 + 7 * sizeof(uint), (uint)numberPlayers);

//            SEED = (uint)(rand.NextDouble() * uint.MaxValue);
//            cuda.SetParameter(randomGPU, IntPtr.Size * 2 + sizeof(uint), SEED);

//            cuda.SynchronizeContext();

//            CUstream stream = cuda.CreateStream();
//            CUevent stop = cuda.CreateEvent();

//            cuda.LaunchAsync(randomGPU, (int)MT_GRID_DIM, 1, stream);
//            cuda.LaunchAsync(handEvaluator, size_x / BLOCK_DIM, size_y / BLOCK_DIM, stream);

//            cuda.RecordEvent(stop, stream);

//            while (CUDADriver.cuEventQuery(stop) == CUResult.ErrorNotReady)
//                Thread.Sleep(0);

//            cuda.CopyDeviceToHost<int>(d_finalResult, h_finalResult);
//            cuda.SynchronizeContext();

//            int successes = AddResults();

//            //Calculate win ratio
//            winPercentage = ((double)(successes + (size_x * size_y)) / (double)(2 * (size_x * size_y))) * 100;
//            double avgOpponentWin = ((100 - winPercentage) / (double)(numberPlayers - 1));

//            if (avgOpponentWin == 0)
//                winRatio = 1000;
//            else
//                winRatio = winPercentage / avgOpponentWin;

//            cuda.DestroyEvent(stop);
//            cuda.DestroyStream(stream);
//        }

//        private int AddResults()
//        {
//            int success = 0;

//            for (int i = 0; i < sumThreads.Length; i++)
//                sumEvents[i].Set();

//            for (int i = 0; i < sumThreads.Length; i++)
//            {
//                sumEvents2[i].WaitOne();
//                success += successCount[i];
//            }            

//            return success;
//        }

//        private void GetWeightedWinRatio(byte hand1, byte hand2, byte table1, byte table2, byte table3, byte table4, byte table5, byte numberPlayers, out double winRatio, out double winPercentage)
//        {
//            winRatio = 0; winPercentage = 0;

//            cuda.SetParameter(weightedHandEvaluator, IntPtr.Size * 2, (uint)hand1);
//            cuda.SetParameter(weightedHandEvaluator, IntPtr.Size * 2 + 1 * sizeof(uint), (uint)hand2);
//            cuda.SetParameter(weightedHandEvaluator, IntPtr.Size * 2 + 2 * sizeof(uint), (uint)table1);
//            cuda.SetParameter(weightedHandEvaluator, IntPtr.Size * 2 + 3 * sizeof(uint), (uint)table2);
//            cuda.SetParameter(weightedHandEvaluator, IntPtr.Size * 2 + 4 * sizeof(uint), (uint)table3);
//            cuda.SetParameter(weightedHandEvaluator, IntPtr.Size * 2 + 5 * sizeof(uint), (uint)table4);
//            cuda.SetParameter(weightedHandEvaluator, IntPtr.Size * 2 + 6 * sizeof(uint), (uint)table5);
//            cuda.SetParameter(weightedHandEvaluator, IntPtr.Size * 2 + 7 * sizeof(uint), (uint)numberPlayers);

//            SEED = (uint)(rand.NextDouble() * uint.MaxValue);
//            cuda.SetParameter(randomGPU, IntPtr.Size * 2 + sizeof(uint), SEED);

//            cuda.SynchronizeContext();

//            CUstream stream = cuda.CreateStream();
//            CUevent stop = cuda.CreateEvent();

//            cuda.LaunchAsync(randomGPU, (int)MT_GRID_DIM, 1, stream);
//            cuda.LaunchAsync(weightedHandEvaluator, size_x / BLOCK_DIM, size_y / BLOCK_DIM, stream);

//            cuda.RecordEvent(stop, stream);

//            while (CUDADriver.cuEventQuery(stop) == CUResult.ErrorNotReady)
//                Thread.Sleep(0);

//            cuda.CopyDeviceToHost<int>(d_finalResult, h_finalResult);
//            cuda.SynchronizeContext();

//            int successes = AddResults();

//            //Calculate win ratio
//            winPercentage = ((double)(successes + (size_x * size_y)) / (double)(2 * (size_x * size_y))) * 100;
//            double avgOpponentWin = ((100 - winPercentage) / (double)(numberPlayers - 1));

//            if (avgOpponentWin == 0)
//                winRatio = 1000;
//            else
//                winRatio = winPercentage / avgOpponentWin;

//            cuda.DestroyEvent(stop);
//            cuda.DestroyStream(stream);
//        }

//        void DealocateMemory()
//        {
//            try { cuda.UnloadModule(); }
//            catch (CUDAException) { }
//            try { cuda.Free(d_finalResult); }
//            catch (CUDAException) { }
//            try { cuda.Free(d_randoms); }
//            catch (CUDAException) { }
//            try { cuda.Free(d_handRank); }
//            catch (CUDAException) { }
//            try { cuda.Free(d_MT); }
//            catch (CUDAException) { }
//        }

//        int[] GetHandRank()
//        {
//            int[] HandRank = new int[32487834];

//            if (File.Exists(FileLocations.HandRanksDOTDAT))
//            {
//                //BinaryReader HandRankFile = new BinaryReader(File.Open(".\\HandRanks.dat", FileMode.Open, FileAccess.Read));
//                BinaryReader HandRankFile = new BinaryReader(File.OpenRead(FileLocations.HandRanksDOTDAT));

//                for (int i = 0; i < HandRank.Length; i++)
//                {
//                    HandRank[i] = HandRankFile.ReadInt32();
//                }

//                HandRankFile.Close();

//            }
//            else
//            {
//                throw new Exception("Unable to find HandRanks.dat file");
//            }

//            return HandRank;
//        }

//        //Load twister configurations
//        byte[] loadMTGPU(string fname)
//        {
//            var file = File.Open(fname, FileMode.Open);

//            int structSize = Marshal.SizeOf(typeof(mt_struct_stripped));

//            byte[] data = new byte[MT_RNG_COUNT * structSize];
//            file.Read(data, 0, data.Length);

//            file.Close();

//            return data;
//        }
//    }
//}
