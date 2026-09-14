namespace MyWaferMap;

public interface IArea
{
    public RectD Bounds { get; }
    MatrixCoordinate MatrixCoordinate { get; set; }
    IReadOnlyList<IReadOnlyList<IArea>> DividedBy(RectD rectD);
}