using System.Collections.Generic;
using FluentAssertions;
using NSubstitute;
using NUnit.Framework;
using Vostok.Logging.Abstractions;
using Vostok.Logging.Abstractions.Wrappers;
using Vostok.Logging.Configuration.Helpers;
using Vostok.Logging.Context;

namespace Vostok.Logging.Configuration.Tests
{
    [TestFixture]
    internal class RuleBasedLog_Tests
    {
        private List<LogConfigurationRule> rules;
        private List<LogEvent> events;
        private ILog baseLog;

        [SetUp]
        public void TestSetup()
        {
            rules = new List<LogConfigurationRule>();
            events = new List<LogEvent>();
            
            baseLog = Substitute.For<ILog>();
            baseLog.IsEnabledFor(Arg.Any<LogLevel>()).Returns(true);
            baseLog.When(log => log.Log(Arg.Any<LogEvent>())).Do(info => events.Add(info.Arg<LogEvent>()));
        }

        [Test]
        public void Should_be_able_to_disable_all_logs()
        {
            rules.Add(new LogConfigurationRule { Enabled = false });

            Log.Debug("Hello!");
            Log.Info("Hello!");
            Log.Warn("Hello!");
            Log.Error("Hello!");
            Log.Fatal("Hello!");

            events.Should().BeEmpty();
        }

        [Test]
        public void Should_be_able_to_disable_logs_by_level()
        {
            rules.Add(new LogConfigurationRule { MinimumLevel = LogLevel.Warn});

            Log.Debug("Hello!");
            Log.Info("Hello!");
            Log.Warn("Hello!");
            Log.Error("Hello!");
            Log.Fatal("Hello!");

            events.Should().HaveCount(3).And.OnlyContain(e => e.Level >= LogLevel.Warn);
        }

        [Test]
        public void Should_be_able_to_disable_logs_by_source_context()
        {
            rules.Add(new LogConfigurationRule { Source = "src", Enabled = false });

            ForContext("src").Info("Hello!");

            events.Should().BeEmpty();

            ForContext("other").Info("Hello!");

            events.Should().HaveCount(1);
        }

        [Test]
        public void Should_be_able_to_disable_logs_by_operation_context()
        {
            rules.Add(new LogConfigurationRule { Operation = "op", Enabled = false });

            using (new OperationContextToken("operation"))
                Log.Info("Hello!");

            events.Should().BeEmpty();

            using (new OperationContextToken("other-op"))
                Log.Info("Hello!");

            events.Should().HaveCount(1);
        }

        [Test]
        public void Should_be_able_to_limit_level_by_source_context()
        {
            rules.Add(new LogConfigurationRule { Source = "src", MinimumLevel = LogLevel.Warn});

            ForContext("src").Info("Hello!");

            events.Should().BeEmpty();

            ForContext("src").Warn("Hello!");

            events.Should().HaveCount(1);
        }

        [Test]
        public void Should_be_able_to_limit_level_by_operation_context()
        {
            rules.Add(new LogConfigurationRule { Operation = "op", MinimumLevel = LogLevel.Warn });

            using (new OperationContextToken("operation"))
                Log.Info("Hello!");

            events.Should().BeEmpty();

            using (new OperationContextToken("operation"))
                Log.Warn("Hello!");

            events.Should().HaveCount(1);
        }

        [Test]
        public void Should_be_able_to_enrich_with_properties_from_several_rules()
        {
            rules.Add(new LogConfigurationRule { Properties = new Dictionary<string, string> {["a"] = "1"}});
            rules.Add(new LogConfigurationRule { Properties = new Dictionary<string, string> {["b"] = "2"}, Source = "src"});
            rules.Add(new LogConfigurationRule { Properties = new Dictionary<string, string> {["c"] = "3"}, Operation = "op"});

            ForContext("src").Info("Hello!");

            using (new OperationContextToken("operation"))
                Log.Info("Hello!");

            events.Should().HaveCount(2);

            events[0].Properties?["a"].Should().Be("1");
            events[0].Properties?["b"].Should().Be("2");
            events[0].Properties?.ContainsKey("c").Should().BeFalse();

            events[1].Properties?["a"].Should().Be("1");
            events[1].Properties?["c"].Should().Be("3");
            events[1].Properties?.ContainsKey("b").Should().BeFalse();
        }

        [Test]
        public void Should_be_able_to_selectively_enable_events_by_scope()
        {
            rules.Add(new LogConfigurationRule {Enabled = false});
            rules.Add(new LogConfigurationRule {Enabled = true, Source = "src"});

            ForContext("another-src").Info("Hello!");

            events.Should().BeEmpty();

            ForContext("src").Info("Hello!");

            events.Should().HaveCount(1);
        }

