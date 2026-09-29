//	LogManager.cs - Implementation of the LogManager class
//
// Copyright 2013. Kwangjin Hong(damhyett@gmail.com)
//
// SPDX-License-Identifier: MIT
//
// This software is released under the MIT License.
// https://opensource.org
//

using System.Collections.Concurrent;
using System.Diagnostics;
using System.Text;

namespace LogLib
{
    public class LogManager : IDisposable
    {
        #region IDisposable Support
        private bool _disposed = false;

        ~LogManager()
        {
            Dispose(false);
        }
        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }
        protected virtual void Dispose(bool disposing)
        {
            if (!_disposed)
            {
                if (disposing)
                {
                    Stop();

                    foreach (var logger in _loggers.Values)
                    {
                        logger.Dispose();
                    }
                    _loggers.Clear();
                }
                _disposed = true;
            }
        }
        #endregion

        private static readonly Lazy<LogManager> lazy = new Lazy<LogManager>(() => new LogManager());
        public static LogManager Instance { get { return lazy.Value; } }

        private Dictionary<int, Logger> _loggers = new Dictionary<int, Logger>();
        private LogLevel minimumLogLevel;
        private CancellationTokenSource? cancellationTokenSource = new();
        private Task? taskDeleteExpiredLogs;
        private int logDeleteAt;    // default 5, range 0 ~ 23, exipired log files are deleted at this o'clock every day.

        public LogManager()
        {
            minimumLogLevel = LogLevel.Info;
            logDeleteAt = 5;    // 5 of the clock(o'clock)
        }

        private int WriteLog(int key, LogLevel level, string? file, int line, string? func, string msg)
        {
            if (minimumLogLevel > level)
                return 0;
            if (!_loggers.TryGetValue(key, out Logger? logger) || logger.IsEnabled() == false)
                return -1;

            DateTime now = DateTime.Now;

            StringBuilder sb = new StringBuilder();
            LogItem logItem = logger.GetLogItem();
            if (logItem.HasFlag(LogItem.Date))
                sb.Append(string.Format("{0:yyyy-MM-dd} ", now));
            if (logItem.HasFlag(LogItem.Time))
                sb.Append(string.Format("{0:HH:mm:ss.fff}", now));
            if (logItem.HasFlag(LogItem.Filename))
            {
                string? fileName = Path.GetFileNameWithoutExtension(file);
                sb.Append("-").Append(fileName?.PadRight(30));
            }
            if (logItem.HasFlag(LogItem.LineNumber))
                sb.Append("(")
                    .Append(line.ToString().PadLeft(5))
                    .Append(") ");
            if (logItem.HasFlag(LogItem.Function))
                sb.Append("-")
                    .Append((func ?? "").PadRight(50))
                    .Append(" ");
            if (logItem.HasFlag(LogItem.Comma))
                sb.Append(",");
            if (logItem.HasFlag(LogItem.Blank1))
                sb.Append("    ");
            if (logItem.HasFlag(LogItem.Level))
                sb.Append(GetLogLevelStr(level));
            if (logItem.HasFlag(LogItem.Blank2))
                sb.Append("    ");

            sb.Append(msg);

            if (logItem.HasFlag(LogItem.NewLine))
                sb.AppendLine();

            string logMsg = sb.ToString();
            logger.Enqueue(logMsg);

            return 0;
        }

        private string GetLogLevelStr(LogLevel level)
        {
            return level switch
            {
                LogLevel.Info => "- I - ",
                LogLevel.Error => "- E - ",
                LogLevel.Warning => "- W - ",
                LogLevel.Debug => "- D - ",
                _ => "- I - "
            };
        }
        private Logger GetLogger(int key)
        {
            if (!_loggers.TryGetValue(key, out Logger? logger))
            {
                throw new NullReferenceException("cannot find logger");
            }
            return logger;
        }
        private Logger GetLogger(object id)
        {
            return GetLogger((int) id);
        }
        private void AddLogger(object id, Logger logger)
        {
            AddLogger((int)id, logger);
        }
        private void AddLogger(int key, Logger logger)
        {
            _loggers[key] = logger;
        }
        private void RemoveLogger(object id)
        {
            RemoveLogger((int)id);
        }
        private void RemoveLogger(int key)
        {
            //_loggers.TryRemove(key, out Logger? logger);
            //logger?.Dispose();
            if (_loggers.TryGetValue(key,out Logger? logger))
                logger.Dispose();
            _loggers.Remove(key);
        }

