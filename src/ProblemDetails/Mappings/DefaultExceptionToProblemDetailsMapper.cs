// <copyright file="DefaultExceptionToProblemDetailsMapper.cs" company="Atya">
// Copyright (c) Atya. All rights reserved.
// </copyright>

using Atya.Errors.Exceptions;
using Atya.Errors.ProblemDetails.Abstractions;
using Atya.Errors.ProblemDetails.Constants;
using Atya.Errors.ProblemDetails.Internal;
using Atya.Errors.ProblemDetails.Options;
using Atya.Foundation.Guards;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Options;
using AspNetProblemDetails = Microsoft.AspNetCore.Mvc.ProblemDetails;

namespace Atya.Errors.ProblemDetails.Mappings;

/// <summary>
/// Default mapper from exceptions to <see cref="ProblemDetails"/>. The mapper snapshots the options when it is
/// constructed; later changes to the options object have no effect on it.
/// </summary>
public sealed class DefaultExceptionToProblemDetailsMapper : IExceptionToProblemDetailsMapper
{
    private readonly ProblemDetailsPolicy _policy;

    /// <summary>
    /// Initializes a new instance of the <see cref="DefaultExceptionToProblemDetailsMapper"/> class.
    /// </summary>
    /// <param name="options">The configured options accessor.</param>
    public DefaultExceptionToProblemDetailsMapper(IOptions<AtyaProblemDetailsOptions> options)
    {
        ArgumentNullException.ThrowIfNull(options);
        _policy = ProblemDetailsPolicy.Create(options.Value);
    }

    private DefaultExceptionToProblemDetailsMapper(ProblemDetailsPolicy policy)
    {
        _policy = policy;
    }

    /// <summary>
    /// Creates a mapper from an options object, for tests and manual composition.
    /// </summary>
    /// <param name="options">The options to snapshot.</param>
    /// <returns>A new mapper.</returns>
    public static DefaultExceptionToProblemDetailsMapper Create(AtyaProblemDetailsOptions options)
    {
        return new DefaultExceptionToProblemDetailsMapper(ProblemDetailsPolicy.Create(options));
    }

    /// <inheritdoc />
    public AspNetProblemDetails Map(Exception exception, HttpContext httpContext)
    {
        Guard.AgainstNull(exception);
        Guard.AgainstNull(httpContext);

        var mapping = _policy.ResolveException(exception.GetType());

        var statusCode = mapping?.StatusCode ?? StatusCodes.Status500InternalServerError;
        var title = mapping?.Title ?? "Internal Server Error";
        var type = mapping?.Type ?? DefaultProblemTypeUris.Unhandled;

        var detail = ProblemDetailsDetailPolicy.CreateDetail(
            statusCode,
            exception.Message,
            exception,
            httpContext,
            mapping?.DetailFactory,
            _policy.IncludeExceptionDetailsPredicate);

        var problemDetails = new AspNetProblemDetails
        {
            Status = statusCode,
            Title = title,
            Type = type,
            Detail = detail,
            Instance = ProblemDetailsPolicy.CreateInstance(httpContext),
        };

        AddExtensions(problemDetails, exception, httpContext);

        _policy.Customize?.Invoke(new ProblemDetailsCustomizationContext(httpContext, problemDetails, exception, error: null));
        _policy.LegacyCustomize?.Invoke(httpContext, exception, problemDetails);

        return problemDetails;
    }

    private void AddExtensions(AspNetProblemDetails problemDetails, Exception exception, HttpContext httpContext)
    {
        var names = _policy.ExtensionNames;

        if (_policy.IncludeTraceId)
        {
            ProblemDetailsPolicy.AddExtension(problemDetails, names.TraceId, ProblemDetailsPolicy.ResolveTraceId(httpContext));
        }

        ProblemDetailsPolicy.AddExtension(problemDetails, names.CorrelationId, _policy.CorrelationIdAccessor(httpContext));

        if (_policy.IncludeErrorCode &&
            exception is AtyaException atyaException &&
            !string.IsNullOrWhiteSpace(atyaException.ErrorCode))
        {
            ProblemDetailsPolicy.AddExtension(problemDetails, names.ErrorCode, atyaException.ErrorCode);
        }

        if (_policy.IncludeExceptionMetadata &&
            exception is AtyaException exceptionWithMetadata &&
            exceptionWithMetadata.Metadata.Count > 0)
        {
            ProblemDetailsPolicy.AddExtension(
                problemDetails,
                names.Metadata,
                new Dictionary<string, object?>(exceptionWithMetadata.Metadata, StringComparer.Ordinal));
        }

        if (exception is ValidationException validationException)
        {
            ProblemDetailsPolicy.AddExtension(problemDetails, names.Errors, CreateValidationErrors(validationException));
        }
    }

    private Dictionary<string, object?>[] CreateValidationErrors(ValidationException validationException)
    {
        var names = _policy.ExtensionNames;

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

                AddValidationMember(item, names.ValidationPropertyName, error.PropertyName);
                AddValidationMember(item, names.ValidationMessage, error.Message);
                AddValidationMember(item, names.ValidationErrorCode, error.ErrorCode);

                if (_policy.IncludeValidationAttemptedValues)
                {
                    AddValidationMember(item, names.ValidationAttemptedValue, error.AttemptedValue);
                }

                return item;
            })
            .ToArray();
    }
}
