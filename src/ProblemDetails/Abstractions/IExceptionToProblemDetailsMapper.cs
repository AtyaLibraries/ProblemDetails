// <copyright file="IExceptionToProblemDetailsMapper.cs" company="Atya">
// Copyright (c) Atya. All rights reserved.
// </copyright>

using Microsoft.AspNetCore.Http;
using AspNetProblemDetails = Microsoft.AspNetCore.Mvc.ProblemDetails;

namespace Atya.Errors.ProblemDetails.Abstractions;

/// <summary>
/// Maps exceptions to ASP.NET Core ProblemDetails instances.
/// </summary>
public interface IExceptionToProblemDetailsMapper
{
    /// <summary>
    /// Maps an exception to a ProblemDetails instance.
    /// </summary>
    /// <param name="exception">The exception to map.</param>
    /// <param name="httpContext">The current HTTP context.</param>
    /// <returns>A mapped ProblemDetails instance.</returns>
    public AspNetProblemDetails Map(Exception exception, HttpContext httpContext);
}
