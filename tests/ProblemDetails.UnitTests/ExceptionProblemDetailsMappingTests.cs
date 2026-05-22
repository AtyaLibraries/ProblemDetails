// <copyright file="ExceptionProblemDetailsMappingTests.cs" company="Atya">
// Copyright (c) Atya. All rights reserved.
// </copyright>

using Atya.Errors.Exceptions;
using Atya.Errors.ProblemDetails.Options;

namespace ProblemDetails.UnitTests;

public sealed class ExceptionProblemDetailsMappingTests
{
    [Fact]
    public void Record_Should_Store_All_Values()
    {
        var mapping = new ExceptionProblemDetailsMapping(
            typeof(NotFoundException),
            404,
            "Not Found",
            "urn:test:not-found");

        mapping.ExceptionType.Should().Be<NotFoundException>();
        mapping.StatusCode.Should().Be(404);
        mapping.Title.Should().Be("Not Found");
        mapping.Type.Should().Be("urn:test:not-found");
    }

    [Fact]
    public void Record_Value_Equality_Should_Work()
    {
        var left = new ExceptionProblemDetailsMapping(
            typeof(ConflictException),
            409,
            "Conflict",
            "urn:test:conflict");

        var right = new ExceptionProblemDetailsMapping(
            typeof(ConflictException),
            409,
            "Conflict",
            "urn:test:conflict");

        left.Should().Be(right);
    }
}
