using System;
using System.IO;


namespace HandshakeEmulator.DataStructures
{
    public class Log
    {
        private const string LogFilePath = @"C:\LogFiles\HandshakeEmulator\Log.log";
        public string Equipment;
        public string Message;
        public string Level;
        public DateTime Timestamp;

        public Log(string equip = "", string level = "DEBUG", string msg = "undefined")
        {
            Equipment = equip;
            Message = msg;
            Level = level;
            Timestamp = DateTime.Now;
            WriteToFile(msg);
        }

        private static void WriteToFile(string msg="")
        {
            try
            {
                // Check if the log file is larger than a fixed size, then archive it
                if (new FileInfo(LogFilePath).Length > 1000000)
                    File.Move(LogFilePath, @"C:\LogFiles\HandshakeEmulator\" + DateTime.Now.ToString("yyyyMMdd") + ".log");

                // Write the string to a file.append mode is enabled so that the log
                // lines get appended to test.txt than wiping content and writing the log
                using (StreamWriter writeFile = new StreamWriter(LogFilePath, true))
                {
                    writeFile.WriteLine("[" + DateTime.Now + "] " + msg);
                    writeFile.Close();
                }
            }
            catch (Exception e)
            {
                Console.WriteLine(@"Error writing log to file: " + e.Message);
                Console.Read();
            }
        }
    }
}
