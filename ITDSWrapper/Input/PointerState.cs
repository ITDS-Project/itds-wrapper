using ITDSWrapper.Core;

namespace ITDSWrapper.Input;

public class PointerState(double width, double height)
{
    public bool Pressed { get; set; }
    public double X { get; set; }
    public double Y { get; set; }
    public double Width { get; set; } = width;
    public double Height { get; set; } = height;
    public short RetroX => (short)((32767 * 2.0 + 1) / Width * X - 0x7FFF);
    public short RetroY => (short)((32767 * 2.0 + 1) / Height * Y - 0x7FFF);

    public void Press(double x, double y, ScreenLayout layout, bool screenCoords = false)
    {
        double xMin, xMax, yMin, yMax;
        if (screenCoords)
        {
            x *= Width / 256.0;
            y *= Height / 384.0;
        }
        switch (layout)
        {
            default:
            case ScreenLayout.TOP_BOTTOM:
                xMin = 0;
                xMax = Width;
                yMin = Height / 2;
                yMax = Height;
                break;

            case ScreenLayout.LEFT_RIGHT:
                xMin = Width / 2;
                xMax = Width;
                yMin = 0;
                yMax = Height;
                break;

            case ScreenLayout.RIGHT_LEFT:
                xMin = 0;
                xMax = Width / 2;
                yMin = 0;
                yMax = Height;
                break;
        }

        Pressed = y > yMin && y <= yMax && x >= xMin && x <= xMax;
        if (Pressed)
        {
            X = x;
            Y = y;
        }
    }

    public void Release()
    {
        Pressed = false;
    }
}