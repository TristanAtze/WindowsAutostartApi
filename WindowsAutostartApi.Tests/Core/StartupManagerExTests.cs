using FluentAssertions;
using Moq;
using WindowsAutostartApi.Abstractions;
using WindowsAutostartApi.Core;

namespace WindowsAutostartApi.Tests.Core;

public class StartupManagerExTests
{
    private readonly Mock<IStartupProvider> _mockRegistryProvider;
    private readonly Mock<IStartupProvider> _mockFolderProvider;
    private readonly StartupManagerEx _manager;

    public StartupManagerExTests()
    {
        _mockRegistryProvider = new Mock<IStartupProvider>();
        _mockFolderProvider = new Mock<IStartupProvider>();

        _mockRegistryProvider.Setup(x => x.Supports(StartupKind.Run)).Returns(true);
        _mockRegistryProvider.Setup(x => x.Supports(StartupKind.RunOnce)).Returns(true);
        _mockRegistryProvider.Setup(x => x.Supports(StartupKind.StartupFolder)).Returns(false);

        _mockFolderProvider.Setup(x => x.Supports(StartupKind.StartupFolder)).Returns(true);
        _mockFolderProvider.Setup(x => x.Supports(StartupKind.Run)).Returns(false);
        _mockFolderProvider.Setup(x => x.Supports(StartupKind.RunOnce)).Returns(false);

        _manager = new StartupManagerEx(new[] { _mockRegistryProvider.Object, _mockFolderProvider.Object });
    }

    [Fact]
    public void TryListAll_WithSuccessfulProviders_ShouldReturnSuccess()
    {
        // Arrange
        var registryEntries = new[]
        {
            new StartupEntry("App1", @"C:\App1.exe", null, StartupScope.CurrentUser, StartupKind.Run)
        };

        var folderEntries = new[]
        {
            new StartupEntry("App2", @"C:\App2.exe", null, StartupScope.CurrentUser, StartupKind.StartupFolder)
        };

        _mockRegistryProvider.Setup(x => x.ListAll()).Returns(registryEntries);
        _mockFolderProvider.Setup(x => x.ListAll()).Returns(folderEntries);

        // Act
        var result = _manager.TryListAll();

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Should().HaveCount(2);
        result.Value.Should().Contain(registryEntries);
        result.Value.Should().Contain(folderEntries);
    }

    [Fact]
    public void TryListAll_WithFailingProvider_ShouldContinueWithOtherProviders()
    {
        // Arrange
        var folderEntries = new[]
        {
            new StartupEntry("App1", @"C:\App1.exe", null, StartupScope.CurrentUser, StartupKind.StartupFolder)
        };

        _mockRegistryProvider.Setup(x => x.ListAll()).Throws(new UnauthorizedAccessException("Access denied"));
        _mockFolderProvider.Setup(x => x.ListAll()).Returns(folderEntries);

        // Act
        var result = _manager.TryListAll();

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Should().HaveCount(1);
        result.Value.Should().Contain(folderEntries);
    }

    [Fact]
    public void TryExists_WithValidInput_ShouldReturnSuccess()
    {
        // Arrange
        _mockRegistryProvider.Setup(x => x.Exists("TestApp", StartupScope.CurrentUser, StartupKind.Run))
            .Returns(true);

        // Act
        var result = _manager.TryExists("TestApp", StartupScope.CurrentUser, StartupKind.Run);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Should().BeTrue();
    }

    [Fact]
    public void TryExists_WithUnsupportedKind_ShouldReturnFailure()
    {
        // Arrange
        var manager = new StartupManagerEx(new IStartupProvider[0]);

        // Act
        var result = manager.TryExists("TestApp", StartupScope.CurrentUser, StartupKind.Run);

        // Assert
        result.IsSuccess.Should().BeFalse();
        result.ErrorMessage.Should().Contain("No provider supports kind");
    }

    [Fact]
    public void TryExists_WithProviderException_ShouldReturnFailure()
    {
        // Arrange
        _mockRegistryProvider.Setup(x => x.Exists("TestApp", StartupScope.CurrentUser, StartupKind.Run))
            .Throws(new UnauthorizedAccessException("Access denied"));

        // Act
        var result = _manager.TryExists("TestApp", StartupScope.CurrentUser, StartupKind.Run);

        // Assert
        result.IsSuccess.Should().BeFalse();
        result.ErrorMessage.Should().Contain("Failed to check if startup entry 'TestApp' exists");
        result.Exception.Should().BeOfType<UnauthorizedAccessException>();
    }

