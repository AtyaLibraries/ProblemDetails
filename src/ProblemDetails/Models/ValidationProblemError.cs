// <copyright file="ValidationProblemError.cs" company="Atya">
// Copyright (c) Atya. All rights reserved.
// </copyright>

using System.Text.Json.Serialization;

namespace Atya.Errors.ProblemDetails.Models;

/// <summary>
/// Represents a machine-friendly validation error item in problem details extensions.
/// </summary>
/// <param name="PropertyName">The related property or logical field.</param>
/// <param name="Message">The validation message.</param>
/// <param name="ErrorCode">The optional machine-readable validation error code.</param>
/// <param name="AttemptedValue">The optional attempted value.</param>
#pragma warning disable SA1313 // Positional record parameters define PascalCase public API properties.
public sealed record ValidationProblemError(
    string PropertyName,
    string Message,
    string? ErrorCode,
    [property: JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    object? AttemptedValue);
#pragma warning restore SA1313
