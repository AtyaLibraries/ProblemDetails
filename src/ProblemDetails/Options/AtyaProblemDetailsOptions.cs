// <copyright file="AtyaProblemDetailsOptions.cs" company="Atya">
// Copyright (c) Atya. All rights reserved.
// </copyright>

using System.Collections.ObjectModel;
using Atya.Errors.Exceptions;
using Atya.Errors.ProblemDetails.Constants;
using Atya.Foundation.Guards;
using Microsoft.AspNetCore.Http;

namespace Atya.Errors.ProblemDetails.Options;

/// <summary>
/// Options for Atya problem details behavior.
/// </summary>
public sealed class AtyaProblemDetailsOptions
{
    private const int MinimumHttpStatusCode = 100;
    private const int MaximumHttpStatusCode = 599;

    private readonly List<ExceptionProblemDetailsMapping> _mappings = new List<ExceptionProblemDetailsMapping>();
    private readonly ReadOnlyCollection<ExceptionProblemDetailsMapping> _mappingsView;
    private Func<HttpContext, string?> _correlationIdAccessor = DefaultCorrelationIdAccessor;
    private Func<HttpContext, Exception, bool> _includeExceptionDetailsPredicate = static (_, _) => false;

    /// <summary>
    /// Initializes a new instance of the <see cref="AtyaProblemDetailsOptions"/> class.
    /// </summary>
    public AtyaProblemDetailsOptions()
    {
        _mappingsView = _mappings.AsReadOnly();
        AddDefaultMappings();
    }

    /// <summary>
    /// Gets the configured exception mappings.
    /// </summary>
    public IReadOnlyList<ExceptionProblemDetailsMapping> Mappings => _mappingsView;

    /// <summary>
    /// Gets the configured problem details extension member names.
    /// </summary>
    public ProblemDetailsExtensionNameOptions ExtensionNames { get; } = new();

    /// <summary>
    /// Gets or sets a value indicating whether trace id should be added to extensions.
    /// </summary>
    public bool IncludeTraceId { get; set; } = true;

    /// <summary>
    /// Gets or sets a value indicating whether exception error code should be added to extensions when available.
    /// </summary>
    public bool IncludeErrorCode { get; set; } = true;

    /// <summary>
    /// Gets or sets a value indicating whether exception metadata should be added to extensions when available.
    /// </summary>
    public bool IncludeExceptionMetadata { get; set; }

    /// <summary>
    /// Gets or sets a value indicating whether validation attempted values should be added to validation errors.
    /// </summary>
    public bool IncludeValidationAttemptedValues { get; set; }

    /// <summary>
    /// Gets or sets a function for resolving a correlation id.
    /// </summary>
    public Func<HttpContext, string?> CorrelationIdAccessor
    {
        get => _correlationIdAccessor;
        set => _correlationIdAccessor = value ?? throw new ArgumentNullException(nameof(value));
    }

    /// <summary>
    /// Gets or sets a predicate that decides whether raw exception details are included for unhandled exceptions.
    /// </summary>
    public Func<HttpContext, Exception, bool> IncludeExceptionDetailsPredicate
    {
        get => _includeExceptionDetailsPredicate;
        set => _includeExceptionDetailsPredicate = value ?? throw new ArgumentNullException(nameof(value));
    }

    /// <summary>
    /// Gets or sets an optional customization callback for final problem details mutation.
    /// </summary>
    public Action<HttpContext, Exception, Microsoft.AspNetCore.Mvc.ProblemDetails>? CustomizeProblemDetails { get; set; }

    /// <summary>
    /// Adds or replaces a mapping for the given exception type.
    /// </summary>
    /// <typeparam name="TException">The exception type.</typeparam>
    /// <param name="statusCode">The HTTP status code.</param>
    /// <param name="title">The problem title.</param>
    /// <param name="type">The problem type URI.</param>
    /// <returns>The current options instance.</returns>
    public AtyaProblemDetailsOptions Map<TException>(int statusCode, string title, string type)
        where TException : Exception
    {
        return MapCore<TException>(statusCode, title, type, detailFactory: null);
    }

