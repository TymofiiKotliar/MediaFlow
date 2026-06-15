using FluentAssertions;
using MediaFlow.Application.Tests.Fixtures;
using MediaFlow.Application.UseCases;
using MediaFlow.Domain.Interfaces;
using NSubstitute;

namespace MediaFlow.Application.Tests.UseCases;

public class GetAllDevicesUseCaseTests
{
    private readonly IDeviceRepository _repository = Substitute.For<IDeviceRepository>();
    private readonly GetAllDevicesUseCase _sut;

    public GetAllDevicesUseCaseTests() => _sut = new GetAllDevicesUseCase(_repository);

    [Fact]
    public async Task ExecuteAsync_ReturnsProfilesFromRepository()
    {
        var profiles = new[] { DeviceFixtures.ExistingProfile("a"), DeviceFixtures.ExistingProfile("b") };
        _repository.GetAllAsync().Returns(profiles);

        var result = await _sut.ExecuteAsync();

        result.Should().BeEquivalentTo(profiles);
    }

    [Fact]
    public async Task ExecuteAsync_RepositoryReturnsEmpty_ReturnsEmptyList()
    {
        _repository.GetAllAsync().Returns(Array.Empty<Domain.Entities.DeviceProfile>());

        var result = await _sut.ExecuteAsync();

        result.Should().BeEmpty();
    }
}
