# Serilog.HttpClient 
A logging handler for HttpClient:

- Data masking for sensitive information
- Captures request/response body based on response status and configuration
- Captures request/response header based on response status and configuration
- Request/response body size truncation for preventing performance penalties
- Log levels based on response status code (Warning for status >= 400, Error for status >= 500)

### Instructions

**First**, install the _Serilog.HttpClient_ [NuGet package](https://www.nuget.org/packages/Serilog.HttpClient) into your app.

```shell
dotnet add package Serilog.HttpClient
```

Add Json destructing policies using AddJsonDestructuringPolicies() when configuring LoggerConfiguration like below:

```csharp
Serilog.Log.Logger = new LoggerConfiguration()
    .WriteTo.File(new JsonFormatter(),"log.json")
    .WriteTo.Console(outputTemplate:
        "[{Timestamp:HH:mm:ss} {Level:u3}] {Message} {NewLine}{Properties} {NewLine}{Exception}{NewLine}",
        theme: SystemConsoleTheme.Literate)
    .AddJsonDestructuringPolicies()
    .CreateLogger();
```

In your application's _Startup.cs_, add the middleware with `UseSerilogPlus()`:

```csharp
        public void ConfigureServices(IServiceCollection services)
        {
            // ...

            services
                .AddHttpClient<IMyService, MyService>()
                .LogRequestResponse();

            // or
                        
            services
                .AddHttpClient<IMyService, MyService>()
                .LogRequestResponse(p =>
                {
                    p.LogMode = LogMode.LogAll;
                    p.RequestHeaderLogMode = LogMode.LogAll;
                    p.RequestBodyLogMode = LogMode.LogAll;
                    p.RequestBodyLogTextLengthLimit = 5000;
                    p.ResponseHeaderLogMode = LogMode.LogFailures;
                    p.ResponseBodyLogMode = LogMode.LogFailures;
                    p.ResponseBodyLogTextLengthLimit = 5000;
                    p.MaskFormat = "*****"; 
                    p.MaskedProperties.Clear();
                    p.MaskedProperties.Add("*password*");
                    p.MaskedProperties.Add("*token*");
                });
        }
 ``

### Sample Logged Item
    
    ```json
    {
        "Timestamp": "2022-08-24T14:51:58.5829167+04:30",
        "Level": "Information",
        "MessageTemplate": "HTTP Client {RequestMethod} {RequestPath} responded {StatusCode} in {Elapsed:0.0000} ms",
        "Properties": {
            "RequestMethod": "GET",
            "RequestPath": "/api/users",
            "StatusCode": 200,
            "Elapsed": 803.8034,
            "__4": "Serilog.HttpClient.Models.HttpClientContext",
            "Context": {
                "_typeTag": "HttpClientContext",
                "Request": {
                    "_typeTag": "HttpRequestInfo",
                    "Method": "GET",
                    "Scheme": "https",
                    "Host": "reqres.in",
                    "Path": "/api/users",
                    "QueryString": "?page=2",
                    "Query": {
                        "page": "2"
                    },
                    "BodyString": "",
                    "Body": null,
                    "Headers": {}
                },
                "Response": {
                    "_typeTag": "HttpResponseInfo",
                    "StatusCode": 200,
                    "IsSucceed": true,
                    "ElapsedMilliseconds": 803.8034,
                    "BodyString": "{\r\n  \"page\": 2,\r\n  \"per_page\": 6,\r\n  \"total\": 12,\r\n  \"total_pages\": 2,\r\n  \"data\": [\r\n    {\r\n      \"id\": 7,\r\n      \"email\": \"michael.lawson@reqres.in\",\r\n      \"first_name\": \"Michael\",\r\n      \"last_name\": \"Lawson\",\r\n      \"avatar\": \"https://reqres.in/img/faces/7-image.jpg\"\r\n    },\r\n    {\r\n      \"id\": 8,\r\n      \"email\": \"lindsay.ferguson@reqres.in\",\r\n      \"first_name\": \"Lindsay\",\r\n      \"last_name\": \"Ferguson\",\r\n      \"avatar\": \"https://reqres.in/img/faces/8-image.jpg\"\r\n    },\r\n    {\r\n      \"id\": 9,\r\n      \"email\": \"tobias.funke@reqres.in\",\r\n      \"first_name\": \"Tobias\",\r\n      \"last_name\": \"Funke\",\r\n      \"avatar\": \"https://reqres.in/img/faces/9-image.jpg\"\r\n    },\r\n    {\r\n      \"id\": 10,\r\n      \"email\": \"byron.fields@reqres.in\",\r\n      \"first_name\": \"Byron\",\r\n      \"last_name\": \"Fields\",\r\n      \"avatar\": \"https://reqres.in/img/faces/10-image.jpg\"\r\n    },\r\n    {\r\n      \"id\": 11,\r\n      \"email\": \"george.edwards@reqres.in\",\r\n      \"first_name\": \"George\",\r\n      \"last_name\": \"Edwards\",\r\n      \"avatar\": \"https://reqres.in/img/faces/11-image.jpg\"\r\n    },\r\n    {\r\n      \"id\": 12,\r\n      \"email\": \"rachel.howell@reqres.in\",\r\n      \"first_name\": \"Rachel\",\r\n      \"last_name\": \"Howell\",\r\n      \"avatar\": \"https://reqres.in/img/faces/12-image.jpg\"\r\n    }\r\n  ],\r\n  \"support\": {\r\n    \"url\": \"https://reqres.in/#support-heading\",\r\n    \"text\": \"To keep ReqRes free, contributions towards server costs are appreciated!\"\r\n  }\r\n}",
                    "Body": {
                        "page": 2,
                        "per_page": 6,
                        "total": 12,
                        "total_pages": 2,
                        "data": [
                            {
                                "id": 7,
                                "email": "michael.lawson@reqres.in",
                                "first_name": "Michael",
                                "last_name": "Lawson",
                                "avatar": "https://reqres.in/img/faces/7-image.jpg"
                            },
                            {
                                "id": 8,
                                "email": "lindsay.ferguson@reqres.in",
                                "first_name": "Lindsay",
                                "last_name": "Ferguson",
                                "avatar": "https://reqres.in/img/faces/8-image.jpg"
                            },
                            {
                                "id": 9,
                                "email": "tobias.funke@reqres.in",
                                "first_name": "Tobias",
                                "last_name": "Funke",
                                "avatar": "https://reqres.in/img/faces/9-image.jpg"
                            },
                            {
                                "id": 10,
                                "email": "byron.fields@reqres.in",
                                "first_name": "Byron",
                                "last_name": "Fields",
                                "avatar": "https://reqres.in/img/faces/10-image.jpg"
                            },
                            {
                                "id": 11,
                                "email": "george.edwards@reqres.in",
                                "first_name": "George",
                                "last_name": "Edwards",
                                "avatar": "https://reqres.in/img/faces/11-image.jpg"
                            },
                            {
                                "id": 12,
                                "email": "rachel.howell@reqres.in",
                                "first_name": "Rachel",
                                "last_name": "Howell",
                                "avatar": "https://reqres.in/img/faces/12-image.jpg"
                            }
                        ],
                        "support": {
                            "url": "https://reqres.in/#support-heading",
                            "text": "To keep ReqRes free, contributions towards server costs are appreciated!"
                        }
                    },
                    "Headers": {
                        "Date": "Wed, 24 Aug 2022 10:21:58 GMT",
                        "Connection": "keep-alive",
                        "X-Powered-By": "Express",
                        "Access-Control-Allow-Origin": "*",
                        "ETag": "W/\"406-ut0vzoCuidvyMf8arZpMpJ6ZRDw\"",
                        "Via": "1.1 vegur",
                        "Cache-Control": "max-age=14400",
                        "CF-Cache-Status": "HIT",
                        "Age": "2415",
                        "Accept-Ranges": "bytes",
                        "Expect-CT": "max-age=604800, report-uri=\"https://report-uri.cloudflare.com/cdn-cgi/beacon/expect-ct\"",
                        "Report-To": "{\"endpoints\":[{\"url\":\"https:\\/\\/a.nel.cloudflare.com\\/report\\/v3?s=f1TrobVjMWxrnhqzHsNhdjNX3rpKmTWetM3%2Br5%2FetZa7nMSr9OcUBg7pM8Pne8u4%2Fn0zButYRjFlQLFP60%2FgZZlIEtihThpuv89S2FkCVTOCAHKZBislBwC6Qg%3D%3D\"}],\"group\":\"cf-nel\",\"max_age\":604800}",
                        "NEL": "{\"success_fraction\":0,\"report_to\":\"cf-nel\",\"max_age\":604800}",
                        "Server": "cloudflare",
                        "CF-RAY": "73fb5d392c7f901f-FRA"
                    }
                }
            },
            "SourceContext": "Serilog.HttpClient.LoggingDelegatingHandler"
        },
        "Renderings": {
            "Elapsed": [
                {
                    "Format": "0.0000",
                    "Rendering": "803.8034"
                }
            ]
        }
    }
    ```
    
