// <copyright file="ProblemDetailsTwoPointZeroTests.cs" company="Atya">
// Copyright (c) Atya. All rights reserved.
// </copyright>

using System.Diagnostics;
using Atya.Errors.ProblemDetails.Abstractions;
using Atya.Errors.ProblemDetails.Constants;
using Atya.Errors.ProblemDetails.Extensions;
using Atya.Errors.ProblemDetails.Mappings;
using Atya.Errors.ProblemDetails.Options;
using Atya.Foundation.Results;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Options;

namespace ProblemDetails.UnitTests;

public sealed class ProblemDetailsTwoPointZeroTests
{
    private static DefaultHttpContext NewContext() => new() { TraceIdentifier = "trace-123" };

    // ---- most specific mapping, independent of registration order ----------------------------------------

    [Fact]
    public void Exception_Mapping_Should_Not_Depend_On_Registration_Order()
    {
        Action<AtyaProblemDetailsOptions, int>[] registrations =
        [
            (o, _) => o.Map<BaseFailure>(400, "Base", "urn:t:base"),
            (o, _) => o.Map<MiddleFailure>(409, "Middle", "urn:t:middle"),
            (o, _) => o.Map<LeafFailure>(422, "Leaf", "urn:t:leaf"),
        ];

        foreach (var order in Permutations([0, 1, 2]))
        {
            var options = new AtyaProblemDetailsOptions().ClearMappings();
            foreach (var index in order)
            {
                registrations[index](options, index);
            }

            var mapper = DefaultExceptionToProblemDetailsMapper.Create(options);

            mapper.Map(new LeafFailure(), NewContext()).Title.Should().Be("Leaf", string.Join(",", order));
            mapper.Map(new MiddleFailure(), NewContext()).Title.Should().Be("Middle", string.Join(",", order));
            mapper.Map(new BaseFailure(), NewContext()).Title.Should().Be("Base", string.Join(",", order));
            mapper.Map(new SiblingFailure(), NewContext()).Title.Should().Be("Middle", string.Join(",", order));
        }
    }

    [Fact]
    public void Exception_Mapping_Should_Prefer_Derived_Mapping_Even_When_Base_Is_Registered_Last()
    {
        var options = new AtyaProblemDetailsOptions().ClearMappings();
        options.Map<MiddleFailure>(409, "Middle", "urn:t:middle");
        options.Map<BaseFailure>(400, "Base", "urn:t:base");

        DefaultExceptionToProblemDetailsMapper.Create(options)
            .Map(new LeafFailure(), NewContext()).Title.Should().Be("Middle");
    }

    [Fact]
    public void Second_Registration_For_The_Same_Type_Replaces_The_First()
    {
        var options = new AtyaProblemDetailsOptions().ClearMappings();
        options.Map<BaseFailure>(400, "First", "urn:t:first");
        options.Map<BaseFailure>(404, "Second", "urn:t:second");

        DefaultExceptionToProblemDetailsMapper.Create(options)
            .Map(new BaseFailure(), NewContext()).Title.Should().Be("Second");
    }

    // ---- customization on both paths -------------------------------------------------------------------

    [Fact]
    public void Customize_Should_Run_Once_On_The_Exception_Path_With_Exception_And_No_Error()
    {
        var calls = new List<ProblemDetailsCustomizationContext>();
        var options = new AtyaProblemDetailsOptions { Customize = calls.Add };
        var exception = new InvalidOperationException("boom");

        var result = DefaultExceptionToProblemDetailsMapper.Create(options).Map(exception, NewContext());

        var call = calls.Should().ContainSingle().Subject;
        call.Exception.Should().BeSameAs(exception);
        call.Error.Should().BeNull();
        call.ProblemDetails.Should().BeSameAs(result);
        call.HttpContext.Should().NotBeNull();
    }

    [Fact]
    public void Customize_Should_Run_Once_On_The_Result_Path_With_Error_And_No_Exception()
    {
        var calls = new List<ProblemDetailsCustomizationContext>();
        var options = new AtyaProblemDetailsOptions { Customize = calls.Add };
        var error = new Error("code", "message", kind: ErrorKind.NotFound);

        var result = DefaultResultToProblemDetailsMapper.Create(options).Map(error, NewContext());

        var call = calls.Should().ContainSingle().Subject;
        call.Error.Should().BeSameAs(error);
        call.Exception.Should().BeNull();
        call.ProblemDetails.Should().BeSameAs(result);
    }

