// <copyright file="ProblemDetailsCustomizationContext.cs" company="Atya">
// Copyright (c) Atya. All rights reserved.
// </copyright>

using Atya.Foundation.Guards;
using Atya.Foundation.Results;
using Microsoft.AspNetCore.Http;
using AspNetProblemDetails = Microsoft.AspNetCore.Mvc.ProblemDetails;

namespace Atya.Errors.ProblemDetails.Options;

/// <summary>
/// Describes one problem details response being customized. Exactly one of <see cref="Exception"/> and
/// <see cref="Error"/> is set.
/// </summary>
public sealed class ProblemDetailsCustomizationContext
{
    /// <summary>
    /// Initializes a new instance of the <see cref="ProblemDetailsCustomizationContext"/> class.
    /// </summary>
    /// <param name="httpContext">The current HTTP context.</param>
    /// <param name="problemDetails">The problem details being produced.</param>
    /// <param name="exception">The exception being mapped, on the exception path.</param>
    /// <param name="error">The error being mapped, on the result path.</param>
    public ProblemDetailsCustomizationContext(
        HttpContext httpContext,
        AspNetProblemDetails problemDetails,
        Exception? exception,
        Error? error)
    {
        HttpContext = Guard.AgainstNull(httpContext);
        ProblemDetails = Guard.AgainstNull(problemDetails);
        Exception = exception;
        Error = error;
    }

    /// <summary>
    /// Gets the current HTTP context.
    /// </summary>
    public HttpContext HttpContext { get; }

    /// <summary>
    /// Gets the exception being mapped, or <see langword="null"/> on the result path.
    /// </summary>
    public Exception? Exception { get; }

    /// <summary>
    /// Gets the error being mapped, or <see langword="null"/> on the exception path.
    /// </summary>
    public Error? Error { get; }

    /// <summary>
    /// Gets the problem details being produced. Customizers may change any member, including Atya extensions.
    /// </summary>
    public AspNetProblemDetails ProblemDetails { get; }
}
