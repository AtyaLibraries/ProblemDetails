// <copyright file="Program.cs" company="Atya">
// Copyright (c) Atya. All rights reserved.
// </copyright>

using Atya.Errors.Exceptions;
using Atya.Errors.Exceptions.Models;
using Atya.Errors.ProblemDetails.Mappings;
using Atya.Errors.ProblemDetails.Options;
using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Running;
using Microsoft.AspNetCore.Http;
using AspNetProblemDetails = Microsoft.AspNetCore.Mvc.ProblemDetails;

namespace ProblemDetails.Benchmarks;

public static class Program
{
    public static void Main(string[] args)
    {
        BenchmarkSwitcher.FromAssembly(typeof(Program).Assembly).Run(args);
    }
}

[MemoryDiagnoser]
[RankColumn]
[CategoriesColumn]
public class KnownExceptionMappingBenchmarks
{
    private DefaultExceptionToProblemDetailsMapper _defaultMapper = null!;
    private DefaultExceptionToProblemDetailsMapper _metadataMapper = null!;
    private DefaultHttpContext _httpContext = null!;
    private NotFoundException _notFoundException = null!;
    private BusinessRuleViolationException _businessRuleViolationException = null!;
    private ValidationException _validationException = null!;
    private ValidationException _validationExceptionWithMetadata = null!;

    [GlobalSetup]
    public void Setup()
    {
        _defaultMapper = new DefaultExceptionToProblemDetailsMapper(new AtyaProblemDetailsOptions());
        _metadataMapper = new DefaultExceptionToProblemDetailsMapper(new AtyaProblemDetailsOptions
        {
            IncludeExceptionMetadata = true,
            IncludeValidationAttemptedValues = true
        });

        _httpContext = new DefaultHttpContext();
        _httpContext.Request.Path = "/customers/42";
        _httpContext.Request.Headers["X-Correlation-ID"] = "bench-correlation";
        _httpContext.TraceIdentifier = "bench-trace";

        _notFoundException = new NotFoundException(
            "Customer was not found.",
            "customers.not_found");

        _businessRuleViolationException = new BusinessRuleViolationException(
            "Order cannot be cancelled.",
            "orders.cancel.not_allowed");

        _validationException = CreateValidationException(metadata: null);
        _validationExceptionWithMetadata = CreateValidationException(new Dictionary<string, object?>
        {
            ["entityId"] = 42,
            ["tenant"] = "bench"
        });
    }

    [Benchmark(Baseline = true)]
    [BenchmarkCategory("known")]
    public AspNetProblemDetails MapNotFoundException()
    {
        return _defaultMapper.Map(_notFoundException, _httpContext);
    }

    [Benchmark]
    [BenchmarkCategory("known")]
    public AspNetProblemDetails MapBusinessRuleViolationException()
    {
        return _defaultMapper.Map(_businessRuleViolationException, _httpContext);
    }

    [Benchmark]
    [BenchmarkCategory("validation")]
    public AspNetProblemDetails MapValidationException()
    {
        return _defaultMapper.Map(_validationException, _httpContext);
    }

    [Benchmark]
    [BenchmarkCategory("validation")]
    public AspNetProblemDetails MapValidationExceptionWithMetadata()
    {
        return _metadataMapper.Map(_validationExceptionWithMetadata, _httpContext);
    }

    private static ValidationException CreateValidationException(IReadOnlyDictionary<string, object?>? metadata)
    {
        return new ValidationException(
            "Customer request is invalid.",
            [
                new ValidationExceptionItem("Email", "Email is required.", "validation.required"),
                new ValidationExceptionItem("Age", "Age must be greater than zero.", "validation.range", 0),
                new ValidationExceptionItem("Name", "Name is too long.", "validation.length", "Very long customer name")
            ],
            metadata: metadata);
    }
}

[MemoryDiagnoser]
[RankColumn]
[CategoriesColumn]
public class UnhandledExceptionMappingBenchmarks
{
    private DefaultExceptionToProblemDetailsMapper _sanitizedMapper = null!;
    private DefaultExceptionToProblemDetailsMapper _detailsMapper = null!;
    private DefaultHttpContext _httpContext = null!;
    private InvalidOperationException _unhandledException = null!;

    [GlobalSetup]
    public void Setup()
    {
        _sanitizedMapper = new DefaultExceptionToProblemDetailsMapper(new AtyaProblemDetailsOptions());
        _detailsMapper = new DefaultExceptionToProblemDetailsMapper(new AtyaProblemDetailsOptions
        {
            IncludeExceptionDetailsPredicate = static (_, _) => true
        });

        _httpContext = new DefaultHttpContext();
        _httpContext.Request.Path = "/customers/42";
        _httpContext.Request.Headers["X-Correlation-ID"] = "bench-correlation";
        _httpContext.TraceIdentifier = "bench-trace";

        _unhandledException = new InvalidOperationException("Unexpected failure.");
    }

    [Benchmark(Baseline = true)]
    [BenchmarkCategory("unhandled")]
    public AspNetProblemDetails MapUnhandledExceptionSanitized()
    {
        return _sanitizedMapper.Map(_unhandledException, _httpContext);
    }

    [Benchmark]
    [BenchmarkCategory("unhandled")]
    public AspNetProblemDetails MapUnhandledExceptionWithDetails()
    {
        return _detailsMapper.Map(_unhandledException, _httpContext);
    }
}
