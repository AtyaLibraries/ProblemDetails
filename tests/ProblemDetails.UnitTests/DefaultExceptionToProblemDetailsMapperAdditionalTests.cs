// <copyright file="DefaultExceptionToProblemDetailsMapperAdditionalTests.cs" company="Atya">
// Copyright (c) Atya. All rights reserved.
// </copyright>

using Atya.Errors.Exceptions;
using Atya.Errors.Exceptions.Models;
using Atya.Errors.ProblemDetails.Constants;
using Atya.Errors.ProblemDetails.Mappings;
using Atya.Errors.ProblemDetails.Options;
using Atya.Governance.Testing.Diagnostics;
using Microsoft.AspNetCore.Http;
using System.Diagnostics;

namespace ProblemDetails.UnitTests;

public sealed class DefaultExceptionToProblemDetailsMapperAdditionalTests
{
    [Fact]
    public void Map_Should_Not_Add_ErrorCode_When_Option_Is_Disabled()
    {
        var options = new AtyaProblemDetailsOptions
        {
            IncludeErrorCode = false
        };

        var mapper = new DefaultExceptionToProblemDetailsMapper(options);
        var httpContext = CreateHttpContext("/customers");
        var exception = new NotFoundException("Customer was not found.", "customer.not_found");

        var result = mapper.Map(exception, httpContext);

        result.Extensions.Should().NotContainKey(ProblemDetailsExtensionNames.ErrorCode);
    }

    [Fact]
    public void Map_Should_Not_Add_TraceId_When_Option_Is_Disabled()
    {
        var options = new AtyaProblemDetailsOptions
        {
            IncludeTraceId = false
        };

        var mapper = new DefaultExceptionToProblemDetailsMapper(options);
        var httpContext = CreateHttpContext("/customers");
        var exception = new NotFoundException("Customer was not found.");

        var result = mapper.Map(exception, httpContext);

        result.Extensions.Should().NotContainKey(ProblemDetailsExtensionNames.TraceId);
    }

    [Fact]
    public void Map_Should_Not_Add_TraceId_When_TraceIdentifier_Is_Blank()
    {
        var mapper = new DefaultExceptionToProblemDetailsMapper(new AtyaProblemDetailsOptions());
        var httpContext = CreateHttpContext("/customers");
        httpContext.TraceIdentifier = string.Empty;
        var exception = new NotFoundException("Customer was not found.");

        var result = mapper.Map(exception, httpContext);

        result.Extensions.Should().NotContainKey(ProblemDetailsExtensionNames.TraceId);
    }

    [Fact]
    public void Map_Should_Use_Current_Activity_Id_When_Available()
    {
        using var activity = new Activity("ProblemDetails.UnitTests").Start();
        var mapper = new DefaultExceptionToProblemDetailsMapper(new AtyaProblemDetailsOptions());
        var httpContext = CreateHttpContext("/customers");
        var exception = new NotFoundException("Customer was not found.");

        var result = mapper.Map(exception, httpContext);

        result.Extensions[ProblemDetailsExtensionNames.TraceId].Should().Be(activity.Id);
    }

    [Fact]
    public void Map_Should_Not_Add_Metadata_When_Option_Is_Disabled()
    {
        var options = new AtyaProblemDetailsOptions
        {
            IncludeExceptionMetadata = false
        };

        var mapper = new DefaultExceptionToProblemDetailsMapper(options);
        var httpContext = CreateHttpContext("/customers");

        var exception = new ConflictException(
            "Customer already exists.",
            "customer.conflict",
            new Dictionary<string, object?>
            {
                ["email"] = "test@example.com"
            });

        var result = mapper.Map(exception, httpContext);

        result.Extensions.Should().NotContainKey(ProblemDetailsExtensionNames.Metadata);
    }

    [Fact]
    public void Map_Should_Not_Add_ErrorCode_When_Atya_Exception_ErrorCode_Is_Null()
    {
        var mapper = new DefaultExceptionToProblemDetailsMapper(new AtyaProblemDetailsOptions());
        var httpContext = CreateHttpContext("/customers");
        var exception = new NotFoundException("Customer was not found.", errorCode: null);

        var result = mapper.Map(exception, httpContext);

        result.Extensions.Should().NotContainKey(ProblemDetailsExtensionNames.ErrorCode);
    }

