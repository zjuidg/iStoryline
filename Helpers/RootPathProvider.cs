using Nancy;
using System.IO;

namespace StorylineBackend.Helpers
{
    public class StorylineRootPathProvider : IRootPathProvider
    {
        public string GetRootPath() 
        {
            return Directory.GetCurrentDirectory();
        }
    }
}