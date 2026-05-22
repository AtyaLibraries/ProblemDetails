// <copyright file="AtyaProblemDetailsPipelineTests.cs" company="Atya">
// Copyright (c) Atya. All rights reserved.
// </copyright>

using Atya.Errors.Exceptions;
using Atya.Errors.ProblemDetails.Extensions;
using System.Text.Json;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

namespace ProblemDetails.UnitTests;

public sealed class AtyaProblemDetailsPipelineTests
{
    [Fact]
    public async Task Pipeline_Should_Write_Sanitized_ProblemDetails_For_Handled_500_Exception()
    {
        using var host = await CreateHostAsync(static app =>
        {
            app.Run(_ => throw new InfrastructureException("Database password leaked."));
        });

        using var response = await host.GetTestClient().GetAsync("/");
        var body = await response.Content.ReadAsStringAsync();

        response.StatusCode.Should().Be(System.Net.HttpStatusCode.InternalServerError);
        response.Content.Headers.ContentType?.MediaType.Should().Be("application/problem+json");

        using var document = JsonDocument.Parse(body);
        var root = document.RootElement;

        root.GetProperty("type").GetString().Should().Be("urn:atya:problem-type:infrastructure-failure");
        root.GetProperty("title").GetString().Should().Be("Internal Server Error");
        root.GetProperty("status").GetInt32().Should().Be(500);
        root.GetProperty("detail").GetString().Should().Be("An unexpected error occurred.");
        root.GetProperty("instance").GetString().Should().Be("/");
        root.GetProperty("traceId").GetString().Should().NotBeNullOrWhiteSpace();
    }

    private static async Task<IHost> CreateHostAsync(Action<IApplicationBuilder> configure)
    {
        var host = new HostBuilder()
            .ConfigureWebHost(webBuilder =>
            {
                webBuilder.UseTestServer();
                webBuilder.ConfigureServices(static services => services.AddAtyaProblemDetails());
                webBuilder.Configure(app =>
                {
                    app.UseAtyaProblemDetails();
                    configure(app);
                });
            })
            .Build();

        await host.StartAsync();

        return host;
    }
}
