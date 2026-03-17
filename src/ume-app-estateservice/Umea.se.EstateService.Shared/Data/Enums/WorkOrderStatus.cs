namespace Umea.se.EstateService.Shared.Data.Enums;

/// <summary>
/// Tracks the submission pipeline state (local → Pythagoras).
/// This is NOT the Pythagoras work order status — see PythagorasStatusId for that.
/// </summary>
public enum WorkOrderSyncStatus
{
    Pending,
    Processing,
    Submitted,
    Failed
}
