using System.Data.Common;
using System.Data.Extensions;

namespace System.Data
{

    /// <summary>
    ///     An implementation of <see cref="IDbCommand"/> with hooks for tracking purposes.
    /// </summary>
    internal sealed class MDbCommand : IDbCommand
    {

        private readonly Action<Trail> _callback;
        private readonly IDbCommand _command;

        /// <summary>
        ///     Creates a new instance of the <see cref="MDbCommand"/>.
        /// </summary>
        /// <param name="command">An <see cref="IDbCommand"/></param>
        /// <param name="trackingCallback">Callback to be executed for tracking purposes.</param>
        public MDbCommand(IDbCommand command,
            Action<Trail> trackingCallback)
        {
            _command = command;
            _callback = trackingCallback;
        }


        #region Properties

        public string CommandText
        {
            get
            {
                return _command.CommandText;
            }

            set
            {
                _command.CommandText = value;
            }
        }

        public int CommandTimeout
        {
            get
            {
                return _command.CommandTimeout;
            }

            set
            {
                _command.CommandTimeout = value;
            }
        }

        public CommandType CommandType
        {
            get
            {
                return _command.CommandType;
            }

            set
            {
                _command.CommandType = value;
            }
        }

        public IDbConnection Connection
        {
            get
            {
                return _command.Connection;
            }

            set
            {
                _command.Connection = value;
            }
        }

        public IDataParameterCollection Parameters => _command.Parameters;

        public IDbTransaction Transaction
        {
            get
            {
                return _command.Transaction;
            }

            set
            {
                _command.Transaction = value;
            }
        }

        public UpdateRowSource UpdatedRowSource
        {
            get
            {
                return _command.UpdatedRowSource;
            }

            set
            {
                _command.UpdatedRowSource = value;
            }
        }

        #endregion Properties

        #region Methods

        public IDbDataParameter CreateParameter() => _command.CreateParameter();

        public void Cancel() => _command.Cancel();

        public void Dispose() => _command.Dispose();

        public void Prepare() => _command.Prepare();

        public int ExecuteNonQuery()
        {
            DbException exception = null;
            var start = DateTime.UtcNow;
            var sw = Diagnostics.Stopwatch.StartNew();
            try
            {
                return _command.ExecuteNonQuery();
            }
            catch (DbException de)
            {
                exception = de;
                throw;
            }
            finally
            {
                sw.Stop();
                _callback?.Invoke(new Trail
                {
                    Query = CommandText,
                    Start = start,
                    Runtime = sw.Elapsed,
                    Parameters = Parameters.ToDictionary(),
                    Exception = exception,
                    IsCacheHit = false
                });
            }
        }

        public IDataReader ExecuteReader()
        {
            DbException exception = null;
            var start = DateTime.UtcNow;
            var sw = Diagnostics.Stopwatch.StartNew();
            try
            {
                return _command.ExecuteReader();
            }
            catch (DbException de)
            {
                exception = de;
                throw;
            }
            finally
            {
                sw.Stop();
                _callback?.Invoke(new Trail
                {
                    Query = CommandText,
                    Start = start,
                    Runtime = sw.Elapsed,
                    Parameters = Parameters.ToDictionary(),
                    Exception = exception,
                    IsCacheHit = false
                });
            }
        }

        public IDataReader ExecuteReader(CommandBehavior behavior)
        {
            DbException exception = null;
            var start = DateTime.UtcNow;
            var sw = Diagnostics.Stopwatch.StartNew();
            try
            {
                return _command.ExecuteReader(behavior);
            }
            catch (DbException de)
            {
                exception = de;
                throw;
            }
            finally
            {
                sw.Stop();
                _callback?.Invoke(new Trail
                {
                    Query = CommandText,
                    Start = start,
                    Runtime = sw.Elapsed,
                    Parameters = Parameters.ToDictionary(),
                    Exception = exception,
                    IsCacheHit = false
                });
            }
        }

        public object ExecuteScalar()
        {
            DbException exception = null;
            var start = DateTime.UtcNow;
            var sw = Diagnostics.Stopwatch.StartNew();
            try
            {
                return _command.ExecuteScalar();
            }
            catch (DbException de)
            {
                exception = de;
                throw;
            }
            finally
            {
                sw.Stop();
                _callback?.Invoke(new Trail
                {
                    Query = CommandText,
                    Start = start,
                    Runtime = sw.Elapsed,
                    Parameters = Parameters.ToDictionary(),
                    Exception = exception,
                    IsCacheHit = false
                });
            }
        }

        #endregion Methods

    }
}