using System.Collections.Generic;
using System.Linq;
using Structure;
using Storyline;
using StorylineBackend.upload;

namespace StorylineBackend.Reference.Storyflow.Algorithm.Group
{
    public class SessionBreaker
    {
        private StorylineApp _app;
        private Story _story;

        public SessionBreaker(StorylineApp app, Story story)
        {
            _app = app;
            _story = story;
        }

        public void BreakAt(List<SessionBreak> sessionBreaks)
        {
            // 
            // 1. break the segment 
            // 2. add y constraints
            int max = -2;
            int maxSession = -1;
            for (int i = 0; i < _story.Characters.Count; i++)
            {
                for (int j = 0; j < _story.FrameCount; j++)
                {
                    if (_app._segments[i, j] > max)
                    {
                        max = _app._segments[i, j];
                    }
                }
            }
            
            for (int i = 0; i < _story.Characters.Count; i++)
            {
                for (int j = 0; j < _story.FrameCount; j++)
                {
                    if (_story.SessionTable[i, j] > max)
                    {
                        maxSession = _story.SessionTable[i, j];
                    }
                }
            }

            maxSession++;
            int next = max + 1;
            foreach (var sessionBreak in sessionBreaks)
            {
                for (int i = 0; i < _story.Characters.Count; i++)
                {
                    for (int j = 0; j < _story.FrameCount - 1; j++)
                    {
                        if (sessionBreak.session1 == _story.SessionTable[i, j] && sessionBreak.session2 == _story.SessionTable[i, j + 1]
                        && j == sessionBreak.frame && 
                        // filter rabbit session
                        sessionBreak.session1 != RabbitAdder.rabbitSession && sessionBreak.session2 != RabbitAdder.rabbitSession)
                        {
                            int oldSeg = _app._segments[i, j + 1];
                            int oldSession = _story.SessionTable[i, j + 1];

                            // replace all identical segs after (aligned)
                            for (int k = j + 1; k < _story.FrameCount; k++)
                            {
                                if (oldSeg == _app._segments[i, k])
                                {
                                    _app._segments[i, k] = next;   
                                }

//                                if (oldSession == _story.SessionTable[i, k])
//                                {
//                                    _story.SessionTable[i, k] = maxSession;
//                                }
                            }
                            break;
                        }
                    }
                    next++;
                    maxSession++;
                }
            }
        }
    }
}