namespace MyWaferMap;

public interface IAreaDivideRule
{
    IReadOnlyList<IReadOnlyList<IArea>> Divide(IArea parentArea);
    IAreaDivideRule? ChildRule { get; set; }
    bool IsDivideContinually { get; }
}