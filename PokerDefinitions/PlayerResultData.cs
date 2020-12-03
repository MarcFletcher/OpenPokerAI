using System;
using System.Collections.Generic;
using System.Linq;
using ProtoBuf;
using PokerBot.Definitions;

namespace PokerBot.Definitions
{
  public enum ResultType : byte
  {
    Undefined = 0,
    General = 1,
    CheatingOpponents = 2,
  }

  [ProtoContract]
  public class CardUsage
  {
    public Card card1;
    public Card card2;

    [ProtoMember(1)]
    public int receivedCount = 0;
    [ProtoMember(2)]
    public int calledCount = 0;
    [ProtoMember(3)]
    public int raisedCount = 0;
    [ProtoMember(4)]
    public int checkedCount = 0;

    public double handValue;

    public double CalledPercentage
    {
      get
      {
        if (receivedCount == 0)
          return 0;
        else
          return (double)calledCount / (double)receivedCount;
      }
    }

    public double RaisedPercentage
    {
      get
      {
        if (receivedCount == 0)
          return 0;
        else
          return (double)raisedCount / (double)receivedCount;
      }
    }

    public double CheckedPercentage
    {
      get
      {
        if (receivedCount == 0)
          return 0;
        else
          return (double)checkedCount / (double)receivedCount;
      }
    }

    private CardUsage() { }

    public CardUsage(Card card1, Card card2, double handValue)
    {
      this.card1 = card1;
      this.card2 = card2;
      this.handValue = handValue;
    }

