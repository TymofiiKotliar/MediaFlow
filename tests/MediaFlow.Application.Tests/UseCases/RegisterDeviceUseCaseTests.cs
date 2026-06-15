using FluentAssertions;
using MediaFlow.Application.Tests.Fixtures;
using MediaFlow.Application.UseCases;
using MediaFlow.Domain.Interfaces;
using NSubstitute;

namespace MediaFlow.Application.Tests.UseCases;

public class RegisterDeviceUseCaseTests
{
    private readonly IDeviceRepository _repository = Substitute.For<IDeviceRepository>();
    private readonly RegisterDeviceUseCase _sut;

    public RegisterDeviceUseCaseTests() => _sut = new RegisterDeviceUseCase(_repository);

    [Fact]
    public async Task ExecuteAsync_ValidInput_ReturnsSuccess()
    {
        var result = await _sut.ExecuteAsync(DeviceFixtures.ValidInput());

        result.Should().BeOfType<RegisterDeviceResult.Success>();
    }

    [Fact]
    public async Task ExecuteAsync_ValidInput_SavesProfileToRepository()
    {
        await _sut.ExecuteAsync(DeviceFixtures.ValidInput());

        await _repository.Received(1).SaveAsync(Arg.Any<Domain.Entities.DeviceProfile>());
    }

    [Fact]
    public async Task ExecuteAsync_ValidInput_ProfileHasCorrectFields()
    {
        var input = DeviceFixtures.ValidInput(name: "Sony A7", filesPerLoad: 200);

        var result = (RegisterDeviceResult.Success)await _sut.ExecuteAsync(input);

        result.Profile.Name.Should().Be("Sony A7");
        result.Profile.FilesPerLoad.Should().Be(200);
        result.Profile.SourceFolderPath.Should().Be(input.SourceFolderPath);
        result.Profile.BackupFolderPath.Should().Be(input.BackupFolderPath);
    }

    [Fact]
    public async Task ExecuteAsync_ValidInput_AssignsNewGuidAsId()
    {
        var result = (RegisterDeviceResult.Success)await _sut.ExecuteAsync(DeviceFixtures.ValidInput());

        Guid.TryParse(result.Profile.Id, out _).Should().BeTrue();
    }

    [Fact]
    public async Task ExecuteAsync_CalledTwice_GeneratesDifferentIds()
    {
        var r1 = (RegisterDeviceResult.Success)await _sut.ExecuteAsync(DeviceFixtures.ValidInput());
        var r2 = (RegisterDeviceResult.Success)await _sut.ExecuteAsync(DeviceFixtures.ValidInput());

        r1.Profile.Id.Should().NotBe(r2.Profile.Id);
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public async Task ExecuteAsync_BlankName_ReturnsValidationFailed(string name)
    {
        var result = await _sut.ExecuteAsync(DeviceFixtures.ValidInput(name: name));

        result.Should().BeOfType<RegisterDeviceResult.ValidationFailed>()
            .Which.Errors.Should().ContainSingle(e => e.Contains("Device name"));
    }

    [Theory]
    [InlineData("")]
    [InlineData("relative/path")]
    public async Task ExecuteAsync_InvalidSourcePath_ReturnsValidationFailed(string path)
    {
        var result = await _sut.ExecuteAsync(DeviceFixtures.ValidInput(sourcePath: path));

        result.Should().BeOfType<RegisterDeviceResult.ValidationFailed>()
            .Which.Errors.Should().ContainSingle(e => e.Contains("Source folder"));
    }

    [Fact]
    public async Task ExecuteAsync_SourcePathWithInvalidChars_ReturnsValidationFailed()
    {
        var result = await _sut.ExecuteAsync(DeviceFixtures.ValidInput(sourcePath: "C:\\bad\0path"));

        result.Should().BeOfType<RegisterDeviceResult.ValidationFailed>()
            .Which.Errors.Should().ContainSingle(e => e.Contains("Source folder"));
    }

    [Theory]
    [InlineData("")]
    [InlineData("relative/path")]
    public async Task ExecuteAsync_InvalidBackupPath_ReturnsValidationFailed(string path)
    {
        var result = await _sut.ExecuteAsync(DeviceFixtures.ValidInput(backupPath: path));

        result.Should().BeOfType<RegisterDeviceResult.ValidationFailed>()
            .Which.Errors.Should().ContainSingle(e => e.Contains("Backup folder"));
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public async Task ExecuteAsync_BlankBotToken_ReturnsValidationFailed(string token)
    {
        var result = await _sut.ExecuteAsync(DeviceFixtures.ValidInput(botToken: token));

        result.Should().BeOfType<RegisterDeviceResult.ValidationFailed>()
            .Which.Errors.Should().ContainSingle(e => e.Contains("bot token"));
    }

    [Theory]
    [InlineData("")]
    [InlineData("not-digits")]
    [InlineData("123abc")]
    public async Task ExecuteAsync_InvalidChatId_ReturnsValidationFailed(string chatId)
    {
        var result = await _sut.ExecuteAsync(DeviceFixtures.ValidInput(chatId: chatId));

        result.Should().BeOfType<RegisterDeviceResult.ValidationFailed>()
            .Which.Errors.Should().ContainSingle(e => e.Contains("chat ID"));
    }

    [Theory]
    [InlineData("-123456789")]
    [InlineData("987654321")]
    public async Task ExecuteAsync_ValidChatId_ReturnsSuccess(string chatId)
    {
        var result = await _sut.ExecuteAsync(DeviceFixtures.ValidInput(chatId: chatId));

        result.Should().BeOfType<RegisterDeviceResult.Success>();
    }

    [Theory]
    [InlineData(9)]
    [InlineData(1001)]
    public async Task ExecuteAsync_FilesPerLoadOutOfRange_ReturnsValidationFailed(int limit)
    {
        var result = await _sut.ExecuteAsync(DeviceFixtures.ValidInput(filesPerLoad: limit));

        result.Should().BeOfType<RegisterDeviceResult.ValidationFailed>()
            .Which.Errors.Should().ContainSingle(e => e.Contains("Files per load"));
    }

    [Theory]
    [InlineData(10)]
    [InlineData(1000)]
    public async Task ExecuteAsync_FilesPerLoadAtBoundary_ReturnsSuccess(int limit)
    {
        var result = await _sut.ExecuteAsync(DeviceFixtures.ValidInput(filesPerLoad: limit));

        result.Should().BeOfType<RegisterDeviceResult.Success>();
    }

    [Fact]
    public async Task ExecuteAsync_MultipleInvalidFields_ReturnsAllErrors()
    {
        var input = DeviceFixtures.ValidInput(name: "", chatId: "bad", filesPerLoad: 0);

        var result = (RegisterDeviceResult.ValidationFailed)await _sut.ExecuteAsync(input);

        result.Errors.Should().HaveCountGreaterThanOrEqualTo(3);
    }

    [Fact]
    public async Task ExecuteAsync_ValidationFailed_DoesNotCallRepository()
    {
        await _sut.ExecuteAsync(DeviceFixtures.ValidInput(name: ""));

        await _repository.DidNotReceive().SaveAsync(Arg.Any<Domain.Entities.DeviceProfile>());
    }
}
