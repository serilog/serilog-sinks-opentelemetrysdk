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

using System.Security.Cryptography;
using System.Text;
using OpenTelemetry.Logs;
using Serilog.Events;

namespace Serilog.Sinks.OpenTelemetrySdk;

static class Conversions
{
    public static LogRecordSeverity ToLogRecordSeverity(LogEventLevel level)
    {
        return level switch
        {
            LogEventLevel.Verbose => LogRecordSeverity.Trace,
            LogEventLevel.Debug => LogRecordSeverity.Debug,
            LogEventLevel.Information => LogRecordSeverity.Info,
            LogEventLevel.Warning => LogRecordSeverity.Warn,
            LogEventLevel.Error => LogRecordSeverity.Error,
            LogEventLevel.Fatal => LogRecordSeverity.Fatal,
            _ => LogRecordSeverity.Unspecified
        };
    }
    
    public static Dictionary<string, object?> ToOpenTelemetryMap(StructureValue value, IncludedData includedData)
    {
        var map = new Dictionary<string, object?>();

        if ((includedData & IncludedData.StructureValueTypeTags) == IncludedData.StructureValueTypeTags && !string.IsNullOrEmpty(value.TypeTag))
        {
            map["$type"] = value.TypeTag;
        }

        foreach (var prop in value.Properties)
        {
            var v = ToOpenTelemetryValue(prop.Value, includedData);
            
#if FEATURE_DICTIONARY_TRY_ADD
            map.TryAdd(prop.Name, v);
#else
            if (!map.ContainsKey(prop.Name))
            {
                map.Add(prop.Name, v);
            }
#endif
        }

        return map;
    }

    public static Dictionary<string, object?> ToOpenTelemetryMap(DictionaryValue value, IncludedData includedData)
    {
        var map = new Dictionary<string, object?>();

        foreach (var element in value.Elements)
        {
            var k = element.Key.Value?.ToString() ?? "null";
            var v = ToOpenTelemetryValue(element.Value, includedData);
            
#if FEATURE_DICTIONARY_TRY_ADD
            map.TryAdd(k, v);
#else
            if (!map.ContainsKey(k))
            {
                map.Add(k, v);
            }
#endif
        }

        return map;
    }

    public static List<object?> ToOpenTelemetryArray(SequenceValue value, IncludedData includedData)
    {
        var array = new List<object?>(value.Elements.Count);
        foreach (var element in value.Elements)
        {
            array.Add(ToOpenTelemetryValue(element, includedData));
        }
        return array;
    }

    public static object? ToOpenTelemetryValue(LogEventPropertyValue value, IncludedData includedData)
    {
        return value switch
        {
            ScalarValue scalar => scalar.Value,
            StructureValue structure => ToOpenTelemetryMap(structure, includedData),
            SequenceValue sequence => ToOpenTelemetryArray(sequence, includedData),
            DictionaryValue dictionary => ToOpenTelemetryMap(dictionary, includedData),
            _ => value,
        };
    }
    
    public static string Md5Hash(string s)
    {
        using var md5 = MD5.Create();
        md5.Initialize();
        var hash = md5.ComputeHash(Encoding.UTF8.GetBytes(s));
        return string.Join(string.Empty, Array.ConvertAll(hash, x => x.ToString("x2")));
    }
}
