using LIME.Shared.Network;

using Xunit.Abstractions;

namespace LIME.Tests.Shared;

public class DataUnitTests
{
    private readonly ITestOutputHelper output;

    public DataUnitTests(ITestOutputHelper output)
    {
        this.output = output;
    }

    [Fact]
    public void GetDataUnitFromBytes_5210Bytes_ReturnsKB()
    {
        long bytes = 5120;
        DataUnit data = DataUnitConverter.GetDataUnitFromBytes(bytes);

        output.WriteLine($"Got unit '{data.Unit}' with amount '{data.Amount}'.");

        Assert.True(data.Unit == DataUnitType.KB);
    }
}