    /// <summary>
    /// Adds or replaces a mapping for the given exception type with a safe detail factory.
    /// </summary>
    /// <typeparam name="TException">The exception type.</typeparam>
    /// <param name="statusCode">The HTTP status code.</param>
    /// <param name="title">The problem title.</param>
    /// <param name="type">The problem type URI.</param>
    /// <param name="detailFactory">A callback used to create the problem detail text.</param>
    /// <returns>The current options instance.</returns>
    public AtyaProblemDetailsOptions Map<TException>(
        int statusCode,
        string title,
        string type,
        Func<TException, HttpContext, string?> detailFactory)
        where TException : Exception
    {
        Guard.AgainstNull(detailFactory);

        return MapCore<TException>(
            statusCode,
            title,
            type,
            (exception, httpContext) => detailFactory((TException)exception, httpContext));
    }

    /// <summary>
    /// Removes a mapping for the given exception type.
    /// </summary>
    /// <typeparam name="TException">The exception type.</typeparam>
    /// <returns>The current options instance.</returns>
    public AtyaProblemDetailsOptions RemoveMapping<TException>()
        where TException : Exception
    {
        var exceptionType = typeof(TException);

        for (var i = _mappings.Count - 1; i >= 0; i--)
        {
            if (_mappings[i].ExceptionType == exceptionType)
            {
                _mappings.RemoveAt(i);
            }
        }

        return this;
    }

    /// <summary>
    /// Removes all configured exception mappings.
    /// </summary>
    /// <returns>The current options instance.</returns>
    public AtyaProblemDetailsOptions ClearMappings()
    {
        _mappings.Clear();

        return this;
    }

    private static string? DefaultCorrelationIdAccessor(HttpContext httpContext)
    {
        if (httpContext.Request.Headers.TryGetValue("X-Correlation-ID", out var values))
        {
            var value = values.ToString();
            return string.IsNullOrWhiteSpace(value) ? null : value;
        }

        return null;
    }

    private static void ValidateHttpStatusCode(int statusCode)
    {
        if (statusCode < MinimumHttpStatusCode || statusCode > MaximumHttpStatusCode)
        {
            throw new ArgumentOutOfRangeException(
                nameof(statusCode),
                statusCode,
                "HTTP status code must be between 100 and 599.");
        }
    }

    private AtyaProblemDetailsOptions MapCore<TException>(
        int statusCode,
        string title,
        string type,
        Func<Exception, HttpContext, string?>? detailFactory)
        where TException : Exception
    {
        ValidateHttpStatusCode(statusCode);
        Guard.AgainstNullOrWhiteSpace(title);
        Guard.AgainstNullOrWhiteSpace(type);

        _mappings.RemoveAll(static mapping => mapping.ExceptionType == typeof(TException));
        _mappings.Insert(
            0,
            new ExceptionProblemDetailsMapping(typeof(TException), statusCode, title.Trim(), type.Trim(), detailFactory));

        return this;
    }

    private void AddDefaultMappings()
    {
        _mappings.Add(new ExceptionProblemDetailsMapping(
            typeof(ValidationException),
            StatusCodes.Status400BadRequest,
            "Bad Request",
            DefaultProblemTypeUris.Validation));

        _mappings.Add(new ExceptionProblemDetailsMapping(
            typeof(BusinessRuleViolationException),
            StatusCodes.Status422UnprocessableEntity,
            "Unprocessable Entity",
            DefaultProblemTypeUris.BusinessRuleViolation));

        _mappings.Add(new ExceptionProblemDetailsMapping(
            typeof(UnauthorizedException),
            StatusCodes.Status401Unauthorized,
            "Unauthorized",
            DefaultProblemTypeUris.Unauthorized));

        _mappings.Add(new ExceptionProblemDetailsMapping(
            typeof(ForbiddenException),
            StatusCodes.Status403Forbidden,
            "Forbidden",
            DefaultProblemTypeUris.Forbidden));

        _mappings.Add(new ExceptionProblemDetailsMapping(
            typeof(NotFoundException),
            StatusCodes.Status404NotFound,
            "Not Found",
            DefaultProblemTypeUris.NotFound));

        _mappings.Add(new ExceptionProblemDetailsMapping(
            typeof(ConflictException),
            StatusCodes.Status409Conflict,
            "Conflict",
            DefaultProblemTypeUris.Conflict));

        _mappings.Add(new ExceptionProblemDetailsMapping(
            typeof(ConcurrencyException),
            StatusCodes.Status409Conflict,
            "Conflict",
            DefaultProblemTypeUris.Concurrency));

        _mappings.Add(new ExceptionProblemDetailsMapping(
            typeof(InfrastructureException),
            StatusCodes.Status500InternalServerError,
            "Internal Server Error",
            DefaultProblemTypeUris.Infrastructure));
    }
}
