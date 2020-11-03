using System;
using System.IO;

namespace KFServerPerks.util
{

    public static class Logging
    {
        public static string logFolder = "./logs/";

        public static void Log(string fileName, string text, bool print = true)
        {
            string logtext = $"[{DateTime.Now}] {text}";
            string filePath = logFolder + fileName;

            try
            {
                Directory.CreateDirectory(logFolder);
                File.AppendAllText(filePath, logtext + "\n");
            }
            catch (Exception e)
            {
                Console.WriteLine($"[ERROR] {e.Message}\n{e.StackTrace}");
            }

            if (print)
                Console.WriteLine(logtext);
        }

        public static void Log(string text, bool print = true)
        {
            string date = DateTime.Now.ToShortDateString().Replace('/', '-') + ".log";
            Log(date, text, print);
        }

        public static void Log(object data, bool print = true)
        {
            Log(data.ToString(), false);
            if(print)
                Console.Write(data);
        }
    }

}