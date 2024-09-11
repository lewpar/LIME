namespace LIME.Shared.Network;

public class DataUnit
{
    public string Amount { get; }
    public DataUnitType Unit { get; }

    public DataUnit(string amount, DataUnitType unit)
    {
        this.Amount = amount;
        this.Unit = unit;
    }

    public override string ToString()
    {
        return $"{Amount}{Unit}";
    }
}
