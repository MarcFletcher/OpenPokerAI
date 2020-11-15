using System;
using System.Linq;
using System.IO;
using ProtoBuf;

namespace PokerBot.Definitions
{
    /// <summary>
    /// Wrapper required to deseriaise large handRank array without causing massive memory usage
    /// </summary>
    [ProtoContract]
    public class HandRankWrapper
    {
        [ProtoMember(1)]
        public int[] Values { get; set; }
    }

    public static class HandRank
    {

        static object handRankLocker = new object();
        static int[] handRank;

        public static bool HandRankLoaded
        {
            get { return handRank != null; }
        }

        public static int GetHandRank(Card card1, Card card2, Card[] tableCards)
        {
            lock (handRankLocker)
                if (handRank == null)
                    LoadHandRank();

            int returnValue = 53;
            
            if (tableCards.Length >= 3)
            {
                returnValue = handRank[returnValue + (byte)tableCards[0]];
                returnValue = handRank[returnValue + (byte)tableCards[1]];
                returnValue = handRank[returnValue + (byte)tableCards[2]];
            }

            if (tableCards.Length >= 4)
                returnValue = handRank[returnValue + (byte)tableCards[3]];

            if (tableCards.Length == 5)
                returnValue = handRank[returnValue + (byte)tableCards[4]];

            if (tableCards.Length > 5)
                throw new Exception("Why are there more than 5 table cards???");

            if (card1 == Card.Unseen)
            {
                returnValue = handRank[returnValue + (byte)card2];

                int val = int.MaxValue;

                for (int i = 1; i <= 52; i++)
                {
                    Card temp = (Card)i;

                    if (card2 == temp || tableCards.Contains(temp))
                        continue;

                    val = handRank[returnValue + i] < val ? handRank[returnValue + i] : val;
                }

                returnValue = val;
            }
            else if (card2 == Card.Unseen)
            {
                returnValue = handRank[returnValue + (byte)card1];

                int val = int.MaxValue;

                for (int i = 1; i <= 52; i++)
                {
                    Card temp = (Card)i;

                    if (card1 == temp || tableCards.Contains(temp))
                        continue;

                    val = handRank[returnValue + i] < val ? handRank[returnValue + i] : val;
                }

                returnValue = val;
            }
            else
            {
                returnValue = handRank[returnValue + (byte)card1];
                returnValue = handRank[returnValue + (byte)card2];
            }

            return returnValue;
        }

        public static int GetHandRank(Card card1, Card card2, Card card3, Card card4, Card card5, Card card6, Card card7)
        {
            lock (handRankLocker)
                if (handRank == null)
                    LoadHandRank();
            
            bool unseenCard = false;

            if (card1 == Card.Unseen) { unseenCard = true; card1 = card7; card7 = Card.Unseen; }
            if (card2 == Card.Unseen) { unseenCard = true; card2 = card7; card7 = Card.Unseen; }
            if (card3 == Card.Unseen) { unseenCard = true; card3 = card7; card7 = Card.Unseen; }
            if (card4 == Card.Unseen) { unseenCard = true; card4 = card7; card7 = Card.Unseen; }
            if (card5 == Card.Unseen) { unseenCard = true; card5 = card7; card7 = Card.Unseen; }
            if (card6 == Card.Unseen) { unseenCard = true; card6 = card7; card7 = Card.Unseen; }
            if (card7 == Card.Unseen) { unseenCard = true; } 

            int returnValue = handRank[53 + (byte)card1];
            returnValue = handRank[returnValue + (byte)card2];
            returnValue = handRank[returnValue + (byte)card3];
            returnValue = handRank[returnValue + (byte)card4];
            returnValue = handRank[returnValue + (byte)card5];
            returnValue = handRank[returnValue + (byte)card6];

            if (!unseenCard)
                returnValue = handRank[returnValue + (byte)card7];
            else
            {
                int val = int.MaxValue;

                for (int i = 1; i <= 52; i++)
                {
                    Card temp = (Card)i;

                    if (card1 == temp || card2 == temp || card3 == temp || card4 == temp || card5 == temp || card6 == temp)
                        continue;

                    val = handRank[returnValue + i] < val ? handRank[returnValue + i] : val;
                }

                returnValue = val;
            }

            return returnValue;
        }

        private static void LoadHandRank()
        {
            //Load hand rank for any CPU calculations
            handRank = new int[32487834];

            String handRanksLocation = Environment.GetEnvironmentVariable("handRanksLocation");
            if (File.Exists(handRanksLocation))
            {
                BinaryReader HandRankFile = new BinaryReader(File.OpenRead(handRanksLocation));

                for (int i = 0; i < handRank.Length; i++)
                {
                    handRank[i] = HandRankFile.ReadInt32();
                }

                HandRankFile.Close();
            }
            else
            {
                throw new Exception("Unable to find HandRanks.dat file");
            }
        }

        //Returns the int array used by handRank
        public static int[] HandRankArray()
        {
            lock (handRankLocker)
                if (handRank == null)
                    LoadHandRank();

            return handRank;
        }

        public static void SetHandRankArray(int[] handRankArray)
        {
            lock (handRankLocker) handRank = handRankArray;
        }

        public static void ClearHandRankArray()
        {
            lock (handRankLocker) handRank = null;
        }
    }
}
