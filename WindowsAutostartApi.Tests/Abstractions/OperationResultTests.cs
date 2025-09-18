using FluentAssertions;
using WindowsAutostartApi.Abstractions;

namespace WindowsAutostartApi.Tests.Abstractions;

public class OperationResultTests
{
    [Fact]
    public void Success_ShouldCreateSuccessfulResult()
    {
        // Act
        var result = OperationResult.Success();

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.ErrorMessage.Should().BeNull();
        result.Exception.Should().BeNull();
    }

    [Fact]
    public void Failure_WithMessage_ShouldCreateFailedResult()
    {
        // Arrange
        var errorMessage = "Something went wrong";

        // Act
        var result = OperationResult.Failure(errorMessage);

        // Assert
        result.IsSuccess.Should().BeFalse();
        result.ErrorMessage.Should().Be(errorMessage);
        result.Exception.Should().BeNull();
    }

    [Fact]
    public void Failure_WithMessageAndException_ShouldCreateFailedResult()
    {
        // Arrange
        var errorMessage = "Something went wrong";
        var exception = new InvalidOperationException("Test exception");

        // Act
        var result = OperationResult.Failure(errorMessage, exception);

        // Assert
        result.IsSuccess.Should().BeFalse();
        result.ErrorMessage.Should().Be(errorMessage);
        result.Exception.Should().Be(exception);
    }

    [Fact]
    public void Failure_WithException_ShouldCreateFailedResultWithExceptionMessage()
    {
        // Arrange
        var exception = new InvalidOperationException("Test exception");

        // Act
        var result = OperationResult.Failure(exception);

        // Assert
        result.IsSuccess.Should().BeFalse();
        result.ErrorMessage.Should().Be("Test exception");
        result.Exception.Should().Be(exception);
    }
}

public class OperationResultTTests
{
    [Fact]
    public void Success_WithValue_ShouldCreateSuccessfulResult()
    {
        // Arrange
        var value = "test value";

        // Act
        var result = OperationResult<string>.Success(value);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Should().Be(value);
        result.ErrorMessage.Should().BeNull();
        result.Exception.Should().BeNull();
    }

    [Fact]
    public void Failure_WithMessage_ShouldCreateFailedResult()
    {
        // Arrange
        var errorMessage = "Something went wrong";

        // Act
        var result = OperationResult<string>.Failure(errorMessage);

        // Assert
        result.IsSuccess.Should().BeFalse();
        result.Value.Should().BeNull();
        result.ErrorMessage.Should().Be(errorMessage);
        result.Exception.Should().BeNull();
    }

    [Fact]
    public void Failure_WithMessageAndException_ShouldCreateFailedResult()
    {
        // Arrange
        var errorMessage = "Something went wrong";
        var exception = new InvalidOperationException("Test exception");

        // Act
        var result = OperationResult<string>.Failure(errorMessage, exception);

        // Assert
        result.IsSuccess.Should().BeFalse();
        result.Value.Should().BeNull();
        result.ErrorMessage.Should().Be(errorMessage);
        result.Exception.Should().Be(exception);
    }

    [Fact]
    public void Failure_WithException_ShouldCreateFailedResultWithExceptionMessage()
    {
        // Arrange
        var exception = new InvalidOperationException("Test exception");

        // Act
        var result = OperationResult<string>.Failure(exception);

        // Assert
        result.IsSuccess.Should().BeFalse();
        result.Value.Should().BeNull();
        result.ErrorMessage.Should().Be("Test exception");
        result.Exception.Should().Be(exception);
    }

    [Fact]
    public void ImplicitConversion_FromValue_ShouldCreateSuccessResult()
    {
        // Arrange
        var value = "test value";

        // Act
        OperationResult<string> result = value;

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Should().Be(value);
        result.ErrorMessage.Should().BeNull();
        result.Exception.Should().BeNull();
    }

    [Fact]
    public void Success_WithNullValue_ShouldCreateSuccessfulResult()
    {
        // Act
        var result = OperationResult<string?>.Success(null);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Should().BeNull();
        result.ErrorMessage.Should().BeNull();
        result.Exception.Should().BeNull();
    }

    [Fact]
    public void Success_WithComplexObject_ShouldCreateSuccessfulResult()
    {
        // Arrange
        var entries = new List<StartupEntry>
        {
            new("App1", @"C:\App1.exe", null, StartupScope.CurrentUser, StartupKind.Run)
        };

        // Act
        var result = OperationResult<IReadOnlyList<StartupEntry>>.Success(entries);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Should().BeEquivalentTo(entries);
        result.ErrorMessage.Should().BeNull();
        result.Exception.Should().BeNull();
    }
}