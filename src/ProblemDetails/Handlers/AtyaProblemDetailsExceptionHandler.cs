// <copyright file="AtyaProblemDetailsExceptionHandler.cs" company="Atya">
// Copyright (c) Atya. All rights reserved.
// </copyright>

using System.Text.Json;
using Atya.Errors.ProblemDetails.Abstractions;
using Atya.Foundation.Guards;
using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.Http;

namespace Atya.Errors.ProblemDetails.Handlers;

/// <summary>
/// ASP.NET Core exception handler that writes RFC 9457 style problem details responses.
/// </summary>
public sealed class AtyaProblemDetailsExceptionHandler : IExceptionHandler
{
    private static readonly JsonSerializerOptions s_jsonSerializerOptions = new(JsonSerializerDefaults.Web);

    private readonly IExceptionToProblemDetailsMapper _mapper;
    private readonly IProblemDetailsService _problemDetailsService;

    /// <summary>
    /// Initializes a new instance of the <see cref="AtyaProblemDetailsExceptionHandler"/> class.
    /// </summary>
    /// <param name="mapper">The mapper used to create <see cref="ProblemDetails"/>.</param>
    /// <param name="problemDetailsService">The ASP.NET Core problem details service.</param>
    public AtyaProblemDetailsExceptionHandler(
        IExceptionToProblemDetailsMapper mapper,
        IProblemDetailsService problemDetailsService)
    {
        _mapper = mapper ?? throw new ArgumentNullException(nameof(mapper));
        _problemDetailsService = problemDetailsService ?? throw new ArgumentNullException(nameof(problemDetailsService));
    }

    /// <inheritdoc />
    public async ValueTask<bool> TryHandleAsync(HttpContext httpContext, Exception exception, CancellationToken cancellationToken)
    {
        Guard.AgainstNull(httpContext);
        Guard.AgainstNull(exception);

        var problemDetails = _mapper.Map(exception, httpContext);

        httpContext.Response.StatusCode = problemDetails.Status ?? StatusCodes.Status500InternalServerError;

        var written = await _problemDetailsService.TryWriteAsync(new ProblemDetailsContext
        {
            HttpContext = httpContext,
            ProblemDetails = problemDetails,
            Exception = exception,
        });

        if (written)
        {
            return true;
        }

        httpContext.Response.ContentType = "application/problem+json";
        await httpContext.Response.WriteAsync(
            JsonSerializer.Serialize(problemDetails, s_jsonSerializerOptions),
            cancellationToken);

        return true;
    }
}
