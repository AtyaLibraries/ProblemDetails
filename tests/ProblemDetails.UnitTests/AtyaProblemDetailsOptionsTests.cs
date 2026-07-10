// <copyright file="AtyaProblemDetailsOptionsTests.cs" company="Atya">
// Copyright (c) Atya. All rights reserved.
// </copyright>

using Atya.Errors.Exceptions;
using Atya.Errors.ProblemDetails.Options;
using Atya.Foundation.Results;
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
        options.ErrorMappings.Should().Contain(mapping => mapping.Kind == ErrorKind.Validation);
        options.ErrorMappings.Should().Contain(mapping => mapping.Kind == ErrorKind.NotFound);
        options.ErrorMappings.Should().Contain(mapping => mapping.Kind == ErrorKind.Failure);
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
    public void ErrorMappings_Should_Not_Be_Externally_Mutable()
    {
        var options = new AtyaProblemDetailsOptions();

        var mappings = options.ErrorMappings.Should().BeAssignableTo<IList<ErrorProblemDetailsMapping>>().Subject;
        var act = () => mappings.Add(new ErrorProblemDetailsMapping(
            ErrorKind.Failure,
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
    public void MapError_Should_Replace_Existing_Mapping_For_Same_Error_Kind()
    {
        var options = new AtyaProblemDetailsOptions();

        options.MapError(ErrorKind.NotFound, 410, "Gone", "urn:test:gone");

        var mappings = options.ErrorMappings.Where(static mapping => mapping.Kind == ErrorKind.NotFound).ToArray();

        mappings.Should().HaveCount(1);
        mappings[0].StatusCode.Should().Be(410);
        mappings[0].Title.Should().Be("Gone");
        mappings[0].Type.Should().Be("urn:test:gone");
    }

    [Fact]
    public void MapError_Should_Store_Detail_Factory_When_Provided()
    {
        var options = new AtyaProblemDetailsOptions();

        options.MapError(
            ErrorKind.Failure,
            422,
            "Unprocessable Entity",
            "urn:test:failure",
            static (_, _) => "Safe detail.");

        var mapping = options.ErrorMappings.Single(static mapping => mapping.Kind == ErrorKind.Failure);

        mapping.DetailFactory.Should().NotBeNull();
        mapping.DetailFactory!(new Error("failure", "Hidden."), new DefaultHttpContext()).Should().Be("Safe detail.");
    }

    [Fact]
    public void MapError_Should_Throw_When_Detail_Factory_Is_Null()
    {
        var options = new AtyaProblemDetailsOptions();

        var act = () => options.MapError(ErrorKind.Failure, 422, "Failure", "urn:test:failure", null!);

        act.Should().Throw<ArgumentNullException>();
    }

    [Fact]
    public void RemoveErrorMapping_Should_Remove_Mapping_For_Error_Kind()
    {
        var options = new AtyaProblemDetailsOptions();

        options.RemoveErrorMapping(ErrorKind.NotFound);

        options.ErrorMappings.Should().NotContain(mapping => mapping.Kind == ErrorKind.NotFound);
    }

    [Fact]
    public void RemoveErrorMapping_Should_Do_Nothing_When_Error_Kind_Is_Not_Mapped()
    {
        var options = new AtyaProblemDetailsOptions();
        var initialCount = options.ErrorMappings.Count;

        options.RemoveErrorMapping((ErrorKind)999);

        options.ErrorMappings.Should().HaveCount(initialCount);
    }

    [Fact]
    public void ClearErrorMappings_Should_Remove_All_Mappings()
    {
        var options = new AtyaProblemDetailsOptions();

        options.ClearErrorMappings();

        options.ErrorMappings.Should().BeEmpty();
    }

    [Fact]
    public void MapError_Should_Throw_When_Title_Is_Empty()
    {
        var options = new AtyaProblemDetailsOptions();

        var act = () => options.MapError(ErrorKind.Failure, 422, " ", "urn:test");

        act.Should().Throw<ArgumentException>();
    }

    [Fact]
    public void MapError_Should_Throw_When_Type_Is_Empty()
    {
        var options = new AtyaProblemDetailsOptions();

        var act = () => options.MapError(ErrorKind.Failure, 422, "Failure", " ");

        act.Should().Throw<ArgumentException>();
    }

    [Theory]
    [InlineData(99)]
    [InlineData(600)]
    public void MapError_Should_Throw_When_Status_Code_Is_Invalid(int statusCode)
    {
        var options = new AtyaProblemDetailsOptions();

        var act = () => options.MapError(ErrorKind.Failure, statusCode, "Failure", "urn:test:failure");

        act.Should().Throw<ArgumentOutOfRangeException>();
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
