using System;
using System.Text;
using System.Diagnostics;

namespace SlalomTracker
{
    public static class Logger
    {
        public static void Log(string message)
        {
            InternalLog(message);
        }

        public static void Log(string message, Exception e)
        {
            StringBuilder sb = new StringBuilder();
            sb.AppendLine(message);
            if (e != null)
                sb.Append("\tERROR: " + e.Message);
            if (e.InnerException != null)
                sb.Append("\n\tINNER ERROR: " + e.InnerException.Message);
            
            ConsoleColor defaultColor = Console.ForegroundColor;
            Console.ForegroundColor = ConsoleColor.Red;
            InternalLog(sb.ToString());
            Console.ForegroundColor = defaultColor;
        }

        private static void InternalLog(string message, int stackPosition = 2)
        {
            const string DateFormat = "dd/MMM/yyyy hh:mm:ss.ff zzz";
            StackTrace stackTrace = new StackTrace();

            string method = stackTrace.GetFrame(stackPosition).GetMethod().Name;
            string type = stackTrace.GetFrame(stackPosition).GetMethod().ReflectedType.Name;
            Console.WriteLine($"[{DateTime.Now.ToString(DateFormat)}, {type}:{method}] {message}");            
        }
    }
}