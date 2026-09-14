namespace MyWaferMap;

public struct RectD(double x, double y, double width, double height)
{
    public double X { get; set; } = x;
    public double Y { get; set; } = y;
    public double Width { get; } = width;
    public double Height { get; } = height;

    // 判断点是否在矩形内部
    public bool Contains(double px, double py)
    {
        return px >= X && px <= X + Width && py >= Y && py <= Y + Height;
    }
}