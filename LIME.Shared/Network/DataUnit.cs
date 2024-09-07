namespace LIME.Shared.Network;

public class DataUnit
{
    public string Amount { get; }
    public string Unit { get; }

    public DataUnit(string amount, string unit)
    {
        this.Amount = amount;
        this.Unit = unit;
    }

    public override string ToString()
    {
        return $"{Amount}{Unit}";
    }
}
