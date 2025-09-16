using System;
using System.Collections.Generic;
using System.Text;

namespace TelemetryCore.Contracts
{
    /// <summary>
    /// JSON serializer facade aligning behaviour between .NET 8/6 (System.Text.Json) and .NET Framework 4.7.2 / .NET Standard 2.0 (Newtonsoft.Json).
    /// </summary>
    /// <remarks>
    /// <para>
    /// The API is stable across target frameworks. Implementations differ by target: .NET 6/8 use
    /// <c>System.Text.Json</c> built into the shared framework; .NET Framework 4.7.2 and .NET Standard 2.0 use <c>Newtonsoft.Json</c>.
    /// Both produce camelCase JSON and omit properties with null values.
    /// </para>
    /// <para>Thread safety: this type is stateless and thread-safe.</para>
    /// </remarks>
    public static class StatsSerializer
    {
#if NET6_0_OR_GREATER
        /// <summary>
        /// Serialiser options for System.Text.Json (camelCase, ignore nulls, no indentation).
        /// </summary>
        private static readonly System.Text.Json.JsonSerializerOptions _opt = new System.Text.Json.JsonSerializerOptions
        {
            PropertyNamingPolicy = System.Text.Json.JsonNamingPolicy.CamelCase,
            DefaultIgnoreCondition = System.Text.Json.Serialization.JsonIgnoreCondition.WhenWritingNull,
            WriteIndented = false
        };

        /// <summary>
        /// Serialises the specified <see cref="StatsEnvelopeV1"/> to JSON.
        /// </summary>
        /// <param name="e">The envelope instance to serialise.</param>
        /// <returns>A compact JSON string (camelCase; null values omitted).</returns>
        /// <remarks>Thread-safe; does not pretty-print.</remarks>
        public static string ToJson(StatsEnvelopeV1 e) => System.Text.Json.JsonSerializer.Serialize(e, _opt);

        /// <summary>
        /// Deserialises a JSON string to a <see cref="StatsEnvelopeV1"/> instance.
        /// </summary>
        /// <param name="s">The JSON payload in camelCase.</param>
        /// <returns>A populated <see cref="StatsEnvelopeV1"/>.</returns>
        /// <exception cref="System.Text.Json.JsonException">Thrown when the JSON is invalid or mismatches the schema.</exception>
        public static StatsEnvelopeV1 FromJson(string s) => System.Text.Json.JsonSerializer.Deserialize<StatsEnvelopeV1>(s, _opt)!;
#else
        /// <summary>
        /// Serialiser settings for Newtonsoft.Json (camelCase, ignore nulls).
        /// </summary>
        private static readonly Newtonsoft.Json.JsonSerializerSettings _opt = new Newtonsoft.Json.JsonSerializerSettings
        {
            ContractResolver = new Newtonsoft.Json.Serialization.CamelCasePropertyNamesContractResolver(),
            NullValueHandling = Newtonsoft.Json.NullValueHandling.Ignore
        };

        /// <summary>
        /// Serialises the specified <see cref="StatsEnvelopeV1"/> to JSON.
        /// </summary>
        /// <param name="e">The envelope instance to serialise.</param>
        /// <returns>A compact JSON string (camelCase; null values omitted).</returns>
        /// <remarks>Thread-safe; does not pretty-print.</remarks>
        public static string ToJson(StatsEnvelopeV1 e) => Newtonsoft.Json.JsonConvert.SerializeObject(e, _opt);

        /// <summary>
        /// Deserialises a JSON string to a <see cref="StatsEnvelopeV1"/> instance.
        /// </summary>
        /// <param name="s">The JSON payload in camelCase.</param>
        /// <returns>A populated <see cref="StatsEnvelopeV1"/>.</returns>
        /// <exception cref="Newtonsoft.Json.JsonException">Thrown when the JSON is invalid or mismatches the schema.</exception>
        public static StatsEnvelopeV1 FromJson(string s) => Newtonsoft.Json.JsonConvert.DeserializeObject<StatsEnvelopeV1>(s);
#endif
    }
}