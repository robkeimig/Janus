using System.IO;
using Microsoft.AspNetCore;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace Janus.Service
{
    class Program
    {
        public static string WwwRootParentPath => Directory.GetCurrentDirectory();

        static void Main(string[] args)
        {
            var host = WebHost
              .CreateDefaultBuilder()
              .UseKestrel()
              .UseStartup<WebStartup>()
              .UseContentRoot(WwwRootParentPath)
              .UseUrls("http://*:8080")
              .Build();

            host.Run();
        }

        public class WebStartup
        {
            public IConfiguration Configuration { get; }

            public WebStartup(IConfiguration configuration)
            {
                Configuration = configuration;
            }
            public void ConfigureServices(IServiceCollection services)
            {
                services.AddMvc();
            }
            public void Configure(IApplicationBuilder app, IHostingEnvironment env)
            {
                if (env.IsDevelopment())
                {
                    app.UseDeveloperExceptionPage();
                }

                app.UseWebSockets();
                app.UseStreamingVideo();
                app.UseDefaultFiles();
                app.UseStaticFiles();
                app.UseMvc();
            }
        }
    }
}
