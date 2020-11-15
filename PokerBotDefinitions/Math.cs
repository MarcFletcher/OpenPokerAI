using System;
using System.Collections.Generic;
using System.Linq;

namespace PokerBot.Definitions
{
    public class FBPMath
    {
        private List<double> values;

        public FBPMath()
        {
            values = new List<double>();
        }

        public void AddValue(double value)
        {
            if (double.IsNaN(value))
                throw new Exception("Value is NaN");

            values.Add(value);
        }

        /// <summary>
        /// Trims the list to the provided maxCount. The most recent added items are preserved.
        /// </summary>
        /// <param name="maxCount"></param>
        public void TrimList(int maxCount)
        {
            if (values.Count > maxCount)
                values = values.Skip(values.Count - maxCount).ToList();
        }

        public void ClearList()
        {
            values = new List<double>();
        }

        public double CalculateMean()
        {
            return FBPMath.CalculateMean(this.values);
        }

        public double CalculateVariance()
        {
            return Math.Pow(FBPMath.CalculateStdDeviation(this.values), 2);
        }

        public double CalculateStdDev()
        {
            return FBPMath.CalculateStdDeviation(this.values);
        }

        #region Static Members
        public static double CalculateMean(List<double> localValues)
        {
            if (localValues.Count == 0)
                return 0;

            double sum = 0;
            double result;

            int countedValues = 0;
            for (int i = 0; i < localValues.Count; i++)
            {
                if (!double.IsNaN(localValues[i]))
                {
                    sum += localValues[i];
                    countedValues++;
                }
            }

            if (countedValues == 0)
                result = 0;
            else
                result = sum / countedValues;

            if (double.IsNaN(result))
                throw new Exception("Result is NaN.");

            return result;
        }

        /// <summary>
        /// Takes a list of arrays, sums all equal rows and returns a single array
        /// </summary>
        /// <param name="valueList"></param>
        /// <returns></returns>
        public static int[] SumArrayList(List<ushort[]> valueList)
        {
            if (valueList.Count > 0)
            {
                int[] returnResult = new int[valueList[0].Length];

                for (int i = 0; i < valueList.Count; i++)
                {
                    if (valueList[i].Length != returnResult.Length)
                        throw new Exception("SumArrayList requires all list elements to be of equal length.");

                    for (int j = 0; j < valueList[i].Length; j++)
                    {
                        returnResult[j] += valueList[i][j];
                    }
                }

                return returnResult;
            }
            else return new int[0];
        }

        /// <summary>
        /// Takes a list of arrays, sums all equal rows and returns a single array
        /// </summary>
        /// <param name="valueList"></param>
        /// <returns></returns>
        public static int[] SumArrayList(List<int[]> valueList)
        {
            if (valueList.Count > 0)
            {
                int[] returnResult = new int[valueList[0].Length];

                for (int i = 0; i < valueList.Count; i++)
                {
                    if (valueList[i].Length != returnResult.Length)
                        throw new Exception("SumArrayList requires all list elements to be of equal length.");

                    for (int j = 0; j < valueList[i].Length; j++)
                    {
                        returnResult[j] += valueList[i][j];
                    }
                }

                return returnResult;
            }
            else
                return new int[0];
        }

        public static double CalculateMedian(List<double> localValues)
        {
            double median = 0;

            if (localValues.Count == 0) return 0;

            int middleIndex;

            if (localValues.Count % 2 == 0)
            {
                middleIndex = (int)((double)localValues.Count() / 2.0);

                //If the values list has an even number of items we need to take the average of the middle two
                double value1, value2;
                value1 = (from current in localValues orderby current ascending select current).ToArray()[middleIndex];
                value2 = (from current in localValues orderby current ascending select current).ToArray()[middleIndex - 1];

                median = (value1 + value2) / 2.0;
            }
            else
            {
                middleIndex = (int)((double)localValues.Count() / 2.0) + 1;

                //If the values list is odd then just take the middle value
                median = (from current in localValues orderby current ascending select current).ToArray()[middleIndex];
            }

            return median;
        }

