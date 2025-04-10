// Copyright © Serilog Contributors
//
// Licensed under the Apache License, Version 2.0 (the "License");
// you may not use this file except in compliance with the License.
// You may obtain a copy of the License at
//
//     http://www.apache.org/licenses/LICENSE-2.0
//
// Unless required by applicable law or agreed to in writing, software
// distributed under the License is distributed on an "AS IS" BASIS,
// WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
// See the License for the specific language governing permissions and
// limitations under the License.

// ReSharper disable once RedundantUsingDirective
using System.Net.Http;
using Microsoft.Extensions.DependencyInjection;
using OpenTelemetry.Logs;
using Serilog.Configuration;
using Serilog.Core;
using Serilog.Events;
using Serilog.Sinks.OpenTelemetrySdk;

// ReSharper disable MemberCanBePrivate.Global

namespace Serilog;

/// <summary>
/// Adds OpenTelemetry SDK sink configuration methods to <see cref="LoggerSinkConfiguration"/>.
/// </summary>
public static class OpenTelemetryLoggerConfigurationExtensions
{
    internal const IncludedData DefaultIncludedData = IncludedData.MessageTemplateTextAttribute |
                                                      IncludedData.StructureValueTypeTags |
                                                      IncludedData.TraceIdField | IncludedData.SpanIdField;

    /// <summary>
    /// Write log events to the OpenTelemetry SDK.
    /// </summary>
    /// <param name="loggerSinkConfiguration">
    /// The <c>WriteTo</c> configuration object.
    /// </param>
    /// <param name="services">An <see cref="IServiceProvider"/> that includes services registered
    /// by the OpenTelemetry SDK.</param>
    /// <param name="includedData">A set of flags specifying how log event data should be converted
    /// for the OpenTelemetry SDK.</param>
    /// <param name="formatProvider">Supplies culture-specific formatting; or <c langword="null"/>.</param>
    /// <param name="restrictedToMinimumLevel">
    /// The minimum level for events passed through the sink. Ignored when <paramref name="levelSwitch"/> is specified.
    /// </param>
    /// <param name="levelSwitch">
    /// A switch allowing the pass-through minimum level to be changed at runtime.
    /// </param>
    /// <returns>Logger configuration, allowing configuration to continue.</returns>
    public static LoggerConfiguration OpenTelemetrySdk(
        this LoggerSinkConfiguration loggerSinkConfiguration,
        IServiceProvider services,
        IncludedData includedData = DefaultIncludedData,
        IFormatProvider? formatProvider = null,
        LogEventLevel restrictedToMinimumLevel = LevelAlias.Minimum,
        LoggingLevelSwitch? levelSwitch = null)
    {
        var loggerProvider = services.GetRequiredService<LoggerProvider>();
        return loggerSinkConfiguration.Sink(
            new OpenTelemetryLoggerProviderSink(loggerProvider, formatProvider, includedData),
            restrictedToMinimumLevel, levelSwitch);
    }
}
