// <copyright file="ProblemDetailsExtensionNames.cs" company="Atya">
// Copyright (c) Atya. All rights reserved.
// </copyright>

namespace Atya.Errors.ProblemDetails.Constants;

/// <summary>
/// Standard extension member names used in problem details payloads.
/// </summary>
public static class ProblemDetailsExtensionNames
{
    /// <summary>
    /// The extension key used for the trace identifier.
    /// </summary>
    public const string TraceId = "traceId";

    /// <summary>
    /// The extension key used for the correlation identifier.
    /// </summary>
    public const string CorrelationId = "correlationId";

    /// <summary>
    /// The extension key used for the error code.
    /// </summary>
    public const string ErrorCode = "errorCode";

    /// <summary>
    /// The extension key used for validation errors.
    /// </summary>
    public const string Errors = "errors";

    /// <summary>
    /// The extension key used for exception metadata.
    /// </summary>
    public const string Metadata = "metadata";
}
