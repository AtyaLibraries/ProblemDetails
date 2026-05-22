// <copyright file="DefaultExceptionToProblemDetailsMapperTests.cs" company="Atya">
// Copyright (c) Atya. All rights reserved.
// </copyright>

using Atya.Errors.Exceptions;
using Atya.Errors.Exceptions.Models;
using Atya.Errors.ProblemDetails.Constants;
using Atya.Errors.ProblemDetails.Mappings;
using Atya.Errors.ProblemDetails.Options;
using Atya.Governance.Testing.Diagnostics;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Options;

namespace ProblemDetails.UnitTests;

public sealed class DefaultExceptionToProblemDetailsMapperTests
{
    [Fact]
    public void Constructor_Should_Throw_When_Options_Is_Null()
    {
        var act = () => new DefaultExceptionToProblemDetailsMapper((AtyaProblemDetailsOptions)null!);

        act.Should().Throw<ArgumentNullException>();
    }

    [Fact]
    public void Constructor_Should_Throw_When_Options_Accessor_Is_Null()
    {
        var act = () => new DefaultExceptionToProblemDetailsMapper((IOptions<AtyaProblemDetailsOptions>)null!);

        act.Should().Throw<ArgumentNullException>();
    }

    [Fact]
    public void Constructor_Should_Read_Options_From_Accessor()
    {
        var mapper = new DefaultExceptionToProblemDetailsMapper(Options.Create(
            new AtyaProblemDetailsOptions
            {
                IncludeTraceId = false
            }));

        var httpContext = CreateHttpContext("/customers/42");
        var exception = new NotFoundException("Customer was not found.");

        var result = mapper.Map(exception, httpContext);

        result.Extensions.Should().NotContainKey(ProblemDetailsExtensionNames.TraceId);
    }

    [Fact]
    public void Map_Should_Throw_When_Exception_Is_Null()
    {
        var mapper = CreateMapper();
        var httpContext = CreateHttpContext("/customers");

        var act = () => mapper.Map(null!, httpContext);

        act.Should().Throw<ArgumentNullException>();
    }

    [Fact]
    public void Map_Should_Throw_When_HttpContext_Is_Null()
    {
        var mapper = CreateMapper();
        var exception = new NotFoundException("Customer was not found.");

        var act = () => mapper.Map(exception, null!);

        act.Should().Throw<ArgumentNullException>();
    }

    [Fact]
    public void Map_Should_Map_NotFoundException_To_404()
    {
        var mapper = CreateMapper();
        var httpContext = CreateHttpContext("/customers/42");
        var exception = new NotFoundException("Customer was not found.", "customer.not_found");

        var result = mapper.Map(exception, httpContext);

        result.Status.Should().Be(StatusCodes.Status404NotFound);
        result.Title.Should().Be("Not Found");
        result.Type.Should().Be("urn:atya:problem-type:not-found");
        result.Detail.Should().Be("Customer was not found.");
        result.Instance.Should().Be("/customers/42");
        result.Extensions[ProblemDetailsExtensionNames.ErrorCode].Should().Be("customer.not_found");
        result.Extensions.Should().ContainKey(ProblemDetailsExtensionNames.TraceId);
    }

    [Fact]
    public void Map_Should_Use_Assignable_Default_Mapping_For_Derived_Exception()
    {
        var mapper = CreateMapper();
        var httpContext = CreateHttpContext("/customers/42");
        var exception = new DerivedNotFoundException("Customer was not found.");

        var result = mapper.Map(exception, httpContext);

        result.Status.Should().Be(StatusCodes.Status404NotFound);
        result.Title.Should().Be("Not Found");
        result.Type.Should().Be("urn:atya:problem-type:not-found");
        result.Detail.Should().Be("Customer was not found.");
    }

    [Fact]
    public void Map_Should_Map_BusinessRuleViolationException_To_422()
    {
        var mapper = CreateMapper();
        var httpContext = CreateHttpContext("/orders");
        var exception = new BusinessRuleViolationException("Order cannot be cancelled.", "order.rule_violation");

        var result = mapper.Map(exception, httpContext);

        result.Status.Should().Be(StatusCodes.Status422UnprocessableEntity);
        result.Title.Should().Be("Unprocessable Entity");
        result.Type.Should().Be("urn:atya:problem-type:business-rule-violation");
        result.Detail.Should().Be("Order cannot be cancelled.");
    }

    [Fact]
    public void Map_Should_Map_ValidationException_And_Add_Errors_Extension()
    {
        var mapper = CreateMapper();
        var httpContext = CreateHttpContext("/customers");

        var exception = new ValidationException(
            "Validation failed.",
            new[]
            {
                new ValidationExceptionItem("Email", "Email is required.", "validation.required"),
                new ValidationExceptionItem("Age", "Age must be greater than zero.", "validation.range", 0)
            });

        var result = mapper.Map(exception, httpContext);

        result.Status.Should().Be(StatusCodes.Status400BadRequest);
        result.Title.Should().Be("Bad Request");
        result.Type.Should().Be("urn:atya:problem-type:validation");
        result.Detail.Should().Be("Validation failed.");
        result.Extensions.Should().ContainKey(ProblemDetailsExtensionNames.Errors);

        var errors = result.Extensions[ProblemDetailsExtensionNames.Errors];
        errors.Should().BeAssignableTo<Dictionary<string, object?>[]>();

        var firstError = ((Dictionary<string, object?>[])errors!)[0];
        firstError["propertyName"].Should().Be("Email");
        firstError["message"].Should().Be("Email is required.");
        firstError["errorCode"].Should().Be("validation.required");
        firstError.Should().NotContainKey("attemptedValue");
    }

