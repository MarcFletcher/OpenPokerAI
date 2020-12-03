using System;
using System.Collections.Generic;
using System.Linq;
using PokerBot.Definitions;
using System.IO;
using System.Runtime.InteropServices;
using PokerBot.AI.InfoProviders;
using Microsoft.Win32;
using NetworkCommsDotNet.DPSBase;
using ProtoBuf;

namespace PokerBot.AI.ProviderWinRatio
{
  public partial class WinRatioProvider : InfoProviderBase, IDisposable
  {
    static readonly bool useOldLookupMethod = false;

    /// <summary>
    /// Used for compressing winRatio data outside of provider
    /// </summary>
    DataProcessor winRatioCompressor = DPSManager.GetDataProcessor<SharpZipLibCompressor.SharpZipLibGzipCompressor>();

    private class WinRatioEntry
    {
      HandState furthestLoadedHandStage; public HandState FurthestLoadedHandStage { get { return furthestLoadedHandStage; } }
      int maxNumPlayersFlop = -1, maxNumPlayersTurn = -1, maxNumPlayersRiver = -1;
      Card card1, card2, card3, card4, card5;
      public Card Card1 { get { return card1; } }
      public Card Card2 { get { return card2; } }
      public Card Card3 { get { return card3; } }
      public Card Card4 { get { return card4; } }
      public Card Card5 { get { return card5; } }

      ushort[] data1, data2, data3;
      ushort[] index1, index2, index3;
      long currentHandId;

      public WinRatioEntry(long handId, HandState handStage, int maxNumPlayers, Card card1, Card card2, Card card3, Card card4, Card card5, ushort[] loadedWR, ushort[] loadedWI)
      {
        if (handStage != HandState.Flop)
          throw new Exception();

        this.furthestLoadedHandStage = handStage;
        this.maxNumPlayersFlop = maxNumPlayers;
        this.card1 = card1;
        this.card2 = card2;
        this.card3 = card3;
        this.card4 = card4;
        this.card5 = card5;
        this.data1 = loadedWR;
        this.index1 = loadedWI;
        this.currentHandId = handId;
      }

      public void CheckNewHandAndResetEntry(long handId)
      {
        if (currentHandId != handId)
        {
          furthestLoadedHandStage = HandState.PreFlop;
          currentHandId = handId;
          maxNumPlayersFlop = -1;
          maxNumPlayersTurn = -1;
          maxNumPlayersRiver = -1;
          card1 = Card.NoCard;
          card2 = Card.NoCard;
          card3 = Card.NoCard;
          card4 = Card.NoCard;
          card5 = Card.NoCard;
          data1 = null;
          data2 = null;
          data3 = null;
          index1 = null;
          index2 = null;
          index3 = null;
        }
      }

      public int MaxNumPlayers(HandState handStage)
      {
        if (handStage == HandState.Flop)
          return maxNumPlayersFlop;
        if (handStage == HandState.Turn)
          return maxNumPlayersTurn;
        if (handStage == HandState.River)
          return maxNumPlayersRiver;
        throw new Exception();
      }

      ushort[] prepData, prepIndexes;
      byte prept1, prept2, prept3, prept4, prept5;
      HandState prepHandStage;
      int prepNumPLayers;

      public void PrepForLargeNumberGets(HandState handStage, int numPlayers)
      {
        prepNumPLayers = numPlayers;
        prepHandStage = handStage;

        if (handStage == this.furthestLoadedHandStage)
        { prepData = data1; prepIndexes = index1; }
        else if (handStage == HandState.Flop && this.furthestLoadedHandStage == HandState.Turn)
        { prepData = data2; prepIndexes = index2; }
        else if (handStage == HandState.Flop && this.furthestLoadedHandStage == HandState.River)
        { prepData = data3; prepIndexes = index3; }
        else if (handStage == HandState.Turn && this.furthestLoadedHandStage == HandState.River)
        { prepData = data2; prepIndexes = index2; }
        else
          throw new Exception();

        if ((int)handStage >= (int)HandState.Flop)
        {
          prept1 = (byte)card1;
          prept2 = (byte)card2;
          prept3 = (byte)card3;
        }
        else
        {
          prept1 = 0;
          prept2 = 0;
          prept3 = 0;
        }

        if ((int)handStage >= (int)HandState.Turn)
          prept4 = (byte)card4;
        else
          prept4 = 0;

        if (handStage == HandState.River)
          prept5 = (byte)card5;
        else
          prept5 = 0;

        bool sorted = false;
        byte tempCard;

        while (!sorted)
        {
          sorted = true;

          if (prept1 < prept2)
          { tempCard = prept1; prept1 = prept2; prept2 = tempCard; sorted = false; }
          if (prept2 < prept3)
          { tempCard = prept2; prept2 = prept3; prept3 = tempCard; sorted = false; }
          if (prept3 < prept4)
          { tempCard = prept3; prept3 = prept4; prept4 = tempCard; sorted = false; }
          if (prept4 < prept5)
          { tempCard = prept4; prept4 = prept5; prept5 = tempCard; sorted = false; }
        }
      }

      public ushort GetPrepWinRatio(Card hc1, Card hc2)
      {
        byte a = (byte)hc1, b = (byte)hc2;

        if (b > a)
        {
          byte temp = a;
          a = b;
          b = temp;
        }

        if ((int)prepHandStage >= (int)HandState.Flop)
        {
          if (a >= prept1)
            a--;
          if (a >= prept2)
            a--;
          if (a >= prept3)
            a--;

          if ((int)prepHandStage >= (int)HandState.Turn)
          {
            if (a >= prept4)
              a--;

            if (prepHandStage == HandState.River)
              if (a >= prept5)
                a--;
          }
        }

        if ((int)prepHandStage >= (int)HandState.Flop)
        {
          if (b >= prept1)
            b--;
          if (b >= prept2)
            b--;
          if (b >= prept3)
            b--;

          if ((int)prepHandStage >= (int)HandState.Turn)
          {
            if (b >= prept4)
              b--;

            if (prepHandStage == HandState.River)
              if (b >= prept5)
                b--;
          }
        }

        ushort result = 0;

        switch (prepHandStage)
        {
          case HandState.Flop:
            result = prepData[1176 * (prepNumPLayers - 2) + b - 1 + (a - 2) * (a - 1) / 2];
            break;
          case HandState.Turn:
            result = prepData[1128 * (prepNumPLayers - 2) + b - 1 + (a - 2) * (a - 1) / 2];
            break;
          case HandState.River:
            result = prepData[1081 * (prepNumPLayers - 2) + b - 1 + (a - 2) * (a - 1) / 2];
            break;
          default:
            throw new Exception();
        }

        return result;
      }

      public int GetPrepSortedIndex(Card hc1, Card hc2)
      {
        byte a = (byte)hc1, b = (byte)hc2;

        if (b > a)
        {
          byte temp = a;
          a = b;
          b = temp;
        }

        if ((int)prepHandStage >= (int)HandState.Flop)
        {
          if (a >= prept1)
            a--;
          if (a >= prept2)
            a--;
          if (a >= prept3)
            a--;

          if ((int)prepHandStage >= (int)HandState.Turn)
          {
            if (a >= prept4)
              a--;

            if (prepHandStage == HandState.River)
              if (a >= prept5)
                a--;
          }
        }

        if ((int)prepHandStage >= (int)HandState.Flop)
        {
          if (b >= prept1)
            b--;
          if (b >= prept2)
            b--;
          if (b >= prept3)
            b--;

          if ((int)prepHandStage >= (int)HandState.Turn)
          {
            if (b >= prept4)
              b--;

            if (prepHandStage == HandState.River)
              if (b >= prept5)
                b--;
          }
        }

        ushort result = 0;

        switch (prepHandStage)
        {
          case HandState.Flop:
            result = prepIndexes[1176 * (prepNumPLayers - 2) + b - 1 + (a - 2) * (a - 1) / 2];
            break;
          case HandState.Turn:
            result = prepIndexes[1128 * (prepNumPLayers - 2) + b - 1 + (a - 2) * (a - 1) / 2];
            break;
          case HandState.River:
            result = prepIndexes[1081 * (prepNumPLayers - 2) + b - 1 + (a - 2) * (a - 1) / 2];
            break;
          default:
            throw new Exception();
        }

        return result;
      }

      public ushort GetWinRatio(HandState handStage, Card hc1, Card hc2, int numPlayers)
      {
        ushort[] data;

        if (handStage == this.furthestLoadedHandStage)
          data = data1;
        else if (handStage == HandState.Flop && this.furthestLoadedHandStage == HandState.Turn)
          data = data2;
        else if (handStage == HandState.Flop && this.furthestLoadedHandStage == HandState.River)
          data = data3;
        else if (handStage == HandState.Turn && this.furthestLoadedHandStage == HandState.River)
          data = data2;
        else
          throw new Exception();

        int a = (int)hc1, b = (int)hc2;

        byte tc1 = 0, tc2 = 0, tc3 = 0, tc4 = 0, tc5 = 0;

        if ((int)handStage >= (int)HandState.Flop)
        {
          tc1 = (byte)card1;
          tc2 = (byte)card2;
          tc3 = (byte)card3;
        }

        if ((int)handStage >= (int)HandState.Turn)
          tc4 = (byte)card4;

        if (handStage == HandState.River)
          tc5 = (byte)card5;

        bool sorted = false;
        byte tempCard;

        while (!sorted)
        {
          sorted = true;

          if (tc1 < tc2)
          { tempCard = tc1; tc1 = tc2; tc2 = tempCard; sorted = false; }
          if (tc2 < tc3)
          { tempCard = tc2; tc2 = tc3; tc3 = tempCard; sorted = false; }
          if (tc3 < tc4)
          { tempCard = tc3; tc3 = tc4; tc4 = tempCard; sorted = false; }
          if (tc4 < tc5)
          { tempCard = tc4; tc4 = tc5; tc5 = tempCard; sorted = false; }
        }

        if ((int)handStage >= (int)HandState.Flop)
        {
          if (a >= tc1)
            a--;
          if (a >= tc2)
            a--;
          if (a >= tc3)
            a--;

          if ((int)handStage >= (int)HandState.Turn)
          {
            if (a >= tc4)
              a--;

            if (handStage == HandState.River)
              if (a >= tc5)
                a--;
          }
        }

        if ((int)handStage >= (int)HandState.Flop)
        {
          if (b >= tc1)
            b--;
          if (b >= tc2)
            b--;
          if (b >= tc3)
            b--;

          if ((int)handStage >= (int)HandState.Turn)
          {
            if (b >= tc4)
              b--;

            if (handStage == HandState.River)
              if (b >= tc5)
                b--;
          }
        }

        ushort result = 0;

        switch (handStage)
        {
          case HandState.Flop:
            result = data[1176 * (numPlayers - 2) + b - 1 + (a - 2) * (a - 1) / 2];
            break;
          case HandState.Turn:
            result = data[1128 * (numPlayers - 2) + b - 1 + (a - 2) * (a - 1) / 2];
            break;
          case HandState.River:
            result = data[1081 * (numPlayers - 2) + b - 1 + (a - 2) * (a - 1) / 2];
            break;
          default:
            throw new Exception();
        }

        return result;
      }

