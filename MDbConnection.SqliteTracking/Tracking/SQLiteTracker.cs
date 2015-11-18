using Dapper;
using MDbConnection.Common;
using System;
using System.Collections.Generic;
using System.Data.SQLite;
using System.Linq;
using System.Threading.Tasks;
using System.Timers;

#pragma warning disable CS1998 // Async method lacks 'await' operators and will run synchronously

namespace MDbConnection.Tracking
{

    /// <summary>
    ///     A thread-safe implementation of <see cref="IDbObserver"/> that flushes its results to a localized SQLite database.
    /// </summary>
    public sealed class SQLiteTracker : IDbObserver, IDisposable
    {

        private const string DefaultDatabaseName = "SqliteTracker";
        private static readonly TimeSpan DefaultFlushTimer =
#if DEBUG
            TimeSpan.FromMinutes(1);
#else
            TimeSpan.FromMinutes(15);
#endif



        private readonly List<Trail> _items = new List<Trail>();
        private readonly SQLiteConnection _connection;
        private readonly Timer _backgroundFlushTimer;

        /// <summary>
        ///     Gets a static, default instance of <see cref="SQLiteTracker"/>
        /// </summary>
        public static SQLiteTracker Default { get; } = new SQLiteTracker();

#region IDbObserver

        /// <summary>
        ///     Gets the callback to invoke for tracking.
        /// </summary>
        public Func<Trail, Task> Callback { get; private set; }

#endregion IDbObserver

        /// <summary>
        ///     Creates a new instance of <see cref="SQLiteTracker"/>.
        /// </summary>
        public SQLiteTracker() : this(DefaultDatabaseName, DefaultFlushTimer) { }

        /// <summary>
        ///     Creates a new instance of <see cref="SQLiteTracker"/>.
        /// </summary>
        /// <param name="databaseName">The name of the database file, excluding the extension, that will get generated.</param>
        /// <param name="flushTimer">The interval at which queries will be flushed from memory into the SQLite database file.</param>
        public SQLiteTracker(string databaseName, TimeSpan flushTimer)
        {
            Callback = Track;
            _connection = new SQLiteConnection($"Data Source={databaseName}.sqlite");
            _connection.Execute(@"CREATE TABLE IF NOT EXISTS Queries (
    ID            INTEGER  PRIMARY KEY AUTOINCREMENT
                           NOT NULL,
    QueryText     TEXT     NOT NULL,
    AvgTimeMS     INTEGER  NOT NULL,
    MaxTimeMS     INTEGER  NOT NULL,
    MinTimeMS     INTEGER  NOT NULL,
    TimeStamp     DATETIME NOT NULL,
    CacheHitRatio DECIMAL  NOT NULL,
    Runs          INTEGER  NOT NULL
);");
            _backgroundFlushTimer = new Timer(flushTimer.TotalMilliseconds) { AutoReset = false };
            _backgroundFlushTimer.Elapsed += SaveHandler;
            _backgroundFlushTimer.Enabled = true;
        }



        /// <summary>
        ///     Disposes of the <see cref="IDisposable"/> instance.
        /// </summary>
        public void Dispose()
        {
            Callback = null; // If we null this, we stop getting queries.
            _backgroundFlushTimer.Dispose();
            FlushTrackedQueries();
            _connection.Dispose();
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
            var queryParams = (from grouping in copy.GroupBy(x => x.Query)
                               select
                                   new
                                   {
                                       QueryText = grouping.First().Query,
                                       AvgTimeMS = (int)grouping.Average(x => x.Runtime.TotalMilliseconds),
                                       MaxTimeMS = (int)grouping.Max(x => x.Runtime.TotalMilliseconds),
                                       MinTimeMS = (int)grouping.Min(x => x.Runtime.TotalMilliseconds),
                                       TimeStamp = DateTime.UtcNow,
                                       CacheHitRatio = grouping.Average(x => !x.IsCacheHit ? 0m : 1m),
                                       Runs = grouping.Count()
                                   }).ToList();
            foreach (var item in queryParams)
                _connection.Execute(
                    "INSERT INTO Queries (QueryText,AvgTimeMS,MaxTimeMS,MinTimeMS,TimeStamp,CacheHitRatio,Runs) VALUES (@QueryText,@AvgTimeMS,@MaxTimeMS,@MinTimeMS,@TimeStamp,@CacheHitRatio,@Runs)",
                    item);
        }

        private async Task Track(Trail trail)
        {
            lock (_items)
                _items.Add(trail);
        }

    }
}