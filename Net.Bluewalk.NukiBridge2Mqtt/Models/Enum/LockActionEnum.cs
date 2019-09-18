namespace Net.Bluewalk.NukiBridge2Mqtt.Models.Enum
{
    /// <summary>
    /// Lock action
    /// </summary>
    public enum LockActionEnum
    {
        Unspecified = 0,
        Unlock = 1,
        ActivateRto = 1,
        Lock = 2,
        DeactivateRto = 2,
        Unlatch = 3,
        ElectricStrikeActuation = 3,
        LockNGo = 4,
        ActivateContinuousMode = 4,
        LockNGoWithUnlatch = 5,
        DeactivateContinuousMode = 5
    }
}
