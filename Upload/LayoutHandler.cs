using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authentication;
using Microsoft.Extensions.Configuration;
using Nancy;
using Nancy.Json;
using Storyline;
using Structure;

namespace StorylineBackend.upload
{
    public interface ILayoutHandler
    {
        Task<LayoutResult> handleLayout(string id);
        Task<LayoutResult> updateLayout(UpdateRequest request);
    }

    public class LayoutResult
    {
        public List<CharacterInfo> array = new List<CharacterInfo>();
        public List<List<int>> sessionTable;
        public List<List<int>> perm;
    }

    public class CharacterInfo
    {
        public int character_id = 0;
        public string name = "";
        public List<Tuple<double, double, double>> points = new List<Tuple<double, double, double>>();
    }


    public class LayoutHandler : ILayoutHandler
    {
        private readonly IConfiguration _applicationSettings;
        private readonly IRootPathProvider _rootPathProvider;

        public static string UploadDirectory;

        public LayoutHandler(IConfiguration applicationSettings, IRootPathProvider rootPathProvider)
        {
            _applicationSettings = applicationSettings;
            _rootPathProvider = rootPathProvider;
            InitUploadDirectory();
        }

        public async Task<LayoutResult> handleLayout(string id)
        {
            string filePath = GetTargetFile(id);
            if (File.Exists(filePath))
            {
                var story = await Task.Factory.StartNew(() => Story.Read(filePath));
                var storylineApp = new StorylineApp();
                await Task.Factory.StartNew(() => storylineApp.SolveStory(story));
                var result = await postProcess(storylineApp._relaxedPos, story, storylineApp);
                return result;
            }
            else
            {
                return null;
            }
        }

        public async Task<LayoutResult> updateLayout(UpdateRequest updateRequest)
        {
            string filePath = GetTargetFile(updateRequest.id);
            if (File.Exists(filePath))
            {
                var story = await Task.Factory.StartNew(() => Story.Read(filePath));
                var storylineApp = new StorylineApp();

                ApplyUpdateConfig(updateRequest, storylineApp);

                await Task.Factory.StartNew(() => storylineApp.SolveStory(story));
                var result = await postProcess(storylineApp._relaxedPos, story, storylineApp);
                return result;
            }
            else
            {
                return null;
            }
        }

        public static void ApplyUpdateConfig(UpdateRequest updateRequest, StorylineApp storylineApp)
        {
            var statusConfig = storylineApp.Status.Config;
            if (updateRequest == null)
            {
                return;
            }
            if (updateRequest.sessionInnerGap != 0.0)
            {
                statusConfig.Style.DefaultInnerGap = updateRequest.sessionInnerGap;
            }

            if (updateRequest.sessionOuterGap != 0)
            {
                statusConfig.Style.OuterGap = updateRequest.sessionOuterGap;
            }

            if (updateRequest.sessionInnerGaps != null)
            {
                statusConfig.sessionInnerGaps = updateRequest.sessionInnerGaps;
            }

            if (updateRequest.characterYConstraints != null)
            {
                statusConfig.CharacterYConstraints = updateRequest.characterYConstraints;
            }

            if (updateRequest.orders != null)
            {
                statusConfig.Orders.Clear();
                updateRequest.orders.ForEach(ints =>
                {
                    statusConfig.Orders.Add(new Tuple<int, int>(ints[0], ints[1]));
                });
            }

            if (updateRequest.orderTable != null)
            {
                statusConfig.OrderTable.Clear();
                updateRequest.orderTable.ForEach(pair =>
                {
                    statusConfig.OrderTable.Add(new Tuple<int, List<int>>(pair.Item1, pair.Item2));
                });
            }

            if (updateRequest.sessionOuterGaps != null)
            {
                statusConfig.sessionOuterGaps = updateRequest.sessionOuterGaps;
            }

            if (updateRequest.groupIds != null)
            {
                statusConfig.GroupIds = updateRequest.groupIds;
            }

            if (updateRequest.selectedSessions != null)
            {
                statusConfig.SelectedSessions = updateRequest.selectedSessions;
            }

            if (updateRequest.majorCharacters != null)
            {
                storylineApp.Status.Config.MajorCharacters = updateRequest.majorCharacters;
            }

            if (updateRequest.sessionBreaks != null)
            {
                statusConfig.SessionBreaks = updateRequest.sessionBreaks;
            }
        }

        public static Task<LayoutResult> postProcess(Tuple<double, double, double>[][] relaxedPos, Story story, StorylineApp app)
        {
            if (relaxedPos.Length != story.Characters.Count)
            {
                throw new ArgumentException("relaxedPos mismatched story.characters.");
            }

            return Task.Factory.StartNew(() =>
            {
                LayoutResult result = new LayoutResult();
                result.sessionTable = new List<List<int>>();
                result.perm = new List<List<int>>();
                
                for (int i = 0; i < story.Characters.Count; i++)
                {
                    var character = story.Characters[i];
                    var id = character.Id == 0 ? i : character.Id;
                    var characterInfo = new CharacterInfo();
                    characterInfo.character_id = id;
                    characterInfo.name = character.Name;
                    var positions = relaxedPos[i];
                    characterInfo.points.AddRange(positions);
                    result.array.Add(characterInfo);
                    
                    var characterFrameSession = new List<int>();
                    for (int j = 0; j < story.FrameCount; j++)
                    {
                        characterFrameSession.Add(story.SessionTable[i, j]);
                    }
                    result.sessionTable.Add(characterFrameSession);

                    var permOfChar = new List<int>();
                    for (int j = 0; j < story.FrameCount; j++)
                    {
                        permOfChar.Add(app._perm[i, j]);
                    }
                    result.perm.Add(permOfChar);
                }

                return result;
            });
        }

        public static string GetTargetFile(string fileName)
        {
            return Path.Combine(UploadDirectory, fileName);
        }

        private void InitUploadDirectory()
        {
            var uploadDirectory =
                Path.Combine(_rootPathProvider.GetRootPath(), _applicationSettings["UploadDirectory"]);

            if (!Directory.Exists(uploadDirectory))
            {
                Directory.CreateDirectory(uploadDirectory);
            }

            UploadDirectory = uploadDirectory;
        }
    }
}