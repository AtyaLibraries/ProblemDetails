// <copyright file="ServiceCollectionExtensionsTests.cs" company="Atya">
// Copyright (c) Atya. All rights reserved.
// </copyright>

using Atya.Errors.ProblemDetails.Abstractions;
using Atya.Errors.ProblemDetails.Extensions;
using Atya.Errors.ProblemDetails.Options;
using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;

namespace ProblemDetails.UnitTests;

public sealed class ServiceCollectionExtensionsTests
{
    [Fact]
    public void AddAtyaProblemDetails_Should_Register_Core_Services()
    {
        var services = new ServiceCollection();

        services.AddOptions();
        services.AddAtyaProblemDetails();

        using var serviceProvider = services.BuildServiceProvider();

        serviceProvider.GetRequiredService<IOptions<AtyaProblemDetailsOptions>>().Value.Should().NotBeNull();
        serviceProvider.GetRequiredService<IExceptionToProblemDetailsMapper>().Should().NotBeNull();
        serviceProvider.GetServices<IExceptionHandler>().Should().NotBeEmpty();
        serviceProvider.GetRequiredService<IProblemDetailsService>().Should().NotBeNull();
    }

    [Fact]
    public void AddAtyaProblemDetails_Should_Apply_Configuration()
    {
        var services = new ServiceCollection();

        services.AddOptions();
        services.AddAtyaProblemDetails(options =>
        {
            options.IncludeExceptionMetadata = true;
        });

        using var serviceProvider = services.BuildServiceProvider();
        var options = serviceProvider.GetRequiredService<IOptions<AtyaProblemDetailsOptions>>().Value;

        options.IncludeExceptionMetadata.Should().BeTrue();
    }

    [Fact]
    public void AddAtyaProblemDetails_Should_Compose_Options_Configuration()
    {
        var services = new ServiceCollection();

        services.Configure<AtyaProblemDetailsOptions>(options =>
        {
            options.IncludeTraceId = false;
        });

        services.AddAtyaProblemDetails(options =>
        {
            options.IncludeExceptionMetadata = true;
        });

        using var serviceProvider = services.BuildServiceProvider();
        var options = serviceProvider.GetRequiredService<IOptions<AtyaProblemDetailsOptions>>().Value;

        options.IncludeTraceId.Should().BeFalse();
        options.IncludeExceptionMetadata.Should().BeTrue();
    }
}
