namespace MyWaferMap.Impls.Areas;

public class RectangleArea(RectD bounds) : IArea
{
    public RectD Bounds { get; } = bounds;
    public MatrixCoordinate MatrixCoordinate { get; set; }
    public IReadOnlyList<IReadOnlyList<IArea>> DividedBy(RectD rectD)
    {
        throw new NotImplementedException();
    }
}