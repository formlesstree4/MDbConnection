using System.Collections.Generic;

namespace System.Data.Caching
{

    /// <summary>
    ///     Defines the contract that must be implemented for cache support.
    /// </summary>
    public interface IDbCacheLayer
    {

        /// <summary>
        ///     Returns a <see cref="IReadOnlyCollection{T}"/> of elements from this caching layer.
        /// </summary>
        /// <typeparam name="T">The type of <see cref="IReadOnlyCollection{T}"/> to return</typeparam>
        /// <param name="key">The unique key of the collection</param>
        /// <returns><see cref="Option"/> of <see cref="IReadOnlyCollection{T}"/></returns>
        Option<IReadOnlyCollection<T>> GetCollection<T>(string key);

        /// <summary>
        ///     Stores an <see cref="IEnumerable{T}"/> in this caching layer.
        /// </summary>
        /// <typeparam name="T">The type of the <see cref="IEnumerable{T}"/></typeparam>
        /// <param name="key">The unique key of the collection</param>
        /// <param name="value">The value to store</param>
        /// <param name="expiration">The <see cref="TimeSpan"/> that describes how long the value may live in cache</param>
        void SetCollection<T>(string key, IEnumerable<T> value, TimeSpan expiration);

        /// <summary>
        ///     Returns a scalar value of <typeparamref name="T"/> from this caching layer.
        /// </summary>
        /// <typeparam name="T">The type to return</typeparam>
        /// <param name="key">The unique key of the value</param>
        /// <returns><see cref="Option"/> of <typeparamref name="T"/></returns>
        Option<T> GetScalar<T>(string key);

        /// <summary>
        ///     Stores a scalar value of <typeparamref name="T"/> in this caching layer.
        /// </summary>
        /// <typeparam name="T">The type to store</typeparam>
        /// <param name="key">The unique key of the value</param>
        /// <param name="value">The value to store</param>
        /// <param name="expiration">The <see cref="TimeSpan"/> that describes how long the value may live in cache</param>
        void SetScalar<T>(string key, T value, TimeSpan expiration);

        /// <summary>
        ///     Clears out the <see cref="IDbCacheLayerLayer"/>, removing all current items from the layer.
        /// </summary>
        void Clear();

    }
}