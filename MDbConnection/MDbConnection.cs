using Dapper;
using System.Collections.Generic;
using System.Data.Caching;
using System.Data.Common;
using System.Data.Extensions;
using System.Data.Tracking;
using System.Linq;
using System.Threading.Tasks;


namespace System.Data
{

    /// <summary>
    ///     An implementation of <see cref="IDbConnection"/> that serves as an abstraction layer.
    /// </summary>
    /// <typeparam name="TDbConnection"><see cref="IDbConnection"/> that is to be wrapped around</typeparam>
    /// <remarks>
    ///     <see cref="MDbConnection{TDbConnection}"/> is a layer that wraps around a given <see cref="IDbConnection"/>.
    /// </remarks>
    public sealed class MDbConnection<TDbConnection> : IDbConnection
        where TDbConnection : IDbConnection, new()
    {

        private const string QueryKey = "Query";
        private const string ScalarKey = "Scalar";

        private readonly List<IDbCacheLayer> _caches = new List<IDbCacheLayer>();
        private readonly List<MDbObserverToken> _callbacks = new List<MDbObserverToken>();
        private readonly TDbConnection _connection;
        private readonly TimeSpan _defaultCacheTimeout = TimeSpan.FromMinutes(1);

        #region Events

        /// <summary>
        ///     Raised when <see cref="MDbConnection{TDbConnection}.Dispose"/> is invoked.
        /// </summary>
        public EventHandler Disposing;

        /// <summary>
        ///     Raised when <see cref="MDbConnection{TDbConnection}.Dispose"/> has completed.
        /// </summary>
        public EventHandler Disposed;

        #endregion

        #region Properties

        /// <summary>
        ///     Gets the underlying <see cref="IDbConnection"/>.
        /// </summary>
        /// <returns>
        ///     <typeparamref name="TDbConnection"/>
        /// </returns>
        public TDbConnection UnderlyingConnection => _connection;

        /// <summary>
        ///     Gets a <see cref="IReadOnlyCollection{T}"/> of the current <see cref="IDbCacheLayer"/> instances that are registered with <see cref="MDbConnection{TDbConnection}"/>.
        /// </summary>
        public IReadOnlyCollection<IDbCacheLayer> Caches => _caches.AsReadOnly();

        /// <summary>
        ///     Gets or sets a string value that will be appended to a query when used for cache lookups that are non-scalar.
        /// </summary>
        /// <remarks>
        ///     Before a query is executed against the underlying <see cref="IDbConnection"/>, the query string is hashed with <see cref="CacheQueryKeyPostFix"/> or <see cref="CacheScalarKeyPostFix"/> appended to it.
        /// <see cref="CacheQueryKeyPostFix"/> is used when <see cref="QueryCached{T}(string, object, string, TimeSpan?, int?, CommandType?)"/> is invoked. while <see cref="CacheScalarKeyPostFix"/> is used when <see cref="QueryScalarCached{T}(string, object, string, TimeSpan?, int?, CommandType?)"/> is invoked.
        /// The reason the query string has a value appended to it is to guarantee uniqueness between the querying types.
        /// 
        /// Be wary when changing this value from its default. If <see cref="MDbConnection{TDbConnection}"/> is used across multiple instances, changing <see cref="CacheScalarKeyPostFix"/> or <see cref="CacheQueryKeyPostFix"/> from their default values inconsistently can lead to duplicated data being stored in the cache.
        /// </remarks>
        public string CacheQueryKeyPostFix { get; set; } = QueryKey;

        /// <summary>
        ///     Gets or sets a string value that will be appended to a query when used for cache lookups that are scalar.
        /// </summary>
        /// <remarks>
        ///     Before a query is executed against the underlying <see cref="IDbConnection"/>, the query string is hashed with <see cref="CacheQueryKeyPostFix"/> or <see cref="CacheScalarKeyPostFix"/> appended to it.
        /// <see cref="CacheQueryKeyPostFix"/> is used when <see cref="QueryCached{T}(string, object, string, TimeSpan?, int?, CommandType?)"/> is invoked. while <see cref="CacheScalarKeyPostFix"/> is used when <see cref="QueryScalarCached{T}(string, object, string, TimeSpan?, int?, CommandType?)"/> is invoked.
        /// The reason the query string has a value appended to it is to guarantee uniqueness between the querying types.
        /// 
        /// Be wary when changing this value from its default. If <see cref="MDbConnection{TDbConnection}"/> is used across multiple instances, changing <see cref="CacheScalarKeyPostFix"/> or <see cref="CacheQueryKeyPostFix"/> from their default values inconsistently can lead to duplicated data being stored in the cache.
        /// </remarks>
        public string CacheScalarKeyPostFix { get; set; } = ScalarKey;

        #region IDbConnection Properties

        /// <summary>
        ///     Gets or sets the string used to open a database.
        /// </summary>
        public string ConnectionString
        {
            get
            {
                return _connection.ConnectionString;
            }

            set
            {
                _connection.ConnectionString = value;
            }
        }

        /// <summary>
        ///     Gets the time to wait while trying to establish a connection before terminating the attempt and generating an error.
        /// </summary>
        public int ConnectionTimeout => _connection.ConnectionTimeout;

        /// <summary>
        ///     Gets the name of the current database or the database to be used after a connection is opened.
        /// </summary>
        public string Database => _connection.Database;

        /// <summary>
        ///     Gets the current state of the connection.
        /// </summary>
        public ConnectionState State => _connection.State;

        #endregion IDbConnection Properties

        #endregion Properties

        #region Methods

        /// <summary>
        ///     Registers an <see cref="IDbCacheLayer"/> for <see cref="MDbConnection{TDbConnection}"/> to leverage.
        /// </summary>
        /// <param name="cache">A <see cref="IDbCacheLayer"/> implementation</param>
        public void Register(IDbCacheLayer cache)
        {
            _caches.Add(cache);
        }

        /// <summary>
        ///     Subscribes to <see cref="MDbConnection{TDbConnection}"/> and listens for every query that is run.
        /// </summary>
        /// <param name="callback">A callback that is invoked when a query has run</param>
        /// <returns><see cref="RapidTrailObserver"/></returns>
        /// <remarks>
        ///     The object the callee gets back is <see cref="RapidTrailObserver"/>. This object may simply be disposed of whenever the callee is done listening to the <see cref="MDbConnection{TDbConnection}"/>.
        /// </remarks>
        public IDisposable Subscribe(Func<Trail, Task> callback)
        {
            return Subscribe(new DbObserverImpl(callback));
        }

        /// <summary>
        ///     Subscribes to <see cref="MDbConnection{TDbConnection}"/> and listens for every query that is run.
        /// </summary>
        /// <param name="subscriber">A <see cref="ITrailSubscriber"/> that is invoked when a query has run.</param>
        /// <returns><see cref="RapidTrailObserver"/></returns>
        /// <remarks>
        ///     The object the callee gets back is <see cref="RapidTrailObserver"/>. This object may simply be disposed of whenever the callee is done listening to the <see cref="MDbConnection{TDbConnection}"/>.
        /// </remarks>
        public IDisposable Subscribe(IDbObserver subscriber)
        {
            var rto = new MDbObserverToken(subscriber);
            _callbacks.Add(rto);
            return rto;
        }

        /// <summary>
        ///     Queries the <see cref="MDbConnection{T}" /> to get a set of objects, using caching as flagged by the object.
        /// </summary>
        /// <typeparam name="T">
        ///     Any type compatible with
        ///     <see cref="SqlMapper.Query{T}(IDbConnection, string, object, IDbTransaction, bool, int?, CommandType?)"/>.
        ///     This includes, but is not limited to:
        ///     1) Types where <see cref="Type.IsPrimitive"/> is true.
        ///     2) <see cref="string"/>
        ///     3) <see cref="double"/>
        ///     4) <see cref="DateTime"/>
        /// </typeparam>
        /// <param name="sql">
        ///     The SQL to be executed by the underlying <typeparamref name="TDbConnection"/>.
        /// </param>
        /// <param name="param">An object that represents the parameters to use in the <paramref name="sql" /></param>
        /// <param name="key">
        ///     An optional, unique key that will be used for cache lookups. If one is not supplied, then, a hash is generated from
        ///     the <paramref name="sql" />.
        /// </param>
        /// <param name="expiration">An optional <see cref="TimeSpan" />. Defaults to <see cref="_defaultCacheTimeout" /></param>
        /// <param name="commandTimeout">
        ///     The time, in seconds, to wait for executing the <paramref name="sql" /> before throwing a
        ///     <see cref="SqlException" />
        /// </param>
        /// <param name="commandType"><see cref="CommandType" />. Defaults to <see cref="CommandType.Text" /></param>
        /// <returns>
        ///     <see cref="IEnumerable{T}" />
        /// </returns>
        /// <remarks>
        ///     Before the underlying <typeparamref name="TDbConnection"/> is invoked, a cache lookup will be performed. If
        ///     <paramref name="key" /> is not null, then, the cache lookup will use the <paramref name="key" />. If
        ///     <paramref name="key" /> is null, then the <paramref name="sql" /> value will be hashed with "Query", 
        ///     and a JSON representation of <paramref name="param"/> object appended to it. This hashed value will then become 
        ///     the new <paramref name="key" /> for the cache lookup. If there is a cache miss, the query is passed 
        ///     through to the underlying <typeparamref name="TDbConnection"/> and the result of the query is cached based 
        ///     on the currently registered <see cref="IDbCacheLayer"/> members.
        /// </remarks>
        public IEnumerable<T> QueryCached<T>(string sql, object param = null,
            string key = null, TimeSpan? expiration = null,
            int? commandTimeout = null, CommandType? commandType = null)
        {
            key = key ?? (sql + CacheQueryKeyPostFix).Hash(param);
            expiration = expiration.HasValue ? expiration : _defaultCacheTimeout;
            List<T> results;

            var cacheHit = IsCollectionCacheHit<T>(key);

            if (cacheHit.HasValue)
            {
                HandleCommandImpl(new Trail
                {
                    IsCacheHit = true,
                    Parameters = param,
                    Query = sql,
                    Runtime = TimeSpan.Zero,
                    Start = DateTime.UtcNow
                });

                results = cacheHit.Value.AsList();
            }
            else
            {
                results = this.Query<T>(sql, param, commandTimeout: commandTimeout, commandType: commandType).ToList();
                StoreCollectionCacheHit(key, results, expiration.Value);
            }

            return results;
        }

        /// <summary>
        ///     Queries the <see cref="MDbConnection{T}" /> to get a scalar value, using caching as flagged by the object.
        /// </summary>
        /// <typeparam name="T">
        ///     Any type compatible with
        ///     <see cref="SqlMapper.ExecuteScalar{T}(IDbConnection, string, object, IDbTransaction, int?, CommandType?)" />
        ///     . This includes, but is not limited to:
        ///     1) Types where <see cref="System.Type.IsPrimitive" /> is true
        ///     2) <see cref="string" />
        ///     3) <see cref="double" />
        ///     4) <see cref="DateTime" />
        /// </typeparam>
        /// <param name="sql">
        ///     The SQL to be executed by the underlying <typeparamref name="TDbConnection"/>.
        /// </param>
        /// <param name="param">An object that represents the parameters to use in the <paramref name="sql" /></param>
        /// <param name="key">
        ///     An optional, unique key that will be used for cache lookups. If one is not supplied, then, a hash is generated from
        ///     the <paramref name="sql" />.
        /// </param>
        /// <param name="expiration">An optional <see cref="TimeSpan" />. Defaults to <see cref="_defaultCacheTimeout" /></param>
        /// <param name="commandTimeout">
        ///     The time, in seconds, to wait for executing the <paramref name="sql" /> before throwing a
        ///     <see cref="SqlException" />
        /// </param>
        /// <param name="commandType"><see cref="CommandType" />. Defaults to <see cref="CommandType.Text" /></param>
        /// <returns>
        ///     <typeparamref name="T" />
        /// </returns>
        /// <remarks>
        ///     Before the underlying <typeparamref name="TDbConnection"/> is invoked, a cache lookup will be performed. If
        ///     <paramref name="key" /> is not null, then, the cache lookup will use the <paramref name="key" />. If
        ///     <paramref name="key" /> is null, then the <paramref name="sql" /> value will be hashed with "Scalar", 
        ///     and a JSON representation of the <paramref name="param"/> object appended to it. This hashed value will 
        ///     then become the new <paramref name="key" /> for the cache lookup. If there is a cache miss, the query 
        ///     is passed through to the underlying <typeparamref name="TDbConnection"/> and the result of the query is cached based 
        ///     on the currently registered <see cref="IDbCacheLayer"/> members.
        /// </remarks>
        public T QueryScalarCached<T>(string sql, object param = null,
            string key = null, TimeSpan? expiration = null,
            int? commandTimeout = null, CommandType? commandType = null)
        {
            key = key ?? (sql + CacheScalarKeyPostFix).Hash(param);
            expiration = expiration ?? _defaultCacheTimeout;
            T results;

            var cacheHit = IsScalarCacheHit<T>(key);

            if (cacheHit.HasValue)
            {
                HandleCommandImpl(new Trail
                {
                    IsCacheHit = true,
                    Parameters = param,
                    Query = sql,
                    Runtime = TimeSpan.Zero,
                    Start = DateTime.UtcNow
                });
                results = cacheHit.Value;
            }
            else
            {
                results = this.ExecuteScalar<T>(sql, param, commandTimeout: commandTimeout, commandType: commandType);
                StoreScalarCacheHit(key, results, expiration.Value);
            }

            return results;
        }

        /// <summary>
        ///     Checks to see if any of the caching layers contains the given <paramref name="key"/> for a collection type.
        /// </summary>
        /// <typeparam name="T">The type of the collection</typeparam>
        /// <param name="key">The unique key that identifies the collection</param>
        /// <returns><see cref="Option{T}"/> where <typeparamref name="T"/> is <see cref="IReadOnlyCollection{T}"/></returns>
        /// <remarks>
        ///     The first cache layer to return a <see cref="Option{T}"/> where <see cref="Option{T}.Empty"/> is false wins.
        /// </remarks>
        private Option<IReadOnlyCollection<T>> IsCollectionCacheHit<T>(string key)
        {
            foreach (var cacheLayer in _caches)
            {
                var isCacheHit = cacheLayer.GetCollection<T>(key);
                if (isCacheHit.HasValue)
                {
                    return isCacheHit;
                }
            }
            return Option.Empty;
        }

        /// <summary>
        ///     Checks to see if any of the caching layers contains the given <paramref name="key"/> for a scalar type.
        /// </summary>
        /// <typeparam name="T">The type of the value</typeparam>
        /// <param name="key">The unique key that identifies the value</param>
        /// <returns><see cref="Option{T}"/></returns>
        /// <remarks>
        ///     The first cache layer to return a <see cref="Option{T}"/> where <see cref="Option{T}.Empty"/> is false wins.
        /// </remarks>
        private Option<T> IsScalarCacheHit<T>(string key)
        {
            foreach (var cacheLayer in _caches)
            {
                var isCacheHit = cacheLayer.GetScalar<T>(key);
                if (isCacheHit.HasValue)
                {
                    return isCacheHit;
                }
            }
            return Option.Empty;

        }

        /// <summary>
        ///     Stores an <see cref="IEnumerable{T}"/> collection into the cache layers.
        /// </summary>
        /// <typeparam name="T">The type of the collection</typeparam>
        /// <param name="key">The unique key that identifies the collection</param>
        /// <param name="value">The collection to save</param>
        /// <param name="expiration"><see cref="TimeSpan"/> from <see cref="DateTime.UtcNow"/> when the collection expires</param>
        private void StoreCollectionCacheHit<T>(string key, IEnumerable<T> value, TimeSpan expiration)
        {
            foreach (var cacheLayer in _caches)
                cacheLayer.SetCollection(key, value, expiration);
        }

        /// <summary>
        ///     Stores a scalar value of type <typeparamref name="T"/> into the cache layers.
        /// </summary>
        /// <typeparam name="T">The type of the value</typeparam>
        /// <param name="key">The unique key that identifies the value</param>
        /// <param name="value">The value to save</param>
        /// <param name="expiration"><see cref="TimeSpan"/> from <see cref="DateTime.UtcNow"/> when the value expires</param>
        private void StoreScalarCacheHit<T>(string key, T value, TimeSpan expiration)
        {
            foreach (var cacheLayer in _caches)
                cacheLayer.SetScalar(key, value, expiration);
        }

        /// <summary>
        ///     Implementation Detail. Read it.
        /// </summary>
        /// <param name="trail"></param>
        private void HandleCommandImpl(Trail trail)
        {
            Task.Run(async () =>
            {
                await InvokeCallbacksImpl(trail);
            });
        }

        /// <summary>
        ///     Handles tracking a <see cref="IDbCommand"/> execution information.
        /// </summary>
        /// <param name="trail">The <see cref="Trail"/> results.</param>
        private async Task InvokeCallbacksImpl(Trail trail)
        {
            foreach (var cb in (from cbo in _callbacks
                                where !cbo.IsDisposed
                                select cbo))
            {
                await cb?.Tracker?.Callback?.Invoke(trail.Clone());
            }
        }

        #region IDbConnection Methods

        /// <summary>
        ///     Begins a database transaction
        /// </summary>
        /// <returns><see cref="IDbTransaction"/></returns>
        public IDbTransaction BeginTransaction() => _connection.BeginTransaction();

        /// <summary>
        ///     Begins a database transaction with the specified <see cref="IsolationLevel"/> value.
        /// </summary>
        /// <param name="il">The <see cref="IsolationLevel"/> to use</param>
        /// <returns><see cref="IDbTransaction"/></returns>
        public IDbTransaction BeginTransaction(IsolationLevel il) => _connection.BeginTransaction(il);

        /// <summary>
        ///     Changes the current database for an open <see cref="IDbConnection"/> object.
        /// </summary>
        /// <param name="databaseName">The name of the database to change to</param>
        public void ChangeDatabase(string databaseName) => _connection.ChangeDatabase(databaseName);

        /// <summary>
        ///     Closes teh connection to the database.
        /// </summary>
        public void Close() => _connection.Close();

        /// <summary>
        ///     Creates and returns a <see cref="IDbCommand"/> object associated with the connection.
        /// </summary>
        /// <returns></returns>
        public IDbCommand CreateCommand() => new MDbCommand(_connection.CreateCommand(), HandleCommandImpl);

        /// <summary>
        ///     Performs application-defined tasks associated with freeing, releasing, or resetting unmanaged resources.
        /// </summary>
        public void Dispose()
        {
            Disposing?.Invoke(this, EventArgs.Empty);
            _connection.Close();

            // For sake of completeness, dispose of our callbacks.
            foreach (var callback in _callbacks) (callback as IDisposable).Dispose();

            _callbacks.Clear();

            // Dispose of our cache layers if applicable.
            foreach (var layer in _caches) (layer as IDisposable)?.Dispose();

            _caches.Clear();
            Disposed?.Invoke(this, EventArgs.Empty);
        }

        /// <summary>
        ///     Opens a database connection with the settings specified by the <see cref="ConnectionString"/> property of the provider-specific Connection object.
        /// </summary>
        public void Open() => _connection.Open();

        #endregion IDbConnection Methods

        #endregion

        #region Constructors

        /// <summary>
        ///     Creates a new instance of <see cref="MDbConnection{TDbConnection}"/>.
        /// </summary>
        /// <param name="connection">The <see cref="T:TDbConnection"/> to use.</param>
        public MDbConnection(TDbConnection connection)
        {
            _connection = connection;
        }

        /// <summary>
        ///     Creates a new instance of <see cref="MDbConnection{TDbConnection}"/>.
        /// </summary>
        /// <param name="connection">The <see cref="T:TDbConnection.ConnectionString"/> to use to create a new <typeparamref name="TDbConnection"/></param>
        public MDbConnection(string connection)
            : this(new TDbConnection() { ConnectionString = connection }) { }

        /// <summary>
        ///     Creates a new instance of <see cref="MDbConnection{TDbConnection}"/>.
        /// </summary>
        public MDbConnection() : this(new TDbConnection()) { }
        
        #endregion

    }
}