      public int GetWinRatioSortedIndex(HandState handStage, Card hc1, Card hc2, int numPlayers)
      {
        ushort[] data;

        if (handStage == this.furthestLoadedHandStage)
          data = index1;
        else if (handStage == HandState.Flop && this.furthestLoadedHandStage == HandState.Turn)
          data = index2;
        else if (handStage == HandState.Flop && this.furthestLoadedHandStage == HandState.River)
          data = index3;
        else if (handStage == HandState.Turn && this.furthestLoadedHandStage == HandState.River)
          data = index2;
        else
          throw new Exception();

        int a = (int)hc1, b = (int)hc2;

        byte tc1 = 0, tc2 = 0, tc3 = 0, tc4 = 0, tc5 = 0;

        if ((int)handStage >= (int)HandState.Flop)
        {
          tc1 = (byte)card1;
          tc2 = (byte)card2;
          tc3 = (byte)card3;
        }

        if ((int)handStage >= (int)HandState.Turn)
          tc4 = (byte)card4;

        if (handStage == HandState.River)
          tc5 = (byte)card5;

        bool sorted = false;
        byte tempCard;

        while (!sorted)
        {
          sorted = true;

          if (tc1 < tc2)
          { tempCard = tc1; tc1 = tc2; tc2 = tempCard; sorted = false; }
          if (tc2 < tc3)
          { tempCard = tc2; tc2 = tc3; tc3 = tempCard; sorted = false; }
          if (tc3 < tc4)
          { tempCard = tc3; tc3 = tc4; tc4 = tempCard; sorted = false; }
          if (tc4 < tc5)
          { tempCard = tc4; tc4 = tc5; tc5 = tempCard; sorted = false; }
        }

        if ((int)handStage >= (int)HandState.Flop)
        {
          if (a >= tc1)
            a--;
          if (a >= tc2)
            a--;
          if (a >= tc3)
            a--;

          if ((int)handStage >= (int)HandState.Turn)
          {
            if (a >= tc4)
              a--;

            if (handStage == HandState.River)
              if (a >= tc5)
                a--;
          }
        }

        if ((int)handStage >= (int)HandState.Flop)
        {
          if (b >= tc1)
            b--;
          if (b >= tc2)
            b--;
          if (b >= tc3)
            b--;

          if ((int)handStage >= (int)HandState.Turn)
          {
            if (b >= tc4)
              b--;

            if (handStage == HandState.River)
              if (b >= tc5)
                b--;
          }
        }

        ushort result = 0;

        switch (handStage)
        {
          case HandState.Flop:
            result = data[1176 * (numPlayers - 2) + b - 1 + (a - 2) * (a - 1) / 2];
            break;
          case HandState.Turn:
            result = data[1128 * (numPlayers - 2) + b - 1 + (a - 2) * (a - 1) / 2];
            break;
          case HandState.River:
            result = data[1081 * (numPlayers - 2) + b - 1 + (a - 2) * (a - 1) / 2];
            break;
          default:
            throw new Exception();
        }

        return result;
      }

      public void ReplaceData(HandState handStage, int maxNumPlayers, Card card1, Card card2, Card card3, Card card4, Card card5, ushort[] data, ushort[] indexes)
      {
        this.furthestLoadedHandStage = handStage;
        this.card1 = card1;
        this.card2 = card2;
        this.card3 = card3;
        this.card4 = card4;
        this.card5 = card5;

        if (handStage == HandState.Flop)
        {
          data1 = data;
          data2 = null;
          data3 = null;

          index1 = indexes;
          index2 = null;
          index3 = null;

          maxNumPlayersFlop = maxNumPlayers;
          maxNumPlayersTurn = -1;
          maxNumPlayersRiver = -1;
        }
        else if (handStage == HandState.Turn)
        {
          data2 = data1;
          data1 = data;

          index2 = index1;
          index1 = indexes;

          maxNumPlayersTurn = maxNumPlayers;
        }
        else if (handStage == HandState.River)
        {
          data3 = data2;
          data2 = data1;
          data1 = data;

          index3 = index2;
          index2 = index1;
          index1 = indexes;

          maxNumPlayersRiver = maxNumPlayers;
        }
        else
          throw new Exception();
      }
    }

    //Default positions of WR files
    static string riverWinPercentageFile = "Y:\\WR\\River.dat", turnWinPercentageFile = "Y:\\WR\\Turn.dat", flopWinPercentageFile = "Y:\\WR\\Flop.dat";

    static string riverIndexFile = "E:\\WR results\\Indexes\\River\\5_52.dat", turnIndexFile = "E:\\WR results\\Indexes\\Turn\\4_52.dat", flopIndexFile = "E:\\WR results\\Indexes\\Flop\\3_52.dat";

    static string riverLocationFile = "Y:\\WRLocations\\riverLocations.dat", turnLocationFile = "Y:\\WRLocations\\turnLocations.dat", flopLocationFile = "Y:\\WRLocations\\flopLocations.dat";

    static string preFlopFile = "Y:\\WR\\PreflopWP.dat", preFlopRanksFile = "Y:\\WR\\PreflopRanks.dat";

    static Dictionary<long, WinRatioEntry> winRatiosByTable;
    static Dictionary<long, TableModel> tableModels;
    static ushort[,,] preflopWR;
    static byte[,,] preFlopRanks;
    static object staticLocker = new object();

    static long[] flopLocations, turnLocations, riverLocations;
    static Stream flopIndexStream, turnIndexStream, riverIndexStream, flopWPStream, turnWPStream, riverWPStream;

    public static WinRatioProvider FirstInstance { get; private set; }

    object instanceLocker = new object();

    /// <summary>
    /// Contructor
    /// </summary>
    /// <param name="information"></param>
    /// <param name="globalRequestedInfoTypes"></param>
    /// <param name="allInformationProviders"></param>
    /// <param name="cacheTracker"></param>
    public WinRatioProvider(InfoCollection information, Dictionary<InfoProviderType, InfoProviderBase> allInformationProviders, AIRandomControl aiRandomControl)
        : base(information, InfoProviderType.WinRatio, allInformationProviders, aiRandomControl)
    {


      requiredInfoTypes = new List<InfoType>() { };
      providedInfoTypes = new List<InfoPiece>() { new InfoPiece(InfoType.WR_CardsOnlyWinRatio, 0),
                                                        new InfoPiece(InfoType.WR_CardsOnlyWinPercentage, 0),
                                                        new InfoPiece(InfoType.WR_CardsOnlyOpponentWinPercentage, 1),
                                                        new InfoPiece(InfoType.WR_CardsOnlyWinPercentageLastRoundChange, 0.5m),
                                                        new InfoPiece(InfoType.WR_ProbOpponentHasBetterWR, 1),
                                                        new InfoPiece(InfoType.WR_CardsOnlyWinPercentageIndex, 1),
                                                        new InfoPiece(InfoType.WR_AveragePercievedProbBotHasBetterHand, 0),
                                                        new InfoPiece(InfoType.WR_ModelAction, (byte)PokerAction.Fold),
                                                        new InfoPiece(InfoType.WR_ModelActionAmount, 0),
                                                        new InfoPiece(InfoType.WR_RaiseToCallAmount, 0),
                                                        new InfoPiece(InfoType.WR_RaiseToStealAmount, 0),
                                                        new InfoPiece(InfoType.WR_RaiseToStealSuccessProb, 0),
                                                        new InfoPiece(InfoType.WR_RaiseToCallStealSuccessProb, 0),
                                                        new InfoPiece(InfoType.WR_ProbOpponentHasBetterWRFIXED, 0)
                                                        };

      AddProviderInformationTypes();

      lock (staticLocker)
      {
        if (winRatiosByTable == null)
        {
          //If current job is null or the job contains no win ratio data we must load off of a local disk
          if (true)
          {
            #region CurrentJob Null
            //If we cannot find files in the default positions we look in the registry
            if (!File.Exists(riverWinPercentageFile) || !File.Exists(turnWinPercentageFile) || !File.Exists(flopWinPercentageFile) || !File.Exists(preFlopFile) || !File.Exists(preFlopRanksFile))
            {
              //If one of the files is missing go to the registry and see if a link exists there
              RegistryKey key = Registry.LocalMachine.OpenSubKey("Software\\FullBotPoker");
              try
              {
                if (key != null)
                {
                  preFlopFile = key.GetValue("WRpreflop.dat", "Y:\\WR\\PreflopWP.dat") as string;
                  preFlopRanksFile = key.GetValue("WRpreflopRanks.dat", "Y:\\WR\\PreflopRanks.dat") as string;
                  flopWinPercentageFile = key.GetValue("WRflop.dat", "Y:\\WR\\Flop.dat") as string;
                  turnWinPercentageFile = key.GetValue("WRturn.dat", "Y:\\WR\\Turn.dat") as string;
                  riverWinPercentageFile = key.GetValue("WRriver.dat", "Y:\\WR\\River.dat") as string;
                  flopIndexFile = key.GetValue("WRflopIndex.dat", "E:\\WR results\\Indexes\\Flop\\3_52.dat") as string;
                  turnIndexFile = key.GetValue("WRturnIndex.dat", "E:\\WR results\\Indexes\\Turn\\4_52.dat") as string;
                  riverIndexFile = key.GetValue("WRriverIndex.dat", "E:\\WR results\\Indexes\\River\\5_52.dat") as string;

                  if (!useOldLookupMethod)
                  {
                    flopLocationFile = key.GetValue("WRflopLocation.dat", "Y:\\WRLocations\\flopLocations.dat") as string;
                    turnLocationFile = key.GetValue("WRturnLocation.dat", "Y:\\WRLocations\\turnLocations.dat") as string;
                    riverLocationFile = key.GetValue("WRriverLocation.dat", "Y:\\WRLocations\\riverLocations.dat") as string;
                  }
                }
                else
                {
                  throw new Exception("WR file not found");
                }
              }
              catch (Exception ex)
              {
                throw ex;
              }
            }



            winRatiosByTable = new Dictionary<long, WinRatioEntry>();
            tableModels = new Dictionary<long, TableModel>();
            preflopWR = new ushort[9, 52, 52];
            preFlopRanks = new byte[9, 52, 52];

            using (BinaryReader br = new BinaryReader(File.Open(preFlopFile, FileMode.Open, FileAccess.Read, FileShare.Read)))
            {
              using (BinaryReader br2 = new BinaryReader(File.Open(preFlopRanksFile, FileMode.Open, FileAccess.Read, FileShare.Read)))
              {

                for (int n = 0; n < 9; n++)
                {
                  for (int i = 0; i < 52; i++)
                  {
                    for (int j = 0; j < 52; j++)
                    {
                      preflopWR[n, i, j] = br.ReadUInt16();
                      preFlopRanks[n, i, j] = br2.ReadByte();
                    }
                  }
                }
              }
            }

            if (!useOldLookupMethod)
            {
              using (BinaryReader br = new BinaryReader(File.Open(flopLocationFile, FileMode.Open, FileAccess.Read, FileShare.Read)))
              {
                flopLocations = new long[52 * 51 * 50 / 6];
                for (int i = 0; i < flopLocations.Length; i++)
                  flopLocations[i] = br.ReadInt64();
              }

              using (BinaryReader br = new BinaryReader(File.Open(turnLocationFile, FileMode.Open, FileAccess.Read, FileShare.Read)))
              {
                turnLocations = new long[52 * 51 * 50 * 49 / 24];
                for (int i = 0; i < turnLocations.Length; i++)
                  turnLocations[i] = br.ReadInt64();
              }

              using (BinaryReader br = new BinaryReader(File.Open(riverLocationFile, FileMode.Open, FileAccess.Read, FileShare.Read)))
              {
                riverLocations = new long[52 * 51 * 50 * 49 * 48 / 120];
                for (int i = 0; i < riverLocations.Length; i++)
                  riverLocations[i] = br.ReadInt64();
              }

              flopWPStream = File.OpenRead(flopWinPercentageFile);
              flopIndexStream = File.OpenRead(flopIndexFile);
              turnWPStream = File.OpenRead(turnWinPercentageFile);
              turnIndexStream = File.OpenRead(turnIndexFile);
              riverWPStream = File.OpenRead(riverWinPercentageFile);
              riverIndexStream = File.OpenRead(riverIndexFile);

            }
            #endregion
          }
          else
          {
            /*
            winRatiosByTable = new Dictionary<long, WinRatioEntry>();
            tableModels = new Dictionary<long, TableModel>();
            preflopWR = new ushort[9, 52, 52];
            preFlopRanks = new byte[9, 52, 52];

            //var decompressed = Packing.FBPSerialiser.DeCompressByteArray(CurrentJob.JobData.WinRatioProviderData);
            byte[] decompressed = DPSManager.GetDataSerializer<ProtobufSerializer>().DeserialiseDataObject<byte[]>(CurrentJob.JobData.WinRatioProviderData, new List<DataProcessor>() { winRatioCompressor }, new Dictionary<string,string>());

            CurrentJob.JobData.DeleteWinRatioData(); GC.Collect();
            WrapperForWinRatioData data = new WrapperForWinRatioData(decompressed);
            decompressed = null; GC.Collect();

            for (int n = 0; n < 9; n++)
            {
                for (int i = 0; i < 52; i++)
                {
                    for (int j = 0; j < 52; j++)
                    {
                        preflopWR[n, i, j] = data.preflopWP[n * 52 * 52 + i * 52 + j];
                        preFlopRanks[n, i, j] = data.preflopRanks[n * 52 * 52 + i * 52 + j];
                    }
                }
            }

            data.preflopWP = null; data.preflopRanks = null;

            flopLocations = data.flopLocations; data.flopLocations = null;
            turnLocations = data.turnLocations; data.turnLocations = null;
            riverLocations = data.riverLocations; data.riverLocations = null;

            int nEntries = data.flopWinPercentages.Length / (49 * 48 / 2);

            ushort[] numbers = new ushort[49 * 48 / 2];
            for (int i = 0; i < numbers.Length; i++)
                numbers[i] = (ushort)i;

            byte[] buffer = new byte[49 * 48];

            var flopWPBytes = new byte[data.flopWinPercentages.Length * 2];
            Buffer.BlockCopy(data.flopWinPercentages, 0, flopWPBytes, 0, flopWPBytes.Length);                        
            flopWPStream = new MemoryStream(flopWPBytes);
            flopIndexStream = new MemoryStream();                        

            ushort[] flopNumbers = new ushort[49 * 48 / 2];                       
            ushort[] outSort = new ushort[49 * 48 / 2];

            for (int i = 0; i < nEntries; i++)
            {
                Array.Copy(numbers, flopNumbers, flopNumbers.Length);

                Array.Sort(flopNumbers, (a, b) => {
                    var comp = data.flopWinPercentages[b + i * 48 * 49 / 2].CompareTo(data.flopWinPercentages[a + i * 48 * 49 / 2]);
                    if (comp != 0)
                        return comp;
                    else
                        return a.CompareTo(b);
                });

                for (int j = 0; j < outSort.Length; j++)
                    outSort[flopNumbers[j]] = (ushort)j;

                Buffer.BlockCopy(outSort, 0, buffer, 0, outSort.Length * sizeof(ushort));
                flopIndexStream.Write(buffer, 0, outSort.Length * sizeof(ushort));
            }
            data.flopWinPercentages = null; GC.Collect();

            var turnWPBytes = new byte[data.turnWinPercentages.Length * 2];
            Buffer.BlockCopy(data.turnWinPercentages, 0, turnWPBytes, 0, turnWPBytes.Length);                        
            turnWPStream = new MemoryStream(turnWPBytes);
            turnIndexStream = new MemoryStream();

            ushort[] turnNumbers = new ushort[48 * 47 / 2];
            outSort = new ushort[48 * 47 / 2];

            for (int i = 0; i < nEntries; i++)
            {
                Array.Copy(numbers, turnNumbers, turnNumbers.Length);

                Array.Sort(turnNumbers, (a, b) =>
                {
                    var comp = data.turnWinPercentages[b + i * 48 * 47 / 2].CompareTo(data.turnWinPercentages[a + i * 48 * 47 / 2]);
                    if (comp != 0)
                        return comp;
                    else
                        return a.CompareTo(b);
                });

                for (int j = 0; j < outSort.Length; j++)
                    outSort[turnNumbers[j]] = (ushort)j;

                Buffer.BlockCopy(outSort, 0, buffer, 0, outSort.Length * sizeof(ushort));
                turnIndexStream.Write(buffer, 0, outSort.Length * sizeof(ushort));
            }
            data.turnWinPercentages = null; GC.Collect();

            var riverWPBytes = new byte[data.riverWinPercentages.Length * 2];
            Buffer.BlockCopy(data.riverWinPercentages, 0, riverWPBytes, 0, riverWPBytes.Length);                        
            riverWPStream = new MemoryStream(riverWPBytes);
            riverIndexStream = new MemoryStream();

            ushort[] riverNumbers = new ushort[47 * 46 / 2];
            outSort = new ushort[47 * 46 / 2];

            for (int i = 0; i < nEntries; i++)
            {
                Array.Copy(numbers, riverNumbers, riverNumbers.Length);

                Array.Sort(riverNumbers, (a, b) =>
                {
                    var comp = data.riverWinPercentages[b + i * 47 * 46 / 2].CompareTo(data.riverWinPercentages[a + i * 47 * 46 / 2]);
                    if (comp != 0)
                        return comp;
                    else
                        return a.CompareTo(b);
                });

                for (int j = 0; j < outSort.Length; j++)
                    outSort[riverNumbers[j]] = (ushort)j;

                Buffer.BlockCopy(outSort, 0, buffer, 0, outSort.Length * sizeof(ushort));
                riverIndexStream.Write(buffer, 0, outSort.Length * sizeof(ushort));
            }
            data.riverWinPercentages = null; GC.Collect();

            flopIndexStream.Seek(0, 0);
            turnIndexStream.Seek(0, 0);
            riverIndexStream.Seek(0, 0);
            */
          }
        }

        if (FirstInstance == null)
          FirstInstance = this;

      }
    }

