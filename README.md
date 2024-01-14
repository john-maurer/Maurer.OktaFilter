# Maurer.OktaFilter

## Intent

.NET Core ActionFilter for acquiring, applying, and storing OKTA security tokens.

### Dependencies

Polly
Microsoft.AspNetCore.Mvc.Core
Microsoft.AspNetCore.Mvc.Abstractions
Microsoft.Extensions.Caching.Abstractions
Microsoft.Extensions.Configuration.Abstractions
Microsoft.Extensions.Logging.Abstractions

### Why use it?
Use Maurer.OKTAFilter where obtaining an authentication OKTA security token and incorporating it into calls where an OKTA token is a necessity. Managing this handshake is crucial due to the cross-cutting concern of operational costs associated with OKTA, and managing the inherent complexity can be challenging when coupled with conflicting standards accross one or more organizations. The Maurer.OKTAFilter class library addresses this need.

### When to Use

Employ the Maurer.OKTAFilter when interacting with APIs that require OKTA authentication to abstract the authentication process away from the core work.

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
* 401, 403, and 407 calls have a configurable retry policy for token acquisition, defined in appsettings.json.
* Tokens are shared across multiple client requests, reducing OKTA costs to the organization compared to issuing a new token per call.
* Token refresh occurs automatically.

## Implementation Guide
