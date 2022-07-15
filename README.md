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
      "@t": "2021-06-07T22:38:08.4416472Z",
      "@mt": "HTTP Request Completed {@Context}",
      "Context": {
        "Request": {
          "ClientIp": "::1",
          "Method": "GET",
          "Scheme": "http",
          "Host": "localhost:5000",
          "Path": "/Home/Index",
          "QueryString": "",
          "Query": {},
          "BodyString": "{\r\n  \"query\": \"query listPageQuery($text: String!) {\\r\\n  search(parameters: $text) {\\r\\n    displayText\\r\\n    contentType\\r\\n    contentItemId\\r\\n    alias {\\r\\n      alias\\r\\n      __typename\\r\\n    }\\r\\n    __typename\\r\\n  }\\r\\n}\\r\\n\",\r\n  \"variables\": {\r\n    \"text\": \"{Term: \\\"test\\\"}\"\r\n  }\r\n}",
          "Body": {
            "query": "query listPageQuery($text: String!) {\r\n  search(parameters: $text) {\r\n    displayText\r\n    contentType\r\n    contentItemId\r\n    alias {\r\n      alias\r\n      __typename\r\n    }\r\n    __typename\r\n  }\r\n}\r\n",
            "variables": {
              "text": "{Term: \"test\"}"
            }
          },
          "Header": {
            "Connection": "keep-alive",
            "Content-Type": "application/json",
            "Accept": "*/*",
            "Accept-Encoding": "gzip, deflate, br",
            "Host": "localhost:5000",
            "User-Agent": "PostmanRuntime/7.28.0",
            "Content-Length": "289",
            "X-Correlation-ID": "123",
            "Postman-Token": "*** MASKED ***"
          },
          "UserAgent": {
            "_Raw": "PostmanRuntime/7.28.0",
            "Browser": "Other",
            "BrowserVersion": ".",
            "OperatingSystem": "Other",
            "OperatingSystemVersion": ".",
            "Device": "Other",
            "DeviceModel": "",
            "DeviceManufacturer": ""
          }
        },
        "Response": {
          "StatusCode": 200,
          "IsSucceed": true,
          "ElapsedMilliseconds": 1086.0481,
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
          "Header": {
            "Date": [
              "Mon, 07 Jun 2021 23:10:07 GMT"
            ],
            "Connection": [
              "keep-alive"
            ],
            "X-Powered-By": [
              "Express"
            ],
            "Access-Control-Allow-Origin": [
              "*"
            ],
            "ETag": [
              "W/\"406-ut0vzoCuidvyMf8arZpMpJ6ZRDw\""
            ],
            "Via": [
              "1.1 vegur"
            ],
            "Cache-Control": [
              "max-age=14400"
            ],
            "CF-Cache-Status": [
              "HIT"
            ],
            "Age": [
              "114"
            ],
            "Accept-Ranges": [
              "bytes"
            ],
            "cf-request-id": [
              "0a8a56a64600002b7150089000000001"
            ],
            "Expect-CT": [
              "max-age=604800, report-uri=\"https://report-uri.cloudflare.com/cdn-cgi/beacon/expect-ct\""
            ],
            "Report-To": [
              "{\"endpoints\":[{\"url\":\"https:\\/\\/a.nel.cloudflare.com\\/report\\/v2?s=YgyrFOvFSMIpafNd%2B4OGV0EZeungoLs0%2FQrtFZKlCviJAUJMt%2FoSmWF82X5OUxcsSJRhyYA%2FwAZOJ0dW%2FlwZq9OSkmcVczmVR8NLQTCTFXs95%2Bv%2BikEF\"}],\"group\":\"cf-nel\",\"max_age\":604800}"
            ],
            "NEL": [
              "{\"report_to\":\"cf-nel\",\"max_age\":604800}"
            ],
            "Server": [
              "cloudflare"
            ],
            "CF-RAY": [
              "65bd8d5069aa2b71-FRA"
            ],
            "Alt-Svc": [
              "h3-27=\":443\"",
              "h3-28=\":443\"",
              "h3-29=\":443\"",
              "h3=\":443\""
            ]
          }
        },
        "Diagnostics": {}
      },
      "SourceContext": "Serilog.AspNetCore.RequestLoggingMiddleware",
      "ActionId": "ee7ce05b-044b-442b-9f46-bd5bd6058cff",
      "ActionName": "Serilog.HttpClient.Samples.AspNetCore.Controllers.HomeController.Index (Serilog.HttpClient.Samples.AspNetCore)",
      "CorrelationId": "123",
      "RequestId": "0HM9A0JCHS60P:00000002",
      "RequestPath": "/Home/Index",
      "ConnectionId": "0HM9A0JCHS60P",
      "EnvironmentUserName": "DESKTOP-SP4IR37\\AV",
      "MachineName": "DESKTOP-SP4IR37",
      "EventId": "C2110DE4"
    }
    ```
    
