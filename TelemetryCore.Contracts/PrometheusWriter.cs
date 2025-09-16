using TelemetryCore.Contracts;
using System;
using System.Collections.Generic;
using System.Text;

namespace TelemetryCore.Contracts
{
    /// <summary>
    /// Renders <see cref="TelemetryCore.Contracts.StatsEnvelopeV1"/> snapshots to the Prometheus text exposition format.
    /// </summary>
    /// <remarks>
    /// <para>
    /// Output includes standard metric headers for known metrics and applies stable labels on every line:
    /// <c>system</c>, <c>env</c>, <c>instance</c> (empty becomes <c>default</c>), <c>host</c>, and <c>version</c>.
    /// Metric names must be Prometheus-safe. Counters end with <c>_total</c>; units use <c>_seconds</c> / <c>_bytes</c>.
    /// </para>
    /// <para>
    /// The writer is allocation-aware but returns a single string. For very large sets, callers may reuse buffers or
    /// streams if needed in higher-level code.
    /// </para>
    /// <para>Thread safety: this type is stateless and thread-safe.</para>
    /// </remarks>
    public static class PrometheusWriter
    {
        // Escape label values per Prometheus text format spec
        private static string Esc(string? v)
        {
            if (string.IsNullOrEmpty(v)) return string.Empty;
            return v
                .Replace("\\", "\\\\")
                .Replace("\n", "\\n")
                .Replace("\r", "\\r")
                .Replace("\t", "\\t")
                .Replace("\"", "\\\"");
        }

        // standard labels on every line
        private static string L(StatsEnvelopeV1 e)
        {
            var instance = string.IsNullOrEmpty(e.Instance) ? "default" : e.Instance;
            return $"system=\"{Esc(e.System)}\",env=\"{Esc(e.Env)}\",instance=\"{Esc(instance)}\",host=\"{Esc(e.Host)}\",version=\"{Esc(e.Version)}\"";
        }

        /// <summary>
        /// Converts a sequence of envelopes into a Prometheus exposition payload.
        /// </summary>
        /// <param name="snapshots">The envelopes to render.</param>
        /// <returns>
        /// A complete text payload ready for scraping, including metric type lines and labelled metric samples.
        /// </returns>
        /// <remarks>
        /// <para>
        /// Known metrics: <c>system_health_status</c> (gauge) and <c>heartbeat_age_seconds</c> (gauge). Generic names are
        /// normalised when callers do not follow suffix rules: gauges become <c>generic_gauge_*</c>, counters become
        /// <c>generic_counter_*_total</c>.
        /// </para>
        /// </remarks>
        /// <example>
        /// <code language="csharp">
        /// var now = DateTime.UtcNow;
        /// var e1 = new TelemetryCore.Contracts.StatsEnvelopeV1
        /// {
        ///     System = "MessageBus",
        ///     Env = "DEV",
        ///     Instance = "P01",
        ///     Host = "example-host",
        ///     Version = "1.2.3",
        ///     Utc = now,
        ///     HealthStatus = 1.0
        /// };
        /// e1.Gauges["messagebus_queue_depth"] = 42;
        /// e1.Counters["messagebus_messages_total"] = 1000;
        /// var payload = PrometheusWriter.Write(new[] { e1 });
        /// </code>
        /// </example>
        public static string Write(IEnumerable<StatsEnvelopeV1> snapshots)
        {
            var sb = new StringBuilder();
            void typeHeader(string metric, string type) => sb.Append("# TYPE ").Append(metric).Append(' ').Append(type).Append('\n');
            void helpHeader(string metric, string help) => sb.Append("# HELP ").Append(metric).Append(' ').Append(help).Append('\n');
            void sample(string name, string labels, string val) => sb.Append(name).Append('{').Append(labels).Append("} ").Append(val).Append('\n');

            var now = DateTime.UtcNow;

            foreach (var e in snapshots)
            {
                var labels = L(e);

                // system_health_status
                helpHeader("system_health_status", "Normalised health status of the system (1=green,0.5=yellow,0=red).");
                typeHeader("system_health_status", "gauge");
                if (e.HealthStatus.HasValue)
                    sample("system_health_status", labels, e.HealthStatus.Value.ToString(System.Globalization.CultureInfo.InvariantCulture));

                // heartbeat_age_seconds derived from Utc
                helpHeader("heartbeat_age_seconds", "Age of the last heartbeat based on envelope Utc (seconds).");
                typeHeader("heartbeat_age_seconds", "gauge");
                var age = Math.Max(0, (now - e.Utc).TotalSeconds);
                sample("heartbeat_age_seconds", labels, age.ToString("0.###", System.Globalization.CultureInfo.InvariantCulture));

                // Generic gauges
                foreach (var kv in e.Gauges)
                {
                    var name = SanitizeGaugeName(kv.Key);
                    helpHeader(name, "Generic gauge from StatsEnvelopeV1");
                    typeHeader(name, "gauge");
                    sample(name, labels, kv.Value.ToString(System.Globalization.CultureInfo.InvariantCulture));
                }

                // Generic counters
                foreach (var kv in e.Counters)
                {
                    var name = SanitizeCounterName(kv.Key);
                    helpHeader(name, "Generic counter from StatsEnvelopeV1");
                    typeHeader(name, "counter");
                    sample(name, labels, kv.Value.ToString(System.Globalization.CultureInfo.InvariantCulture));
                }
            }
            return sb.ToString();
        }

        // Rewritten to C# 7.3-compatible if/else (instead of C# 8 switch expression)
        private static string SanitizeGaugeName(string name)
        {
            if (string.IsNullOrEmpty(name)) return "generic_gauge";
            if (name.EndsWith("_seconds") || name.EndsWith("_bytes"))
                return name;
            return $"generic_gauge_{name}";
        }

        private static string SanitizeCounterName(string name)
        {
            if (string.IsNullOrEmpty(name)) return "generic_counter_total";
            return name.EndsWith("_total") ? name : $"generic_counter_{name}_total";
        }
    }
}
