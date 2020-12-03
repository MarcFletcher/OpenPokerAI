using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace PokerBot.AI
{
    class AIHandler : IPokerAI
    {
        /// <summary>
        /// Returns the play struct after having queried all included AI modules having done something clever
        /// to average over what the various modules say
        /// </summary>
        /// <param name="state"></param>
        /// <returns></returns>
        public PokerBot.Definitions.Play GetDecision(PokerBot.Definitions.TableState state)
        {
            throw new NotImplementedException();
        }

    }
}
