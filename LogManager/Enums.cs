namespace LogLib
{
    [Flags]
    public enum LogItem : ushort
    {
        Filename = 0x1,
        LineNumber = 0x2,
        Function = 0x4,
        Date = 0x8,
        Time = 0x10,
        ThreadId = 0x20,    // not implemented
        LoggerName = 0x40,  // not implemented
        Level = 0x80,
        NewLine = 0x100,
        Header = 0x200,
        Blank1 = 0x400, // after time
        Blank2 = 0x800, // before main log text
        Comma = 0x1000, // specified item
    }

    public enum LogFileType
    {
        Hour = 0,       // 20130405(21)-test.log
        Day = 1,        // 20130405-test.log
        Capacity = 2,   // 20130405(21)-test-01.log
        HalfDay = 3,    // 20130405(AM)-test.log
        Specified = 4   // Specified Name
    }

    public enum LogPathType
    {                    // eg.
        Day = 0,         // D:\Log\20130312\test.log
        Month = 1,       // D:\Log\09\test.log
        Name = 2,        // D:\Log\Comm\test.log
        Root = 3,        // D:\Log\test.log
        DayName = 4,     // D:\Log\20130312\Comm\test.log
        NameMonth = 5,   // D:\Log\Comm\12\test.log
        NameDay = 6,     // D:\Log\Comm\20130312\test.log
        Specified = 7,   // D:\Log\specified sub path\test.log
        SpecialFormat    // special format
    }

    public enum LogLevel
    {
        Debug,
        Info,
        Warning,
        Error
    }
}
