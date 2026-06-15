using FluentAssertions;
using MediaFlow.Application.UseCases;
using MediaFlow.Domain.Interfaces;
using MediaFlow.Domain.ValueObjects;
using NSubstitute;

namespace MediaFlow.Application.Tests.UseCases;

public class BuildNamingTemplateUseCaseTests
{
    private static INamingTokenResolver ResolverFor<T>(Func<NamingToken, string> resolve) where T : NamingToken
    {
        var r = Substitute.For<INamingTokenResolver>();
        r.CanResolve(Arg.Any<NamingToken>()).Returns(t => t.Arg<NamingToken>() is T);
        r.Resolve(Arg.Any<NamingToken>(), Arg.Any<string?>(), Arg.Any<int>())
            .Returns(call => resolve(call.Arg<NamingToken>()));
        return r;
    }

    private static BuildNamingTemplateUseCase Sut(params INamingTokenResolver[] resolvers) =>
        new(resolvers);

    [Fact]
    public void Execute_EmptyTemplate_ReturnsOriginalFileName()
    {
        var result = Sut().Execute([], "IMG_001.jpg", null, 1);

        result.Should().Be("IMG_001.jpg");
    }

    [Fact]
    public void Execute_PrefixToken_ReturnsPrefixPlusExtension()
    {
        var resolver = ResolverFor<PrefixToken>(t => ((PrefixToken)t).Text);
        var template = new NamingToken[] { new PrefixToken("Holiday-") };

        var result = Sut(resolver).Execute(template, "photo.jpg", null, 1);

        result.Should().Be("Holiday-.jpg");
    }

    [Fact]
    public void Execute_MultipleTokens_ConcatenatesInOrder()
    {
        var prefixResolver = ResolverFor<PrefixToken>(t => ((PrefixToken)t).Text);
        var seqResolver = ResolverFor<SequenceNumberToken>(_ => "001");
        var template = new NamingToken[] { new PrefixToken("Trip-"), new SequenceNumberToken() };

        var result = Sut(prefixResolver, seqResolver).Execute(template, "img.png", null, 1);

        result.Should().Be("Trip-001.png");
    }

    [Fact]
    public void Execute_TokenWithNoMatchingResolver_IsSkipped()
    {
        var prefixResolver = ResolverFor<PrefixToken>(t => ((PrefixToken)t).Text);
        // no resolver for SequenceNumberToken
        var template = new NamingToken[] { new PrefixToken("A-"), new SequenceNumberToken() };

        var result = Sut(prefixResolver).Execute(template, "img.jpg", null, 1);

        result.Should().Be("A-.jpg");
    }

    [Fact]
    public void Execute_PassesExifCaptureDateToResolver()
    {
        string? capturedDate = null;
        var r = Substitute.For<INamingTokenResolver>();
        r.CanResolve(Arg.Any<NamingToken>()).Returns(true);
        r.Resolve(Arg.Any<NamingToken>(), Arg.Any<string?>(), Arg.Any<int>())
            .Returns(call => { capturedDate = call.ArgAt<string?>(1); return "x"; });

        Sut(r).Execute([new PhotoDateToken()], "f.jpg", "2026-06-15", 1);

        capturedDate.Should().Be("2026-06-15");
    }

    [Fact]
    public void Execute_PassesSequenceNumberToResolver()
    {
        int capturedSeq = -1;
        var r = Substitute.For<INamingTokenResolver>();
        r.CanResolve(Arg.Any<NamingToken>()).Returns(true);
        r.Resolve(Arg.Any<NamingToken>(), Arg.Any<string?>(), Arg.Any<int>())
            .Returns(call => { capturedSeq = call.ArgAt<int>(2); return "x"; });

        Sut(r).Execute([new SequenceNumberToken()], "f.jpg", null, 42);

        capturedSeq.Should().Be(42);
    }

    [Fact]
    public void Execute_PreservesExtensionFromOriginalFileName()
    {
        var r = ResolverFor<PrefixToken>(t => ((PrefixToken)t).Text);

        var result = Sut(r).Execute([new PrefixToken("file")], "source.MP4", null, 1);

        result.Should().EndWith(".MP4");
    }
}