        public static double CalculateStdDeviation(List<double> localValues)
        {
            double s = 0;
            double result;
            for (int i = 0; i <= localValues.Count - 1; i++)
                s += Math.Pow(localValues[i], 2);

            if (localValues.Count > 1)
                result = (s - localValues.Count * Math.Pow(CalculateMean(localValues), 2)) / (localValues.Count - 1);
            else
                result = Math.Pow((Math.Sqrt(Math.Abs(localValues[0]) * 100)) / 100, 2);

            if (double.IsNaN(result))
                throw new Exception("Error");

            return Math.Sqrt(result);
        }

        public static double CalculateFBPScore(double limitValue, List<double> localValues)
        {
            double mean = FBPMath.CalculateMean(localValues);
            double stdError = FBPMath.CalculateStdDeviation(localValues) / Math.Sqrt((double)localValues.Count);
            double sqrt2 = Math.Sqrt(2);

            if (stdError == 0)
                stdError = 0.000001;

            return 1 - (0.5 * (1 + erf((limitValue - mean) / (sqrt2 * stdError))));
        }

        public static double CalculateFBPScore(double limitValue, double localValueMean, double localValueStdError)
        {
            double sqrt2 = Math.Sqrt(2);

            if (localValueStdError == 0)
                localValueStdError = 0.000001;

            return 1 - (0.5 * (1 + erf((limitValue - localValueMean) / (sqrt2 * localValueStdError))));
        }

        public static double CalculateMeanConfidenceError(double calculatedMeanErrorConfidence, List<double> localValues)
        {
            return (inverf(calculatedMeanErrorConfidence) * CalculateStdDeviation(localValues)) / Math.Sqrt(localValues.Count);
        }

        public static double CalculateMeanConfidenceError(double calculatedMeanErrorConfidence, double meanStdDev, int numberMeanSamples)
        {
            return (inverf(calculatedMeanErrorConfidence) * meanStdDev) / Math.Sqrt(numberMeanSamples);
        }

        public const double machineepsilon = 5E-16;
        public const double maxrealnumber = 1E300;
        public const double minrealnumber = 1E-300;

        /* Error function

        The integral is

                                  x
                                   -
                        2         | |          2
          erf(x)  =  --------     |    exp( - t  ) dt.
                     sqrt(pi)   | |
                                 -
                                  0

        For 0 <= |x| < 1, erf(x) = x * P4(x**2)/Q5(x**2); otherwise
        erf(x) = 1 - erfc(x).


        ACCURACY:

                             Relative error:
        arithmetic   domain     # trials      peak         rms
           IEEE      0,1         30000       3.7e-16     1.0e-16

        Cephes Math Library Release 2.8:  June, 2000
        Copyright 1984, 1987, 1988, 1992, 2000 by Stephen L. Moshier
        *************************************************************************/
        public static double erf(double x)
        {
            double result = 0;
            double xsq = 0;
            double s = 0;
            double p = 0;
            double q = 0;

            s = Math.Sign(x);
            x = Math.Abs(x);
            if ((double)(x) < (double)(0.5))
            {
                xsq = x * x;
                p = 0.007547728033418631287834;
                p = 0.288805137207594084924010 + xsq * p;
                p = 14.3383842191748205576712 + xsq * p;
                p = 38.0140318123903008244444 + xsq * p;
                p = 3017.82788536507577809226 + xsq * p;
                p = 7404.07142710151470082064 + xsq * p;
                p = 80437.3630960840172832162 + xsq * p;
                q = 0.0;
                q = 1.00000000000000000000000 + xsq * q;
                q = 38.0190713951939403753468 + xsq * q;
                q = 658.070155459240506326937 + xsq * q;
                q = 6379.60017324428279487120 + xsq * q;
                q = 34216.5257924628539769006 + xsq * q;
                q = 80437.3630960840172826266 + xsq * q;
                result = s * 1.1283791670955125738961589031 * x * p / q;
                return result;
            }
            if ((double)(x) >= (double)(10))
            {
                result = s;
                return result;
            }
            result = s * (1 - errorfunctionc(x));
            return result;
        }

        /* Inverse of the error function
        Cephes Math Library Release 2.8:  June, 2000
        Copyright 1984, 1987, 1988, 1992, 2000 by Stephen L. Moshier
        *************************************************************************/
        public static double inverf(double e)
        {
            double result = 0;

            result = invnormaldistribution(0.5 * (e + 1)) / Math.Sqrt(2);
            return result;
        }

