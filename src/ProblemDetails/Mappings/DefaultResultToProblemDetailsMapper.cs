// <copyright file="DefaultResultToProblemDetailsMapper.cs" company="Atya">
// Copyright (c) Atya. All rights reserved.
// </copyright>

using System.Diagnostics;
using Atya.Errors.ProblemDetails.Abstractions;
using Atya.Errors.ProblemDetails.Constants;
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
    private readonly AtyaProblemDetailsOptions _options;

    /// <summary>
    /// Initializes a new instance of the <see cref="DefaultResultToProblemDetailsMapper"/> class.
    /// </summary>
    /// <param name="options">The configured options.</param>
    public DefaultResultToProblemDetailsMapper(AtyaProblemDetailsOptions options)
    {
        _options = options ?? throw new ArgumentNullException(nameof(options));
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="DefaultResultToProblemDetailsMapper"/> class.
    /// </summary>
    /// <param name="options">The configured options accessor.</param>
    public DefaultResultToProblemDetailsMapper(IOptions<AtyaProblemDetailsOptions> options)
    {
        _options = options?.Value ?? throw new ArgumentNullException(nameof(options));
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

        var mapping = ResolveMapping(resultError);

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
            Instance = CreateInstance(httpContext),
        };

        AddExtensions(problemDetails, resultError, httpContext, statusCode);

        return problemDetails;
    }

    private static string CreateInstance(HttpContext httpContext)
    {
        return string.IsNullOrWhiteSpace(httpContext.Request.Path.Value)
            ? "/"
            : httpContext.Request.Path.Value!;
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

    private ErrorProblemDetailsMapping? ResolveMapping(Error error)
    {
        return _options.ErrorMappings.FirstOrDefault(mapping => mapping.Kind == error.Kind);
    }

    private void AddExtensions(
        AspNetProblemDetails problemDetails,
        Error error,
        HttpContext httpContext,
        int statusCode)
    {
        if (_options.IncludeTraceId)
        {
            var traceId = Activity.Current?.Id ?? httpContext.TraceIdentifier;
            AddExtension(problemDetails, _options.ExtensionNames.TraceId, traceId);
        }

        var correlationId = _options.CorrelationIdAccessor(httpContext);
        AddExtension(problemDetails, _options.ExtensionNames.CorrelationId, correlationId);

        if (!ProblemDetailsDetailPolicy.IsServerError(statusCode) && _options.IncludeErrorCode)
        {
            AddExtension(problemDetails, _options.ExtensionNames.ErrorCode, error.Code);
        }

        if (!ProblemDetailsDetailPolicy.IsServerError(statusCode))
        {
            var validationErrors = CreateValidationErrors(error);
            if (validationErrors.Count > 0)
            {
                AddExtension(problemDetails, _options.ExtensionNames.Errors, validationErrors);
            }
        }

        static void AddExtension(AspNetProblemDetails problemDetails, string? name, object? value)
        {
            if (!string.IsNullOrWhiteSpace(name) &&
                value is not null &&
                (value is not string text || !string.IsNullOrWhiteSpace(text)))
            {
                problemDetails.Extensions[name.Trim()] = value;
            }
        }
    }

}
