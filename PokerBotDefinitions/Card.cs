namespace PokerBot.Definitions
{
    /// <summary>
    /// Enum containing the card types in a standard 52 card deck
    /// 
    /// This enum has been 1 indexed to maintain compatibility with hand comparison algorithm
    /// </summary>
    public enum Card : byte
    {
        NoCard=0,

        Clubs2=1,     //[1] 
        Diamonds2,
        Hearts2,
        Spades2,

        Clubs3,     //[5]
        Diamonds3,
        Hearts3,
        Spades3,

        Clubs4,     //[9]
        Diamonds4,
        Hearts4,
        Spades4,

        Clubs5,     //[13]
        Diamonds5,
        Hearts5,
        Spades5,

        Clubs6,     //[17]
        Diamonds6,
        Hearts6,
        Spades6,

        Clubs7,     //[21]
        Diamonds7,
        Hearts7,
        Spades7,

        Clubs8,     //[25]
        Diamonds8,
        Hearts8,
        Spades8,

        Clubs9,     //[29]
        Diamonds9,
        Hearts9,
        Spades9,

        Clubs10,    //[33]
        Diamonds10,
        Hearts10,
        Spades10,

        ClubsJ,     //[37]
        DiamondsJ,
        HeartsJ,
        SpadesJ,

        ClubsQ,     //[41]
        DiamondsQ,
        HeartsQ,
        SpadesQ,

        ClubsK,     //[45]
        DiamondsK,
        HeartsK,
        SpadesK,

        ClubsA,     //[49]
        DiamondsA,
        HeartsA,
        SpadesA,

        Unseen = 255, //For when somebody shows only a single card
    }   
}