        /* Inverse of Normal distribution function
        Returns the argument, x, for which the area under the
        Gaussian probability density function (integrated from
        minus infinity to x) is equal to y.


        For small arguments 0 < y < exp(-2), the program computes
        z = sqrt( -2.0 * log(y) );  then the approximation is
        x = z - log(z)/z  - (1/z) P(1/z) / Q(1/z).
        There are two rational functions P/Q, one for 0 < y < exp(-32)
        and the other for y up to exp(-2).  For larger arguments,
        w = y - 0.5, and  x/sqrt(2pi) = w + w**3 R(w**2)/S(w**2)).

        ACCURACY:

                                Relative error:
        arithmetic   domain        # trials      peak         rms
            IEEE     0.125, 1        20000       7.2e-16     1.3e-16
            IEEE     3e-308, 0.135   50000       4.6e-16     9.8e-17

        Cephes Math Library Release 2.8:  June, 2000
        Copyright 1984, 1987, 1988, 1992, 2000 by Stephen L. Moshier
        *************************************************************************/
        public static double invnormaldistribution(double y0)
        {
            double result = 0;
            double expm2 = 0;
            double s2pi = 0;
            double x = 0;
            double y = 0;
            double z = 0;
            double y2 = 0;
            double x0 = 0;
            double x1 = 0;
            int code = 0;
            double p0 = 0;
            double q0 = 0;
            double p1 = 0;
            double q1 = 0;
            double p2 = 0;
            double q2 = 0;

            expm2 = 0.13533528323661269189;
            s2pi = 2.50662827463100050242;
            if ((double)(y0) <= (double)(0))
            {
                result = -maxrealnumber;
                return result;
            }
            if ((double)(y0) >= (double)(1))
            {
                result = maxrealnumber;
                return result;
            }
            code = 1;
            y = y0;
            if ((double)(y) > (double)(1.0 - expm2))
            {
                y = 1.0 - y;
                code = 0;
            }
            if ((double)(y) > (double)(expm2))
            {
                y = y - 0.5;
                y2 = y * y;
                p0 = -59.9633501014107895267;
                p0 = 98.0010754185999661536 + y2 * p0;
                p0 = -56.6762857469070293439 + y2 * p0;
                p0 = 13.9312609387279679503 + y2 * p0;
                p0 = -1.23916583867381258016 + y2 * p0;
                q0 = 1;
                q0 = 1.95448858338141759834 + y2 * q0;
                q0 = 4.67627912898881538453 + y2 * q0;
                q0 = 86.3602421390890590575 + y2 * q0;
                q0 = -225.462687854119370527 + y2 * q0;
                q0 = 200.260212380060660359 + y2 * q0;
                q0 = -82.0372256168333339912 + y2 * q0;
                q0 = 15.9056225126211695515 + y2 * q0;
                q0 = -1.18331621121330003142 + y2 * q0;
                x = y + y * y2 * p0 / q0;
                x = x * s2pi;
                result = x;
                return result;
            }
            x = Math.Sqrt(-(2.0 * Math.Log(y)));
            x0 = x - Math.Log(x) / x;
            z = 1.0 / x;
            if ((double)(x) < (double)(8.0))
            {
                p1 = 4.05544892305962419923;
                p1 = 31.5251094599893866154 + z * p1;
                p1 = 57.1628192246421288162 + z * p1;
                p1 = 44.0805073893200834700 + z * p1;
                p1 = 14.6849561928858024014 + z * p1;
                p1 = 2.18663306850790267539 + z * p1;
                p1 = -(1.40256079171354495875 * 0.1) + z * p1;
                p1 = -(3.50424626827848203418 * 0.01) + z * p1;
                p1 = -(8.57456785154685413611 * 0.0001) + z * p1;
                q1 = 1;
                q1 = 15.7799883256466749731 + z * q1;
                q1 = 45.3907635128879210584 + z * q1;
                q1 = 41.3172038254672030440 + z * q1;
                q1 = 15.0425385692907503408 + z * q1;
                q1 = 2.50464946208309415979 + z * q1;
                q1 = -(1.42182922854787788574 * 0.1) + z * q1;
                q1 = -(3.80806407691578277194 * 0.01) + z * q1;
                q1 = -(9.33259480895457427372 * 0.0001) + z * q1;
                x1 = z * p1 / q1;
            }
            else
            {
                p2 = 3.23774891776946035970;
                p2 = 6.91522889068984211695 + z * p2;
                p2 = 3.93881025292474443415 + z * p2;
                p2 = 1.33303460815807542389 + z * p2;
                p2 = 2.01485389549179081538 * 0.1 + z * p2;
                p2 = 1.23716634817820021358 * 0.01 + z * p2;
                p2 = 3.01581553508235416007 * 0.0001 + z * p2;
                p2 = 2.65806974686737550832 * 0.000001 + z * p2;
                p2 = 6.23974539184983293730 * 0.000000001 + z * p2;
                q2 = 1;
                q2 = 6.02427039364742014255 + z * q2;
                q2 = 3.67983563856160859403 + z * q2;
                q2 = 1.37702099489081330271 + z * q2;
                q2 = 2.16236993594496635890 * 0.1 + z * q2;
                q2 = 1.34204006088543189037 * 0.01 + z * q2;
                q2 = 3.28014464682127739104 * 0.0001 + z * q2;
                q2 = 2.89247864745380683936 * 0.000001 + z * q2;
                q2 = 6.79019408009981274425 * 0.000000001 + z * q2;
                x1 = z * p2 / q2;
            }
            x = x0 - x1;
            if (code != 0)
            {
                x = -x;
            }
            result = x;
            return result;
        }

