namespace LogicFlowEnterpriseFramework.Shared.Helpers;

public interface IDateTimeProvider
{
    DateTimeOffset UtcNow { get; }
}

public sealed class DateTimeProvider : IDateTimeProvider
{
    public DateTimeOffset UtcNow => DateTimeOffset.UtcNow;
}
