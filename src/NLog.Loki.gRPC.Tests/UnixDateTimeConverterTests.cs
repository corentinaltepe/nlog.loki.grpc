using System;
using NUnit.Framework;

namespace NLog.Loki.gRPC.Tests;

internal class UnixDateTimeConverterTests
{
    [Test]
    public void ToSecondsAndNano()
    {
        var date = new DateTime(2022, 05, 31, 08, 05, 45, 478, DateTimeKind.Utc);

        var (seconds, nano) = UnixDateTimeConverter.ToSecondsAndNano(date);

        Assert.AreEqual(1653984345, seconds);
        Assert.AreEqual(478000000, nano);
    }
}
