namespace MyWaferMap.Impls.Rules;

public class AccordingToAnchorRule(RectD anchor) : IAreaDivideRule
{

    public IReadOnlyList<IReadOnlyList<IArea>> Divide(IArea parentArea)
    {
        return parentArea.DividedBy(anchor);
    }

    public IAreaDivideRule? ChildRule { get; set; }
    public bool IsDivideContinually { get; } = true;
}