    public override void ResetProvider()
    {
      lock (staticLocker)
      {
        tableModels = new Dictionary<long, TableModel>();
        winRatiosByTable = new Dictionary<long, WinRatioEntry>();
      }
    }

    public void GetWinRatioExt(int numberPlayers, byte hc1, byte hc2, byte tc1, byte tc2, byte tc3, byte tc4, byte tc5, out double winRatio, out double winPercentage)
    {
      lock (instanceLocker)
      {
        List<Card> tcs = new List<Card>();
        if (tc1 != 0)
          tcs.Add((Card)tc1);
        if (tc2 != 0)
          tcs.Add((Card)tc2);
        if (tc3 != 0)
          tcs.Add((Card)tc3);
        if (tc4 != 0)
          tcs.Add((Card)tc4);
        if (tc5 != 0)
          tcs.Add((Card)tc5);

        float WP = GetWinPercentage((Card)hc1, (Card)hc2, tcs.ToArray(), numberPlayers);

        winPercentage = WP;
        winRatio = WP * (numberPlayers - 1) / (100.0 - WP);
      }
    }

    private int internalCounter = 0;
    public int GetWinPercentageIndexExt(Card hc1, Card hc2, Card[] tableCards, int numberPlayers, long tableId = -1, long handId = -1)
    {
      lock (instanceLocker)
      {
        int result = GetWinPercentageIndex(hc1, hc2, tableCards, numberPlayers, tableId, handId == -1 ? internalCounter : handId);
        internalCounter++;
        return result;
      }
    }

    private float GetWinPercentage(Card hc1, Card hc2, Card[] tableCards, int numPlayers, long tableId = -1, long handId = -1)
    {
      lock (instanceLocker)
      {
        float result = 100.0f * ((float)GetWinPercentageShort(hc1, hc2, tableCards, numPlayers, tableId, handId == -1 ? internalCounter : handId)) / ushort.MaxValue;
        internalCounter++;
        return result;
      }
    }


    private ushort GetWinPercentageShort(Card hc1, Card hc2, Card[] tableCards, int numPlayers, long tableID, long handId)
    {
      HandState handStage;
      WinRatioEntry entry = null;

      if ((byte)hc2 > (byte)hc1)
      {
        Card temp = hc2;
        hc2 = hc1;
        hc1 = temp;
      }

      switch (tableCards.Length)
      {
        case 0:
          handStage = HandState.PreFlop;
          break;
        case 3:
          handStage = HandState.Flop;
          break;
        case 4:
          handStage = HandState.Turn;
          break;
        case 5:
          handStage = HandState.River;
          break;
        default:
          throw new Exception();
      }

      if (handStage == HandState.PreFlop)
        return preflopWR[numPlayers - 2, (int)(hc1 - 1), (int)(hc2 - 1)];

      if (!winRatiosByTable.ContainsKey(tableID))
      {
        lock (staticLocker)
        {
          if (!winRatiosByTable.ContainsKey(tableID))
          {
            ushort[] loadedWR, loadedWI;

            LoadWinRatio(tableCards[0], tableCards[1], tableCards[2], Card.NoCard, Card.NoCard, numPlayers, out loadedWR, out loadedWI);

            lock (staticLocker)
              winRatiosByTable.Add(tableID, new WinRatioEntry(handId, HandState.Flop, numPlayers, tableCards[0], tableCards[1], tableCards[2], Card.NoCard, Card.NoCard, loadedWR, loadedWI));

            entry = winRatiosByTable[tableID];
          }
        }

        lock (instanceLocker)
        {
          if (handStage >= HandState.Turn)
            if (entry.Card1 != tableCards[0] || entry.Card2 != tableCards[1] || entry.Card3 != tableCards[2] || entry.Card4 != tableCards[3] || entry.MaxNumPlayers(HandState.Turn) < numPlayers)
            {
              ushort[] loadedWR, loadedWI;
              LoadWinRatio(tableCards[0], tableCards[1], tableCards[2], tableCards[3], Card.NoCard, numPlayers, out loadedWR, out loadedWI);

              entry.ReplaceData(HandState.Turn, numPlayers, tableCards[0], tableCards[1], tableCards[2], tableCards[3], Card.NoCard, loadedWR, loadedWI);
            }

          if (handStage == HandState.River)
            if (entry.Card1 != tableCards[0] || entry.Card2 != tableCards[1] || entry.Card3 != tableCards[2] || entry.Card4 != tableCards[3] || entry.Card5 != tableCards[4] || entry.MaxNumPlayers(HandState.River) < numPlayers)
            {
              ushort[] loadedWR, loadedWI;
              LoadWinRatio(tableCards[0], tableCards[1], tableCards[2], tableCards[3], tableCards[4], numPlayers, out loadedWR, out loadedWI);

              entry.ReplaceData(handStage, numPlayers, tableCards[0], tableCards[1], tableCards[2], tableCards[3], tableCards[4], loadedWR, loadedWI);
            }
        }
      }
      else
      {
        lock (staticLocker)
          entry = winRatiosByTable[tableID];

        entry.CheckNewHandAndResetEntry(handId);

        if ((int)handStage <= (int)(entry.FurthestLoadedHandStage) && numPlayers <= entry.MaxNumPlayers(handStage))
        {
          switch (handStage)
          {
            case HandState.Flop:
              if (entry.Card1 == tableCards[0] && entry.Card2 == tableCards[1] && entry.Card3 == tableCards[2])
                return entry.GetWinRatio(handStage, hc1, hc2, numPlayers);
              break;
            case HandState.Turn:
              if (entry.Card1 == tableCards[0] && entry.Card2 == tableCards[1] && entry.Card3 == tableCards[2] && entry.Card4 == tableCards[3])
                return entry.GetWinRatio(handStage, hc1, hc2, numPlayers);
              break;
            case HandState.River:
              if (entry.Card1 == tableCards[0] && entry.Card2 == tableCards[1] && entry.Card3 == tableCards[2] && entry.Card4 == tableCards[3] && entry.Card5 == tableCards[4])
                return entry.GetWinRatio(handStage, hc1, hc2, numPlayers);
              break;
          }
        }

        lock (instanceLocker)
        {
          if (handStage >= HandState.Flop)
          {
            if (entry.Card1 != tableCards[0] || entry.Card2 != tableCards[1] || entry.Card3 != tableCards[2] || entry.MaxNumPlayers(HandState.Flop) < numPlayers)
            {
              ushort[] loadedWR, loadedWI;
              LoadWinRatio(tableCards[0], tableCards[1], tableCards[2], Card.NoCard, Card.NoCard, numPlayers, out loadedWR, out loadedWI);

              entry.ReplaceData(HandState.Flop, numPlayers, tableCards[0], tableCards[1], tableCards[2], Card.NoCard, Card.NoCard, loadedWR, loadedWI);
            }
          }

          if (handStage >= HandState.Turn)
          {
            if (entry.Card1 != tableCards[0] || entry.Card2 != tableCards[1] || entry.Card3 != tableCards[2] || entry.Card4 != tableCards[3] || entry.MaxNumPlayers(HandState.Turn) < numPlayers)
            {
              ushort[] loadedWR, loadedWI;
              LoadWinRatio(tableCards[0], tableCards[1], tableCards[2], tableCards[3], Card.NoCard, numPlayers, out loadedWR, out loadedWI);

              entry.ReplaceData(HandState.Turn, numPlayers, tableCards[0], tableCards[1], tableCards[2], tableCards[3], Card.NoCard, loadedWR, loadedWI);
            }
          }

          if (handStage == HandState.River)
          {
            if (entry.Card1 != tableCards[0] || entry.Card2 != tableCards[1] || entry.Card3 != tableCards[2] || entry.Card4 != tableCards[3] || entry.Card5 != tableCards[4] || entry.MaxNumPlayers(HandState.River) < numPlayers)
            {
              ushort[] loadedWR, loadedWI;
              LoadWinRatio(tableCards[0], tableCards[1], tableCards[2], tableCards[3], tableCards[4], numPlayers, out loadedWR, out loadedWI);

              entry.ReplaceData(handStage, numPlayers, tableCards[0], tableCards[1], tableCards[2], tableCards[3], tableCards[4], loadedWR, loadedWI);
            }
          }
        }
      }


      lock (staticLocker)
        entry = winRatiosByTable[tableID];

      switch (handStage)
      {
        case HandState.Flop:
          if (entry.Card1 == tableCards[0] && entry.Card2 == tableCards[1] && entry.Card3 == tableCards[2])
            return entry.GetWinRatio(handStage, hc1, hc2, numPlayers);
          break;
        case HandState.Turn:
          if (entry.Card1 == tableCards[0] && entry.Card2 == tableCards[1] && entry.Card3 == tableCards[2] && entry.Card4 == tableCards[3])
            return entry.GetWinRatio(handStage, hc1, hc2, numPlayers);
          break;
        case HandState.River:
          if (entry.Card1 == tableCards[0] && entry.Card2 == tableCards[1] && entry.Card3 == tableCards[2] && entry.Card4 == tableCards[3] && entry.Card5 == tableCards[4])
            return entry.GetWinRatio(handStage, hc1, hc2, numPlayers);
          break;
      }

      throw new Exception();
    }


