using System;
using System.Collections.Generic;
using System.Linq;
using Vostok.Logging.Abstractions;

namespace Vostok.Logging.Configuration.Helpers
{
    internal static class RuleOrderingHelper
    {
        private static readonly int MaxPermissiveness = Enum.GetValues(typeof(LogLevel)).Length;

        public static IEnumerable<LogConfigurationRule> Order(IEnumerable<LogConfigurationRule> rules)
            => rules
                .OrderByDescending(GetSpecificity)
                .ThenByDescending(GetPermissiveness);

        private static int GetSpecificity(LogConfigurationRule rule)
        {
            var specificity = 0;

            if (rule.HasSourceScope)
                specificity += 1;

            if (rule.HasOperationScope)
                specificity += 2;

            return specificity;
        }

        private static int GetPermissiveness(LogConfigurationRule rule)
        {
            if (!rule.Enabled)
                return 0;

            if (rule.MinimumLevel == null)
                return MaxPermissiveness;

            return MaxPermissiveness - (int) rule.MinimumLevel.Value;
        }
    }
}