    public static Dictionary<Card, Dictionary<Card, CardUsage>> EmptyCardUsageResultDict()
    {
      #region Result List Setup
      Dictionary<Card, Dictionary<Card, CardUsage>> cardUsageResult = new Dictionary<Card, Dictionary<Card, CardUsage>>()
                {
                {
                Card.Clubs3, new Dictionary<Card, CardUsage>()
                {
                { Card.Clubs2, new CardUsage(Card.Clubs3, Card.Clubs2,11.522087097168)},
                { Card.Diamonds2, new CardUsage(Card.Clubs3, Card.Diamonds2,7.66460657119751)},
                { Card.Diamonds3, new CardUsage(Card.Clubs3, Card.Diamonds3,13.8094148635864)},
                }
                },
                {
                Card.Clubs4, new Dictionary<Card, CardUsage>()
                {
                { Card.Clubs2, new CardUsage(Card.Clubs4, Card.Clubs2,12.1141376495361)},
                { Card.Clubs3, new CardUsage(Card.Clubs4, Card.Clubs3,13.1136035919189)},
                { Card.Diamonds2, new CardUsage(Card.Clubs4, Card.Diamonds2,8.29785633087158)},
                { Card.Diamonds3, new CardUsage(Card.Clubs4, Card.Diamonds3,9.35835838317871)},
                { Card.Diamonds4, new CardUsage(Card.Clubs4, Card.Diamonds4,14.2885484695435)},
                }
                },
                {
                Card.Clubs5, new Dictionary<Card, CardUsage>()
                {
                { Card.Clubs2, new CardUsage(Card.Clubs5, Card.Clubs2,12.5307083129883)},
                { Card.Clubs3, new CardUsage(Card.Clubs5, Card.Clubs3,13.7300682067871)},
                { Card.Clubs4, new CardUsage(Card.Clubs5, Card.Clubs4,14.7463188171387)},
                { Card.Diamonds2, new CardUsage(Card.Clubs5, Card.Diamonds2,8.7312126159668)},
                { Card.Diamonds3, new CardUsage(Card.Clubs5, Card.Diamonds3,10.0358591079712)},
                { Card.Diamonds4, new CardUsage(Card.Clubs5, Card.Diamonds4,11.1238269805908)},
                { Card.Diamonds5, new CardUsage(Card.Clubs5, Card.Diamonds5,14.9156942367554)},
                }
                },
                {
                Card.Clubs6, new Dictionary<Card, CardUsage>()
                {
                { Card.Clubs2, new CardUsage(Card.Clubs6, Card.Clubs2,11.6380558013916)},
                { Card.Clubs3, new CardUsage(Card.Clubs6, Card.Clubs3,12.8496227264404)},
                { Card.Clubs4, new CardUsage(Card.Clubs6, Card.Clubs4,14.0718698501587)},
                { Card.Clubs5, new CardUsage(Card.Clubs6, Card.Clubs5,15.1018543243408)},
                { Card.Diamonds2, new CardUsage(Card.Clubs6, Card.Diamonds2,7.79278230667114)},
                { Card.Diamonds3, new CardUsage(Card.Clubs6, Card.Diamonds3,9.07606601715088)},
                { Card.Diamonds4, new CardUsage(Card.Clubs6, Card.Diamonds4,10.3868160247803)},
                { Card.Diamonds5, new CardUsage(Card.Clubs6, Card.Diamonds5,11.4991989135742)},
                { Card.Diamonds6, new CardUsage(Card.Clubs6, Card.Diamonds6,15.8617534637451)},
                }
                },
                {
                Card.Clubs7, new Dictionary<Card, CardUsage>()
                {
                { Card.Clubs2, new CardUsage(Card.Clubs7, Card.Clubs2,11.224536895752)},
                { Card.Clubs3, new CardUsage(Card.Clubs7, Card.Clubs3,12.2667274475098)},
                { Card.Clubs4, new CardUsage(Card.Clubs7, Card.Clubs4,13.4935531616211)},
                { Card.Clubs5, new CardUsage(Card.Clubs7, Card.Clubs5,14.7341117858887)},
                { Card.Clubs6, new CardUsage(Card.Clubs7, Card.Clubs6,15.6832227706909)},
                { Card.Diamonds2, new CardUsage(Card.Clubs7, Card.Diamonds2,7.29381227493286)},
                { Card.Diamonds3, new CardUsage(Card.Clubs7, Card.Diamonds3,8.3863582611084)},
                { Card.Diamonds4, new CardUsage(Card.Clubs7, Card.Diamonds4,9.71999740600586)},
                { Card.Diamonds5, new CardUsage(Card.Clubs7, Card.Diamonds5,11.0566873550415)},
                { Card.Diamonds6, new CardUsage(Card.Clubs7, Card.Diamonds6,12.0637826919556)},
                { Card.Diamonds7, new CardUsage(Card.Clubs7, Card.Diamonds7,16.8398571014404)},
                }
                },
                {
                Card.Clubs8, new Dictionary<Card, CardUsage>()
                {
                { Card.Clubs2, new CardUsage(Card.Clubs8, Card.Clubs2,11.5907526016235)},
                { Card.Clubs3, new CardUsage(Card.Clubs8, Card.Clubs3,11.8852519989014)},
                { Card.Clubs4, new CardUsage(Card.Clubs8, Card.Clubs4,12.967116355896)},
                { Card.Clubs5, new CardUsage(Card.Clubs8, Card.Clubs5,14.2366676330566)},
                { Card.Clubs6, new CardUsage(Card.Clubs8, Card.Clubs6,15.3887233734131)},
                { Card.Clubs7, new CardUsage(Card.Clubs8, Card.Clubs7,16.4003963470459)},
                { Card.Diamonds2, new CardUsage(Card.Clubs8, Card.Diamonds2,7.61577796936035)},
                { Card.Diamonds3, new CardUsage(Card.Clubs8, Card.Diamonds3,7.97131299972534)},
                { Card.Diamonds4, new CardUsage(Card.Clubs8, Card.Diamonds4,9.1065845489502)},
                { Card.Diamonds5, new CardUsage(Card.Clubs8, Card.Diamonds5,10.4844741821289)},
                { Card.Diamonds6, new CardUsage(Card.Clubs8, Card.Diamonds6,11.6899366378784)},
                { Card.Diamonds7, new CardUsage(Card.Clubs8, Card.Diamonds7,12.7962160110474)},
                { Card.Diamonds8, new CardUsage(Card.Clubs8, Card.Diamonds8,18.1857013702393)},
                }
                },
                {
                Card.Clubs9, new Dictionary<Card, CardUsage>()
                {
                { Card.Clubs2, new CardUsage(Card.Clubs9, Card.Clubs2,12.0531015396118)},
                { Card.Clubs3, new CardUsage(Card.Clubs9, Card.Clubs3,12.3826961517334)},
                { Card.Clubs4, new CardUsage(Card.Clubs9, Card.Clubs4,12.7031354904175)},
                { Card.Clubs5, new CardUsage(Card.Clubs9, Card.Clubs5,13.8063631057739)},
                { Card.Clubs6, new CardUsage(Card.Clubs9, Card.Clubs6,14.9843597412109)},
                { Card.Clubs7, new CardUsage(Card.Clubs9, Card.Clubs7,16.1989784240723)},
                { Card.Clubs8, new CardUsage(Card.Clubs9, Card.Clubs8,17.3037300109863)},
                { Card.Diamonds2, new CardUsage(Card.Clubs9, Card.Diamonds2,8.06744480133057)},
                { Card.Diamonds3, new CardUsage(Card.Clubs9, Card.Diamonds3,8.3894100189209)},
                { Card.Diamonds4, new CardUsage(Card.Clubs9, Card.Diamonds4,8.7495231628418)},
                { Card.Diamonds5, new CardUsage(Card.Clubs9, Card.Diamonds5,9.96261501312256)},
                { Card.Diamonds6, new CardUsage(Card.Clubs9, Card.Diamonds6,11.2275886535645)},
                { Card.Diamonds7, new CardUsage(Card.Clubs9, Card.Diamonds7,12.5368127822876)},
                { Card.Diamonds8, new CardUsage(Card.Clubs9, Card.Diamonds8,13.7178602218628)},
                { Card.Diamonds9, new CardUsage(Card.Clubs9, Card.Diamonds9,19.8641948699951)},
                }
                },
                {
                Card.Clubs10, new Dictionary<Card, CardUsage>()
                {
                { Card.Clubs2, new CardUsage(Card.Clubs10, Card.Clubs2,12.9900054931641)},
                { Card.Clubs3, new CardUsage(Card.Clubs10, Card.Clubs3,13.3043413162231)},
                { Card.Clubs4, new CardUsage(Card.Clubs10, Card.Clubs4,13.629358291626)},
                { Card.Clubs5, new CardUsage(Card.Clubs10, Card.Clubs5,13.9864196777344)},
                { Card.Clubs6, new CardUsage(Card.Clubs10, Card.Clubs6,15.037766456604)},
                { Card.Clubs7, new CardUsage(Card.Clubs10, Card.Clubs7,16.2996864318848)},
                { Card.Clubs8, new CardUsage(Card.Clubs10, Card.Clubs8,17.6897850036621)},
                { Card.Clubs9, new CardUsage(Card.Clubs10, Card.Clubs9,19.1638050079346)},
                { Card.Diamonds2, new CardUsage(Card.Clubs10, Card.Diamonds2,8.92652797698975)},
                { Card.Diamonds3, new CardUsage(Card.Clubs10, Card.Diamonds3,9.26832962036133)},
                { Card.Diamonds4, new CardUsage(Card.Clubs10, Card.Diamonds4,9.66964244842529)},
                { Card.Diamonds5, new CardUsage(Card.Clubs10, Card.Diamonds5,10.0694284439087)},
                { Card.Diamonds6, new CardUsage(Card.Clubs10, Card.Diamonds6,11.1879148483276)},
                { Card.Diamonds7, new CardUsage(Card.Clubs10, Card.Diamonds7,12.5352869033813)},
                { Card.Diamonds8, new CardUsage(Card.Clubs10, Card.Diamonds8,14.0733957290649)},
                { Card.Diamonds9, new CardUsage(Card.Clubs10, Card.Diamonds9,15.6923780441284)},
                { Card.Diamonds10, new CardUsage(Card.Clubs10, Card.Diamonds10,22.3392086029053)},
                }
                },
                {
                Card.ClubsJ, new Dictionary<Card, CardUsage>()
                {
                { Card.Clubs2, new CardUsage(Card.ClubsJ, Card.Clubs2,13.6125736236572)},
                { Card.Clubs3, new CardUsage(Card.ClubsJ, Card.Clubs3,13.9131765365601)},
                { Card.Clubs4, new CardUsage(Card.ClubsJ, Card.Clubs4,14.2809181213379)},
                { Card.Clubs5, new CardUsage(Card.ClubsJ, Card.Clubs5,14.6639204025269)},
                { Card.Clubs6, new CardUsage(Card.ClubsJ, Card.Clubs6,14.9202718734741)},
                { Card.Clubs7, new CardUsage(Card.ClubsJ, Card.Clubs7,16.0738544464111)},
                { Card.Clubs8, new CardUsage(Card.ClubsJ, Card.Clubs8,17.4151210784912)},
                { Card.Clubs9, new CardUsage(Card.ClubsJ, Card.Clubs9,18.9562835693359)},
                { Card.Clubs10, new CardUsage(Card.ClubsJ, Card.Clubs10,21.2344551086426)},
                { Card.Diamonds2, new CardUsage(Card.ClubsJ, Card.Diamonds2,9.47280120849609)},
                { Card.Diamonds3, new CardUsage(Card.ClubsJ, Card.Diamonds3,9.81765460968018)},
                { Card.Diamonds4, new CardUsage(Card.ClubsJ, Card.Diamonds4,10.2143888473511)},
                { Card.Diamonds5, new CardUsage(Card.ClubsJ, Card.Diamonds5,10.6416416168213)},
                { Card.Diamonds6, new CardUsage(Card.ClubsJ, Card.Diamonds6,10.9452962875366)},
                { Card.Diamonds7, new CardUsage(Card.ClubsJ, Card.Diamonds7,12.1843290328979)},
                { Card.Diamonds8, new CardUsage(Card.ClubsJ, Card.Diamonds8,13.6903944015503)},
                { Card.Diamonds9, new CardUsage(Card.ClubsJ, Card.Diamonds9,15.329213142395)},
                { Card.Diamonds10, new CardUsage(Card.ClubsJ, Card.Diamonds10,17.837797164917)},
                { Card.DiamondsJ, new CardUsage(Card.ClubsJ, Card.DiamondsJ,25.0995655059814)},
                }
                },
                {
                Card.ClubsQ, new Dictionary<Card, CardUsage>()
                {
                { Card.Clubs2, new CardUsage(Card.ClubsQ, Card.Clubs2,14.4914932250977)},
                { Card.Clubs3, new CardUsage(Card.ClubsQ, Card.Clubs3,14.8409242630005)},
                { Card.Clubs4, new CardUsage(Card.ClubsQ, Card.Clubs4,15.1918821334839)},
                { Card.Clubs5, new CardUsage(Card.ClubsQ, Card.Clubs5,15.547417640686)},
                { Card.Clubs6, new CardUsage(Card.ClubsQ, Card.Clubs6,15.8480205535889)},
                { Card.Clubs7, new CardUsage(Card.ClubsQ, Card.Clubs7,16.240177154541)},
                { Card.Clubs8, new CardUsage(Card.ClubsQ, Card.Clubs8,17.4929428100586)},
                { Card.Clubs9, new CardUsage(Card.ClubsQ, Card.Clubs9,19.0386810302734)},
                { Card.Clubs10, new CardUsage(Card.ClubsQ, Card.Clubs10,21.3763637542725)},
                { Card.ClubsJ, new CardUsage(Card.ClubsQ, Card.ClubsJ,22.0965900421143)},
                { Card.Diamonds2, new CardUsage(Card.ClubsQ, Card.Diamonds2,10.2601661682129)},
                { Card.Diamonds3, new CardUsage(Card.ClubsQ, Card.Diamonds3,10.62180519104)},
                { Card.Diamonds4, new CardUsage(Card.ClubsQ, Card.Diamonds4,11.0200653076172)},
                { Card.Diamonds5, new CardUsage(Card.ClubsQ, Card.Diamonds5,11.4625768661499)},
                { Card.Diamonds6, new CardUsage(Card.ClubsQ, Card.Diamonds6,11.7952241897583)},
                { Card.Diamonds7, new CardUsage(Card.ClubsQ, Card.Diamonds7,12.2270545959473)},
                { Card.Diamonds8, new CardUsage(Card.ClubsQ, Card.Diamonds8,13.6079959869385)},
                { Card.Diamonds9, new CardUsage(Card.ClubsQ, Card.Diamonds9,15.251392364502)},
                { Card.Diamonds10, new CardUsage(Card.ClubsQ, Card.Diamonds10,17.819486618042)},
                { Card.DiamondsJ, new CardUsage(Card.ClubsQ, Card.DiamondsJ,18.6663608551025)},
                { Card.DiamondsQ, new CardUsage(Card.ClubsQ, Card.DiamondsQ,28.7129020690918)},
                }
                },
                {
                Card.ClubsK, new Dictionary<Card, CardUsage>()
                {
                { Card.Clubs2, new CardUsage(Card.ClubsK, Card.Clubs2,15.724422454834)},
                { Card.Clubs3, new CardUsage(Card.ClubsK, Card.Clubs3,16.0631732940674)},
                { Card.Clubs4, new CardUsage(Card.ClubsK, Card.Clubs4,16.4202327728271)},
                { Card.Clubs5, new CardUsage(Card.ClubsK, Card.Clubs5,16.8383312225342)},
                { Card.Clubs6, new CardUsage(Card.ClubsK, Card.Clubs6,17.1663990020752)},
                { Card.Clubs7, new CardUsage(Card.ClubsK, Card.Clubs7,17.576868057251)},
                { Card.Clubs8, new CardUsage(Card.ClubsK, Card.Clubs8,18.0346374511719)},
                { Card.Clubs9, new CardUsage(Card.ClubsK, Card.Clubs9,19.5361251831055)},
                { Card.Clubs10, new CardUsage(Card.ClubsK, Card.Clubs10,21.858549118042)},
                { Card.ClubsJ, new CardUsage(Card.ClubsK, Card.ClubsJ,22.6367588043213)},
                { Card.ClubsQ, new CardUsage(Card.ClubsK, Card.ClubsQ,23.7155723571777)},
                { Card.Diamonds2, new CardUsage(Card.ClubsK, Card.Diamonds2,11.4290075302124)},
                { Card.Diamonds3, new CardUsage(Card.ClubsK, Card.Diamonds3,11.7814912796021)},
                { Card.Diamonds4, new CardUsage(Card.ClubsK, Card.Diamonds4,12.2026395797729)},
                { Card.Diamonds5, new CardUsage(Card.ClubsK, Card.Diamonds5,12.6451511383057)},
                { Card.Diamonds6, new CardUsage(Card.ClubsK, Card.Diamonds6,13.0189971923828)},
                { Card.Diamonds7, new CardUsage(Card.ClubsK, Card.Diamonds7,13.4782943725586)},
                { Card.Diamonds8, new CardUsage(Card.ClubsK, Card.Diamonds8,14.0032043457031)},
                { Card.Diamonds9, new CardUsage(Card.ClubsK, Card.Diamonds9,15.6145572662354)},
                { Card.Diamonds10, new CardUsage(Card.ClubsK, Card.Diamonds10,18.1628131866455)},
                { Card.DiamondsJ, new CardUsage(Card.ClubsK, Card.DiamondsJ,19.0859851837158)},
                { Card.DiamondsQ, new CardUsage(Card.ClubsK, Card.DiamondsQ,20.3295955657959)},
                { Card.DiamondsK, new CardUsage(Card.ClubsK, Card.DiamondsK,33.2417793273926)},
                }
                },
                {
                Card.ClubsA, new Dictionary<Card, CardUsage>()
                {
                { Card.Clubs2, new CardUsage(Card.ClubsA, Card.Clubs2,17.8973064422607)},
                { Card.Clubs3, new CardUsage(Card.ClubsA, Card.Clubs3,18.4191646575928)},
                { Card.Clubs4, new CardUsage(Card.ClubsA, Card.Clubs4,18.8738842010498)},
                { Card.Clubs5, new CardUsage(Card.ClubsA, Card.Clubs5,19.2889289855957)},
                { Card.Clubs6, new CardUsage(Card.ClubsA, Card.Clubs6,18.6114292144775)},
                { Card.Clubs7, new CardUsage(Card.ClubsA, Card.Clubs7,19.1088733673096)},
                { Card.Clubs8, new CardUsage(Card.ClubsA, Card.Clubs8,19.6658267974854)},
                { Card.Clubs9, new CardUsage(Card.ClubsA, Card.Clubs9,20.2899208068848)},
                { Card.Clubs10, new CardUsage(Card.ClubsA, Card.Clubs10,22.5497817993164)},
                { Card.ClubsJ, new CardUsage(Card.ClubsA, Card.ClubsJ,23.4012355804443)},
                { Card.ClubsQ, new CardUsage(Card.ClubsA, Card.ClubsQ,24.518196105957)},
                { Card.ClubsK, new CardUsage(Card.ClubsA, Card.ClubsK,26.0013732910156)},
                { Card.Diamonds2, new CardUsage(Card.ClubsA, Card.Diamonds2,13.5545892715454)},
                { Card.Diamonds3, new CardUsage(Card.ClubsA, Card.Diamonds3,14.1405353546143)},
                { Card.Diamonds4, new CardUsage(Card.ClubsA, Card.Diamonds4,14.63645362854)},
                { Card.Diamonds5, new CardUsage(Card.ClubsA, Card.Diamonds5,15.1049060821533)},
                { Card.Diamonds6, new CardUsage(Card.ClubsA, Card.Diamonds6,14.3862056732178)},
                { Card.Diamonds7, new CardUsage(Card.ClubsA, Card.Diamonds7,14.9324789047241)},
                { Card.Diamonds8, new CardUsage(Card.ClubsA, Card.Diamonds8,15.5580987930298)},
                { Card.Diamonds9, new CardUsage(Card.ClubsA, Card.Diamonds9,16.270694732666)},
                { Card.Diamonds10, new CardUsage(Card.ClubsA, Card.Diamonds10,18.7396049499512)},
                { Card.DiamondsJ, new CardUsage(Card.ClubsA, Card.DiamondsJ,19.6978721618652)},
                { Card.DiamondsQ, new CardUsage(Card.ClubsA, Card.DiamondsQ,20.9704742431641)},
                { Card.DiamondsK, new CardUsage(Card.ClubsA, Card.DiamondsK,22.6916923522949)},
                { Card.DiamondsA, new CardUsage(Card.ClubsA, Card.DiamondsA,39.0234222412109)},
                }
                },
                {
                Card.Clubs2, new Dictionary<Card, CardUsage>()
                {
                { Card.Diamonds2, new CardUsage(Card.Clubs2, Card.Diamonds2,13.5271224975586)},
                }
                },
                };
      #endregion
      return cardUsageResult;
    }