        /* Complementary error function
         1 - erf(x) =

                                  inf.
                                    -
                         2         | |          2
          erfc(x)  =  --------     |    exp( - t  ) dt
                      sqrt(pi)   | |
                                  -
                                   x


        For small x, erfc(x) = 1 - erf(x); otherwise rational
        approximations are computed.


        ACCURACY:

                             Relative error:
        arithmetic   domain     # trials      peak         rms
           IEEE      0,26.6417   30000       5.7e-14     1.5e-14

        Cephes Math Library Release 2.8:  June, 2000
        Copyright 1984, 1987, 1988, 1992, 2000 by Stephen L. Moshier
        *************************************************************************/
        public static double errorfunctionc(double x)
        {
            double result = 0;
            double p = 0;
            double q = 0;

            if ((double)(x) < (double)(0))
            {
                result = 2 - errorfunctionc(-x);
                return result;
            }
            if ((double)(x) < (double)(0.5))
            {
                result = 1.0 - errorfunction(x);
                return result;
            }
            if ((double)(x) >= (double)(10))
            {
                result = 0;
                return result;
            }
            p = 0.0;
            p = 0.5641877825507397413087057563 + x * p;
            p = 9.675807882987265400604202961 + x * p;
            p = 77.08161730368428609781633646 + x * p;
            p = 368.5196154710010637133875746 + x * p;
            p = 1143.262070703886173606073338 + x * p;
            p = 2320.439590251635247384768711 + x * p;
            p = 2898.0293292167655611275846 + x * p;
            p = 1826.3348842295112592168999 + x * p;
            q = 1.0;
            q = 17.14980943627607849376131193 + x * q;
            q = 137.1255960500622202878443578 + x * q;
            q = 661.7361207107653469211984771 + x * q;
            q = 2094.384367789539593790281779 + x * q;
            q = 4429.612803883682726711528526 + x * q;
            q = 6089.5424232724435504633068 + x * q;
            q = 4958.82756472114071495438422 + x * q;
            q = 1826.3348842295112595576438 + x * q;
            result = Math.Exp(-Math.Pow(x, 2)) * p / q;
            return result;
        }

