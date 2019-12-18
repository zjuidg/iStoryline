using System;
using System.Collections.Generic;
using System.Linq;
using System.Xml;

/// <summary>
/// Summary description for lstory
/// </summary>
namespace Structure
{
    public enum NodeType
    {
        Root,
        Location,
        Session,
        Segment
    }

    public class PositionCache
    {
        public double Old = -1;
        public double New = -1;
        public void Push(double pos)
        {
            Old = New;
            New = pos;
        }
        public void Set(double pos)
        {
            Old = pos;
            New = pos;
        }
        public double GetInter(double ratio)
        {
            return New * ratio + Old * (1 - ratio);
        }
        public void Reset()
        {
            Old = -1;
            New = -1;
        }
    }

    public class Node
    {
        public NodeType type;
        public object content;

        public HashSet<Segment> segments;
        public HashSet<Segment> next;
        public HashSet<Segment> prev;
        public List<Node> children;
        public Node parent;
        public int level; //root=0;

        public PositionCache position = new PositionCache();
        public Node alignNext;
        public Node alignPrev;
        public int order;
        public bool isMustDraw = false;

        public Node root
        {
            get
            {
                if (this.type == NodeType.Root) return this;
                else
                {
                    Node n = this.parent;
                    while (n.type != NodeType.Root)
                        n = n.parent;
                    return n;
                }
            }
        }

        public Node(NodeType _type)
        {
            type = _type;
            level = 0;
            order = -1;
            segments = new HashSet<Segment>();
            next = new HashSet<Segment>();
            prev = new HashSet<Segment>();
            children = new List<Node>();
        }

        public Node(NodeType _type, object _content)
        {
            type = _type;
            level = 0;
            order = -1;
            content = _content;
            segments = new HashSet<Segment>();
            next = new HashSet<Segment>();
            prev = new HashSet<Segment>();
            children = new List<Node>();
        }

        public Node(NodeType _type, object _content, Segment _segment)
        {
            level = 0;
            order = -1;
            type = _type;
            content = _content;
            segments = new HashSet<Segment>();
            segments.Add(_segment);
            next = new HashSet<Segment>();
            prev = new HashSet<Segment>();
            children = new List<Node>();
        }
    }

    public class MyLocation
    {
        public List<Session> sessions;
        public MyLocation parent;
        public List<MyLocation> children;
        public int start;
        public int end;
        public string name;
        public Color color;
        public int index;

        public bool ExistAtThisFrame(int frame)
        {
            foreach (Session session in sessions)
            {
                if (session.start <= frame && session.end >= frame)
                    return true;
            }
            return false;
        }
    }

    public class Session
    {
        public int start = int.MaxValue; // start frame
        public int end = int.MinValue;
        public List<Segment> segments;
        public MyLocation parent;
    }

    public class Segment
    {
        public double weight;
        public Session session;
        public int id;
        public int frame;
        public Color color;
        public List<Session> prevSession;
        public List<Session> nextSession;
        public List<Segment> prev;
        public List<Segment> next;
    }
    public class lstory
    {
        public List<List<segmentread>> segments;
        public LocationNode LTree;

        public List<Character> Characters { get; set; }

        public int[] TimeStamps { get; set; }
        public Dictionary<int, int> TimeStampStartDic;
        public Dictionary<int, int> TimeStampEndDic;
        public int sessionCount;
        public int CC
        {
            get { return Characters.Count; }
        }

        public int FC;

        public string FileName;

