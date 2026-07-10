// <copyright file="ErrorProblemDetailsMapping.cs" company="Atya">
// Copyright (c) Atya. All rights reserved.
// </copyright>

using Atya.Foundation.Results;
using Microsoft.AspNetCore.Http;

namespace Atya.Errors.ProblemDetails.Options;

/// <summary>
/// Represents a mapping between a Results error kind and problem details defaults.
/// </summary>
/// <param name="Kind">The Results error kind to map.</param>
/// <param name="StatusCode">The HTTP status code.</param>
/// <param name="Title">The default problem title.</param>
/// <param name="Type">The default problem type URI.</param>
/// <param name="DetailFactory">An optional safe detail factory for the mapped error.</param>
#pragma warning disable SA1313 // Positional record parameters define PascalCase public API properties.
public sealed record ErrorProblemDetailsMapping(
    ErrorKind Kind,
    int StatusCode,
    string Title,
    string Type,
    Func<Error, HttpContext, string?>? DetailFactory = null);
#pragma warning restore SA1313
