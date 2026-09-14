namespace WaferMap;

public class DieDivideRule
{
    /// <summary>
    /// 当前规则：把输入的父区域，划分成多少份
    /// </summary>
    public int DivideColumns { get; set; } // 横向划分成多少列
    public int DivideRows { get; set; }    // 纵向划分成多少行
    
    /// <summary>
    /// 可选：子嵌套规则，每一个划分出来的子区域，继续应用下一级划分规则
    /// 留空就表示当前层级划分出来的就是最终die
    /// </summary>
    public DieDivideRule ChildRule { get; set; }
}