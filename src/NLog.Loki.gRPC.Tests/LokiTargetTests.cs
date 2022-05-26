using System;
using NLog.Config;
using NLog.Layouts;
using NLog.Targets.Wrappers;
using NUnit.Framework;

namespace NLog.Loki.gRPC.Tests;

[TestFixture]
public class LokiTargetTests
{
    [Test]
    public void Write()
    {
        var configuration = new LoggingConfiguration();

        using var lokiTarget = new LokiTarget
        {
            Endpoint = "http://grafana.lvh.me:3100",
            IncludeMdlc = true,
            Labels = {
                new LokiTargetLabel {
                    Name = "env",
                    Layout = Layout.FromString("${basedir}")
                },
                new LokiTargetLabel {
                    Name = "server",
                    Layout = Layout.FromString("${machinename:lowercase=true}")
                },
                new LokiTargetLabel {
                    Name = "level",
                    Layout = Layout.FromString("${level:lowercase=true}")
                }
            }
        };

        var target = new BufferingTargetWrapper(lokiTarget)
        {
            BufferSize = 500
        };

        configuration.AddTarget("loki", target);

        var rule = new LoggingRule("*", LogLevel.Debug, target);
        configuration.LoggingRules.Add(rule);

        LogManager.Configuration = configuration;

        var log = LogManager.GetLogger(typeof(LokiTargetTests).FullName);

        for(var n = 0; n < 100; ++n)
        {
            using(MappedDiagnosticsLogicalContext.SetScoped("env", "dev"))
            {
                log.Fatal("Hello world");
            }

            using(MappedDiagnosticsLogicalContext.SetScoped("server", Environment.MachineName))
            {
                log.Info($"hello again {n}");

                log.Info($"hello again {n * 2}");
                log.Warn($"hello again {n * 3}");
            }

            using(MappedDiagnosticsLogicalContext.SetScoped("cfg", "v1"))
                log.Error($"hello again {n * 4}");

            try
            {
                throw new InvalidOperationException();
            }
            catch(Exception e)
            {
                log.Error(e);
            }
        }

        LogManager.Shutdown();
    }
}

