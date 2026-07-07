// <copyright file="ExceptionProblemDetailsMapping.cs" company="Atya">
// Copyright (c) Atya. All rights reserved.
// </copyright>

using Microsoft.AspNetCore.Http;

namespace Atya.Errors.ProblemDetails.Options;

/// <summary>
/// Represents a mapping between an exception type and problem details defaults.
/// </summary>
/// <param name="ExceptionType">The exception type to map.</param>
/// <param name="StatusCode">The HTTP status code.</param>
/// <param name="Title">The default problem title.</param>
/// <param name="Type">The default problem type URI.</param>
/// <param name="DetailFactory">An optional safe detail factory for the mapped exception.</param>
#pragma warning disable SA1313 // Positional record parameters define PascalCase public API properties.
public sealed record ExceptionProblemDetailsMapping(
    Type ExceptionType,
    int StatusCode,
    string Title,
    string Type,
    Func<Exception, HttpContext, string?>? DetailFactory = null);
#pragma warning restore SA1313
