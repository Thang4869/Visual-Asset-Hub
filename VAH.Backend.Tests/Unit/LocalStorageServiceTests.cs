using FluentAssertions;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Logging;
using Moq;
using System.Text;
using VAH.Backend.Services;

namespace VAH.Backend.Tests.Unit;

public class LocalStorageServiceTests
{
    private readonly Mock<IWebHostEnvironment> _envMock;
    private readonly Mock<ILogger<LocalStorageService>> _loggerMock;

    public LocalStorageServiceTests()
    {
        _envMock = new Mock<IWebHostEnvironment>();
        _loggerMock = new Mock<ILogger<LocalStorageService>>();

        // Setup mock to use a dummy directory
        var tempDir = Path.Combine(Path.GetTempPath(), "vah_tests_wwwroot");
        _envMock.Setup(e => e.WebRootPath).Returns(tempDir);
    }

    [Fact]
    public async Task UploadAsync_ValidFile_ReturnsUploadedPath()
    {
        // Arrange
        var service = new LocalStorageService(_envMock.Object, _loggerMock.Object);
        using var stream = new MemoryStream(Encoding.UTF8.GetBytes("fake file content"));
        string filename = "test.txt";

        // Act
        var result = await service.UploadAsync(stream, filename, "text/plain", CancellationToken.None);

        // Assert
        result.Should().StartWith("/uploads/");
        result.Should().EndWith(".txt");
    }

    [Fact]
    public async Task DeleteAsync_PathTraversalAttempt_ThrowsSecurityException()
    {
        // Arrange
        var service = new LocalStorageService(_envMock.Object, _loggerMock.Object);
        string traversalPath = "/uploads/../../Windows/System32/cmd.exe";

        // Act
        Func<Task> act = async () => await service.DeleteAsync(traversalPath, CancellationToken.None);

        // Assert
        await act.Should().ThrowAsync<System.Security.SecurityException>()
            .WithMessage("*Path traversal attempt detected*");
    }

    [Fact]
    public async Task UploadAsync_MagicBytesSpoof_ThrowsSecurityException()
    {
        // Arrange
        var service = new LocalStorageService(_envMock.Object, _loggerMock.Object);
        
        // Simulating an 'MZ' executable signature disguised as a PNG
        var spoofedBytes = new byte[] { 0x4D, 0x5A, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00 };
        using var stream = new MemoryStream(spoofedBytes);
        
        // Act
        Func<Task> act = async () => await service.UploadAsync(stream, "image.png", "image/png", CancellationToken.None);

        // Assert
        await act.Should().ThrowAsync<System.Security.SecurityException>()
            .WithMessage("*Executable files disguised as safe assets are prohibited*");
    }
}