    [Fact]
    public void Map_Should_Not_Add_Metadata_When_Exception_Is_Not_Atya_Exception()
    {
        var options = new AtyaProblemDetailsOptions
        {
            IncludeExceptionMetadata = true,
            IncludeExceptionDetailsPredicate = static (_, _) => true
        };

        var mapper = new DefaultExceptionToProblemDetailsMapper(options);
        var httpContext = CreateHttpContext("/customers");
        var exception = new InvalidOperationException("Unexpected failure.");

        var result = mapper.Map(exception, httpContext);

        result.Extensions.Should().NotContainKey(ProblemDetailsExtensionNames.Metadata);
    }

    [Fact]
    public void Map_Should_Not_Add_Metadata_When_Atya_Exception_Metadata_Is_Empty()
    {
        var options = new AtyaProblemDetailsOptions
        {
            IncludeExceptionMetadata = true
        };

        var mapper = new DefaultExceptionToProblemDetailsMapper(options);
        var httpContext = CreateHttpContext("/customers");
        var exception = new NotFoundException("Customer was not found.");

        var result = mapper.Map(exception, httpContext);

        result.Extensions.Should().NotContainKey(ProblemDetailsExtensionNames.Metadata);
    }

    [Fact]
    public void Map_Should_Not_Add_CorrelationId_When_Accessor_Returns_Null()
    {
        var correlationIdAccessor = new FakeCorrelationIdAccessor(null);
        var options = new AtyaProblemDetailsOptions
        {
            CorrelationIdAccessor = _ => correlationIdAccessor.CorrelationId,
        };

        var mapper = new DefaultExceptionToProblemDetailsMapper(options);
        var httpContext = CreateHttpContext("/customers");
        var exception = new ConflictException("Conflict.");

        var result = mapper.Map(exception, httpContext);

        result.Extensions.Should().NotContainKey(ProblemDetailsExtensionNames.CorrelationId);
    }

    [Fact]
    public void Map_Should_Sanitize_Known_Handled_500_Exception_By_Default()
    {
        var mapper = new DefaultExceptionToProblemDetailsMapper(new AtyaProblemDetailsOptions());
        var httpContext = CreateHttpContext("/infrastructure");
        var exception = new InfrastructureException("Database timeout.");

        var result = mapper.Map(exception, httpContext);

        result.Status.Should().Be(StatusCodes.Status500InternalServerError);
        result.Detail.Should().Be("An unexpected error occurred.");
        result.Type.Should().Be("urn:atya:problem-type:infrastructure-failure");
    }

    [Fact]
    public void Map_Should_Use_Exception_Message_For_Known_Handled_500_Exception_When_Details_Are_Enabled()
    {
        var options = new AtyaProblemDetailsOptions
        {
            IncludeExceptionDetailsPredicate = static (_, _) => true
        };

        var mapper = new DefaultExceptionToProblemDetailsMapper(options);
        var httpContext = CreateHttpContext("/infrastructure");
        var exception = new InfrastructureException("Database timeout.");

        var result = mapper.Map(exception, httpContext);

        result.Detail.Should().Be("Database timeout.");
    }

    [Fact]
    public void Map_Should_Include_AttemptedValues_When_Enabled()
    {
        var options = new AtyaProblemDetailsOptions
        {
            IncludeValidationAttemptedValues = true
        };

        var mapper = new DefaultExceptionToProblemDetailsMapper(options);
        var httpContext = CreateHttpContext("/customers");
        var exception = new ValidationException(
            "Validation failed.",
            [new ValidationExceptionItem("Age", "Invalid age.", "validation.range", 0)]);

        var result = mapper.Map(exception, httpContext);

        var errors = result.Extensions[ProblemDetailsExtensionNames.Errors]
            .Should()
            .BeOfType<Dictionary<string, object?>[]>()
            .Subject;
        errors[0]["attemptedValue"].Should().Be(0);
    }

