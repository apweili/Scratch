namespace MyWaferMap.Enums;

[Flags]
internal enum RectToAreaRelation
{
    FullyAboveOutside = 0,
    FullyBelowOutside = 1 << 0,
    FullyInside = 1 << 1,
    IntersectAbove = 1 << 2,
    IntersectBelow = 1 << 3,
    // FullyEnclosing = 1 << 6
}
