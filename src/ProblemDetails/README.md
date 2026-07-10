# Atya.Errors.ProblemDetails

`Atya.Errors.ProblemDetails` adds RFC 9457-style ASP.NET Core problem details
responses for the exception types in `Atya.Errors.Exceptions` and failed
Result values from `Atya.Foundation.Results`.

It is intended for API boundaries where production services need consistent
HTTP status codes, stable problem type identifiers, trace/correlation metadata,
and safe handling of unexpected exceptions.

## Install

```bash
dotnet add package Atya.Errors.ProblemDetails
```

## Compatibility

- Targets `net10.0`.
- Requires ASP.NET Core through the `Microsoft.AspNetCore.App` framework
  reference.
- Depends on `Atya.Errors.Exceptions`, `Atya.Foundation.Guards`, and
  `Atya.Foundation.Results`.

## Quick Start

Register the services and add the exception handler middleware early in the
pipeline.

```csharp
using Atya.Errors.ProblemDetails.Extensions;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddAtyaProblemDetails();

var app = builder.Build();

app.UseAtyaProblemDetails();

app.MapGet("/customers/{id}", (Guid id) =>
{
    throw new Atya.Errors.Exceptions.NotFoundException(
        $"Customer '{id}' was not found.",
        "customers.not_found");
});

app.Run();
```

The response is written as `application/problem+json` and includes the standard
problem details members plus Atya extension members when available.

```json
{
  "type": "urn:atya:problem-type:not-found",
  "title": "Not Found",
  "status": 404,
  "detail": "Customer '...' was not found.",
  "instance": "/customers/...",
  "traceId": "...",
  "errorCode": "customers.not_found"
}
```

## Default Mappings

| Exception | Status | Title | Type |
| --- | ---: | --- | --- |
| `ValidationException` | 400 | `Bad Request` | `urn:atya:problem-type:validation` |
| `BusinessRuleViolationException` | 422 | `Unprocessable Entity` | `urn:atya:problem-type:business-rule-violation` |
| `UnauthorizedException` | 401 | `Unauthorized` | `urn:atya:problem-type:unauthorized` |
| `ForbiddenException` | 403 | `Forbidden` | `urn:atya:problem-type:forbidden` |
| `NotFoundException` | 404 | `Not Found` | `urn:atya:problem-type:not-found` |
| `ConflictException` | 409 | `Conflict` | `urn:atya:problem-type:conflict` |
| `ConcurrencyException` | 409 | `Conflict` | `urn:atya:problem-type:concurrency-conflict` |
| `InfrastructureException` | 500 | `Internal Server Error` | `urn:atya:problem-type:infrastructure-failure` |
| unhandled exceptions | 500 | `Internal Server Error` | `urn:atya:problem-type:internal-server-error` |

Derived exception types use the first assignable mapping when an exact mapping
is not configured.

## Extension Members

| Extension | Included when |
| --- | --- |
| `traceId` | `IncludeTraceId` is `true` and ASP.NET Core has a trace identifier. |
| `correlationId` | `CorrelationIdAccessor` returns a non-empty value. The default reads `X-Correlation-ID`. |
| `errorCode` | `IncludeErrorCode` is `true` and the exception has an Atya error code. |
| `metadata` | `IncludeExceptionMetadata` is `true` and the exception has metadata. |
| `errors` | The exception is an `Atya.Errors.Exceptions.ValidationException`, or a mapped Results error has `Kind` = `Validation` on itself or any child detail. |

Validation errors use this shape:

```json
{
  "propertyName": "Email",
  "message": "Email is required.",
  "errorCode": "validation.required"
}
```

Attempted values are omitted by default because they can contain sensitive user
input.

Failed Results validation errors use an object keyed by `Error.Target`. Multiple
errors for the same target are preserved in order. A targetless validation error
uses an empty-string key.

```json
{
  "Email": [
    {
      "propertyName": "Email",
      "message": "Email is required.",
      "errorCode": "validation.required"
    }
  ]
}
```

## Results Mapping

