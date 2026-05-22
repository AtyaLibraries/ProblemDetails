// <copyright file="AtyaProblemDetailsOptionsAdditionalTests.cs" company="Atya">
// Copyright (c) Atya. All rights reserved.
// </copyright>

using Atya.Errors.ProblemDetails.Options;
using Microsoft.AspNetCore.Http;

namespace ProblemDetails.UnitTests;

public sealed class AtyaProblemDetailsOptionsAdditionalTests
{
    [Fact]
    public void Constructor_Should_Set_Expected_Default_Flags()
    {
        var options = new AtyaProblemDetailsOptions();

        options.IncludeTraceId.Should().BeTrue();
        options.IncludeErrorCode.Should().BeTrue();
        options.IncludeExceptionMetadata.Should().BeFalse();
        options.IncludeValidationAttemptedValues.Should().BeFalse();
        options.CustomizeProblemDetails.Should().BeNull();
    }

    [Fact]
    public void Constructor_Should_Set_Expected_Default_Extension_Names()
    {
        var options = new AtyaProblemDetailsOptions();

        options.ExtensionNames.TraceId.Should().Be("traceId");
        options.ExtensionNames.CorrelationId.Should().Be("correlationId");
        options.ExtensionNames.ErrorCode.Should().Be("errorCode");
        options.ExtensionNames.Errors.Should().Be("errors");
        options.ExtensionNames.Metadata.Should().Be("metadata");
        options.ExtensionNames.ValidationPropertyName.Should().Be("propertyName");
        options.ExtensionNames.ValidationMessage.Should().Be("message");
        options.ExtensionNames.ValidationErrorCode.Should().Be("errorCode");
        options.ExtensionNames.ValidationAttemptedValue.Should().Be("attemptedValue");
    }

    [Fact]
    public void CorrelationIdAccessor_Should_Return_Null_When_Header_Is_Missing()
    {
        var options = new AtyaProblemDetailsOptions();
        var httpContext = new DefaultHttpContext();

        var correlationId = options.CorrelationIdAccessor(httpContext);

        correlationId.Should().BeNull();
    }

    [Fact]
    public void CorrelationIdAccessor_Should_Return_Null_When_Header_Is_Whitespace()
    {
        var options = new AtyaProblemDetailsOptions();
        var httpContext = new DefaultHttpContext();
        httpContext.Request.Headers["X-Correlation-ID"] = "   ";

        var correlationId = options.CorrelationIdAccessor(httpContext);

        correlationId.Should().BeNull();
    }

    [Fact]
    public void CorrelationIdAccessor_Should_Return_Header_Value_When_Present()
    {
        var options = new AtyaProblemDetailsOptions();
        var httpContext = new DefaultHttpContext();
        httpContext.Request.Headers["X-Correlation-ID"] = "corr-456";

        var correlationId = options.CorrelationIdAccessor(httpContext);

        correlationId.Should().Be("corr-456");
    }

    [Fact]
    public void IncludeExceptionDetailsPredicate_Should_Return_False_By_Default()
    {
        var options = new AtyaProblemDetailsOptions();
        var httpContext = new DefaultHttpContext();
        var exception = new InvalidOperationException("boom");

        var result = options.IncludeExceptionDetailsPredicate(httpContext, exception);

        result.Should().BeFalse();
    }

    [Fact]
    public void CorrelationIdAccessor_Should_Throw_When_Set_To_Null()
    {
        var options = new AtyaProblemDetailsOptions();

        var act = () => options.CorrelationIdAccessor = null!;

        act.Should().Throw<ArgumentNullException>();
    }

    [Fact]
    public void IncludeExceptionDetailsPredicate_Should_Throw_When_Set_To_Null()
    {
        var options = new AtyaProblemDetailsOptions();

        var act = () => options.IncludeExceptionDetailsPredicate = null!;

        act.Should().Throw<ArgumentNullException>();
    }

    [Fact]
    public void Map_Should_Insert_New_Mapping_At_Beginning()
    {
        var options = new AtyaProblemDetailsOptions();

        options.Map<InvalidOperationException>(499, "Custom", "urn:test:custom");

        options.Mappings[0].ExceptionType.Should().Be<InvalidOperationException>();
        options.Mappings[0].StatusCode.Should().Be(499);
        options.Mappings[0].Title.Should().Be("Custom");
        options.Mappings[0].Type.Should().Be("urn:test:custom");
    }
}