        public int CreateLogger(object id, string name)
        {
            return CreateLogger((int)id, name);
        }
        public int CreateLogger(int key, string name)
        {
            Logger logger = new Logger(name);
            logger.Enable(true);
            AddLogger(key, logger);
            return 0;
        }
        public bool StartLogger(object loggerId)
        {
            return StartLogger((int)loggerId);
        }
        public bool StartLogger(int key)
        {
            Logger logger = GetLogger(key);
            if (logger.IsEnabled() == false)
                logger.Enable(true);
            
            return logger.Run();
        }
        public void StopLogger(object loggerId)
        {
            StopLogger((int)loggerId);
        }
        public void StopLogger(int key)
        {
            GetLogger(key).Enable(false);
            
        }
        public int Start()
        {
            foreach (var logger in _loggers.Values)
            {
                logger.Run();
            }
            taskDeleteExpiredLogs = Task.Run(() => DeleteExpiredLogs(cancellationTokenSource!.Token));
            return 0;
        }
        public int Stop()
        {
            cancellationTokenSource?.Cancel();
            try
            {
                taskDeleteExpiredLogs?.Wait();
            }
            catch (AggregateException ex) when (ex.InnerExceptions.All(e => e is OperationCanceledException))
            {
                // 정상적인 취소 종료
            }
            finally
            {
                cancellationTokenSource?.Dispose();
                cancellationTokenSource = null;
            }

            foreach (var logger in _loggers.Values)
            {
                logger.Enable(false);
            }
            return 0;
        }
        public bool SetRootPath(object loggerId, string root)
        {
            return SetRootPath((int)loggerId, root);
        }
        private bool SetRootPath(int key, string root)
        {
            if (_loggers.TryGetValue(key, out Logger? logger))
            {
                string rootPath = root.TrimEnd('\\');
                logger.SetRootPath(rootPath);
            }
            return true;
        }
        public bool SetExtension(object loggerId, string ext)
        {
            return SetExtension((int)loggerId, ext);
        }
        private bool SetExtension(int key, string ext)
        {
            if (_loggers.TryGetValue(key, out Logger? logger))
            {
                logger.SetExtension(ext);
            }
            return true;
        }
        public bool SetLogItem(object loggerId, LogItem item)
        {
            return SetLogItem((int)loggerId, item);
        }
        private bool SetLogItem(int key, LogItem item)
        {

            if (_loggers.TryGetValue(key, out Logger? logger))
            {
                logger.SetLogItem(item);
            }
            return true;
        }
        public bool SetStoreType(object loggerId, LogFileType type)
        {
            return SetStoreType((int)loggerId, type);
        }
        private bool SetStoreType(int key, LogFileType type)
        {
            if (_loggers.TryGetValue(key, out Logger? logger))
            {
                logger.SetStoreType(type);
            }
            return true;
        }
        public bool SetPathType(object loggerId, LogPathType type)
        {
            return SetPathType((int)loggerId, type);
        }
        private bool SetPathType(int key, LogPathType type)
        {
            if (_loggers.TryGetValue(key, out Logger? logger))
            {
                logger.SetPathType(type);
            }
            return true;
        }
        public bool SetStoragePeriod(object loggerId, int day)
        {
            return SetStoragePeriod((int)loggerId, day);
        }
        private bool SetStoragePeriod(int key, int day)
        {
            if (_loggers.TryGetValue(key, out Logger? logger))
            {
                logger.SetStoragePeriod(day);
            }
            return true;
        }
        public bool Enable(object loggerId, bool enable)
        {
            return Enable((int)loggerId, enable);
        }
        private bool Enable(int key, bool enable)
        {
            if (_loggers.TryGetValue(key, out Logger? logger))
            {
                logger.Enable(enable);
            }
            return true;
        }

        // You must set path type(LogPathType.Specified) before set sub directory(intermediate path)
        public bool SetSubDir(object loggerId, string dir)
        {
            return SetSubDir((int)loggerId, dir);
        }
        private bool SetSubDir(int key, string dir)
        {
            if (_loggers.TryGetValue(key, out Logger? logger))
            {
                if (logger.GetPathType() == LogPathType.Specified)
                {
                    string subDir = dir.TrimEnd('\\');
                    logger.SetSubDir(subDir);
                }
            }

            return true;
        }

