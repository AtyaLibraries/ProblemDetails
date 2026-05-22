// <copyright file="AtyaProblemDetailsOptionsTests.cs" company="Atya">
// Copyright (c) Atya. All rights reserved.
// </copyright>

using Atya.Errors.Exceptions;
using Atya.Errors.ProblemDetails.Options;
using Microsoft.AspNetCore.Http;

namespace ProblemDetails.UnitTests;

public sealed class AtyaProblemDetailsOptionsTests
{
    [Fact]
    public void Constructor_Should_Register_Default_Mappings()
    {
        var options = new AtyaProblemDetailsOptions();

        options.Mappings.Should().Contain(mapping => mapping.ExceptionType == typeof(NotFoundException));
        options.Mappings.Should().Contain(mapping => mapping.ExceptionType == typeof(ConflictException));
        options.Mappings.Should().Contain(mapping => mapping.ExceptionType == typeof(ValidationException));
    }

    [Fact]
    public void Mappings_Should_Not_Be_Externally_Mutable()
    {
        var options = new AtyaProblemDetailsOptions();

        var mappings = options.Mappings.Should().BeAssignableTo<IList<ExceptionProblemDetailsMapping>>().Subject;
        var act = () => mappings.Add(new ExceptionProblemDetailsMapping(
            typeof(InvalidOperationException),
            499,
            "Custom",
            "urn:test:custom"));

        act.Should().Throw<NotSupportedException>();
    }

    [Fact]
    public void Map_Should_Replace_Existing_Mapping_For_Same_Exception_Type()
    {
        var options = new AtyaProblemDetailsOptions();

        options.Map<NotFoundException>(410, "Gone", "urn:test:gone");

        var mappings = options.Mappings.Where(static mapping => mapping.ExceptionType == typeof(NotFoundException)).ToArray();

        mappings.Should().HaveCount(1);
        mappings[0].StatusCode.Should().Be(410);
        mappings[0].Title.Should().Be("Gone");
        mappings[0].Type.Should().Be("urn:test:gone");
    }

    [Fact]
    public void Map_Should_Store_Detail_Factory_When_Provided()
    {
        var options = new AtyaProblemDetailsOptions();

        options.Map<NotFoundException>(
            404,
            "Not Found",
            "urn:test:not-found",
            static (_, _) => "Safe detail.");

        var mapping = options.Mappings.Single(static mapping => mapping.ExceptionType == typeof(NotFoundException));

        mapping.DetailFactory.Should().NotBeNull();
        mapping.DetailFactory!(new NotFoundException("Hidden."), new DefaultHttpContext()).Should().Be("Safe detail.");
    }

    [Fact]
    public void Map_Should_Throw_When_Detail_Factory_Is_Null()
    {
        var options = new AtyaProblemDetailsOptions();

        var act = () => options.Map<NotFoundException>(404, "Not Found", "urn:test:not-found", null!);

        act.Should().Throw<ArgumentNullException>();
    }

    [Fact]
    public void RemoveMapping_Should_Remove_Mapping_For_Exception_Type()
    {
        var options = new AtyaProblemDetailsOptions();

        options.RemoveMapping<NotFoundException>();

        options.Mappings.Should().NotContain(mapping => mapping.ExceptionType == typeof(NotFoundException));
    }

    [Fact]
    public void RemoveMapping_Should_Do_Nothing_When_Exception_Type_Is_Not_Mapped()
    {
        var options = new AtyaProblemDetailsOptions();
        var initialCount = options.Mappings.Count;

        options.RemoveMapping<InvalidOperationException>();

        options.Mappings.Should().HaveCount(initialCount);
    }

    [Fact]
    public void ClearMappings_Should_Remove_All_Mappings()
    {
        var options = new AtyaProblemDetailsOptions();

        options.ClearMappings();

        options.Mappings.Should().BeEmpty();
    }

    [Fact]
    public void Map_Should_Throw_When_Title_Is_Empty()
    {
        var options = new AtyaProblemDetailsOptions();

        var act = () => options.Map<NotFoundException>(404, " ", "urn:test");

        act.Should().Throw<ArgumentException>();
    }

    [Fact]
    public void Map_Should_Throw_When_Type_Is_Empty()
    {
        var options = new AtyaProblemDetailsOptions();

        var act = () => options.Map<NotFoundException>(404, "Not Found", " ");

        act.Should().Throw<ArgumentException>();
    }

    [Theory]
    [InlineData(99)]
    [InlineData(600)]
    public void Map_Should_Throw_When_Status_Code_Is_Invalid(int statusCode)
    {
        var options = new AtyaProblemDetailsOptions();

        var act = () => options.Map<NotFoundException>(statusCode, "Not Found", "urn:test:not-found");

        act.Should().Throw<ArgumentOutOfRangeException>();
    }
}
