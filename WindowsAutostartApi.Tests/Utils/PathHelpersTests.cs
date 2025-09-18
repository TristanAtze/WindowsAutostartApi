using FluentAssertions;
using WindowsAutostartApi.Utils;

namespace WindowsAutostartApi.Tests.Utils;

public class PathHelpersTests
{
    [Theory]
    [InlineData(@"C:\Program Files\Test.exe", @"""C:\Program Files\Test.exe""")]
    [InlineData(@"C:\Test.exe", @"C:\Test.exe")]
    [InlineData(@"""C:\Program Files\Test.exe""", @"""C:\Program Files\Test.exe""")]
    [InlineData(@"C:\Program&Files\Test.exe", @"""C:\Program&Files\Test.exe""")]
    [InlineData(@"C:\Program^Files\Test.exe", @"""C:\Program^Files\Test.exe""")]
    public void QuoteIfNeeded_ShouldQuoteCorrectly(string input, string expected)
    {
        // Act
        var result = PathHelpers.QuoteIfNeeded(input);

        // Assert
        result.Should().Be(expected);
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData(null)]
    public void QuoteIfNeeded_WithNullOrEmpty_ShouldReturnInput(string? input)
    {
        // Act
        var result = PathHelpers.QuoteIfNeeded(input!);

        // Assert
        result.Should().Be(input);
    }

    [Theory]
    [InlineData(@"C:\Program Files\Test.exe")]
    [InlineData(@"C:\Test.exe")]
    [InlineData(@"Test.exe")]
    [InlineData(@"D:\Very Long Path\With Spaces\Application.exe")]
    public void IsValidPath_WithValidPaths_ShouldReturnTrue(string path)
    {
        // Act
        var result = PathHelpers.IsValidPath(path);

        // Assert
        result.Should().BeTrue();
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData(null)]
    public void IsValidPath_WithNullOrEmpty_ShouldReturnFalse(string? path)
    {
        // Act
        var result = PathHelpers.IsValidPath(path!);

        // Assert
        result.Should().BeFalse();
    }

    [Theory]
    [InlineData(@"C:\Program Files\Test|exe")]
    public void IsValidPath_WithInvalidCharacters_ShouldReturnFalse(string path)
    {
        // Act
        var result = PathHelpers.IsValidPath(path);

        // Assert
        result.Should().BeFalse();
    }

    [Theory]
    [InlineData("CON")]
    [InlineData("PRN")]
    [InlineData("AUX")]
    [InlineData("NUL")]
    [InlineData("COM1")]
    [InlineData("COM9")]
    [InlineData("LPT1")]
    [InlineData("LPT9")]
    public void IsValidPath_WithReservedNames_ShouldReturnFalse(string reservedName)
    {
        // Act
        var result = PathHelpers.IsValidPath(reservedName);

        // Assert
        result.Should().BeFalse();
    }

    [Fact]
    public void IsValidPath_WithTooLongPath_ShouldReturnFalse()
    {
        // Arrange
        var longPath = @"C:\" + new string('a', 300) + ".exe";

        // Act
        var result = PathHelpers.IsValidPath(longPath);

        // Assert
        result.Should().BeFalse();
    }

    [Theory]
    [InlineData(@"""C:\Program Files\Test.exe""")]
    [InlineData(@"""C:\Test.exe""")]
    public void IsValidPath_WithQuotedPaths_ShouldReturnTrue(string quotedPath)
    {
        // Act
        var result = PathHelpers.IsValidPath(quotedPath);

        // Assert
        result.Should().BeTrue();
    }

    [Theory]
    [InlineData("TestApp")]
    [InlineData("My Cool App")]
    [InlineData("App123")]
    [InlineData("App_With_Underscores")]
    [InlineData("App-With-Dashes")]
    public void IsValidEntryName_WithValidNames_ShouldReturnTrue(string name)
    {
        // Act
        var result = PathHelpers.IsValidEntryName(name);

        // Assert
        result.Should().BeTrue();
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData(null)]
    public void IsValidEntryName_WithNullOrEmpty_ShouldReturnFalse(string? name)
    {
        // Act
        var result = PathHelpers.IsValidEntryName(name!);

        // Assert
        result.Should().BeFalse();
    }

    [Theory]
    [InlineData(@"App\Name")]
    [InlineData("App/Name")]
    [InlineData("App:Name")]
    [InlineData("App*Name")]
    [InlineData("App?Name")]
    [InlineData(@"App""Name")]
    [InlineData("App<Name")]
    [InlineData("App>Name")]
    [InlineData("App|Name")]
    public void IsValidEntryName_WithInvalidCharacters_ShouldReturnFalse(string name)
    {
        // Act
        var result = PathHelpers.IsValidEntryName(name);

        // Assert
        result.Should().BeFalse();
    }

    [Fact]
    public void IsValidEntryName_WithTooLongName_ShouldReturnFalse()
    {
        // Arrange
        var longName = new string('a', 256);

        // Act
        var result = PathHelpers.IsValidEntryName(longName);

        // Assert
        result.Should().BeFalse();
    }

    [Fact]
    public void LooksLikeFilePath_ShouldCallIsValidPath()
    {
        // Arrange
        var validPath = @"C:\Test.exe";

        // Act
        var result = PathHelpers.LooksLikeFilePath(validPath);

        // Assert
        result.Should().BeTrue();
    }

    [Theory]
    [InlineData(@"C:\")]
    [InlineData(@"C:\Folder\")]
    [InlineData(@"\\Server\Share")]
    [InlineData(@"\\?\C:\VeryLongPath")]
    public void IsValidPath_WithDifferentPathFormats_ShouldReturnTrue(string path)
    {
        // Act
        var result = PathHelpers.IsValidPath(path);

        // Assert
        result.Should().BeTrue();
    }

    [Theory]
    [InlineData(@"C:\Program Files\Test|exe")]
    public void IsValidPath_WithMalformedPaths_ShouldReturnFalse(string path)
    {
        // Act
        var result = PathHelpers.IsValidPath(path);

        // Assert
        result.Should().BeFalse();
    }
}