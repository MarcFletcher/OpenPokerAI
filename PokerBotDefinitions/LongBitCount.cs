namespace PokerBot.Definitions
{
    public static class LongBitCount
    {
        /// <summary>
        /// Returns the number of bits set to 1 in a ulong
        /// </summary>
        /// <param name="longToCount"></param>
        /// <returns></returns>
        public static byte CountBits(ulong inputLong)
        {
            byte currentCount = 0;
            long tmp = 0;

            int lowerLong = (int)(inputLong & 0x00000000FFFFFFFF);
            tmp = (lowerLong - ((lowerLong >> 1) & 3681400539) - ((lowerLong >> 2) & 1227133513));
            currentCount += (byte)(((tmp + (tmp >> 3)) & 3340530119) % 63);

            int upperLong = (int)(inputLong >> 32);
            tmp = (upperLong - ((upperLong >> 1) & 3681400539) - ((upperLong >> 2) & 1227133513));
            currentCount += (byte)(((tmp + (tmp >> 3)) & 3340530119) % 63);

            return currentCount;
        }
    }
}
