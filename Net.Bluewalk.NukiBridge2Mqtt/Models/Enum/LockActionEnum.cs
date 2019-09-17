namespace Net.Bluewalk.NukiBridge2Mqtt.Models.Enum
{
    /// <summary>
    /// Lock action
    ///   [Smartlock]__[Opener]
    /// </summary>
    public enum LockActionEnum
    {
        Unspecified = 0,
        Unlock__ActivateRto = 1,
        Lock__DeactivateRtok = 2,
        Unlatch__ElectricStrikeActuation = 3,
        LockNGo__ActivateContinuousMode = 4,
        LockNGoWithUnlatch__DeactivateContinuousMode = 5
    }
}
