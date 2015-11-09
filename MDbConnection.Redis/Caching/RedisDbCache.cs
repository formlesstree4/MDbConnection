using Newtonsoft.Json;
using StackExchange.Redis;
using System.Collections.Generic;
using System.Data.Extensions;

namespace System.Data.Caching
{

    /// <summary>
    ///     A <see cref="IDbCacheLayer"/> implementation specifically for using Redis as a caching layer.
    /// </summary>
    public sealed class RedisDbCache : IDbCacheLayer
    {

        private readonly IConnectionMultiplexer _connection;



        /// <summary>
        ///     Creates a new <see cref="RedisDbCache"/> with the given <see cref="IConnectionMultiplexer"/>.
        /// </summary>
        /// <param name="connection"><see cref="IConnectionMultiplexer"/></param>
        public RedisDbCache(IConnectionMultiplexer connection)
        {
            _connection = connection;
        }



        /// <summary>
        ///     Clears out the <see cref="IDbCacheLayer"/>, removing all current items from the layer.
        /// </summary>
        public void Clear()
        {
            ;
        }

        /// <summary>
        ///     Returns a <see cref="IReadOnlyCollection{T}"/> of elements from this caching layer.
        /// </summary>
        /// <typeparam name="T">The type of <see cref="IReadOnlyCollection{T}"/> to return</typeparam>
        /// <param name="key">The unique key of the collection</param>
        /// <returns><see cref="Option"/> of <see cref="IReadOnlyCollection{T}"/></returns>
        public Option<IReadOnlyCollection<T>> GetCollection<T>(string key)
        {
            var db = _connection.GetDatabase();
            RedisValue result;
            try
            {
                result = db.StringGet(key);
            }
            catch (TimeoutException)
            {
                return Option.Empty;
            }
            if (!result.HasValue) return Option.Empty;
            return new Option<IReadOnlyCollection<T>>(JsonConvert.DeserializeObject<List<T>>(result.ToString()).AsReadOnly());
        }

        /// <summary>
        ///     Returns a scalar value of <typeparamref name="T"/> from this caching layer.
        /// </summary>
        /// <typeparam name="T">The type to return</typeparam>
        /// <param name="key">The unique key of the value</param>
        /// <returns><see cref="Option"/> of <typeparamref name="T"/></returns>
        public Option<T> GetScalar<T>(string key)
        {
            var db = _connection.GetDatabase();
            RedisValue result;
            try
            {
                result = db.StringGet(key);
            }
            catch (TimeoutException)
            {
                return Option.Empty;
            }
            if (!result.HasValue) return Option.Empty;
            return new Option<T>(JsonConvert.DeserializeObject<T>(result));
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
            var db = _connection.GetDatabase();
            try
            {
                db.StringSet(key, JsonConvert.SerializeObject(value.CastOrToList()), expiration);
            }
            catch (TimeoutException)
            {
                // Do nothing
            }
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
            var db = _connection.GetDatabase();
            try
            {
                db.StringSet(key, JsonConvert.SerializeObject(value), expiration);
            }
            catch (TimeoutException)
            {
                // Do nothing
            }
        }

    }
}
