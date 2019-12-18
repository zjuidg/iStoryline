using System;
using System.IO;
using System.Threading.Tasks;
using Newtonsoft.Json;
using Structure;
using SQLitePCL;
using Storyline;
using StorylineBackend.models;
using StorylineBackend.upload;
using WebSocketSharp;
using WebSocketSharp.Server;

namespace StorylineBackend.Layout
{
        public class LayoutRequestHandler : WebSocketBehavior
        {
            public const string ACTION_SET_ID = "setId";
            public const string ACTION_UPDATE_LAYOUT = "updateLayout";
            private int count = 0;
            private string id;
            private Story Story;
            
            protected override async void OnMessage (MessageEventArgs e)
            {
                var rawString = e.Data;
                var request = JsonConvert.DeserializeObject<Request<Object>>(rawString);
                switch (request.action)
                {
                        case ACTION_SET_ID:
                            id = JsonConvert.DeserializeObject<Request<string>>(rawString).payload;
                            Story = await Task.Factory.StartNew(() => Story.Read(GetTargetFile(id)));
                            break;
                        case ACTION_UPDATE_LAYOUT:
                            
                            if (Story == null)
                            {
                                // fail
//                                Send();
                            }
                            else
                            {
                                var updateRequest = JsonConvert.DeserializeObject<Request<UpdateRequest>>(rawString)
                                    .payload;
                                var storylineApp = new StorylineApp();
                                LayoutHandler.ApplyUpdateConfig(updateRequest, storylineApp);
                                await Task.Factory.StartNew(() => storylineApp.SolveStory(Story));
                                var result = await LayoutHandler.postProcess(storylineApp._relaxedPos, Story, storylineApp);
                                
                                Send(JsonConvert.SerializeObject(result));
                            }
                            break;
                }
            }
            
            private string GetTargetFile(string fileName)
            {
                return LayoutHandler.GetTargetFile(fileName);
            }
        }
}