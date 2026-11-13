// <copyright file="DefaultResultToProblemDetailsMapperTests.cs" company="Atya">
// Copyright (c) Atya. All rights reserved.
// </copyright>

using Atya.Errors.ProblemDetails.Constants;
using Atya.Errors.ProblemDetails.Mappings;
using Atya.Errors.ProblemDetails.Models;
using Atya.Errors.ProblemDetails.Options;
using Atya.Foundation.Results;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Options;

namespace ProblemDetails.UnitTests;

public sealed class DefaultResultToProblemDetailsMapperTests
{
    [Fact]
    public void Constructor_Should_Throw_When_Options_Is_Null()
    {
        var act = () => new DefaultResultToProblemDetailsMapper((AtyaProblemDetailsOptions)null!);

        act.Should().Throw<ArgumentNullException>();
    }

    [Fact]
    public void Constructor_Should_Throw_When_Options_Accessor_Is_Null()
    {
        var act = () => new DefaultResultToProblemDetailsMapper((IOptions<AtyaProblemDetailsOptions>)null!);

        act.Should().Throw<ArgumentNullException>();
    }

    [Fact]
    public void Constructor_Should_Read_Options_From_Accessor()
    {
        var mapper = new DefaultResultToProblemDetailsMapper(Options.Create(
            new AtyaProblemDetailsOptions
            {
                IncludeTraceId = false
            }));

        var result = mapper.Map(
            Result.Failure(new Error("customers.not_found", "Customer was not found.", ErrorKind.NotFound)),
            CreateHttpContext("/customers/42"));

        result.Extensions.Should().NotContainKey(ProblemDetailsExtensionNames.TraceId);
    }

    [Fact]
    public void Map_Should_Throw_When_Result_Is_Null()
    {
        var mapper = CreateMapper();
        var httpContext = CreateHttpContext("/customers");

        var act = () => mapper.Map((Result)null!, httpContext);

        act.Should().Throw<ArgumentNullException>();
    }

    [Fact]
    public void Map_Should_Throw_When_HttpContext_Is_Null()
    {
        var mapper = CreateMapper();
        var result = Result.Failure(new Error("failure", "Failure."));

        var act = () => mapper.Map(result, null!);

        act.Should().Throw<ArgumentNullException>();
    }

    [Fact]
    public void Map_Should_Throw_When_Result_Is_Successful()
    {
        var mapper = CreateMapper();
        var httpContext = CreateHttpContext("/customers");

        var act = () => mapper.Map(Result.Success(), httpContext);

        act.Should()
            .Throw<InvalidOperationException>()
            .WithMessage("A successful result cannot be mapped to problem details.");
    }

    [Fact]
    public void Map_Should_Throw_When_Typed_Result_Is_Successful()
    {
        var mapper = CreateMapper();
        var httpContext = CreateHttpContext("/customers");

        var act = () => mapper.Map(Result.Success("value"), httpContext);

        act.Should()
            .Throw<InvalidOperationException>()
            .WithMessage("A successful result cannot be mapped to problem details.");
    }

    [Fact]
    public void Map_Should_Map_NotFound_Error_To_404()
    {
        var mapper = CreateMapper();
        var httpContext = CreateHttpContext("/customers/42");
        var result = Result.Failure(new Error("customers.not_found", "Customer was not found.", ErrorKind.NotFound));

        var problemDetails = mapper.Map(result, httpContext);

        problemDetails.Status.Should().Be(StatusCodes.Status404NotFound);
        problemDetails.Title.Should().Be("Not Found");
        problemDetails.Type.Should().Be("urn:atya:problem-type:not-found");
        problemDetails.Detail.Should().Be("Customer was not found.");
        problemDetails.Instance.Should().Be("/customers/42");
        problemDetails.Extensions[ProblemDetailsExtensionNames.ErrorCode].Should().Be("customers.not_found");
        problemDetails.Extensions.Should().ContainKey(ProblemDetailsExtensionNames.TraceId);
    }

    [Fact]
    public void Map_Should_Map_Typed_Result_Failure()
    {
        var mapper = CreateMapper();
        var httpContext = CreateHttpContext("/customers/42");
        var result = Result.Failure<string>(new Error("customers.conflict", "Customer already exists.", ErrorKind.Conflict));

        var problemDetails = mapper.Map(result, httpContext);

        problemDetails.Status.Should().Be(StatusCodes.Status409Conflict);
        problemDetails.Title.Should().Be("Conflict");
        problemDetails.Type.Should().Be("urn:atya:problem-type:conflict");
        problemDetails.Detail.Should().Be("Customer already exists.");
    }

