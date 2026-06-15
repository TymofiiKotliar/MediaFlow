using FluentAssertions;
using MediaFlow.Application.Tests.Fixtures;
using MediaFlow.Application.UseCases;
using MediaFlow.Domain.Interfaces;
using NSubstitute;

namespace MediaFlow.Application.Tests.UseCases;

public class EditDeviceUseCaseTests
{
    private readonly IDeviceRepository _repository = Substitute.For<IDeviceRepository>();
    private readonly EditDeviceUseCase _sut;

    public EditDeviceUseCaseTests() => _sut = new EditDeviceUseCase(_repository);

    [Fact]
    public async Task ExecuteAsync_ExistingProfile_ReturnsSuccess()
    {
        var existing = DeviceFixtures.ExistingProfile("abc");
        _repository.GetByIdAsync("abc").Returns(existing);

        var result = await _sut.ExecuteAsync("abc", DeviceFixtures.ValidInput());

        result.Should().BeOfType<EditDeviceResult.Success>();
    }

    [Fact]
    public async Task ExecuteAsync_ExistingProfile_PreservesOriginalId()
    {
        var existing = DeviceFixtures.ExistingProfile("abc");
        _repository.GetByIdAsync("abc").Returns(existing);

        var result = (EditDeviceResult.Success)await _sut.ExecuteAsync("abc", DeviceFixtures.ValidInput());

        result.Profile.Id.Should().Be("abc");
    }

    [Fact]
    public async Task ExecuteAsync_ExistingProfile_AppliesNewFieldValues()
    {
        var existing = DeviceFixtures.ExistingProfile("abc");
        _repository.GetByIdAsync("abc").Returns(existing);
        var input = DeviceFixtures.ValidInput(name: "New Name", filesPerLoad: 500);

        var result = (EditDeviceResult.Success)await _sut.ExecuteAsync("abc", input);

        result.Profile.Name.Should().Be("New Name");
        result.Profile.FilesPerLoad.Should().Be(500);
        result.Profile.SourceFolderPath.Should().Be(input.SourceFolderPath);
        result.Profile.BackupFolderPath.Should().Be(input.BackupFolderPath);
    }

    [Fact]
    public async Task ExecuteAsync_ExistingProfile_SavesUpdatedProfile()
    {
        var existing = DeviceFixtures.ExistingProfile("abc");
        _repository.GetByIdAsync("abc").Returns(existing);

        await _sut.ExecuteAsync("abc", DeviceFixtures.ValidInput());

        await _repository.Received(1).SaveAsync(Arg.Is<Domain.Entities.DeviceProfile>(p => p.Id == "abc"));
    }

    [Fact]
    public async Task ExecuteAsync_ProfileNotFound_ReturnsNotFound()
    {
        _repository.GetByIdAsync("missing").Returns((Domain.Entities.DeviceProfile?)null);

        var result = await _sut.ExecuteAsync("missing", DeviceFixtures.ValidInput());

        result.Should().BeOfType<EditDeviceResult.NotFound>()
            .Which.Id.Should().Be("missing");
    }

    [Fact]
    public async Task ExecuteAsync_ProfileNotFound_DoesNotCallSave()
    {
        _repository.GetByIdAsync("missing").Returns((Domain.Entities.DeviceProfile?)null);

        await _sut.ExecuteAsync("missing", DeviceFixtures.ValidInput());

        await _repository.DidNotReceive().SaveAsync(Arg.Any<Domain.Entities.DeviceProfile>());
    }

    [Fact]
    public async Task ExecuteAsync_InvalidInput_ReturnsValidationFailed()
    {
        var existing = DeviceFixtures.ExistingProfile("abc");
        _repository.GetByIdAsync("abc").Returns(existing);

        var result = await _sut.ExecuteAsync("abc", DeviceFixtures.ValidInput(name: ""));

        result.Should().BeOfType<EditDeviceResult.ValidationFailed>();
    }

    [Fact]
    public async Task ExecuteAsync_InvalidInput_DoesNotCallSave()
    {
        var existing = DeviceFixtures.ExistingProfile("abc");
        _repository.GetByIdAsync("abc").Returns(existing);

        await _sut.ExecuteAsync("abc", DeviceFixtures.ValidInput(name: ""));

        await _repository.DidNotReceive().SaveAsync(Arg.Any<Domain.Entities.DeviceProfile>());
    }
}
