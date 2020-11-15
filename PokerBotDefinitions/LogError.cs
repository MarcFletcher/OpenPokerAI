using System;
using System.Diagnostics;
using System.Threading;

namespace PokerBot.Definitions
{
    public static class LogError
    {
        static object errorLocker = new object();
        public static string Log(Exception ex, string fileAppendStr, string optionalCommentStr="")
        {
            string fileName;

            lock (errorLocker)
            {
                fileName = fileAppendStr + " " + DateTime.Now.Hour.ToString() + "." + DateTime.Now.Minute.ToString() + "." + DateTime.Now.Second.ToString() + "." + DateTime.Now.Millisecond.ToString() + " " + DateTime.Now.ToString("dd-MM-yyyy" + " [" + Process.GetCurrentProcess().Id + "-" + Thread.CurrentContext.ContextID + "]");
                try
                {
                    using (System.IO.StreamWriter sw = new System.IO.StreamWriter(fileName + ".txt", false))
                    {
                        if (optionalCommentStr != "")
                        {
                            sw.WriteLine("Comment: " + optionalCommentStr);
                            sw.WriteLine("");
                        }

                        if (ex.GetBaseException() != null)
                            sw.WriteLine("Base Exception Type: " + ex.GetBaseException().ToString());

                        if (ex.InnerException != null)
                            sw.WriteLine("Inner Exception Type: " + ex.InnerException.ToString());

                        if (ex.StackTrace != null)
                        {
                            sw.WriteLine("");
                            sw.WriteLine("Stack Trace: " + ex.StackTrace.ToString());
                        }
                    }
                }
                catch (Exception)
                {
                    //For possible acts of god!
                }
            }

            return fileName;
        }

        /// <summary>
        /// Appends provided logString to end of fileName.txt
        /// </summary>
        /// <param name="fileName"></param>
        /// <param name="logString"></param>
        public static void AppendStringToLogFile(string fileName, string logString)
        {
            try
            {
                lock (errorLocker)
                {
                    using (System.IO.StreamWriter sw = new System.IO.StreamWriter(fileName + ".txt", true))
                        sw.WriteLine(logString);
                }
            }
            catch (Exception)
            {
                //Incase the file is open ;|
            }
        }
    }
}
