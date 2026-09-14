namespace WaferMap;

public class WaferDieDivider
{
    // 晶圆半径，单位和你使用的坐标系统保持一致，比如150mm对应6英寸晶圆
    public double WaferRadius { get; set; }
    // 晶圆中心全局坐标，工控场景常用原点在wafer中心的坐标系，这里默认配置为(0,0)
    public PointD WaferCenter { get; set; } = new PointD(0, 0);

    /// <summary>
    /// 入口方法：输入wafer的最大边界矩形和划分规则，输出所有die的分布
    /// </summary>
    public List<Die> CalculateAllDies(RectD waferBoundingRect, DieDivideRule rootDivideRule)
    {
        var result = new List<Die>();
        // 从根区域开始递归划分，初始行列偏移都为0
        RecursiveDivide(waferBoundingRect, rootDivideRule, 0, 0, result);
        return result;
    }

    /// <summary>
    /// 递归嵌套划分的核心逻辑
    /// </summary>
    private void RecursiveDivide(RectD parentRegion, DieDivideRule currentRule, 
        int parentStartCol, int parentStartRow, List<Die> outputDies)
    {
        // 计算当前层级每一个小格子的宽高
        double childWidth = parentRegion.Width / currentRule.DivideColumns;
        double childHeight = parentRegion.Height / currentRule.DivideRows;

        // 遍历生成当前层级的所有子区域
        for (int col = 0; col < currentRule.DivideColumns; col++)
        {
            for (int row = 0; row < currentRule.DivideRows; row++)
            {
                // 计算当前子区域的全局坐标
                var childRegion = new RectD(
                    x: parentRegion.X + col * childWidth,
                    y: parentRegion.Y + row * childHeight,
                    width: childWidth,
                    height: childHeight
                );

                int globalCol = parentStartCol + col;
                int globalRow = parentStartRow + row;

                // 如果存在子嵌套规则，继续递归划分
                if (currentRule.ChildRule != null)
                {
                    RecursiveDivide(childRegion, currentRule.ChildRule, 
                        globalCol * currentRule.ChildRule.DivideColumns, 
                        globalRow * currentRule.ChildRule.DivideRows, 
                        outputDies);
                }
                else
                {
                    // 当前没有下一级规则，这个子区域就是最终die
                    var die = new Die
                    {
                        Bounds = childRegion,
                        ColumnIndex = globalCol,
                        RowIndex = globalRow,
                        // 自动判断该die是否落在wafer的有效圆形区域内
                        IsOnWafer = IsRectIntersectWafer(childRegion)
                    };
                    outputDies.Add(die);
                }
            }
        }
    }

    // 辅助方法：判断矩形区域是否和wafer圆形边界相交
    private bool IsRectIntersectWafer(RectD rect)
    {
        // 找矩形中距离wafer圆心最近的点
        double closestX = Clamp(WaferCenter.X, rect.X, rect.X + rect.Width);
        double closestY = Clamp(WaferCenter.Y, rect.Y, rect.Y + rect.Height);

        // 计算这个点到晶圆中心的距离，判断是否在晶圆内部
        double dx = closestX - WaferCenter.X;
        double dy = closestY - WaferCenter.Y;
        return dx * dx + dy * dy <= WaferRadius * WaferRadius;
    }

    private double Clamp(double value, double min, double max)
    {
        return value < min ? min : value > max ? max : value;
    }
}