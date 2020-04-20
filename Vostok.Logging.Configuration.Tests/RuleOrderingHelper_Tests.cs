using FluentAssertions;
using NUnit.Framework;
using Vostok.Logging.Abstractions;
using Vostok.Logging.Configuration.Helpers;

namespace Vostok.Logging.Configuration.Tests
{
    [TestFixture]
    internal class RuleOrderingHelper_Tests
    {
        [Test]
        public void Should_order_by_decreasing_rule_specificity()
        {
            var rule1 = new LogConfigurationRule { Log = "log", MinimumLevel = LogLevel.Info };
            var rule2 = new LogConfigurationRule { Log = "log", Source = "src", MinimumLevel = LogLevel.Warn };
            var rule3 = new LogConfigurationRule { Log = "log", Operation = "op", MinimumLevel = LogLevel.Error };
            var rule4 = new LogConfigurationRule { Log = "log", Source = "src", Operation = "op", MinimumLevel = LogLevel.Fatal };

            RuleOrderingHelper
                .Order(new[] {rule1, rule2, rule3, rule4})
                .Should().Equal(rule4, rule3, rule2, rule1);
        }

        [Test]
        public void Should_order_by_decreasing_scope_length_within_same_specificity()
        {
            var rule1 = new LogConfigurationRule { Source = "A.B", MinimumLevel = LogLevel.Debug };
            var rule2 = new LogConfigurationRule { Source = "A", MinimumLevel = LogLevel.Info };
            var rule3 = new LogConfigurationRule { Source = "A.B.C", MinimumLevel = LogLevel.Warn};

            RuleOrderingHelper
                .Order(new[] { rule1, rule2, rule3 })
                .Should().Equal(rule3, rule1, rule2);
        }

        [Test]
        public void Should_order_by_decreasing_rule_permissiveness_within_same_specificity()
        {
            var rule1 = new LogConfigurationRule { Enabled = false };
            var rule2 = new LogConfigurationRule { Enabled = true };
            var rule3 = new LogConfigurationRule { Enabled = true, MinimumLevel = LogLevel.Fatal };
            var rule4 = new LogConfigurationRule { Enabled = true, MinimumLevel = LogLevel.Error };
            var rule5 = new LogConfigurationRule { Enabled = true, MinimumLevel = LogLevel.Warn };
            var rule6 = new LogConfigurationRule { Enabled = true, MinimumLevel = LogLevel.Info };

            RuleOrderingHelper
                .Order(new[] { rule1, rule2, rule3, rule4, rule5, rule6 })
                .Should().Equal(rule2, rule6, rule5, rule4, rule3, rule1);
        }
    }
}
