using System.Collections.Immutable;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using Moq;
using Moq.Protected;
using Serilog.Events;
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

        var result = "{\"token\": \"abc\"}".MaskFields(blacklist, mask);
        Assert.Equal("{\"token\":\"*MASK*\"}", result);

        result = "[{\"token\": \"abc\"}]".MaskFields(blacklist, mask);
        Assert.Equal("[{\"token\":\"*MASK*\"}]", result);

        result = "{\"nested\": {\"token\": \"abc\"}}".MaskFields(blacklist, mask);
        Assert.Equal("{\"nested\":{\"token\":\"*MASK*\"}}", result);
    }
}
