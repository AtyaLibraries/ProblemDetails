// <copyright file="ProblemDetailsDetailPolicy.cs" company="Atya">
// Copyright (c) Atya. All rights reserved.
// </copyright>

using Microsoft.AspNetCore.Http;

namespace Atya.Errors.ProblemDetails.Mappings;

internal static class ProblemDetailsDetailPolicy
{
    internal const string UnexpectedErrorDetail = "An unexpected error occurred.";

    internal static bool IsServerError(int statusCode)
    {
        return statusCode >= StatusCodes.Status500InternalServerError && statusCode <= 599;
    }

    internal static string? CreateDetail<TSource>(
        int statusCode,
        string sourceDetail,
        TSource source,
        HttpContext httpContext,
        Func<TSource, HttpContext, string?>? detailFactory,
        Func<HttpContext, TSource, bool>? includeDetailsPredicate = null)
    {
        if (detailFactory is not null)
        {
            return detailFactory(source, httpContext);
        }

        if (!IsServerError(statusCode) || includeDetailsPredicate?.Invoke(httpContext, source) == true)
        {
            return sourceDetail;
        }

        return UnexpectedErrorDetail;
    }
}
