// <copyright file="Program.cs" company="Atya">
// Copyright (c) Atya. All rights reserved.
// </copyright>

using Atya.Errors.Exceptions;
using Atya.Errors.Exceptions.Models;
using Atya.Errors.ProblemDetails.Abstractions;
using Atya.Errors.ProblemDetails.Extensions;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using AspNetProblemDetails = Microsoft.AspNetCore.Mvc.ProblemDetails;

namespace ProblemDetails.Samples.ConsoleApp;

/// <summary>
/// Runs the Atya.Errors.ProblemDetails console sample.
/// </summary>
public static class Program
{
    /// <summary>
    /// Demonstrates mapping an exception to ASP.NET Core problem details.
    /// </summary>
    public static void Main()
    {
        ServiceCollection services = new();
        services.AddAtyaProblemDetails(options =>
        {
            options.CorrelationIdAccessor = static httpContext =>
                httpContext.Request.Headers.TryGetValue("X-Correlation-ID", out var values)
                    ? values.ToString()
                    : null;
        });

        using ServiceProvider serviceProvider = services.BuildServiceProvider();
        IExceptionToProblemDetailsMapper mapper = serviceProvider.GetRequiredService<IExceptionToProblemDetailsMapper>();

        DefaultHttpContext httpContext = new();
        httpContext.Request.Path = "/customers";
        httpContext.Request.Headers["X-Correlation-ID"] = "corr-42";

        ValidationException exception = new(
            "Customer request is invalid.",
            [
                new ValidationExceptionItem("Email", "Email is required.", "validation.required"),
                new ValidationExceptionItem("Age", "Age must be greater than zero.", "validation.range", 0)
            ],
            metadata: new Dictionary<string, object?>
            {
                ["entityId"] = 42
            });

        AspNetProblemDetails problemDetails = mapper.Map(exception, httpContext);

        Console.WriteLine($"{problemDetails.Status}: {problemDetails.Title}");
        Console.WriteLine(problemDetails.Type);

        if (problemDetails.Extensions.TryGetValue("correlationId", out object? correlationId))
        {
            Console.WriteLine($"Correlation: {correlationId}");
        }

        if (problemDetails.Extensions.TryGetValue("errors", out object? errors) && errors is Array errorsArray)
        {
            Console.WriteLine($"Validation errors: {errorsArray.Length}");
        }
    }
}
