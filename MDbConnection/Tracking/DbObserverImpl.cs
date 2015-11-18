using MDbConnection.Common;
using System;
using System.Threading.Tasks;

namespace MDbConnection.Tracking
{

    /// <summary>
    ///     Internal implementation of <see cref="IDbObserver"/> that <see cref="MDbConnection{TDbConnection}"/> uses.
    /// </summary>
    internal sealed class DbObserverImpl : IDbObserver
    {

        /// <summary>
        ///     Gets the callback function associated with this <see cref="ITrailSubscriber"/>.
        /// </summary>
        public Func<Trail, Task> Callback { get; }

        /// <summary>
        ///     Creates a new instance of the <see cref="DbObserverImpl"/> class.
        /// </summary>
        /// <param name="callback">The callback to invoke</param>
        public DbObserverImpl(Func<Trail, Task> callback)
        {
            Callback = callback;
        }
    }
}