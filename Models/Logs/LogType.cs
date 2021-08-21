using System;
using System.Collections.Generic;
using System.Text;

namespace Pete.Models.Logs
{
    public enum EntryLogType : byte
    {
        Create = 0,
        Edit = 1,
        View = 2,
        Delete = 3,
    }

    public enum TamperType : byte
    {
        LogWiped = 0,
        LogRestored = 1,
        EvasionAttempt = 2,
    }

    public enum LogType : byte
    {
        Entry = 0,

        Login = 1,
        FailedLogin = 2,
        Register = 3,

        TamperAttempt = 4,
        Cleanup = 5,

        WarningsSeen = 6,

        Reserved = 7,
    }
}
