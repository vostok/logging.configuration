using System;
using FluentAssertions;
using NUnit.Framework;
using Vostok.Logging.Abstractions;
using Vostok.Logging.Abstractions.Values;
using Vostok.Logging.Configuration.Helpers;

namespace Vostok.Logging.Configuration.Tests
{
    [TestFixture]
    internal class RuleExtensions_Tests
    {
        [Test]
        public void Allows_should_return_false_when_rule_disables_logging()
        {
            var rule = new LogConfigurationRule { Enabled = false };

            rule.Allows(Event(LogLevel.Info)).Should().BeFalse();
            rule.Allows(Event(LogLevel.Fatal)).Should().BeFalse();
        }

        [Test]
        public void Allows_should_return_false_when_rule_prohibits_event_level()
        {
            var rule = new LogConfigurationRule { MinimumLevel = LogLevel.Warn};

            rule.Allows(Event(LogLevel.Debug)).Should().BeFalse();
            rule.Allows(Event(LogLevel.Info)).Should().BeFalse();
        }

        [Test]
        public void Allows_should_return_false_when_rule_does_not_restrict_logging()
        {
            var rule = new LogConfigurationRule();

            rule.Allows(Event(LogLevel.Debug)).Should().BeTrue();
            rule.Allows(Event(LogLevel.Fatal)).Should().BeTrue();
        }

        [Test]
        public void Allows_should_return_false_when_rule_permits_event_level()
        {
            var rule = new LogConfigurationRule { MinimumLevel = LogLevel.Warn };

            rule.Allows(Event(LogLevel.Warn)).Should().BeTrue();
            rule.Allows(Event(LogLevel.Error)).Should().BeTrue();
            rule.Allows(Event(LogLevel.Fatal)).Should().BeTrue();
        }

        [Test]
        public void Matches_should_return_true_when_rule_has_no_source_or_operation_scope()
        {
            var rule = new LogConfigurationRule();

            rule.Matches(Event()).Should().BeTrue();
            rule.Matches(Event("src", "op")).Should().BeTrue();
        }

        [Test]
        public void Matches_should_handle_source_context_requirement()
        {
            var rule = new LogConfigurationRule { Source = "src" };

            rule.Matches(Event()).Should().BeFalse();
            rule.Matches(Event("some-other-source")).Should().BeFalse();

            rule.Matches(Event("src")).Should().BeTrue();
            rule.Matches(Event("SRC")).Should().BeTrue();
            rule.Matches(Event("Src-with-suffix", "op")).Should().BeTrue();
        }

        [Test]
        public void Matches_should_handle_operation_context_requirement()
        {
            var rule = new LogConfigurationRule { Operation = "op" };

            rule.Matches(Event()).Should().BeFalse();
            rule.Matches(Event(operationContext: "some-other-op")).Should().BeFalse();

            rule.Matches(Event(operationContext: "op")).Should().BeTrue();
            rule.Matches(Event(operationContext: "OP")).Should().BeTrue();
            rule.Matches(Event("src", "Op-with-suffix")).Should().BeTrue();
        }

        [Test]
        public void Matches_should_handle_source_and_operation_context_requirements()
        {
            var rule = new LogConfigurationRule { Source = "src", Operation = "op" };

            rule.Matches(Event()).Should().BeFalse();
            rule.Matches(Event("src")).Should().BeFalse();
            rule.Matches(Event(operationContext: "op")).Should().BeFalse();

            rule.Matches(Event("src", "op")).Should().BeTrue();
            rule.Matches(Event("SRC", "OP")).Should().BeTrue();
            rule.Matches(Event("Src-with-suffix", "Op-with-suffix")).Should().BeTrue();
        }

        private LogEvent Event(LogLevel level)
            => new LogEvent(level, DateTimeOffset.Now, null);

        private LogEvent Event(string sourceContext = null, string operationContext = null)
        {
            var result = Event(LogLevel.Info);

            if (sourceContext != null)
                result = result.WithProperty(WellKnownProperties.SourceContext, new SourceContextValue(sourceContext));

            if (operationContext != null)
                result = result.WithProperty(WellKnownProperties.OperationContext, new OperationContextValue(operationContext));

            return result;
        }
    }
}
