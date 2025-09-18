using FluentAssertions;
using WindowsAutostartApi.Abstractions;
using WindowsAutostartApi.Core;

namespace WindowsAutostartApi.Tests.Integration;

/// <summary>
/// Integration tests that test the actual providers against the real system.
/// These tests require Windows and appropriate permissions.
/// </summary>
[Collection("Integration")]
public class StartupManagerIntegrationTests : IDisposable
{
    private readonly StartupManager _manager;
    private readonly List<(string Name, StartupScope Scope, StartupKind Kind)> _entriesToCleanup;

    public StartupManagerIntegrationTests()
    {
        _manager = new StartupManager();
        _entriesToCleanup = new List<(string, StartupScope, StartupKind)>();
    }

    [Fact]
    [Trait("Category", "Integration")]
    public void ListAll_ShouldReturnSystemStartupEntries()
    {
        // Act
        var entries = _manager.ListAll();

        // Assert
        entries.Should().NotBeNull();
        // We can't assert specific entries since they vary by system,
        // but we can verify the structure is correct
        entries.Should().AllSatisfy(entry =>
        {
            entry.Name.Should().NotBeNullOrWhiteSpace();
            entry.TargetPath.Should().NotBeNullOrWhiteSpace();
            entry.Scope.Should().BeOneOf(StartupScope.CurrentUser, StartupScope.AllUsers);
            entry.Kind.Should().BeOneOf(StartupKind.Run, StartupKind.RunOnce, StartupKind.StartupFolder);
        });
    }

    [Fact]
    [Trait("Category", "Integration")]
    public void Add_Remove_Registry_ShouldWorkCorrectly()
    {
        // Arrange
        var testEntry = new StartupEntry(
            "WindowsAutostartApiTest",
            @"C:\Windows\System32\notepad.exe",
            "--test-arg",
            StartupScope.CurrentUser,
            StartupKind.Run);

        _entriesToCleanup.Add((testEntry.Name, testEntry.Scope, testEntry.Kind));

        // Act & Assert - Add
        var addAction = () => _manager.Add(testEntry);
        addAction.Should().NotThrow();

        // Verify entry exists
        var exists = _manager.Exists(testEntry.Name, testEntry.Scope, testEntry.Kind);
        exists.Should().BeTrue();

        // Verify entry appears in list
        var entries = _manager.ListAll();
        entries.Should().Contain(e => e.Name == testEntry.Name &&
                                      e.TargetPath == testEntry.TargetPath &&
                                      e.Arguments == testEntry.Arguments &&
                                      e.Scope == testEntry.Scope &&
                                      e.Kind == testEntry.Kind);

        // Act & Assert - Remove
        var removeAction = () => _manager.Remove(testEntry.Name, testEntry.Scope, testEntry.Kind);
        removeAction.Should().NotThrow();

        // Verify entry no longer exists
        var existsAfterRemove = _manager.Exists(testEntry.Name, testEntry.Scope, testEntry.Kind);
        existsAfterRemove.Should().BeFalse();
    }

    [Fact]
    [Trait("Category", "Integration")]
    public void Add_Remove_StartupFolder_ShouldWorkCorrectly()
    {
        // Arrange
        var testEntry = new StartupEntry(
            "WindowsAutostartApiTestFolder",
            @"C:\Windows\System32\notepad.exe",
            "--folder-test-arg",
            StartupScope.CurrentUser,
            StartupKind.StartupFolder);

        _entriesToCleanup.Add((testEntry.Name, testEntry.Scope, testEntry.Kind));

        // Act & Assert - Add
        var addAction = () => _manager.Add(testEntry);
        addAction.Should().NotThrow();

        // Verify entry exists
        var exists = _manager.Exists(testEntry.Name, testEntry.Scope, testEntry.Kind);
        exists.Should().BeTrue();

        // Verify entry appears in list
        var entries = _manager.ListAll();
        entries.Should().Contain(e => e.Name == testEntry.Name &&
                                      e.TargetPath == testEntry.TargetPath &&
                                      e.Arguments == testEntry.Arguments &&
                                      e.Scope == testEntry.Scope &&
                                      e.Kind == testEntry.Kind);

        // Act & Assert - Remove
        var removeAction = () => _manager.Remove(testEntry.Name, testEntry.Scope, testEntry.Kind);
        removeAction.Should().NotThrow();

        // Verify entry no longer exists
        var existsAfterRemove = _manager.Exists(testEntry.Name, testEntry.Scope, testEntry.Kind);
        existsAfterRemove.Should().BeFalse();
    }

