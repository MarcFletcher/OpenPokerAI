using System;

namespace PokerBot.Definitions
{
  public class Deck
  {
    /*  Clubs2 = 1,
        Diamonds2 = 2,
        Hearts2 = 4,
        Spades2 = 8,
        Clubs3 = 16,
        Diamonds3 = 32,
        Hearts3 = 64,
        Spades3 = 128,
        Clubs4 = 256,
        Diamonds4 = 512,
        Hearts4 = 1024,
        Spades4 = 2048,
        Clubs5 = 4096,
        Diamonds5 = 8192,
        Hearts5 = 16384,
        Spades5 = 32768,
        Clubs6 = 65536,
        Diamonds6 = 131072,
        Hearts6 = 262144,
        Spades6 = 524288,
        Clubs7 = 1048576,
        Diamonds7 = 2097152,
        Hearts7 = 4194304,
        Spades7 = 8388608,
        Clubs8 = 16777216,
        Diamonds8 = 33554432,
        Hearts8 = 67108864,
        Spades8 = 134217728,
        Clubs9 = 268435456,
        Diamonds9 = 536870912,
        Hearts9 = 1073741824,
        Spades9 = 2147483648,
        Clubs10 = 4294967296,
        Diamonds10 = 8589934592,
        Hearts10 = 17179869184,
        Spades10 = 34359738368,
        ClubsJ = 68719476736,
        DiamondsJ = 137438953472,
        HeartsJ = 274877906944,
        SpadesJ = 549755813888,
        ClubsQ = 1099511627776,
        DiamondsQ = 2199023255552,
        HeartsQ = 4398046511104,
        SpadesQ = 8796093022208,
        ClubsK = 17592186044416,
        DiamondsK = 35184372088832,
        HeartsK = 70368744177664,
        SpadesK = 140737488355328,
        ClubsA = 281474976710656,
        DiamondsA = 562949953421312,
        HeartsA = 1125899906842624,
        SpadesA = 2251799813685248,
    */
    long dealtCards;
    Random randomGen;

    public Deck()
    {
      randomGen = new CMWCRandom();
      dealtCards = 0;
    }

    public Deck(long seed)
    {
      randomGen = new CMWCRandom(seed);
      dealtCards = 0;
    }

    public void Shuffle()
    {
      dealtCards = 0;
    }

    public void Shuffle(long savedDeckState)
    {
      dealtCards = savedDeckState;
    }

    public long GetDealtCards
    {
      get { return dealtCards; }
    }

    /// <summary>
    /// Removes the given card from the current deck meaning it won't be pulled again
    /// </summary>
    public void RemoveCard(int thisCard)
    {
      if (CardAlreadyDealt((Card)thisCard))
        throw new Exception("You cannot remove a card that has already been dealt.");
      else
        dealtCards |= (1L << (thisCard - 1));
    }

    /// <summary>
    /// Returns a random card from the deck but ensures it is greater than minimumCardValue
    /// </summary>
    /// <param name="minimumCardValue">The exclsuive lower limit of cards to return.</param>
    /// <returns></returns>
    public byte GetNextCard(byte minimumCardValue)
    {
      int i;
      long j;

      while (true)
      {
        i = minimumCardValue + (int)(randomGen.NextDouble() * (52 - minimumCardValue));

        j = 1L << i;

        if ((j & dealtCards) == 0)
        {
          dealtCards |= j;
          break;
        }
      }

      //card = (Card)(i + 1);
      //return card;

      return (byte)(i + 1);
    }

    /// <summary>
    /// Returns a random card from the deck.
    /// Testing - Standard Deviation (Drawing 1E9 single cards) = 0.02%
    /// </summary>
    /// <returns>A Card</returns>
    public byte GetNextCard()
    {
      return GetNextCard(0);
    }

    /// <summary>
    /// Returns the number of cards currently dealt or removed from this deck
    /// </summary>
    /// <returns></returns>
    public byte GetNumCards()
    {
      return GetNumCards(dealtCards);
    }

