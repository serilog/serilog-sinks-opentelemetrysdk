# Serilog.Sinks.OpenTelemetrySdk&nbsp;[![Build status](https://github.com/serilog/serilog-sinks-opentelemetrysdk/actions/workflows/ci.yml/badge.svg?branch=dev)](https://github.com/serilog/serilog-sinks-opentelemetrysdk/actions)&nbsp;[![NuGet Version](https://img.shields.io/nuget/vpre/Serilog.Sinks.OpenTelemetrySdk.svg?style=flat)](https://www.nuget.org/packages/Serilog.Sinks.OpenTelemetrySdk/)

This Serilog sink passes Serilog `LogEvents` to the OpenTelemetry SDK.

> [!IMPORTANT]
> This package relies on experimental OpenTelemetry features.
> It will only work with pre-release builds
> of the .NET OpenTelemetry SDK. Because there's no guarantee that these 
> experimental features will ever ship as stable APIs, there's also no guarantee
> that a stable version of this sink will ever be available.

## Getting started

To use this sink, you should already have a **pre-release** (`-beta` or `-rc`) version of the OpenTeleme~~~~try SDK installed
and configured.

Install the [NuGet package](https://nuget.org/packages/serilog.sinks.opentelemetrysdk):

```shell
dotnet add package Serilog.Sinks.OpenTelemetrySdk
```

Then enable the sink using `WriteTo.OpenTelemetrySdk()`, passing your application's `IServiceProvider`:

```csharp
// var app = ...

Log.Logger = new LoggerConfiguration()
    .WriteTo.OpenTelemetrySdk(app.Services)
    .CreateLogger();
```

Logs written with `Log.Information(...)` and similar methods will be output by the configured OpenTelemetry exporters.

## Serilog `LogEvent` to OpenTelemetry log record mapping

The following table provides the mapping between the Serilog log 
events and the OpenTelemetry log records. 

Serilog `LogEvent`               | OpenTelemetry `LogRecord`                  | Comments                                                                                      |
---------------------------------|--------------------------------------------|-----------------------------------------------------------------------------------------------| 
`Exception.GetType().ToString()` | `Attributes["exception.type"]`             |                                                                                               |
`Exception.Message`              | `Attributes["exception.message"]`          | Ignored if empty                                                                              |
`Exception.StackTrace`           | `Attributes[ "exception.stacktrace"]`      | Value of `ex.ToString()`                                                                      |
`Level`                          | `LogRecordSeverity`                           | Serilog levels are mapped to corresponding OpenTelemetry severities                           | 
`Level.ToString()`               | `SeverityText`                             |                                                                                               |
`Message`                        | `Body`                                     | Culture-specific formatting can be provided via sink configuration                            |
`MessageTemplate`                | `Attributes[ "message_template.text"]`     | Requires `IncludedData. MessageTemplateText` (enabled by default)                             |
`MessageTemplate` (MD5)          | `Attributes[ "message_template.hash.md5"]` | Requires `IncludedData. MessageTemplateMD5 HashAttribute`                                     |
`Properties`                     | `Attributes`                               | Each property is mapped to an attribute keeping the name; the value's structure is maintained |
`SpanId` (`Activity.Current`)    | `SpanId`                                   | Requires `IncludedData.SpanIdField` (enabled by default)                                           |
`Timestamp`                      | `TimeUnixNano`                             | .NET provides 100-nanosecond precision                                                        |
`TraceId` (`Activity.Current`)   | `TraceId`                                  | Requires `IncludedData.TraceIdField` (enabled by default)                                          |

Serialized data is passed using the `Dictionary<string, object?>` type. Logged arrays are passed as `List<object?>`.

### Configuring included data

This sink supports configuration of how common OpenTelemetry fields are populated from
the Serilog `LogEvent` by passing `includedData`:

```csharp
Log.Logger = new LoggerConfiguration()
    .WriteTo.OpenTelemetrySdk(app.Services, includedData: IncludedData.MessageTemplateTextAttribute)
    .CreateLogger();
```

## Acknowledgements

This sink is a fork of [`Serilog.Sinks.OpenTelemetry`](https://github.com/serilog/serilog-sinks-opentelemetry), which
sends log events using the OpenTelemetry wire protocol, and does not have a dependency on the OpenTelemetry SDK.

_Copyright &copy; Serilog Contributors - Provided under the [Apache License, Version 2.0](http://apache.org/licenses/LICENSE-2.0.html)._
