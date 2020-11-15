using System;
using ProtoBuf;

namespace PokerBot.Definitions
{
    /// <summary>
    /// Describes the desired poker play to be performed
    /// </summary>
    [ProtoContract]
    public class Play
    {
        [ProtoMember(1)]
        private PokerAction action;
        [ProtoMember(2)]
        private decimal amount;
        [ProtoMember(3)]
        private float decisionTime;
        [ProtoMember(4)]
        private long handId;
        [ProtoMember(5)]
        private long playerId;

        [ProtoMember(6)]
        private string aiDecisionStr;
        [ProtoMember(7)]
        private short aiDecisionStrType;

        /// <summary>
        /// Gets the turn action to be performed
        /// </summary>
        public PokerAction Action
        {
            get { return action; }
            set { action = value; }
        }

        /// <summary>
        /// Gets the handId for this decision
        /// </summary>
        public long HandId
        {
            get { return handId; }
        }

        /// <summary>
        /// Gets the playerId for this decision
        /// </summary>
        public long PlayerId
        {
            get { return playerId; }
        }

        /// <summary>
        /// Gets the amount to raise by if that is the desired action.
        /// </summary>
        public decimal Amount
        {
            get { return amount; }
            set { amount = value; }
        }

        /// <summary>
        /// Gets the elapsed time in seconds to allow before implementing the decision.
        /// </summary>
        public float DecisionTime
        {
            get { return decisionTime; }
            set { decisionTime = value; }
        }

        public string AiDecisionStr
        {
            get
            {
                return aiDecisionStr;
            }
        }

        public short AIDecisionStrType
        {
            get { return aiDecisionStrType; }
        }

        private Play() { }

        /// <summary>
        /// Contructor for Play struct
        /// </summary>
        /// <param name="action">The action to be performed</param>
        /// <param name="amount">The amount to raise by if that is the desired action</param>
        /// <param name="decisionTime">The elapsed time to allow before implementing the decision</param>
        /// <param name="handId">The handId for which this decision applies</param>
        /// <param name="playerId">The playerId for which this decision applies</param>
        public Play(PokerAction action, decimal amount, float decisionTime, long handId, long playerId)
        {
            this.action = action; 
            this.amount = amount; 
            this.decisionTime = decisionTime;
            this.handId = handId;
            this.playerId = playerId;
        }

        public Play(PokerAction action, decimal amount, float decisionTime, long handId, long playerId, string aiDecisionStr, short aiDecisionStrType)
        {
            this.action = action;
            this.amount = amount;
            this.decisionTime = decisionTime;
            this.handId = handId;
            this.playerId = playerId;
            this.aiDecisionStrType = aiDecisionStrType;
            this.aiDecisionStr = aiDecisionStr;
        }

        public Play(long handId, long playerId)
        {
            this.action = PokerAction.CatastrophicError;
            this.amount = 0;
            this.decisionTime = 0;
            this.handId = handId;
            this.playerId = playerId;
        }

        public override string ToString()
        {
            return (Enum.GetName(typeof(PokerAction), action) + " " + amount.ToString() + " to be performed after " + decisionTime.ToString("0.##") + "s");
        }
    }
}
