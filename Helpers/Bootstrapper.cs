using Microsoft.Extensions.Configuration;
using Nancy;
using Nancy.Configuration;
using Nancy.Conventions;
using Nancy.TinyIoc;
using StorylineBackend.upload;

namespace StorylineBackend.Helpers
{
    public class StorylineBackendBootstrapper : DefaultNancyBootstrapper
    {
        public IConfigurationRoot Configuration;
        private IFileUploadHandler _fileUploadHandler;
        private ILayoutHandler _layoutHandler;
        
        public StorylineBackendBootstrapper()
        {
            var builder = new ConfigurationBuilder()
                .SetBasePath(RootPathProvider.GetRootPath())
                .AddJsonFile("appsettings.json")
                .AddEnvironmentVariables();

            Configuration = builder.Build();
            // auto denpendency injection not working, mannually new this
            _fileUploadHandler = new UploadHandler(Configuration, RootPathProvider);
            _layoutHandler = new LayoutHandler(Configuration, RootPathProvider);
        }
        
        protected override void ConfigureApplicationContainer(TinyIoCContainer ctr)
        {
            ctr.Register<IConfiguration>(Configuration);
            ctr.Register(_layoutHandler);
            ctr.Register(_fileUploadHandler);
        }

        protected override void ConfigureConventions(NancyConventions nancyConventions)
        {
            base.ConfigureConventions(nancyConventions);
            nancyConventions.StaticContentsConventions.Add(StaticContentConventionBuilder.AddDirectory("/", "Content"));
        }

        public override void Configure(INancyEnvironment environment)
        {
            base.Configure(environment);
            
            environment.Tracing(enabled: false, displayErrorTraces: true);
        }
    }
}