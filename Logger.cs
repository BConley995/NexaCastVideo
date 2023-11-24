using System;
using System.IO;

namespace NexaCastVideo
{
    public static class Logger
    {
        private static string _logFilePath;
        private static object _lockObj = new object();

        public static void Setup(string logFilePath)
        {
            _logFilePath = logFilePath;

            try
            {
                // Ensure the directory exists.
                string dirPath = Path.GetDirectoryName(_logFilePath);
                if (!Directory.Exists(dirPath))
                {
                    Directory.CreateDirectory(dirPath);
                }

                // Log the setup initialization.
                LogInfo("Logger setup initialized.");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Logger setup failed. {ex.Message}");
            }
        }

        public static void LogError(string message, [System.Runtime.CompilerServices.CallerMemberName] string memberName = "")
        {
            var formattedMessage = $"[ERROR] {DateTime.Now} ({memberName}): {message}";
            Log(formattedMessage);
            Console.WriteLine(formattedMessage);
        }

        public static void LogInfo(string message, [System.Runtime.CompilerServices.CallerMemberName] string memberName = "")
        {
            var formattedMessage = $"[INFO] {DateTime.Now} ({memberName}): {message}";
            Log(formattedMessage);
            Console.WriteLine(formattedMessage);
        }

        public static void LogWarning(string message, [System.Runtime.CompilerServices.CallerMemberName] string memberName = "")
        {
            var formattedMessage = $"[WARNING] {DateTime.Now} ({memberName}): {message}";
            Log(formattedMessage);
            Console.WriteLine(formattedMessage);
        }

        private static void Log(string message)
        {
            try
            {
                lock (_lockObj)
                {
                    File.AppendAllText(_logFilePath, message + Environment.NewLine);
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Failed to log message. {ex.Message}");
            }
        }
    }
}
