using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Grpc.Core;

namespace GrpcDemo.Client
{
    class Program
    {
        private static Channel _channel;

        static void Main(string[] args)
        {
            _channel = new Channel("127.0.0.1", 51000, ChannelCredentials.Insecure);
            _channel.ConnectAsync().Wait();
            //GetDataUnary();
            //Task.Run(() => GetDataServerStreaming()).Wait();
            //Task.Run(() => GetDataClientStreaming()).Wait();
            //Task.Run(() => GetDataBidirectionalStreaming()).Wait();
            Task.Run(() => PubSub()).Wait();

        }

        static DataAccessService.DataAccessServiceClient GetClient()
        {
            return new DataAccessService.DataAccessServiceClient(_channel);
        }

        static void GetDataUnary()
        {
            var client = GetClient();
            var response = client.GetDataUnary(new DataRequest());
            Console.WriteLine("Received {0} messages from the server", response.Record.Count);
        }

        static async Task GetDataServerStreaming()
        {
            var client = GetClient();
            using (var call = client.GetDataServerStreaming(new DataRequest()))
            {
                while (await call.ResponseStream.MoveNext())
                {
                    var resp = call.ResponseStream.Current;
                    Console.WriteLine("Received message Id {0} from the server", resp.Id);
                }
            }

            //Console.WriteLine("Received {0} messages from the server", response.Record.Count);
        }

        static async Task GetDataClientStreaming()
        {
            var client = GetClient();
            using (var call = client.GetDataClientStreaming())
            {
                for (int i = 1; i <= 1000; i++)
                {
                    Console.WriteLine("Requesting message Id {0} from the server", i);
                    await call.RequestStream.WriteAsync(new DataRequestRecord { Id = i });
                }

                await call.RequestStream.CompleteAsync();

                var resp = await call.ResponseAsync;
                foreach (var record in resp.Record)
                {
                    Console.WriteLine("Received message Id {0} from the server", record.Id);

                }
            }

        }

        static async Task GetDataBidirectionalStreaming()
        {
            var client = GetClient();
            using (var call = client.GetDataBidirectionalStreaming())
            {
                //Start a task to begin reading the response so we can get start getting responses before all of the requests are completed
                var responseTask = Task.Run(async () =>
                {
                    while (await call.ResponseStream.MoveNext())
                    {
                        var resp = call.ResponseStream.Current;
                        Console.WriteLine("Received message Id {0} from the server", resp.Id);
                    }
                });

                for (int i = 1; i <= 1000; i++)
                {
                    Console.WriteLine("Requesting message Id {0} from the server", i);
                    await call.RequestStream.WriteAsync(new DataRequestRecord { Id = i });
                }

                await call.RequestStream.CompleteAsync();

                await responseTask;

            }
        }

        static async Task PubSub()
        {

            var client = new PubSubService.PubSubServiceClient(_channel);

            using (var call = client.Subscribe())
            {
                var responseTask = Task.Run(async () =>
                {
                    while (await call.ResponseStream.MoveNext())
                    {
                        var resp = call.ResponseStream.Current;
                        Console.WriteLine("Received message Id {0} from the server", resp.Id);
                    }
                });

                Console.WriteLine("Subscribing to channel1");
                await call.RequestStream.WriteAsync(new PubSubRequest { Id = Guid.NewGuid().ToString(), ChannelName = "channel1" });

                await call.RequestStream.CompleteAsync();

                await responseTask;

            }
        }
    }
}
