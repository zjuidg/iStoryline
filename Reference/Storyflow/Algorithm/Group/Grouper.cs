using System.Collections.Generic;
using System.Linq;
using Microsoft.AspNetCore.Rewrite.Internal.UrlActions;
using Structure;
using Storyline;

namespace Algorithm.Group
{
    public class Grouper
    {
        private StorylineApp _app;

        public void group(Story story, HashSet<int> group)
        {
            Location location;
            if (story.Locations.Count < 1)
            {
                // no root location
                return;
            }
            else
            {
                location = story.Locations[0];
            }

            // add a session to the the location with largest amount of sessions
            var locationId = location.Id;
            int stubSessionId = story.Sessions.Max() + 1;
            story.Sessions.Add(stubSessionId);
            story._sessionToLocation.Add(stubSessionId, locationId);

            for (int frame = 0; frame < story.FrameCount; frame++)
            {
                HashSet<int> affectedSessions = new HashSet<int>();
                HashSet<int> excludeSessions = new HashSet<int>();
                for (int i = 0; i < story.Characters.Count; i++)
                {
                    var session = story.SessionTable[i, frame];
                    if (group.Contains(i))
                    {
                        affectedSessions.Add(session);
                    }
                    else
                    {
                        excludeSessions.Add(session);
                    }
                }
                
                foreach (var excludeSession in excludeSessions)
                {
                    affectedSessions.Remove(excludeSession);
                }

                for (int i = 0; i < story.Characters.Count; i++)
                {
                    var session = story.SessionTable[i, frame];
                    if (affectedSessions.Contains(session))
                    {
                        story.SessionTable[i, frame] = stubSessionId;
                    }
                }
            }    
        }
    }
}