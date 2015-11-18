using System;
using System.Data.Common;
using System.IO;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Text;

namespace MDbConnection.Common
{

    /// <summary>
    ///     Tracked Information for queries.
    /// </summary>
    public sealed class Trail
    {



        /// <summary>
        ///     Internal API calls to resolve the name of the executing assembly.
        /// </summary>
        private static class Win32
        {

            /// <summary>
            ///     Retrieves a module handle (<see cref="IntPtr"/>) for the specified <paramref name="moduleName"/>.
            /// </summary>
            /// <param name="moduleName">The name of the module</param>
            /// <returns><see cref="IntPtr"/></returns>
            /// <remarks>
            ///     If <paramref name="moduleName"/> is null, the <see cref="IntPtr"/> returned is the handle to the file that created the calling process.
            /// </remarks>
            [DllImport("kernel32.dll", CharSet = CharSet.Auto)]
            private static extern IntPtr GetModuleHandle(string moduleName);

            /// <summary>
            ///     Retrieves the fully qualified path for the file that contains the specified module.
            /// </summary>
            /// <param name="hModule">A <see cref="IntPtr"/> to the module</param>
            /// <param name="filename">A <see cref="StringBuilder"/> that will be populated with the path.</param>
            /// <param name="size">The maximum length of <paramref name="filename"/></param>
            /// <returns><see cref="uint"/> to indicate success</returns>
            [DllImport("kernel32.dll", CharSet = CharSet.Auto)]
            private static extern uint GetModuleFileName(IntPtr hModule, StringBuilder filename, uint size);

            /// <summary>
            ///     A lazy way to resolve the full path of the assembly that is currently loading.
            /// </summary>
            /// <remarks>
            ///     Lazy because under certain circumstances, this could become expensive. Said "certain" circumstances are:
            /// 1) If <see cref="Environment.GetCommandLineArgs"/> has no items. While unfortunate, this could happen in server environments.
            /// 2) If <see cref="Assembly.GetEntryAssembly"/> returns null. This would be insane, but, it could happen.
            /// </remarks>
            private static readonly Lazy<string> LazyGetName = new Lazy<string>(() =>
            {
                // Let's try this the real easy way.
                var fileNameViaCommandline = Environment.GetCommandLineArgs()[0];
                if (!string.IsNullOrWhiteSpace(fileNameViaCommandline) && File.Exists(fileNameViaCommandline)) return fileNameViaCommandline;

                // Well, that way didn't work. Let's try the 2nd easy (but expensive) way:
                var entryAssembly = Assembly.GetEntryAssembly();
                if (entryAssembly != null) return entryAssembly.FullName;

                // Well, the easy way didn't work. We have to do this the harder way now and make API calls.
                IntPtr moduleHandle = GetModuleHandle(null);
                var nameBuilder = new StringBuilder(1024);
                GetModuleFileName(moduleHandle, nameBuilder, (uint)nameBuilder.Capacity);
                return nameBuilder.ToString();
            });

            /// <summary>
            ///     Gets the full name of the assembly that is currently executing.
            /// </summary>
            /// <returns><see cref="string"/></returns>
            public static string ExecutingAssembly => LazyGetName.Value;

        }



        /// <summary>
        ///     Gets whether or not the tracked query executed successfully.
        /// </summary>
        /// <remarks>
        ///     Lazy helper property.
        /// </remarks>
        public bool Success => Exception == null;

        /// <summary>
        ///     Gets the query that was executed.
        /// </summary>
        public string Query { get; set; }

        /// <summary>
        ///     Gets the parameters, if any, that were used.
        /// </summary>
        public object Parameters { get; set; }

        /// <summary>
        ///     Gets the UTC <see cref="DateTime"/> the query started executing.
        /// </summary>
        public DateTime Start { get; set; }

        /// <summary>
        ///     Gets the <see cref="TimeSpan"/> that is the duration of the execution.
        /// </summary>
        public TimeSpan Runtime { get; set; }

        /// <summary>
        ///     Gets the name of the machine that has executed the query.
        /// </summary>
        public string MachineName { get; set; }

        /// <summary>
        ///     Gets the name of the program that has executed the query.
        /// </summary>
        public string ProgramName { get; set; }

        /// <summary>
        ///     Gets or sets the <see cref="DbException"/> that may have occurred.
        /// </summary>
        public DbException Exception { get; set; }

        /// <summary>
        ///     Gets or sets whether or not the query being tracked hit the Cache.
        /// </summary>
        public bool IsCacheHit { get; set; }



        /// <summary>
        ///     Creates a new instance of the <see cref="Trail"/> class.
        /// </summary>
        public Trail()
        {
            MachineName = Environment.MachineName;
            ProgramName = Win32.ExecutingAssembly;
        }


        /// <summary>
        ///     Creates a deep clone that is an exact duplicate of the current <see cref="Trail"/>
        /// </summary>
        /// <returns><see cref="Trail"/></returns>
        public Trail Clone()
        {
            return new Trail()
            {
                Query = Query,
                Parameters = Parameters,
                Start = Start,
                Runtime = Runtime,
                MachineName = MachineName,
                ProgramName = ProgramName,
                Exception = Exception,
                IsCacheHit = IsCacheHit
            };
        }


    }

}