using System;

namespace NLog.Loki;
internal static class UnixDateTimeConverter
{
    private static readonly DateTime UnixEpoch = new(1970, 1, 1, 0, 0, 0, DateTimeKind.Utc);
    public static (long Seconds, int Nano) ToSecondsAndNano(DateTime date)
    {
#if NET6_0_OR_GREATER
        var (quotient, remainder) = Math.DivRem((date.ToUniversalTime() - UnixEpoch).Ticks, 10_000_000);
#else
        var quotient = Math.DivRem((date.ToUniversalTime() - UnixEpoch).Ticks, 10_000_000, out var remainder);
#endif
        return (quotient, (int)remainder * 100);
    }
}