        [Test]
        public void IsEnabledFor_should_return_false_for_all_levels_when_there_is_a_disabling_rule_without_scopes()
        {
            rules.Add(new LogConfigurationRule { Enabled = false });

            Log.IsEnabledFor(LogLevel.Debug).Should().BeFalse();
            Log.IsEnabledFor(LogLevel.Fatal).Should().BeFalse();
        }

        [Test]
        public void IsEnabledFor_should_not_return_false_for_all_levels_when_there_is_a_disabling_rule_without_scopes_but_there_is_also_a_scoped_enabling_rule()
        {
            rules.Add(new LogConfigurationRule { Enabled = false });
            rules.Add(new LogConfigurationRule { Enabled = true, Source = "src" });

            Log.IsEnabledFor(LogLevel.Debug).Should().BeTrue();
            Log.IsEnabledFor(LogLevel.Fatal).Should().BeTrue();
        }

        [Test]
        public void IsEnabledFor_should_not_return_false_for_all_levels_when_there_is_a_disabling_rule_without_scopes_but_there_is_also_a_scoped_limiting_rule()
        {
            rules.Add(new LogConfigurationRule { Enabled = false });
            rules.Add(new LogConfigurationRule { MinimumLevel = LogLevel.Fatal, Source = "src" });

            Log.IsEnabledFor(LogLevel.Debug).Should().BeTrue();
            Log.IsEnabledFor(LogLevel.Fatal).Should().BeTrue();
        }

        [Test]
        public void IsEnabledFor_should_be_affected_by_unscoped_rules_with_minimum_level()
        {
            rules.Add(new LogConfigurationRule { MinimumLevel = LogLevel.Warn});

            Log.IsEnabledFor(LogLevel.Debug).Should().BeFalse();
            Log.IsEnabledFor(LogLevel.Info).Should().BeFalse();
            Log.IsEnabledFor(LogLevel.Warn).Should().BeTrue();
            Log.IsEnabledFor(LogLevel.Error).Should().BeTrue();
            Log.IsEnabledFor(LogLevel.Fatal).Should().BeTrue();
        }

        [Test]
        public void IsEnabledFor_should_be_affected_by_unscoped_rules_with_minimum_level_even_when_there_are_equally_restricting_rules_with_scope()
        {
            rules.Add(new LogConfigurationRule { MinimumLevel = LogLevel.Warn });
            rules.Add(new LogConfigurationRule { MinimumLevel = LogLevel.Warn, Source = "src"});

            Log.IsEnabledFor(LogLevel.Debug).Should().BeFalse();
            Log.IsEnabledFor(LogLevel.Info).Should().BeFalse();
            Log.IsEnabledFor(LogLevel.Warn).Should().BeTrue();
            Log.IsEnabledFor(LogLevel.Error).Should().BeTrue();
            Log.IsEnabledFor(LogLevel.Fatal).Should().BeTrue();
        }

        [Test]
        public void IsEnabledFor_should_be_affected_by_unscoped_rules_with_minimum_level_even_when_there_are_more_restricting_rules_with_scope()
        {
            rules.Add(new LogConfigurationRule { MinimumLevel = LogLevel.Warn });
            rules.Add(new LogConfigurationRule { MinimumLevel = LogLevel.Error, Source = "src" });

            Log.IsEnabledFor(LogLevel.Debug).Should().BeFalse();
            Log.IsEnabledFor(LogLevel.Info).Should().BeFalse();
            Log.IsEnabledFor(LogLevel.Warn).Should().BeTrue();
            Log.IsEnabledFor(LogLevel.Error).Should().BeTrue();
            Log.IsEnabledFor(LogLevel.Fatal).Should().BeTrue();
        }

        [Test]
        public void IsEnabledFor_should_not_be_affected_by_unscoped_rules_with_minimum_level_when_there_are_more_permissive_rules_with_scope()
        {
            rules.Add(new LogConfigurationRule { MinimumLevel = LogLevel.Warn });
            rules.Add(new LogConfigurationRule { MinimumLevel = LogLevel.Info, Source = "src" });

            Log.IsEnabledFor(LogLevel.Debug).Should().BeTrue();
            Log.IsEnabledFor(LogLevel.Info).Should().BeTrue();
            Log.IsEnabledFor(LogLevel.Warn).Should().BeTrue();
            Log.IsEnabledFor(LogLevel.Error).Should().BeTrue();
            Log.IsEnabledFor(LogLevel.Fatal).Should().BeTrue();
        }

        private ILog Log => new RuleBasedLog(baseLog, rules).WithOperationContext();

        private ILog ForContext(string context) => new SourceContextWrapper(Log, context);
    }
}
