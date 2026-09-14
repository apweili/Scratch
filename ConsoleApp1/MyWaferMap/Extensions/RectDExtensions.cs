namespace MyWaferMap.Extensions;

public static class RectDExtensions
{
    public static RectD MoveToLeft(this RectD rectD)
    {
        return new RectD(rectD.X - rectD.Width, rectD.Y, rectD.Width, rectD.Height);
    }

    public static RectD MoveToRight(this RectD rectD)
    {
        return new RectD(rectD.X + rectD.Width, rectD.Y, rectD.Width, rectD.Height);
    }

    public static RectD MoveToAbove(this RectD rectD)
    {
        return new RectD(rectD.X, rectD.Y - rectD.Height, rectD.Width, rectD.Height);
    }

    public static RectD MoveToBelow(this RectD rectD)
    {
        return new RectD(rectD.X, rectD.Y + rectD.Height, rectD.Width, rectD.Height);
    }
}