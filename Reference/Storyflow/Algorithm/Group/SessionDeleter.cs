using System.Collections.Generic;
using System.Linq;
using Microsoft.EntityFrameworkCore;
using Structure;

namespace Storyline
{
    public class SessionDeleter
    {
        public void delete(Story story, List<int> sessionsToDelete)
        {
            var newSessions = new Dictionary<int, int>();
            var sessions = sessionsToDelete.ToHashSet();
            var count = story.Sessions.Count;
            for (int i = 0; i < story.Characters.Count; i++)
            {
                for (int j = 0; j < story.FrameCount; j++)
                {
                    var session = story.SessionTable[i, j];
                    if (sessions.Contains(session))
                    {
                        int newSession;
                        if (!newSessions.TryGetValue(session, out newSession))
                        {
                            newSession = story.Sessions.Max() + 1;
                            story.Sessions.Remove(session);
                            story.Sessions.Add(newSession);
                            story._sessionToLocation.Add(newSession, story._sessionToLocation[session]);
//                            story._sessionToLocation.Remove(session);
//                            story.Locations.Sessions is not used in any where no bother to update it
                        }
                        story.SessionTable[i, j] = newSession;
                    }
                }
            }
        }
    }
}