using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using JetBrains.Annotations;
using Vostok.Logging.Abstractions;
using Vostok.Logging.Abstractions.Wrappers;
using Vostok.Logging.Configuration.Helpers;

// ReSharper disable AssignNullToNotNullAttribute

namespace Vostok.Logging.Configuration
{
    /// <summary>
    /// <para><see cref="ConfigurableLog"/> is essentially a dynamic <see cref="CompositeLog"/> with named components and external configuration.</para>
    /// <para>It allows to supply a set of <see cref="LogConfigurationRule"/>s from application configuration to adjust filtering/enrichment without application restart and code changes.</para>
    /// <para>Use <see cref="ConfigurableLogBuilder"/> to create an instance of <see cref="ConfigurableLog"/>.</para>
    /// </summary>
    [PublicAPI]
    public class ConfigurableLog : ILog, IDisposable, IObserver<LogConfigurationRule[]>
    {
        private readonly IObservable<LogConfigurationRule[]> rulesSource;
        private readonly IDisposable rulesSubscription;
        private readonly TaskCompletionSource<bool> initSignal;
        private volatile ILog currentLog;

        internal ConfigurableLog(IReadOnlyDictionary<string, ILog> baseLogs, IObservable<LogConfigurationRule[]> rulesSource)
        {
            this.rulesSource = rulesSource;
            
            initSignal = new TaskCompletionSource<bool>(TaskCreationOptions.RunContinuationsAsynchronously);
            BaseLogs = baseLogs;
            currentLog = BuildInternalLog(null);
            rulesSubscription = rulesSource.Subscribe(this);
        }

        public IReadOnlyDictionary<string, ILog> BaseLogs { get; }

        public void Dispose()
        {
            rulesSubscription.Dispose();
            (rulesSource as IDisposable)?.Dispose();
        }

        public void Log(LogEvent @event)
            => currentLog.Log(@event);

        public bool IsEnabledFor(LogLevel level)
            => currentLog.IsEnabledFor(level);

        public ILog ForContext(string context)
            => new SourceContextWrapper(this, context ?? throw new ArgumentNullException(nameof(context)));

        public void OnNext(LogConfigurationRule[] rules)
        {
            currentLog = BuildInternalLog(rules);
            initSignal.TrySetResult(true);
        }

        public bool WaitForRulesInitialization(TimeSpan timeout)
            => initSignal.Task.Wait(timeout);

        public void OnCompleted()
        {
        }

        public void OnError(Exception error)
        {
        }

        private ILog BuildInternalLog([CanBeNull] LogConfigurationRule[] rules)
        {
            var components = BaseLogs.ToDictionary(pair => pair.Key, pair => pair.Value, StringComparer.OrdinalIgnoreCase);

            foreach (var rule in SelectRulesWithLogScope(rules))
            {
                if (components.TryGetValue(rule.Log, out var log))
                    components[rule.Log] = ConfigureLog(log, rule);
            }

            var compositeLog = new CompositeLog(components.Values.ToArray()) as ILog;

            foreach (var rule in SelectRulesWithoutLogScope(rules))
                compositeLog = ConfigureLog(compositeLog, rule);

            return compositeLog;
        }

        [NotNull]
        private static IEnumerable<LogConfigurationRule> SelectRulesWithLogScope([CanBeNull] LogConfigurationRule[] rules)
            => (rules ?? Enumerable.Empty<LogConfigurationRule>()).Where(rule => !string.IsNullOrEmpty(rule?.Log));

        [NotNull]
        private static IEnumerable<LogConfigurationRule> SelectRulesWithoutLogScope([CanBeNull] LogConfigurationRule[] rules)
            => (rules ?? Enumerable.Empty<LogConfigurationRule>()).Where(rule => rule != null && string.IsNullOrEmpty(rule.Log));

        [NotNull]
        private static ILog ConfigureLog([NotNull] ILog log, [NotNull] LogConfigurationRule rule)
        {
            if (string.IsNullOrEmpty(rule.Source))
            {
                if (!rule.Enabled)
                    return new SilentLog();

                if (rule.Properties?.Count > 0)
                    log = EnrichWithProperties(log, rule.Properties);

                if (rule.MinimumLevel.HasValue)
                    log = log.WithMinimumLevel(rule.MinimumLevel.Value);
            }
            else log = new SourceContextFilteringLog(log, rule);

            return log;
        }

        [NotNull]
        private static ILog EnrichWithProperties([NotNull] ILog log, [NotNull] IReadOnlyDictionary<string, string> properties)
            => log.WithProperties(properties.ToDictionary(pair => pair.Key, pair => pair.Value as object));
    }
}