`IResultToProblemDetailsMapper` maps failed `Result`, failed `Result<TValue>`,
or an `Error` directly to `Microsoft.AspNetCore.Mvc.ProblemDetails`. Successful
results are not errors and throw `InvalidOperationException` when passed to the
mapper.

```csharp
using Atya.Errors.ProblemDetails.Abstractions;
using Atya.Foundation.Results;

app.MapGet("/customers/{id}", (
    Guid id,
    HttpContext httpContext,
    IResultToProblemDetailsMapper mapper) =>
{
    Result result = Result.Failure(new Error(
        "customers.not_found",
        $"Customer '{id}' was not found.",
        ErrorKind.NotFound));

    return result.IsSuccess
        ? Results.NoContent()
        : Results.Problem(mapper.Map(result, httpContext));
});
```

Default Results error-kind mappings are:

| ErrorKind | Status | Title | Type |
| --- | ---: | --- | --- |
| `Validation` | 400 | `Bad Request` | `urn:atya:problem-type:validation` |
| `Failure` | 422 | `Unprocessable Entity` | `urn:atya:problem-type:failure` |
| `NotFound` | 404 | `Not Found` | `urn:atya:problem-type:not-found` |
| `Conflict` | 409 | `Conflict` | `urn:atya:problem-type:conflict` |
| `Unauthorized` | 401 | `Unauthorized` | `urn:atya:problem-type:unauthorized` |
| `Forbidden` | 403 | `Forbidden` | `urn:atya:problem-type:forbidden` |
| `Unexpected` | 500 | `Internal Server Error` | `urn:atya:problem-type:internal-server-error` |

`ErrorKind.Failure` maps to 422 by default because it represents an expected
operation failure for a syntactically valid request that the service understood
but could not process as requested. Use `MapError` when a service needs a
different public contract for its own failure category.

## Configuration

```csharp
builder.Services.AddAtyaProblemDetails(options =>
{
    options.IncludeExceptionMetadata = true;
    options.IncludeTraceId = true;
    options.IncludeValidationAttemptedValues = false;

    options.CorrelationIdAccessor = httpContext =>
        httpContext.Request.Headers.TryGetValue("X-Request-ID", out var values)
            ? values.ToString()
            : null;

    options.Map<TimeoutException>(
        StatusCodes.Status504GatewayTimeout,
        "Gateway Timeout",
        "urn:atya:problem-type:gateway-timeout",
        static (_, _) => "The upstream service did not respond in time.");

    // Remap the default ErrorKind.Failure contract for this service.
    options.MapError(
        ErrorKind.Failure,
        StatusCodes.Status400BadRequest,
        "Bad Request",
        "urn:example:problem-type:invalid-command");

    options.ExtensionNames.Errors = "validationErrors";
    options.ExtensionNames.Metadata = null;

    options.CustomizeProblemDetails = (httpContext, exception, problemDetails) =>
    {
        problemDetails.Extensions["service"] = "orders-api";
    };
});
```

`Map<TException>` replaces an existing mapping for the same exception type,
trims the title and type values, and rejects HTTP status codes outside the
standard `100` through `599` range. `RemoveMapping<TException>()` removes one
mapping, and `ClearMappings()` lets a service fully own its public error
contract.

## Exception Details

Unhandled exceptions and mapped 500-level exceptions are deliberately sanitized
by default:

```json
{
  "status": 500,
  "title": "Internal Server Error",
  "detail": "An unexpected error occurred."
}
```

Only enable raw exception details for trusted environments.

```csharp
builder.Services.AddAtyaProblemDetails(options =>
{
    options.IncludeExceptionDetailsPredicate = (httpContext, exception) =>
        builder.Environment.IsDevelopment();
});
```

Known Atya exceptions below status 500 use their exception message as `detail`.
For mapped 500-level exceptions, use a safe `Map<TException>` detail factory
when a public detail is useful.

## Versioning

Stable packages use SemVer and are produced from `vMAJOR.MINOR.PATCH` tags.
Problem type URIs, extension names, public types, and serialized JSON member
names are treated as compatibility contracts.

## Support

Report issues through the repository at
https://github.com/AtyaLibraries/ProblemDetails. Include the package version,
target framework, ASP.NET Core version, and a minimal reproduction whenever
possible.
