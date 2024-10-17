using System;
using System.IO;
using System.Threading;

public enum LogLevel
{
    INFO,
    WARNING,
    ERROR
}

public sealed class Logger
{
    private static Logger instance = null;
    private static readonly object lockObj = new object();
    private LogLevel currentLogLevel = LogLevel.INFO;
    private string logFilePath = "log.txt";
    private long maxFileSize = 1024 * 1024; // 1MB
    private string logDirectory = "logs";

    private Logger()
    {
        LoadConfig();
    }

    public static Logger GetInstance()
    {
        if (instance == null)
        {
            lock (lockObj)
            {
                if (instance == null)
                {
                    instance = new Logger();
                }
            }
        }
        return instance;
    }

    public void SetLogLevel(LogLevel level)
    {
        lock (lockObj)
        {
            currentLogLevel = level;
        }
    }

    public void Log(string message, LogLevel level)
    {
        if (level < currentLogLevel)
            return;

        lock (lockObj)
        {
            RotateLogFile();
            using (StreamWriter writer = new StreamWriter(logFilePath, true))
            {
                writer.WriteLine($"{DateTime.Now} [{level}] {message}");
            }
            Console.WriteLine($"{DateTime.Now} [{level}] {message}");
        }
    }

    private void RotateLogFile()
    {
        if (!Directory.Exists(logDirectory))
        {
            Directory.CreateDirectory(logDirectory);
        }

        if (File.Exists(logFilePath) && new FileInfo(logFilePath).Length > maxFileSize)
        {
            string archivePath = Path.Combine(logDirectory, $"log_{DateTime.Now:yyyyMMddHHmmss}.txt");
            File.Move(logFilePath, archivePath);
        }
    }

    private void LoadConfig()
    {
        // For simplicity, just hardcode the configuration. In real scenario, load from JSON or XML.
        logFilePath = "log.txt";
        currentLogLevel = LogLevel.INFO;
    }
}

public class LogReader
{
    public static void ReadLogs(LogLevel level)
    {
        if (!File.Exists("log.txt"))
            return;

        using (StreamReader reader = new StreamReader("log.txt"))
        {
            string line;
            while ((line = reader.ReadLine()) != null)
            {
                if (line.Contains($"[{level}]"))
                {
                    Console.WriteLine(line);
                }
            }
        }
    }

    public static void ReadLogsByDate(DateTime start, DateTime end)
    {
        if (!File.Exists("log.txt"))
            return;

        using (StreamReader reader = new StreamReader("log.txt"))
        {
            string line;
            while ((line = reader.ReadLine()) != null)
            {
                var logDate = DateTime.Parse(line.Substring(0, 19));
                if (logDate >= start && logDate <= end)
                {
                    Console.WriteLine(line);
                }
            }
        }
    }
}

class Program
{
    static void Main(string[] args)
    {
        Logger logger = Logger.GetInstance();

        Thread t1 = new Thread(() =>
        {
            logger.Log("Information message", LogLevel.INFO);
            logger.Log("Warning message", LogLevel.WARNING);
            logger.Log("Error message", LogLevel.ERROR);
        });

        Thread t2 = new Thread(() =>
        {
            logger.SetLogLevel(LogLevel.WARNING);
            logger.Log("Another warning message", LogLevel.WARNING);
            logger.Log("Another error message", LogLevel.ERROR);
        });

        t1.Start();
        t2.Start();

        t1.Join();
        t2.Join();

        Console.WriteLine("Logs filtered by level WARNING:");
        LogReader.ReadLogs(LogLevel.WARNING);

        Console.WriteLine("Logs from the last minute:");
        LogReader.ReadLogsByDate(DateTime.Now.AddMinutes(-1), DateTime.Now);
    }
}
