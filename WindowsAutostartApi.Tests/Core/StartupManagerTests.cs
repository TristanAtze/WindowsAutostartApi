using FluentAssertions;
using Moq;
using WindowsAutostartApi.Abstractions;
using WindowsAutostartApi.Core;

namespace WindowsAutostartApi.Tests.Core;

public class StartupManagerTests
{
    private readonly Mock<IStartupProvider> _mockRegistryProvider;
    private readonly Mock<IStartupProvider> _mockFolderProvider;
    private readonly StartupManager _manager;

    public StartupManagerTests()
    {
        _mockRegistryProvider = new Mock<IStartupProvider>();
        _mockFolderProvider = new Mock<IStartupProvider>();

        _mockRegistryProvider.Setup(x => x.Supports(StartupKind.Run)).Returns(true);
        _mockRegistryProvider.Setup(x => x.Supports(StartupKind.RunOnce)).Returns(true);
        _mockRegistryProvider.Setup(x => x.Supports(StartupKind.StartupFolder)).Returns(false);

        _mockFolderProvider.Setup(x => x.Supports(StartupKind.StartupFolder)).Returns(true);
        _mockFolderProvider.Setup(x => x.Supports(StartupKind.Run)).Returns(false);
        _mockFolderProvider.Setup(x => x.Supports(StartupKind.RunOnce)).Returns(false);

        _manager = new StartupManager(new[] { _mockRegistryProvider.Object, _mockFolderProvider.Object });
    }

    [Fact]
    public void ListAll_ShouldCombineResultsFromAllProviders()
    {
        // Arrange
        var registryEntries = new[]
        {
            new StartupEntry("App1", @"C:\App1.exe", null, StartupScope.CurrentUser, StartupKind.Run),
            new StartupEntry("App2", @"C:\App2.exe", "--start", StartupScope.AllUsers, StartupKind.RunOnce)
        };

        var folderEntries = new[]
        {
            new StartupEntry("App3", @"C:\App3.exe", null, StartupScope.CurrentUser, StartupKind.StartupFolder)
        };

        _mockRegistryProvider.Setup(x => x.ListAll()).Returns(registryEntries);
        _mockFolderProvider.Setup(x => x.ListAll()).Returns(folderEntries);

        // Act
        var result = _manager.ListAll();

        // Assert
        result.Should().HaveCount(3);
        result.Should().Contain(registryEntries);
        result.Should().Contain(folderEntries);
    }

    [Fact]
    public void Exists_ShouldUseCorrectProvider()
    {
        // Arrange
        _mockRegistryProvider.Setup(x => x.Exists("TestApp", StartupScope.CurrentUser, StartupKind.Run))
            .Returns(true);

        // Act
        var result = _manager.Exists("TestApp", StartupScope.CurrentUser, StartupKind.Run);

        // Assert
        result.Should().BeTrue();
        _mockRegistryProvider.Verify(x => x.Exists("TestApp", StartupScope.CurrentUser, StartupKind.Run), Times.Once);
        _mockFolderProvider.Verify(x => x.Exists(It.IsAny<string>(), It.IsAny<StartupScope>(), It.IsAny<StartupKind>()), Times.Never);
    }

    [Fact]
    public void Add_WithValidEntry_ShouldUseCorrectProvider()
    {
        // Arrange
        var entry = new StartupEntry("TestApp", @"C:\TestApp.exe", "--arg", StartupScope.CurrentUser, StartupKind.Run);

        // Act
        _manager.Add(entry);

        // Assert
        _mockRegistryProvider.Verify(x => x.Add(entry), Times.Once);
        _mockFolderProvider.Verify(x => x.Add(It.IsAny<StartupEntry>()), Times.Never);
    }

    [Fact]
    public void Add_WithInvalidName_ShouldThrowArgumentException()
    {
        // Arrange
        var entry = new StartupEntry("", @"C:\TestApp.exe", null, StartupScope.CurrentUser, StartupKind.Run);

        // Act & Assert
        var action = () => _manager.Add(entry);
        action.Should().Throw<ArgumentException>().WithMessage("Entry name cannot be empty.*");
    }

    [Fact]
    public void Add_WithInvalidPath_ShouldThrowArgumentException()
    {
        // Arrange
        var entry = new StartupEntry("TestApp", "", null, StartupScope.CurrentUser, StartupKind.Run);

        // Act & Assert
        var action = () => _manager.Add(entry);
        action.Should().Throw<ArgumentException>().WithMessage("TargetPath cannot be empty.*");
    }

    [Fact]
    public void Add_WithInvalidEntryName_ShouldThrowArgumentException()
    {
        // Arrange
        var entry = new StartupEntry("Test<>App", @"C:\TestApp.exe", null, StartupScope.CurrentUser, StartupKind.Run);

        // Act & Assert
        var action = () => _manager.Add(entry);
        action.Should().Throw<ArgumentException>().WithMessage("Entry name contains invalid characters*");
    }

    [Fact]
    public void Add_WithTooLongArguments_ShouldThrowArgumentException()
    {
        // Arrange
        var longArgs = new string('a', 1025);
        var entry = new StartupEntry("TestApp", @"C:\TestApp.exe", longArgs, StartupScope.CurrentUser, StartupKind.Run);

        // Act & Assert
        var action = () => _manager.Add(entry);
        action.Should().Throw<ArgumentException>().WithMessage("Arguments string is too long*");
    }

    [Fact]
    public void Remove_ShouldUseCorrectProvider()
    {
        // Arrange & Act
        _manager.Remove("TestApp", StartupScope.CurrentUser, StartupKind.Run);

        // Assert
        _mockRegistryProvider.Verify(x => x.Remove("TestApp", StartupScope.CurrentUser, StartupKind.Run), Times.Once);
        _mockFolderProvider.Verify(x => x.Remove(It.IsAny<string>(), It.IsAny<StartupScope>(), It.IsAny<StartupKind>()), Times.Never);
    }

    [Fact]
    public void Add_WithUnsupportedKind_ShouldThrowNotSupportedException()
    {
        // Arrange
        var manager = new StartupManager(new IStartupProvider[0]);
        var entry = new StartupEntry("TestApp", @"C:\TestApp.exe", null, StartupScope.CurrentUser, StartupKind.Run);

        // Act & Assert
        var action = () => manager.Add(entry);
        action.Should().Throw<NotSupportedException>().WithMessage("No provider supports kind*");
    }

    [Theory]
    [InlineData(@"C:\Program Files\Test.exe")]
    [InlineData(@"C:\Test.exe")]
    [InlineData(@"Test.exe")]
    public void Add_WithValidPaths_ShouldSucceed(string path)
    {
        // Arrange
        var entry = new StartupEntry("TestApp", path, null, StartupScope.CurrentUser, StartupKind.Run);

        // Act & Assert
        var action = () => _manager.Add(entry);
        action.Should().NotThrow();
    }

    [Theory]
    [InlineData("CON")]
    [InlineData("PRN")]
    [InlineData("AUX")]
    [InlineData("COM1")]
    [InlineData("LPT1")]
    public void Add_WithReservedNames_ShouldThrowArgumentException(string reservedPath)
    {
        // Arrange
        var entry = new StartupEntry("TestApp", reservedPath, null, StartupScope.CurrentUser, StartupKind.Run);

        // Act & Assert
        var action = () => _manager.Add(entry);
        action.Should().Throw<ArgumentException>().WithMessage("TargetPath is invalid*");
    }
}