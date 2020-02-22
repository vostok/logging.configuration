using System.Collections.Generic;
using FluentAssertions;
using NUnit.Framework;
using Vostok.Logging.Abstractions;

namespace Vostok.Logging.Configuration.Tests
{
    [TestFixture]
    internal class ConfigurableLog_Tests
    {
        private TestLog log1;
        private TestLog log2;
        private TestLog log3;
        private ConfigurableLogBuilder builder;

        [SetUp]
        public void TestSetup()
        {
            log1 = new TestLog();
            log2 = new TestLog();
            log3 = new TestLog();
            
            builder = new ConfigurableLogBuilder()
                .AddLog("Log1", log1)
                .AddLog("Log2", log2)
                .AddLog("Log3", log3);
        }


        [Test]
        public void Should_support_routing_by_source_context()
        {
            var log = builder
                .SetRules(new []
                {
                    new LogConfigurationRule { Log = "log1", Enabled = false}, 
                    new LogConfigurationRule { Log = "log1", Source = "src1", Enabled = true}, 
                    new LogConfigurationRule { Log = "log2", Enabled = false},
                    new LogConfigurationRule { Log = "log2", Source = "src2", Enabled = true},
                    new LogConfigurationRule { Log = "log3", Source = "src1", Enabled = false},
                    new LogConfigurationRule { Log = "log3", Source = "src2", Enabled = false}
                })
                .Build();

            log.ForContext("src1").Info("src1");
            log.ForContext("src2").Info("src2");
            log.ForContext("src3").Info("src3");
            log.ForContext("src4").Info("src4");

            log1.Events.Should().ContainSingle().Which.MessageTemplate.Should().Be("src1");
            log2.Events.Should().ContainSingle().Which.MessageTemplate.Should().Be("src2");

            log3.Events.Should().HaveCount(2);
            log3.Events[0].MessageTemplate.Should().Be("src3");
            log3.Events[1].MessageTemplate.Should().Be("src4");
        }

        private class TestLog : ILog
        {
            public List<LogEvent> Events { get; } = new List<LogEvent>();

            public void Log(LogEvent @event)
                => Events.Add(@event);

            public bool IsEnabledFor(LogLevel level) => true;

            public ILog ForContext(string context) => this;
        }
    }
}
