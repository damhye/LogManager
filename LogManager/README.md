# LogManager

A C# multi-logger management library that lets you create multiple independent loggers and flexibly configure log file formats, paths, retention periods, and more.

---

## Table of Contents

- [Quick Start](#quick-start)
- [Default Settings](#default-settings)
- [API Reference](#api-reference)
  - [Logger Lifecycle](#logger-lifecycle)
  - [Path Configuration](#path-configuration)
  - [Log Item Configuration](#log-item-configuration)
  - [File Store Type](#file-store-type)
  - [Path Type](#path-type)
  - [Additional Settings](#additional-settings)
  - [Writing Logs](#writing-logs)
- [Enum Definitions](#enum-definitions)

---

## Quick Start

```csharp
// 1. Import namespace
using LogLib;

// 2. Declare Logger IDs
public enum LoggerId
{
    Event     = 1,
    UserEvent = 2,
    Test      = 3,
    Default   = 99
}

// 3. Create and configure loggers
LogManager.Instance.CreateLogger(LoggerId.Default, "SystemLog");
LogManager.Instance.SetRootPath(LoggerId.Default, @"D:\Log\GUI\");
LogManager.Instance.SetLogItem(LoggerId.Default,
    LogItem.Time | LogItem.NewLine | LogItem.Blank1 | LogItem.Blank2);
LogManager.Instance.StartLogger(LoggerId.Default);

LogManager.Instance.CreateLogger(LoggerId.Test, "TestLog");
LogManager.Instance.SetStoreType(LoggerId.Test, LogFileType.Capacity);
LogManager.Instance.SetCapacity(LoggerId.Test, 0.0001f); // 0.0001 MB = 100 bytes

// 4. Start all loggers
LogManager.Instance.Start();

// 5. Write logs
LogManager.LogOut(LoggerId.Default, "started: {0}", DateTime.Now);
LogManager.LogErr(LoggerId.Default, "error occurred: {0}", ex.Message);

// 6. Shut down
LogManager.Instance.Dispose();
```

---

## Default Settings

| Setting | Default Value |
|---|---|
| `LogItem` | `Time \| Filename \| LineNumber \| Level \| NewLine` |
| `StoreType` | `LogFileType.Hour` → `20130405(21)-LoggerName.log` |
| `PathType` | `LogPathType.NameDay` → `D:\Log\LoggerName\20130312\test.log` |
| `Capacity` | `5 MB` (ignored unless `LogFileType.Capacity` is set) |
| `StoragePeriod` | `90 days` |
| `RootPath` | `D:\Log` |
| `Extension` | `log` |
| `MinimumLogLevel` | `LogLevel.Info` |

---

## API Reference

### Logger Lifecycle

#### `CreateLogger(object id, string name)`
Creates a new logger instance.

| Parameter | Description |
|---|---|
| `id` | Logger ID. Accepts numeric or enum types (enum recommended). |
| `name` | Logger name. Used as an identifier and in file/folder naming. |

```csharp
LogManager.Instance.CreateLogger(LoggerId.Default, "SystemLog");
```

---

#### `StartLogger(object id)`
Starts the thread for the specified logger (begins logging).

#### `StopLogger(object id)`
Stops the thread for the specified logger.

#### `Start()`
Starts threads for **all** created loggers.

#### `Stop()`
Stops threads for **all** loggers.

#### `Dispose()`
Releases the LogManager instance and all associated resources.

---

### Path Configuration

#### `SetRootPath(object id, string path)`
Sets the root path for log files. Subdirectories are created under this path based on the configured `PathType`.

```csharp
LogManager.Instance.SetRootPath(LoggerId.Default, @"D:\Log\GUI\");
// Default if not set: "D:\Log\"
```

#### `SetExtension(object id, string ext)`
Sets the file extension for log files.

```csharp
LogManager.Instance.SetExtension(LoggerId.Default, "txt");
```

---

### Log Item Configuration

#### `SetLogItem(object id, LogItem item)`
Configures which metadata fields are included alongside the log message. Combine flags as needed.

```csharp
LogManager.Instance.SetLogItem(LoggerId.Default,
    LogItem.Time | LogItem.NewLine | LogItem.Blank1 | LogItem.Blank2);
```

---

### File Store Type

#### `SetStoreType(object id, LogFileType type)`
Sets the naming format for log files.

```csharp
LogManager.Instance.SetStoreType(LoggerId.Test, LogFileType.Capacity);
```

#### `SetCapacity(object id, double capacity)`
Sets the maximum file size when using `LogFileType.Capacity`.  
Unit: MB (e.g., `1.2` → 1.2 MB = 1,200,000 bytes)

> ⚠️ `SetStoreType` must be called with `LogFileType.Capacity` before calling this method.

```csharp
LogManager.Instance.SetCapacity(LoggerId.Test, 0.0001f); // ~100 bytes
```

---

### Path Type

#### `SetPathType(object id, LogPathType type)`
Sets the directory structure format for log files.

```csharp
LogManager.Instance.SetPathType(LoggerId.Default, LogPathType.NameDay);
```

#### `SetSubDir(object id, string dir)`
Sets a custom subdirectory path when using `LogPathType.Specified`.

> ⚠️ `SetPathType` must be called with `LogPathType.Specified` before calling this method.

---

### Additional Settings

#### `SetStoragePeriod(object id, int days)`
Sets the log retention period in days. Files older than this are automatically deleted.

```csharp
LogManager.Instance.SetStoragePeriod(LoggerId.Default, 30);
```

#### `SetHeader(object id, string header)`
Sets a header string written at the top of each log file.

> ⚠️ `LogItem.Header` must be included in `SetLogItem` before calling this method.

#### `SetFileNameWithoutExtension(object id, string file)`
Sets a custom file name (without extension) when using `LogFileType.Specified`.

> ⚠️ `SetStoreType` must be called with `LogFileType.Specified` before calling this method.

---

### Writing Logs

#### `Log(object id, LogLevel level, string fmt, params object[] args)`
Writes a log entry at the specified level.

```csharp
LogManager.Log(LoggerId.Default, LogLevel.Warning, "value: {0}", 42);
```

#### Convenience Methods

| Method | Equivalent |
|---|---|
| `LogOut(id, fmt, args)` | `Log(id, LogLevel.Info, ...)` |
| `LogErr(id, fmt, args)` | `Log(id, LogLevel.Error, ...)` |
| `LogWar(id, fmt, args)` | `Log(id, LogLevel.Warning, ...)` |
| `LogDbg(id, fmt, args)` | `Log(id, LogLevel.Debug, ...)` |

```csharp
LogManager.LogOut(LoggerId.Default, "started: {0}", DateTime.Now);
LogManager.LogErr(LoggerId.Default, "failed with code {0}", errorCode);
LogManager.LogWar(LoggerId.Default, "retrying... attempt {0}", retryCount);
LogManager.LogDbg(LoggerId.Default, "debug value: {0}", debugVal);
```

---

## Enum Definitions

### `LogItem` — Log Metadata Flags

```csharp
[Flags]
public enum LogItem : ushort
{
    Filename   = 0x001,  // Source file name
    LineNumber = 0x002,  // Source line number
    Function   = 0x004,  // Function/method name
    Date       = 0x008,  // Date
    Time       = 0x010,  // Time
    Level      = 0x080,  // Log level
    NewLine    = 0x100,  // New line after each entry
    Header     = 0x200,  // File header
    Blank1     = 0x400,  // Space after timestamp
    Blank2     = 0x800,  // Space before log message
    Comma      = 0x1000, // Comma separator
}
```

### `LogFileType` — File Naming Format

| Value | Example Filename |
|---|---|
| `Hour` *(default)* | `20130405(21)-LoggerName.log` |
| `Day` | `20130405-LoggerName.log` |
| `Capacity` | `20130405(21)-LoggerName-01.log` |
| `HalfDay` | `20130405(AM)-LoggerName.log` |
| `Specified` | User-defined filename |

### `LogPathType` — Directory Structure

| Value | Example Path |
|---|---|
| `Day` | `D:\Log\20130312\test.log` |
| `Month` | `D:\Log\2013-09\test.log` |
| `Name` | `D:\Log\LoggerName\test.log` |
| `Root` | `D:\Log\test.log` |
| `DayName` | `D:\Log\20130312\LoggerName\test.log` |
| `NameMonth` | `D:\Log\LoggerName\2013-12\test.log` |
| `NameDay` *(default)* | `D:\Log\LoggerName\20130312\test.log` |
| `Specified` | `D:\Log\custom-subpath\test.log` |

### `LogLevel` — Log Levels

```csharp
public enum LogLevel
{
    Debug,    // Verbose diagnostic information
    Info,     // General information (default minimum level)
    Warning,  // Non-critical issues
    Error     // Errors and failures
}
```
