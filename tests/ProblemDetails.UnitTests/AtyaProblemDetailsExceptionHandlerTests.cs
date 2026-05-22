// <copyright file="AtyaProblemDetailsExceptionHandlerTests.cs" company="Atya">
// Copyright (c) Atya. All rights reserved.
// </copyright>

using System.Text;
using Atya.Errors.ProblemDetails.Abstractions;
using Atya.Errors.ProblemDetails.Handlers;
using Atya.Errors.ProblemDetails.Models;
using Microsoft.AspNetCore.Http;
using Atya.Governance.Testing.Json;
using AspNetProblemDetails = Microsoft.AspNetCore.Mvc.ProblemDetails;

namespace ProblemDetails.UnitTests;

public sealed class AtyaProblemDetailsExceptionHandlerTests
{
    [Fact]
    public void Constructor_Should_Throw_When_Mapper_Is_Null()
    {
        var service = new FakeProblemDetailsService(shouldWrite: true);

        var act = () => new AtyaProblemDetailsExceptionHandler(null!, service);

        act.Should().Throw<ArgumentNullException>();
    }

    [Fact]
    public void Constructor_Should_Throw_When_ProblemDetailsService_Is_Null()
    {
        var mapper = new FakeExceptionToProblemDetailsMapper(new AspNetProblemDetails());

        var act = () => new AtyaProblemDetailsExceptionHandler(mapper, null!);

        act.Should().Throw<ArgumentNullException>();
    }

    [Fact]
    public async Task TryHandleAsync_Should_Return_True_And_Use_ProblemDetailsService_When_Service_Writes_Response()
    {
        var expectedProblemDetails = new AspNetProblemDetails
        {
            Status = StatusCodes.Status404NotFound,
            Title = "Not Found",
            Detail = "Customer was not found.",
            Type = "urn:test:not-found",
            Instance = "/customers/42"
        };

        var mapper = new FakeExceptionToProblemDetailsMapper(expectedProblemDetails);
        var service = new FakeProblemDetailsService(shouldWrite: true);
        var handler = new AtyaProblemDetailsExceptionHandler(mapper, service);
        var httpContext = CreateHttpContext();
        var exception = new InvalidOperationException("boom");

        var result = await handler.TryHandleAsync(httpContext, exception, CancellationToken.None);

        result.Should().BeTrue();
        mapper.LastException.Should().BeSameAs(exception);
        mapper.LastHttpContext.Should().BeSameAs(httpContext);
        service.TryWriteCallCount.Should().Be(1);
        service.LastContext.Should().NotBeNull();
        service.LastContext!.ProblemDetails.Should().BeSameAs(expectedProblemDetails);
        service.LastContext.Exception.Should().BeSameAs(exception);
        httpContext.Response.StatusCode.Should().Be(StatusCodes.Status404NotFound);
    }

    [Fact]
    public async Task TryHandleAsync_Should_Fallback_To_Json_Response_When_ProblemDetailsService_Does_Not_Write()
    {
        var expectedProblemDetails = new AspNetProblemDetails
        {
            Status = StatusCodes.Status409Conflict,
            Title = "Conflict",
            Detail = "Email already exists.",
            Type = "urn:test:conflict",
            Instance = "/customers"
        };

        var mapper = new FakeExceptionToProblemDetailsMapper(expectedProblemDetails);
        var service = new FakeProblemDetailsService(shouldWrite: false);
        var handler = new AtyaProblemDetailsExceptionHandler(mapper, service);
        var httpContext = CreateHttpContext();
        var exception = new InvalidOperationException("boom");

        var result = await handler.TryHandleAsync(httpContext, exception, CancellationToken.None);

        result.Should().BeTrue();
        httpContext.Response.StatusCode.Should().Be(StatusCodes.Status409Conflict);
        httpContext.Response.ContentType.Should().Be("application/problem+json");

        httpContext.Response.Body.Position = 0;
        using var reader = new StreamReader(httpContext.Response.Body, Encoding.UTF8);
        var responseBody = await reader.ReadToEndAsync();

        JsonAssert.Equal(
            """
            {
              "type": "urn:test:conflict",
              "title": "Conflict",
              "status": 409,
              "detail": "Email already exists.",
              "instance": "/customers"
            }
            """,
            responseBody);
    }

