namespace PokerBot.Definitions
{
    public enum AIGeneration
    {
        Undefined = -1,
        NoAi_Human,
        SimpleV1,
        SimpleV2,
        NeuralV1,
        SimpleV3,
        NeuralV2,
        SimpleV4AggressionTrack,
        NeuralV3,
        NeuralV4,
        SimpleV5, //AI based purely on WR model
        SimpleV6, //AI based purely on prob beat. Just fold right now
        NeuralV5,
        NeuralV6,
        SimpleV7, //AI based on probBeat and EV metrics
        CheatV1,
        NeuralV7,
    }
}