    [Fact]
    public void Map_Should_Redact_Default_Unexpected_Error_Content()
    {
        const string marker = "ATYA014_SYNTHETIC_MARKER";
        var mapper = CreateMapper();
        var error = new Error(marker, marker, ErrorKind.Unexpected);

        var problemDetails = mapper.Map(error, CreateHttpContext("/orders"));

        problemDetails.Status.Should().Be(StatusCodes.Status500InternalServerError);
        problemDetails.Detail.Should().Be("An unexpected error occurred.");
        problemDetails.Extensions.Should().NotContainKey(ProblemDetailsExtensionNames.ErrorCode);
    }

    [Fact]
    public void Map_Should_Not_Add_Error_Derived_Extensions_For_Server_Errors()
    {
        const string marker = "ATYA014_SYNTHETIC_EXTENSION_MARKER";
        var options = new AtyaProblemDetailsOptions()
            .MapError(
                ErrorKind.Failure,
                StatusCodes.Status503ServiceUnavailable,
                "Service Unavailable",
                "urn:test:service-unavailable");
        var mapper = new DefaultResultToProblemDetailsMapper(options);
        var error = new Error(
            marker,
            marker,
            target: marker,
            details:
            [
                new Error(marker, marker, marker, kind: ErrorKind.Validation),
            ],
            kind: ErrorKind.Failure);

        var problemDetails = mapper.Map(error, CreateHttpContext("/orders"));

        problemDetails.Extensions.Should().NotContainKey(ProblemDetailsExtensionNames.ErrorCode);
        problemDetails.Extensions.Should().NotContainKey(ProblemDetailsExtensionNames.Errors);
    }

    [Theory]
    [InlineData(StatusCodes.Status500InternalServerError)]
    [InlineData(599)]
    public void Map_Should_Redact_Representative_Server_Error_Boundaries(int statusCode)
    {
        const string marker = "ATYA014_SYNTHETIC_BOUNDARY_MARKER";
        var options = new AtyaProblemDetailsOptions()
            .MapError(ErrorKind.Failure, statusCode, "Server Error", "urn:test:server-error");
        var mapper = new DefaultResultToProblemDetailsMapper(options);

        var problemDetails = mapper.Map(
            new Error("server.error", marker),
            CreateHttpContext("/orders"));

        problemDetails.Detail.Should().Be("An unexpected error occurred.");
    }

    [Fact]
    public void Map_Should_Preserve_Explicit_Server_Error_Detail_Override_Only()
    {
        const string disclosedMarker = "ATYA014_REVIEWED_DETAIL";
        const string extensionMarker = "ATYA014_HIDDEN_EXTENSION";
        var options = new AtyaProblemDetailsOptions()
            .MapError(
                ErrorKind.Unexpected,
                StatusCodes.Status500InternalServerError,
                "Internal Server Error",
                "urn:test:internal-server-error",
                static (error, _) => error.Message);
        var mapper = new DefaultResultToProblemDetailsMapper(options);
        var error = new Error(
            extensionMarker,
            disclosedMarker,
            target: null,
            details:
            [
                new Error(extensionMarker, extensionMarker, extensionMarker, kind: ErrorKind.Validation),
            ],
            kind: ErrorKind.Unexpected);

        var problemDetails = mapper.Map(error, CreateHttpContext("/orders"));

        problemDetails.Detail.Should().Be(disclosedMarker);
        problemDetails.Extensions.Should().NotContainKey(ProblemDetailsExtensionNames.ErrorCode);
        problemDetails.Extensions.Should().NotContainKey(ProblemDetailsExtensionNames.Errors);
    }

    [Fact]
    public void Map_Should_Preserve_Error_Content_For_Non_Server_Error()
    {
        const string marker = "ATYA014_SYNTHETIC_CLIENT_MARKER";
        var options = new AtyaProblemDetailsOptions()
            .MapError(ErrorKind.Failure, 499, "Client Error", "urn:test:client-error");
        var mapper = new DefaultResultToProblemDetailsMapper(options);

        var problemDetails = mapper.Map(
            new Error(marker, marker),
            CreateHttpContext("/orders"));

        problemDetails.Detail.Should().Be(marker);
        problemDetails.Extensions[ProblemDetailsExtensionNames.ErrorCode].Should().Be(marker);
    }

