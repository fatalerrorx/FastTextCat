using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;

namespace FastTextCat.NaiveBayes
{
    public class Distribution<T> : IModifiableDistribution<T>
        where T : notnull
    {
        private readonly Dictionary<T, long> _store;
        private bool _containsUnrepresentedNoiseEvents;

        #region Implementation of IDistribution<T>

        public IEnumerable<T> DistinctRepresentedEvents => _store.Keys;

        public IEnumerable<long> RepresentedEventCounts => _store.Values;

        public long DistinctRepresentedEventsCount => _store.Count;

        public long DistinctNoiseEventsCount => DistinctRepresentedEventsCountWithNoise - _store.Count;

        public long DistinctRepresentedEventsCountWithNoise { get; private set; }

        public long TotalRepresentedEventCount { get; private set; }

        public long TotalNoiseEventsCount => TotalEventCountWithNoise - TotalRepresentedEventCount;

        public long TotalEventCountWithNoise { get; private set; }

        public long this[T obj] => GetEventCount(obj);

        #endregion

        public Distribution()
        {
            _store = new Dictionary<T, long>();
            _containsUnrepresentedNoiseEvents = false;
        }

        #region Implementation of IDistribution<T>

        public long GetEventCount(T obj)
        {
            long eventCount;
            if (_store.TryGetValue(obj, out eventCount))
            {
                return eventCount;
            }
            else
            {
                return 0;
            }
        }

        #endregion

        #region Implementation of IModifiableDistribution<T>

        public void AddEvent(T obj)
        {
            AddEvent(obj, 1);
        }

        public void AddEvent(T obj, long eventCount)
        {
            if(eventCount < 0)
            {
                throw new ArgumentOutOfRangeException("Cannot add negative number of items");
            }

            // impossible because the internal bag should contain all the events (including those that will be considered as noise after pruning)
            // otherwise we just cannot reliably keep track of DistinctEventsCountWithNoise 
            // (because we cannot distinguish between if the feature has been seen as noise or hasn't been seen at all, 
            // hence do not know if we should add +1 to DistinctEventsCountWithNoise).
            if (_containsUnrepresentedNoiseEvents)
            {
                throw new InvalidOperationException("Cannot add new items to the distribution after it has been pruned.");
            }

            long newCount = (_store.TryGetValue(obj, out var oldCount) ? oldCount : 0) + eventCount;

            _store[obj] = newCount;

            DistinctRepresentedEventsCountWithNoise = _store.Count;
            TotalRepresentedEventCount += eventCount;
            TotalEventCountWithNoise += eventCount;
        }

        public void AddNoise(long totalCount, long distinctCount)
        {
            if(totalCount < 0)
            {
                throw new ArgumentOutOfRangeException("Cannot add negative number of items");
            }

            _containsUnrepresentedNoiseEvents = true;
            DistinctRepresentedEventsCountWithNoise += distinctCount;
            TotalEventCountWithNoise += totalCount;
        }

        public void AddEventRange(IEnumerable<T> collection)
        {
            foreach(T item in collection)
            {
                AddEvent(item);
            }
        }

        public void PruneByRank(long maxRankAllowed)
        {
            IEnumerable<T> eventsToPrune = 
                _store
                .OrderBy(kvp => kvp.Value)
                .Select(kvp => kvp.Key)
                .Take((int)Math.Max(0, DistinctRepresentedEvents.LongCount() - maxRankAllowed));

            foreach(T eventToPrune in eventsToPrune)
            {
                removeAllCopies(eventToPrune);
            }

            _containsUnrepresentedNoiseEvents = true;
        }

        public void PruneByCount(long minCountAllowed)
        {
            if(minCountAllowed < 0)
            {
                throw new ArgumentOutOfRangeException(nameof(minCountAllowed), "Only non-negative values allowed");
            }

            IEnumerable<T> eventsToPrune =
                _store
                .Where(kvp => kvp.Value <= minCountAllowed)
                .Select(kvp => kvp.Key)
                .ToList();

            foreach(T eventToPrune in eventsToPrune)
            {
                removeAllCopies(eventToPrune);
            }

            _containsUnrepresentedNoiseEvents = true;
        }

        private void removeAllCopies(T item)
        {
            if (_store.TryGetValue(item, out var oldCount))
            {
                TotalRepresentedEventCount -= oldCount;
                _store.Remove(item);
            }
        }

        #endregion

        #region Implementation of IEnumerable

        IEnumerator IEnumerable.GetEnumerator()
        {
            return GetEnumerator();
        }

        #endregion

        #region Implementation of IEnumerable<out KeyValuePair<T,long>>

        public IEnumerator<KeyValuePair<T, long>> GetEnumerator()
        {
            return _store.GetEnumerator();
        }

        #endregion
    }
}
