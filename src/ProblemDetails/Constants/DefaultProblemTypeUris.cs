// <copyright file="DefaultProblemTypeUris.cs" company="Atya">
// Copyright (c) Atya. All rights reserved.
// </copyright>

namespace Atya.Errors.ProblemDetails.Constants;

/// <summary>
/// Default problem type URIs used by the package.
/// </summary>
public static class DefaultProblemTypeUris
{
    /// <summary>
    /// The default problem type URI for validation failures.
    /// </summary>
    public const string Validation = "urn:atya:problem-type:validation";

    /// <summary>
    /// The default problem type URI for business rule violations.
    /// </summary>
    public const string BusinessRuleViolation = "urn:atya:problem-type:business-rule-violation";

    /// <summary>
    /// The default problem type URI for unauthorized requests.
    /// </summary>
    public const string Unauthorized = "urn:atya:problem-type:unauthorized";

    /// <summary>
    /// The default problem type URI for forbidden requests.
    /// </summary>
    public const string Forbidden = "urn:atya:problem-type:forbidden";

    /// <summary>
    /// The default problem type URI for missing resources.
    /// </summary>
    public const string NotFound = "urn:atya:problem-type:not-found";

    /// <summary>
    /// The default problem type URI for conflicts.
    /// </summary>
    public const string Conflict = "urn:atya:problem-type:conflict";

    /// <summary>
    /// The default problem type URI for concurrency conflicts.
    /// </summary>
    public const string Concurrency = "urn:atya:problem-type:concurrency-conflict";

    /// <summary>
    /// The default problem type URI for infrastructure failures.
    /// </summary>
    public const string Infrastructure = "urn:atya:problem-type:infrastructure-failure";

    /// <summary>
    /// The default problem type URI for unhandled exceptions.
    /// </summary>
    public const string Unhandled = "urn:atya:problem-type:internal-server-error";
}
