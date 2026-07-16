namespace CartSmart.Mobile.Services;

/// <summary>
/// FR-3.x local "running low" push notifications (spec Section 5.4). Out of scope for this
/// pass — stubbed so DI wiring is in place. <see cref="IPredictionService"/> is implemented and
/// could drive this, but scheduling actual local notifications is still unbuilt.
/// </summary>
public interface INotificationService
{
    Task ScheduleDueSoonNotificationAsync(string productName, DateTimeOffset dueAt);
}

public class NotificationService : INotificationService
{
    public Task ScheduleDueSoonNotificationAsync(string productName, DateTimeOffset dueAt)
        => throw new NotImplementedException(
            "NotificationService (FR-3.x) is not implemented in this pass — see Section 5.4.");
}