    [Fact]
    public void Map_Should_Add_Keyed_Errors_Extension_For_Validation_Error_Details()
    {
        var mapper = CreateMapper();
        var httpContext = CreateHttpContext("/customers");
        var error = new Error(
            "validation.failed",
            "Validation failed.",
            target: null,
            details:
            [
                new Error("validation.required", "Email is required.", "Email", kind: ErrorKind.Validation),
                new Error("validation.format", "Email is invalid.", "Email", kind: ErrorKind.Validation),
                new Error("validation.range", "Age must be greater than zero.", "Age", kind: ErrorKind.Validation),
            ],
            kind: ErrorKind.Validation);

        var problemDetails = mapper.Map(error, httpContext);

        problemDetails.Status.Should().Be(StatusCodes.Status400BadRequest);
        problemDetails.Type.Should().Be("urn:atya:problem-type:validation");
        problemDetails.Extensions.Should().ContainKey(ProblemDetailsExtensionNames.Errors);

        var errors = problemDetails.Extensions[ProblemDetailsExtensionNames.Errors];
        errors.Should().BeAssignableTo<Dictionary<string, ValidationProblemError[]>>();

        var keyedErrors = (Dictionary<string, ValidationProblemError[]>)errors!;
        keyedErrors["Email"].Should().HaveCount(2);
        keyedErrors["Email"][0].Message.Should().Be("Email is required.");
        keyedErrors["Email"][0].ErrorCode.Should().Be("validation.required");
        keyedErrors["Age"].Should().ContainSingle();
        keyedErrors["Age"][0].PropertyName.Should().Be("Age");
    }

    [Fact]
    public void Map_Should_Add_Keyed_Errors_Extension_When_Detail_Is_Validation()
    {
        var mapper = CreateMapper();
        var httpContext = CreateHttpContext("/customers");
        var error = new Error(
            "customers.invalid",
            "Customer is invalid.",
            target: null,
            details:
            [
                new Error("validation.required", "Email is required.", "Email", kind: ErrorKind.Validation),
            ]);

        var problemDetails = mapper.Map(error, httpContext);

        var keyedErrors = (Dictionary<string, ValidationProblemError[]>)
            problemDetails.Extensions[ProblemDetailsExtensionNames.Errors]!;

        keyedErrors["Email"].Should().ContainSingle();
    }

    [Fact]
    public void Map_Should_Use_Empty_Key_For_Targetless_Validation_Errors()
    {
        var mapper = CreateMapper();
        var httpContext = CreateHttpContext("/customers");
        var error = new Error("validation.failed", "Validation failed.", ErrorKind.Validation);

        var problemDetails = mapper.Map(error, httpContext);

        var keyedErrors = (Dictionary<string, ValidationProblemError[]>)
            problemDetails.Extensions[ProblemDetailsExtensionNames.Errors]!;

        keyedErrors[string.Empty].Should().ContainSingle();
    }

    [Fact]
    public void Map_Should_Use_Custom_Error_Mapping_When_Overridden()
    {
        var options = new AtyaProblemDetailsOptions()
            .MapError(
                ErrorKind.Failure,
                StatusCodes.Status400BadRequest,
                "Bad Request",
                "urn:test:failure",
                static (_, _) => "Safe failure detail.");

        var mapper = new DefaultResultToProblemDetailsMapper(options);
        var httpContext = CreateHttpContext("/customers");
        var error = new Error("failure", "Hidden failure.");

        var result = mapper.Map(error, httpContext);

        result.Status.Should().Be(StatusCodes.Status400BadRequest);
        result.Title.Should().Be("Bad Request");
        result.Type.Should().Be("urn:test:failure");
        result.Detail.Should().Be("Safe failure detail.");
    }

    [Fact]
    public void Map_Should_Not_Add_Optional_Extensions_When_Disabled_Or_Unavailable()
    {
        var options = new AtyaProblemDetailsOptions
        {
            IncludeTraceId = false,
            IncludeErrorCode = false,
            CorrelationIdAccessor = static _ => " "
        };

        var mapper = new DefaultResultToProblemDetailsMapper(options);
        var httpContext = CreateHttpContext("/customers");
        var error = new Error("failure", "Failure.");

        var result = mapper.Map(error, httpContext);

        result.Extensions.Should().NotContainKey(ProblemDetailsExtensionNames.TraceId);
        result.Extensions.Should().NotContainKey(ProblemDetailsExtensionNames.CorrelationId);
        result.Extensions.Should().NotContainKey(ProblemDetailsExtensionNames.ErrorCode);
    }

    [Fact]
    public void Map_Should_Use_Root_Instance_When_Request_Path_Is_Empty()
    {
        var mapper = CreateMapper();
        var httpContext = CreateHttpContext(string.Empty);
        var error = new Error("failure", "Failure.");

        var result = mapper.Map(error, httpContext);

        result.Instance.Should().Be("/");
    }

    private static DefaultResultToProblemDetailsMapper CreateMapper()
    {
        return new DefaultResultToProblemDetailsMapper(new AtyaProblemDetailsOptions());
    }

    private static DefaultHttpContext CreateHttpContext(string path)
    {
        return new DefaultHttpContext
        {
            TraceIdentifier = "trace-123",
            Request =
            {
                Path = path
            }
        };
    }
}
