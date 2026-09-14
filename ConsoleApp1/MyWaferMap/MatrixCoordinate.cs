namespace MyWaferMap;

public struct MatrixCoordinate(int row, int column)
{
    public int Row { get; private set; } = row;
    public int Column { get; private set; } = column;
}