    /// <summary>
    /// Returns a boolean which can be used to determine if a specific card has already been drawn.
    /// </summary>
    /// <returns></returns>
    public bool CardAlreadyDealt(Card card)
    {
      if ((dealtCards & (1L << ((byte)card - 1))) != 0)
        return true;
      else
        return false;
    }

    /// <summary>
    /// Returns the number of cards present in the provided (long)cards
    /// </summary>
    /// <param name="cards"></param>
    /// <returns></returns>
    public static byte GetNumCards(long cards)
    {
      //long uCardDeck = 0;
      long tmp = 0;

      //uCardDeck = BitConverter.ToUInt64(BitConverter.GetBytes(dealtCards), 0);

      //64-Bit
      tmp = cards - ((cards >> 1) & 0x06DB6DB6DB6DB6DB) - ((cards >> 2) & 0x0249249249249249);
      tmp = ((tmp + (tmp >> 3)) & 0x01C71C71C71C71C7);
      return (byte)(((tmp + (tmp >> 6)) & 0x003F03F03F03F03F) % 4095);

      //32-Bit
      /*
      tmp = (cardDeck - ((cardDeck >> 1) & 3681400539) - ((cardDeck >> 2) & 1227133513));
      result = ((tmp + (tmp >> 3)) & 3340530119) % 63;
      */
    }

    /// <summary>
    /// Returns the integer index value of the card between 1 and 13 corresponding to 2 and ace.
    /// </summary>
    /// <param name="card"></param>
    /// <returns></returns>
    public static byte GetCardNumber(Card card)
    {
      return (byte)((((double)(int)(((int)card - 1) / 4)) + 1));
    }

    public static byte GetCardSuit(Card card)
    {
      return (byte)(((((double)card - 1) % 4) + 1));
    }

    #region Straight

    /// <summary>
    /// Returns true if any combination of hole cards in conjunction with given tablecards could result in a straight
    /// </summary>
    /// <param name="tableCards"></param>
    /// <returns></returns>
    public static bool StraightPossible(long tableCards)
    {
      long mask = 0x011111000000000;
      long tmp = (tableCards | (tableCards >> 1) | (tableCards >> 2) | (tableCards >> 3)) & 0x011111111111111;
      tmp = (tmp << 4) + ((tmp & 0x000F000000000000) >> 48);
      for (byte i = 0; i < 10; i++)
      {
        if (GetNumCards(tmp & (mask >> (i * 4))) >= 3)
          return true;
      }

      return false;
    }

    public static bool StraightDrawPossibleOnTable(long tableCards)
    {
      long mask = 0x011111000000000;
      long tmp = (tableCards | (tableCards >> 1) | (tableCards >> 2) | (tableCards >> 3)) & 0x011111111111111;
      tmp = (tmp << 4) + ((tmp & 0x000F000000000000) >> 48);
      int numTableCards = GetNumCards(tableCards);
      bool straightPossible = StraightPossible(tableCards);

      for (byte i = 0; i < 10; i++)
      {
        if ((GetNumCards(tmp & (mask >> (i * 4))) >= 2 && numTableCards != 5) || straightPossible)
          return true;
      }

      return false;
    }

    /// <summary>
    /// Straight Draw is defind as one additional card required to make a straight on top of those provided.
    /// 3, 4, 5 or 6 cards can be provided.
    /// </summary>
    /// <param name="knownCards"></param>
    /// <returns></returns>
    public static bool StraightDrawPossible(long knownCards)
    {
      long mask = 0x011111000000000;

      long tmp = (knownCards | (knownCards >> 1) | (knownCards >> 2) | (knownCards >> 3)) & 0x011111111111111;
      tmp = (tmp << 4) + ((tmp & 0x000F000000000000) >> 48);
      int numKnownCards = GetNumCards(knownCards);
      bool straightMade = StraightMade(knownCards);

      for (int i = 0; i <= 10; i++)
      {
        if ((GetNumCards(tmp & (mask >> (i * 4))) >= 4 && numKnownCards != 7) || straightMade)
          return true;
      }

      return false;
    }