        /* Error function
        The integral is

                                  x
                                   -
                        2         | |          2
          erf(x)  =  --------     |    exp( - t  ) dt.
                     sqrt(pi)   | |
                                 -
                                  0

        For 0 <= |x| < 1, erf(x) = x * P4(x**2)/Q5(x**2); otherwise
        erf(x) = 1 - erfc(x).


        ACCURACY:

                             Relative error:
        arithmetic   domain     # trials      peak         rms
           IEEE      0,1         30000       3.7e-16     1.0e-16

        Cephes Math Library Release 2.8:  June, 2000
        Copyright 1984, 1987, 1988, 1992, 2000 by Stephen L. Moshier
        *************************************************************************/
        public static double errorfunction(double x)
        {
            double result = 0;
            double xsq = 0;
            double s = 0;
            double p = 0;
            double q = 0;

            s = Math.Sign(x);
            x = Math.Abs(x);
            if ((double)(x) < (double)(0.5))
            {
                xsq = x * x;
                p = 0.007547728033418631287834;
                p = 0.288805137207594084924010 + xsq * p;
                p = 14.3383842191748205576712 + xsq * p;
                p = 38.0140318123903008244444 + xsq * p;
                p = 3017.82788536507577809226 + xsq * p;
                p = 7404.07142710151470082064 + xsq * p;
                p = 80437.3630960840172832162 + xsq * p;
                q = 0.0;
                q = 1.00000000000000000000000 + xsq * q;
                q = 38.0190713951939403753468 + xsq * q;
                q = 658.070155459240506326937 + xsq * q;
                q = 6379.60017324428279487120 + xsq * q;
                q = 34216.5257924628539769006 + xsq * q;
                q = 80437.3630960840172826266 + xsq * q;
                result = s * 1.1283791670955125738961589031 * x * p / q;
                return result;
            }
            if ((double)(x) >= (double)(10))
            {
                result = s;
                return result;
            }
            result = s * (1 - errorfunctionc(x));
            return result;
        }

        /// <summary>
        /// Returns the r-Squared linear Pearson product moment correlation coefficient of two int arrays.
        /// This uses the same implementation used by Excel
        /// </summary>
        /// <param name="array1"></param>
        /// <param name="array2"></param>
        /// <returns></returns>
        public static double RSquared(int[] array1, int[] array2)
        {
            double[] normRaisedCountIdeal = new double[array1.Length];
            double normIdeal = array1.Sum();
            double normGenetic = array2.Sum();

            double geneticBinMean = normGenetic / array2.Length;

            for (int i = 0; i < normRaisedCountIdeal.Length; i++)
                normRaisedCountIdeal[i] = (double)array1[i] / (normIdeal / normGenetic);

            double idealBinMean = normRaisedCountIdeal.Sum() / normRaisedCountIdeal.Length;

            //Implementation of the linear Pearson product moment correlation coefficient, booom!
            //We might be better off using a non-linear regression correlation coefficient instead but I'm don't know any good ones
            double sumTop = 0;
            double x2 = 0, y2 = 0;
            for (int i = 0; i < normRaisedCountIdeal.Length; i++)
            {
                sumTop += (array2[i] - geneticBinMean) * (normRaisedCountIdeal[i] - idealBinMean);
                x2 += Math.Pow(array2[i] - geneticBinMean, 2);
                y2 += Math.Pow(normRaisedCountIdeal[i] - idealBinMean, 2);
            }

            return Math.Pow(sumTop / Math.Sqrt(x2 * y2), 2);
        }

        public static double RSquared(double[] array1, int[] array2)
        {
            double[] normRaisedCountIdeal = new double[array1.Length];
            double normIdeal = array1.Sum();
            double normGenetic = array2.Sum();

            double geneticBinMean = normGenetic / array2.Length;

            for (int i = 0; i < normRaisedCountIdeal.Length; i++)
                normRaisedCountIdeal[i] = array1[i] / (normIdeal / normGenetic);

            double idealBinMean = normRaisedCountIdeal.Sum() / normRaisedCountIdeal.Length;

            //Implementation of the linear Pearson product moment correlation coefficient, booom!
            //We might be better off using a non-linear regression correlation coefficient instead but I'm don't know any good ones
            double sumTop = 0;
            double x2 = 0, y2 = 0;
            for (int i = 0; i < normRaisedCountIdeal.Length; i++)
            {
                sumTop += (array2[i] - geneticBinMean) * (normRaisedCountIdeal[i] - idealBinMean);
                x2 += Math.Pow(array2[i] - geneticBinMean, 2);
                y2 += Math.Pow(normRaisedCountIdeal[i] - idealBinMean, 2);
            }

            return Math.Pow(sumTop / Math.Sqrt(x2 * y2), 2);
        }