    [Fact]
    public void Map_Should_Include_CorrelationId_When_Available()
    {
        var correlationIdAccessor = new FakeCorrelationIdAccessor();
        correlationIdAccessor.Set("corr-123");
        var options = new AtyaProblemDetailsOptions
        {
            CorrelationIdAccessor = _ => correlationIdAccessor.CorrelationId,
        };

        var mapper = new DefaultExceptionToProblemDetailsMapper(options);
        var httpContext = CreateHttpContext("/customers");
        var exception = new ConflictException("Customer already exists.", "customer.conflict");

        var result = mapper.Map(exception, httpContext);

        result.Extensions[ProblemDetailsExtensionNames.CorrelationId].Should().Be("corr-123");
    }

    [Fact]
    public void Map_Should_Include_Metadata_When_Option_Is_Enabled()
    {
        var options = new AtyaProblemDetailsOptions
        {
            IncludeExceptionMetadata = true
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

        result.Extensions.Should().ContainKey(ProblemDetailsExtensionNames.Metadata);

        var metadata = result.Extensions[ProblemDetailsExtensionNames.Metadata];
        metadata.Should().NotBeNull();
    }

    [Fact]
    public void Map_Should_Not_Add_Optional_Extensions_When_Disabled_Or_Unavailable()
    {
        var options = new AtyaProblemDetailsOptions
        {
            IncludeTraceId = false,
            IncludeErrorCode = false,
            IncludeExceptionMetadata = false,
            CorrelationIdAccessor = static _ => " "
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

        result.Extensions.Should().NotContainKey(ProblemDetailsExtensionNames.TraceId);
        result.Extensions.Should().NotContainKey(ProblemDetailsExtensionNames.CorrelationId);
        result.Extensions.Should().NotContainKey(ProblemDetailsExtensionNames.ErrorCode);
        result.Extensions.Should().NotContainKey(ProblemDetailsExtensionNames.Metadata);
    }

    [Fact]
    public void Map_Should_Use_Custom_Mapping_When_Overridden()
    {
        var options = new AtyaProblemDetailsOptions()
            .Map<NotFoundException>(
                StatusCodes.Status410Gone,
                "Gone",
                "urn:test:not-found-overridden");

        var mapper = new DefaultExceptionToProblemDetailsMapper(options);
        var httpContext = CreateHttpContext("/customers/42");
        var exception = new NotFoundException("Customer was not found.");

        var result = mapper.Map(exception, httpContext);

        result.Status.Should().Be(StatusCodes.Status410Gone);
        result.Title.Should().Be("Gone");
        result.Type.Should().Be("urn:test:not-found-overridden");
    }

    [Fact]
    public void Map_Should_Use_Custom_Detail_Factory_When_Provided()
    {
        var options = new AtyaProblemDetailsOptions()
            .Map<InfrastructureException>(
                StatusCodes.Status503ServiceUnavailable,
                "Service Unavailable",
                "urn:test:service-unavailable",
                static (_, httpContext) => $"Safe detail for {httpContext.Request.Path.Value}");

        var mapper = new DefaultExceptionToProblemDetailsMapper(options);
        var httpContext = CreateHttpContext("/customers");
        var exception = new InfrastructureException("Database password leaked.");

        var result = mapper.Map(exception, httpContext);

        result.Status.Should().Be(StatusCodes.Status503ServiceUnavailable);
        result.Detail.Should().Be("Safe detail for /customers");
    }

    [Fact]
    public void Map_Should_Use_Root_Instance_When_Request_Path_Is_Empty()
    {
        var mapper = CreateMapper();
        var httpContext = CreateHttpContext(string.Empty);
        var exception = new NotFoundException("Customer was not found.");

        var result = mapper.Map(exception, httpContext);

        result.Instance.Should().Be("/");
    }

    [Fact]
    public void Map_Should_Return_Generic_500_For_Unhandled_Exception_By_Default()
    {
        var mapper = CreateMapper();
        var httpContext = CreateHttpContext("/customers");
        var exception = new InvalidOperationException("Sensitive internal details.");

        var result = mapper.Map(exception, httpContext);

        result.Status.Should().Be(StatusCodes.Status500InternalServerError);
        result.Title.Should().Be("Internal Server Error");
        result.Type.Should().Be("urn:atya:problem-type:internal-server-error");
        result.Detail.Should().Be("An unexpected error occurred.");
    }

    [Fact]
    public void Map_Should_Return_Raw_Detail_For_Unhandled_Exception_When_Enabled()
    {
        var options = new AtyaProblemDetailsOptions
        {
            IncludeExceptionDetailsPredicate = static (_, _) => true
        };

        var mapper = new DefaultExceptionToProblemDetailsMapper(options);
        var httpContext = CreateHttpContext("/customers");
        var exception = new InvalidOperationException("Sensitive internal details.");

        var result = mapper.Map(exception, httpContext);

        result.Detail.Should().Be("Sensitive internal details.");
    }

    [Fact]
    public void Map_Should_Allow_Final_Customization()
    {
        var options = new AtyaProblemDetailsOptions
        {
            CustomizeProblemDetails = static (_, _, problemDetails) =>
            {
                problemDetails.Extensions["custom"] = "value";
            }
        };

        var mapper = new DefaultExceptionToProblemDetailsMapper(options);
        var httpContext = CreateHttpContext("/customers");
        var exception = new ForbiddenException("Forbidden.");

        var result = mapper.Map(exception, httpContext);

        result.Extensions["custom"].Should().Be("value");
    }

    private static DefaultExceptionToProblemDetailsMapper CreateMapper()
    {
        return new DefaultExceptionToProblemDetailsMapper(new AtyaProblemDetailsOptions());
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

    private sealed class DerivedNotFoundException : NotFoundException
    {
        public DerivedNotFoundException(string message)
            : base(message)
        {
        }
    }
}