    [Fact]
    public void Customize_Should_Run_After_Extensions_And_May_Overwrite_Them()
    {
        var options = new AtyaProblemDetailsOptions
        {
            Customize = context => context.ProblemDetails.Extensions[ProblemDetailsExtensionNames.TraceId] = "overwritten",
        };

        var viaResult = DefaultResultToProblemDetailsMapper.Create(options)
            .Map(new Error("code", "message", kind: ErrorKind.Failure), NewContext());

        viaResult.Extensions[ProblemDetailsExtensionNames.TraceId].Should().Be("overwritten");
    }

    [Fact]
    public void Obsolete_CustomizeProblemDetails_Still_Runs_On_The_Exception_Path_Only()
    {
        var calls = 0;
#pragma warning disable CS0618 // Verifying the obsolete sugar still works in 2.0.
        var options = new AtyaProblemDetailsOptions { CustomizeProblemDetails = (_, _, _) => calls++ };
#pragma warning restore CS0618

        DefaultExceptionToProblemDetailsMapper.Create(options).Map(new InvalidOperationException("x"), NewContext());
        DefaultResultToProblemDetailsMapper.Create(options).Map(new Error("c", "m", kind: ErrorKind.Failure), NewContext());

        calls.Should().Be(1);
    }

    // ---- snapshot ----------------------------------------------------------------------------------------

    [Fact]
    public void Mappers_Should_Ignore_Option_Changes_Made_After_Construction()
    {
        var options = new AtyaProblemDetailsOptions();
        var exceptionMapper = DefaultExceptionToProblemDetailsMapper.Create(options);
        var resultMapper = DefaultResultToProblemDetailsMapper.Create(options);

        options.IncludeTraceId = false;
        options.ExtensionNames.TraceId = "renamed";
        options.Map<InvalidOperationException>(418, "Teapot", "urn:t:teapot");
        options.MapError(ErrorKind.NotFound, 410, "Gone", "urn:t:gone");
        options.CorrelationIdAccessor = _ => "late";
        options.Customize = context => context.ProblemDetails.Title = "late";

        var fromException = exceptionMapper.Map(new InvalidOperationException("x"), NewContext());
        var fromError = resultMapper.Map(new Error("c", "m", kind: ErrorKind.NotFound), NewContext());

        fromException.Status.Should().Be(500);
        fromException.Extensions.Should().ContainKey(ProblemDetailsExtensionNames.TraceId);
        fromException.Extensions.Should().NotContainKey("renamed").And.NotContainKey(ProblemDetailsExtensionNames.CorrelationId);
        fromException.Title.Should().NotBe("late");
        fromError.Status.Should().Be(404);
        fromError.Title.Should().NotBe("late");
    }

    // ---- validation --------------------------------------------------------------------------------------

    [Theory]
    [InlineData(200)]
    [InlineData(302)]
    [InlineData(399)]
    public void Non_Error_Status_Codes_Should_Be_Rejected_By_Default(int status)
    {
        var options = new AtyaProblemDetailsOptions().MapError(ErrorKind.Failure, status, "T", "urn:t");

        var act = () => DefaultResultToProblemDetailsMapper.Create(options);

        act.Should().Throw<OptionsValidationException>().WithMessage($"*{status}*");
    }

    [Fact]
    public void AllowNonErrorStatusCodes_Can_Be_Set_After_The_Mapping_Calls()
    {
        var options = new AtyaProblemDetailsOptions().MapError(ErrorKind.Failure, 200, "OK", "urn:t");
        options.AllowNonErrorStatusCodes = true;

        var result = DefaultResultToProblemDetailsMapper.Create(options)
            .Map(new Error("c", "m", kind: ErrorKind.Failure), NewContext());

        result.Status.Should().Be(200);
    }

    [Fact]
    public void Map_Should_Still_Reject_Codes_Outside_The_Http_Range()
    {
        var options = new AtyaProblemDetailsOptions();

        options.Invoking(o => o.Map<BaseFailure>(99, "T", "urn:t")).Should().Throw<ArgumentOutOfRangeException>();
        options.Invoking(o => o.Map<BaseFailure>(600, "T", "urn:t")).Should().Throw<ArgumentOutOfRangeException>();
    }

    [Theory]
    [InlineData("type")]
    [InlineData("title")]
    [InlineData("status")]
    [InlineData("detail")]
    [InlineData("instance")]
    [InlineData("Detail")]
    public void Reserved_Extension_Names_Should_Be_Rejected(string name)
    {
        var options = new AtyaProblemDetailsOptions();
        options.ExtensionNames.TraceId = name;

        var act = () => DefaultExceptionToProblemDetailsMapper.Create(options);

        act.Should().Throw<OptionsValidationException>().WithMessage("*reserved*");
    }

