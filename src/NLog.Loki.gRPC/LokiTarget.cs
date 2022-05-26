using System;
using System.Collections.Generic;
using System.IO.Compression;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Grpc.Net.Client;
using Logproto;
using NLog.Config;
using NLog.Layouts;
using NLog.Loki.gRPC.Model;
using NLog.Targets;
using static Logproto.Pusher;

namespace NLog.Loki
{
    [Target("loki.grpc")]
    public class LokiTarget : AsyncTaskTarget
    {
        private GrpcChannel _channel;
        private PusherClient _pusherClient;

        [RequiredParameter]
        public string Endpoint { get; set; }

        public Layout Username { get; set; }

        public Layout Password { get; set; }

        /// <summary>
        /// Orders the logs by timestamp before sending them to Loki.
        /// Required as <see langword="true"/> before Loki v2.4. Leave as <see langword="false"/> if you are running Loki v2.4 or above.
        /// See <see href="https://grafana.com/docs/loki/latest/configuration/#accept-out-of-order-writes"/>.
        /// </summary>
        public bool OrderWrites { get; set; } = true;

        /// <summary>
        /// Defines if the HTTP messages sent to Loki must be gzip compressed, and with which compression level.
        /// Possible values: NoCompression (default), Optimal, Fastest and SmallestSize (.NET 6 support only).
        /// </summary>
        public CompressionLevel CompressionLevel { get; set; } = CompressionLevel.NoCompression;

        [ArrayParameter(typeof(LokiTargetLabel), "label")]
        public IList<LokiTargetLabel> Labels { get; } = new List<LokiTargetLabel>();

        protected override void InitializeTarget()
        {
            base.InitializeTarget();

            _channel = GrpcChannel.ForAddress(Endpoint);
            _pusherClient = new PusherClient(_channel);
        }

        protected override void CloseTarget()
        {
            _channel?.Dispose();
            base.CloseTarget();
        }

        protected override async Task WriteAsyncTask(LogEventInfo logEvent, CancellationToken cancellationToken)
        {
            var @event = GetLokiEvent(logEvent);
            var stream = new StreamAdapter { Labels = FormatLabels(@event.Labels.Labels) };
            stream.Entries.Add(new EntryAdapter()
            {
                Line = @event.Line,
                Timestamp = new Google.Protobuf.WellKnownTypes.Timestamp
                {
                    Seconds = (long)ConvertToUnixTimestamp(@event.Timestamp)
                }
            });

            var query = new PushRequest();
            query.Streams.Add(stream);
            _ = await _pusherClient.PushAsync(query);
        }

        protected override async Task WriteAsyncTask(IList<LogEventInfo> logEvents, CancellationToken cancellationToken)
        {
            var events = logEvents.Select(GetLokiEvent);
            var streams = events
                .GroupBy(le => le.Labels)
                .Select((gp) =>
                {
                    var stream = new StreamAdapter { Labels = FormatLabels(gp.Key.Labels) };
                    foreach(var @event in gp)
                        stream.Entries.Add(new EntryAdapter()
                        {
                            Line = @event.Line,
                            Timestamp = new Google.Protobuf.WellKnownTypes.Timestamp
                            {
                                Seconds = (long)ConvertToUnixTimestamp(@event.Timestamp)
                            }
                        });
                    return stream;
                });

            var query = new PushRequest();
            query.Streams.AddRange(streams);
            _ = await _pusherClient.PushAsync(query);
        }

        private static double ConvertToUnixTimestamp(DateTime date)
        {
            var origin = new DateTime(1970, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc);
            var diff = date.ToUniversalTime() - origin;
            return Math.Floor(diff.TotalSeconds);
        }

        private string FormatLabels(ISet<LokiLabel> labels)
        {
            // Estimate capacity
            var capacity = 1;
            foreach(var l in labels)
                capacity += l.Label.Length + l.Value.Length + 4;
            var strBuilder = new StringBuilder("{", capacity);
            foreach(var l in labels)
            {
                strBuilder = strBuilder
                    .Append(l.Label)
                    .Append("=\"")
                    .Append(l.Value)
                    .Append("\",");
            }
            strBuilder.Length -= 1;
            strBuilder = strBuilder.Append('}');

            return strBuilder.ToString();
        }

        private LokiEvent GetLokiEvent(LogEventInfo logEvent)
        {
            var lokiLabels = Labels.Select(label => new LokiLabel(label.Name, label.Layout.Render(logEvent)));
            var labels = new LokiLabels(lokiLabels);

            var line = RenderLogEvent(Layout, logEvent);
            return new LokiEvent(labels, logEvent.TimeStamp, line);
        }

        internal static ILokiHttpClient GetLokiHttpClient(Uri uri, string username, string password)
        {
            var httpClient = new HttpClient { BaseAddress = uri };
            if(!string.IsNullOrEmpty(username) && !string.IsNullOrEmpty(password))
            {
                var byteArray = Encoding.ASCII.GetBytes($"{username}:{password}");
                httpClient.DefaultRequestHeaders.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Basic", Convert.ToBase64String(byteArray));
            }
            return new LokiHttpClient(httpClient);
        }

        private bool _isDisposed;
        protected override void Dispose(bool isDisposing)
        {
            if(!_isDisposed)
            {
                if(isDisposing)
                {
                    _channel?.Dispose();
                }
                _isDisposed = true;
            }
            base.Dispose(isDisposing);
        }
    }
}
