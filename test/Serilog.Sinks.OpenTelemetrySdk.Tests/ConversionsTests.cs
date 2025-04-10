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

using System.Text.RegularExpressions;
using OpenTelemetry.Logs;
using Serilog.Events;
using Xunit;

namespace Serilog.Sinks.OpenTelemetrySdk.Tests;

public class ConversionsTests
{
    [Theory]
    [InlineData(LogEventLevel.Verbose, LogRecordSeverity.Trace)]
    [InlineData(LogEventLevel.Debug, LogRecordSeverity.Debug)]
    [InlineData(LogEventLevel.Information, LogRecordSeverity.Info)]
    [InlineData(LogEventLevel.Warning, LogRecordSeverity.Warn)]
    [InlineData(LogEventLevel.Error, LogRecordSeverity.Error)]
    [InlineData(LogEventLevel.Fatal, LogRecordSeverity.Fatal)]
    public void TestToLogRecordSeverity(LogEventLevel level, Enum expectedLogRecordSeverity)
    {
        Assert.Equal((LogRecordSeverity)expectedLogRecordSeverity, Conversions.ToLogRecordSeverity(level));
    }
    
    [Fact]
    public void TestToOpenTelemetryMap()
    {
        var input = new StructureValue(
        [
            new("a", new ScalarValue(1)),
            new("b", new ScalarValue("2")),
            new("c", new ScalarValue(true))
        ], "Test");

        // direct conversion
        AssertEquivalentToInput(Conversions.ToOpenTelemetryMap(input, IncludedData.StructureValueTypeTags));

        // indirect conversion
        AssertEquivalentToInput(Conversions.ToOpenTelemetryValue(input, IncludedData.StructureValueTypeTags));

        // no type tag
        AssertEquivalentToInput(Conversions.ToOpenTelemetryMap(input, IncludedData.None), noTypeTag: true);
        
        return;
        
        static void AssertEquivalentToInput(object? result, bool noTypeTag = false)
        {
            var dictionary = Assert.IsType<Dictionary<string, object?>>(result);
            var values = new Queue<KeyValuePair<string, object?>>(dictionary);
            Assert.Equal(noTypeTag ? 3 : 4, values.Count);

            if (!noTypeTag)
            {
                var type = values.Dequeue();
                Assert.Equal("$type", type.Key);
                Assert.Equal("Test", type.Value);
            }

            var a = values.Dequeue();
            Assert.Equal("a", a.Key);
            Assert.Equal(1, a.Value);

            var b = values.Dequeue();
            Assert.Equal("b", b.Key);
            Assert.Equal("2", b.Value);

            var c = values.Dequeue();
            Assert.Equal("c", c.Key);
            Assert.True(Assert.IsType<bool>(c.Value));
        }
    }

    [Fact]
    public void TestToOpenTelemetryArray()
    {
        List<LogEventPropertyValue> elements =
        [
            new ScalarValue(1),
            new ScalarValue("2"),
            new ScalarValue(false)
        ];

        var input = new SequenceValue(elements);

        var result = Conversions.ToOpenTelemetryArray(input, IncludedData.None);
        Assert.NotNull(result);
        var arrayValue = Assert.IsType<List<object?>>(result);
        Assert.Equal(3, arrayValue.Count);
        var secondElement = arrayValue.ElementAt(1);
        Assert.Equal("2", secondElement);
    }

    [Theory]
    [InlineData("")]
    [InlineData("first string")]
    [InlineData("second string")]
    public void MD5RegexMatchesMD5Chars(string input)
    {
        var md5Regex = new Regex(@"^[a-f\d]{32}$");
        Assert.Matches(md5Regex, Conversions.Md5Hash(input));
    }


    [Fact]
    public void MD5HashIsComparable()
    {
        Assert.Equal(Conversions.Md5Hash("alpha"), Conversions.Md5Hash("alpha"));
        Assert.NotEqual(Conversions.Md5Hash("alpha"), Conversions.Md5Hash("beta"));
    }
}