    [Fact]
    public void Map_Should_Use_Custom_Extension_And_Validation_Member_Names()
    {
        var options = new AtyaProblemDetailsOptions
        {
            IncludeExceptionMetadata = true,
            IncludeValidationAttemptedValues = true,
            CorrelationIdAccessor = static _ => "corr-123"
        };

        options.ExtensionNames.TraceId = "trace";
        options.ExtensionNames.CorrelationId = "correlation";
        options.ExtensionNames.ErrorCode = "code";
        options.ExtensionNames.Metadata = "meta";
        options.ExtensionNames.Errors = "validationFailures";
        options.ExtensionNames.ValidationPropertyName = "field";
        options.ExtensionNames.ValidationMessage = "reason";
        options.ExtensionNames.ValidationErrorCode = "rule";
        options.ExtensionNames.ValidationAttemptedValue = "value";

        var mapper = new DefaultExceptionToProblemDetailsMapper(options);
        var httpContext = CreateHttpContext("/customers");
        var exception = new ValidationException(
            "Validation failed.",
            [new ValidationExceptionItem("Age", "Invalid age.", "validation.range", 0)],
            metadata: new Dictionary<string, object?>
            {
                ["entityId"] = 42
            });

        var result = mapper.Map(exception, httpContext);

        result.Extensions["trace"].Should().Be("trace-123");
        result.Extensions["correlation"].Should().Be("corr-123");
        result.Extensions["code"].Should().Be("validation.failed");
        result.Extensions["meta"].Should().BeAssignableTo<Dictionary<string, object?>>();

        var errors = result.Extensions["validationFailures"]
            .Should()
            .BeOfType<Dictionary<string, object?>[]>()
            .Subject;
        errors[0]["field"].Should().Be("Age");
        errors[0]["reason"].Should().Be("Invalid age.");
        errors[0]["rule"].Should().Be("validation.range");
        errors[0]["value"].Should().Be(0);
    }

    [Fact]
    public void Map_Should_Suppress_Extensions_When_Custom_Names_Are_Blank()
    {
        var options = new AtyaProblemDetailsOptions
        {
            IncludeExceptionMetadata = true,
            IncludeValidationAttemptedValues = true,
            CorrelationIdAccessor = static _ => "corr-123"
        };

        options.ExtensionNames.TraceId = " ";
        options.ExtensionNames.CorrelationId = null;
        options.ExtensionNames.ErrorCode = null;
        options.ExtensionNames.Metadata = null;
        options.ExtensionNames.Errors = null;

        var mapper = new DefaultExceptionToProblemDetailsMapper(options);
        var httpContext = CreateHttpContext("/customers");
        var exception = new ValidationException(
            "Validation failed.",
            [new ValidationExceptionItem("Age", "Invalid age.", "validation.range", 0)],
            metadata: new Dictionary<string, object?>
            {
                ["entityId"] = 42
            });

        var result = mapper.Map(exception, httpContext);

        result.Extensions.Should().BeEmpty();
    }

    [Fact]
    public void Map_Should_Suppress_Configured_Validation_Members_When_Names_Are_Blank()
    {
        var options = new AtyaProblemDetailsOptions
        {
            IncludeValidationAttemptedValues = true
        };

        options.ExtensionNames.ValidationPropertyName = null;
        options.ExtensionNames.ValidationMessage = " ";
        options.ExtensionNames.ValidationErrorCode = null;
        options.ExtensionNames.ValidationAttemptedValue = null;

        var mapper = new DefaultExceptionToProblemDetailsMapper(options);
        var httpContext = CreateHttpContext("/customers");
        var exception = new ValidationException(
            "Validation failed.",
            [new ValidationExceptionItem("Age", "Invalid age.", "validation.range", 0)]);

        var result = mapper.Map(exception, httpContext);

        var errors = result.Extensions[ProblemDetailsExtensionNames.Errors]
            .Should()
            .BeOfType<Dictionary<string, object?>[]>()
            .Subject;
        errors[0].Should().BeEmpty();
    }

    [Fact]
    public void Map_Should_Use_Base_Exception_Mapping_For_Derived_Exception()
    {
        var mapper = new DefaultExceptionToProblemDetailsMapper(new AtyaProblemDetailsOptions());
        var httpContext = CreateHttpContext("/customers");
        var exception = new CustomNotFoundException("Derived not found.");

        var result = mapper.Map(exception, httpContext);

        result.Status.Should().Be(StatusCodes.Status404NotFound);
        result.Title.Should().Be("Not Found");
        result.Type.Should().Be("urn:atya:problem-type:not-found");
        result.Detail.Should().Be("Derived not found.");
    }

    [Fact]
    public void Map_Should_Use_Root_Path_When_Request_Path_Is_Empty()
    {
        var mapper = new DefaultExceptionToProblemDetailsMapper(new AtyaProblemDetailsOptions());
        var httpContext = new DefaultHttpContext
        {
            TraceIdentifier = "trace-123"
        };

        var exception = new NotFoundException("Customer was not found.");

        var result = mapper.Map(exception, httpContext);

        result.Instance.Should().Be("/");
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

    private sealed class CustomNotFoundException : NotFoundException
    {
        public CustomNotFoundException(string message)
            : base(message)
        {
        }
    }
}
