using MyWaferMap.Enums;
using MyWaferMap.Extensions;

namespace MyWaferMap.Impls.Areas;

public class CircleArea(double centerX, double centerY, double radius) : IArea
{
    private readonly Center _circleCenter = new(centerX, centerY, radius);
    public RectD Bounds { get; } = new(centerX - radius, centerY - radius, radius * 2, radius * 2);
    public MatrixCoordinate MatrixCoordinate { get; set; }

    public IReadOnlyList<IReadOnlyList<IArea>> DividedBy(RectD rectD)
    {
        if (IsOutOfArea(in rectD))
        {
            throw new ArgumentException("The area is not outside of the circle area");
        }

        var leftColumnList = GetLeftAllColumns(rectD);
        var rightColumnList = GetRightAllColumns(rectD.MoveToRight());
        var columnList = new List<IReadOnlyList<IArea>>();
        columnList.AddRange(leftColumnList.Reverse());
        columnList.AddRange(rightColumnList);
        ModifyMatrixCoordinate(columnList);
        return columnList;
    }

    private IEnumerable<IReadOnlyList<IArea>> GetLeftAllColumns(RectD rectD)
    {
        while (!IsColumnOutOfCircle(in rectD, out var rectToCircleRelation))
        {
            yield return GetColumnAreas(in rectD, rectToCircleRelation!.Value);
            rectD = rectD.MoveToLeft();
        }
    }

    private IEnumerable<IReadOnlyList<IArea>> GetRightAllColumns(RectD rectD)
    {
        while (!IsColumnOutOfCircle(in rectD, out var rectToCircleRelation))
        {
            yield return GetColumnAreas(in rectD, rectToCircleRelation!.Value);
            rectD = rectD.MoveToRight();
        }
    }

    private IReadOnlyList<IArea> GetColumnAreas(ref readonly RectD rectD, RectToAreaRelation rectToAreaRelation)
    {
        if (rectToAreaRelation.HasFlag(RectToAreaRelation.FullyInside))
        {
            return [.. ProbeInBidirectionalDirections(in rectD)];
        }

        if (rectToAreaRelation.HasFlag(RectToAreaRelation.FullyAboveOutside | RectToAreaRelation.IntersectAbove))
        {
            return [.. ProbeInBelowDirection(rectD)];
        }

        return [.. ProbeInAboveDirection(rectD).Reverse()];
    }

    private IEnumerable<IArea> ProbeInBidirectionalDirections(ref readonly RectD rectD)
    {
        var rightStart = rectD.MoveToBelow();
        return ProbeInAboveDirection(rectD).Reverse().Concat(ProbeInBelowDirection(rightStart));
    }

    private IEnumerable<IArea> ProbeInBelowDirection(RectD rectD)
    {
        while (IsOutOfArea(in rectD))
        {
            rectD = rectD.MoveToBelow();
        }

        while (!IsOutOfArea(in rectD))
        {
            yield return new RectangleArea(rectD);
            rectD = rectD.MoveToBelow();
        }
    }

    private IEnumerable<IArea> ProbeInAboveDirection(RectD rectD)
    {
        while (IsOutOfArea(in rectD))
        {
            rectD = rectD.MoveToAbove();
        }

        while (!IsOutOfArea(in rectD))
        {
            yield return new RectangleArea(rectD);
            rectD = rectD.MoveToAbove();
        }
    }

    private bool IsOutOfArea(ref readonly RectD rectD)
    {
        var (horizontalShortestDistancePower, _) = CalculateHorizontalDistancePower(in rectD, in _circleCenter);
        var (verticalShortestDistancePower, _) = CalculateVerticalShortestDistancePower(in rectD, in _circleCenter);
        return horizontalShortestDistancePower + verticalShortestDistancePower > _circleCenter.RadiusPower;
    }

    private bool IsColumnOutOfCircle(ref readonly RectD rectD, out RectToAreaRelation? rectToAreaRelation)
    {
        var (horizontalShortestDistancePower, horizontalLongestDistancePower) =
            CalculateHorizontalDistancePower(in rectD, in _circleCenter);
        var isOut = horizontalShortestDistancePower > _circleCenter.RadiusPower;
        if (isOut)
        {
            rectToAreaRelation = null;
            return true;
        }

        var rectCenterVerticalOffset = rectD.Y + rectD.Height / 2;

        var (verticalShortestDistancePower, verticalLongestDistancePower) =
            CalculateVerticalShortestDistancePower(in rectD, in _circleCenter);
        if (verticalShortestDistancePower + horizontalShortestDistancePower > _circleCenter.RadiusPower)
        {
            if (rectCenterVerticalOffset > _circleCenter.Y)
            {
                rectToAreaRelation = RectToAreaRelation.FullyAboveOutside;
                return false;
            }

            rectToAreaRelation = RectToAreaRelation.FullyBelowOutside;
            return false;
        }

        if (verticalLongestDistancePower + horizontalLongestDistancePower < _circleCenter.RadiusPower)
        {
            rectToAreaRelation = RectToAreaRelation.FullyInside;
            return false;
        }

        rectToAreaRelation = rectCenterVerticalOffset > _circleCenter.Y
            ? RectToAreaRelation.IntersectAbove
            : RectToAreaRelation.IntersectBelow;
        return false;
    }

    private void ModifyMatrixCoordinate(IReadOnlyList<IReadOnlyList<IArea>> areas)
    {
        var columnCount = areas.Count;
        var rowCount = areas.Max(c => c.Count);
        var highestColumn = areas.First(c => c.Count == rowCount);
        var highestRectangle = highestColumn.First().Bounds;
        var currentColumnIndex = -1;
        foreach (var column in areas)
        {
            currentColumnIndex++;
            var startRowIndex = Equals(highestColumn, column)
                ? 0
                : CalculateRowIndex(column[0].Bounds.Y, highestRectangle.Y, highestRectangle.Height);
            foreach (var row in column)
            {
                row.MatrixCoordinate = new MatrixCoordinate(MatrixCoordinate.Row * rowCount + startRowIndex,
                    MatrixCoordinate.Column * columnCount + currentColumnIndex);
                startRowIndex++;
            }
        }

        return;

        static int CalculateRowIndex(double currentRectY, double highestRectY, double height)
        {
           return (int)double.Round(Math.Abs(currentRectY - highestRectY) / height);
        }
    }

    private static (double horizontalShortestDistancePower, double horizontalLongestDistancePower )
        CalculateHorizontalDistancePower(ref readonly RectD rectD, ref readonly Center center)
    {
        var value1 = Math.Pow(Math.Abs(rectD.X + rectD.Width - center.X), 2);
        var value2 = Math.Pow(Math.Abs(rectD.X - center.X), 2);
        return value1 >= value2 ? (value2, value1) : (value1, value2);
    }

    private static (double verticalShortestDistancePower, double verticalLongestDistancePower)
        CalculateVerticalShortestDistancePower(ref readonly RectD rectD, ref readonly Center center)
    {
        var value1 = Math.Pow(Math.Abs(rectD.Y + rectD.Height - center.Y), 2);
        var value2 = Math.Pow(Math.Abs(rectD.Y - center.Y), 2);
        return value1 >= value2 ? (value2, value1) : (value1, value2);
    }

    private readonly struct Center(double x, double y, double radius)
    {
        public readonly double X = x;
        public readonly double Y = y;
        public readonly double RadiusPower = Math.Pow(radius, 2);
    }
}