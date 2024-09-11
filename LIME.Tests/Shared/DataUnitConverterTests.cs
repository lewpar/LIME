using LIME.Shared.Network;

using Xunit.Abstractions;

namespace LIME.Tests.Shared;

public class DataUnitConverterTests
{
    private readonly ITestOutputHelper output;

    public DataUnitConverterTests(ITestOutputHelper output)
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

    [Fact]
    public void GetDataUnitFromBytes_ZeroBytes_ReturnsB()
    {
        long bytes = 0;
        DataUnit data = DataUnitConverter.GetDataUnitFromBytes(bytes);

        output.WriteLine($"Got unit '{data.Unit}' with amount '{data.Amount}'.");

        Assert.True(data.Unit == DataUnitType.B);
    }

    [Fact]
    public void GetDataUnitFromBytes_NegativeBytes_ThrowsException()
    {
        long bytes = -1;

        Assert.Throws<DataUnitConverterException>(() => DataUnitConverter.GetDataUnitFromBytes(bytes));
    }
}