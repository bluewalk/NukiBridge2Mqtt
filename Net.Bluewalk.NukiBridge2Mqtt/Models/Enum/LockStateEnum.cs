namespace Net.Bluewalk.NukiBridge2Mqtt.Models.Enum
{
    public enum LockStateEnum
    {
        Uncalibrated = 0,
        Locked = 1,
        Unlocking = 2,
        Unlocked = 3,
        Locking = 4,
        Unlatched = 5,
        UnlockedLockNGo = 6,
        Unlatching = 7,
        MotorBlocked = 254,
        Undefined = 255,
    }

    public enum LockStateOpenerEnum
    {
        Untrained = 0,
        Online = 1,
        RtoActive = 3,
        Open = 5,
        Opening = 7,
        BootRun = 253,
        Undefined = 255
    }
}
