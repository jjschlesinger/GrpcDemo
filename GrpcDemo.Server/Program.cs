using System.Linq;
using Grpc.Core;
using Grpc.Core.Logging;
using Serilog;
using Topshelf;
using Topshelf.Configurators;

namespace GrpcDemo.Server
{
    class Program
    {

        static void Main(string[] args)
        {
            Log.Logger = new LoggerConfiguration()
                .WriteTo.ColoredConsole()
                .CreateLogger();

            HostFactory.Run(c =>
            {
                c.Service<GrpcDemoService>(s =>
                {
                    s.ConstructUsing(() => new GrpcDemoService());
                    s.WhenStarted(grpc => grpc.Start());
                    s.WhenStopped(grpc => grpc.Stop());
                });

                c.UseSerilog();
            });
        }
        
    }

    class GrpcDemoService
    {
        private Grpc.Core.Server _server;

        public void Start()
        {
            GrpcEnvironment.SetLogger(new ConsoleLogger());

            _server = new Grpc.Core.Server();
            _server.Ports.Add("127.0.0.1", 51000, ServerCredentials.Insecure);
            Log.Information("Starting gRPC server: {Host}:{Port}", _server.Ports.Single().Host, _server.Ports.Single().Port);
            _server.Services.Add(DataAccessService.BindService(new DataAccessServiceImpl()));
            _server.Services.Add(PubSubService.BindService(new PubSubServiceImpl()));
            _server.Start();
        }

        public void Stop()
        {
            _server.ShutdownAsync().Wait();
        }
    }
}
