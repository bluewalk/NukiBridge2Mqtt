namespace Net.Bluewalk.NukiBridge2Mqtt.Models.Enum
{
    public enum LockActionEnum
    {
        Unspecified = 0,
        Unlock = 1,
        Lock = 2,
        Unlatch = 3,
        LockNGo = 4,
        LockNGoWithUnlatch = 5
    }

    public enum LockActionOpenerEnum
    {
        ActivateRto = 1,
        DeactivateRto = 2,
        ElectricStrikeActuation = 3,
        ActivateContinuousMode = 4,
        DeactivateContinuousMode = 5
    }
}