    private int GetWinPercentageIndex(Card hc1, Card hc2, Card[] tableCards, int numPlayers, long tableID, long handId)
    {
      HandState handStage;
      WinRatioEntry entry = null;

      if ((byte)hc2 > (byte)hc1)
      {
        Card temp = hc2;
        hc2 = hc1;
        hc1 = temp;
      }

      switch (tableCards.Length)
      {
        case 0:
          handStage = HandState.PreFlop;
          break;
        case 3:
          handStage = HandState.Flop;
          break;
        case 4:
          handStage = HandState.Turn;
          break;
        case 5:
          handStage = HandState.River;
          break;
        default:
          throw new Exception();
      }

      if (handStage == HandState.PreFlop)
        return preFlopRanks[numPlayers - 2, (int)(hc1 - 1), (int)(hc2 - 1)];

      if (!winRatiosByTable.ContainsKey(tableID))
      {
        lock (staticLocker)
        {
          if (!winRatiosByTable.ContainsKey(tableID))
          {
            ushort[] loadedWR, loadedWI;
            LoadWinRatio(tableCards[0], tableCards[1], tableCards[2], Card.NoCard, Card.NoCard, numPlayers, out loadedWR, out loadedWI);

            lock (staticLocker)
              winRatiosByTable.Add(tableID, new WinRatioEntry(handId, HandState.Flop, numPlayers, tableCards[0], tableCards[1], tableCards[2], Card.NoCard, Card.NoCard, loadedWR, loadedWI));

            entry = winRatiosByTable[tableID];
          }
        }

        lock (instanceLocker)
        {
          if (handStage >= HandState.Turn)
          {
            if (entry.Card1 != tableCards[0] || entry.Card2 != tableCards[1] || entry.Card3 != tableCards[2] || entry.Card4 != tableCards[3] || entry.MaxNumPlayers(HandState.Turn) < numPlayers)
            {
              ushort[] loadedWR, loadedWI;
              LoadWinRatio(tableCards[0], tableCards[1], tableCards[2], tableCards[3], Card.NoCard, numPlayers, out loadedWR, out loadedWI);

              entry.ReplaceData(HandState.Turn, numPlayers, tableCards[0], tableCards[1], tableCards[2], tableCards[3], Card.NoCard, loadedWR, loadedWI);
            }
          }

          if (handStage == HandState.River)
            if (entry.Card1 != tableCards[0] || entry.Card2 != tableCards[1] || entry.Card3 != tableCards[2] || entry.Card4 != tableCards[3] || entry.Card5 != tableCards[4] || entry.MaxNumPlayers(HandState.River) < numPlayers)
            {
              ushort[] loadedWR, loadedWI;
              LoadWinRatio(tableCards[0], tableCards[1], tableCards[2], tableCards[3], tableCards[4], numPlayers, out loadedWR, out loadedWI);

              entry.ReplaceData(handStage, numPlayers, tableCards[0], tableCards[1], tableCards[2], tableCards[3], tableCards[4], loadedWR, loadedWI);
            }
        }
      }
      else
      {
        lock (staticLocker)
          entry = winRatiosByTable[tableID];

        entry.CheckNewHandAndResetEntry(handId);

        if ((int)handStage <= (int)(entry.FurthestLoadedHandStage) && numPlayers <= entry.MaxNumPlayers(handStage))
        {
          switch (handStage)
          {
            case HandState.Flop:
              if (entry.Card1 == tableCards[0] && entry.Card2 == tableCards[1] && entry.Card3 == tableCards[2])
                return entry.GetWinRatioSortedIndex(handStage, hc1, hc2, numPlayers);
              break;
            case HandState.Turn:
              if (entry.Card1 == tableCards[0] && entry.Card2 == tableCards[1] && entry.Card3 == tableCards[2] && entry.Card4 == tableCards[3])
                return entry.GetWinRatioSortedIndex(handStage, hc1, hc2, numPlayers);
              break;
            case HandState.River:
              if (entry.Card1 == tableCards[0] && entry.Card2 == tableCards[1] && entry.Card3 == tableCards[2] && entry.Card4 == tableCards[3] && entry.Card5 == tableCards[4])
                return entry.GetWinRatioSortedIndex(handStage, hc1, hc2, numPlayers);
              break;
          }
        }

        lock (instanceLocker)
        {

          if (handStage >= HandState.Flop)
          {
            if (entry.Card1 != tableCards[0] || entry.Card2 != tableCards[1] || entry.Card3 != tableCards[2] || entry.MaxNumPlayers(HandState.Flop) < numPlayers)
            {
              ushort[] loadedWR, loadedWI;
              LoadWinRatio(tableCards[0], tableCards[1], tableCards[2], Card.NoCard, Card.NoCard, numPlayers, out loadedWR, out loadedWI);

              entry.ReplaceData(HandState.Flop, numPlayers, tableCards[0], tableCards[1], tableCards[2], Card.NoCard, Card.NoCard, loadedWR, loadedWI);
            }
          }

          if (handStage >= HandState.Turn)
          {
            if (entry.Card1 != tableCards[0] || entry.Card2 != tableCards[1] || entry.Card3 != tableCards[2] || entry.Card4 != tableCards[3] || entry.MaxNumPlayers(HandState.Turn) < numPlayers)
            {
              ushort[] loadedWR, loadedWI;
              LoadWinRatio(tableCards[0], tableCards[1], tableCards[2], tableCards[3], Card.NoCard, numPlayers, out loadedWR, out loadedWI);

              entry.ReplaceData(HandState.Turn, numPlayers, tableCards[0], tableCards[1], tableCards[2], tableCards[3], Card.NoCard, loadedWR, loadedWI);
            }
          }

          if (handStage == HandState.River)
          {
            if (entry.Card1 != tableCards[0] || entry.Card2 != tableCards[1] || entry.Card3 != tableCards[2] || entry.Card4 != tableCards[3] || entry.Card5 != tableCards[4] || entry.MaxNumPlayers(HandState.River) < numPlayers)
            {
              ushort[] loadedWR, loadedWI;
              LoadWinRatio(tableCards[0], tableCards[1], tableCards[2], tableCards[3], tableCards[4], numPlayers, out loadedWR, out loadedWI);

              entry.ReplaceData(handStage, numPlayers, tableCards[0], tableCards[1], tableCards[2], tableCards[3], tableCards[4], loadedWR, loadedWI);
            }
          }
        }
      }


      lock (staticLocker)
        entry = winRatiosByTable[tableID];

      switch (handStage)
      {
        case HandState.Flop:
          if (entry.Card1 == tableCards[0] && entry.Card2 == tableCards[1] && entry.Card3 == tableCards[2])
            return entry.GetWinRatioSortedIndex(handStage, hc1, hc2, numPlayers);
          break;
        case HandState.Turn:
          if (entry.Card1 == tableCards[0] && entry.Card2 == tableCards[1] && entry.Card3 == tableCards[2] && entry.Card4 == tableCards[3])
            return entry.GetWinRatioSortedIndex(handStage, hc1, hc2, numPlayers);
          break;
        case HandState.River:
          if (entry.Card1 == tableCards[0] && entry.Card2 == tableCards[1] && entry.Card3 == tableCards[2] && entry.Card4 == tableCards[3] && entry.Card5 == tableCards[4])
            return entry.GetWinRatioSortedIndex(handStage, hc1, hc2, numPlayers);
          break;
      }

      throw new Exception();
    }


