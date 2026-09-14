namespace WaferMap;

public class Die
{
    // die在wafer全局坐标系下的物理边界
    public RectD Bounds { get; set; }
    // die的行列索引，晶圆领域通用坐标约定，从左下角(0,0)开始计数
    public int RowIndex { get; set; }
    public int ColumnIndex { get; set; }
    // 可选：die在wafer上的唯一ID，可直接用于后续检测流程
    public string DieId => $"D{ColumnIndex}_{RowIndex}";
    // 是否落在wafer有效区域内
    public bool IsOnWafer { get; set; }
}