    [Fact]
    public async Task TryHandleAsync_Should_Fallback_To_Web_Json_Casing_For_Extension_Values()
    {
        var expectedProblemDetails = new AspNetProblemDetails
        {
            Status = StatusCodes.Status400BadRequest,
            Title = "Bad Request",
            Detail = "Validation failed.",
            Type = "urn:test:validation",
            Instance = "/customers"
        };

        expectedProblemDetails.Extensions["errors"] = new[]
        {
            new ValidationProblemError("Email", "Email is required.", "validation.required", null)
        };

        var mapper = new FakeExceptionToProblemDetailsMapper(expectedProblemDetails);
        var service = new FakeProblemDetailsService(shouldWrite: false);
        var handler = new AtyaProblemDetailsExceptionHandler(mapper, service);
        var httpContext = CreateHttpContext();
        var exception = new InvalidOperationException("boom");

        _ = await handler.TryHandleAsync(httpContext, exception, CancellationToken.None);

        httpContext.Response.Body.Position = 0;
        using var reader = new StreamReader(httpContext.Response.Body, Encoding.UTF8);
        var responseBody = await reader.ReadToEndAsync();

        JsonAssert.Equal(
            """
            {
              "type": "urn:test:validation",
              "title": "Bad Request",
              "status": 400,
              "detail": "Validation failed.",
              "instance": "/customers",
              "errors": [
                {
                  "propertyName": "Email",
                  "message": "Email is required.",
                  "errorCode": "validation.required"
                }
              ]
            }
            """,
            responseBody);
    }

    [Fact]
    public async Task TryHandleAsync_Should_Throw_When_HttpContext_Is_Null()
    {
        var mapper = new FakeExceptionToProblemDetailsMapper(new AspNetProblemDetails());
        var service = new FakeProblemDetailsService(shouldWrite: true);
        var handler = new AtyaProblemDetailsExceptionHandler(mapper, service);

        var act = async () => await handler.TryHandleAsync(null!, new InvalidOperationException("boom"), CancellationToken.None);

        await act.Should().ThrowAsync<ArgumentNullException>();
    }

    [Fact]
    public async Task TryHandleAsync_Should_Throw_When_Exception_Is_Null()
    {
        var mapper = new FakeExceptionToProblemDetailsMapper(new AspNetProblemDetails());
        var service = new FakeProblemDetailsService(shouldWrite: true);
        var handler = new AtyaProblemDetailsExceptionHandler(mapper, service);
        var httpContext = CreateHttpContext();

        var act = async () => await handler.TryHandleAsync(httpContext, null!, CancellationToken.None);

        await act.Should().ThrowAsync<ArgumentNullException>();
    }

    private static DefaultHttpContext CreateHttpContext()
    {
        return new DefaultHttpContext
        {
            Response =
            {
                Body = new MemoryStream()
            }
        };
    }

    private sealed class FakeExceptionToProblemDetailsMapper : IExceptionToProblemDetailsMapper
    {
        private readonly AspNetProblemDetails _problemDetails;

        public FakeExceptionToProblemDetailsMapper(AspNetProblemDetails problemDetails)
        {
            _problemDetails = problemDetails;
        }

        public Exception? LastException { get; private set; }

        public HttpContext? LastHttpContext { get; private set; }

        public AspNetProblemDetails Map(Exception exception, HttpContext httpContext)
        {
            LastException = exception;
            LastHttpContext = httpContext;
            return _problemDetails;
        }
    }

    private sealed class FakeProblemDetailsService : IProblemDetailsService
    {
        private readonly bool _shouldWrite;

        public FakeProblemDetailsService(bool shouldWrite)
        {
            _shouldWrite = shouldWrite;
        }

        public int TryWriteCallCount { get; private set; }

        public ProblemDetailsContext? LastContext { get; private set; }

        public ValueTask<bool> TryWriteAsync(ProblemDetailsContext context)
        {
            TryWriteCallCount++;
            LastContext = context;
            return ValueTask.FromResult(_shouldWrite);
        }

        public ValueTask WriteAsync(ProblemDetailsContext context)
        {
            LastContext = context;
            return ValueTask.CompletedTask;
        }
    }
}
