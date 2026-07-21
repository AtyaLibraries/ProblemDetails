// <copyright file="AtyaProblemDetailsPipelineTests.cs" company="Atya">
// Copyright (c) Atya. All rights reserved.
// </copyright>

using Atya.Errors.Exceptions;
using Atya.Errors.ProblemDetails.Abstractions;
using Atya.Errors.ProblemDetails.Extensions;
using Atya.Errors.ProblemDetails.Options;
using Atya.Foundation.Results;
using System.Text.Json;
using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
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
        }, TestContext.Current.CancellationToken);

        using var response = await host.GetTestClient().GetAsync("/", TestContext.Current.CancellationToken);
        var body = await response.Content.ReadAsStringAsync(TestContext.Current.CancellationToken);

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

    [Fact]
    public async Task Pipeline_Should_Not_Serialize_Result_Error_Markers_For_Default_500()
    {
        const string marker = "ATYA014_SYNTHETIC_PIPELINE_MARKER";
        using var host = await CreateResultHostAsync(marker, configureOptions: null);

        using var response = await host.GetTestClient().GetAsync("/", TestContext.Current.CancellationToken);
        var body = await response.Content.ReadAsStringAsync(TestContext.Current.CancellationToken);

        response.StatusCode.Should().Be(System.Net.HttpStatusCode.InternalServerError);
        response.Content.Headers.ContentType?.MediaType.Should().Be("application/problem+json");
        body.Should().NotContain(marker);

        using var document = JsonDocument.Parse(body);
        document.RootElement.GetProperty("detail").GetString().Should().Be("An unexpected error occurred.");
        document.RootElement.TryGetProperty("errorCode", out _).Should().BeFalse();
        document.RootElement.TryGetProperty("errors", out _).Should().BeFalse();
    }

    [Fact]
    public async Task Pipeline_Should_Isolate_Default_And_Reviewed_Override_Between_Hosts()
    {
        const string marker = "ATYA014_REVIEWED_PIPELINE_DETAIL";
        using var defaultHost = await CreateResultHostAsync(marker, configureOptions: null);
        using var overrideHost = await CreateResultHostAsync(
            marker,
            options => options.MapError(
                ErrorKind.Unexpected,
                StatusCodes.Status500InternalServerError,
                "Internal Server Error",
                "urn:test:internal-server-error",
                static (error, _) => error.Message));

        using var defaultResponse = await defaultHost.GetTestClient()
            .GetAsync("/", TestContext.Current.CancellationToken);
        using var overrideResponse = await overrideHost.GetTestClient()
            .GetAsync("/", TestContext.Current.CancellationToken);
        var defaultBody = await defaultResponse.Content.ReadAsStringAsync(TestContext.Current.CancellationToken);
        var overrideBody = await overrideResponse.Content.ReadAsStringAsync(TestContext.Current.CancellationToken);

        defaultBody.Should().NotContain(marker);
        overrideBody.Should().Contain(marker);
    }

    private static async Task<IHost> CreateHostAsync(
        Action<IApplicationBuilder> configure,
        CancellationToken cancellationToken,
        Action<AtyaProblemDetailsOptions>? configureOptions = null)
    {
        var host = new HostBuilder()
            .ConfigureWebHost(webBuilder =>
            {
                webBuilder.UseTestServer();
                webBuilder.ConfigureServices(services => services.AddAtyaProblemDetails(configureOptions));
                webBuilder.Configure(app =>
                {
                    app.UseAtyaProblemDetails();
                    configure(app);
                });
            })
            .Build();

        await host.StartAsync(cancellationToken);

        return host;
    }

    private static Task<IHost> CreateResultHostAsync(
        string marker,
        Action<AtyaProblemDetailsOptions>? configureOptions)
    {
        return CreateHostAsync(
            app =>
            {
                app.Run(async httpContext =>
                {
                    var mapper = httpContext.RequestServices.GetRequiredService<IResultToProblemDetailsMapper>();
                    var problemDetailsService = httpContext.RequestServices.GetRequiredService<IProblemDetailsService>();
                    var error = new Error(
                        marker,
                        marker,
                        target: marker,
                        details:
                        [
                            new Error(marker, marker, marker, kind: ErrorKind.Validation),
                        ],
                        kind: ErrorKind.Unexpected);
                    var problemDetails = mapper.Map(error, httpContext);

                    httpContext.Response.StatusCode = problemDetails.Status ?? StatusCodes.Status500InternalServerError;
                    var written = await problemDetailsService.TryWriteAsync(new ProblemDetailsContext
                    {
                        HttpContext = httpContext,
                        ProblemDetails = problemDetails,
                    });

                    if (!written)
                    {
                        throw new InvalidOperationException("Problem details response was not written.");
                    }
                });
            },
            TestContext.Current.CancellationToken,
            configureOptions);
    }
}
