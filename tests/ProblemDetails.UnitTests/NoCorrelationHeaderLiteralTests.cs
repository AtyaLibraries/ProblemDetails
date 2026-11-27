// <copyright file="NoCorrelationHeaderLiteralTests.cs" company="Atya">
// Copyright (c) Atya. All rights reserved.
// </copyright>

namespace ProblemDetails.UnitTests;

public sealed class NoCorrelationHeaderLiteralTests
{
    [Fact]
    public void Source_Should_Not_Contain_The_Correlation_Header_Literal()
    {
        var directory = new DirectoryInfo(AppContext.BaseDirectory);
        while (directory is not null && directory.GetFiles("*.sln").Length == 0 && directory.GetFiles("*.slnx").Length == 0)
        {
            directory = directory.Parent;
        }

        directory.Should().NotBeNull("the repository root must be found");

        var offenders = Directory
            .EnumerateFiles(Path.Combine(directory!.FullName, "src"), "*.cs", SearchOption.AllDirectories)
            .Where(path => !path.Contains($"{Path.DirectorySeparatorChar}obj{Path.DirectorySeparatorChar}", StringComparison.Ordinal))
            .Where(path => File.ReadAllText(path).Contains("X-Correlation-ID", StringComparison.OrdinalIgnoreCase))
            .ToArray();

        offenders.Should().BeEmpty("the correlation header name is owned by Atya.Diagnostics.Tracing");
    }
}
