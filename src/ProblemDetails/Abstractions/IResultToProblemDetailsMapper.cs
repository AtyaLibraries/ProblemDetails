// <copyright file="IResultToProblemDetailsMapper.cs" company="Atya">
// Copyright (c) Atya. All rights reserved.
// </copyright>

using Atya.Foundation.Results;
using Microsoft.AspNetCore.Http;
using AspNetProblemDetails = Microsoft.AspNetCore.Mvc.ProblemDetails;

namespace Atya.Errors.ProblemDetails.Abstractions;

/// <summary>
/// Maps failed Results errors into RFC 9457 style problem details payloads.
/// </summary>
public interface IResultToProblemDetailsMapper
{
    /// <summary>
    /// Maps a failed result into problem details.
    /// </summary>
    /// <param name="result">The failed result to map.</param>
    /// <param name="httpContext">The current HTTP context.</param>
    /// <returns>The mapped problem details payload.</returns>
    /// <exception cref="InvalidOperationException"><paramref name="result"/> is successful.</exception>
    public AspNetProblemDetails Map(Result result, HttpContext httpContext);

    /// <summary>
    /// Maps a failed typed result into problem details.
    /// </summary>
    /// <typeparam name="TValue">The success value type.</typeparam>
    /// <param name="result">The failed result to map.</param>
    /// <param name="httpContext">The current HTTP context.</param>
    /// <returns>The mapped problem details payload.</returns>
    /// <exception cref="InvalidOperationException"><paramref name="result"/> is successful.</exception>
    public AspNetProblemDetails Map<TValue>(Result<TValue> result, HttpContext httpContext);

    /// <summary>
    /// Maps an error into problem details.
    /// </summary>
    /// <param name="resultError">The error to map.</param>
    /// <param name="httpContext">The current HTTP context.</param>
    /// <returns>The mapped problem details payload.</returns>
    public AspNetProblemDetails Map(Error resultError, HttpContext httpContext);
}