    public static bool StraightMade(long knownCards)
    {
      long mask = 0x011111000000000;
      long tmp = (knownCards | (knownCards >> 1) | (knownCards >> 2) | (knownCards >> 3)) & 0x011111111111111;
      tmp = (tmp << 4) + ((tmp & 0x000F000000000000) >> 48);

      for (int i = 0; i <= 10; i++)
      {
        if (GetNumCards(tmp & (mask >> (i * 4))) == 5)
          return true;
      }

      return false;
    }

    public static bool StraightMadeWithHoleCards(long holeCards, long knownTableCards)
    {
      long knownCards = holeCards | knownTableCards;

      long mask = 0x011111000000000;
      long tmpWithHC = (knownCards | (knownCards >> 1) | (knownCards >> 2) | (knownCards >> 3)) & 0x011111111111111;
      tmpWithHC = (tmpWithHC << 4) + ((tmpWithHC & 0x000F000000000000) >> 48);
      long tmpWithOutHC = (knownTableCards | (knownTableCards >> 1) | (knownTableCards >> 2) | (knownTableCards >> 3)) & 0x011111111111111;
      tmpWithOutHC = (tmpWithOutHC << 4) + ((tmpWithOutHC & 0x000F000000000000) >> 48);

      for (int i = 0; i <= 10; i++)
      {
        if (GetNumCards(tmpWithHC & (mask >> (i * 4))) == 5 && GetNumCards(tmpWithOutHC & (mask >> (i * 4))) != 5)
          return true;
      }

      return false;
    }

    public static bool OutsideStraightDrawWithHoleCards(long holeCards, long knownTableCards)
    {
      long knownCards = holeCards | knownTableCards;

      long mask = 0x011111000000000;
      long outerStraightMask = 0x011110000000000;

      long tmpWithHC = (knownCards | (knownCards >> 1) | (knownCards >> 2) | (knownCards >> 3)) & 0x011111111111111;
      tmpWithHC = (tmpWithHC << 4) + ((tmpWithHC & 0x000F000000000000) >> 48);
      long tmpWithoutHC = (knownTableCards | (knownTableCards >> 1) | (knownTableCards >> 2) | (knownTableCards >> 3)) & 0x011111111111111;
      tmpWithoutHC = (tmpWithoutHC << 4) + ((tmpWithoutHC & 0x000F000000000000) >> 48);

      for (int i = 0; i <= 10; i++)
      {
        if (GetNumCards(tmpWithHC & (mask >> (i * 4))) >= 4 && GetNumCards(knownCards) != 7)
        {
          if (!(GetNumCards(tmpWithoutHC & (mask >> (i * 4))) >= 4 && GetNumCards(knownTableCards) != 5))
          {
            if (((tmpWithHC & (mask >> (i * 4))) & ~(outerStraightMask >> (i * 4))) == 0 || ((tmpWithHC & (mask >> (i * 4))) & ~(outerStraightMask >> (i * 4 + 1))) == 0)
              return true;
          }
        }
      }

      if (StraightMade(knownCards) && !StraightMade(knownTableCards))
        return true;

      return false;
    }

    public static bool InsideStraightDrawWithHoleCards(long holeCards, long knownTableCards)
    {
      long knownCards = holeCards | knownTableCards;

      long mask = 0x011111000000000;
      long innerStraightMask1 = 0x010111000000000;
      long innerStraightMask2 = 0x011011000000000;
      long innerStraightMask3 = 0x011101000000000;

      long tmpWithHC = (knownCards | (knownCards >> 1) | (knownCards >> 2) | (knownCards >> 3)) & 0x011111111111111;
      tmpWithHC = (tmpWithHC << 4) + ((tmpWithHC & 0x000F000000000000) >> 48);
      long tmpWithoutHC = (knownTableCards | (knownTableCards >> 1) | (knownTableCards >> 2) | (knownTableCards >> 3)) & 0x011111111111111;
      tmpWithoutHC = (tmpWithoutHC << 4) + ((tmpWithoutHC & 0x000F000000000000) >> 48);

      for (int i = 0; i <= 10; i++)
      {
        if (GetNumCards(tmpWithHC & (mask >> (i * 4))) >= 4 && GetNumCards(knownCards) != 7)
        {
          if (!(GetNumCards(tmpWithoutHC & (mask >> (i * 4))) >= 4 && GetNumCards(knownTableCards) != 5))
          {
            if (((tmpWithHC & (mask >> (i * 4))) & ~(innerStraightMask1 >> (i * 4))) == 0 ||
                ((tmpWithHC & (mask >> (i * 4))) & ~(innerStraightMask2 >> (i * 4))) == 0 ||
                ((tmpWithHC & (mask >> (i * 4))) & ~(innerStraightMask3 >> (i * 4))) == 0)
              return true;
          }
        }
      }

      if (StraightMade(knownCards) && !StraightMade(knownTableCards))
        return true;

      return false;
    }

