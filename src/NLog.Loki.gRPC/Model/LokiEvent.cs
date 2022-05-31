using System;

namespace NLog.Loki.Grpc.Model
{
    public class LokiEvent
    {
        public LokiLabels Labels { get; }

        public DateTime Timestamp { get; }

        public string Line { get; }

        public LokiEvent(LokiLabels labels, DateTime timestamp, string line)
        {
            Labels = labels ?? throw new ArgumentNullException(nameof(labels));
            Timestamp = timestamp;
            Line = line ?? throw new ArgumentNullException(nameof(line));
        }
    }
}
