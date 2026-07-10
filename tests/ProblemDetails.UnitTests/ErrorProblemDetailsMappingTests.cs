// <copyright file="ErrorProblemDetailsMappingTests.cs" company="Atya">
// Copyright (c) Atya. All rights reserved.
// </copyright>

using Atya.Errors.ProblemDetails.Options;
using Atya.Foundation.Results;

namespace ProblemDetails.UnitTests;

public sealed class ErrorProblemDetailsMappingTests
{
    [Fact]
    public void Record_Should_Store_All_Values()
    {
        var mapping = new ErrorProblemDetailsMapping(
            ErrorKind.Conflict,
            409,
            "Conflict",
            "urn:test:conflict");

        mapping.Kind.Should().Be(ErrorKind.Conflict);
        mapping.StatusCode.Should().Be(409);
        mapping.Title.Should().Be("Conflict");
        mapping.Type.Should().Be("urn:test:conflict");
    }

    [Fact]
    public void Record_Value_Equality_Should_Work()
    {
        var left = new ErrorProblemDetailsMapping(
            ErrorKind.NotFound,
            404,
            "Not Found",
            "urn:test:not-found");

        var right = new ErrorProblemDetailsMapping(
            ErrorKind.NotFound,
            404,
            "Not Found",
            "urn:test:not-found");

        left.Should().Be(right);
    }
}
