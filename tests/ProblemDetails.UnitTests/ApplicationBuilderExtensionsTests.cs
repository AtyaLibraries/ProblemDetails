// <copyright file="ApplicationBuilderExtensionsTests.cs" company="Atya">
// Copyright (c) Atya. All rights reserved.
// </copyright>

using Atya.Errors.ProblemDetails.Extensions;
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.DependencyInjection;
using ApplicationBuilderExtensions = Atya.Errors.ProblemDetails.Extensions.ApplicationBuilderExtensions;

namespace ProblemDetails.UnitTests;

public sealed class ApplicationBuilderExtensionsTests
{
    [Fact]
    public void UseAtyaProblemDetails_Should_Throw_When_App_Is_Null()
    {
        var act = () => ApplicationBuilderExtensions.UseAtyaProblemDetails(null!);

        act.Should().Throw<ArgumentNullException>();
    }

    [Fact]
    public void UseAtyaProblemDetails_Should_Return_ApplicationBuilder()
    {
        var services = new ServiceCollection();
        services.AddAtyaProblemDetails();

        var serviceProvider = services.BuildServiceProvider();
        var appBuilder = new ApplicationBuilder(serviceProvider);

        var result = appBuilder.UseAtyaProblemDetails();

        result.Should().BeSameAs(appBuilder);
    }
}
