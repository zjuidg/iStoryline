using System;
using System.IO;
using Microsoft.AspNetCore.Hosting;
using Nancy.Json;
using Newtonsoft.Json;
using WebSocketSharp;
using WebSocketSharp.Server;

namespace StorylineBackend
{

    public class Program
    {
        public static void Main(string[] args)
        {
            var fileServer = new WebHostBuilder()
                .UseContentRoot(Directory.GetCurrentDirectory())
                .UseKestrel()
                .UseStartup<Startup>()
                .UseUrls("http://localhost:5050")
                .Build();

            var webSocketServer = new WebSocketServer(5051);
//            webSocketServer.AddWebSocketService<Layout> ("/");
            webSocketServer.Start();
            fileServer.Run();

        }
    }
}