        // You must set file type(LogFileType.Specified) before set file name
        public bool SetFileNameWithoutExtension(object loggerId, string file)
        {
            return SetFileNameWithoutExtension((int)loggerId, file);
        }
        private bool SetFileNameWithoutExtension(int key, string file)
        {
            if (_loggers.TryGetValue(key, out Logger? logger))
            {
                if (logger.GetStoreType() == LogFileType.Specified)
                {
                    logger.SetFileNameWithoutExtension(file);
                }
            }
            return true;
        }

        // You must set logitem(LogItem.Header) before set header
        public bool SetHeader(object loggerId, string header)
        {
            return SetHeader((int)loggerId, header);
        }
        private bool SetHeader(int key, string header)
        {
            if (_loggers.TryGetValue(key, out Logger? logger))
            {
                if (logger.GetLogItem().HasFlag(LogItem.Header))
                {
                    string headerStr = header + "\n";
                    logger.SetHeader(headerStr);
                }
            }
            return true;
        }

        // unit: MB,  You must set file type(LogFileType.Capacity) before set capacity
        public bool SetCapacity(object loggerId, double capacity)
        {
            return SetCapacity((int)loggerId, capacity);
        }
        private bool SetCapacity(int key, double capacity)
        {
            if (_loggers.TryGetValue(key, out Logger? logger))
            {
                if (logger.GetStoreType() == LogFileType.Capacity)
                    logger.SetCapacity(capacity);
            }
            return true;
        }

        private async Task DeleteExpiredLogs(CancellationToken token)
        {
            bool deleteLog = true;

            while (!token.IsCancellationRequested)
            {
                DateTime now = DateTime.Now;
                if (now.Hour != LogDeleteAt)
                    deleteLog = true;
                else if (deleteLog)
                {
                    foreach (var logger in _loggers.Values)
                    {
                        if (logger.IsEnabled())
                            logger.DeleteLog();
                    }
                    deleteLog = false;
                }
                try
                {
                    await Task.Delay(2500, token).ConfigureAwait(false);
                }
                catch (OperationCanceledException)
                {
                    break;  // Delay 중 취소되면 즉시 탈출
                }
            }
        }

        private void MakeLog(int key, LogLevel level, string? file, int line, string? func, string fmt, params object?[] args)
        {
            string msg = string.Format(fmt, args);
            WriteLog(key, level, file, line, func, msg);
        }

        public static void LogOut(object loggerId, string fmt, params object?[] args)
        {
            StackFrame frame = new StackFrame(1, true);
            int key = (int)loggerId;
            Instance.MakeLog(key, LogLevel.Info, frame.GetFileName(), frame.GetFileLineNumber(), frame.GetMethod()?.Name, fmt, args);
        }
        public static void LogErr(object loggerId, string fmt, params object?[] args)
        {
            StackFrame frame = new StackFrame(1, true);
            int key = (int)loggerId;
            Instance.MakeLog(key, LogLevel.Error, frame.GetFileName(), frame.GetFileLineNumber(), frame.GetMethod()?.Name, fmt, args);
        }
        public static void LogWar(object loggerId, string fmt, params object?[] args)
        {
            StackFrame frame = new StackFrame(1, true);
            int key = (int)loggerId;
            Instance.MakeLog(key, LogLevel.Warning, frame.GetFileName(), frame.GetFileLineNumber(), frame.GetMethod()?.Name, fmt, args);
        }
        public static void LogDbg(object loggerId, string fmt, params object?[] args)
        {
            StackFrame frame = new StackFrame(1, true);
            int key = (int)loggerId;
            Instance.MakeLog(key, LogLevel.Debug, frame.GetFileName(), frame.GetFileLineNumber(), frame.GetMethod()?.Name, fmt, args);
        }
        public static void Log(object loggerId, LogLevel level, string fmt, params object?[] args)
        {
            StackFrame frame = new StackFrame(1, true);
            int key = (int)loggerId;
            Instance.MakeLog(key, level, frame.GetFileName(), frame.GetFileLineNumber(), frame.GetMethod()?.Name, fmt, args);
        }

        public LogLevel MinimumLogLevel { get => minimumLogLevel; set => minimumLogLevel = value; }
        public int LogDeleteAt { get => logDeleteAt; set => logDeleteAt = value; }
    }
}
