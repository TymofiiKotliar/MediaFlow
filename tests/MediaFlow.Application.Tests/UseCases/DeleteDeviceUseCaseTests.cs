using FluentAssertions;
using MediaFlow.Application.Tests.Fixtures;
using MediaFlow.Application.UseCases;
using MediaFlow.Domain.Interfaces;
using NSubstitute;

namespace MediaFlow.Application.Tests.UseCases;

public class DeleteDeviceUseCaseTests
{
    private readonly IDeviceRepository _repository = Substitute.For<IDeviceRepository>();
    private readonly IDeviceProfilePictureStore _pictureStore = Substitute.For<IDeviceProfilePictureStore>();
    private readonly DeleteDeviceUseCase _sut;

    public DeleteDeviceUseCaseTests() => _sut = new DeleteDeviceUseCase(_repository, _pictureStore);

    [Fact]
    public async Task ExecuteAsync_ExistingProfile_ReturnsSuccess()
    {
        _repository.GetByIdAsync("abc").Returns(DeviceFixtures.ExistingProfile("abc"));

        var result = await _sut.ExecuteAsync("abc");

        result.Should().BeOfType<DeleteDeviceResult.Success>();
    }

    [Fact]
    public async Task ExecuteAsync_ExistingProfile_CallsDeleteWithCorrectId()
    {
        _repository.GetByIdAsync("abc").Returns(DeviceFixtures.ExistingProfile("abc"));

        await _sut.ExecuteAsync("abc");

        await _repository.Received(1).DeleteAsync("abc");
    }

    [Fact]
    public async Task ExecuteAsync_ProfileNotFound_ReturnsNotFound()
    {
        _repository.GetByIdAsync("missing").Returns((Domain.Entities.DeviceProfile?)null);

        var result = await _sut.ExecuteAsync("missing");

        result.Should().BeOfType<DeleteDeviceResult.NotFound>()
            .Which.Id.Should().Be("missing");
    }

    [Fact]
    public async Task ExecuteAsync_ProfileNotFound_DoesNotCallDelete()
    {
        _repository.GetByIdAsync("missing").Returns((Domain.Entities.DeviceProfile?)null);

        await _sut.ExecuteAsync("missing");

        await _repository.DidNotReceive().DeleteAsync(Arg.Any<string>());
    }
}
