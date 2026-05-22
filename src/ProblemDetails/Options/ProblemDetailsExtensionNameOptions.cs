// <copyright file="ProblemDetailsExtensionNameOptions.cs" company="Atya">
// Copyright (c) Atya. All rights reserved.
// </copyright>

using Atya.Errors.ProblemDetails.Constants;

namespace Atya.Errors.ProblemDetails.Options;

/// <summary>
/// Configures extension member names used by generated problem details payloads.
/// Set a member name to <see langword="null"/> or whitespace to suppress that member.
/// </summary>
public sealed class ProblemDetailsExtensionNameOptions
{
    /// <summary>
    /// Gets or sets the trace id extension name.
    /// </summary>
    public string? TraceId { get; set; } = ProblemDetailsExtensionNames.TraceId;

    /// <summary>
    /// Gets or sets the correlation id extension name.
    /// </summary>
    public string? CorrelationId { get; set; } = ProblemDetailsExtensionNames.CorrelationId;

    /// <summary>
    /// Gets or sets the error code extension name.
    /// </summary>
    public string? ErrorCode { get; set; } = ProblemDetailsExtensionNames.ErrorCode;

    /// <summary>
    /// Gets or sets the validation errors extension name.
    /// </summary>
    public string? Errors { get; set; } = ProblemDetailsExtensionNames.Errors;

    /// <summary>
    /// Gets or sets the metadata extension name.
    /// </summary>
    public string? Metadata { get; set; } = ProblemDetailsExtensionNames.Metadata;

    /// <summary>
    /// Gets or sets the validation property name member.
    /// </summary>
    public string? ValidationPropertyName { get; set; } = "propertyName";

    /// <summary>
    /// Gets or sets the validation message member.
    /// </summary>
    public string? ValidationMessage { get; set; } = "message";

    /// <summary>
    /// Gets or sets the validation error code member.
    /// </summary>
    public string? ValidationErrorCode { get; set; } = "errorCode";

    /// <summary>
    /// Gets or sets the validation attempted value member.
    /// </summary>
    public string? ValidationAttemptedValue { get; set; } = "attemptedValue";
}
