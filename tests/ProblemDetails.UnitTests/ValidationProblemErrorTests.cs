// <copyright file="ValidationProblemErrorTests.cs" company="Atya">
// Copyright (c) Atya. All rights reserved.
// </copyright>

using Atya.Errors.ProblemDetails.Models;

namespace ProblemDetails.UnitTests;

public sealed class ValidationProblemErrorTests
{
    [Fact]
    public void Record_Should_Store_All_Values()
    {
        var error = new ValidationProblemError(
            "Email",
            "Email is required.",
            "validation.required",
            "test@example.com");

        error.PropertyName.Should().Be("Email");
        error.Message.Should().Be("Email is required.");
        error.ErrorCode.Should().Be("validation.required");
        error.AttemptedValue.Should().Be("test@example.com");
    }

    [Fact]
    public void Record_Value_Equality_Should_Work()
    {
        var left = new ValidationProblemError(
            "Age",
            "Age must be greater than zero.",
            "validation.range",
            0);

        var right = new ValidationProblemError(
            "Age",
            "Age must be greater than zero.",
            "validation.range",
            0);

        left.Should().Be(right);
    }
}
