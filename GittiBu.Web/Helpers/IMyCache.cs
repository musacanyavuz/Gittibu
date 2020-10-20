using Microsoft.Extensions.Caching.Memory;
using System;
using System.Collections;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace GittiBu.Web.Helpers
{
    public interface IMyCache : IEnumerable<KeyValuePair<object, object>>, IMemoryCache
    {
        /// <summary>
        /// Clears all cache entries.
        /// </summary>
        void Clear();
    }

    public class MyMemoryCache : IMyCache
    {
        private readonly IMemoryCache _memoryCache;
        private readonly ConcurrentDictionary<object, ICacheEntry> _cacheEntries = new ConcurrentDictionary<object, ICacheEntry>();

        public MyMemoryCache(IMemoryCache memoryCache)
        {
            this._memoryCache = memoryCache;
        }

        public MyMemoryCache()
        {
        }

        public void Dispose()
        {
            this._memoryCache.Dispose();
        }

        private void PostEvictionCallback(object key, object value, EvictionReason reason, object state)
        {
            if (reason != EvictionReason.Replaced)
                this._cacheEntries.TryRemove(key, out var _);
        }

        /// <inheritdoc cref="IMemoryCache.TryGetValue"/>
        public bool TryGetValue(object key, out object value)
        {
            return this._memoryCache.TryGetValue(key, out value);
        }

        /// <summary>
        /// Create or overwrite an entry in the cache and add key to Dictionary.
        /// </summary>
        /// <param name="key">An object identifying the entry.</param>
        /// <returns>The newly created <see cref="T:Microsoft.Extensions.Caching.Memory.ICacheEntry" /> instance.</returns>
        public ICacheEntry CreateEntry(object key)
        {
            var entry = this._memoryCache.CreateEntry(key);
            entry.RegisterPostEvictionCallback(this.PostEvictionCallback);
            this._cacheEntries.AddOrUpdate(key, entry, (o, cacheEntry) =>
            {
                cacheEntry.Value = entry;
                return cacheEntry;
            });
            return entry;
        }

        /// <inheritdoc cref="IMemoryCache.Remove"/>
        public void Remove(object key)
        {
            this._memoryCache.Remove(key);
        }

        /// <inheritdoc cref="IMyCache.Clear"/>
        public void Clear()
        {
            foreach (var cacheEntry in this._cacheEntries.Keys.ToList())
                this._memoryCache.Remove(cacheEntry);
        }

        public IEnumerator<KeyValuePair<object, object>> GetEnumerator()
        {
            return this._cacheEntries.Select(pair => new KeyValuePair<object, object>(pair.Key, pair.Value.Value)).GetEnumerator();
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return this.GetEnumerator();
        }

        /// <summary>
        /// Gets keys of all items in MemoryCache.
        /// </summary>
        public IEnumerator<object> Keys => this._cacheEntries.Keys.GetEnumerator();
    }

    public static class MyMemoryCacheExtensions
    {
        public static T Set<T>(this IMyCache cache, object key, T value)
        {
            var entry = cache.CreateEntry(key);
            entry.Value = value;
            entry.Dispose();

            return value;
        }

        public static T Set<T>(this IMyCache cache, object key, T value, CacheItemPriority priority)
        {
            var entry = cache.CreateEntry(key);
            entry.Priority = priority;
            entry.Value = value;
            entry.Dispose();

            return value;
        }

        public static T Set<T>(this IMyCache cache, object key, T value, DateTimeOffset absoluteExpiration)
        {
            var entry = cache.CreateEntry(key);
            entry.AbsoluteExpiration = absoluteExpiration;
            entry.Value = value;
            entry.Dispose();

            return value;
        }

        public static T Set<T>(this IMyCache cache, object key, T value, TimeSpan absoluteExpirationRelativeToNow)
        {
            var entry = cache.CreateEntry(key);
            entry.AbsoluteExpirationRelativeToNow = absoluteExpirationRelativeToNow;
            entry.Value = value;
            entry.Dispose();

            return value;
        }

        public static T Set<T>(this IMyCache cache, object key, T value, MemoryCacheEntryOptions options)
        {
            using (var entry = cache.CreateEntry(key))
            {
                if (options != null)
                    entry.SetOptions(options);

                entry.Value = value;
            }

            return value;
        }

        public static TItem GetOrCreate<TItem>(this IMyCache cache, object key, Func<ICacheEntry, TItem> factory)
        {
            if (!cache.TryGetValue(key, out var result))
            {
                var entry = cache.CreateEntry(key);
                result = factory(entry);
                entry.SetValue(result);
                entry.Dispose();
            }

            return (TItem)result;
        }

        public static async Task<TItem> GetOrCreateAsync<TItem>(this IMyCache cache, object key, Func<ICacheEntry, Task<TItem>> factory)
        {
            if (!cache.TryGetValue(key, out object result))
            {
                var entry = cache.CreateEntry(key);
                result = await factory(entry);
                entry.SetValue(result);
                entry.Dispose();
            }

            return (TItem)result;
        }
    }
}
