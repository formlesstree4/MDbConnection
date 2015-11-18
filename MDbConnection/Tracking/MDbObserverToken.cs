using MDbConnection.Common;
using System;
using System.Threading.Tasks;

namespace MDbConnection.Tracking
{

    /// <summary>
    ///     A token passed off to the consumer of <see cref="MDbConnection{TDbConnection}"/> that contains a <see cref="IDbObserver"/> instance for receiving tracking callbacks.
    /// </summary>
    /// <remarks>
    ///     Upon being disposed, <see cref="Tracker"/> ceases to be invoked.
    /// </remarks>
    internal sealed class MDbObserverToken : IDisposable
    {

        /// <summary>
        ///     Gets the <see cref="IDbObserver"/> instance that will receive callbacks.
        /// </summary>
        public IDbObserver Tracker { get; private set; }

        /// <summary>
        ///     Gets a flag that indicates whether or not <see cref="MDbObserverToken"/> has been disposed of.
        /// </summary>
        public bool IsDisposed { get; private set; } = false;

        /// <summary>
        ///     Creates a new instance of <see cref="MDbObserverToken"/>.
        /// </summary>
        /// <param name="callback">The <see cref="IDbObserver"/> callback.</param>
        public MDbObserverToken(IDbObserver callback)
        {
            Tracker = callback;
        }

        /// <summary>
        ///     Creates a new instance of <see cref="MDbObserverToken"/>.
        /// </summary>
        /// <param name="callback">A <see cref="Func{T, TResult}"/> that is invoked when a query is to be tracked.</param>
        public MDbObserverToken(Func<Trail, Task> callback) : this(new DbObserverImpl(callback)) { }

        /// <summary>
        ///     Disposes of the <see cref="IDisposable"/> instance.
        /// </summary>
        public void Dispose()
        {
            IsDisposed = true;
            Tracker = null;
        }

    }
}