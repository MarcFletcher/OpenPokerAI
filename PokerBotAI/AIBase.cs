using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using PokerBot.Definitions;
using PokerBot.Database;
using System.Threading;
using System.Windows.Forms;
using System.Diagnostics;
using PokerBot.AI.InfoProviders;

namespace PokerBot.AI
{
  internal abstract class AIBase
  {
    //protected databaseCache cache;
    //protected long playerId;
    //protected string aiConfigStr;

    protected DecisionRequest currentDecision;

    protected InfoCollection infoStore = new InfoCollection();

    protected static AIGeneration aiType;

    protected Random randomGen;
    AIRandomControl aiRandomControl;

    protected object locker = new object();

    protected static string AI_FILES_STORE = Environment.GetEnvironmentVariable("PlayerNetworkStoreDir");

    public static AIGeneration AiType
    {
      get { return aiType; }
    }

    protected string decisionLogStr = "";

    protected Dictionary<InfoType, string> defaultInfoTypeUpdateConfigs;

    private RequestedInfoKey defaultUpdateKey;
    protected RequestedInfoKey specificUpdateKey;

    public AIBase(AIRandomControl aiRandomControl)
    {
      //this.disableStochasticChoice = disableStochasticChoice;
      this.aiRandomControl = aiRandomControl;
      defaultInfoTypeUpdateConfigs = new Dictionary<InfoType, string>();

      if (aiRandomControl.DecisionRandomPerHandSeedEnabled)
        randomGen = new CMWCRandom(aiRandomControl.DecisionRandomPerHandSeed);
      else
        randomGen = new CMWCRandom();

      //This set's all info types required to true and disables the few which are never used
      defaultUpdateKey = new RequestedInfoKey(true);
      defaultUpdateKey.SetInfoTypeRequired(InfoType.IO_ImpliedPotOdds_Double, false);
      defaultUpdateKey.SetInfoTypeRequired(InfoType.WR_CardsOnlyWeightedPercentage, false);
      defaultUpdateKey.SetInfoTypeRequired(InfoType.WR_CardsOnlyWeightedOpponentWinPercentage, false);
      defaultUpdateKey.SetInfoTypeRequired(InfoType.PAP_RaiseToBotRaise_Prob, false);
      defaultUpdateKey.SetInfoTypeRequired(InfoType.WR_AveragePercievedProbBotHasBetterHand, false);
    }

    #region decisionMaking

    public virtual void PrepareAIForDecision(DecisionRequest currentDecision)
    {
      this.currentDecision = currentDecision;
      this.currentDecision.RequiredInfoTypeUpdateKey = GetUpdateKeyOrPreDecision();
      this.currentDecision.RequiredInfoTypeUpdateConfigs = GetInfoUpdateConfigs();
    }

    protected virtual Dictionary<InfoType, string> GetInfoUpdateConfigs()
    {
      return defaultInfoTypeUpdateConfigs;
    }

    protected virtual RequestedInfoKey GetUpdateKeyOrPreDecision()
    {
      return defaultUpdateKey;
    }

    /// <summary>
    /// All AI's which inherit off this base must implement their own GetDecision method.
    /// </summary>
    /// <returns></returns>
    protected abstract Play GetDecision();

    public Play GetDecision(DecisionRequest currentDecision, InfoCollection infoStore)
    {
      //this.playerId = playerId;
      //this.aiConfigStr = aiConfigStr;
      //this.cache = genericGameCache;
      this.currentDecision = currentDecision;
      this.infoStore = infoStore;

      //Set the random seed value here
      if (aiRandomControl.DecisionRandomPerHandSeedEnabled)
      {
        //decimal distanceToDealer = (infoStore.GetInfoValue(InfoType.GP_DealerDistance_Byte) - 1) / (infoStore.GetInfoValue(InfoType.GP_NumActivePlayers_Byte) - 1);
        //decimal currentWinPercentage = infoStore.GetInfoValue(InfoType.WR_CardsOnlyWinPercentage);
        //decimal currentPotValue = infoStore.GetInfoValue(InfoType.BP_TotalPotAmount_Decimal);

        var hashFunc = new Func<long, long>((long key) =>
        {
          key = (~key) + (key << 21); // key = (key << 21) - key - 1;
          key = key ^ (key >> 24);
          key = (key + (key << 3)) + (key << 8); // key * 265
          key = key ^ (key >> 14);
          key = (key + (key << 2)) + (key << 4); // key * 21
          key = key ^ (key >> 28);
          key = key + (key << 31);
          return key;
        });

        //long randomSeed = hashFunc((long)(7919 * (distanceToDealer + 1))) ^
        //    hashFunc((long)(currentWinPercentage * 100)) ^
        //    hashFunc((long)(currentPotValue * 991)) ^
        //    hashFunc(1 + currentDecision.Cache.getNumHandsPlayed());

        //(randomGen as CMWCRandom).ReSeed(randomSeed);

        //We can now seed based on the table and hand random number
        (randomGen as CMWCRandom).ReSeed((long)(currentDecision.Cache.TableRandomNumber ^ hashFunc(currentDecision.Cache.CurrentHandRandomNumber()) ^ hashFunc(1 + currentDecision.Cache.getCurrentHandSeqIndex())));
      }

      //Validate the decision and then set the decision time if currently 0.
      return GetDecision();
    }

    #endregion decisionMaking
  }
}