    public static List<CardUsage> EmptyCardUsageResultList()
    {
      List<CardUsage> result = new List<CardUsage>();
      var resultDict = EmptyCardUsageResultDict();

      foreach (KeyValuePair<Card, Dictionary<Card, CardUsage>> index1 in resultDict)
        result.AddRange(index1.Value.Values);

      return (from current in result orderby current.handValue descending, current.card1 descending select current).ToList();
    }

    public override string ToString()
    {
      return card1 + ", " + card2 + " - ch%:" + CheckedPercentage + " c%:" + CalledPercentage + " r%:" + RaisedPercentage;
    }
  }

  [ProtoContract]
  public class PlayerResultData
  {
    [ProtoMember(22)]
    public ResultType ResultType;

    [ProtoMember(1)]
    public long playerId;
    [ProtoMember(2)]
    public int numHandsInResult;
    [ProtoMember(3)]
    private int avgWinPerHand;
    [ProtoMember(4)]
    public int sliceIndex;

    //We now want to store aggression information in here
    [ProtoMember(5)]
    private ushort rFreq_PreFlop = 0;
    [ProtoMember(6)]
    private ushort rFreq_PostFlop = 0;
    [ProtoMember(7)]
    private ushort cFreq_PreFlop = 0;
    [ProtoMember(8)]
    private ushort cFreq_PostFlop = 0;
    [ProtoMember(9)]
    private ushort checkFreq_PreFlop = 0;
    [ProtoMember(10)]
    private ushort checkFreq_PostFlop = 0;
    [ProtoMember(11)]
    private ushort preFlopPlayFreq = 0;
    [ProtoMember(12)]
    private ushort postFlopPlayFreq = 0;