    private WinRatioEntry GetWinPercentageEntry(Card[] tableCards, int numPlayers, long tableID, long handId)
    {
      HandState handStage;
      WinRatioEntry entry = null;

      switch (tableCards.Length)
      {
        case 0:
          throw new Exception("Should not call this for preflop");
        case 3:
          handStage = HandState.Flop;
          break;
        case 4:
          handStage = HandState.Turn;
          break;
        case 5:
          handStage = HandState.River;
          break;
        default:
          throw new Exception();
      }

      if (!winRatiosByTable.ContainsKey(tableID))
      {
        lock (staticLocker)
        {
          if (!winRatiosByTable.ContainsKey(tableID))
          {
            ushort[] loadedWR, loadedWI;
            LoadWinRatio(tableCards[0], tableCards[1], tableCards[2], Card.NoCard, Card.NoCard, numPlayers, out loadedWR, out loadedWI);

            lock (staticLocker)
              winRatiosByTable.Add(tableID, new WinRatioEntry(handId, HandState.Flop, numPlayers, tableCards[0], tableCards[1], tableCards[2], Card.NoCard, Card.NoCard, loadedWR, loadedWI));

            entry = winRatiosByTable[tableID];
          }
        }

        lock (instanceLocker)
        {
          if (handStage >= HandState.Turn)
          {
            if (entry.Card1 != tableCards[0] || entry.Card2 != tableCards[1] || entry.Card3 != tableCards[2] || entry.Card4 != tableCards[3] || entry.MaxNumPlayers(HandState.Turn) < numPlayers)
            {
              ushort[] loadedWR, loadedWI;
              LoadWinRatio(tableCards[0], tableCards[1], tableCards[2], tableCards[3], Card.NoCard, numPlayers, out loadedWR, out loadedWI);

              entry.ReplaceData(HandState.Turn, numPlayers, tableCards[0], tableCards[1], tableCards[2], tableCards[3], Card.NoCard, loadedWR, loadedWI);
            }
          }

          if (handStage == HandState.River)
            if (entry.Card1 != tableCards[0] || entry.Card2 != tableCards[1] || entry.Card3 != tableCards[2] || entry.Card4 != tableCards[3] || entry.Card5 != tableCards[4] || entry.MaxNumPlayers(HandState.River) < numPlayers)
            {
              ushort[] loadedWR, loadedWI;
              LoadWinRatio(tableCards[0], tableCards[1], tableCards[2], tableCards[3], tableCards[4], numPlayers, out loadedWR, out loadedWI);

              entry.ReplaceData(handStage, numPlayers, tableCards[0], tableCards[1], tableCards[2], tableCards[3], tableCards[4], loadedWR, loadedWI);
            }
        }
      }
      else
      {
        lock (staticLocker)
          entry = winRatiosByTable[tableID];

        entry.CheckNewHandAndResetEntry(handId);

        if ((int)handStage <= (int)(entry.FurthestLoadedHandStage) && numPlayers <= entry.MaxNumPlayers(handStage))
        {
          switch (handStage)
          {
            case HandState.Flop:
              if (entry.Card1 == tableCards[0] && entry.Card2 == tableCards[1] && entry.Card3 == tableCards[2])
                return entry;
              break;
            case HandState.Turn:
              if (entry.Card1 == tableCards[0] && entry.Card2 == tableCards[1] && entry.Card3 == tableCards[2] && entry.Card4 == tableCards[3])
                return entry;
              break;
            case HandState.River:
              if (entry.Card1 == tableCards[0] && entry.Card2 == tableCards[1] && entry.Card3 == tableCards[2] && entry.Card4 == tableCards[3] && entry.Card5 == tableCards[4])
                return entry;
              break;
          }
        }

        lock (instanceLocker)
        {

          if (handStage >= HandState.Flop)
          {
            if (entry.Card1 != tableCards[0] || entry.Card2 != tableCards[1] || entry.Card3 != tableCards[2] || entry.MaxNumPlayers(HandState.Flop) < numPlayers)
            {
              ushort[] loadedWR, loadedWI;
              LoadWinRatio(tableCards[0], tableCards[1], tableCards[2], Card.NoCard, Card.NoCard, numPlayers, out loadedWR, out loadedWI);

              entry.ReplaceData(HandState.Flop, numPlayers, tableCards[0], tableCards[1], tableCards[2], Card.NoCard, Card.NoCard, loadedWR, loadedWI);
            }
          }

          if (handStage >= HandState.Turn)
          {
            if (entry.Card1 != tableCards[0] || entry.Card2 != tableCards[1] || entry.Card3 != tableCards[2] || entry.Card4 != tableCards[3] || entry.MaxNumPlayers(HandState.Turn) < numPlayers)
            {
              ushort[] loadedWR, loadedWI;
              LoadWinRatio(tableCards[0], tableCards[1], tableCards[2], tableCards[3], Card.NoCard, numPlayers, out loadedWR, out loadedWI);

              entry.ReplaceData(HandState.Turn, numPlayers, tableCards[0], tableCards[1], tableCards[2], tableCards[3], Card.NoCard, loadedWR, loadedWI);
            }
          }

          if (handStage == HandState.River)
          {
            if (entry.Card1 != tableCards[0] || entry.Card2 != tableCards[1] || entry.Card3 != tableCards[2] || entry.Card4 != tableCards[3] || entry.Card5 != tableCards[4] || entry.MaxNumPlayers(HandState.River) < numPlayers)
            {
              ushort[] loadedWR, loadedWI;
              LoadWinRatio(tableCards[0], tableCards[1], tableCards[2], tableCards[3], tableCards[4], numPlayers, out loadedWR, out loadedWI);

              entry.ReplaceData(handStage, numPlayers, tableCards[0], tableCards[1], tableCards[2], tableCards[3], tableCards[4], loadedWR, loadedWI);
            }
          }
        }
      }


      lock (staticLocker)
        entry = winRatiosByTable[tableID];

      switch (handStage)
      {
        case HandState.Flop:
          if (entry.Card1 == tableCards[0] && entry.Card2 == tableCards[1] && entry.Card3 == tableCards[2])
            return entry;
          break;
        case HandState.Turn:
          if (entry.Card1 == tableCards[0] && entry.Card2 == tableCards[1] && entry.Card3 == tableCards[2] && entry.Card4 == tableCards[3])
            return entry;
          break;
        case HandState.River:
          if (entry.Card1 == tableCards[0] && entry.Card2 == tableCards[1] && entry.Card3 == tableCards[2] && entry.Card4 == tableCards[3] && entry.Card5 == tableCards[4])
            return entry;
          break;
      }

      throw new Exception();
    }

    public override void ProviderSlowUpdateTask()
    {
      lock (staticLocker)
      {
        var activeTables = CacheTracker.Instance.AllActiveTableIds();
        var toRemove = tableModels.Keys.Except(activeTables).ToArray();

        foreach (var element in toRemove)
        {
          tableModels.Remove(element);
          winRatiosByTable.Remove(element);
        }
      }
    }



    public override void Close()
    {
      base.Close();

      lock (staticLocker)
      {
        if (flopIndexStream != null)
          flopIndexStream.Close();
        if (turnIndexStream != null)
          turnIndexStream.Close();
        if (riverIndexStream != null)
          riverIndexStream.Close();
        if (flopWPStream != null)
          flopWPStream.Close();
        if (turnWPStream != null)
          turnWPStream.Close();
        if (riverWPStream != null)
          riverWPStream.Close();

        flopIndexStream = null;
        turnIndexStream = null;
        riverIndexStream = null;
        flopWPStream = null;
        turnWPStream = null;
        riverWPStream = null;

        winRatiosByTable = null;
        tableModels = null;

        preflopWR = null;
        preFlopRanks = null;

        flopLocations = null;
        turnLocations = null;
        riverLocations = null;

        FirstInstance = null;
      }
    }