    #endregion Straight

    #region Flush

    public static bool FlushPossible(long tableCards)
    {
      long mask = 0x0008888888888888;

      for (int i = 0; i < 4; i++)
      {
        if (GetNumCards(tableCards & (mask >> i)) >= 3)
          return true;
      }

      return false;
    }

    public static bool FlushDrawPossible(long knownCards)
    {
      long mask = 0x0008888888888888;

      for (int i = 0; i < 4; i++)
      {
        if ((GetNumCards(knownCards & (mask >> i)) >= 4 && GetNumCards(knownCards) != 7) || FlushMade(knownCards))
          return true;
      }

      return false;
    }

    public static bool FlushDrawPossibleOnTable(long tableCards)
    {
      long mask = 0x0008888888888888;

      for (int i = 0; i < 4; i++)
      {
        if ((GetNumCards(tableCards & (mask >> i)) >= 2 && GetNumCards(tableCards) != 5) || FlushPossible(tableCards))
          return true;
      }

      return false;
    }

    public static bool FlushMade(long knownCards)
    {
      long mask = 0x0008888888888888;

      for (int i = 0; i < 4; i++)
      {
        if (GetNumCards(knownCards & (mask >> i)) >= 5)
          return true;
      }

      return false;
    }

    public static bool FlushMadeWithHoleCards(long holeCards, long knownTableCards)
    {
      long knownCards = holeCards | knownTableCards;

      long mask = 0x0008888888888888;

      for (int i = 0; i < 4; i++)
      {
        if (GetNumCards(knownCards & (mask >> i)) >= 5 && GetNumCards(knownTableCards & (mask >> i)) < 5)
          return true;
      }

      return false;
    }

    #endregion Flush

    #region ThreeOfKind

    public static bool ThreeOfKindMadeWithHoleCards(long holeCards, long knownTableCards)
    {
      long knownCards = holeCards | knownTableCards;

      long mask = 0x000F000000000000;

      for (int i = 0; i < 13; i++)
      {
        if (GetNumCards(knownCards & (mask >> 4 * i)) >= 3 && GetNumCards(knownTableCards & (mask >> 4 * i)) < 3)
          return true;
      }

      return false;
    }

    public static bool ThreeOfKindOnTable(long knownTableCards)
    {
      long mask = 0x000F000000000000;

      for (int i = 0; i < 13; i++)
      {
        if (GetNumCards(knownTableCards & (mask >> 4 * i)) >= 3)
          return true;
      }

      return false;
    }

    #endregion ThreeOfKind

    #region FourOfKind

    public static bool FourOfKindMadeWithHoleCards(long holeCards, long knownTableCards)
    {
      long knownCards = holeCards | knownTableCards;

      long mask = 0x000F000000000000;

      for (int i = 0; i < 13; i++)
      {
        if (GetNumCards(knownCards & (mask >> 4 * i)) == 4 && GetNumCards(knownTableCards & (mask >> 4 * i)) < 4)
          return true;
      }

      return false;
    }

    #endregion

    #region FullHouse

