using System;
using System.Collections.Generic;

/// <summary>
/// Summary description for location
/// </summary>
/// 
namespace Structure
{
    public class Location
    {
        public int Id;
        public string Name { get; set; }
        public bool Visible { get; set; }
        public Color Color { get; set; }
        public List<int> Sessions { set; get; }
        public int Parent { get; set; }

        public Location(string name, string color, bool visible, int parent)
        {
            Name = name;
            Visible = visible;
            Sessions = new List<int>();
            Parent = parent;
            try
            {
                Color = (Color) ColorConverter.ConvertFromString(color);
            }
            catch
            {
            }
        }

        public Location(LocationNode node)
        {
            var loc = new Location(node.Name, node.Color.ToString(), node.Visible, node.Parent.Id);
            loc.Sessions = node.Sessions;
        }
    }
}