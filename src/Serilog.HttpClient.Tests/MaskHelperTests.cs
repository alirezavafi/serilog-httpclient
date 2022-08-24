using System.Collections.Immutable;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using Moq;
using Moq.Protected;
using Newtonsoft.Json.Linq;
using Serilog.Events;
using Serilog.HttpClient.Extensions;
using Serilog.HttpClient.Tests.Support;
using Serilog.Sinks.TestCorrelator;

namespace Serilog.HttpClient.Tests;

public class MaskHelperTests
{
    [Fact]
    public void Test_MaskFields()
    {
        var blacklist = new string[] { "*token*" };
        var mask = "*MASK*";

        "{\"token\": \"abc\"}".TryGetJToken(out JToken jToken);
        var result = jToken.MaskFields(blacklist, mask).ToString().Replace("\r\n", string.Empty).Replace(" ", string.Empty);
        Assert.Equal("{\"token\":\"*MASK*\"}", result);

        "[{\"token\": \"abc\"}]".TryGetJToken(out jToken);
        result = jToken.MaskFields(blacklist, mask).ToString().Replace("\r\n", string.Empty).Replace(" ", string.Empty);
        Assert.Equal("[{\"token\":\"*MASK*\"}]", result);

        "{\"nested\": {\"token\": \"abc\"}}".TryGetJToken(out jToken);
        result = jToken.MaskFields(blacklist, mask).ToString().Replace("\r\n", string.Empty).Replace(" ", string.Empty);
        Assert.Equal("{\"nested\":{\"token\":\"*MASK*\"}}", result);
    }
}
