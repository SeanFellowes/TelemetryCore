using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using System.Text;
using TelemetryCore.Contracts;

var builder = WebApplication.CreateBuilder(args);

builder.Logging.ClearProviders();
builder.Logging.AddConsole();

var app = builder.Build();

app.Urls.Add("http://0.0.0.0:9000");

// Simple health endpoint
app.MapGet("/healthz", () => Results.Ok("OK"));

// Demo metrics: serialize a sample StatsEnvelopeV1 and expose as Prometheus text.
// In a real app, you would compose metrics over time and counters would be monotonic.
app.MapGet("/metrics", () =>
{
    var now = DateTime.UtcNow;
    var e = new StatsEnvelopeV1
    {
        System = "DemoSystem",
        Env = "DEV",
        Instance = "P01",
        Host = Environment.MachineName,
        Version = typeof(StatsEnvelopeV1).Assembly.GetName().Version?.ToString() ?? "1.0.0",
        Utc = now,
        HealthStatus = 1.0
    };

    e.Gauges["demo_queue_depth"] = 3;
    e.Counters["demo_messages_total"] = 42;
    e.Tags["region"] = "local";

    // Emit Prometheus text exposition format 0.0.4
    var sb = new StringBuilder();

    // Common labels used on all metrics
    var labels = Labels(e);

    // Health metric
    sb.AppendLine("# HELP system_health_status Normalised health status of the system (1=green,0.5=yellow,0=red).");
    sb.AppendLine("# TYPE system_health_status gauge");
    sb.AppendLine($"system_health_status{labels} {e.HealthStatus?.ToString(System.Globalization.CultureInfo.InvariantCulture) ?? "NaN"}");

    // Heartbeat age metric derived from envelope Utc
    if (e.Utc != default)
    {
        var age = Math.Max(0, (now - e.Utc).TotalSeconds);
        sb.AppendLine("# HELP heartbeat_age_seconds Age of the last heartbeat based on envelope Utc (seconds).");
        sb.AppendLine("# TYPE heartbeat_age_seconds gauge");
        sb.AppendLine($"heartbeat_age_seconds{labels} {age.ToString("0.###", System.Globalization.CultureInfo.InvariantCulture)}");
    }

    // Generic gauges with HELP/TYPE
    foreach (var kv in e.Gauges)
    {
        var name = Sanitize(kv.Key);
        sb.AppendLine($"# HELP {name} Generic gauge from StatsEnvelopeV1");
        sb.AppendLine($"# TYPE {name} gauge");
        sb.AppendLine($"{name}{labels} {kv.Value.ToString(System.Globalization.CultureInfo.InvariantCulture)}");
    }

    // Generic counters with HELP/TYPE (should end with _total)
    foreach (var kv in e.Counters)
    {
        var name = Sanitize(kv.Key);
        sb.AppendLine($"# HELP {name} Generic counter from StatsEnvelopeV1");
        sb.AppendLine($"# TYPE {name} counter");
        sb.AppendLine($"{name}{labels} {kv.Value.ToString(System.Globalization.CultureInfo.InvariantCulture)}");
    }

    return Results.Text(sb.ToString(), "text/plain; version=0.0.4; charset=utf-8");
});

app.MapGet("/", () => Results.Text("TelemetryCore DemoWeb. Endpoints: /healthz, /metrics\n", "text/plain"));

app.Run();

static string Sanitize(string name)
{
    // Prometheus metric name allowed charset: [a-zA-Z_:][a-zA-Z0-9_:]*
    if (string.IsNullOrEmpty(name)) return "generic_metric";
    var sb = new StringBuilder(name.Length);
    for (int i = 0; i < name.Length; i++)
    {
        var c = name[i];
        if ((i == 0 && (char.IsLetter(c) || c == '_' || c == ':')) ||
            (i > 0 && (char.IsLetterOrDigit(c) || c == '_' || c == ':')))
        {
            sb.Append(c);
        }
        else
        {
            sb.Append('_');
        }
    }
    return sb.ToString();
}

static string Labels(StatsEnvelopeV1 e)
{
    // Escape label values per Prometheus text format spec: \\ , \" , \n (also handle \r and \t conservatively)
    static string Esc(string? v)
    {
        if (string.IsNullOrEmpty(v)) return string.Empty;
        return v
            .Replace("\\", "\\\\")
            .Replace("\n", "\\n")
            .Replace("\r", "\\r")
            .Replace("\t", "\\t")
            .Replace("\"", "\\\"");
    }

    var instance = string.IsNullOrEmpty(e.Instance) ? "default" : e.Instance;

    var sb = new StringBuilder();
    sb.Append('{');
    sb.Append($"system=\"{Esc(e.System)}\"");
    sb.Append(','); sb.Append($"env=\"{Esc(e.Env)}\"");
    sb.Append(','); sb.Append($"instance=\"{Esc(instance)}\"");
    sb.Append(','); sb.Append($"host=\"{Esc(e.Host)}\"");
    sb.Append(','); sb.Append($"version=\"{Esc(e.Version)}\"");
    sb.Append('}');
    return sb.ToString();
}
