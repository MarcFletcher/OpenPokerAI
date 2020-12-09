namespace PokerBot.Definitions
{
  /// <summary>
  /// The different generations of AI the project iteration through.
  /// </summary>
  public enum AIGeneration
  {
    /// <summary>
    /// An undefined AI type
    /// </summary>
    Undefined = -1,

    /// <summary>
    /// This is a human AI ;)
    /// </summary>
    NoAi_Human,

    /// <summary>
    /// A very simple tight, parameterisable AI, that plays more
    /// aggressively the better it's hand. Kind of an obvious opponent.
    /// </summary>
    SimpleV1,

    /// <summary>
    /// Similar to SimpleV1 but with independent pre and post flop strategies
    /// </summary>
    SimpleV2,

    /// <summary>
    /// A neural AI with a large number of inputs that attempted to replicate 
    /// the play of Doyle Brunson in his book Super System. Loose Aggressive (LA)
    /// and Tight Aggressive (TA) players included.
    /// </summary>
    NeuralV1,

    /// <summary>
    /// Iteration of SimpleV1 that includes initial Player Action Prediction (PAP)
    /// values of 'RaiseToCall' to make it more dynamic. Remains parameterisable.
    /// </summary>
    SimpleV3,

    /// <summary>
    /// A vastly simplified series of neural inputs in comparison with NeuralV1
    /// that was optimised using a genetic algorithm to beat SimpleV1. Managed
    /// to crush simpleV1 but play style was not realistic.
    /// </summary>
    NeuralV2,

    /// <summary>
    /// Iteration of SimpleV3 but rather than being parameterisable matches
    /// the aggression (AvgScaledOppPreFlopPlayFreq) of it's opponents.
    /// </summary>
    SimpleV4AggressionTrack,

    /// <summary>
    /// Iteration of Neural V2 with more inputs that was optimised using a genetic
    /// algorithm to beat previous simple AIs. Again crushes them but play style
    /// still not realistic.
    /// </summary>
    NeuralV3,

    /// <summary>
    /// Iteration of NeuralV3 and the introduction of more realistic win ratio
    /// metrics that take into account opponents plays, i.e. Probability Beat.
    /// This was the first neural generation for which we created a 'distribution' of
    /// aggression/play styles to represent realistic play.
    /// </summary>
    NeuralV4,

    /// <summary>
    /// Test AI model that attempted to simply play on the modelled action
    /// an average player might make based on the win ratio models, i.e. quality
    /// of hand. Ended up not working so implementation in WRProvider was removed.
    /// </summary>
    SimpleV5,

    /// <summary>
    /// Test AI model that always folded by logged the probability it was beat.
    /// </summary>
    SimpleV6,

    /// <summary>
    /// Iteration on NeuralV4 which included real-time play and aggression statistics
    /// on table opponents. This allowed it to adapt strategies depending on
    /// who the opponents were. NeuralV5_Inca was the single player that beat
    /// the Neural V4 distribution. Instabilities/Anomalies started to emerge in
    /// the play which appear to be taking advantage of specific weaknesses in the
    /// previous generations.
    /// </summary>
    NeuralV5,

    /// <summary>
    /// Neural structure very similar to NeuralV5. In an attempt to suppress anomalous
    /// play behaviours we introduced a cheating opponent, CheatV1. During genetic
    /// training players would get heavily penalised if their loses were high against
    /// the cheating player. All player aggression distributions were recreated.
    /// NeuralV6_Utgard was the winning player against V6 distributions and displayed
    /// markedly more stable/predictable play than NeuralV5_Inca.
    /// </summary>
    NeuralV6,

    /// <summary>
    /// The most sophisticated rule based AI so far. Could have been made parameterisable
    /// but we never got that far. Used fixed play frequencies for hole cards pre
    /// flop (which is how most humans do it) and then took advantage of expected (EV)
    /// and probability beat metrics, rather than the more naive win probability metrics
    /// of earlier simple AIs.
    /// </summary>
    SimpleV7,

    /// <summary>
    /// A cheating AI that knows your cards. Logic is primarily focused on how to maximise
    /// the winnings, i.e. will slow play if you're being aggressive.
    /// </summary>
    CheatV1,

    /// <summary>
    /// An iteration on Neural V6 with the big difference that a denormalised neural network
    /// output is used to determine a raise amount. During genetic training the profile of raises
    /// was constrained to match that of real play. Genetic player distributions were
    /// recreated, similar to NeuralV6 (using the cheater). We saw further stabilisation
    /// in crazy/anomalous strategies. Two final players were optimised against these NeuralV7
    /// distributions; NeuralV7_Valhalla had to very closely match the required raise amount
    /// distribution and NeuralV7_Vanaheim had to only loosely match the raise distribution.
    /// </summary>
    NeuralV7,
  }
}