    public static bool FullHouseWithHoleCards(long holeCards, long knownTableCards)
    {
      long knownCards = holeCards | knownTableCards;

      long mask = 0x000F000000000000;

      for (int i = 0; i < 13; i++)
      {
        if (GetNumCards(knownCards & (mask >> 4 * i)) >= 3)
        {
          if (GetNumCards(knownTableCards & (mask >> 4 * i)) < 3)
          {
            for (int j = 0; j < 13; j++)
            {
              if (j == i)
                continue;

              if (GetNumCards(knownCards & (mask >> 4 * j)) >= 2)
                return true;
            }
          }
          else
          {
            for (int j = 0; j < 13; j++)
            {
              if (j == i)
                continue;

              if (GetNumCards(knownCards & (mask >> 4 * j)) >= 2 && GetNumCards(knownTableCards & (mask >> 4 * j)) < 2)
                return true;
            }
          }
        }
      }

      return false;
    }

    #endregion FullHouse

    public static bool TopPairMadeWithHoleCards(long holeCards, long knownTableCards)
    {
      long knownCards = holeCards | knownTableCards;

      long mask = 0x000F000000000000;
      long mask2 = 0x000FFFFFFFFFFFFF;

      if (knownTableCards == 0)
        return GetNumCards(knownCards & mask) >= 2;

      for (int i = 0; i < 13; i++)
      {
        if (GetNumCards(knownCards & (mask >> (4 * i))) >= 2 &&
            GetNumCards(knownTableCards & (mask >> (4 * i))) < 2 &&
            (knownTableCards & ~(mask2 >> (4 * i))) == 0)
          return true;
      }

      return false;
    }

    public static bool BottomPairMadeWithHoleCards(long holeCards, long knownTableCards)
    {
      if (knownTableCards == 0)
        return false;

      long knownCards = holeCards | knownTableCards;

      long mask = 0x000000000000000F;
      long mask2 = 0x000FFFFFFFFFFFFF;

      for (int i = 0; i < 13; i++)
      {
        if (GetNumCards(knownCards & (mask << (4 * i))) >= 2 &&
            GetNumCards(knownTableCards & (mask << (4 * i))) < 2 &&
            (knownTableCards & ~(mask2 & (mask2 << (4 * i)))) == 0)
          return true;
      }

      return false;
    }

    public static bool MiddlePairMadeWithHoleCards(long holeCards, long knownTableCards)
    {
      if (knownTableCards == 0)
        return false;

      if (BottomPairMadeWithHoleCards(holeCards, knownTableCards) || TopPairMadeWithHoleCards(holeCards, knownTableCards))
        return false;

      long knownCards = holeCards | knownTableCards;

      long mask = 0x000F000000000000;

      for (int i = 0; i < 13; i++)
      {
        if (GetNumCards(knownCards & (mask >> (4 * i))) >= 2 &&
            GetNumCards(knownTableCards & (mask >> (4 * i))) < 2)
          return true;
      }

      return false;
    }

    public static bool PairOnTable(long knownTableCards)
    {
      long mask = 0x000F000000000000;

      for (int i = 0; i < 13; i++)
      {
        if (GetNumCards(knownTableCards & (mask >> (4 * i))) >= 2)
          return true;
      }

      return false;
    }

    public static bool TwoPairMadeWithHoleCards(long holeCards, long knownTableCards)
    {
      long knownCards = holeCards | knownTableCards;

      long mask = 0x000F000000000000;

      for (int i = 0; i < 13; i++)
      {
        if (GetNumCards(knownCards & (mask >> 4 * i)) >= 2 && GetNumCards(knownTableCards & (mask >> 4 * i)) < 2)
        {
          for (int j = 0; j < 13; j++)
          {
            if (j == i)
              continue;

            if (GetNumCards(knownCards & (mask >> 4 * j)) >= 2 && GetNumCards(knownTableCards & (mask >> 4 * j)) < 2)
              return true;
          }
        }
      }

      return false;
    }

    public static bool TwoPairOnTable(long knownTableCards)
    {
      long mask = 0x000F000000000000;

      for (int i = 0; i < 13; i++)
      {
        if (GetNumCards(knownTableCards & (mask >> 4 * i)) >= 2)
        {
          for (int j = 0; j < 13; j++)
          {
            if (j == i)
              continue;

            if (GetNumCards(knownTableCards & (mask >> 4 * j)) >= 2)
              return true;
          }
        }
      }

      return false;
    }
  }
}
