namespace Net.Bluewalk.NukiBridge2Mqtt
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
}