    [Fact]
    [Trait("Category", "Integration")]
    public void Add_DuplicateEntry_ShouldUpdateExisting()
    {
        // Arrange
        var testEntry1 = new StartupEntry(
            "WindowsAutostartApiTestUpdate",
            @"C:\Windows\System32\notepad.exe",
            "--original-arg",
            StartupScope.CurrentUser,
            StartupKind.Run);

        var testEntry2 = new StartupEntry(
            "WindowsAutostartApiTestUpdate",
            @"C:\Windows\System32\calc.exe",
            "--updated-arg",
            StartupScope.CurrentUser,
            StartupKind.Run);

        _entriesToCleanup.Add((testEntry1.Name, testEntry1.Scope, testEntry1.Kind));

        // Act
        _manager.Add(testEntry1);
        _manager.Add(testEntry2); // Should update the existing entry

        // Assert
        var entries = _manager.ListAll();
        var matchingEntries = entries.Where(e => e.Name == testEntry1.Name).ToList();

        matchingEntries.Should().HaveCount(1);
        var entry = matchingEntries.First();
        entry.TargetPath.Should().Contain("calc.exe"); // Should have the updated target
        entry.Arguments.Should().Be("--updated-arg"); // Should have the updated arguments
    }

    [Fact]
    [Trait("Category", "Integration")]
    public void Remove_NonExistentEntry_ShouldNotThrow()
    {
        // Act & Assert
        var action = () => _manager.Remove("NonExistentTestEntry", StartupScope.CurrentUser, StartupKind.Run);
        action.Should().NotThrow();
    }

    [SkippableFact]
    [Trait("Category", "Integration")]
    public void Add_AllUsersScope_ShouldRequireElevation()
    {
        // This test should be skipped if not running as administrator
        Skip.IfNot(IsAdministrator(), "This test requires administrator privileges");

        // Arrange
        var testEntry = new StartupEntry(
            "WindowsAutostartApiTestAllUsers",
            @"C:\Windows\System32\notepad.exe",
            "--admin-test",
            StartupScope.AllUsers,
            StartupKind.Run);

        _entriesToCleanup.Add((testEntry.Name, testEntry.Scope, testEntry.Kind));

        // Act & Assert
        var action = () => _manager.Add(testEntry);
        action.Should().NotThrow();

        // Cleanup
        _manager.Remove(testEntry.Name, testEntry.Scope, testEntry.Kind);
    }

    [Fact]
    [Trait("Category", "Integration")]
    public void Add_AllUsersScope_ShouldBehaveDependingOnElevation()
    {
        // Arrange
        var testEntry = new StartupEntry(
            "WindowsAutostartApiTestAllUsersPermission",
            @"C:\Windows\System32\notepad.exe",
            "--permission-test",
            StartupScope.AllUsers,
            StartupKind.Run);

        if (IsAdministrator())
        {
            // When running as admin, operation should succeed
            _entriesToCleanup.Add((testEntry.Name, testEntry.Scope, testEntry.Kind));

            // Act & Assert
            var action = () => _manager.Add(testEntry);
            action.Should().NotThrow();

            // Verify it was added
            var exists = _manager.Exists(testEntry.Name, testEntry.Scope, testEntry.Kind);
            exists.Should().BeTrue();

            // Cleanup
            _manager.Remove(testEntry.Name, testEntry.Scope, testEntry.Kind);
        }
        else
        {
            // When running without admin privileges, should throw
            var action = () => _manager.Add(testEntry);
            action.Should().Throw<UnauthorizedAccessException>();
        }
    }

    private static bool IsAdministrator()
    {
        try
        {
            var identity = System.Security.Principal.WindowsIdentity.GetCurrent();
            var principal = new System.Security.Principal.WindowsPrincipal(identity);
            return principal.IsInRole(System.Security.Principal.WindowsBuiltInRole.Administrator);
        }
        catch
        {
            return false;
        }
    }

    public void Dispose()
    {
        // Clean up any test entries that were created
        foreach (var (name, scope, kind) in _entriesToCleanup)
        {
            try
            {
                _manager.Remove(name, scope, kind);
            }
            catch
            {
                // Ignore cleanup errors
            }
        }
    }
}