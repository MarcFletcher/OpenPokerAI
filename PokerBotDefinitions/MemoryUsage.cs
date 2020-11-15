using System;
using System.Diagnostics;

namespace PokerBot.Definitions
{
    public static class MemoryUsage
    {
        /// <summary>
        /// Returns the amount of memory currently being used by the calling application. 
        /// If disableInMono = true returns -1
        /// </summary>
        /// <returns></returns>
        public static double CurrentApplicationMemoryUsageMB()
        {
            //if (!disableInMono || Type.GetType("Mono.Runtime") == null)
                return Math.Abs(Process.GetCurrentProcess().WorkingSet64 / 1048576.0);
            //else
            //    return -1;
        }

        /// <summary>
        /// Returns the total available system memory. Uses the System.Managment interface so only available when run in Windows. 
        /// Will return double.maxValue if error or non windows environment.
        /// </summary>
        /// <returns></returns>
        public static double AvailableSystemMemoryMB()
        {
            if (Type.GetType("Mono.Runtime") == null)
            {
                try
                {
                    System.Management.ObjectQuery wql = new System.Management.ObjectQuery("SELECT * FROM Win32_OperatingSystem");
                    System.Management.ManagementObjectSearcher searcher = new System.Management.ManagementObjectSearcher(wql);
                    System.Management.ManagementObjectCollection results = searcher.Get();

                    double total = 0;

                    foreach (System.Management.ManagementObject result in results)
                    {
                        ulong? value = result["FreePhysicalMemory"] as ulong?;
                        if (value != null) total += (ulong)value;
                    }

                    return total / 1024.0;
                }
                catch (Exception)
                {
                    return double.MaxValue;
                }
            }
            else
                return double.MaxValue;
        }
    }
}
