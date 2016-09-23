using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Grpc.Core;
using Serilog;

namespace GrpcDemo.Server
{
    public class PubSubServiceImpl : PubSubService.PubSubServiceBase
    {
        private static readonly ConcurrentDictionary<string, ConcurrentBag<PubSubRequest>> _subscriptions = new ConcurrentDictionary<string, ConcurrentBag<PubSubRequest>>();


        public override async Task Subscribe(IAsyncStreamReader<PubSubRequest> requestStream, IServerStreamWriter<PubSubResponse> responseStream, ServerCallContext context)
        {
            var requestTask = Task.Run(async () =>
            {
                while (await requestStream.MoveNext())
                {
                    var req = requestStream.Current;
                    Log.Information("Client subscribed to channel: {req}", req);
                    ConcurrentBag<PubSubRequest> concurrentBag;

                    _subscriptions.TryGetValue(req.ChannelName, out concurrentBag);

                    if (concurrentBag == null)
                        concurrentBag = new ConcurrentBag<PubSubRequest>();

                    concurrentBag.Add(req);

                    _subscriptions.AddOrUpdate(req.ChannelName, concurrentBag, (s, bag) => bag);

                }
            });

            var fsw = new FileSystemWatcher("c:\\tmp\\grpcdemo")
            {
                EnableRaisingEvents = true,
                IncludeSubdirectories = true
            };

            fsw.Created += async (sender, args) =>
            {
                Log.Information("FileSystemWatcher Created Event Raised: {args}", args);
                //TODO: See if the channel name is subscribed to by the client--use the name of the directory the file is in as the channel name
                if (File.Exists(args.FullPath))
                {
                    var fi = new FileInfo(args.FullPath);
                    var channelName = fi.Directory.Name;
                    if (!String.IsNullOrEmpty(channelName))
                    {
                        var msg = PubSubMessage.Parser.ParseJson(await fi.OpenText().ReadToEndAsync());
                        if (_subscriptions.ContainsKey(channelName))
                        {
                            Log.Information("Found {channelName} in subscriptions");
                            foreach (var req in _subscriptions[channelName])
                            {
                                var resp = new PubSubResponse {ChannelName = channelName, Id = req.Id, Message = msg};
                                Log.Information("Publishing message to client: {resp}", resp);
                                await responseStream.WriteAsync(resp);
                            }
                        }
                    }
                }
            };

            while (true)
            {
                await Task.Delay(1000, context.CancellationToken);
            }

            //await requestTask;
        }


    }
}
