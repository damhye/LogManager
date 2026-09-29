//	Logger.cs - Implementation of the Logger class
//
// Copyright 2013. Kwangjin Hong(damhyett@gmail.com)
//
// SPDX-License-Identifier: MIT
//
// This software is released under the MIT License.
// https://opensource.org
//

using System.Collections.Concurrent;
using System.Text.RegularExpressions;

namespace LogLib
{
    public class Logger : IDisposable
    {
        #region IDisposable Support
        private bool _disposed = false;

        ~Logger()
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
                    if (IsEnabled())
                        Enable(false);
                    _queue.Dispose();
                }
                _disposed = true;
            }
        }
        #endregion

        public Logger(string name)
        {
            _logItem = LogItem.Time | LogItem.Filename | LogItem.LineNumber | LogItem.Level | LogItem.NewLine;
            _storeType = LogFileType.Hour;
            _pathType = LogPathType.NameDay;
            _capacity = 5 * 1024 * 1024;  // default 5MB
            _storagePeriod = 90;  // default 90 days
            _enabled = false;
            _name = name;
        }

        private readonly BlockingCollection<string> _queue = new();
        private Task? _worker = null;

        private bool _enabled;

        private string _rootPath = @"D:\Log";
        private string _subDir = string.Empty;
        private string _ext = "log";
        private string _stem = string.Empty;
        private string _header = string.Empty;
        private string _name = "Info";

        private LogItem _logItem;
        private nint _capacity;  // in bytes
        private int _storagePeriod;  // in days
        private LogFileType _storeType;
        private LogPathType _pathType;

        private readonly Dictionary<string, int> _lastFileNumberCache = new();

        private DateTime _now;

        private void Stop()
        {
            _queue.CompleteAdding();   // 더 이상 Add 금지
            _worker?.Wait();           // 남은 메시지 모두 처리 후 종료
        }

        public void Enable(bool enable)
        {
            _enabled = enable;
            if (!enable)
            {
                Stop();
            }
        }
        public bool IsEnabled()
        {
            return _enabled;
        }

        public void SetExtension(string ext)
        {
            _ext = ext;
        }
        public void SetFileNameWithoutExtension(string name)
        {
            _stem = name;
        }
        public void SetRootPath(string path)
        {
            _rootPath = path;
        }
        public void SetSubDir(string subDir)
        {
            this._subDir = subDir;
        }
        public void SetName(string name)
        {
            this._name = name;
        }
        public void SetHeader(string header)
        {
            this._header = header;
        }
        public void SetLogItem(LogItem item)
        {
            _logItem = item;
        }
        public LogItem GetLogItem()
        {
            return _logItem;
        }
        public string GetRootPath()
        {
            return _rootPath;
        }
        public string GetSubDir()
        {
            return _subDir;
        }
        public string GetName()
        {
            return _name;
        }
        public string GetExtension()
        {
            return _ext;
        }
        public LogFileType GetStoreType()
        {
            return _storeType;
        }
        public LogPathType GetPathType()
        {
            return _pathType;
        }
        public void SetStoragePeriod(int days)
        {
            _storagePeriod = days;
        }
        public void SetStoreType(LogFileType type)
        {
            _storeType = type;
        }
        public void SetPathType(LogPathType type)
        {
            _pathType = type;
        }
        public void SetCapacity(double capacity/* in MB */)
        {
            _capacity = (nint)(capacity * 1024 * 1024);
        }

        public bool Run()
        {
            if (!_enabled)
                return false;

            if (_worker != null)
                return false;
            _worker = Task.Factory.StartNew(ProcessQueue, TaskCreationOptions.LongRunning);

            return true;
        }

        public void Enqueue(string msg)
        {
            if (_queue.IsAddingCompleted)  // Dispose 여부 확인
                return;

            try
            {
                _queue.Add(msg);
            }
            catch (InvalidOperationException)
            {
                // CompleteAdding 직후 타이밍 충돌 방어
            }
        }

        private void ProcessQueue()
        {
            foreach (string msg in _queue.GetConsumingEnumerable())
            {
                WriteLog(msg);
            }
        }

        private int WriteLog(string msg)
        {
            try
            {
                _now = DateTime.Now;
                string fullPath = Path.Combine(GetDirectoryName(), GetFileName());

                if (!CreateDirectoryIfNeeded(fullPath))
                    return -1;

                for (int attempt = 0; attempt < 2; attempt++)
                {
                    try
                    {
                        using (var fs = new FileStream(fullPath, FileMode.Append, FileAccess.Write, FileShare.ReadWrite))
                        {
                            using (var writer = new StreamWriter(fs))
                            {

                                if (_logItem.HasFlag(LogItem.Header) && !string.IsNullOrEmpty(_header) && fs.Length == 0)
                                {
                                    writer.Write(_header);
                                }

                                writer.Write(msg);
                                writer.Flush();
                            }
                        }
                        return 0;
                    }
                    catch (IOException)
                    {
                        if (attempt == 0)
                            Thread.Sleep(5);
                    }
                }

                return -1;
            }
            catch
            {
                return -1;
            }
        }

        private bool CreateDirectoryIfNeeded(string path)
        {
            try
            {
                string? dir = Path.GetDirectoryName(path);

                if (string.IsNullOrEmpty(dir))
                    return true;

                if (!Directory.Exists(dir))
                    Directory.CreateDirectory(dir);

                return true;
            }
            catch
            {
                return false;
            }
        }

        private string GetDirectoryName()
        {
            string sub = "";

            switch (_pathType)
            {
                case LogPathType.Specified:
                    sub = _subDir;
                    break;

                case LogPathType.Day:
                    sub = _now.ToString("yyyyMMdd");
                    break;

                case LogPathType.Month:
                    sub = _now.ToString("yyyy-MM");
                    break;

                case LogPathType.Name:
                    sub = _name;
                    break;

                case LogPathType.Root:
                    sub = "";
                    break;

                case LogPathType.DayName:
                    sub = Path.Combine(_now.ToString("yyyyMMdd"), _name);
                    break;

                case LogPathType.NameMonth:
                    sub = Path.Combine(_name, _now.ToString("yyyy-MM"));
                    break;

                case LogPathType.NameDay:
                    sub = Path.Combine(_name, _now.ToString("yyyyMMdd"));
                    break;
            }

            return string.IsNullOrEmpty(sub)
                ? _rootPath
                : Path.Combine(_rootPath, sub);
        }

        private string GetFileName()
        {
            switch (_storeType)
            {
                case LogFileType.Specified:
                    return _stem;

                case LogFileType.HalfDay:
                    return $"{_now:yyyyMMdd}({(_now.Hour >= 12 ? "PM" : "AM")})-{_name}.{_ext}";

                case LogFileType.Day:
                    return $"{_now:yyyyMMdd}-{_name}.{_ext}";

                case LogFileType.Hour:
                    return $"{_now:yyyyMMdd(HH)}-{_name}.{_ext}";

                case LogFileType.Capacity:
                    {
                        string cacheKey = $"{_now:yyyyMMdd(HH)}-{_name}";

                        if (!_lastFileNumberCache.TryGetValue(cacheKey, out int num))
                        {
                            num = GetLastFileNumber(cacheKey);
                            _lastFileNumberCache[cacheKey] = num;
                        }

                        while (true)
                        {
                            string file = $"{cacheKey}-{num:00}.{_ext}";
                            string full = Path.Combine(GetDirectoryName(), file);

                            if (NeedCreateNew(full))
                            {
                                num++;
                                _lastFileNumberCache[cacheKey] = num;
                                continue;
                            }

                            return file;
                        }
                    }

                default:
                    return $"{_now:yyyyMMdd(HH)}-{_name}.{_ext}";
            }
        }

        private int GetLastFileNumber(string cacheKey)
        {
            int max = 1;
            string dir = GetDirectoryName();

            if (!Directory.Exists(dir))
                return max;

            foreach (string file in Directory.EnumerateFiles(dir))
            {
                string name = Path.GetFileNameWithoutExtension(file);

                if (name.StartsWith(cacheKey))
                {
                    int pos = name.LastIndexOf('-');
                    if (pos >= 0)
                    {
                        if (int.TryParse(name[(pos + 1)..], out int num))
                            max = Math.Max(max, num);
                    }
                }
            }

            return max;
        }

        private bool NeedCreateNew(string filename)
        {
            try
            {
                if (!File.Exists(filename))
                    return false;

                long size = new FileInfo(filename).Length;
                return size >= _capacity;
            }
            catch
            {
                return false;
            }
        }

        public int DeleteExpiredFile(string folder)
        {
            try
            {
                if (!Directory.Exists(folder))
                    return -1;

                foreach (string file in Directory.EnumerateFiles(folder))
                {
                    string name = Path.GetFileName(file);

                    if (string.IsNullOrEmpty(name) || name.StartsWith("."))
                        continue;

                    CheckExpiredByCreatedTime(file);
                }

                return 0;
            }
            catch
            {
                return -1;
            }
        }

        public int GetDiffDaysFromNow(int year, int month, int day)
        {
            try
            {
                DateTime created = new DateTime(year, month, day, 12, 0, 0, DateTimeKind.Local);
                return (DateTime.Now - created).Days;
            }
            catch
            {
                return int.MaxValue;
            }
        }

        public void CheckExpiredByFileName(string filename)
        {
            try
            {
                string name = Path.GetFileName(filename);

                int year = int.Parse(name.Substring(0, 4));
                int month = int.Parse(name.Substring(4, 2));
                int day = int.Parse(name.Substring(6, 2));

                if (GetDiffDaysFromNow(year, month, day) > _storagePeriod)
                    File.Delete(filename);
            }
            catch
            {
            }
        }

        public void CheckExpiredByCreatedTime(string filename)
        {
            try
            {
                DateTime last = File.GetLastWriteTime(filename);
                int diffDays = (DateTime.Now - last).Days;

                if (diffDays > _storagePeriod)
                    File.Delete(filename);
            }
            catch
            {
            }
        }

        public bool DeleteDirectory(string dir)
        {
            try
            {
                if (!Directory.Exists(dir))
                    return false;

                Directory.Delete(dir, true);
                return true;
            }
            catch
            {
                return false;
            }
        }

        public void DeleteExpiredDayDir(string path)
        {
            try
            {
                if (!Directory.Exists(path))
                    return;

                foreach (string dir in Directory.EnumerateDirectories(path))
                {
                    string name = Path.GetFileName(dir);

                    if (string.IsNullOrEmpty(name) || name.StartsWith("."))
                        continue;

                    if (!Regex.IsMatch(name, @"^\d{8}$"))
                        continue;

                    int year = int.Parse(name.Substring(0, 4));
                    int month = int.Parse(name.Substring(4, 2));
                    int day = int.Parse(name.Substring(6, 2));

                    if (GetDiffDaysFromNow(year, month, day) > _storagePeriod + 1)
                        Directory.Delete(dir, true);
                }
            }
            catch
            {
            }
        }

        public void DeleteExpiredMonthDir(string path)
        {
            try
            {
                if (!Directory.Exists(path))
                    return;

                foreach (string dir in Directory.EnumerateDirectories(path))
                {
                    string name = Path.GetFileName(dir);

                    if (string.IsNullOrEmpty(name) || name.StartsWith("."))
                        continue;

                    if (name.Length < 7)
                        continue;

                    int year = int.Parse(name.Substring(0, 4));
                    int month = int.Parse(name.Substring(5, 2));

                    if (GetDiffDaysFromNow(year, month, 28) > _storagePeriod + 2)
                        Directory.Delete(dir, true);
                }
            }
            catch
            {
            }
        }

        public void DeleteLog()
        {
            string path;

            switch (_pathType)
            {
                case LogPathType.Name:
                case LogPathType.NameDay:
                case LogPathType.NameMonth:
                    path = Path.Combine(_rootPath, _name);
                    break;

                case LogPathType.Specified:
                    path = Path.Combine(_rootPath, _subDir);
                    break;

                default:
                    path = _rootPath;
                    break;
            }

            switch (_pathType)
            {
                case LogPathType.Name:
                case LogPathType.Root:
                case LogPathType.Specified:
                    DeleteExpiredFile(path);
                    break;

                case LogPathType.NameDay:
                case LogPathType.DayName:
                case LogPathType.Day:
                    DeleteExpiredDayDir(path);
                    break;

                case LogPathType.NameMonth:
                case LogPathType.Month:
                    DeleteExpiredMonthDir(path);
                    break;
            }
        }
    }
}
