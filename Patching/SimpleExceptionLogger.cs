using System;
using System.IO;

namespace Patching
{
    public class SimpleExceptionLogger
    {
        private static string LOG_PATH = "errors.log";

        public static void LogException(Exception e)
        {
            using (var sr = new StreamWriter(LOG_PATH, false))
            {
                sr.WriteLine(DateTime.UtcNow);
                sr.WriteLine(e);
            }
        }
    }
}
