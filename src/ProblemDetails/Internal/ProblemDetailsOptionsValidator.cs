// <copyright file="ProblemDetailsOptionsValidator.cs" company="Atya">
// Copyright (c) Atya. All rights reserved.
// </copyright>

using Atya.Errors.ProblemDetails.Options;
using Microsoft.Extensions.Options;

namespace Atya.Errors.ProblemDetails.Internal;

/// <summary>
/// Validates <see cref="AtyaProblemDetailsOptions"/> at host start and when a mapper snapshot is built.
/// </summary>
internal sealed class ProblemDetailsOptionsValidator : IValidateOptions<AtyaProblemDetailsOptions>
{
    private static readonly string[] s_reservedMembers = ["type", "title", "status", "detail", "instance"];

    public ValidateOptionsResult Validate(string? name, AtyaProblemDetailsOptions options)
    {
        var failures = Collect(options);
        return failures.Count == 0 ? ValidateOptionsResult.Success : ValidateOptionsResult.Fail(failures);
    }

    internal static List<string> Collect(AtyaProblemDetailsOptions options)
    {
        var failures = new List<string>();

        foreach (var mapping in options.Mappings)
        {
            ValidateMapping($"Exception mapping '{mapping.ExceptionType.Name}'", mapping.StatusCode, mapping.Title, mapping.Type, options, failures);
        }

        foreach (var mapping in options.ErrorMappings)
        {
            ValidateMapping($"Error mapping '{mapping.Kind}'", mapping.StatusCode, mapping.Title, mapping.Type, options, failures);
        }

        ValidateExtensionNames(options.ExtensionNames, failures);

        return failures;
    }

    private static void ValidateMapping(
        string label,
        int statusCode,
        string title,
        string type,
        AtyaProblemDetailsOptions options,
        List<string> failures)
    {
        if (!options.AllowNonErrorStatusCodes && (statusCode < 400 || statusCode > 599))
        {
            failures.Add($"{label} uses status code {statusCode}; problem details must use 400-599 unless AllowNonErrorStatusCodes is set.");
        }

        if (string.IsNullOrWhiteSpace(title))
        {
            failures.Add($"{label} has an empty title.");
        }

        if (string.IsNullOrWhiteSpace(type))
        {
            failures.Add($"{label} has an empty type.");
        }
    }

    private static void ValidateExtensionNames(ProblemDetailsExtensionNameOptions names, List<string> failures)
    {
        // A null or blank name disables that extension and is therefore not validated.
        var seen = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        CheckTopLevel(seen, failures, nameof(names.TraceId), names.TraceId);
        CheckTopLevel(seen, failures, nameof(names.CorrelationId), names.CorrelationId);
        CheckTopLevel(seen, failures, nameof(names.ErrorCode), names.ErrorCode);
        CheckTopLevel(seen, failures, nameof(names.Errors), names.Errors);
        CheckTopLevel(seen, failures, nameof(names.Metadata), names.Metadata);

        // Validation item members live in their own object, so they only need to be distinct from each other.
        var itemSeen = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        CheckItem(itemSeen, failures, nameof(names.ValidationPropertyName), names.ValidationPropertyName);
        CheckItem(itemSeen, failures, nameof(names.ValidationMessage), names.ValidationMessage);
        CheckItem(itemSeen, failures, nameof(names.ValidationErrorCode), names.ValidationErrorCode);
        CheckItem(itemSeen, failures, nameof(names.ValidationAttemptedValue), names.ValidationAttemptedValue);
    }

    private static void CheckTopLevel(HashSet<string> seen, List<string> failures, string property, string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return;
        }

        var trimmed = value.Trim();
        if (Array.Exists(s_reservedMembers, member => string.Equals(member, trimmed, StringComparison.OrdinalIgnoreCase)))
        {
            failures.Add($"Extension name '{property}' uses the reserved RFC 9457 member '{trimmed}'.");
        }
        else if (!seen.Add(trimmed))
        {
            failures.Add($"Extension name '{property}' duplicates another extension name '{trimmed}'.");
        }
    }

    private static void CheckItem(HashSet<string> seen, List<string> failures, string property, string? value)
    {
        if (!string.IsNullOrWhiteSpace(value) && !seen.Add(value.Trim()))
        {
            failures.Add($"Validation member name '{property}' duplicates another validation member name '{value.Trim()}'.");
        }
    }
}
