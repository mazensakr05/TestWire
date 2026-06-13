# ASP.NET Core Mastery: Senior Engineer Reference

Timeless ASP.NET Core principles for building robust, secure, and performant web APIs.

## 1. HTTP Semantics & Status Codes
Use the correct status code to communicate intent to the client.

| Scenario | Verb | Status Code |
| :--- | :--- | :--- |
| **Success (with body)** | GET, PUT, PATCH, DELETE | `200 OK` |
| **Resource Created** | POST | `201 Created` (Include `Location` header) |
| **Success (no body)** | DELETE, PUT, POST | `204 No Content` |
| **Invalid Input** | ANY | `400 Bad Request` |
| **Unauthenticated** | ANY | `401 Unauthorized` |
| **Unauthorized (No permission)**| ANY | `403 Forbidden` |
| **Not Found** | ANY | `404 Not Found` |
| **Conflict (e.g. Unique key)** | POST, PUT | `409 Conflict` |
| **Server Error** | ANY | `500 Internal Server Error` |

## 2. Route Templates
- **Constraints:** Use `[HttpGet("{id:int}")]` to restrict types at the routing level.
- **Tokens:** `[controller]` and `[action]` are useful but can lead to breaking changes if classes are renamed. Prefer literal segments for public APIs.
- **Parameters:** Optional parameters `{id?}` and defaults `{id=1}` should be handled gracefully in the action.

## 3. Model Binding
- **[FromBody]:** Complex objects in the request body (JSON).
- **[FromRoute]:** Parameters from the URL path.
- **[FromQuery]:** Parameters from the query string (e.g., `?page=1`).
- **[FromHeader]:** Metadata like `X-Correlation-ID`.
- **[FromForm]:** For multipart/form-data (file uploads).

## 4. Authentication & Authorization
- **[Authorize] vs [AllowAnonymous]:** `[AllowAnonymous]` always wins if both are present. Use `[Authorize]` at the controller level and `[AllowAnonymous]` only when necessary.
- **401 vs 403:** 
    - `401 Unauthorized`: "I don't know who you are."
    - `403 Forbidden`: "I know who you are, but you can't do this."
- **Policy-based:** Prefer `[Authorize(Policy = "AdminOnly")]` over `[Authorize(Roles = "Admin")]` for better flexibility and decoupling.

## 5. Middleware Pipeline
- **Order Matters:** `UseRouting()` must come before `UseAuthorization()`. `UseExceptionHandler()` should be at the very top to catch everything.
- **Short-circuiting:** Middleware like `UseStaticFiles()` can return a response and stop the pipeline before it reaches your controllers.

## 6. WebApplicationFactory (Integration Testing)
- **Setup:** Use `WebApplicationFactory<TEntryPoint>` to spin up an in-memory test server.
- **Service Overrides:** Use `ConfigureTestServices` to swap out real DBs or external APIs for mocks/fakes.
- **Fake Auth:** Implement a `TestAuthHandler` to simulate authenticated users without a real OIDC provider.
- **Lifecycle:** Use `IClassFixture` in xUnit to share the factory instance across tests in a class.

## 7. Dependency Injection (DI)
- **Lifetimes:**
    - **Transient:** New instance every time it's requested.
    - **Scoped:** New instance once per client request (within the same scope).
    - **Singleton:** One instance for the life of the application.
- **Captive Dependency:** A singleton service depending on a scoped service. This is a BUG. The scoped service will live as long as the singleton, effectively becoming a singleton itself.
- **Best Practice:** Never `new` up services inside a controller. Always inject them via the constructor.
