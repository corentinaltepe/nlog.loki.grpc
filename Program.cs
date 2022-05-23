using Grpc.Core;
using Grpc.Net.Client;
using Logproto;
using static Logproto.Pusher;

Console.WriteLine("Hello World");

using var channel = GrpcChannel.ForAddress("http://localhost:9095");
var client = new PusherClient(channel);

Console.WriteLine("Sending query");
var now = new Google.Protobuf.WellKnownTypes.Timestamp();
now.Seconds = (long)ConvertToUnixTimestamp(DateTime.Now);
var stream = new StreamAdapter() { Labels = "{level=\"info\",server=\"corentin\",location=\"here\"}" };
stream.Entries.Add(new EntryAdapter() { Line = "This is my log 1", Timestamp = now });
stream.Entries.Add(new EntryAdapter() { Line = "This is my log 2", Timestamp = now });
stream.Entries.Add(new EntryAdapter() { Line = "This is my log 3", Timestamp = now });
Console.WriteLine("stream: " + stream);

var query = new PushRequest();
query.Streams.Add(stream);

var response = client.Push(query, new CallOptions());
Console.WriteLine("End of program: " + response);

static double ConvertToUnixTimestamp(DateTime date)
{
    var origin = new DateTime(1970, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc);
    var diff = date.ToUniversalTime() - origin;
    return Math.Floor(diff.TotalSeconds);
}