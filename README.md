# ProblemDetails

`ProblemDetails` is the repository for the `Atya.Errors.ProblemDetails` NuGet
package.

| | |
| --- | --- |
| Repository | [https://github.com/AtyaLibraries/ProblemDetails](https://github.com/AtyaLibraries/ProblemDetails) |
| NuGet | `Atya.Errors.ProblemDetails` |
| License | MIT |

This package provides RFC 9457-style ASP.NET Core error responses and exception
mapping at the HTTP boundary. It depends on `Atya.Errors.Exceptions` for the
core exception taxonomy and on `Atya.Foundation.Results` for Result/Error
mapping.

## Consumer documentation

The NuGet package includes the full consumer README from
`src/ProblemDetails/README.md`. It covers installation, quick start,
configuration, default exception mappings, extension members, security behavior,
versioning, and support guidance.

## Included APIs

- `AddAtyaProblemDetails`
- `UseAtyaProblemDetails`
- `IExceptionToProblemDetailsMapper`
- `IResultToProblemDetailsMapper`
- `AtyaProblemDetailsOptions`
- `ExceptionProblemDetailsMapping`
- `ErrorProblemDetailsMapping`
- `ValidationProblemError`

## Layout

```text
.
|-- src/ProblemDetails/
|-- tests/ProblemDetails.UnitTests/
|-- samples/ProblemDetails.Samples.Console/
|-- benchmarks/ProblemDetails.Benchmarks/
`-- .github/
```

## Build, test, pack

```bash
dotnet restore
dotnet format ./ProblemDetails.sln --verify-no-changes --verbosity minimal --no-restore
dotnet build --configuration Release --no-restore
dotnet test ./tests/ProblemDetails.UnitTests/ProblemDetails.UnitTests.csproj --configuration Release --no-build --collect "XPlat Code Coverage"
dotnet list ./ProblemDetails.sln package --vulnerable --include-transitive
dotnet pack ./src/ProblemDetails/ProblemDetails.csproj --configuration Release --no-build --output artifacts/packages -p:EnablePackageValidation=true
```

Artifacts land in `artifacts/packages/`.

## Benchmarks

Benchmarks use BenchmarkDotNet and must be run in Release:

```bash
dotnet run --project ./benchmarks/ProblemDetails.Benchmarks/ProblemDetails.Benchmarks.csproj --configuration Release -- --filter *
```

Use `--list flat` to list available benchmark names and `--filter *Validation*`
or `--filter *Unhandled*` to run a focused subset.

## Versioning

Versions are derived from git tags via [MinVer](https://github.com/adamralph/minver).
Releases are published by the central publisher, not by a workflow in this repository. After promoting `development` to `master` and tagging the `master` commit, a maintainer runs `gh workflow run publish.yml --repo AtyaLibraries/publisher -f repository=AtyaLibraries/ProblemDetails -f ref=<tag>`; see the publisher RUNBOOK (https://github.com/AtyaLibraries/publisher/blob/main/docs/RUNBOOK.md).
