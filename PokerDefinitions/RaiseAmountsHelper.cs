using System;

namespace PokerBot.Definitions
{
  public static class RaiseAmountsHelper
  {
    ///////////////////////////////////////////////////////////////////////////////////
    //Bin width of 0.10 (which keeps raises of 0.5, 0.75 and 1 (BB=0.25) seperate)
    ///////////////////////////////////////////////////////////////////////////////////
    public static readonly int[] PreFlopRaiseBinIdeal = new int[] { 712911, 3658260, 1310430, 759645, 357779, 73622, 32099, 46874, 17478, 19961, 1 };

    public static readonly int[] PostFlopRaiseBinIdealSmall = new int[] { 289308, 532368, 493414, 1169170, 173580, 9393, 3792, 3880, 1921, 2298, 1 };
    public static readonly int[] FlopRaiseBinIdealSmall = new int[] { 173052, 338076, 340384, 907799, 129592, 4764, 1839, 2344, 1023, 1079, 1 };
    public static readonly int[] TurnRaiseBinIdealSmall = new int[] { 82649, 145041, 113002, 190875, 27249, 1706, 599, 477, 241, 252, 1 };
    public static readonly int[] RiverRaiseBinIdealSmall = new int[] { 33607, 49251, 40028, 70495, 16739, 2923, 1354, 1059, 657, 967, 1 };

    public static readonly int[] PostFlopRaiseBinIdealBig = new[] { 65658, 177718, 400990, 825542, 394935, 68932, 36085, 21620, 13190, 14113, 1 };
    public static readonly int[] FlopRaiseBinIdealBig = new int[] { 14289, 34932, 122679, 363223, 197351, 28036, 14414, 9138, 5589, 6570, 1 };
    public static readonly int[] TurnRaiseBinIdealBig = new int[] { 24346, 69528, 149822, 303268, 122725, 20223, 10878, 6094, 3657, 3553, 1 };
    public static readonly int[] RiverRaiseBinIdealBig = new int[] { 27023, 73258, 128489, 159051, 74859, 20673, 10793, 6388, 3944, 3990, 1 };

    ///////////////////////////////////////////////////////////////////////////////////
    //Raise amount CDF for randomly selecting correct raise amount based on distribution
    ///////////////////////////////////////////////////////////////////////////////////
    public static readonly double[] PreFlopRaiseBinIdealCDF = new double[] { 0.10202, 0.6255, 0.81301, 0.92172, 0.97282, 0.98343, 0.98804, 0.99475, 0.99725, 1 };

    public static readonly double[] FlopRaiseBinIdealSmallCDF = new double[] { 0.09108, 0.26902, 0.44818, 0.92598, 0.99418, 0.99669, 0.99766, 0.99889, 0.99943, 1 };
    public static readonly double[] TurnRaiseBinIdealSmallCDF = new double[] { 0.14704, 0.40508, 0.60612, 0.9457, 0.99417, 0.99721, 0.99827, 0.99912, 0.99955, 1 };
    public static readonly double[] RiverRaiseBinIdealSmallCDF = new double[] { 0.15481, 0.38169, 0.56609, 0.89083, 0.96794, 0.9814, 0.98764, 0.99252, 0.99555, 1 };

    public static readonly double[] FlopRaiseBinIdealBigCDF = new double[] { 0.01795, 0.06182, 0.21589, 0.67208, 0.91994, 0.95515, 0.97325, 0.98473, 0.99175, 1 };
    public static readonly double[] TurnRaiseBinIdealBigCDF = new double[] { 0.03409, 0.13146, 0.34127, 0.76596, 0.93782, 0.96614, 0.98137, 0.9899, 0.99502, 1 };
    public static readonly double[] RiverRaiseBinIdealBigCDF = new double[] { 0.05315, 0.19722, 0.44992, 0.76272, 0.90995, 0.95061, 0.97183, 0.9844, 0.99215, 1 };

    /// <summary>
    /// The multiple of big blinds after which a pot amount is no longer considered small
    /// </summary>
    public const decimal SmallPotBBMultiplierLimit = 10;

    /// <summary>
    /// The multiple of big blinds after which a pot amount is considered MAXIMUM (i.e. no longer increasing)
    /// </summary>
    public const decimal BigPotBBMultiplierLimit = 50;

