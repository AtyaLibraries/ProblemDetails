// <copyright file="ProblemDetailsExtensionNamesTests.cs" company="Atya">
// Copyright (c) Atya. All rights reserved.
// </copyright>

using Atya.Errors.ProblemDetails.Constants;

namespace ProblemDetails.UnitTests;

public sealed class ProblemDetailsExtensionNamesTests
{
    [Fact]
    public void All_Constants_Should_Have_Expected_Values()
    {
        ProblemDetailsExtensionNames.TraceId.Should().Be("traceId");
        ProblemDetailsExtensionNames.CorrelationId.Should().Be("correlationId");
        ProblemDetailsExtensionNames.ErrorCode.Should().Be("errorCode");
        ProblemDetailsExtensionNames.Errors.Should().Be("errors");
        ProblemDetailsExtensionNames.Metadata.Should().Be("metadata");
    }
}
