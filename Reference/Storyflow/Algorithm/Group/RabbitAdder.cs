using System.Collections.Generic;
using Structure;

namespace StorylineBackend.Reference.Storyflow.Algorithm.Group
{
    
    public class RabbitAdder
    {
        public const int rabbitSession = 9999;
        // rabbit is virtual character with character id of largest
        // stay in session 0 in all time frame
        // so other session can use session 0 as a 
        public static void AddRabbit(Story story)
        {
            var rabbit = new Character("RABBIT", 1, "", story.Characters.Count);
            story.rabbitId = rabbit.Id = story.Characters.Count;
            story.Characters.Add(rabbit);
            var characters = new HashSet<int>();
            characters.Add(rabbit.Id);
            story.sessionToCharacteres.Add(rabbitSession, characters);
            var newSessionTable = new SessionTable(story.Characters.Count, story.FrameCount);
            story._sessionToLocation.Add(rabbitSession, story.LocationRoot.Id);
            // copy old one
            for (int id = 0; id < story.Characters.Count - 1; ++id)
            {
                for (int frame = 0; frame < story.TimeStamps.Length - 1; ++frame)
                {
                    newSessionTable[id, frame] = story.SessionTable[id, frame];
                }
            }
            
            for (int frame = 0; frame < story.TimeStamps.Length - 1; ++frame)
            {
                newSessionTable[rabbit.Id, frame] = rabbitSession;
            }

            story.SessionTable = newSessionTable;
        }
    }
}