    [Fact]
    public void TryAdd_WithValidEntry_ShouldReturnSuccess()
    {
        // Arrange
        var entry = new StartupEntry("TestApp", @"C:\TestApp.exe", "--arg", StartupScope.CurrentUser, StartupKind.Run);

        // Act
        var result = _manager.TryAdd(entry);

        // Assert
        result.IsSuccess.Should().BeTrue();
        _mockRegistryProvider.Verify(x => x.Add(entry), Times.Once);
    }

    [Fact]
    public void TryAdd_WithInvalidEntry_ShouldReturnFailure()
    {
        // Arrange
        var entry = new StartupEntry("", @"C:\TestApp.exe", null, StartupScope.CurrentUser, StartupKind.Run);

        // Act
        var result = _manager.TryAdd(entry);

        // Assert
        result.IsSuccess.Should().BeFalse();
        result.ErrorMessage.Should().Contain("Entry name cannot be empty");
        _mockRegistryProvider.Verify(x => x.Add(It.IsAny<StartupEntry>()), Times.Never);
    }

    [Fact]
    public void TryAdd_WithProviderException_ShouldReturnFailure()
    {
        // Arrange
        var entry = new StartupEntry("TestApp", @"C:\TestApp.exe", null, StartupScope.CurrentUser, StartupKind.Run);
        _mockRegistryProvider.Setup(x => x.Add(entry)).Throws(new UnauthorizedAccessException("Access denied"));

        // Act
        var result = _manager.TryAdd(entry);

        // Assert
        result.IsSuccess.Should().BeFalse();
        result.ErrorMessage.Should().Contain("Failed to add startup entry 'TestApp'");
        result.Exception.Should().BeOfType<UnauthorizedAccessException>();
    }

    [Fact]
    public void TryRemove_WithValidInput_ShouldReturnSuccess()
    {
        // Act
        var result = _manager.TryRemove("TestApp", StartupScope.CurrentUser, StartupKind.Run);

        // Assert
        result.IsSuccess.Should().BeTrue();
        _mockRegistryProvider.Verify(x => x.Remove("TestApp", StartupScope.CurrentUser, StartupKind.Run), Times.Once);
    }

    [Fact]
    public void TryRemove_WithUnsupportedKind_ShouldReturnFailure()
    {
        // Arrange
        var manager = new StartupManagerEx(new IStartupProvider[0]);

        // Act
        var result = manager.TryRemove("TestApp", StartupScope.CurrentUser, StartupKind.Run);

        // Assert
        result.IsSuccess.Should().BeFalse();
        result.ErrorMessage.Should().Contain("No provider supports kind");
    }

    [Fact]
    public void TryRemove_WithProviderException_ShouldReturnFailure()
    {
        // Arrange
        _mockRegistryProvider.Setup(x => x.Remove("TestApp", StartupScope.CurrentUser, StartupKind.Run))
            .Throws(new IOException("File locked"));

        // Act
        var result = _manager.TryRemove("TestApp", StartupScope.CurrentUser, StartupKind.Run);

        // Assert
        result.IsSuccess.Should().BeFalse();
        result.ErrorMessage.Should().Contain("Failed to remove startup entry 'TestApp'");
        result.Exception.Should().BeOfType<IOException>();
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData(null)]
    public void TryAdd_WithInvalidName_ShouldReturnFailure(string? invalidName)
    {
        // Arrange
        var entry = new StartupEntry(invalidName!, @"C:\TestApp.exe", null, StartupScope.CurrentUser, StartupKind.Run);

        // Act
        var result = _manager.TryAdd(entry);

        // Assert
        result.IsSuccess.Should().BeFalse();
        result.ErrorMessage.Should().Contain("Entry name cannot be empty");
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData(null)]
    public void TryAdd_WithInvalidPath_ShouldReturnFailure(string? invalidPath)
    {
        // Arrange
        var entry = new StartupEntry("TestApp", invalidPath!, null, StartupScope.CurrentUser, StartupKind.Run);

        // Act
        var result = _manager.TryAdd(entry);

        // Assert
        result.IsSuccess.Should().BeFalse();
        result.ErrorMessage.Should().Contain("TargetPath cannot be empty");
    }
}