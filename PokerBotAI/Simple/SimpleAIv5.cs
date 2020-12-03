using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using PokerBot.Definitions;
using PokerBot.AI.InfoProviders;

namespace PokerBot.AI
{
  internal class SimpleAIv5 : AIBase
  {
    internal SimpleAIv5(AIRandomControl aiRandomControl)
        : base(aiRandomControl)
    {
      specificUpdateKey = new RequestedInfoKey();
      specificUpdateKey.SetInfoTypeRequired(InfoProviders.InfoType.WR_ModelAction);
      specificUpdateKey.SetInfoTypeRequired(InfoProviders.InfoType.WR_ModelActionAmount);
    }

    protected override RequestedInfoKey GetUpdateKeyOrPreDecision()
    {
      return specificUpdateKey;
    }

    protected override Definitions.Play GetDecision()
    {
      PokerAction action = (PokerAction)(infoStore.GetInfoValue(InfoProviders.InfoType.WR_ModelAction));
      decimal amount = (decimal)infoStore.GetInfoValue(InfoProviders.InfoType.WR_ModelActionAmount);
      float decisionTime = 0;

      return new Play(action, amount, decisionTime, currentDecision.Cache.getCurrentHandId(), currentDecision.PlayerId);
    }
  }
}
