using Newtonsoft.Json;
using StackExchange.Redis;
using System.Collections.Generic;
using System.Data.Extensions;
using System.Data.Common;
using System.Threading.Tasks;
using System.Timers;
using System.Linq;

#pragma warning disable CS1998 // Async method lacks 'await' operators and will run synchronously

namespace System.Data.Tracking
{

    /// <summary>
    ///     A thread-safe implementation of <see cref="IDbObserver"/> that flushes its results to a REDIS server.
    /// </summary>
    public sealed class RedisTracker : IDbObserver
    {

        private const string DefaultCollectionName = "QUERIES";
        private static readonly TimeSpan DefaultFlushTimer =
#if DEBUG
            TimeSpan.FromMinutes(1);
#else
            TimeSpan.FromMinutes(15);
#endif

        private readonly IConnectionMultiplexer _connection;
        private readonly List<Trail> _items = new List<Trail>();
        private readonly Timer _backgroundFlushTimer;
        private readonly string _collectionName;


        /// <summary>
        ///     Gets the callback to invoke for tracking.
        /// </summary>
        public Func<Trail, Task> Callback { get; private set; }



        /// <summary>
        ///     Creates a new instance of the <see cref="RedisTracker"/>.
        /// </summary>
        /// <param name="connectionString">The connection string to the Redis server.</param>
        /// <param name="flushInterval">An optional <see cref="TimeSpan"/> that indicates how frequently <see cref="RedisTracker"/> will aggregate and flush results to the Redis server.</param>
        /// <param name="collectionName">The name of the collection that will store the aggregate results in Redis.</param>
        public RedisTracker(string connectionString, Option<TimeSpan> flushInterval, string collectionName = DefaultCollectionName) :
            this(ConnectionMultiplexerPool.GetMultiplexer(connectionString), flushInterval, collectionName) { }

        /// <summary>
        ///     Creates a new instance of the <see cref="RedisTracker"/>.
        /// </summary>
        /// <param name="connection">The <see cref="IConnectionMultiplexer"/> that has already established a connection to the Redis server.</param>
        /// <param name="flushInterval">An optional <see cref="TimeSpan"/> that indicates how frequently <see cref="RedisTracker"/> will aggregate and flush results to the Redis server.</param>
        /// <param name="collectionName">The name of the collection that will store the aggregate results in Redis.</param>
        public RedisTracker(IConnectionMultiplexer connection, Option<TimeSpan> flushInterval, string collectionName = DefaultCollectionName)
        {
            _collectionName = collectionName;
            _connection = connection;
            _backgroundFlushTimer = new Timer { AutoReset = false };
            _backgroundFlushTimer.Interval = flushInterval.HasValue ?
                flushInterval.Value.TotalMilliseconds :
                DefaultFlushTimer.TotalMilliseconds;
            _backgroundFlushTimer.Elapsed += SaveHandler;
            _backgroundFlushTimer.Enabled = true;
        }
        


        private void SaveHandler(object o, ElapsedEventArgs e)
        {
            _backgroundFlushTimer.Enabled = false;
            FlushTrackedQueries();
            _backgroundFlushTimer.Enabled = true;
        }

        private void FlushTrackedQueries()
        {
            Trail[] copy;
            lock (_items)
            {
                copy = _items.ToArray();
                _items.Clear();
            }
            var aggregatedQueries = (from grouping in copy.GroupBy(x => x.Query)
                               select (RedisValue)JsonConvert.SerializeObject(
                                   new
                                   {
                                       QueryText = grouping.First().Query,
                                       AvgTimeMS = (int)grouping.Average(x => x.Runtime.TotalMilliseconds),
                                       MaxTimeMS = (int)grouping.Max(x => x.Runtime.TotalMilliseconds),
                                       MinTimeMS = (int)grouping.Min(x => x.Runtime.TotalMilliseconds),
                                       TimeStamp = DateTime.UtcNow,
                                       CacheHitRatio = grouping.Average(x => !x.IsCacheHit ? 0m : 1m),
                                       Runs = grouping.Count()
                                   })).ToArray();
            _connection.GetDatabase().ListRightPush(_collectionName, aggregatedQueries);
        }

        private async Task Track(Trail trail)
        {
            lock (_items)
                _items.Add(trail);
        }

    }
}