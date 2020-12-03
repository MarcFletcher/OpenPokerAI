using System.Collections.Generic;
using System.Threading;
using PokerBot.AI.InfoProviders;
using PokerBot.Database;
using PokerBot.Definitions;

namespace PokerBot.AI
{
  //Need an object which is stored in the decisionRequestQueue
  public class DecisionRequest
  {
    ManualResetEvent decisionSignal;

    //The following objects are assigned when the decision request is created.
    long playerId;
    databaseCache cache;
    AIGeneration serverType = AIGeneration.Undefined;
    string aiConfigStr = null;
    Play decisionPlay = null;

    RequestedInfoKey requiredInfo;
    Dictionary<InfoType, string> infoUpdateConfigs;

    bool aiManagerInSafeMode;

    public DecisionRequest(long playerId, databaseCache cache, AIGeneration serverType, string aiConfigStr, bool aiManagerInSafeMode)
    {
      this.playerId = playerId;
      this.cache = cache;
      this.serverType = serverType;
      this.aiConfigStr = aiConfigStr;
      this.aiManagerInSafeMode = aiManagerInSafeMode;

      this.decisionSignal = new ManualResetEvent(false);
    }

    /// <summary>
    /// Wait for the decision to be set.
    /// </summary>
    /// <param name="timeOutMilliSeconds"></param>
    /// <returns></returns>
    public bool WaitForDecision()
    {
      return decisionSignal.WaitOne();
    }

    /// <summary>
    /// Set the calculated decision.
    /// </summary>
    /// <param name="decisionPlay"></param>
    public void SetDecision(Play decisionPlay)
    {
      this.decisionPlay = decisionPlay;
      decisionSignal.Set();
    }

    #region Get & Sets

    public long PlayerId
    {
      get { return playerId; }
    }

    public databaseCache Cache
    {
      get { return cache; }
    }

    public AIGeneration ServerType
    {
      get { return serverType; }
    }

    public string AiConfigStr
    {
      get { return aiConfigStr; }
    }

    public Play DecisionPlay
    {
      get { return decisionPlay; }
    }

    public bool AIManagerInSafeMode
    {
      get { return aiManagerInSafeMode; }
    }

    public RequestedInfoKey RequiredInfoTypeUpdateKey
    {
      get { return requiredInfo; }
      set { requiredInfo = value; }
    }

    public Dictionary<InfoType, string> RequiredInfoTypeUpdateConfigs
    {
      get { return infoUpdateConfigs; }
      set { infoUpdateConfigs = value; }
    }

    #endregion
  }
}
