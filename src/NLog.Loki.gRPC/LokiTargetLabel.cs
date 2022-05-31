using NLog.Config;
using NLog.Layouts;

namespace NLog.Loki.Grpc
{
    [NLogConfigurationItem]
    public class LokiTargetLabel
    {
        [RequiredParameter]
        public string Name { get; set; }

        [RequiredParameter]
        public Layout Layout { get; set; }
    }
}