        public static lstory ReadOriginalXml(string path)
        {
            lstory lstory = new lstory();
            lstory.FileName = path;
            List<string> getdata = new List<string>();
            XmlDocument xmlDoc = new XmlDocument();
            xmlDoc.Load(path);

            // 1.read locations and tree structure
            XmlNode xmlNode = xmlDoc.SelectSingleNode("Story/Locations");
            lstory.LTree = new LocationNode(-1, "root", "Black", false, null);
            if (xmlNode != null && xmlNode.HasChildNodes)
            {
                foreach (XmlNode locationnode in xmlNode.ChildNodes)
                {
                    ParseXmlNode(locationnode, lstory.LTree);
                }
            }
            // 2.read characters and session table
            lstory.Characters = new List<Character>();
            lstory.segments = new List<List<segmentread>>();
            HashSet<int> timestamps = new HashSet<int>();
            lstory.sessionCount = 0;
            xmlNode = xmlDoc.SelectSingleNode("Story/Characters");
            foreach (XmlNode characterNode in xmlNode.ChildNodes)
            {
                string name = characterNode.Attributes["Color"] != null ? characterNode.Attributes["Name"].Value : "";
                getdata.Add(name);
                string color = characterNode.Attributes["Color"] != null ? characterNode.Attributes["Color"].Value : "Black";
                double weight = characterNode.Attributes["Weight"] != null ? double.Parse(characterNode.Attributes["Weight"].Value) : 1.0;
                Character character = new Character(name, weight, color, lstory.Characters.Count);
                lstory.Characters.Add(character);
                lstory.segments.Add(new List<segmentread>());
                foreach (XmlNode spanNode in characterNode.ChildNodes)
                {
                    int start = int.Parse(spanNode.Attributes["Start"].Value);
                    getdata.Add(Convert.ToString(start));
                    int end = int.Parse(spanNode.Attributes["End"].Value);
                    getdata.Add(Convert.ToString(start));
                    timestamps.Add(start);
                    timestamps.Add(end);
                    int now = int.Parse(spanNode.Attributes["Session"].Value);
                    lstory.sessionCount = now > lstory.sessionCount ? now : lstory.sessionCount;
                    var prev = new List<int>();
                    var next = new List<int>();
                    double sweight = 1.0;
                    lstory.segments.Last().Add(new segmentread(start, end, prev, now, next, sweight));
                }
            }
            lstory.TimeStamps = timestamps.ToArray();

            for (int i = 0; i < lstory.Characters.Count; i++)
            {
                if (lstory.segments[i].Count == 1)
                {
                    lstory.segments[i][0].Prev.Add(-1);
                    lstory.segments[i][0].Next.Add(-1);
                }
                else
                {
                    #region >=2
                    int j = 0;
                    lstory.segments[i][j].Prev.Add(-1);
                    if (lstory.segments[i][j + 1].Start == lstory.segments[i][j].End)
                        lstory.segments[i][j].Next.Add(lstory.segments[i][j + 1].Now);
                    for (j = 1; j < lstory.segments[i].Count - 1; j++)
                    {
                        if (lstory.segments[i][j].Start == lstory.segments[i][j - 1].End)
                            lstory.segments[i][j].Prev.Add(lstory.segments[i][j - 1].Now);
                        if (lstory.segments[i][j].End == lstory.segments[i][j + 1].Start)
                            lstory.segments[i][j].Next.Add(lstory.segments[i][j + 1].Now);
                    }
                    lstory.segments[i][j].Next.Add(-1);
                    if (lstory.segments[i][j].Start == lstory.segments[i][j - 1].End)
                        lstory.segments[i][j].Prev.Add(lstory.segments[i][j - 1].Now);
                    #endregion
                }
            }
            Array.Sort(lstory.TimeStamps);
            lstory.FC = lstory.TimeStamps.Count() - 1;
            getdata.Add(Convert.ToString(lstory.FC));
            lstory.TimeStampStartDic = new Dictionary<int, int>();
            lstory.TimeStampEndDic = new Dictionary<int, int>();
            for (int i = 0; i < lstory.TimeStamps.Length - 1; i++)
            {
                lstory.TimeStampStartDic.Add(lstory.TimeStamps[i], i);
                lstory.TimeStampEndDic.Add(lstory.TimeStamps[i + 1], i);
            }
            lstory.sessionCount++;
            return lstory;
        }


