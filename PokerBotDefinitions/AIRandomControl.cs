using ProtoBuf;

namespace PokerBot.Definitions
{
  [ProtoContract]
  public class AIRandomControl
  {
    [ProtoMember(1)]
#pragma warning disable IDE1006 // Naming Styles
    public bool DecisionRandomPerHandSeedEnabled = false;

    [ProtoMember(2)]
    public bool InfoProviderRandomPerHandSeedEnabled = true;
    [ProtoMember(3)]
    public long DecisionRandomPerHandSeed = 123456789L;
    [ProtoMember(4)]
    public long InfoProviderRandomPerHandSeed = 987654321L;
#pragma warning restore IDE1006 // Naming Styles

    //Create the aiRandomControl using default values
    public AIRandomControl() { }

    public AIRandomControl(bool DecisionRandomPerHandSeedEnabled, bool InfoProviderRandomPerHandSeedEnabled)
    {
      this.DecisionRandomPerHandSeedEnabled = DecisionRandomPerHandSeedEnabled;
      this.InfoProviderRandomPerHandSeedEnabled = InfoProviderRandomPerHandSeedEnabled;
    }

    public AIRandomControl(bool DecisionRandomPerHandSeedEnabled, bool InfoProviderRandomPerHandSeedEnabled,
        long DecisionRandomPerHandSeed, long InfoProviderRandomPerHandSeed)
    {
      this.DecisionRandomPerHandSeedEnabled = DecisionRandomPerHandSeedEnabled;
      this.InfoProviderRandomPerHandSeedEnabled = InfoProviderRandomPerHandSeedEnabled;
      this.DecisionRandomPerHandSeed = DecisionRandomPerHandSeed;
      this.InfoProviderRandomPerHandSeed = InfoProviderRandomPerHandSeed;
    }
  }
}
