// <copyright file="ProblemDetailsPolicy.cs" company="Atya">
// Copyright (c) Atya. All rights reserved.
// </copyright>

using System.Collections.Frozen;
using System.Diagnostics;
using Atya.Errors.ProblemDetails.Options;
using Atya.Foundation.Results;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Options;
using AspNetProblemDetails = Microsoft.AspNetCore.Mvc.ProblemDetails;

namespace Atya.Errors.ProblemDetails.Internal;

/// <summary>
/// An immutable snapshot of <see cref="AtyaProblemDetailsOptions"/> taken when a mapper is constructed.
/// </summary>
internal sealed class ProblemDetailsPolicy
{
    private readonly ExceptionProblemDetailsMapping[] _exceptionMappings;
    private readonly FrozenDictionary<ErrorKind, ErrorProblemDetailsMapping> _errorMappings;

    private ProblemDetailsPolicy(AtyaProblemDetailsOptions options)
    {
        // Most specific first: a deeper type is closer to the thrown type on its single inheritance chain, so
        // registration order can never change which mapping wins.
        _exceptionMappings = options.Mappings
            .OrderByDescending(static mapping => InheritanceDepth(mapping.ExceptionType))
            .ToArray();
        _errorMappings = options.ErrorMappings.ToFrozenDictionary(static mapping => mapping.Kind);
        ExtensionNames = options.ExtensionNames.Clone();
        IncludeTraceId = options.IncludeTraceId;
        IncludeErrorCode = options.IncludeErrorCode;
        IncludeExceptionMetadata = options.IncludeExceptionMetadata;
        IncludeValidationAttemptedValues = options.IncludeValidationAttemptedValues;
        CorrelationIdAccessor = options.CorrelationIdAccessor;
        IncludeExceptionDetailsPredicate = options.IncludeExceptionDetailsPredicate;
        Customize = options.Customize;
#pragma warning disable CS0618 // The obsolete sugar is still honoured on the exception path.
        LegacyCustomize = options.CustomizeProblemDetails;
#pragma warning restore CS0618
    }

    internal ProblemDetailsExtensionNameOptions ExtensionNames { get; }

    internal bool IncludeTraceId { get; }

    internal bool IncludeErrorCode { get; }

    internal bool IncludeExceptionMetadata { get; }

    internal bool IncludeValidationAttemptedValues { get; }

    internal Func<HttpContext, string?> CorrelationIdAccessor { get; }

    internal Func<HttpContext, Exception, bool> IncludeExceptionDetailsPredicate { get; }

    internal Action<ProblemDetailsCustomizationContext>? Customize { get; }

    internal Action<HttpContext, Exception, AspNetProblemDetails>? LegacyCustomize { get; }

    internal static ProblemDetailsPolicy Create(AtyaProblemDetailsOptions options)
    {
        ArgumentNullException.ThrowIfNull(options);

        var failures = ProblemDetailsOptionsValidator.Collect(options);
        if (failures.Count > 0)
        {
            throw new OptionsValidationException(nameof(AtyaProblemDetailsOptions), typeof(AtyaProblemDetailsOptions), failures);
        }

        return new ProblemDetailsPolicy(options);
    }

    internal static string CreateInstance(HttpContext httpContext)
    {
        return string.IsNullOrWhiteSpace(httpContext.Request.Path.Value)
            ? "/"
            : httpContext.Request.Path.Value!;
    }

    /// <summary>
    /// Returns the W3C trace id (32 hex characters) of the current activity, or the request's trace identifier.
    /// A hierarchical <see cref="Activity.Id"/> is never emitted.
    /// </summary>
    /// <param name="httpContext">The current HTTP context.</param>
    /// <returns>The trace id.</returns>
    internal static string? ResolveTraceId(HttpContext httpContext)
    {
        var activity = Activity.Current;
        if (activity is { IdFormat: ActivityIdFormat.W3C } && activity.TraceId != default)
        {
            return activity.TraceId.ToString();
        }

        return httpContext.TraceIdentifier;
    }

    internal static void AddExtension(AspNetProblemDetails problemDetails, string? name, object? value)
    {
        if (!string.IsNullOrWhiteSpace(name) &&
            value is not null &&
            (value is not string text || !string.IsNullOrWhiteSpace(text)))
        {
            problemDetails.Extensions[name.Trim()] = value;
        }
    }

    internal ExceptionProblemDetailsMapping? ResolveException(Type exceptionType)
    {
        foreach (var mapping in _exceptionMappings)
        {
            if (mapping.ExceptionType.IsAssignableFrom(exceptionType))
            {
                return mapping;
            }
        }

        return null;
    }

    internal ErrorProblemDetailsMapping? ResolveError(ErrorKind kind)
    {
        return _errorMappings.TryGetValue(kind, out var mapping) ? mapping : null;
    }

    private static int InheritanceDepth(Type type)
    {
        var depth = 0;
        for (var current = type.BaseType; current is not null; current = current.BaseType)
        {
            depth++;
        }

        return depth;
    }
}
