using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using PokerBot.Definitions;
using PokerBot.AI.InfoProviders;

namespace PokerBot.AI
{
  internal class SimpleAIv6 : AIBase
  {
    internal SimpleAIv6(AIRandomControl aiRandomControl)
        : base(aiRandomControl)
    {
      specificUpdateKey = new RequestedInfoKey();
      specificUpdateKey.SetInfoTypeRequired(InfoProviders.InfoType.WR_ProbOpponentHasBetterWR);
    }

    protected override RequestedInfoKey GetUpdateKeyOrPreDecision()
    {
      return specificUpdateKey;
    }

    protected override Definitions.Play GetDecision()
    {
      string probBeatString = infoStore.GetInfoValue(InfoProviders.InfoType.WR_ProbOpponentHasBetterWR).ToString();

      return new Play(PokerAction.Fold, 0, 0, currentDecision.Cache.getCurrentHandId(), currentDecision.PlayerId, probBeatString, 0);
    }
  }
}