        public static lstory Read(string path)
        {
            lstory lstory = new lstory();
            lstory.FileName = path;

            XmlDocument xmlDoc = new XmlDocument();
            xmlDoc.Load(path);

            // 1.read locations and tree structure
            XmlNode xmlNode = xmlDoc.SelectSingleNode("Story/Locations");
            lstory.LTree = new LocationNode(-1, "root", "Black", false, null);
            if (xmlNode != null && xmlNode.HasChildNodes)
            {
                foreach (XmlNode locationNode in xmlNode.ChildNodes)
                {
                    ParseXmlNode(locationNode, lstory.LTree);
                }
            }

            // 2.read characters and session table
            lstory.Characters = new List<Character>();
            lstory.segments = new List<List<segmentread>>();
            HashSet<int> timestamps = new HashSet<int>();
            lstory.sessionCount = 0;
            xmlNode = xmlDoc.SelectSingleNode("Story/Characters");
            foreach (XmlNode characterNode in xmlNode.ChildNodes)
            {
                string name = characterNode.Attributes["Color"] != null ? characterNode.Attributes["Name"].Value : "";


                string color = characterNode.Attributes["Color"] != null ? characterNode.Attributes["Color"].Value : "Black";
                double weight = characterNode.Attributes["Weight"] != null ? double.Parse(characterNode.Attributes["Weight"].Value) : 1.0;
                Character character = new Character(name, weight, color, lstory.Characters.Count);
                lstory.Characters.Add(character);
                lstory.segments.Add(new List<segmentread>());
                foreach (XmlNode spanNode in characterNode.ChildNodes)
                {
                    int start = int.Parse(spanNode.Attributes["Start"].Value);

                    int end = int.Parse(spanNode.Attributes["End"].Value);

                    timestamps.Add(start);
                    timestamps.Add(end);
                    string[] prevItems = spanNode.Attributes["Prev"].Value.Split(',');
                    var prev = prevItems.Select(item => int.Parse(item.Trim())).ToList();
                    int now = int.Parse(spanNode.Attributes["Now"].Value);
                    lstory.sessionCount = now > lstory.sessionCount ? now : lstory.sessionCount;
                    string[] nextItems = spanNode.Attributes["Next"].Value.Split(',');
                    var next = nextItems.Select(item => int.Parse(item.Trim())).ToList();
                    double sweight = double.Parse(spanNode.Attributes["Weight"].Value);
                    lstory.segments.Last().Add(new segmentread(start, end, prev, now, next, sweight));
                }
            }
            lstory.TimeStamps = timestamps.ToArray();
            Array.Sort(lstory.TimeStamps);
            lstory.FC = lstory.TimeStamps.Count() - 1;


            lstory.TimeStampStartDic = new Dictionary<int, int>();
            lstory.TimeStampEndDic = new Dictionary<int, int>();
            for (int i = 0; i < lstory.TimeStamps.Length - 1; i++)
            {
                lstory.TimeStampStartDic.Add(lstory.TimeStamps[i], i);
                lstory.TimeStampEndDic.Add(lstory.TimeStamps[i + 1], i);
            }
            lstory.sessionCount++;


            return lstory;
        }


        private static void ParseXmlNode(XmlNode xmlNode, LocationNode parentNode)
        {
            int id = xmlNode.Attributes["Id"] != null ? int.Parse(xmlNode.Attributes["Id"].Value) : -1;
            string name = xmlNode.Attributes["Name"] != null ? xmlNode.Attributes["Name"].Value : "Unnamed";
            string color = xmlNode.Attributes["Color"] != null ? xmlNode.Attributes["Color"].Value : "Black";
            bool visible = xmlNode.Attributes["Visible"] != null && bool.Parse(xmlNode.Attributes["Visible"].Value);
            string list = xmlNode.Attributes["Sessions"] != null ? xmlNode.Attributes["Sessions"].Value : "";
            var location = new LocationNode(id, name, color, visible, parentNode);
            string[] items = list.Split(',');
            foreach (string item in items)
            {
                if (!string.IsNullOrWhiteSpace(item))
                {
                    int sessionId = int.Parse(item.Trim());
                    location.Sessions.Add(sessionId);
                }
            }

            parentNode.Children.Add(location);

            foreach (XmlNode child in xmlNode.ChildNodes)
            {
                ParseXmlNode(child, location);
            }
        }
    }
    
}