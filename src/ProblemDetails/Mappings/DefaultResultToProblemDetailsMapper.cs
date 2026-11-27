// <copyright file="DefaultResultToProblemDetailsMapper.cs" company="Atya">
// Copyright (c) Atya. All rights reserved.
// </copyright>

using Atya.Errors.ProblemDetails.Abstractions;
using Atya.Errors.ProblemDetails.Constants;
using Atya.Errors.ProblemDetails.Internal;
using Atya.Errors.ProblemDetails.Models;
using Atya.Errors.ProblemDetails.Options;
using Atya.Foundation.Guards;
using Atya.Foundation.Results;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Options;
using AspNetProblemDetails = Microsoft.AspNetCore.Mvc.ProblemDetails;

namespace Atya.Errors.ProblemDetails.Mappings;

/// <summary>
/// Default mapper from failed Results values to <see cref="ProblemDetails"/>.
/// </summary>
public sealed class DefaultResultToProblemDetailsMapper : IResultToProblemDetailsMapper
{
    private readonly ProblemDetailsPolicy _policy;

    /// <summary>
    /// Initializes a new instance of the <see cref="DefaultResultToProblemDetailsMapper"/> class.
    /// </summary>
    /// <param name="options">The configured options accessor.</param>
    public DefaultResultToProblemDetailsMapper(IOptions<AtyaProblemDetailsOptions> options)
    {
        ArgumentNullException.ThrowIfNull(options);
        _policy = ProblemDetailsPolicy.Create(options.Value);
    }

    private DefaultResultToProblemDetailsMapper(ProblemDetailsPolicy policy)
    {
        _policy = policy;
    }

    /// <summary>
    /// Creates a mapper from an options object, for tests and manual composition.
    /// </summary>
    /// <param name="options">The options to snapshot.</param>
    /// <returns>A new mapper.</returns>
    public static DefaultResultToProblemDetailsMapper Create(AtyaProblemDetailsOptions options)
    {
        return new DefaultResultToProblemDetailsMapper(ProblemDetailsPolicy.Create(options));
    }

    /// <inheritdoc />
    public AspNetProblemDetails Map(Result result, HttpContext httpContext)
    {
        Guard.AgainstNull(result);
        Guard.AgainstNull(httpContext);

        if (result.IsSuccess)
        {
            throw new InvalidOperationException("A successful result cannot be mapped to problem details.");
        }

        return Map(result.Error, httpContext);
    }

    /// <inheritdoc />
    public AspNetProblemDetails Map<TValue>(Result<TValue> result, HttpContext httpContext)
    {
        Guard.AgainstNull(result);
        Guard.AgainstNull(httpContext);

        if (result.IsSuccess)
        {
            throw new InvalidOperationException("A successful result cannot be mapped to problem details.");
        }

        return Map(result.Error, httpContext);
    }

    /// <inheritdoc />
    public AspNetProblemDetails Map(Error resultError, HttpContext httpContext)
    {
        Guard.AgainstNull(resultError);
        Guard.AgainstNull(httpContext);

        var mapping = _policy.ResolveError(resultError.Kind);

        var statusCode = mapping?.StatusCode ?? StatusCodes.Status422UnprocessableEntity;
        var title = mapping?.Title ?? "Unprocessable Entity";
        var type = mapping?.Type ?? DefaultProblemTypeUris.Failure;

        var problemDetails = new AspNetProblemDetails
        {
            Status = statusCode,
            Title = title,
            Type = type,
            Detail = ProblemDetailsDetailPolicy.CreateDetail(
                statusCode,
                resultError.Message,
                resultError,
                httpContext,
                mapping?.DetailFactory),
            Instance = ProblemDetailsPolicy.CreateInstance(httpContext),
        };

        AddExtensions(problemDetails, resultError, httpContext, statusCode);

        _policy.Customize?.Invoke(new ProblemDetailsCustomizationContext(httpContext, problemDetails, exception: null, resultError));

        return problemDetails;
    }

    private static Dictionary<string, ValidationProblemError[]> CreateValidationErrors(Error error)
    {
        var validationErrors = EnumerateValidationErrors(error).ToArray();
        var result = new Dictionary<string, List<ValidationProblemError>>(StringComparer.Ordinal);

        foreach (var validationError in validationErrors)
        {
            var target = validationError.Target ?? string.Empty;
            if (!result.TryGetValue(target, out var items))
            {
                items = new List<ValidationProblemError>();
                result[target] = items;
            }

            items.Add(new ValidationProblemError(
                target,
                validationError.Message,
                validationError.Code,
                AttemptedValue: null));
        }

        return result.ToDictionary(
            pair => pair.Key,
            pair => pair.Value.ToArray(),
            StringComparer.Ordinal);
    }

    private static IEnumerable<Error> EnumerateValidationErrors(Error error)
    {
        if (error.Kind == ErrorKind.Validation)
        {
            yield return error;
        }

        foreach (var detail in error.Details)
        {
            foreach (var validationError in EnumerateValidationErrors(detail))
            {
                yield return validationError;
            }
        }
    }

    private void AddExtensions(
        AspNetProblemDetails problemDetails,
        Error error,
        HttpContext httpContext,
        int statusCode)
    {
        var names = _policy.ExtensionNames;

        if (_policy.IncludeTraceId)
        {
            ProblemDetailsPolicy.AddExtension(problemDetails, names.TraceId, ProblemDetailsPolicy.ResolveTraceId(httpContext));
        }

        ProblemDetailsPolicy.AddExtension(problemDetails, names.CorrelationId, _policy.CorrelationIdAccessor(httpContext));

        if (!ProblemDetailsDetailPolicy.IsServerError(statusCode) && _policy.IncludeErrorCode)
        {
            ProblemDetailsPolicy.AddExtension(problemDetails, names.ErrorCode, error.Code);
        }

        if (!ProblemDetailsDetailPolicy.IsServerError(statusCode))
        {
            var validationErrors = CreateValidationErrors(error);
            if (validationErrors.Count > 0)
            {
                ProblemDetailsPolicy.AddExtension(problemDetails, names.Errors, validationErrors);
            }
        }
    }
}