    /// <summary>
    /// Takes the provided ADDITIONAL raise amounts and scales accordingly between 0 (min additional raise) and 1 (max additional raise) on a log scale.
    /// </summary>
    /// <param name="currentPotAmount"></param>
    /// <param name="bigBlind"></param>
    /// <param name="minAdditionalRaiseAmount"></param>
    /// <param name="maxAdditionalRaiseAmount"></param>
    /// <param name="additionalRaiseAmountToScale"></param>
    /// <returns></returns>
    public static double ScaleAdditionalRaiseAmount(decimal currentPotAmount, decimal bigBlind, decimal minAdditionalRaiseAmount, decimal maxAdditionalRaiseAmount, decimal additionalRaiseAmountToScale)
    {
      if (additionalRaiseAmountToScale < 0)
        throw new Exception("additionalRaiseAmountToScale cannot be less than 0.");
      if (minAdditionalRaiseAmount > maxAdditionalRaiseAmount)
        throw new Exception("minAdditionalRaiseAmount should be less than maxAdditionalRaiseAmount!");

      double potAmount_Double = (double)(currentPotAmount);

      if (potAmount_Double > (double)(BigPotBBMultiplierLimit * bigBlind))
        potAmount_Double = (double)(BigPotBBMultiplierLimit * bigBlind);
      if (potAmount_Double < (double)(SmallPotBBMultiplierLimit * bigBlind))
        potAmount_Double = (double)(SmallPotBBMultiplierLimit * bigBlind);

      double b = ((double)(101 * bigBlind) - 2 * potAmount_Double) / Math.Pow((double)bigBlind - potAmount_Double, 2);
      double c = 1 - (double)bigBlind * b;
      double a = 1 / Math.Log((double)(100 * bigBlind) * b + c, Math.E);

      var min = a * Math.Log(b * (double)minAdditionalRaiseAmount + c, Math.E);
      var max = a * Math.Log(b * (double)maxAdditionalRaiseAmount + c, Math.E);

      var result = a * Math.Log(b * (double)(additionalRaiseAmountToScale) + c, Math.E);
      if (result < min)
        result = min;
      if (result > max)
        result = max;

      return result;
    }

    /// <summary>
    /// Takes the provided ADDITIONAL raise amounts and unscales accordingly between the provided min and max ADDITIONAL raise amounts.
    /// </summary>
    /// <param name="currentPotAmount"></param>
    /// <param name="bigBlind"></param>
    /// <param name="minAdditionalRaiseAmount"></param>
    /// <param name="maxAdditionalRaiseAmount"></param>
    /// <param name="scaledAdditionalRaiseAmount"></param>
    /// <returns></returns>
    public static decimal UnscaleAdditionalRaiseAmount(decimal currentPotAmount, decimal bigBlind, decimal minAdditionalRaiseAmount, decimal maxAdditionalRaiseAmount, double scaledAdditionalRaiseAmount)
    {
      if (scaledAdditionalRaiseAmount < 0)
        throw new Exception("scaledAdditionalRaiseAmount cannot be less than 0.");
      if (minAdditionalRaiseAmount > maxAdditionalRaiseAmount)
        throw new Exception("minAdditionalRaiseAmount should be less than maxAdditionalRaiseAmount!");

      double potAmount_Double = (double)(currentPotAmount);

      if (potAmount_Double > (double)(BigPotBBMultiplierLimit * bigBlind))
        potAmount_Double = (double)(BigPotBBMultiplierLimit * bigBlind);
      if (potAmount_Double < (double)(SmallPotBBMultiplierLimit * bigBlind))
        potAmount_Double = (double)(SmallPotBBMultiplierLimit * bigBlind);

      double b = ((double)(101 * bigBlind) - 2 * potAmount_Double) / Math.Pow((double)bigBlind - potAmount_Double, 2);
      double c = 1 - (double)bigBlind * b;
      double a = 1 / Math.Log((double)(100 * bigBlind) * b + c, Math.E);

      var result = (decimal)((Math.Exp(scaledAdditionalRaiseAmount / a) - c) / b);

      if (result < minAdditionalRaiseAmount)
        result = minAdditionalRaiseAmount;
      if (result > maxAdditionalRaiseAmount)
        result = maxAdditionalRaiseAmount;

      return result;
    }
  }
}
