namespace MyWaferMap;

public class AreaMatrix : IArea
{
    public RectD Bounds { get; }
    public MatrixCoordinate MatrixCoordinate { get; set; }
    public IReadOnlyList<IReadOnlyList<IArea>> DividedBy(RectD rectD)
    {
        throw new NotImplementedException();
    }
}