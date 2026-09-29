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

## Install
Just install the [Damhye.LogLib nuget package](https://www.nuget.org/packages/Damhye.LogLib/)
