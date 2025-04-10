// Copyright 2022 Serilog Contributors
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

using OpenTelemetry.Logs;
using Serilog.Events;
using Serilog.Sinks.OpenTelemetrySdk.Tests.Support;
using Xunit;

namespace Serilog.Sinks.OpenTelemetrySdk.Tests;

public class OpenTelemetryLogRecordBuilderTests
{
    [Fact]
    public void TestProcessMessage()
    {
        var logRecord = new LogRecordData();

        OpenTelemetryLogRecordBuilder.ProcessBody(ref logRecord, Some.SerilogEvent(messageTemplate: ""), OpenTelemetryLoggerConfigurationExtensions.DefaultIncludedData, null);
        Assert.Null(logRecord.Body);

        OpenTelemetryLogRecordBuilder.ProcessBody(ref logRecord, Some.SerilogEvent(messageTemplate: "\t\f "), OpenTelemetryLoggerConfigurationExtensions.DefaultIncludedData, null);
        Assert.Null(logRecord.Body);

        const string message = "log message";
        OpenTelemetryLogRecordBuilder.ProcessBody(ref logRecord, Some.SerilogEvent(messageTemplate: message), OpenTelemetryLoggerConfigurationExtensions.DefaultIncludedData, null);
        Assert.NotNull(logRecord.Body);
        Assert.Equal(message, logRecord.Body);
    }

    [Fact]
    public void TestProcessLevel()
    {
        var logRecord = new LogRecordData();
        var logEvent = Some.DefaultSerilogEvent();

        OpenTelemetryLogRecordBuilder.ProcessLevel(ref logRecord, logEvent);

        Assert.Equal(LogEventLevel.Warning.ToString(), logRecord.SeverityText);
        Assert.Equal(LogRecordSeverity.Warn, logRecord.Severity);
    }
    
    [Fact]
    public void SourceContextIsInstrumentationScope()
    {
        var contextType = typeof(OpenTelemetryLogRecordBuilderTests);
        var logEvent = CollectingSink.CollectSingle(log => log.ForContext(contextType).Information("Hello, world!"));
        
        var (_, logRecordAttributes, scopeName) = OpenTelemetryLogRecordBuilder.ToLogRecord(logEvent, null, OpenTelemetryLoggerConfigurationExtensions.DefaultIncludedData);
        
        Assert.Equal(contextType.FullName, scopeName);
        Assert.DoesNotContain(Core.Constants.SourceContextPropertyName, logRecordAttributes.Select(a => a.Key));        
    }

    [Fact]
    public void SourceContextCanBePreservedAsAttribute()
    {
        var contextType = typeof(OpenTelemetryLogRecordBuilderTests);
        var logEvent = CollectingSink.CollectSingle(log => log.ForContext(contextType).Information("Hello, world!"));
        
        var (_, logRecordAttributes, scopeName) = OpenTelemetryLogRecordBuilder.ToLogRecord(logEvent, null, OpenTelemetryLoggerConfigurationExtensions.DefaultIncludedData | IncludedData.SourceContextAttribute);
        
        Assert.Equal(contextType.FullName, scopeName);
        var ctx = Assert.Single(logRecordAttributes, a => a.Key == Core.Constants.SourceContextPropertyName);
        Assert.Equal(contextType.FullName, ctx.Value);
    }
}
