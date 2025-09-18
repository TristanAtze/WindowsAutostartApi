using FluentAssertions;
using Moq;
using WindowsAutostartApi.Abstractions;
using WindowsAutostartApi.Core;

namespace WindowsAutostartApi.Tests.Core;

public class CachedStartupManagerTests : IDisposable
{
    private readonly Mock<IStartupProvider> _mockProvider;
    private readonly CachedStartupManager _manager;

    public CachedStartupManagerTests()
    {
        _mockProvider = new Mock<IStartupProvider>();
        _mockProvider.Setup(x => x.Supports(It.IsAny<StartupKind>())).Returns(true);

        _manager = new CachedStartupManager(new[] { _mockProvider.Object }, TimeSpan.FromMinutes(1));
    }

    [Fact]
    public void ListAll_FirstCall_ShouldCallProvider()
    {
        // Arrange
        var entries = new[]
        {
            new StartupEntry("App1", @"C:\App1.exe", null, StartupScope.CurrentUser, StartupKind.Run)
        };
        _mockProvider.Setup(x => x.ListAll()).Returns(entries);

        // Act
        var result = _manager.ListAll();

        // Assert
        result.Should().HaveCount(1);
        result.Should().Contain(entries);
        _mockProvider.Verify(x => x.ListAll(), Times.Once);
    }

    [Fact]
    public void ListAll_SecondCallWithinCacheExpiry_ShouldUseCachedData()
    {
        // Arrange
        var entries = new[]
        {
            new StartupEntry("App1", @"C:\App1.exe", null, StartupScope.CurrentUser, StartupKind.Run)
        };
        _mockProvider.Setup(x => x.ListAll()).Returns(entries);

        // Act
        var result1 = _manager.ListAll();
        var result2 = _manager.ListAll();

        // Assert
        result1.Should().HaveCount(1);
        result2.Should().HaveCount(1);
        result1.Should().BeEquivalentTo(result2);
        _mockProvider.Verify(x => x.ListAll(), Times.Once); // Should only be called once due to caching
    }

    [Fact]
    public void RefreshCache_ShouldForceReloadFromProviders()
    {
        // Arrange
        var entries1 = new[]
        {
            new StartupEntry("App1", @"C:\App1.exe", null, StartupScope.CurrentUser, StartupKind.Run)
        };
        var entries2 = new[]
        {
            new StartupEntry("App2", @"C:\App2.exe", null, StartupScope.CurrentUser, StartupKind.Run)
        };

        _mockProvider.SetupSequence(x => x.ListAll())
            .Returns(entries1)
            .Returns(entries2);

        // Act
        var result1 = _manager.ListAll();
        var result2 = _manager.RefreshCache();

        // Assert
        result1.Should().Contain(entries1);
        result2.Should().Contain(entries2);
        _mockProvider.Verify(x => x.ListAll(), Times.Exactly(2));
    }

    [Fact]
    public void InvalidateCache_ShouldClearCachedData()
    {
        // Arrange
        var entries = new[]
        {
            new StartupEntry("App1", @"C:\App1.exe", null, StartupScope.CurrentUser, StartupKind.Run)
        };
        _mockProvider.Setup(x => x.ListAll()).Returns(entries);

        // Act
        var result1 = _manager.ListAll(); // First call populates cache
        _manager.InvalidateCache();
        var result2 = _manager.ListAll(); // Second call should reload

        // Assert
        result1.Should().HaveCount(1);
        result2.Should().HaveCount(1);
        _mockProvider.Verify(x => x.ListAll(), Times.Exactly(2)); // Should be called twice
    }

    [Fact]
    public void Add_ShouldInvalidateCache()
    {
        // Arrange
        var entry = new StartupEntry("TestApp", @"C:\TestApp.exe", null, StartupScope.CurrentUser, StartupKind.Run);
        var entries = new[] { entry };
        _mockProvider.Setup(x => x.ListAll()).Returns(entries);

        // Act
        _manager.ListAll(); // Populate cache
        _manager.Add(entry); // Should invalidate cache
        _manager.ListAll(); // Should reload from provider

        // Assert
        _mockProvider.Verify(x => x.ListAll(), Times.Exactly(2));
        _mockProvider.Verify(x => x.Add(entry), Times.Once);
    }

