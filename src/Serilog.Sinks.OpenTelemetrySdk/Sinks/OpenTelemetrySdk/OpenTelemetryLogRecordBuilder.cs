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

using System.Globalization;
using OpenTelemetry.Logs;
using Serilog.Events;
using Serilog.Parsing;
using Serilog.Sinks.OpenTelemetrySdk.Formatting;
// ReSharper disable MemberCanBePrivate.Global
// ReSharper disable PossibleMultipleEnumeration

namespace Serilog.Sinks.OpenTelemetrySdk;

static class OpenTelemetryLogRecordBuilder
{
    public static (LogRecordData logRecord, LogRecordAttributeList attributes, string? scopeName) ToLogRecord(LogEvent logEvent, IFormatProvider? formatProvider, IncludedData includedData)
    {
        var logRecord = new LogRecordData();
        var logRecordAttributes = new LogRecordAttributeList();

        ProcessProperties(ref logRecordAttributes, logEvent, includedData, out var scopeName);
        ProcessTimestamp(ref logRecord, logEvent);
        ProcessBody(ref logRecord, logEvent, includedData, formatProvider);
        ProcessLevel(ref logRecord, logEvent);
        ProcessException(ref logRecordAttributes, logEvent);
        ProcessIncludedFields(ref logRecord, ref logRecordAttributes, logEvent, includedData);

        return (logRecord, logRecordAttributes, scopeName);
    }

    public static void ProcessBody(ref LogRecordData logRecord, LogEvent logEvent, IncludedData includedFields, IFormatProvider? formatProvider)
    {
        if (!includedFields.HasFlag(IncludedData.TemplateBody))
        {
            var renderedMessage = CleanMessageTemplateFormatter.Format(logEvent.MessageTemplate, logEvent.Properties, formatProvider);

            if (!string.IsNullOrWhiteSpace(renderedMessage))
            {
                logRecord.Body = renderedMessage;
            }
        }
        else if (!string.IsNullOrWhiteSpace(logEvent.MessageTemplate.Text))
        {
            logRecord.Body = logEvent.MessageTemplate.Text;
        }
    }

    public static void ProcessLevel(ref LogRecordData logRecord, LogEvent logEvent)
    {
        var level = logEvent.Level;
        logRecord.Severity = Conversions.ToLogRecordSeverity(level);
        logRecord.SeverityText = level.ToString();
    }

    public static void ProcessProperties(ref LogRecordAttributeList logRecordAttributes, LogEvent logEvent, IncludedData includedData, out string? scopeName)
    {
        scopeName = null;
        foreach (var property in logEvent.Properties)
        {
            if (property is { Key: Core.Constants.SourceContextPropertyName, Value: ScalarValue { Value: string sourceContext } })
            {
                scopeName = sourceContext;
                if ((includedData & IncludedData.SourceContextAttribute) != IncludedData.SourceContextAttribute)
                {
                    continue;
                }
            }

            var v = Conversions.ToOpenTelemetryValue(property.Value, includedData);
            logRecordAttributes.Add(property.Key, v);
        }
    }

    public static void ProcessTimestamp(ref LogRecordData logRecord, LogEvent logEvent)
    {
        logRecord.Timestamp = logEvent.Timestamp.UtcDateTime;
    }
    
    public static void ProcessException(ref LogRecordAttributeList attrs, LogEvent logEvent)
    {
        var ex = logEvent.Exception;
        if (ex != null)
        {
            attrs.Add(SemanticConventions.AttributeExceptionType, ex.GetType().ToString());

            if (ex.Message != "")
            {
                attrs.Add(SemanticConventions.AttributeExceptionMessage, ex.Message);
            }

            if (ex.ToString() != "")
            {
                attrs.Add(SemanticConventions.AttributeExceptionStacktrace, ex.ToString());
            }
        }
    }

    static void ProcessIncludedFields(ref LogRecordData logRecord, ref LogRecordAttributeList logRecordAttributes, LogEvent logEvent, IncludedData includedFields)
    {
        if ((includedFields & IncludedData.TraceIdField) != IncludedData.None && logEvent.TraceId is { } traceId)
        {
            logRecord.TraceId = traceId;
        }

        if ((includedFields & IncludedData.SpanIdField) != IncludedData.None && logEvent.SpanId is { } spanId)
        {
            logRecord.SpanId = spanId;
        }

        if ((includedFields & IncludedData.MessageTemplateTextAttribute) != IncludedData.None)
        {
            logRecordAttributes.Add(SemanticConventions.AttributeMessageTemplateText, logEvent.MessageTemplate.Text);
        }

        if ((includedFields & IncludedData.MessageTemplateMD5HashAttribute) != IncludedData.None)
        {
            logRecordAttributes.Add(SemanticConventions.AttributeMessageTemplateMD5Hash,
                Conversions.Md5Hash(logEvent.MessageTemplate.Text));
        }

        if ((includedFields & IncludedData.MessageTemplateRenderingsAttribute) != IncludedData.None)
        {
            var tokensWithFormat = logEvent.MessageTemplate.Tokens
                .OfType<PropertyToken>()
                .Where(pt => pt.Format != null);

            // Better not to allocate an array in the 99.9% of cases where this is false
            if (tokensWithFormat.Any())
            {
                var renderings = new List<object?>();

                foreach (var propertyToken in tokensWithFormat)
                {
                    var space = new StringWriter();
                    propertyToken.Render(logEvent.Properties, space, CultureInfo.InvariantCulture);
                    renderings.Add(space.ToString());
                }

                logRecordAttributes.Add(
                    SemanticConventions.AttributeMessageTemplateRenderings,
                    renderings);
            }
        }
    }
}
