using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Drawing;
using System.Text;
/// <summary>
/// Summary description for ColorspaceHelper
/// </summary>
/// 
namespace Algorithm
{
    public sealed class RGB
    {
        /// <summary>
        /// Red component
        /// </summary>
        public byte Red;

        /// <summary>
        /// Green component
        /// </summary>
        public byte Green;

        /// <summary>
        /// Blue component
        /// </summary>
        public byte Blue;

        /// <summary>
        /// Index of Red component
        /// </summary>
        public const short R = 2;

        /// <summary>
        /// Index of Green component
        /// </summary>
        public const short G = 1;

        /// <summary>
        /// Index of Blue component
        /// </summary>
        public const short B = 0;

        /// <summary>
        /// Initializes a new instance of the RGB class
        /// </summary>
        public RGB() { }

        /// <summary>
        /// Initializes a new instance of the RGB class
        /// </summary>
        /// <param name="red">Red component</param>
        /// <param name="green">Green component</param>
        /// <param name="blue">Blue component</param>
        public RGB(byte red, byte green, byte blue)
        {
            this.Red = red;
            this.Green = green;
            this.Blue = blue;
        }

        /// <summary>
        /// Initializes a new instance of the RGB class
        /// </summary>
        /// <param name="color">Input color</param>
        public RGB(Color color)
        {
            this.Red = color.R;
            this.Green = color.G;
            this.Blue = color.B;
        }

        /// <summary>
        /// Color property
        /// </summary>
        public System.Drawing.Color Color
        {
            get { return Color.FromArgb(Red, Green, Blue); }
            set
            {
                this.Red = value.R;
                this.Green = value.G;
                this.Blue = value.B;
            }
        }

        public override string ToString()
        {
            return String.Format("RGB: ({0},{1},{2})", Red, Green, Blue);
        }
    }

    /// <summary>
    /// HSI colorspace
    /// </summary>
    /// <remarks>All components normalized</remarks>
    public sealed class HSI
    {
        /// <summary>
        /// Hue component
        /// </summary>
        /// <remarks>Hue ranges [0,1]</remarks>
        public double Hue;

        /// <summary>
        /// Saturation component
        /// </summary>
        /// <remarks>Saturation ranges [0,1]</remarks>
        public double Saturation;

        /// <summary>
        /// Intensity component
        /// </summary>
        /// <remarks>Intensity ranges [0,1]</remarks>
        public double Intensity;

        /// <summary>
        /// Initializes a new instance of the HSI class
        /// </summary>
        public HSI() { }

        /// <summary>
        /// Initializes a new instance of the HSI class
        /// </summary>
        /// <param name="hue">Hue component</param>
        /// <param name="saturation">Saturation component</param>
        /// <param name="intensity">Intensity component</param>
        public HSI(int hue, double saturation, double intensity)
        {
            this.Hue = hue;
            this.Saturation = saturation;
            this.Intensity = intensity;
        }

        public override string ToString()
        {
            return String.Format("HSI: ({0},{1},{2})", Hue, Saturation, Intensity);
        }
    }

    /// <summary>
    /// This helper class provides with colorspace-convertion extension methods
    /// </summary>
    /// <remarks>The algorithms are from "Digital Image Processing Using MATLAB" (DIPUM)</remarks>
    public static class ColorspaceHelper
    {
        /// <summary>
        /// Convert RGB colorspace to HSI colorspace
        /// </summary>
        /// <param name="rgb">Input RGB pixel</param>
        /// <returns>HSI colorspace pixel</returns>
        public static HSI RGB2HSI(this RGB rgb)
        {
            HSI hsi = new HSI();

            double r = (rgb.Red / 255.0);
            double g = (rgb.Green / 255.0);
            double b = (rgb.Blue / 255.0);

            double theta = Math.Acos(0.5 * ((r - g) + (r - b)) / Math.Sqrt((r - g) * (r - g) + (r - b) * (g - b))) / (2 * Math.PI);

            hsi.Hue = (b <= g) ? theta : (1 - theta);

            hsi.Saturation = 1 - 3 * Math.Min(Math.Min(r, g), b) / (r + g + b);

            hsi.Intensity = (r + g + b) / 3;

            return hsi;
        }

        /// <summary>
        /// Convert HSI colorspace to RGB colorspace
        /// </summary>
        /// <param name="hsi">Input HSI pixel</param>
        /// <returns>RGB colorspace pixel</returns>
        public static RGB HSI2RGB(this HSI hsi)
        {
            double r, g, b;

            double h = hsi.Hue;
            double s = hsi.Saturation;
            double i = hsi.Intensity;

            h = h * 2 * Math.PI;

            if (h >= 0 && h < 2 * Math.PI / 3)
            {
                b = i * (1 - s);
                r = i * (1 + s * Math.Cos(h) / Math.Cos(Math.PI / 3 - h));
                g = 3 * i - (r + b);
            }
            else if (h >= 2 * Math.PI / 3 && h < 4 * Math.PI / 3)
            {
                r = i * (1 - s);
                g = i * (1 + s * Math.Cos(h - 2 * Math.PI / 3) / Math.Cos(Math.PI - h));
                b = 3 * i - (r + g);
            }
            else //if (h >= 4 * Math.PI / 3 && h <= 2 * Math.PI)
            {
                g = i * (1 - s);
                b = i * (1 + s * Math.Cos(h - 4 * Math.PI / 3) / Math.Cos(5 * Math.PI / 3 - h));
                r = 3 * i - (g + b);
            }

            return new RGB((byte)(r * 255.0 + .5), (byte)(g * 255.0 + .5), (byte)(b * 255.0 + .5));
        }
    }
}