// <copyright file="DefaultExceptionToProblemDetailsMapper.cs" company="Atya">
// Copyright (c) Atya. All rights reserved.
// </copyright>

using System.Diagnostics;
using Atya.Errors.Exceptions;
using Atya.Errors.ProblemDetails.Abstractions;
using Atya.Errors.ProblemDetails.Constants;
using Atya.Errors.ProblemDetails.Options;
using Atya.Foundation.Guards;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Options;
using AspNetProblemDetails = Microsoft.AspNetCore.Mvc.ProblemDetails;

namespace Atya.Errors.ProblemDetails.Mappings;

/// <summary>
/// Default mapper from exceptions to <see cref="ProblemDetails"/>.
/// </summary>
public sealed class DefaultExceptionToProblemDetailsMapper : IExceptionToProblemDetailsMapper
{
    private readonly AtyaProblemDetailsOptions _options;

    /// <summary>
    /// Initializes a new instance of the <see cref="DefaultExceptionToProblemDetailsMapper"/> class.
    /// </summary>
    /// <param name="options">The configured options.</param>
    public DefaultExceptionToProblemDetailsMapper(AtyaProblemDetailsOptions options)
    {
        _options = options ?? throw new ArgumentNullException(nameof(options));
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="DefaultExceptionToProblemDetailsMapper"/> class.
    /// </summary>
    /// <param name="options">The configured options accessor.</param>
    public DefaultExceptionToProblemDetailsMapper(IOptions<AtyaProblemDetailsOptions> options)
    {
        _options = options?.Value ?? throw new ArgumentNullException(nameof(options));
    }

    /// <inheritdoc />
    public AspNetProblemDetails Map(Exception exception, HttpContext httpContext)
    {
        Guard.AgainstNull(exception);
        Guard.AgainstNull(httpContext);

        var mapping = ResolveMapping(exception);

        var statusCode = mapping?.StatusCode ?? StatusCodes.Status500InternalServerError;
        var title = mapping?.Title ?? "Internal Server Error";
        var type = mapping?.Type ?? DefaultProblemTypeUris.Unhandled;

        var detail = ProblemDetailsDetailPolicy.CreateDetail(
            statusCode,
            exception.Message,
            exception,
            httpContext,
            mapping?.DetailFactory,
            _options.IncludeExceptionDetailsPredicate);

        var problemDetails = new AspNetProblemDetails
        {
            Status = statusCode,
            Title = title,
            Type = type,
            Detail = detail,
            Instance = CreateInstance(httpContext),
        };

        AddExtensions(problemDetails, exception, httpContext);

        _options.CustomizeProblemDetails?.Invoke(httpContext, exception, problemDetails);

        return problemDetails;
    }

    private static string CreateInstance(HttpContext httpContext)
    {
        return string.IsNullOrWhiteSpace(httpContext.Request.Path.Value)
            ? "/"
            : httpContext.Request.Path.Value!;
    }

    private ExceptionProblemDetailsMapping? ResolveMapping(Exception exception)
    {
        var exceptionType = exception.GetType();

        var exactMatch = _options.Mappings.FirstOrDefault(mapping => mapping.ExceptionType == exceptionType);
        if (exactMatch is not null)
        {
            return exactMatch;
        }

        return _options.Mappings.FirstOrDefault(mapping => mapping.ExceptionType.IsAssignableFrom(exceptionType));
    }

    private void AddExtensions(AspNetProblemDetails problemDetails, Exception exception, HttpContext httpContext)
    {
        if (_options.IncludeTraceId)
        {
            var traceId = Activity.Current?.Id ?? httpContext.TraceIdentifier;
            AddExtension(problemDetails, _options.ExtensionNames.TraceId, traceId);
        }

        var correlationId = _options.CorrelationIdAccessor(httpContext);
        AddExtension(problemDetails, _options.ExtensionNames.CorrelationId, correlationId);

        if (_options.IncludeErrorCode &&
            exception is AtyaException atyaException &&
            !string.IsNullOrWhiteSpace(atyaException.ErrorCode))
        {
            AddExtension(problemDetails, _options.ExtensionNames.ErrorCode, atyaException.ErrorCode);
        }

        if (_options.IncludeExceptionMetadata &&
            exception is AtyaException exceptionWithMetadata &&
            exceptionWithMetadata.Metadata.Count > 0)
        {
            AddExtension(
                problemDetails,
                _options.ExtensionNames.Metadata,
                new Dictionary<string, object?>(exceptionWithMetadata.Metadata, StringComparer.Ordinal));
        }

        if (exception is ValidationException validationException)
        {
            AddExtension(problemDetails, _options.ExtensionNames.Errors, CreateValidationErrors(validationException));
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

    private Dictionary<string, object?>[] CreateValidationErrors(ValidationException validationException)
    {
        static void AddValidationMember(Dictionary<string, object?> item, string? name, object? value)
        {
            if (!string.IsNullOrWhiteSpace(name))
            {
                item[name.Trim()] = value;
            }
        }

        return validationException.Errors
            .Select(error =>
            {
                Dictionary<string, object?> item = new(StringComparer.Ordinal);

                AddValidationMember(item, _options.ExtensionNames.ValidationPropertyName, error.PropertyName);
                AddValidationMember(item, _options.ExtensionNames.ValidationMessage, error.Message);
                AddValidationMember(item, _options.ExtensionNames.ValidationErrorCode, error.ErrorCode);

                if (_options.IncludeValidationAttemptedValues)
                {
                    AddValidationMember(item, _options.ExtensionNames.ValidationAttemptedValue, error.AttemptedValue);
                }

                return item;
            })
            .ToArray();
    }
}
