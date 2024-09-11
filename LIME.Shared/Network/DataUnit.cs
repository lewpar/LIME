namespace LIME.Shared.Network;

public class DataUnit
{
    public float Amount { get; }
    public DataUnitType Unit { get; }

    public DataUnit(float amount, DataUnitType unit)
    {
        this.Amount = amount;
        this.Unit = unit;
    }

    public override string ToString()
    {
        return $"{Amount}{Unit}";
    }
}
