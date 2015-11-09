using System.Data.Common;
using System.Threading.Tasks;

namespace System.Data.Tracking
{

    /// <summary>
    ///     Describes an observer for <see cref="MDbConnection"/>
    /// </summary>
    public interface IDbObserver
    {

        /// <summary>
        ///     Gets the callback to invoke for tracking.
        /// </summary>
        Func<Trail, Task> Callback { get; }

    }
}