    protected override void updateInfo()
    {
      //Need win percentage for almost all stuff from this provider so calculate that first no matter what
      var botHoleCards = decisionRequest.Cache.getPlayerHoleCards(decisionRequest.PlayerId);
      var handdetails = decisionRequest.Cache.getCurrentHandDetails();

      List<Card> tableCardsList = new List<Card>();
      List<Card> prevTableCardsList = new List<Card>();
      if (handdetails.tableCard1 != 0)
      {
        tableCardsList.Add((Card)(handdetails.tableCard1));
        tableCardsList.Add((Card)(handdetails.tableCard2));
        tableCardsList.Add((Card)(handdetails.tableCard3));
        if (handdetails.tableCard4 != 0)
        {
          prevTableCardsList.AddRange(tableCardsList);
          tableCardsList.Add((Card)(handdetails.tableCard4));

          if (handdetails.tableCard5 != 0)
          {
            prevTableCardsList.Add((Card)(handdetails.tableCard4));
            tableCardsList.Add((Card)(handdetails.tableCard5));
          }
        }
      }

      int numberActivePlayers = decisionRequest.Cache.getActivePlayerIds().Length;
      ushort winPercentage = GetWinPercentageShort((Card)(botHoleCards.holeCard1), (Card)(botHoleCards.holeCard2),
          tableCardsList.ToArray(), numberActivePlayers, decisionRequest.Cache.TableId, decisionRequest.Cache.getCurrentHandId());

      if (decisionRequest.RequiredInfoTypeUpdateKey.IsInfoTypeRequired(InfoType.WR_CardsOnlyOpponentWinPercentage) ||
          decisionRequest.RequiredInfoTypeUpdateKey.IsInfoTypeRequired(InfoType.WR_CardsOnlyWinPercentageIndex) ||
          decisionRequest.RequiredInfoTypeUpdateKey.IsInfoTypeRequired(InfoType.WR_CardsOnlyWinPercentage) ||
          decisionRequest.RequiredInfoTypeUpdateKey.IsInfoTypeRequired(InfoType.WR_CardsOnlyWinPercentageLastRoundChange) ||
          decisionRequest.RequiredInfoTypeUpdateKey.IsInfoTypeRequired(InfoType.WR_CardsOnlyWinRatio) ||
          decisionRequest.RequiredInfoTypeUpdateKey.IsInfoTypeRequired(InfoType.WR_ModelAction) ||
          decisionRequest.RequiredInfoTypeUpdateKey.IsInfoTypeRequired(InfoType.WR_ModelAction))
      {
        if (decisionRequest.RequiredInfoTypeUpdateKey.IsInfoTypeRequired(InfoType.WR_CardsOnlyWinPercentageLastRoundChange))
        {
          ushort oldWinPercentage = GetWinPercentageShort((Card)(botHoleCards.holeCard1), (Card)(botHoleCards.holeCard2),
              prevTableCardsList.ToArray(), numberActivePlayers, decisionRequest.Cache.TableId, decisionRequest.Cache.getCurrentHandId());

          double winPercentageChange = (((double)winPercentage - (double)oldWinPercentage) + ushort.MaxValue) / (2.0 * ushort.MaxValue);

          if (winPercentageChange > 1)
            winPercentageChange = 1;
          if (winPercentageChange < 0)
            winPercentageChange = 0;

          infoStore.SetInformationValue(InfoType.WR_CardsOnlyWinPercentageLastRoundChange, (decimal)winPercentageChange);
        }

        if (decisionRequest.RequiredInfoTypeUpdateKey.IsInfoTypeRequired(InfoType.WR_CardsOnlyWinPercentageIndex) ||
            decisionRequest.RequiredInfoTypeUpdateKey.IsInfoTypeRequired(InfoType.WR_ModelAction) ||
            decisionRequest.RequiredInfoTypeUpdateKey.IsInfoTypeRequired(InfoType.WR_ModelAction))
        {
          int winIndexInt = GetWinPercentageIndex((Card)(botHoleCards.holeCard1), (Card)(botHoleCards.holeCard2),
              tableCardsList.ToArray(), numberActivePlayers, decisionRequest.Cache.TableId, decisionRequest.Cache.getCurrentHandId());

          decimal winPercentageIndex = winIndexInt;

          switch (tableCardsList.Count)
          {
            case 5:
              winPercentageIndex = winPercentageIndex / 1080.0m;
              break;
            case 4:
              winPercentageIndex = winPercentageIndex / 1127.0m;
              break;
            case 3:
              winPercentageIndex = winPercentageIndex / 1175.0m;
              break;
            case 0:
              winPercentageIndex = winPercentageIndex / 168.0m;
              break;
            default:
              throw new Exception();
          }

          infoStore.SetInformationValue(InfoType.WR_CardsOnlyWinPercentageIndex, winPercentageIndex);

          if (decisionRequest.RequiredInfoTypeUpdateKey.IsInfoTypeRequired(InfoType.WR_ModelAction))
          {
            throw new NotImplementedException();

            //PokerAction modelAction;
            //decimal modelAmount;

            //var handStage = (HandState)(tableCardsList.Count);
            //var lastRaise = decisionRequest.Cache.getMinimumPlayAmount();
            //var callAmount = lastRaise - decisionRequest.Cache.getPlayerCurrentRoundBetAmount(decisionRequest.PlayerId);
            //var potAmount = handdetails.potValue;

            //var raisedPot = handStage == HandState.PreFlop ? lastRaise > decisionRequest.Cache.BigBlind : lastRaise > 0;
            //var earlyPosition = (decisionRequest.Cache.getActivePlayerDistanceToDealer(decisionRequest.PlayerId) - 1.0) / (decisionRequest.Cache.getActivePositions().Length - 1.0) < 0.5;

            //TableModel.PlayerModel.GetActionForModelPlayer(randomGen, winIndexInt, handStage, callAmount, potAmount, earlyPosition, raisedPot, out modelAction, out modelAmount);

            //infoStore.SetInformationValue(InfoType.WR_ModelAction, (decimal)(modelAction));
            //infoStore.SetInformationValue(InfoType.WR_ModelActionAmount, modelAction == PokerAction.Raise ? (modelAmount + lastRaise) : (modelAmount));
          }
        }

        if (decisionRequest.RequiredInfoTypeUpdateKey.IsInfoTypeRequired(InfoType.WR_CardsOnlyWinPercentage) || decisionRequest.RequiredInfoTypeUpdateKey.IsInfoTypeRequired(InfoType.WR_CardsOnlyWinRatio) || decisionRequest.RequiredInfoTypeUpdateKey.IsInfoTypeRequired(InfoType.WR_CardsOnlyOpponentWinPercentage))
        {
          double ourWinPercentage = (double)winPercentage / (double)ushort.MaxValue;

          if (decisionRequest.RequiredInfoTypeUpdateKey.IsInfoTypeRequired(InfoType.WR_CardsOnlyWinPercentage))
            infoStore.SetInformationValue(InfoType.WR_CardsOnlyWinPercentage, (decimal)ourWinPercentage);

          if (decisionRequest.RequiredInfoTypeUpdateKey.IsInfoTypeRequired(InfoType.WR_CardsOnlyWinRatio) || decisionRequest.RequiredInfoTypeUpdateKey.IsInfoTypeRequired(InfoType.WR_CardsOnlyWeightedOpponentWinPercentage))
          {
            decimal opponentWinPercentage = (1 - (decimal)ourWinPercentage) / (numberActivePlayers - 1);
            decimal winRatio;

            if (opponentWinPercentage == 0)
              winRatio = 1000;
            else
              winRatio = (decimal)ourWinPercentage / opponentWinPercentage;

            if (winRatio > 1000)
              winRatio = 1000;

            infoStore.SetInformationValue(InfoType.WR_CardsOnlyWinRatio, winRatio);

            if (decisionRequest.RequiredInfoTypeUpdateKey.IsInfoTypeRequired(InfoType.WR_CardsOnlyOpponentWinPercentage))
              infoStore.SetInformationValue(InfoType.WR_CardsOnlyOpponentWinPercentage, opponentWinPercentage);
          }
        }


      }

      if (decisionRequest.RequiredInfoTypeUpdateKey.IsInfoTypeRequired(InfoType.WR_AveragePercievedProbBotHasBetterHand) ||
          decisionRequest.RequiredInfoTypeUpdateKey.IsInfoTypeRequired(InfoType.WR_ProbOpponentHasBetterWR) ||
          decisionRequest.RequiredInfoTypeUpdateKey.IsInfoTypeRequired(InfoType.WR_ProbOpponentHasBetterWRFIXED) ||
          decisionRequest.RequiredInfoTypeUpdateKey.IsInfoTypeRequired(InfoType.WR_RaiseToCallAmount) ||
          decisionRequest.RequiredInfoTypeUpdateKey.IsInfoTypeRequired(InfoType.WR_RaiseToStealAmount) ||
          decisionRequest.RequiredInfoTypeUpdateKey.IsInfoTypeRequired(InfoType.WR_RaiseToStealSuccessProb))
      {
        TableModel model;

        lock (staticLocker)
        {
          if (!(tableModels.ContainsKey(decisionRequest.Cache.TableId)))
            tableModels.Add(decisionRequest.Cache.TableId, new TableModel(decisionRequest.Cache, this, false));

          model = tableModels[decisionRequest.Cache.TableId];
        }

        model.UpdatePlayerModels(decisionRequest.Cache);

        if (decisionRequest.RequiredInfoTypeUpdateKey.IsInfoTypeRequired(InfoType.WR_ProbOpponentHasBetterWR))
        {
          throw new NotImplementedException();

          //double probBetter = model.GetProbAnyPlayerHasBetterHandOld(decisionRequest.Cache, decisionRequest.PlayerId, winPercentage);
          //infoStore.SetInformationValue(InfoType.WR_ProbOpponentHasBetterWR, (decimal)probBetter);
        }

        if (decisionRequest.RequiredInfoTypeUpdateKey.IsInfoTypeRequired(InfoType.WR_ProbOpponentHasBetterWRFIXED) ||
            decisionRequest.RequiredInfoTypeUpdateKey.IsInfoTypeRequired(InfoType.WR_RaiseToCallAmount) ||
            decisionRequest.RequiredInfoTypeUpdateKey.IsInfoTypeRequired(InfoType.WR_RaiseToStealAmount) ||
            decisionRequest.RequiredInfoTypeUpdateKey.IsInfoTypeRequired(InfoType.WR_RaiseToStealSuccessProb))
        {
          if (decisionRequest.RequiredInfoTypeUpdateKey.IsInfoTypeRequired(InfoType.WR_ProbOpponentHasBetterWRFIXED))
          {
            double probBetter = model.GetProbAnyPlayerHasBetterHandNew(decisionRequest.Cache, decisionRequest.PlayerId, winPercentage);
            infoStore.SetInformationValue(InfoType.WR_ProbOpponentHasBetterWRFIXED, (decimal)probBetter);
          }

          if (decisionRequest.RequiredInfoTypeUpdateKey.IsInfoTypeRequired(InfoType.WR_RaiseToCallAmount) ||
              decisionRequest.RequiredInfoTypeUpdateKey.IsInfoTypeRequired(InfoType.WR_RaiseToStealAmount) ||
              decisionRequest.RequiredInfoTypeUpdateKey.IsInfoTypeRequired(InfoType.WR_RaiseToStealSuccessProb) ||
              decisionRequest.RequiredInfoTypeUpdateKey.IsInfoTypeRequired(InfoType.WR_RaiseToCallStealSuccessProb))
          {
            if (tableCardsList.Count != 0)
            {
              decimal call, steal;
              double probSteal, probCall;
              model.GetRaiseCallStealAmounts(decisionRequest.Cache, decisionRequest.PlayerId, out call, out steal, out probSteal, out probCall);
              infoStore.SetInformationValue(InfoType.WR_RaiseToCallAmount, call);
              infoStore.SetInformationValue(InfoType.WR_RaiseToStealAmount, steal);
              infoStore.SetInformationValue(InfoType.WR_RaiseToStealSuccessProb, (decimal)probSteal);
              infoStore.SetInformationValue(InfoType.WR_RaiseToCallStealSuccessProb, (decimal)probCall);
            }
            else
            {
              infoStore.SetInformationValue(InfoType.WR_RaiseToCallAmount, 0);
              infoStore.SetInformationValue(InfoType.WR_RaiseToStealAmount, 0);
              infoStore.SetInformationValue(InfoType.WR_RaiseToStealSuccessProb, 0);
              infoStore.SetInformationValue(InfoType.WR_RaiseToCallStealSuccessProb, 0);
            }
          }
        }



        if (decisionRequest.RequiredInfoTypeUpdateKey.IsInfoTypeRequired(InfoType.WR_AveragePercievedProbBotHasBetterHand))
        {
          throw new NotImplementedException();

          //double avPercievedProbBetter = model.GetAveragePercievedProbPlayerHasBetterHand(decisionRequest.PlayerId, decisionRequest.Cache);
          //infoStore.SetInformationValue(InfoType.WR_AveragePercievedProbBotHasBetterHand, (decimal)avPercievedProbBetter);
        }
      }


    }

