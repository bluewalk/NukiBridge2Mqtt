namespace Net.Bluewalk.NukiBridge2Mqtt.Models.Enum
{
    /// <summary>
    /// State of the device
    ///   [Smartlock]__[Opener]
    /// </summary>
    public enum StateEnum
    {
        Uncalibrated__Untrained = 0,
        Locked__Online = 1,
        Unlocking = 2,
        Unlocked__RtoActive = 3,
        Locking = 4,
        Unlatched__Open = 5,
        UnlockedLockNGo = 6,
        Unlatching__Opening = 7,
        X__BootRun = 253,
        MotorBlocked = 254,
        Undefined = 255,
    }
}