    [Fact]
    public void Duplicate_Extension_Names_Should_Be_Rejected()
    {
        var options = new AtyaProblemDetailsOptions();
        options.ExtensionNames.TraceId = "id";
        options.ExtensionNames.CorrelationId = "ID";

        var act = () => DefaultExceptionToProblemDetailsMapper.Create(options);

        act.Should().Throw<OptionsValidationException>().WithMessage("*duplicates*");
    }

    [Fact]
    public void Duplicate_Validation_Member_Names_Should_Be_Rejected()
    {
        var options = new AtyaProblemDetailsOptions();
        options.ExtensionNames.ValidationMessage = "propertyName";

        var act = () => DefaultExceptionToProblemDetailsMapper.Create(options);

        act.Should().Throw<OptionsValidationException>();
    }

    [Fact]
    public void Disabled_Extension_Names_Should_Remain_Valid()
    {
        var options = new AtyaProblemDetailsOptions();
        options.ExtensionNames.TraceId = null;
        options.ExtensionNames.CorrelationId = " ";

        var result = DefaultExceptionToProblemDetailsMapper.Create(options).Map(new InvalidOperationException("x"), NewContext());

        result.Extensions.Should().NotContainKey(ProblemDetailsExtensionNames.TraceId);
    }

    [Fact]
    public async Task Invalid_Options_Should_Fail_Host_Start()
    {
        using var host = new HostBuilder()
            .ConfigureWebHost(web =>
            {
                web.UseTestServer();
                web.ConfigureServices(services => services.AddAtyaProblemDetails(o => o.ExtensionNames.TraceId = "title"));
                web.Configure(_ => { });
            })
            .Build();

        var act = () => host.StartAsync(TestContext.Current.CancellationToken);

        await act.Should().ThrowAsync<OptionsValidationException>();
    }

    [Fact]
    public async Task Invalid_Status_Should_Fail_Host_Start()
    {
        using var host = new HostBuilder()
            .ConfigureWebHost(web =>
            {
                web.UseTestServer();
                web.ConfigureServices(services => services.AddAtyaProblemDetails(o => o.MapError(ErrorKind.Failure, 200, "OK", "urn:t")));
                web.Configure(_ => { });
            })
            .Build();

        var act = () => host.StartAsync(TestContext.Current.CancellationToken);

        await act.Should().ThrowAsync<OptionsValidationException>();
    }

    // ---- trace id ----------------------------------------------------------------------------------------

    [Fact]
    public void TraceId_Should_Be_The_W3C_Trace_Id_Without_Span_Or_Flags()
    {
        using var activity = new Activity("w3c");
        activity.SetIdFormat(ActivityIdFormat.W3C);
        activity.Start();

        var traceId = Map_TraceId();

        traceId.Should().Be(activity.TraceId.ToString()).And.MatchRegex("^[0-9a-f]{32}$");
        traceId.Should().NotBe(activity.Id);
    }

    [Fact]
    public void TraceId_Should_Fall_Back_To_TraceIdentifier_For_A_Hierarchical_Activity()
    {
        using var activity = new Activity("hier");
        activity.SetIdFormat(ActivityIdFormat.Hierarchical);
        activity.Start();

        Map_TraceId().Should().Be("trace-123").And.NotBe(activity.Id);
    }

    [Fact]
    public void TraceId_Should_Fall_Back_To_TraceIdentifier_Without_An_Activity()
    {
        Activity.Current.Should().BeNull();

        Map_TraceId().Should().Be("trace-123");
    }

    [Fact]
    public void TraceId_Should_Match_On_The_Result_Path_Too()
    {
        using var activity = new Activity("w3c");
        activity.SetIdFormat(ActivityIdFormat.W3C);
        activity.Start();

        var result = DefaultResultToProblemDetailsMapper.Create(new AtyaProblemDetailsOptions())
            .Map(new Error("c", "m", kind: ErrorKind.Failure), NewContext());

        result.Extensions[ProblemDetailsExtensionNames.TraceId].Should().Be(activity.TraceId.ToString());
    }

    // ---- new ErrorKind rows ------------------------------------------------------------------------------

