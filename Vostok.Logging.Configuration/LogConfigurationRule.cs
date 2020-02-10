using System.Collections.Generic;
using JetBrains.Annotations;
using Vostok.Logging.Abstractions;

namespace Vostok.Logging.Configuration
{
    /// <summary>
    /// Represents a single rule governing the behaviour of <see cref="ConfigurableLog"/>.
    /// </summary>
    [PublicAPI]
    public class LogConfigurationRule
    {
        /// <summary>
        /// If set to <c>false</c>, disables logging in scope of this rule. <c>True</c> by default.
        /// </summary>
        public bool Enabled { get; set; } = true;

        /// <summary>
        /// If set to some value, limits the scope of this rule to the log with such name.
        /// </summary>
        [CanBeNull]
        public string Log { get; set; }

        /// <summary>
        /// If set to some value, limits the scope of this rule to the source context with such name prefix.
        /// </summary>
        [CanBeNull]
        public string Source { get; set; }

        /// <summary>
        /// If set to some value, limits the scope of this rule to the operation context with such value prefix.
        /// </summary>
        [CanBeNull]
        public string Operation { get; set; }

        /// <summary>
        /// If set to some value, limits the minimum level of log entries in the scope of this rule.
        /// </summary>
        [CanBeNull]
        public LogLevel? MinimumLevel { get; set; }

        /// <summary>
        /// If initialized with a non-null value, enriches log events with given properties in the scope of this rule.
        /// </summary>
        [CanBeNull]
        public IReadOnlyDictionary<string, string> Properties { get; set; }
    }
}
