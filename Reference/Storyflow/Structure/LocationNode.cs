using System;
using System.Collections.Generic;

/// <summary>
/// Summary description for locationnode
/// </summary>
/// 
namespace Structure
{
    public class LocationNode
    {
        public int Id { get; set; }
        public string Name { get; set; }
        public bool Visible { get; set; }
        public Color Color { get; set; }
        public List<int> Sessions { set; get; }
        public LocationNode Parent { get; set; }
        public List<LocationNode> Children { get; set; }

        public LocationNode(int id)
        {
            Id = id;
            Children = new List<LocationNode>();
        }

        public LocationNode(int id, string name, string color, bool visible, LocationNode parent)
        {
            Name = name;
            Visible = visible;
            Sessions = new List<int>();
            Parent = parent;
            Id = id;
            Children = new List<LocationNode>();
            var convertFromString = ColorConverter.ConvertFromString(color);
            if (convertFromString != null)
                Color = (Color)convertFromString;

        }
    }
}