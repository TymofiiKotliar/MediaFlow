using FluentAssertions;
using MediaFlow.Application.Tests.Fixtures;
using MediaFlow.Application.UseCases;
using MediaFlow.Domain.Interfaces;
using NSubstitute;
using NSubstitute.ExceptionExtensions;

namespace MediaFlow.Application.Tests.UseCases;

public class LoadMediaUseCaseTests
{
    private readonly IMediaLoader _loader = Substitute.For<IMediaLoader>();
    private readonly LoadMediaUseCase _sut;
    private readonly Domain.Entities.DeviceProfile _device = DeviceFixtures.ExistingProfile();

    public LoadMediaUseCaseTests() => _sut = new LoadMediaUseCase(_loader);

    [Fact]
    public async Task ExecuteAsync_LoaderReturnsFiles_ReturnsSuccess()
    {
        var files = new[] { DeviceFixtures.ImageFile(), DeviceFixtures.VideoFile() };
        _loader.LoadBatchAsync(Arg.Any<string>(), Arg.Any<int>(), Arg.Any<int>(), Arg.Any<CancellationToken>())
            .Returns(files);

        var result = await _sut.ExecuteAsync(_device, offset: 0);

        result.Should().BeOfType<LoadMediaResult.Success>()
            .Which.Files.Should().BeEquivalentTo(files);
    }

    [Fact]
    public async Task ExecuteAsync_PassesCorrectOffsetAndLimitToLoader()
    {
        _loader.LoadBatchAsync(Arg.Any<string>(), Arg.Any<int>(), Arg.Any<int>(), Arg.Any<CancellationToken>())
            .Returns([]);

        await _sut.ExecuteAsync(_device, offset: 50);

        await _loader.Received(1).LoadBatchAsync(_device.SourceFolderPath, 50, _device.FilesPerLoad, Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task ExecuteAsync_LoaderThrows_ReturnsSourceNotAccessible()
    {
        _loader.LoadBatchAsync(Arg.Any<string>(), Arg.Any<int>(), Arg.Any<int>(), Arg.Any<CancellationToken>())
            .Throws(new IOException("Access denied"));

        var result = await _sut.ExecuteAsync(_device, offset: 0);

        result.Should().BeOfType<LoadMediaResult.SourceNotAccessible>()
            .Which.Path.Should().Be(_device.SourceFolderPath);
    }

    [Fact]
    public async Task ExecuteAsync_LoaderThrows_ResultContainsExceptionMessage()
    {
        _loader.LoadBatchAsync(Arg.Any<string>(), Arg.Any<int>(), Arg.Any<int>(), Arg.Any<CancellationToken>())
            .Throws(new IOException("Access denied"));

        var result = (LoadMediaResult.SourceNotAccessible)await _sut.ExecuteAsync(_device, offset: 0);

        result.Reason.Should().Contain("Access denied");
    }
}
