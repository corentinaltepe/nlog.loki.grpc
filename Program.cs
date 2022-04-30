using Grpc.Core;
using Grpc.Net.Client;
using Logproto;
using static Logproto.Pusher;

Console.WriteLine("Hello World");

using var channel = GrpcChannel.ForAddress("http://localhost:9095");
var client = new PusherClient(channel);


var query = new PushRequest();
var response = client.Push(query, new CallOptions());
Console.WriteLine("End of program: " + response);