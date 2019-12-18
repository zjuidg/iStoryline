using Microsoft.AspNetCore.SpaServices.Webpack;

namespace StorylineBackend
{
    using Microsoft.AspNetCore.Builder;
    using Microsoft.Owin.Cors;
    using Nancy.Owin;
    using Microsoft.AspNetCore.Hosting;
    using Microsoft.EntityFrameworkCore;
    using Microsoft.Extensions.DependencyInjection;

    public class Startup
    {
        public void ConfigureServices(IServiceCollection services)
        {
        }

        public void Configure(IApplicationBuilder app, IHostingEnvironment env)
        {
            app.UseOwin(x => x.UseNancy());
            app.UseStaticFiles();
        }
    }
}
