# TelemetryCore.Contracts 📡📊

Contracts and helpers for emitting consistent monitoring telemetry from systems to Prometheus/Grafana.

- 📦 NuGet-friendly multi-target package
- ✉️ Consistent envelope: identity + health + flexible Gauges/Counters/Tags
- 🧾 JSON facade: System.Text.Json on modern TFMs, Newtonsoft.Json for legacy
- 🖥️ Demo web app included (net8) exposing /healthz and /metrics

## What is this?

A tiny, transport-agnostic contract and helpers so multiple bespoke monitors emit the same telemetry shape:

- StatsEnvelopeV1: identity + health + flexible Gauges/Counters/Tags
- StatsSerializer: JSON facade (net6/net8: System.Text.Json; net472/netstandard2.0: Newtonsoft.Json)
- PrometheusWriter: renders envelopes to Prometheus text exposition format

Targets: net472, netstandard2.0, net6.0, net8.0.

## Quick start 🚀

```csharp
var now = DateTime.UtcNow;
var e = new TelemetryCore.Contracts.StatsEnvelopeV1
{
    System = "MessageBus",
    Env = "DEV",
    Instance = "P01",
    Host = Environment.MachineName,
    Version = "1.2.3",
    Utc = now,
    HealthStatus = 1.0
};

e.Gauges["messagebus_queue_depth"] = 42;
e.Counters["messagebus_messages_total"] = 1000;

// JSON
var json = TelemetryCore.Contracts.StatsSerializer.ToJson(e);

// Prometheus text (single or multiple envelopes)
var text = TelemetryCore.Contracts.PrometheusWriter.Write(new[] { e });
```

## Prometheus exposition 📈

- Content-Type: `text/plain; version=0.0.4; charset=utf-8`
- Labels on every sample: `system`, `env`, `instance` (empty ➡️ `default`), `host`, `version`
- Known metrics: `system_health_status` (gauge), `heartbeat_age_seconds` (gauge, derived from `Utc`)
- Naming rules: counters end with `_total` (use `rate()` / `increase()`), units use `_seconds`, `_bytes`
- Generic names are prefixed if you don't follow rules: gauges ➡️ `generic_gauge_*`, counters ➡️ `generic_counter_*_total`
- Metric/label values are escaped per Prometheus text format spec

### Minimal HTTP exposure examples 🌐

- net472: host with `HttpListener` and write `PrometheusWriter.Write(...)` to `/metrics`
- net6/8: host with Kestrel (Minimal API) and return text with the content type above

A ready-to-run demo site is included:
- Project: `TelemetryCore.DemoWeb` (net8.0)
- Endpoints: `/healthz` and `/metrics` on port 9000

## NuGet package 📦

- Multi-targets: net472, netstandard2.0, net6.0, net8.0
- Serializer: Newtonsoft.Json for net472/netstandard2.0; System.Text.Json for net6/net8
- XML docs included for IntelliSense
- Package readme, license, and icon are included

## Versioning and compatibility 🔄

- Schema version is additive (see `SchemaVersion` in `StatsEnvelopeV1`)
- Consumers should tolerate unknown fields for forward compatibility

## Contributing 🤝

Issues and PRs are welcome. Keep the envelope stable and additive. For new metrics:
- Prefer snake_case
- Units in names (`_seconds`, `_bytes`)
- Counters end with `_total`

## License ⚖️

MIT License ➡️ see LICENSE in the package/repo.

## Attribution 🙌

- Author: Sean Fellowes (https://github.com/SeanFellowes)
- Assisted by: GPT-5 via GitHub Copilot
