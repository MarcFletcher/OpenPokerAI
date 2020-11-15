using ProtoBuf;

namespace PokerBot.Definitions
{
    [ProtoContract]
    public class PokerPlayer
    {
        [ProtoMember(1)]
        public long PlayerId { get; private set; }
        [ProtoMember(2)]
        public string PlayerName { get; private set; }
        [ProtoMember(5)]
        public short PokerClientId { get; private set; }
        [ProtoMember(3)]
        public AIGeneration AiType { get; private set; }
        [ProtoMember(4)]
        public string AiConfigStr { get; private set; }

        private PokerPlayer() { }

        public PokerPlayer(long playerId, string playerName, short pokerClientId, AIGeneration aiType, string aiConfigStr)
        {
            this.PlayerId = playerId;
            this.PlayerName = playerName;
            this.PokerClientId = pokerClientId;

            this.AiType = aiType;
            this.AiConfigStr = aiConfigStr;
        }

        public override string ToString()
        {
            return "(" + PlayerId.ToString() + ") " + PlayerName + " (" + (int)AiType + " - " + AiConfigStr + ")";
        }
    }
}
