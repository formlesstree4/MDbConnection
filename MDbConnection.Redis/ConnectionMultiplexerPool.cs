using StackExchange.Redis;
using System.Collections.Concurrent;

namespace System.Data
{

    /// <summary>
    ///     Contains pooling logic for <see cref="ConnectionMultiplexer"/>
    /// </summary>
    public static class ConnectionMultiplexerPool
    {

        /// <summary>
        ///     Contains all the pooled <see cref="ConnectionMultiplexer"/> instances.
        /// </summary>
        private static readonly ConcurrentDictionary<string, ConnectionMultiplexer> _connections =
            new ConcurrentDictionary<string, ConnectionMultiplexer>();

        /// <summary>
        ///     Returns a <see cref="ConnectionMultiplexer"/> for the given <paramref name="connectionString"/>
        /// </summary>
        /// <param name="connectionString">The connection string used to connect to a Redis server</param>
        /// <returns><see cref="ConnectionMultiplexer"/></returns>
        /// <remarks>
        ///     Don't dispose of the <see cref="ConnectionMultiplexer"/>. You will anger the Gods.
        /// </remarks>
        public static ConnectionMultiplexer GetMultiplexer(string connectionString)
        {
            return string.IsNullOrWhiteSpace(connectionString) ? null : _connections.GetOrAdd(connectionString, s => ConnectionMultiplexer.Connect(s));
        }
    }

}