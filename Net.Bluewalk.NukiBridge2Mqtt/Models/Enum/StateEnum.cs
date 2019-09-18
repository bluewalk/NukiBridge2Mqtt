namespace Net.Bluewalk.NukiBridge2Mqtt.Models.Enum
{
    /// <summary>
    /// State of the device
    /// </summary>
    public enum StateEnum
    {
        Uncalibrated = 0,
        Untrained = 0,
        Locked = 1,
        Online = 1,
        Unlocking = 2,
        Unlocked = 3,
        RtoActive = 3,
        Locking = 4,
        Unlatched = 5,
        Open = 5,
        UnlockedLockNGo = 6,
        Unlatching = 7,
        Opening = 7,
        BootRun = 253,
        MotorBlocked = 254,
        Undefined = 255,
    }
}
