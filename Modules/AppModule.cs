using Nancy;

namespace StorylineBackend.modules
{
    public class AppModule: NancyModule
    {
        public AppModule()
        {
            Get("/", args => View["index"]);
        }
    }
}