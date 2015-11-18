using MDbConnection.Caching.Hashing;
using Newtonsoft.Json;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Data;
using System.Linq;
using System.Text;

namespace MDbConnection.Extensions
{

    /// <summary>
    ///     Helpful extensions.
    /// </summary>
    public static class Extensions
    {

        /// <summary>
        ///     Takes <see cref="IDataParameterCollection"/> and converts it to a <see cref="ReadOnlyDictionary{TKey, TValue}"/>.
        /// </summary>
        /// <param name="parameters">Parameters to convert</param>
        /// <returns><see cref="ReadOnlyDictionary{TKey, TValue}"/></returns>
        public static ReadOnlyDictionary<string, object> ToDictionary(this IDataParameterCollection parameters)
        {
            var dict = parameters.Cast<IDbDataParameter>().ToDictionary(parameter => parameter.ParameterName, parameter => parameter.Value);
            return new ReadOnlyDictionary<string, object>(dict);
        }

        /// <summary>
        ///     Converts <paramref name="source"/> into a <see cref="List{T}"/>.
        /// </summary>
        /// <param name="source">The <see cref="IEnumerable{T}"/> to convert or cast to <see cref="List{T}"/></param>
        public static List<T> CastOrToList<T>(this IEnumerable<T> source)
        {
            return (source == null || source is List<T>) ? (List<T>)source : source.ToList();
        }

        /// <summary>
        ///     Creates a hash of the <paramref name="input"/> using <see cref="Murmur3"/>
        /// </summary>
        /// <param name="input">
        ///     The <see cref="string"/> to hash.
        /// </param>
        /// <param name="o">
        ///     An additional, optional object to append to the end of <paramref name="input"/> as JSON.
        /// </param>
        /// <returns>
        ///     <see cref="string"/>
        /// </returns>
        /// <exception cref="ArgumentNullException"><paramref name="input" /> is null. </exception>
        /// <exception cref="EncoderFallbackException">A fallback occurred (see Character Encoding in the .NET Framework for complete explanation)-and-<see cref="P:System.Text.Encoding.EncoderFallback" /> is set to <see cref="T:System.Text.EncoderExceptionFallback" />.</exception>
        /// <exception cref="ArgumentOutOfRangeException"><paramref name="input" /> is null. </exception>
        /// <exception cref="FormatException"/>
        public static string Hash(this string input, object o = null)
        {
            var stringtoHash = input;
            if (o != null) stringtoHash += JsonConvert.SerializeObject(o);
            var hashedBytes = Murmur3.ComputeHash32(stringtoHash);
            return hashedBytes.ToHex();
        }

        /// <summary>
        ///     Converts <see cref="input"/> to a hexadecimal string representation.
        /// </summary>
        /// <param name="input">
        ///     Byte array to be converted.
        /// </param>
        /// <returns><see cref="string"/></returns>
        /// <exception cref="FormatException"/>
        /// <exception cref="ArgumentOutOfRangeException"><paramref name="input" /> length is less than zero. </exception>
        /// <remarks>
        ///     ... we can probably make this faster.
        /// </remarks>
        public static string ToHex(this byte[] input)
        {
            var sb = new StringBuilder(input.Length * 2);
            foreach (var b in input) sb.Append(b.ToString("x2"));
            return sb.ToString();
        }

    }

}