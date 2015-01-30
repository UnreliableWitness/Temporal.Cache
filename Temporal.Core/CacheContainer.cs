﻿using System;
using System.Linq;
using System.Runtime.Caching;
using Temporal.Core.Events;

namespace Temporal.Core
{
    public delegate void ItemAddedEventHandler(object sender, ItemAddedEventArgs e);
    public delegate void ItemUpdatedEventHandler(object sender, ItemUpdatedEventArgs e);
    public delegate void ItemEvictedEventHandler(object sender, ItemEvictedEventArgs e);

    public class CacheContainer
    {
        private readonly ObjectCache _cache;

        public CacheKeyGenerator CacheKeyGenerator { get; private set; }

        public event ItemAddedEventHandler ItemAdded;
        public event ItemUpdatedEventHandler ItemUpdated;
        public event ItemEvictedEventHandler ItemEvicted;

        public string[] MethodsToCacheStartWith { get; set; }
        public string[] MethodsThatInvalidateStartWith { get; set; }

        public CacheContainer()
        {
            CacheKeyGenerator = new CacheKeyGenerator();
            _cache = MemoryCache.Default;
        }

        public bool TryAdd(string key, object toCache, TimeSpan expiration)
        {
            if (!AllowCache(key))
                return false;

            if (string.IsNullOrEmpty(key) || toCache == null)
                return false;
            if (_cache[key] != null)
            {
                _cache[key] = toCache;
                if (ItemUpdated != null)
                    ItemUpdated(this, new ItemUpdatedEventArgs {CacheKey = key});
                return true;
            }

            var cachePolicy = new CacheItemPolicy();
            cachePolicy.SlidingExpiration = expiration;
            cachePolicy.RemovedCallback = arguments =>
            {
                if (ItemEvicted != null)
                    ItemEvicted(this, new ItemEvictedEventArgs {CacheKey = key});
            };

            _cache.Add(key, toCache, cachePolicy);

            if (ItemAdded != null)
                ItemAdded(this, new ItemAddedEventArgs {CacheKey = key});

            return true;
        }

        private bool AllowCache(string key)
        {
            if (MethodsToCacheStartWith == null || MethodsToCacheStartWith.Length == 0)
                return false;

            foreach (var s in MethodsToCacheStartWith)
            {
                if (key.StartsWith(s))
                    return true;
            }
            return false;
        }

        public bool TryGet(string key, out object returnValue)
        {
            if (string.IsNullOrEmpty(key))
            {
                returnValue = null;
                return false;
            }

            returnValue = _cache[key];
            if (returnValue == null)
                return false;
            return true;
        }

        public void InvalidateIfNeeded(string cacheKey)
        {
            foreach (var s in MethodsThatInvalidateStartWith)
            {
                if (cacheKey.StartsWith(s))
                {
                    var allKeys = _cache.Select(o => o.Key);
                    //Parallel.ForEach(allKeys, key => _cache.Remove(key));
                    foreach (var allKey in allKeys)
                    {
                        _cache.Remove(allKey);
                    }
                    return;
                }
            }
        }
    }
}