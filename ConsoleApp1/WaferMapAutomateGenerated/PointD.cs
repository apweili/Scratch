namespace WaferMap;

public struct PointD
{
    public double X { get; set; }
    public double Y { get; set; }
    public PointD(double x, double y) => (X, Y) = (x, y);
}