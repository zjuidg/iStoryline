using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Xml;
using Microsoft.Extensions.DependencyInjection;

/// <summary>
/// Summary description for story
/// </summary>
/// 
namespace Structure
{
    public class Story
    {
        public LocationNode LocationRoot { get; set; }
        public List<Character> Characters { get; set; }
        public List<Location> Locations { get; set; }
        public HashSet<int> Sessions { get; set; }
        public int rabbitId = 0;

        public int[] TimeStamps { get; set; }

        public SessionTable SessionTable { get; set; }

        public Dictionary<int, int> _sessionToLocation = new Dictionary<int, int>();
        // { sessionid -> (character) }
        public Dictionary<int, HashSet<int>> sessionToCharacteres = new Dictionary<int, HashSet<int>>();

        public string FileName;

        public int FrameCount
        {
            get
            {
                return TimeStamps.Length > 0 ? TimeStamps.Length - 1 : 0;
            }
        }
        public int GetLocationId(int sessionId)
        {
            return _sessionToLocation[sessionId];
        }

        public static Story Read(string path)
        {
            Story story = new Story();
            story.FileName = path;

            XmlDocument xmlDoc = new XmlDocument();
            xmlDoc.Load(path);
            // Console.WriteLine(xmlDoc.OuterXml);

            // 1.read locations and tree structure
            story.Locations = new List<Location>();
            XmlNode xmlNode = xmlDoc.SelectSingleNode("Story/Locations");

            List<string> characterlist = new List<string>();
            List<int> sessionlist = new List<int>();
            story.LocationRoot = new LocationNode(-1);
            if (xmlNode != null && xmlNode.HasChildNodes)
            {
                foreach (XmlNode locationNode in xmlNode.ChildNodes)
                {
                    ParseXmlNode(story, locationNode, story.LocationRoot, story.Locations);
                }
            }

            // 2.read characters and session table
            story.Characters = new List<Character>();
            story.Sessions = new HashSet<int>();
            xmlNode = xmlDoc.SelectSingleNode("Story/Characters");
            List<List<Tuple<int, int, int>>> spansOfCharacters = new List<List<Tuple<int, int, int>>>();
            HashSet<int> timestamps = new HashSet<int>();
            foreach (XmlNode characterNode in xmlNode.ChildNodes)
            {
                string name = characterNode.Attributes["Name"].Value;
                string color = characterNode.Attributes["Color"] != null ? characterNode.Attributes["Color"].Value : "Black";
                double weight = characterNode.Attributes["Weight"] != null ? double.Parse(characterNode.Attributes["Weight"].Value) : 1.0;
                Character character = new Character(name, weight, color);
                story.Characters.Add(character);
                characterlist.Add(name);
                characterlist.Add(color);

                List<Tuple<int, int, int>> spans = new List<Tuple<int, int, int>>();
                foreach (XmlNode spanNode in characterNode.ChildNodes)
                {
                    int start = int.Parse(spanNode.Attributes["Start"].Value);
                    int end = int.Parse(spanNode.Attributes["End"].Value);
                    int session = int.Parse(spanNode.Attributes["Session"].Value);
                    spans.Add(new Tuple<int, int, int>(start, end, session));
                    timestamps.Add(start);
                    timestamps.Add(end);

                    sessionlist.Add(session);
                    sessionlist.Add(start);
                    sessionlist.Add(end);

                    story.Sessions.Add(session);
                }
                spansOfCharacters.Add(spans);
            }
            story.TimeStamps = timestamps.ToArray();
            int[] sessiondetails = sessionlist.ToArray();
            Array.Sort(story.TimeStamps);
            foreach (var storySession in story.Sessions)
            {
                story.sessionToCharacteres.Add(storySession, new HashSet<int>());
            }
            story.SessionTable = new SessionTable(story.Characters.Count, story.TimeStamps.Length - 1);
            for (int id = 0; id < story.Characters.Count; ++id)
            {
                for (int frame = 0; frame < story.TimeStamps.Length - 1; ++frame)
                {
                    foreach (Tuple<int, int, int> tuple in spansOfCharacters[id])
                    {
                        if (story.TimeStamps[frame] >= tuple.Item1 &&
                            story.TimeStamps[frame + 1] <= tuple.Item2)
                        {
                            story.SessionTable[id, frame] = tuple.Item3;
                            story.sessionToCharacteres[tuple.Item3].Add(id);
                        }
                    }
                }
            }

            
            string[] timings = Array.ConvertAll(story.TimeStamps, x => x.ToString());
            string[] entities = characterlist.ToArray();
            string[] sessiontiming = Array.ConvertAll(sessiondetails, x => x.ToString());
            return story;
        }

        private static void ParseXmlNode(Story story, XmlNode xmlNode, LocationNode parentNode, List<Location> locations)
        {
            //int id = int.Parse(xmlNode.Attributes["Id"].Value);
            string name = xmlNode.Attributes["Name"] != null ? xmlNode.Attributes["Name"].Value : "Unnamed";
            string color = xmlNode.Attributes["Color"] != null ? xmlNode.Attributes["Color"].Value : "Black";
            bool visible = xmlNode.Attributes["Visible"] != null && bool.Parse(xmlNode.Attributes["Visible"].Value);
            string list = xmlNode.Attributes["Sessions"] != null ? xmlNode.Attributes["Sessions"].Value : "";

            Location location = new Location(name, color, visible, parentNode.Id);
            int locationId = locations.Count;
            string[] items = list.Split(',');
            foreach (string item in items)
            {
                if (!string.IsNullOrWhiteSpace(item))
                {
                    int sessionId = int.Parse(item.Trim());
                    location.Sessions.Add(sessionId);
                    story._sessionToLocation.Add(sessionId, locationId);
                }
            }

            locations.Add(location);
            LocationNode curNode = new LocationNode(locationId);
            parentNode.Children.Add(curNode);

            foreach (XmlNode child in xmlNode.ChildNodes)
            {
                ParseXmlNode(story, child, curNode, locations);
            }
        }
    }
}