    [Theory]
    [InlineData(ErrorKind.TooManyRequests, 429, "Too Many Requests", "urn:atya:problem-type:too-many-requests")]
    [InlineData(ErrorKind.Unavailable, 503, "Service Unavailable", "urn:atya:problem-type:service-unavailable")]
    [InlineData(ErrorKind.Timeout, 504, "Gateway Timeout", "urn:atya:problem-type:gateway-timeout")]
    public void New_ErrorKinds_Should_Map_To_Their_Status_Title_And_Type(ErrorKind kind, int status, string title, string type)
    {
        var result = DefaultResultToProblemDetailsMapper.Create(new AtyaProblemDetailsOptions())
            .Map(new Error("code", "internal detail", kind: kind), NewContext());

        result.Status.Should().Be(status);
        result.Title.Should().Be(title);
        result.Type.Should().Be(type);
        result.Extensions.Should().NotContainKey("Retry-After");
    }

    [Fact]
    public void TooManyRequests_Should_Show_Detail_And_ErrorCode()
    {
        var result = DefaultResultToProblemDetailsMapper.Create(new AtyaProblemDetailsOptions())
            .Map(new Error("rate.limited", "Slow down.", kind: ErrorKind.TooManyRequests), NewContext());

        result.Detail.Should().Be("Slow down.");
        result.Extensions[ProblemDetailsExtensionNames.ErrorCode].Should().Be("rate.limited");
    }

    [Theory]
    [InlineData(ErrorKind.Unavailable)]
    [InlineData(ErrorKind.Timeout)]
    public void Unavailable_And_Timeout_Should_Mask_Detail_And_ErrorCode(ErrorKind kind)
    {
        var result = DefaultResultToProblemDetailsMapper.Create(new AtyaProblemDetailsOptions())
            .Map(new Error("db.down", "connection string leaked", kind: kind), NewContext());

        result.Detail.Should().Be("An unexpected error occurred.");
        result.Extensions.Should().NotContainKey(ProblemDetailsExtensionNames.ErrorCode);
    }

    // ---- DI ----------------------------------------------------------------------------------------------

    [Fact]
    public void Both_Mappers_Should_Resolve_When_Concrete_Options_And_IOptions_Are_Registered()
    {
        var services = new ServiceCollection();
        services.AddOptions();
        services.AddAtyaProblemDetails();
        services.AddSingleton(new AtyaProblemDetailsOptions());

        using var provider = services.BuildServiceProvider();

        provider.GetRequiredService<IExceptionToProblemDetailsMapper>().Should().BeOfType<DefaultExceptionToProblemDetailsMapper>();
        provider.GetRequiredService<IResultToProblemDetailsMapper>().Should().BeOfType<DefaultResultToProblemDetailsMapper>();
    }

    [Fact]
    public void Mappers_Should_Expose_A_Single_Public_Constructor()
    {
        typeof(DefaultExceptionToProblemDetailsMapper).GetConstructors().Should().ContainSingle();
        typeof(DefaultResultToProblemDetailsMapper).GetConstructors().Should().ContainSingle();
    }

    // ---- default correlation -----------------------------------------------------------------------------

    [Fact]
    public void Default_Correlation_Accessor_Should_Add_No_Extension_Even_When_The_Header_Is_Present()
    {
        var context = NewContext();
        context.Request.Headers["X-Correlation-ID"] = "abc";

        var result = DefaultExceptionToProblemDetailsMapper.Create(new AtyaProblemDetailsOptions())
            .Map(new InvalidOperationException("x"), context);

        result.Extensions.Should().NotContainKey(ProblemDetailsExtensionNames.CorrelationId);
    }

    private static string? Map_TraceId()
    {
        var result = DefaultExceptionToProblemDetailsMapper.Create(new AtyaProblemDetailsOptions())
            .Map(new InvalidOperationException("x"), NewContext());

        return result.Extensions[ProblemDetailsExtensionNames.TraceId] as string;
    }

    private static IEnumerable<int[]> Permutations(int[] items)
    {
        if (items.Length <= 1)
        {
            yield return items;
            yield break;
        }

        for (var i = 0; i < items.Length; i++)
        {
            var rest = items.Where((_, index) => index != i).ToArray();
            foreach (var tail in Permutations(rest))
            {
                yield return [items[i], .. tail];
            }
        }
    }

    private class BaseFailure : Exception
    {
    }

    private class MiddleFailure : BaseFailure
    {
    }

    private sealed class LeafFailure : MiddleFailure
    {
    }

    private sealed class SiblingFailure : MiddleFailure
    {
    }
}