    private void LoadWinRatio(Card tc1, Card tc2, Card tc3, Card tc4, Card tc5, int maxNumPlayers, out ushort[] winPercentages, out ushort[] indexes, bool loadIndexes = true)
    {
      if (!useOldLookupMethod)
      {
        LoadWinRatioNew(tc1, tc2, tc3, tc4, tc5, maxNumPlayers, out winPercentages, out indexes);
        return;
      }

      indexes = null;

      bool sorted = false;
      Card tempCard;

      while (!sorted)
      {
        sorted = true;

        if (tc1 < tc2)
        { tempCard = tc1; tc1 = tc2; tc2 = tempCard; sorted = false; }
        if (tc2 < tc3)
        { tempCard = tc2; tc2 = tc3; tc3 = tempCard; sorted = false; }
        if (tc3 < tc4)
        { tempCard = tc3; tc3 = tc4; tc4 = tempCard; sorted = false; }
        if (tc4 < tc5)
        { tempCard = tc4; tc4 = tc5; tc5 = tempCard; sorted = false; }
      }

      long a, b, c, d, e;

      if (tc5 != Card.NoCard)
      {
        a = (int)tc1;
        b = (int)tc2;
        c = (int)tc3;
        d = (int)tc4;
        e = (int)tc5;

        long offset = 0;
        int size;

        if (a > 5)
          offset += (a * a * a * a * a - 15L * a * a * a * a + 85L * a * a * a - 225L * a * a + 274L * a - 120L) / 120L;
        if (b > 4)
          offset += (b * b * b * b - 10L * b * b * b + 35L * b * b - 50L * b + 24L) / 24L;
        if (c > 3)
          offset += (c * c * c - 6L * c * c + 11L * c - 6L) / 6L;
        if (d > 2)
          offset += (d * d - 3L * d + 2L) / 2L;
        if (e > 1)
          offset += e - 1;

        offset *= 5 + 9 * 2 * 47 * 46 / 2;
        offset += 5;
        size = 2 * 47 * 46 * (maxNumPlayers - 1) / 2;

        BinaryReader br = new BinaryReader(File.Open(riverWinPercentageFile, FileMode.Open, FileAccess.Read, FileShare.Read));
        br.BaseStream.Seek(offset, SeekOrigin.Begin);
        ByteToShortConverter converter = new ByteToShortConverter();
        converter.bytes = br.ReadBytes(size);
        br.Close();

        winPercentages = converter.ushorts;

        if (loadIndexes)
        {
          br = new BinaryReader(File.Open(riverIndexFile, FileMode.Open, FileAccess.Read, FileShare.Read));
          br.BaseStream.Seek(offset, SeekOrigin.Begin);
          ByteToShortConverter converter2 = new ByteToShortConverter();
          converter2.bytes = br.ReadBytes(size);
          br.Close();

          indexes = converter2.ushorts;
        }
      }
      else if (tc4 != Card.NoCard)
      {
        b = (int)tc1;
        c = (int)tc2;
        d = (int)tc3;
        e = (int)tc4;

        long offset = 0;
        int size;

        if (b > 4)
          offset += (b * b * b * b - 10L * b * b * b + 35L * b * b - 50L * b + 24L) / 24L;
        if (c > 3)
          offset += (c * c * c - 6L * c * c + 11L * c - 6L) / 6L;
        if (d > 2)
          offset += (d * d - 3L * d + 2L) / 2L;
        if (e > 1)
          offset += e - 1;

        offset *= 4 + 9 * 2 * 48 * 47 / 2;
        offset += 4;
        size = 2 * 48 * 47 * (maxNumPlayers - 1) / 2;

        BinaryReader br = new BinaryReader(File.Open(turnWinPercentageFile, FileMode.Open, FileAccess.Read, FileShare.Read));
        br.BaseStream.Seek(offset, SeekOrigin.Begin);
        ByteToShortConverter converter = new ByteToShortConverter();
        converter.bytes = br.ReadBytes(size);
        br.Close();

        winPercentages = converter.ushorts;

        if (loadIndexes)
        {
          br = new BinaryReader(File.Open(turnIndexFile, FileMode.Open, FileAccess.Read, FileShare.Read));
          br.BaseStream.Seek(offset, SeekOrigin.Begin);
          ByteToShortConverter converter2 = new ByteToShortConverter();
          converter2.bytes = br.ReadBytes(size);
          br.Close();

          indexes = converter2.ushorts;
        }
      }
      else if (tc3 != Card.NoCard)
      {
        c = (int)tc1;
        d = (int)tc2;
        e = (int)tc3;

        long offset = 0;
        int size;

        if (c > 3)
          offset += (c * c * c - 6L * c * c + 11L * c - 6L) / 6L;
        if (d > 2)
          offset += (d * d - 3L * d + 2L) / 2L;
        if (e > 1)
          offset += e - 1;

        offset *= 3 + 9 * 2 * 49 * 48 / 2;
        offset += 3;
        size = 2 * 49 * 48 * (maxNumPlayers - 1) / 2;

        BinaryReader br = new BinaryReader(File.Open(flopWinPercentageFile, FileMode.Open, FileAccess.Read, FileShare.Read));
        br.BaseStream.Seek(offset, SeekOrigin.Begin);
        ByteToShortConverter converter = new ByteToShortConverter();
        converter.bytes = br.ReadBytes(size);
        br.Close();

        winPercentages = converter.ushorts;

        if (loadIndexes)
        {
          br = new BinaryReader(File.Open(flopIndexFile, FileMode.Open, FileAccess.Read, FileShare.Read));
          br.BaseStream.Seek(offset, SeekOrigin.Begin);
          ByteToShortConverter converter2 = new ByteToShortConverter();
          converter2.bytes = br.ReadBytes(size);
          br.Close();

          indexes = converter2.ushorts;
        }
      }
      else
        throw new Exception();

      //ushort[] temp = new ushort[indexes.Length / 2];

      //for (int i = 0; i < temp.Length; i++)
      //    temp[i] = indexes[i];

      //indexes = temp;
    }


    private void LoadWinRatioNew(Card tc1, Card tc2, Card tc3, Card tc4, Card tc5, int maxNumPlayers, out ushort[] winPercentages, out ushort[] indexes, bool loadIndexes = true)
    {
      try
      {
        indexes = null;
        bool sorted = false;
        Card tempCard;

        while (!sorted)
        {
          sorted = true;

          if (tc1 < tc2)
          { tempCard = tc1; tc1 = tc2; tc2 = tempCard; sorted = false; }
          if (tc2 < tc3)
          { tempCard = tc2; tc2 = tc3; tc3 = tempCard; sorted = false; }
          if (tc3 < tc4)
          { tempCard = tc3; tc3 = tc4; tc4 = tempCard; sorted = false; }
          if (tc4 < tc5)
          { tempCard = tc4; tc4 = tc5; tc5 = tempCard; sorted = false; }
        }

        long a, b, c, d, e;

        if (tc5 != Card.NoCard)
        {
          a = (int)tc1;
          b = (int)tc2;
          c = (int)tc3;
          d = (int)tc4;
          e = (int)tc5;

          long offset = 0;
          int size;

          if (a > 5)
            offset += (a * a * a * a * a - 15L * a * a * a * a + 85L * a * a * a - 225L * a * a + 274L * a - 120L) / 120L;
          if (b > 4)
            offset += (b * b * b * b - 10L * b * b * b + 35L * b * b - 50L * b + 24L) / 24L;
          if (c > 3)
            offset += (c * c * c - 6L * c * c + 11L * c - 6L) / 6L;
          if (d > 2)
            offset += (d * d - 3L * d + 2L) / 2L;
          if (e > 1)
            offset += e - 1;

          offset = riverLocations[offset];

          if (offset < 0)
            throw new Exception("TC5 - Data not available for these cards");

          size = 2 * 47 * 46 * (maxNumPlayers - 1) / 2;

          lock (staticLocker)
          {
            riverWPStream.Seek(offset, SeekOrigin.Begin);
            ByteToShortConverter converter = new ByteToShortConverter();
            converter.bytes = new byte[size];
            riverWPStream.Read(converter.bytes, 0, size);

            winPercentages = converter.ushorts;

            if (loadIndexes)
            {
              riverIndexStream.Seek(offset, SeekOrigin.Begin);
              var converter2 = new ByteToShortConverter();
              converter2.bytes = new byte[size];
              riverIndexStream.Read(converter2.bytes, 0, size);

              indexes = converter2.ushorts;
            }
          }
        }
        else if (tc4 != Card.NoCard)
        {
          b = (int)tc1;
          c = (int)tc2;
          d = (int)tc3;
          e = (int)tc4;

          long offset = 0;
          int size;

          if (b > 4)
            offset += (b * b * b * b - 10L * b * b * b + 35L * b * b - 50L * b + 24L) / 24L;
          if (c > 3)
            offset += (c * c * c - 6L * c * c + 11L * c - 6L) / 6L;
          if (d > 2)
            offset += (d * d - 3L * d + 2L) / 2L;
          if (e > 1)
            offset += e - 1;

          offset = turnLocations[offset];

          if (offset < 0)
            throw new Exception("TC4 - Data not available for these cards");

          size = 2 * 48 * 47 * (maxNumPlayers - 1) / 2;

          lock (staticLocker)
          {
            turnWPStream.Seek(offset, SeekOrigin.Begin);
            ByteToShortConverter converter = new ByteToShortConverter();
            converter.bytes = new byte[size];
            turnWPStream.Read(converter.bytes, 0, size);

            winPercentages = converter.ushorts;

            if (loadIndexes)
            {
              turnIndexStream.Seek(offset, SeekOrigin.Begin);
              var converter2 = new ByteToShortConverter();
              converter2.bytes = new byte[size];
              turnIndexStream.Read(converter2.bytes, 0, size);

              indexes = converter2.ushorts;
            }
          }
        }
        else if (tc3 != Card.NoCard)
        {
          c = (int)tc1;
          d = (int)tc2;
          e = (int)tc3;

          long offset = 0;
          int size;

          if (c > 3)
            offset += (c * c * c - 6L * c * c + 11L * c - 6L) / 6L;
          if (d > 2)
            offset += (d * d - 3L * d + 2L) / 2L;
          if (e > 1)
            offset += e - 1;

          offset = flopLocations[offset];

          if (offset < 0)
            throw new Exception("TC3 - Data not available for these cards");

          size = 2 * 49 * 48 * (maxNumPlayers - 1) / 2;

          lock (staticLocker)
          {
            flopWPStream.Seek(offset, SeekOrigin.Begin);
            ByteToShortConverter converter = new ByteToShortConverter();
            converter.bytes = new byte[size];
            flopWPStream.Read(converter.bytes, 0, size);

            winPercentages = converter.ushorts;

            if (loadIndexes)
            {
              flopIndexStream.Seek(offset, SeekOrigin.Begin);
              var converter2 = new ByteToShortConverter();
              converter2.bytes = new byte[size];
              flopIndexStream.Read(converter2.bytes, 0, size);

              indexes = converter2.ushorts;
            }
          }
        }
        else
          throw new Exception();
      }
      catch (NullReferenceException)
      {
        throw;
      }
      catch (Exception ex)
      {
        throw new Exception("FlopStreamNull=" + (flopWPStream == null) +
            ", TurnStreamNull=" + (turnWPStream == null) +
            ", RiverStreamNull=" + (riverWPStream == null) +
            ". " +
            " numHands-" + decisionRequest.Cache.getNumHandsPlayed() +
            ", C1-" + tc1 +
            ", C2-" + tc2 +
            ", C3-" + tc3 +
            ", C4-" + tc4 +
            ", C5-" + tc5 +
            ", numPlayers " + maxNumPlayers + " " +
            ex.ToString());
      }
    }

    [ProtoContract]
    internal class WrapperForWinRatioData
    {
      [ProtoMember(1, IsPacked = true)]
      internal long[] flopLocations;
      [ProtoMember(2, IsPacked = true)]
      internal ushort[] flopWinPercentages;
      [ProtoMember(3, IsPacked = true)]
      internal long[] turnLocations;
      [ProtoMember(4, IsPacked = true)]
      internal ushort[] turnWinPercentages;
      [ProtoMember(5, IsPacked = true)]
      internal long[] riverLocations;
      [ProtoMember(6, IsPacked = true)]
      internal ushort[] riverWinPercentages;
      [ProtoMember(7, IsPacked = true)]
      internal ushort[] preflopWP;
      [ProtoMember(8, IsPacked = true)]
      internal byte[] preflopRanks;

      internal WrapperForWinRatioData() { }

      internal WrapperForWinRatioData(byte[] data)
      {
        int offset = 0;
        var sizes = new long[8];
        Buffer.BlockCopy(data, offset, sizes, 0, sizes.Length * sizeof(long));
        offset += sizes.Length * sizeof(long);

        if (data.Length != sizeof(long) * sizes.Length +
            (sizes[0] + sizes[1] + sizes[2]) * sizeof(long) +
            (sizes[3] + sizes[4] + sizes[5] + sizes[6]) * sizeof(ushort) + sizes[7])
          throw new InvalidDataException("data array for win ratio data not expected size");

        flopLocations = new long[sizes[0]];
        turnLocations = new long[sizes[1]];
        riverLocations = new long[sizes[2]];
        flopWinPercentages = new ushort[sizes[3]];
        turnWinPercentages = new ushort[sizes[4]];
        riverWinPercentages = new ushort[sizes[5]];
        preflopWP = new ushort[sizes[6]];
        preflopRanks = new byte[sizes[7]];

        Buffer.BlockCopy(data, offset, flopLocations, 0, flopLocations.Length * sizeof(long));
        offset += flopLocations.Length * sizeof(long);
        Buffer.BlockCopy(data, offset, turnLocations, 0, turnLocations.Length * sizeof(long));
        offset += turnLocations.Length * sizeof(long);
        Buffer.BlockCopy(data, offset, riverLocations, 0, riverLocations.Length * sizeof(long));
        offset += riverLocations.Length * sizeof(long);
        Buffer.BlockCopy(data, offset, flopWinPercentages, 0, flopWinPercentages.Length * sizeof(ushort));
        offset += flopWinPercentages.Length * sizeof(ushort);
        Buffer.BlockCopy(data, offset, turnWinPercentages, 0, turnWinPercentages.Length * sizeof(ushort));
        offset += turnWinPercentages.Length * sizeof(ushort);
        Buffer.BlockCopy(data, offset, riverWinPercentages, 0, riverWinPercentages.Length * sizeof(ushort));
        offset += riverWinPercentages.Length * sizeof(ushort);
        Buffer.BlockCopy(data, offset, preflopWP, 0, preflopWP.Length * sizeof(ushort));
        offset += preflopWP.Length * sizeof(ushort);
        Buffer.BlockCopy(data, offset, preflopRanks, 0, preflopRanks.Length * sizeof(byte));
        offset += preflopRanks.Length * sizeof(byte);
      }