    [ProtoMember(13)]
    public CardUsage[] cardUsage;

    [ProtoMember(14)]
    public ushort[] PreFlopAdditionalRaiseBinsSmall { get; private set; }
    [ProtoMember(15)]
    public ushort[] FlopAdditionalRaiseBinsSmall { get; private set; }
    [ProtoMember(16)]
    public ushort[] TurnAdditionalRaiseBinsSmall { get; private set; }
    [ProtoMember(17)]
    public ushort[] RiverAdditionalRaiseBinsSmall { get; private set; }

    [ProtoMember(18)]
    public ushort[] PreFlopAdditionalRaiseBinsBig { get; private set; }
    [ProtoMember(19)]
    public ushort[] FlopAdditionalRaiseBinsBig { get; private set; }
    [ProtoMember(20)]
    public ushort[] TurnAdditionalRaiseBinsBig { get; private set; }
    [ProtoMember(21)]
    public ushort[] RiverAdditionalRaiseBinsBig { get; private set; }

    private PlayerResultData() { }

    public PlayerResultData(long playerId, int resultNumHandSize, decimal avgWinPerHand, int playerPlaySliceIndex,
        decimal RFreq_PreFlop,
        decimal RFreq_PostFlop,
        decimal CFreq_PreFlop,
        decimal CFreq_PostFlop,
        decimal CheckFreq_PreFlop,
        decimal CheckFreq_PostFlop,
        decimal PreFlopPlayFreq,
        decimal PostFlopPlayFreq,
        CardUsage[] cardUsage,
        ushort[] preFlopAdditionalRaiseBinsSmall,
        ushort[] flopAdditionalRaiseBinsSmall,
        ushort[] turnAdditionalRaiseBinsSmall,
        ushort[] riverAdditionalRaiseBinsSmall,
        ushort[] preFlopAdditionalRaiseBinsBig,
        ushort[] flopAdditionalRaiseBinsBig,
        ushort[] turnAdditionalRaiseBinsBig,
        ushort[] riverAdditionalRaiseBinsBig)
    {
      this.playerId = playerId;
      this.numHandsInResult = resultNumHandSize;

      //Convert from decimal to int for storage
      //Anything from -100 to 100 will be converted succesfully
      //This should be more than enough for avgWinPerHand
      if (avgWinPerHand > 100 || avgWinPerHand < -100)
        throw new Exception("avgWinPerHand must be between -100 and 100 to be correctly stored.");

      this.avgWinPerHand = (int)(avgWinPerHand * 1E7m);

      this.sliceIndex = playerPlaySliceIndex;

      //These values will always be less than 1
      this.rFreq_PreFlop = (ushort)(RFreq_PreFlop * ushort.MaxValue);
      this.rFreq_PostFlop = (ushort)(RFreq_PostFlop * ushort.MaxValue);
      this.cFreq_PreFlop = (ushort)(CFreq_PreFlop * ushort.MaxValue);
      this.cFreq_PostFlop = (ushort)(CFreq_PostFlop * ushort.MaxValue);
      this.checkFreq_PreFlop = (ushort)(CheckFreq_PreFlop * ushort.MaxValue);
      this.checkFreq_PostFlop = (ushort)(CheckFreq_PostFlop * ushort.MaxValue);
      this.preFlopPlayFreq = (ushort)(PreFlopPlayFreq * ushort.MaxValue);
      this.postFlopPlayFreq = (ushort)(PostFlopPlayFreq * ushort.MaxValue);

      this.cardUsage = cardUsage;

      //Small pots
      this.PreFlopAdditionalRaiseBinsSmall = preFlopAdditionalRaiseBinsSmall;
      this.FlopAdditionalRaiseBinsSmall = flopAdditionalRaiseBinsSmall;
      this.TurnAdditionalRaiseBinsSmall = turnAdditionalRaiseBinsSmall;
      this.RiverAdditionalRaiseBinsSmall = riverAdditionalRaiseBinsSmall;

      //BigPots
      this.PreFlopAdditionalRaiseBinsBig = preFlopAdditionalRaiseBinsBig;
      this.FlopAdditionalRaiseBinsBig = flopAdditionalRaiseBinsBig;
      this.TurnAdditionalRaiseBinsBig = turnAdditionalRaiseBinsBig;
      this.RiverAdditionalRaiseBinsBig = riverAdditionalRaiseBinsBig;
    }

