using System;
using System.Collections.Generic;

namespace TelemetryCore.Contracts
{
    /// <summary>
    /// Transport-agnostic heartbeat and metrics envelope containing identity, health, gauges, counters, and tags.
    /// </summary>
    /// <remarks>
    /// <para>
    /// This type models a single snapshot of system health and metrics intended for emission to Prometheus/Grafana or
    /// internal tools. It carries system identity, an optional numeric health status, flexible gauges/counters, and
    /// free-form tags.
    /// </para>
    /// <para>
    /// Thread safety: this is a mutable data-transfer object; instances are not thread-safe for concurrent mutation.
    /// Prefer creating, populating, and publishing from a single thread.
    /// </para>
    /// <para>
    /// Schema version: see <see cref="SchemaVersion"/> for the additive evolution policy.
    /// </para>
    /// </remarks>
    public sealed class StatsEnvelopeV1
    {
        // Identity
        /// <summary>
        /// Logical system name emitting the snapshot (for example, "MessageBus").
        /// </summary>
        /// <remarks>Lowercase/uppercase is preserved as provided.</remarks>
        public string System { get; set; } = "";

        /// <summary>
        /// Deployment environment identifier, such as DEV, TEST, or PROD.
        /// </summary>
        /// <remarks>Use short, stable identifiers (for example, DEV/TEST/PROD/QA).</remarks>
        public string Env { get; set; } = "";

        /// <summary>
        /// Optional instance or partition identifier (for example, "P01"/"T01"). Use empty string if not applicable.
        /// </summary>
        /// <remarks>Empty string is rendered as <c>default</c> in Prometheus labels.</remarks>
        public string Instance { get; set; } = "";

        /// <summary>
        /// Host machine name where the snapshot originates.
        /// </summary>
        public string Host { get; set; } = "";

        /// <summary>
        /// Service or component version (semantic version recommended).
        /// </summary>
        public string Version { get; set; } = "";

        /// <summary>
        /// Snapshot timestamp in UTC when the heartbeat/metrics are taken.
        /// </summary>
        public DateTime Utc { get; set; }

        // Health
        /// <summary>
        /// Normalised health value where 1 = green, 0.5 = yellow, 0 = red, and <c>null</c> = unknown.
        /// </summary>
        /// <remarks>
        /// <para>
        /// Emit <c>null</c> when health cannot be determined; serializers omit <c>null</c> fields from JSON.
        /// </para>
        /// </remarks>
        public double? HealthStatus { get; set; }

        // Generic metrics (names are lowercase snake_case; units in the name)
        /// <summary>
        /// Point-in-time gauges keyed by lowercase snake_case metric names.
        /// </summary>
        /// <remarks>
        /// <para>
        /// Names include units where applicable (for example, <c>_seconds</c>, <c>_bytes</c>). Examples:
        /// <c>messagebus_queue_depth</c>, <c>processor_busy_seconds</c>.
        /// </para>
        /// </remarks>
        public Dictionary<string, double> Gauges { get; set; } = new Dictionary<string, double>();

        /// <summary>
        /// Monotonic counters keyed by lowercase snake_case metric names ending with <c>_total</c>.
        /// </summary>
        /// <remarks>
        /// <para>
        /// Use with PromQL <c>rate()</c> / <c>increase()</c>. Examples: <c>messagebus_messages_total</c>,
        /// <c>requests_total</c>.
        /// </para>
        /// </remarks>
        public Dictionary<string, long> Counters { get; set; } = new Dictionary<string, long>();

        /// <summary>
        /// Free-form string tags providing additional context (for example, <c>region</c>, <c>role</c>).
        /// </summary>
        public Dictionary<string, string> Tags { get; set; } = new Dictionary<string, string>();

        // Versioning
        /// <summary>
        /// Schema version for the envelope payload; increases with additive, backward-compatible changes.
        /// </summary>
        /// <remarks>
        /// <para>
        /// Increment only for additive fields or semantics; avoid breaking changes. Consumers should tolerate
        /// unknown fields.
        /// </para>
        /// </remarks>
        public int SchemaVersion { get; set; } = 1;

        // Convenience (transport-agnostic)
        /// <summary>
        /// Returns a stable composite key of <see cref="System"/>, <see cref="Env"/>, and <see cref="Instance"/>.
        /// </summary>
        /// <returns>
        /// A string formatted as <c>System|Env|Instance</c>, with a hyphen (<c>-</c>) in place of an empty
        /// <see cref="Instance"/>.
        /// </returns>
        /// <example>
        /// <code language="csharp">
        /// var e = new TelemetryCore.Contracts.StatsEnvelopeV1
        /// {
        ///     System = "MessageBus",
        ///     Env = "DEV",
        ///     Instance = "P01"
        /// };
        /// var key = e.Key(); // "MessageBus|DEV|P01"
        /// </code>
        /// </example>
        public string Key() => $"{this.System}|{this.Env}|{(string.IsNullOrEmpty(this.Instance) ? "-" : this.Instance)}";

        /// <summary>
        /// Derived topic identifier for transport systems that use topics. Not intended for serialization.
        /// </summary>
        /// <remarks>
        /// The topic is built as <c>monitoring.{system}.{env}.{instance}</c>, where empty instance becomes
        /// <c>default</c>.
        /// </remarks>
        public string Topic => $"monitoring.{this.System.ToLowerInvariant()}.{this.Env}.{(string.IsNullOrEmpty(this.Instance) ? "default" : this.Instance)}";
    }
}