    [Fact]
    public void Remove_ShouldInvalidateCache()
    {
        // Arrange
        var entries = new[]
        {
            new StartupEntry("App1", @"C:\App1.exe", null, StartupScope.CurrentUser, StartupKind.Run)
        };
        _mockProvider.Setup(x => x.ListAll()).Returns(entries);

        // Act
        _manager.ListAll(); // Populate cache
        _manager.Remove("TestApp", StartupScope.CurrentUser, StartupKind.Run); // Should invalidate cache
        _manager.ListAll(); // Should reload from provider

        // Assert
        _mockProvider.Verify(x => x.ListAll(), Times.Exactly(2));
        _mockProvider.Verify(x => x.Remove("TestApp", StartupScope.CurrentUser, StartupKind.Run), Times.Once);
    }

    [Fact]
    public void Exists_ShouldNotUseCache()
    {
        // Arrange
        _mockProvider.Setup(x => x.Exists("TestApp", StartupScope.CurrentUser, StartupKind.Run)).Returns(true);

        // Act
        var result = _manager.Exists("TestApp", StartupScope.CurrentUser, StartupKind.Run);

        // Assert
        result.Should().BeTrue();
        _mockProvider.Verify(x => x.Exists("TestApp", StartupScope.CurrentUser, StartupKind.Run), Times.Once);
    }

    [Fact]
    public void ListAll_WithProviderFailure_ShouldSkipFailedProvider()
    {
        // Arrange
        var workingProvider = new Mock<IStartupProvider>();
        var failingProvider = new Mock<IStartupProvider>();

        var entries = new[]
        {
            new StartupEntry("App1", @"C:\App1.exe", null, StartupScope.CurrentUser, StartupKind.Run)
        };

        workingProvider.Setup(x => x.ListAll()).Returns(entries);
        failingProvider.Setup(x => x.ListAll()).Throws(new UnauthorizedAccessException("Access denied"));

        var manager = new CachedStartupManager(new[] { workingProvider.Object, failingProvider.Object }, TimeSpan.FromMinutes(1));

        // Act
        var result = manager.ListAll();

        // Assert
        result.Should().HaveCount(1);
        result.Should().Contain(entries);

        // Cleanup
        manager.Dispose();
    }

    [Fact]
    public void Dispose_ShouldDisposeResources()
    {
        // Act & Assert
        var action = () => _manager.Dispose();
        action.Should().NotThrow();
    }

    [Fact]
    public void ListAll_AfterDispose_ShouldThrowObjectDisposedException()
    {
        // Arrange
        _manager.Dispose();

        // Act & Assert
        var action = () => _manager.ListAll();
        action.Should().Throw<ObjectDisposedException>();
    }

    [Fact]
    public void Add_AfterDispose_ShouldThrowObjectDisposedException()
    {
        // Arrange
        _manager.Dispose();
        var entry = new StartupEntry("TestApp", @"C:\TestApp.exe", null, StartupScope.CurrentUser, StartupKind.Run);

        // Act & Assert
        var action = () => _manager.Add(entry);
        action.Should().Throw<ObjectDisposedException>();
    }

    [Fact]
    public void RefreshCache_WithExpiredCache_ShouldReloadData()
    {
        // Arrange
        var shortCacheManager = new CachedStartupManager(new[] { _mockProvider.Object }, TimeSpan.FromMilliseconds(1));
        var entries = new[]
        {
            new StartupEntry("App1", @"C:\App1.exe", null, StartupScope.CurrentUser, StartupKind.Run)
        };
        _mockProvider.Setup(x => x.ListAll()).Returns(entries);

        // Act
        shortCacheManager.ListAll(); // First call
        Thread.Sleep(10); // Wait for cache to expire
        shortCacheManager.ListAll(); // Second call should reload

        // Assert
        _mockProvider.Verify(x => x.ListAll(), Times.Exactly(2));

        // Cleanup
        shortCacheManager.Dispose();
    }

    public void Dispose()
    {
        _manager?.Dispose();
    }
}