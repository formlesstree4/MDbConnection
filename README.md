# MDbConnection
An implementation of IDbConnection that supports multiple layers of caching and statistical tracking of queries. These cache layers and tracking layers are added at runtime after the object has been constructed.


# Usages: Caching
Simply treat it like a regular IDbConnection:
```csharp
var sqlConnection = new SqlConnection("connectionString");
using(var wrappedConnection = new MDbConnection<SqlConnection>(sqlConnection))
{
   // Adding a cache layer is simple:
   // wrappedConnection.Register(your implementation of IDbCacheLayer).

   // If you don't want to take advantage of caching, you don't have to!
   // You can simply invoke the regular IDbConnection methods and functions.
   // However, if you want to take advantage of caching, you have to use the cache methods:
   var enumerableResult = wrappedConnection.QueryCached<int>("SELECT 1");
   var scalarResult = wrappedConnection.QueryScalarCached<int>("SELECT 1");
   
   // If a caching layer has been added, then, when QueryCached or QueryScalarCached are executed
   // again, the results will come right from the cache layer first instead of SQL.
   
   // Parameterized queries are supported for caching as well:
   enumerableResult = wrappedConnection.QueryCached<int>("SELECT @a", new {a = 1});
   
   // You can even specify your own key to add additional uniqueness to the cache result:
   enumerableResult = wrappedConnection.QueryCached<int>("SELECT @a", new {a = 1}, "test");
   
}
```
MDbConnection comes with two implementations of IDbCacheLayer already completed:
  * MDbConnection: There is a built-in implementation of IDbCacheLayer that uses MemoryCache.
  * MDbConnection.Redis: Handles caching via a REDIS server.

The parameters should feel familiar to anyone that's used [Dapper](https://github.com/StackExchange/dapper-dot-net) before.

#Usages: Tracking
Every time a query is run, regardless if it is a cache hit or not, it gets reported to any IDbObserver that has subscribed to MDbConnection.
```csharp
var observer = new YourIDbObserver();
var sqlConnection = new SqlConnection("connectionString");
using(var wrappedConnection = new MDbConnection<SqlConnection>(sqlConnection))
{
   var disposable = wrappedConnection.Register(observer);
   // When you register to observe MDbConnection, you get back an IDisposable object.
   // At any point in time, you may choose to dispose of the object. Your observer will
   // no longer receive any additional tracking updates.
   // Please note that tracking updates are invoked on a different thread.
}
```
MDbConnection comes with two implementations of IDbObserver already completed:
  * MDbConnection.SqliteTracking: Handles aggregating and dumping results to a local SQLite database.
  * MDbConnection.Redis: Handles aggregating and dumping results to a REDIS server.
