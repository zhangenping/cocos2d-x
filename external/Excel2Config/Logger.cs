using System;

public enum LogLevel
{
    Debug,
    Info,
    Warning,
    Error,
    WarningEx,
}

public static class Logger
{
    private static LogLevel currentLogLevel = LogLevel.Info; // 默认日志级别

    public static void SetLogLevel(LogLevel level)
    {
        currentLogLevel = level;
    }

    public static void Log(LogLevel level, string message)
    {
        if (level >= currentLogLevel)
        {
            string timestamp = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss");

            if (level == LogLevel.WarningEx)
            {
                string logMessage = $"[warning] {message}";
                Console.Error.WriteLine(logMessage);
            }
            else
            {
                string logMessage = $"[{timestamp}] [{level}] {message}";
                Console.WriteLine(logMessage);
            }
        }
    }

    public static void ForceLog(string message)
    {
        string timestamp = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss");
        string logMessage = $"[{timestamp}] [FORCED] {message}";
        Console.WriteLine(logMessage);
    }

    // 便捷方法
    public static void Debug(string message) => Log(LogLevel.Debug, message);
    public static void Info(string message) => Log(LogLevel.Info, message);
    public static void Warning(string message) => Log(LogLevel.Warning, message);
    public static void Error(string message) => Log(LogLevel.Error, message);
    public static void WarningEx(string message) => Log(LogLevel.WarningEx, message); //用于输出启动器
}