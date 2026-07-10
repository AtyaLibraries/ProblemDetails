// <copyright file="ServiceCollectionExtensions.cs" company="Atya">
// Copyright (c) Atya. All rights reserved.
// </copyright>

using Atya.Errors.ProblemDetails.Abstractions;
using Atya.Errors.ProblemDetails.Handlers;
using Atya.Errors.ProblemDetails.Mappings;
using Atya.Errors.ProblemDetails.Options;
using Atya.Foundation.Guards;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace Atya.Errors.ProblemDetails.Extensions;

/// <summary>
/// Service collection extensions for Atya problem details.
/// </summary>
public static class ServiceCollectionExtensions
{
    /// <summary>
    /// Registers the services required to map exceptions into problem details payloads.
    /// </summary>
    /// <param name="services">The service collection to add registrations to.</param>
    /// <param name="configure">An optional callback for customizing problem details options.</param>
    /// <returns>The same service collection instance.</returns>
    public static IServiceCollection AddAtyaProblemDetails(
        this IServiceCollection services,
        Action<AtyaProblemDetailsOptions>? configure = null)
    {
        Guard.AgainstNull(services);

        services.AddOptions<AtyaProblemDetailsOptions>();

        if (configure is not null)
        {
            services.Configure(configure);
        }

        services.AddProblemDetails();
        services.AddExceptionHandler<AtyaProblemDetailsExceptionHandler>();

        services.TryAddSingleton<IExceptionToProblemDetailsMapper, DefaultExceptionToProblemDetailsMapper>();
        services.TryAddSingleton<IResultToProblemDetailsMapper, DefaultResultToProblemDetailsMapper>();

        return services;
    }
}
