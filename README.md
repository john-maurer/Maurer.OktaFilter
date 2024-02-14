#Maurer.OktaFilter

##Intent

This library aims to provide a seamless solution for acquiring, applying, and storing OKTA security tokens for OKTA token dependant services with minimal impact.

###Dependencies

Polly
Microsoft.AspNetCore.Mvc.Core
Microsoft.AspNetCore.Mvc.Abstractions
Microsoft.Extensions.Caching.Abstractions
Microsoft.Extensions.Configuration.Abstractions
Microsoft.Extensions.Logging.Abstractions

###Why use it?
Use OKTA.Filter where obtaining an authentication OKTA security token and incorporating it into calls where an OKTA token is a necessity. Managing this handshake is crucial due to the cross-cutting concern of operational costs associated with OKTA, and managing the inherent complexity coupled with conflicting team standards can make it challenging. The OKTA.Filter class library addresses this need.

###When to Use

Employ the OKTA.Filter when interacting with APIs that require OKTA authentication to abstract the authentication process away from the core work.

###Components

1. DistributedCacheHelper
  * A DistributedCache wrapper that abstracts usage and facilitates unit testing.
2. TokenService
  * Responsible for retrieving the OKTA token.
3. OKTAFilter
  * IAsyncActionFilter implementation encapsulating the token service and retry logic for refreshes and retries.

###Interactions

A client initiates a token request from a distributed in-memory cache.

###Outcomes

* Simplifies OKTA authentication services and associated complexity to straightforward Dependency Injection (DI) and collection-like references to the stored token.
* 401, 403, and 407 calls have a configurable retry policy for token acquisition, defined in appsettings.json.
* Tokens are shared across multiple client requests, reducing OKTA costs to the organization compared to issuing a new token per call.
* Token refresh occurs automatically.

##Implementation Guide

###1. Configure Secrets in Azure Key Vault and Export to Azure Configuration Services

1.1 Add three elements to your Azure Key Vault:

  OAUTH-PASSWORD
  OAUTH-URL
  OAUTH-USER

You'll need to acquire a valid oauth user name, password and URL from your organization.

###2. Install the Package

2.1 Right-click on your project and select 'Manage NuGet Packages...'.
2.2 Change your Package Source to 'Nuget' (https://my.awesome.link/nuget/v3/index.json).
2.3 Migrate to the 'Browse' tab.
2.4 In the search bar, type 'Maurer.OKTAFilter' and install the latest package.

###3. Configure appsettings.json

3.1 Add the following block to your project's appsettings.json and set these values as needed:

```
"AuthenticationRetrySettings": {
  "Count": 3,
  "Sleep": 1000,
  "LifetimeInMinutes": 55
},
```

###4. Configure DI in Startup

Multiple methods exist for setting up your distributed cache. The simplest is an in-memory cache for local caching within the same application instance.

```
//...other code...

//...inject caching...
services.AddMemoryCache();
services.AddDistributedMemoryCache();

//Inject the distributed cache helper
services.AddSingleton<IDistributedCacheHelper, DistributedCacheHelper>();

//...inject the token service...
services.AddSingleton<ITokenService, TokenService>();

//...inject the action filter...
services.AddSingleton<OKTA.Filter>(services => {
    return new OKTA.Filter(
        services.GetRequiredService<ITokenService>(),
        services.GetRequiredService<IConfiguration>(),
        services.GetRequiredService<IDistributedCacheHelper>(),
        services.GetRequiredService<ILogger<Filter>>()
    );
});

Alternative caching methods are detailed below.

###Redis Cache

Redis can cache data across multiple applications and application instances on multiple servers, unlike just a single application instance.

Install the latest Microsoft.Extensions.Caching.StackExchangeRedis package and replace the following code from the previous example:

```
//...inject caching...
services.AddMemoryCache();
services.AddDistributedMemoryCache();
```

With this:

```
services.AddStackExchangeRedisCache(options =>
{
    options.Configuration = "your-redis-connection-string";
});
```

Then replace this line:

```
services.AddSingleton<IDistributedCacheHelper, DistributedCacheHelper>();
```

With this:

```
services.AddSingleton<IDistributedCacheHelper, DistributedCacheHelper>(services => {
  return new DistributedCacheHelper(services.GetRequiredService<IDistributedCache>());
});
```

##SQL Server Cache

An SQL server can also be used as a caching store. The 'sql-cache' tool can help create a table for caching.

###1. Use the dotnet command 'sql-cache create'.


```
dotnet sql-cache create "Data Source=(localdb)/MSSQLLocalDB;Initial Catalog=DistCache;Integrated Security=True;" dbo MySuperRadCache
```

This produces a table called 'MySuperRadCache' with the following schema:

| Name | Data Type | Allow Nulls }
| :--: | :--: | :--: | 
| Id | nvarchar(499) | No |
| Value | varbinary(MAX) | No |
| ExpiresAtTime | dateTimeOffset(7) | No |
| SlidingExpirationInSeconds | bigint | Yes |
| AbsoluteExpiration | datetimeoffset(7) | No |

Next, replace this code from the original example:

```
//...inject caching...
services.AddMemoryCache();
services.AddDistributedMemoryCache();
```

With something like this:

```
builder.Services.AddDistributedSqlServerCache(options =>
{
    options.ConnectionString = builder.Configuration.GetConnectionString("your-sql-connection-string");
    options.SchemaName = "dbo";
    options.TableName = "TestCache";
});
```

###Distributed SQL Server Cache (using Entity Framework)

Entity framework can also be used for more control and application integration.

```
services.AddDbContext<YourCacheDbContext>(options =>
{
    options.UseSqlServer("your-sql-connection-string");
});

services.AddDistributedSqlServerCache(options =>
{
    options.ConnectionString = "your-sql-connection-string";
    options.SchemaName = "dbo";
    options.TableName = "TestCache";
});
```

##Using the Filter in a Controller

Add the Filter's Tag to the Controller:

```
[ApiController]
[Route("mine")]
[TypeFilter(typeof(LPSAuthenticationFilter))]
public class MyController : ControllerBase
{
```

Include an Internal Point of Reference to the Distributed Cache:

```
[ApiController]
[Route("mine")]
[TypeFilter(typeof(LPSAuthenticationFilter))]
public class MyController : ControllerBase
{
    private readonly IConfiguration _configuration;
    private readonly ILogger<MyController> _logger;
    private readonly IDistributedCacheHelper _memoryCache;
```

Modify the Constructor to take a Reference to the Distributed Cache from DI:

```
public MyController(ILogger<MyController> logger, IConfiguration configuration, IDistributedCacheHelper memoryCache)
{
    _logger = logger;
    _configuration = configuration;
    _memoryCache = memoryCache;
}
```

###Extracting the Token

####As an Token Object

```
var tokenResponse = JsonConvert.DeserializeObject<Token>((await _memoryCache.Get("OKTA-TOKEN"))!);
```

####Just the Token as a String

```
var token = JsonConvert.DeserializeObject<Token>((await _memoryCache.Get("OKTA-TOKEN"))!).AccessToken;
```