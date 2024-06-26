using Google.Protobuf.WellKnownTypes;

namespace TomorrowDAO.Contracts.Election;

public static class TimestampHelper
{
    /// <summary>
    ///     0001-01-01T00:00:00Z
    /// </summary>
    public static Timestamp MinValue => new() { Nanos = 0, Seconds = -62135596800L };

    /// <summary>
    ///     9999-12-31T23:59:59.999999999Z
    /// </summary>
    public static Timestamp MaxValue => new() { Nanos = 999999999, Seconds = 253402300799L };
}