        public static class Matrices
        {
            public static void LUDecompose(double[,] a, out int[] indx, out double d)
            {
                const double TINY = 1.0e-20;
                int i, imax = 0, j, k;
                double big, dum, sum, temp;

                int n = a.GetLength(0);
                double[] vv = new double[n];
                indx = new int[n];
                d = 1.0;
                for (i = 0; i < n; i++)
                {
                    big = 0.0;
                    for (j = 0; j < n; j++)
                    {
                        if ((temp = Math.Abs(a[i, j])) > big)
                            big = temp;
                    }

                    if (big == 0.0)
                        throw new Exception("Matrix is singular!!!");

                    vv[i] = 1.0 / big;
                }
                for (j = 0; j < n; j++)
                {
                    for (i = 0; i < j; i++)
                    {
                        sum = a[i, j];
                        for (k = 0; k < i; k++)
                            sum -= a[i, k] * a[k, j];

                        a[i, j] = sum;
                    }
                    big = 0.0;
                    for (i = j; i < n; i++)
                    {
                        sum = a[i, j];
                        for (k = 0; k < j; k++)
                            sum -= a[i, k] * a[k, j];

                        a[i, j] = sum;
                        if ((dum = vv[i] * Math.Abs(sum)) >= big)
                        {
                            big = dum;
                            imax = i;
                        }

                    }
                    if (j != imax)
                    {
                        for (k = 0; k < n; k++)
                        {
                            dum = a[imax, k];
                            a[imax, k] = a[j, k];
                            a[j, k] = dum;
                        }
                        d = -d;
                        vv[imax] = vv[j];
                    }
                    indx[j] = imax;
                    if (a[j, j] == 0.0)
                        a[j, j] = TINY;

                    if (j != n - 1)
                    {
                        dum = 1.0 / a[j, j];
                        for (i = j + 1; i < n; i++)
                            a[i, j] *= dum;
                    }
                }
            }

            public static void LUBackSub(double[,] a, int[] indx, double[] b)
            {
                int i, ii = 0, ip, j;
                double sum;

                int n = a.GetLength(0);
                for (i = 0; i < n; i++)
                {
                    ip = indx[i];
                    sum = b[ip];
                    b[ip] = b[i];
                    if (ii != 0)
                    {
                        for (j = ii - 1; j < i; j++)
                            sum -= a[i, j] * b[j];
                    }
                    else
                    {
                        if (sum != 0.0)
                            ii = i + 1;
                    }
                    b[i] = sum;
                }
                for (i = n - 1; i >= 0; i--)
                {
                    sum = b[i];
                    for (j = i + 1; j < n; j++)
                        sum -= a[i, j] * b[j];

                    b[i] = sum / a[i, i];
                }
            }

            public static double[,] InvertMatrix(double[,] a)
            {
                if (a.GetLength(0) != a.GetLength(1))
                    throw new Exception("Cannot invert a non square matrix");

                int dim = a.GetLength(0);
                double[,] inverse = new double[dim, dim];
                double[,] temp = new double[dim, dim];
                double[] v = new double[dim];
                int[] indx;
                double d;

                for (int i = 0; i < dim; i++)
                {
                    for (int j = 0; j < dim; j++)
                        temp[i, j] = a[i, j];
                }

                LUDecompose(temp, out indx, out d);

                for (int i = 0; i < dim; i++)
                {
                    for (int j = 0; j < dim; j++)
                    {
                        if (j == i)
                            v[j] = 1;
                        else
                            v[j] = 0;
                    }

                    LUBackSub(temp, indx, v);

                    for (int j = 0; j < dim; j++)
                        inverse[j, i] = v[j];
                }

                return inverse;
            }

            public static double Determinant(double[,] a)
            {
                if (a.GetLength(0) != a.GetLength(1))
                    throw new Exception("Cannot find determinant for a non square matrix");

                int[] indx;
                double d;

                int dim = a.GetLength(0);
                double[,] temp = new double[dim, dim];

                for (int i = 0; i < dim; i++)
                {
                    for (int j = 0; j < dim; j++)
                        temp[i, j] = a[i, j];
                }

                LUDecompose(temp, out indx, out d);

                for (int i = 0; i < dim; i++)
                    d *= temp[i, i];

                return d;
            }
        }

        #endregion
    }
}
