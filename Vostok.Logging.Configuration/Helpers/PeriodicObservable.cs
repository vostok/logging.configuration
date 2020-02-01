using System;
using Vostok.Commons.Helpers.Observable;
using Vostok.Commons.Time;

namespace Vostok.Logging.Configuration.Helpers
{
    internal class PeriodicObservable<T> : IObservable<T>, IDisposable
    {
        private readonly CachingObservable<T> baseObservable;
        private readonly PeriodicalAction updateAction;

        public PeriodicObservable(Func<T> valueProvider, TimeSpan period)
        {
            baseObservable = new CachingObservable<T>();
            updateAction = new PeriodicalAction(() => baseObservable.Next(valueProvider()), _ => {}, () => period);
        }

        public void Dispose()
            => updateAction.Stop();

        public IDisposable Subscribe(IObserver<T> observer)
        {
            updateAction.Start();

            return baseObservable.Subscribe(observer);
        }
    }
}
