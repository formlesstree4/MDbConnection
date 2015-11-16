using System.Collections.Generic;
using System.Data.Extensions;
using System.Runtime.Caching;

namespace System.Data.Caching
{

    /// <summary>
    ///     An <see cref="IDbCacheLayer"/> implementation using <see cref="MemoryCache"/>.
    /// </summary>
    public sealed class MemoryDbCache : IDbCacheLayer
    {

        /// <summary>
        ///     Clears out the <see cref="IDbCacheLayer"/>, removing all current items from the layer.
        /// </summary>
        public void Clear()
        {
            //
        }

        /// <summary>
        ///     Returns a <see cref="IReadOnlyCollection{T}"/> of elements from this caching layer.
        /// </summary>
        /// <typeparam name="T">The type of <see cref="IReadOnlyCollection{T}"/> to return</typeparam>
        /// <param name="key">The unique key of the collection</param>
        /// <returns><see cref="Option"/> of <see cref="IReadOnlyCollection{T}"/></returns>
        public Option<IReadOnlyCollection<T>> GetCollection<T>(string key)
        {
            List<T> c;
            if (MemoryCache.Default.Contains(key) && ((c = MemoryCache.Default.Get(key) as List<T>) != null))
                return new Option<IReadOnlyCollection<T>>(c.AsReadOnly());
            return Option.Empty;
        }

        /// <summary>
        ///     Returns a scalar value of <typeparamref name="T"/> from this caching layer.
        /// </summary>
        /// <typeparam name="T">The type to return</typeparam>
        /// <param name="key">The unique key of the value</param>
        /// <returns><see cref="Option"/> of <typeparamref name="T"/></returns>
        public Option<T> GetScalar<T>(string key)
        {
            if (MemoryCache.Default.Contains(key))
            {
                var c = (T)MemoryCache.Default.Get(key);
                return new Option<T>(c);
            }
            return Option.Empty;
        }

        /// <summary>
        ///     Stores an <see cref="IEnumerable{T}"/> in this caching layer.
        /// </summary>
        /// <typeparam name="T">The type of the <see cref="IEnumerable{T}"/></typeparam>
        /// <param name="key">The unique key of the collection</param>
        /// <param name="value">The value to store</param>
        /// <param name="expiration">The <see cref="TimeSpan"/> that describes how long the value may live in cache</param>
        public void SetCollection<T>(string key, IEnumerable<T> value, TimeSpan expiration)
        {
            MemoryCache.Default.Set(key, value.CastOrToList(), DateTime.UtcNow.Add(expiration));
        }

        /// <summary>
        ///     Stores a scalar value of <typeparamref name="T"/> in this caching layer.
        /// </summary>
        /// <typeparam name="T">The type to store</typeparam>
        /// <param name="key">The unique key of the value</param>
        /// <param name="value">The value to store</param>
        /// <param name="expiration">The <see cref="TimeSpan"/> that describes how long the value may live in cache</param>
        public void SetScalar<T>(string key, T value, TimeSpan expiration)
        {
            MemoryCache.Default.Set(key, value, DateTime.UtcNow.Add(expiration));
        }

    }
}