# TelemetryCore Release Notes

## 1.0.0 - 2025-09-17

- Initial public release of TelemetryCore.Contracts.
- Provides shared telemetry envelope with gauges, counters, and health status fields.
- Includes Prometheus exposition writer and JSON serialization facade (System.Text.Json on modern TFMs; Newtonsoft.Json for legacy).
- Ships demo ASP.NET Core app exposing `/healthz` and `/metrics` endpoints.
