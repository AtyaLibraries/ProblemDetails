// <copyright file="DefaultProblemTypeUrisTests.cs" company="Atya">
// Copyright (c) Atya. All rights reserved.
// </copyright>

using Atya.Errors.ProblemDetails.Constants;

namespace ProblemDetails.UnitTests;

public sealed class DefaultProblemTypeUrisTests
{
    [Fact]
    public void All_Constants_Should_Have_Expected_Values()
    {
        DefaultProblemTypeUris.Failure.Should().Be("urn:atya:problem-type:failure");
        DefaultProblemTypeUris.Validation.Should().Be("urn:atya:problem-type:validation");
        DefaultProblemTypeUris.BusinessRuleViolation.Should().Be("urn:atya:problem-type:business-rule-violation");
        DefaultProblemTypeUris.Unauthorized.Should().Be("urn:atya:problem-type:unauthorized");
        DefaultProblemTypeUris.Forbidden.Should().Be("urn:atya:problem-type:forbidden");
        DefaultProblemTypeUris.NotFound.Should().Be("urn:atya:problem-type:not-found");
        DefaultProblemTypeUris.Conflict.Should().Be("urn:atya:problem-type:conflict");
        DefaultProblemTypeUris.Concurrency.Should().Be("urn:atya:problem-type:concurrency-conflict");
        DefaultProblemTypeUris.Infrastructure.Should().Be("urn:atya:problem-type:infrastructure-failure");
        DefaultProblemTypeUris.Unhandled.Should().Be("urn:atya:problem-type:internal-server-error");
    }
}
