namespace WaferMap;

public struct RectD
{
    public double X { get; set; }      // 区域左上角X坐标（基于wafer全局坐标系）
    public double Y { get; set; }      // 区域左上角Y坐标
    public double Width { get; set; } // 区域宽度
    public double Height { get; set; }// 区域高度

    public RectD(double x, double y, double width, double height)
    {
        X = x; Y = y; Width = width; Height = height;
    }

    // 判断点是否在矩形内部
    public bool Contains(double px, double py)
    {
        return px >= X && px <= X + Width && py >= Y && py <= Y + Height;
    }
}