    public override string ToString()
    {
      return "PlayerId:" + playerId + " - AvgWinPerHand:" + AvgWinPerHand;
    }

    #region Get
    public decimal AvgWinPerHand
    {
      get
      {
        //this.avgWinPerHand = (int)(avgWinPerHand * 1E7m);
        return (decimal)avgWinPerHand / 1E7m;
      }
    }

    public int NumHandsInResults
    {
      get { return numHandsInResult; }
    }

    public decimal RFreq_PreFlop { get { return (decimal)rFreq_PreFlop / (decimal)ushort.MaxValue; } }
    public decimal RFreq_PostFlop { get { return (decimal)rFreq_PostFlop / (decimal)ushort.MaxValue; } }
    public decimal CFreq_PreFlop { get { return (decimal)cFreq_PreFlop / (decimal)ushort.MaxValue; } }
    public decimal CFreq_PostFlop { get { return (decimal)cFreq_PostFlop / (decimal)ushort.MaxValue; } }
    public decimal CheckFreq_PreFlop { get { return (decimal)checkFreq_PreFlop / (decimal)ushort.MaxValue; } }
    public decimal CheckFreq_PostFlop { get { return (decimal)checkFreq_PostFlop / (decimal)ushort.MaxValue; } }
    public decimal PreFlopPlayFreq { get { return (decimal)preFlopPlayFreq / (decimal)ushort.MaxValue; } }
    public decimal PostFlopPlayFreq { get { return (decimal)postFlopPlayFreq / (decimal)ushort.MaxValue; } }

    public decimal PreFlopAggression { get { return RFreq_PreFlop / (CFreq_PreFlop + CheckFreq_PreFlop); } }
    public decimal PostFlopAggression { get { return RFreq_PostFlop / (CFreq_PostFlop + CheckFreq_PostFlop); } }
    #endregion
  }
}
