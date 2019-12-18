
namespace Structure
{
    // minimal stub to clear errors
    public class Color
    {
        public byte R;
        public byte G;
        public byte B;

        public Color()
        {
        }

        public Color(byte r, byte g, byte blue)
        {
            this.R = r;
            this.G = g;
            this.B = blue;
        }

        public static Color FromRgb(byte r, byte g, byte b)
        {
            return new Color(r ,g, b);
        }

        public static Color operator *(Color color, float f)
        {
            return color;
        }
        
        public static Color operator +(Color color0, Color color1)
        {
            return color0;
        }
    }

    public class Colors
    {
        public static Color Black = new Color();
        public static Color Gray = new Color();
    }

    // stub
    public class ColorConverter
    {
        public static Color ConvertFromString(string color)
        {
            return new Color();
        }
    }
}