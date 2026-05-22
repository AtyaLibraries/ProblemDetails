// <copyright file="ApplicationBuilderExtensions.cs" company="Atya">
// Copyright (c) Atya. All rights reserved.
// </copyright>

using Atya.Foundation.Guards;
using Microsoft.AspNetCore.Builder;

namespace Atya.Errors.ProblemDetails.Extensions;

/// <summary>
/// Application builder extensions for Atya problem details.
/// </summary>
public static class ApplicationBuilderExtensions
{
    /// <summary>
    /// Adds the middleware pipeline hook that writes mapped problem details responses.
    /// </summary>
    /// <param name="app">The application builder to configure.</param>
    /// <returns>The same application builder instance.</returns>
    public static IApplicationBuilder UseAtyaProblemDetails(this IApplicationBuilder app)
    {
        Guard.AgainstNull(app);

        app.UseExceptionHandler();

        return app;
    }
}
