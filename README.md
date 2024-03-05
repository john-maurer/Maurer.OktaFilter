# Maurer.OktaFilter

## Intent

This library aims to provide a seamless solution for acquiring, applying, and storing OKTA security tokens for OKTA token dependant services with minimal impact.

### Dependencies

* Polly
* Microsoft.AspNetCore.Mvc.Core
* Microsoft.AspNetCore.Mvc.Abstractions
* Microsoft.Extensions.Caching.Abstractions
* Microsoft.Extensions.Configuration.Abstractions
* Microsoft.Extensions.Logging.Abstractions

### Why use it?
Use OKTA.Filter where obtaining an authentication OKTA security token and incorporating it into calls where an OKTA token is a necessity. Managing this handshake is crucial due to the cross-cutting concern of operational costs associated with OKTA, and managing the inherent complexity coupled with conflicting team standards can make it challenging. The OKTA.Filter class library addresses this need.

### When to Use

Employ the OKTA.Filter when interacting with APIs that require OKTA authentication to abstract the authentication process away from the core work.

### Components

1. DistributedCacheHelper
  * A DistributedCache wrapper that abstracts usage and facilitates unit testing.
2. TokenService
  * Responsible for retrieving the OKTA token.
3. OKTAFilter
  * IAsyncActionFilter implementation encapsulating the token service and retry logic for refreshes and retries.

### Interactions

A client initiates a token request from a distributed in-memory cache.

### Outcomes

* Simplifies OKTA authentication services and associated complexity to straightforward Dependency Injection (DI) and collection-like references to the stored token.
* 401, 403, and 407 calls have a configurable retry policy for token acquisition, defined in Maurer.OktaFilter.Settings.cs which can be used with appsettings.json in your startup.cs file.
* Tokens are shared across multiple client requests, reducing OKTA costs to the organization compared to issuing a new token per call.
* Token refresh occurs automatically.

## Implementation Guide

### 1. Configure Secrets in Azure Key Vault and Export to Azure Configuration Services

1.1 Add three elements to your Azure Key Vault:

  OAUTH-PASSWORD
  OAUTH-URL
  OAUTH-USER

You'll need to acquire a valid oauth user name, password and URL from your organization.

### 2. Install the Package

