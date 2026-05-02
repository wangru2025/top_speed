using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;

namespace TS.Audio
{
    public sealed partial class AudioEngine
    {
        private void TrackTransientSource(Source source)
        {
            lock (_transientLock)
                _transientSources.Add(source);

            source.AddEndObserver(() => _transientRetireHandler(source));
        }

        private void QueueTransientRetire(Source source)
        {
            lock (_transientLock)
            {
                if (_transientSources.Contains(source))
                    _retiredSources.Enqueue(source);
            }
        }

        private void DrainRetiredVoices()
        {
            while (true)
            {
                Source? source;
                lock (_transientLock)
                {
                    if (_retiredSources.Count == 0)
                        break;

                    source = _retiredSources.Dequeue();
                    _transientSources.Remove(source);
                }

                try
                {
                    source.Stop();
                }
                catch
                {
                }

                try
                {
                    source.Dispose();
                }
                catch
                {
                }
            }
        }

        private void ClearTransientVoices()
        {
            lock (_transientLock)
            {
                while (_retiredSources.Count > 0)
                    _transientSources.Remove(_retiredSources.Dequeue());

                foreach (var source in _transientSources)
                {
                    try
                    {
                        source.Stop();
                    }
                    catch
                    {
                    }

                    try
                    {
                        source.Dispose();
                    }
                    catch
                    {
                    }
                }

                _transientSources.Clear();
            }
        }

        private Source BorrowPooledOneShot(SoundAsset asset, string? busName, bool? useHrtf)
        {
            var key = new OneShotPoolKey(asset, ResolveBusName(busName), useHrtf ?? false);
            lock (_oneShotPoolLock)
            {
                if (_pooledOneShots.TryGetValue(key, out var pool) && pool.Count > 0)
                {
                    while (pool.Count > 0)
                    {
                        var pooled = pool.Pop();
                        if (pooled.IsDisposed)
                            continue;

                        _activePooledOneShots[pooled] = key;
                        return pooled;
                    }
                }
            }

            var created = CreateSource(asset, key.BusName, spatialize: false, useHrtf: key.UseHrtf);
            created.AddEndObserver(() => _pooledOneShotReturnHandler(created));
            lock (_oneShotPoolLock)
                _activePooledOneShots[created] = key;
            return created;
        }

        private void QueuePooledOneShotReturn(Source source)
        {
            lock (_oneShotPoolLock)
            {
                if (_activePooledOneShots.Remove(source, out var key))
                    _pendingPooledOneShotReturns.Enqueue(new PooledOneShotReturn(key, source));
            }
        }

        private void DrainReturnedPooledOneShots()
        {
            while (true)
            {
                PooledOneShotReturn entry;
                lock (_oneShotPoolLock)
                {
                    if (_pendingPooledOneShotReturns.Count == 0)
                        break;

                    entry = _pendingPooledOneShotReturns.Dequeue();
                    _activePooledOneShots.Remove(entry.Source);
                }

                ResetReturnedPooledOneShot(entry);
            }
        }

        private void ResetReturnedPooledOneShot(PooledOneShotReturn entry)
        {
            try
            {
                if (entry.Source.IsDisposed)
                    return;

                ResetOneShotSource(entry.Source);
            }
            catch
            {
                try
                {
                    entry.Source.Dispose();
                }
                catch
                {
                }
                return;
            }

            lock (_oneShotPoolLock)
            {
                if (_disposed)
                {
                    try
                    {
                        entry.Source.Dispose();
                    }
                    catch
                    {
                    }
                    return;
                }

                if (!_pooledOneShots.TryGetValue(entry.Key, out var pool))
                {
                    pool = new Stack<Source>();
                    _pooledOneShots[entry.Key] = pool;
                }

                if (pool.Count >= OneShotPoolLimitPerKey)
                {
                    try
                    {
                        entry.Source.Dispose();
                    }
                    catch
                    {
                    }
                    return;
                }

                pool.Push(entry.Source);
            }
        }

        private void ClearPooledOneShots()
        {
            List<Source>? pooled = null;
            lock (_oneShotPoolLock)
            {
                while (_pendingPooledOneShotReturns.Count > 0)
                {
                    var pending = _pendingPooledOneShotReturns.Dequeue();
                    _activePooledOneShots.Remove(pending.Source);
                    pooled ??= new List<Source>();
                    pooled.Add(pending.Source);
                }
                foreach (var pair in _pooledOneShots)
                {
                    pooled ??= new List<Source>();
                    pooled.AddRange(pair.Value);
                }

                pooled ??= new List<Source>(_activePooledOneShots.Count);
                foreach (var pair in _activePooledOneShots)
                    pooled.Add(pair.Key);

                _pooledOneShots.Clear();
                _activePooledOneShots.Clear();
            }

            for (var i = 0; i < pooled.Count; i++)
            {
                try
                {
                    pooled[i].Stop();
                }
                catch
                {
                }

                try
                {
                    pooled[i].Dispose();
                }
                catch
                {
                }
            }
        }

        private void DisposeFailedPooledOneShot(Source source)
        {
            lock (_oneShotPoolLock)
                _activePooledOneShots.Remove(source);

            try
            {
                source.Stop();
            }
            catch
            {
            }

            try
            {
                source.Dispose();
            }
            catch
            {
            }
        }

        private readonly struct OneShotPoolKey : IEquatable<OneShotPoolKey>
        {
            public OneShotPoolKey(SoundAsset asset, string busName, bool useHrtf)
            {
                Asset = asset;
                BusName = busName;
                UseHrtf = useHrtf;
            }

            public SoundAsset Asset { get; }
            public string BusName { get; }
            public bool UseHrtf { get; }

            public bool Equals(OneShotPoolKey other)
            {
                return ReferenceEquals(Asset, other.Asset)
                    && StringComparer.OrdinalIgnoreCase.Equals(BusName, other.BusName)
                    && UseHrtf == other.UseHrtf;
            }

            public override bool Equals(object? obj)
            {
                return obj is OneShotPoolKey other && Equals(other);
            }

            public override int GetHashCode()
            {
                unchecked
                {
                    var hash = 17;
                    hash = (hash * 31) + RuntimeHelpers.GetHashCode(Asset);
                    hash = (hash * 31) + StringComparer.OrdinalIgnoreCase.GetHashCode(BusName);
                    hash = (hash * 31) + (UseHrtf ? 1 : 0);
                    return hash;
                }
            }
        }

        private readonly struct PooledOneShotReturn
        {
            public PooledOneShotReturn(OneShotPoolKey key, Source source)
            {
                Key = key;
                Source = source;
            }

            public OneShotPoolKey Key { get; }
            public Source Source { get; }
        }
    }
}