      internal byte[] getBytes()
      {
        long totalSize = 0;
        var sizes = new long[8];
        totalSize += 8 * sizeof(long);
        sizes[0] = flopLocations.Length;
        totalSize += sizes[0] * sizeof(long);
        sizes[1] = turnLocations.Length;
        totalSize += sizes[1] * sizeof(long);
        sizes[2] = riverLocations.Length;
        totalSize += sizes[2] * sizeof(long);
        sizes[3] = flopWinPercentages.Length;
        totalSize += sizes[3] * sizeof(ushort);
        sizes[4] = turnWinPercentages.Length;
        totalSize += sizes[4] * sizeof(ushort);
        sizes[5] = riverWinPercentages.Length;
        totalSize += sizes[5] * sizeof(ushort);
        sizes[6] = preflopWP.Length;
        totalSize += sizes[6] * sizeof(ushort);
        sizes[7] = preflopRanks.Length;
        totalSize += sizes[7] * sizeof(byte);

        byte[] result = new byte[totalSize];

        int offset = 0;
        Buffer.BlockCopy(sizes, 0, result, offset, sizes.Length * sizeof(long));
        offset += sizes.Length * sizeof(long);
        Buffer.BlockCopy(flopLocations, 0, result, offset, flopLocations.Length * sizeof(long));
        offset += flopLocations.Length * sizeof(long);
        Buffer.BlockCopy(turnLocations, 0, result, offset, turnLocations.Length * sizeof(long));
        offset += turnLocations.Length * sizeof(long);
        Buffer.BlockCopy(riverLocations, 0, result, offset, riverLocations.Length * sizeof(long));
        offset += riverLocations.Length * sizeof(long);
        Buffer.BlockCopy(flopWinPercentages, 0, result, offset, flopWinPercentages.Length * sizeof(ushort));
        offset += flopWinPercentages.Length * sizeof(ushort);
        Buffer.BlockCopy(turnWinPercentages, 0, result, offset, turnWinPercentages.Length * sizeof(ushort));
        offset += turnWinPercentages.Length * sizeof(ushort);
        Buffer.BlockCopy(riverWinPercentages, 0, result, offset, riverWinPercentages.Length * sizeof(ushort));
        offset += riverWinPercentages.Length * sizeof(ushort);
        Buffer.BlockCopy(preflopWP, 0, result, offset, preflopWP.Length * sizeof(ushort));
        offset += preflopWP.Length * sizeof(ushort);
        Buffer.BlockCopy(preflopRanks, 0, result, offset, preflopRanks.Length * sizeof(byte));
        offset += preflopRanks.Length * sizeof(byte);

        return result;
      }
    }

    /// <summary>
    /// 
    /// </summary>
    /// <param name="handCards">The cards for this hand. First 5 must be table cards.</param>
    /// <param name="serialisedBytes"></param>
    public byte[] GenerateLocationsIndexesAndWinPercentages(List<byte[]> handCards)
    {
      byte[] serialisedBytes;

      /*if (CurrentJob != null && CurrentJob.JobData.WinRatioProviderData != null && CurrentJob.JobData.WinRatioProviderData.Length != 0)
          throw new Exception("Cannot create new location data from subset data sent over network");*/

      if (handCards == null)
        throw new Exception("Provided handCards cannot be null if this method is too suceed.");

      long[] flopLocations;
      ushort[] flopIndexes;
      ushort[] flopWinPercentages;
      long[] turnLocations;
      ushort[] turnIndexes;
      ushort[] turnWinPercentages;
      long[] riverLocations;
      ushort[] riverIndexes;
      ushort[] riverWinPercentages;

      flopLocations = new long[52 * 51 * 50 / 6];
      for (int i = 0; i < flopLocations.Length; i++)
        flopLocations[i] = -1L;
      turnLocations = new long[52 * 51 * 50 * 49 / 24];
      for (int i = 0; i < turnLocations.Length; i++)
        turnLocations[i] = -1L;
      riverLocations = new long[52 * 51 * 50 * 49 * 48 / 120];
      for (int i = 0; i < riverLocations.Length; i++)
        riverLocations[i] = -1L;

      riverWinPercentages = new ushort[handCards.Count * 47 * 46 * 9 / 2];

      turnWinPercentages = new ushort[handCards.Count * 47 * 48 * 9 / 2];

      flopWinPercentages = new ushort[handCards.Count * 49 * 48 * 9 / 2];

      for (int i = 0; i < handCards.Count; i++)
      {
        Card[] cards = (from current in handCards[i] select (Card)current).ToArray();

        if (cards.Length < 5)
          throw new InvalidOperationException("Must provide atleast 5 table cards");

        #region riverSection

        {
          byte a = (byte)cards[0], b = (byte)cards[1], c = (byte)cards[2], d = (byte)cards[3], e = (byte)cards[4];

          long riverOffset = 0;
          long sizeInBytes = 2 * 47 * 46 * 9 / 2;
          bool sorted = false;
          byte temp;

          while (!sorted)
          {
            sorted = true;

            if (a < b)
            { temp = a; a = b; b = temp; sorted = false; }
            if (b < c)
            { temp = b; b = c; c = temp; sorted = false; }
            if (c < d)
            { temp = c; c = d; d = temp; sorted = false; }
            if (d < e)
            { temp = d; d = e; e = temp; sorted = false; }
          }

          if (a > 5)
            riverOffset += (a * a * a * a * a - 15L * a * a * a * a + 85L * a * a * a - 225L * a * a + 274L * a - 120L) / 120L;
          if (b > 4)
            riverOffset += (b * b * b * b - 10L * b * b * b + 35L * b * b - 50L * b + 24L) / 24L;
          if (c > 3)
            riverOffset += (c * c * c - 6L * c * c + 11L * c - 6L) / 6L;
          if (d > 2)
            riverOffset += (d * d - 3L * d + 2L) / 2L;
          if (e > 1)
            riverOffset += e - 1;

          riverLocations[(int)riverOffset] = i * sizeInBytes;
          ushort[] riverI, riverWP;

          LoadWinRatioNew(cards[0], cards[1], cards[2], cards[3], cards[4], 10, out riverWP, out riverI, false);

          if (riverWP.Length != sizeInBytes)
            throw new Exception();

          for (long j = i * sizeInBytes / 2; j < (i + 1) * sizeInBytes / 2; j++)
          {
            riverWinPercentages[j] = riverWP[j - i * sizeInBytes / 2];
          }
        }

        #endregion

        #region turnSection

        {
          byte b = (byte)cards[0], c = (byte)cards[1], d = (byte)cards[2], e = (byte)cards[3];

          long turnOffset = 0;
          long sizeInBytes = 2 * 48 * 47 * 9 / 2;
          bool sorted = false;
          byte temp;

          while (!sorted)
          {
            sorted = true;

            if (b < c)
            { temp = b; b = c; c = temp; sorted = false; }
            if (c < d)
            { temp = c; c = d; d = temp; sorted = false; }
            if (d < e)
            { temp = d; d = e; e = temp; sorted = false; }
          }

          if (b > 4)
            turnOffset += (b * b * b * b - 10L * b * b * b + 35L * b * b - 50L * b + 24L) / 24L;
          if (c > 3)
            turnOffset += (c * c * c - 6L * c * c + 11L * c - 6L) / 6L;
          if (d > 2)
            turnOffset += (d * d - 3L * d + 2L) / 2L;
          if (e > 1)
            turnOffset += e - 1;

          turnLocations[(int)turnOffset] = i * sizeInBytes;
          ushort[] turnI, turnWP;

          LoadWinRatioNew(cards[0], cards[1], cards[2], cards[3], Card.NoCard, 10, out turnWP, out turnI, false);

          if (turnWP.Length != sizeInBytes)
            throw new Exception();

          for (long j = i * sizeInBytes / 2; j < (i + 1) * sizeInBytes / 2; j++)
          {
            turnWinPercentages[j] = turnWP[j - i * sizeInBytes / 2];
          }
        }

        #endregion

        #region flopSection

        {
          byte c = (byte)cards[0], d = (byte)cards[1], e = (byte)cards[2];

          long flopOffset = 0;
          long sizeInBytes = 2 * 49 * 48 * 9 / 2;
          bool sorted = false;
          byte temp;

          while (!sorted)
          {
            sorted = true;

            if (c < d)
            { temp = c; c = d; d = temp; sorted = false; }
            if (d < e)
            { temp = d; d = e; e = temp; sorted = false; }
          }

          if (c > 3)
            flopOffset += (c * c * c - 6L * c * c + 11L * c - 6L) / 6L;
          if (d > 2)
            flopOffset += (d * d - 3L * d + 2L) / 2L;
          if (e > 1)
            flopOffset += e - 1;

          flopLocations[(int)flopOffset] = i * sizeInBytes;
          ushort[] flopI, flopWP;

          LoadWinRatioNew(cards[0], cards[1], cards[2], Card.NoCard, Card.NoCard, 10, out flopWP, out flopI, false);

          if (flopWP.Length != sizeInBytes)
            throw new Exception();

          for (long j = i * sizeInBytes / 2; j < (i + 1) * sizeInBytes / 2; j++)
          {
            flopWinPercentages[j] = flopWP[j - i * sizeInBytes / 2];
          }
        }

        #endregion
      }

      WrapperForWinRatioData output = new WrapperForWinRatioData();
      output.flopLocations = flopLocations;
      output.flopWinPercentages = flopWinPercentages;
      output.turnLocations = turnLocations;
      output.turnWinPercentages = turnWinPercentages;
      output.riverLocations = riverLocations;
      output.riverWinPercentages = riverWinPercentages;
      output.preflopWP = new ushort[preflopWR.GetLength(0) * preflopWR.GetLength(1) * preflopWR.GetLength(2)];
      output.preflopRanks = new byte[preFlopRanks.GetLength(0) * preFlopRanks.GetLength(1) * preFlopRanks.GetLength(2)];

      for (int n = 0; n < 9; n++)
      {
        for (int i = 0; i < 52; i++)
        {
          for (int j = 0; j < 52; j++)
          {
            output.preflopWP[n * 52 * 52 + i * 52 + j] = preflopWR[n, i, j];
            output.preflopRanks[n * 52 * 52 + i * 52 + j] = preFlopRanks[n, i, j];
          }
        }
      }

      //serialisedBytes = Packing.FBPSerialiser.SerialiseDataObject(output, compressionLevel);
      //serialisedBytes = Packing.FBPSerialiser.CompressArray(output.getBytes(), compressionLevel);
      serialisedBytes = DPSManager.GetDataSerializer<NullSerializer>().SerialiseDataObject<byte[]>(output.getBytes(), new List<DataProcessor>() { winRatioCompressor }, new Dictionary<string, string>()).ThreadSafeStream.ToArray();

      output = null;
      return serialisedBytes;
    }

    /// <summary>
    /// Crazy struct for pointer cast see
    /// http://kristofverbiest.blogspot.com/2008/11/casting-array-of-value-types.html
    /// </summary>
    [StructLayout(LayoutKind.Explicit)]
    struct ByteToShortConverter
    {
      [FieldOffset(0)]
      public byte[] bytes;

      [FieldOffset(0)]
      public ushort[] ushorts;
    }


    public void Dispose()
    {
      Close();
    }


  }
}