2.1 Right-click on your project and select 'Manage NuGet Packages...'.
2.2 Change your Package Source to 'Nuget' (https://nuget/v3/index.json).
2.3 Migrate to the 'Browse' tab.
2.4 In the search bar, type 'Maurer.OKTAFilter' and install the latest package.

### 3. Configure Settings

3.1 Use the static 'Settings' object properties to configure the filter and how OKTA tokens are managed:

* **OAUTHUSER** - Authorized user/principle ID
* **OAUTHPASSWORD** - Password associated with user/principle ID
* **OAUTHURL** - Your organizations (OKTA url)[https://developer.okta.com/docs/guides/find-your-domain/main/].
* **RETRIES** - The number of attempts the filter should make to acquire an OKTA token.
* **RETRYSLEEP** - The number in seconds to wait in between retry attempts.
* **TOKENLIFETIME** - The lifetime in minutes of the OKTA token, should not exceed 55 minutes.
* **GRANTTYPE** - In the context of Azure and identity management, a "grant type" typically refers to the method used by an application to obtain an access token. Access tokens are credentials that represent the authorization granted to the application to access a user's data.  Azure Active Directory (Azure AD), which is Microsoft's cloud-based identity and access management service, supports several OAuth 2.0 authorization grant types. Here are some common grant types used in Azure AD:

    **Authorization Code Grant:**

    **Usage:** This is the most common and secure grant type for server-side web applications.
    **Flow:** The application redirects the user to Azure AD's authorization endpoint. After successful authentication and authorization, Azure AD redirects back to the application with an authorization code. The application then exchanges this code for an access token and a refresh token.

    **Implicit Grant:**

    **Usage:** Used in single-page applications (SPAs) or mobile apps where the client-side cannot securely store client secrets.
    **Flow:** The access token is directly returned to the client (browser) after authentication and authorization. This grant type skips the step of exchanging an authorization code for tokens, making it simpler for client-side applications.

    **Client Credentials Grant:**

    **Usage:** Used for non-interactive applications, like background services or daemons.
    **Flow:** The application sends its client ID and client secret directly to Azure AD to obtain an access token. Since this doesn't involve user interaction, it's suitable for server-to-server communication.

    **Resource Owner Password Credentials (ROPC) Grant:**

    **Usage:** Used when the client application has the user's credentials and can authenticate on behalf of the user.
    **Flow:** The client application collects the user's username and password and sends them to Azure AD to obtain an access token. This grant type is less secure and should be avoided unless necessary.

    **Device Code Grant:**

    **Usage:** Designed for devices that can't directly enter credentials, such as smart TVs or gaming consoles.
    **Flow:** The user is given a code to enter on a different device. The device polls Azure AD until the user completes the authentication on the other device, and then it receives the tokens.

* **SCOPE** - In the context of an HTTP request within an Azure environment, the term "scope" typically refers to the permissions or access levels requested by a client application when it requests an access token. This concept is closely associated with OAuth 2.0 and OpenID Connect, which are authentication and authorization protocols used in Azure Active Directory (Azure AD).

    When a client application wants to access a protected resource (like a user's data or a secured API), it needs to include information about the permissions it's requesting in the form of "scopes" within its authentication request. The scope parameter specifies the access level the application is requesting from the user or the resource owner.

    Here's a brief overview of how scope works in an HTTP request within an Azure environment:

    **Authentication Request:**

    The client application initiates the authentication process by redirecting the user to Azure AD for login. As part of this request, the client includes a scope parameter specifying the permissions it needs.

    **Consent Prompt:**

    If the user hasn't previously granted consent for the requested scopes, Azure AD may prompt the user to consent to the requested permissions.
    The consent page informs the user about the requested scopes and asks for their approval.

    **Token Issuance:**

    After successful authentication and, if necessary, consent, Azure AD issues an access token to the client application.
    The access token includes information about the granted permissions (scopes) and is used to access the requested resource.

    **Access to Resource:**

    The client application includes the obtained access token in its requests to the protected resource (e.g., an API).
    The resource server (API) checks the access token to ensure it has the necessary scopes to perform the requested actions.
    Common scopes might include "openid" for authentication, "profile" for user information, and specific resource-related scopes for accessing APIs. The specific scopes available depend on the configuration of the resource server and the permissions registered for the application in Azure AD.

    It's essential to carefully define and request only the scopes needed for the application's functionality to follow the principle of least privilege and minimize potential security risks.

    Learn more (here)[https://learn.microsoft.com/en-us/entra/identity-platform/scopes-oidc].

### 4. Configure DI in Startup

Multiple methods exist for setting up your distributed cache. The simplest is an in-memory cache for local caching within the same application instance.

```
using Maurer.OktaFilter

//...other code...

//...inject caching...
services.AddMemoryCache();
services.AddDistributedMemoryCache();

//Inject the distributed cache helper
services.AddSingleton<IDistributedCacheHelper, DistributedCacheHelper>();

//...inject the token service...
services.AddSingleton<ITokenService, TokenService>();

//...inject the action filter...
services.AddSingleton<Filter>(services => {
    return new Filter(
        services.GetRequiredService<ITokenService>(),
        services.GetRequiredService<IConfiguration>(),
        services.GetRequiredService<IDistributedCacheHelper>(),
        services.GetRequiredService<ILogger<Filter>>()
    );
});
```

Alternative caching methods are detailed below.

### Redis Cache

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

## SQL Server Cache

An SQL server can also be used as a caching store. The 'sql-cache' tool can help create a table for caching.

### 1. Use the dotnet command 'sql-cache create'.


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

### Distributed SQL Server Cache (using Entity Framework)

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

## Using the Filter in a Controller

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

### Extracting the Token

#### As an Token Object

```
var tokenResponse = JsonConvert.DeserializeObject<Token>((await _memoryCache.Get("OKTA-TOKEN"))!);
```

#### Just the Token as a String

```
var token = JsonConvert.DeserializeObject<Token>((await _memoryCache.Get("OKTA-TOKEN"))!).AccessToken;
```
