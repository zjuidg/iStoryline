using System;
using System.Collections.Generic;
using System.Linq;

/// <summary>
/// Summary description for character
/// </summary>
/// 
namespace Structure
{
    public class Character
    {
        public string Name { get; set; }
        public double Weight { get; set; }
        public Color Color { get; set; }
        public int Id;
        public Character(string name, double weight, string color, int id)
        {
            //
            // TODO: Add constructor logic here
            //Id = id;
            Name = name;
            Weight = weight;
            try
            {
                Color = (Color)ColorConverter.ConvertFromString(color);
            }
            catch
            {
            }
        }
        public Character(string name, double weight, string color)
        {
            Name = name;
            Weight = weight;
            try
            {
                Color = (Color)ColorConverter.ConvertFromString(color);
            }
            catch
            {
            }
        }

        public override bool Equals(object obj)
        {
            if (obj is Character)
                return Name == (obj as Character).Name;
            return false;
        }
    }
}