using MeldenIT.Agent.Core.Models;
using MeldenIT.Agent.Core.Services;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;

namespace MeldenIT.Agent.Tests.Services;

public class InventoryCollectorTests
{
    private readonly Mock<ILogger<InventoryCollector>> _mockLogger;
    private readonly InventoryCollector _inventoryCollector;

    public InventoryCollectorTests()
    {
        _mockLogger = new Mock<ILogger<InventoryCollector>>();
        _inventoryCollector = new InventoryCollector(_mockLogger.Object);
    }

    [Fact]
    public async Task CollectFullInventoryAsync_ShouldReturnInventoryData()
    {
        // Act
        var result = await _inventoryCollector.CollectFullInventoryAsync();

        // Assert
        Assert.NotNull(result);
        Assert.NotNull(result.DeviceIdentity);
        Assert.NotNull(result.Hardware);
        Assert.NotNull(result.Software);
        Assert.NotNull(result.Network);
        Assert.NotNull(result.Bios);
        Assert.NotNull(result.Usage);
        Assert.NotNull(result.Tagging);
    }

    [Fact]
    public async Task GetDeviceIdentityAsync_ShouldReturnDeviceIdentity()
    {
        // Act
        var result = await _inventoryCollector.GetDeviceIdentityAsync();

        // Assert
        Assert.NotNull(result);
        Assert.NotEmpty(result.Hostname);
        Assert.NotEmpty(result.SerialNumber);
    }

    [Fact]
    public async Task CollectDeltaInventoryAsync_ShouldReturnInventoryData()
    {
        // Arrange
        var lastSync = DateTime.UtcNow.AddDays(-1);

        // Act
        var result = await _inventoryCollector.CollectDeltaInventoryAsync(lastSync);

        // Assert
        Assert.NotNull(result);
        Assert.NotNull(result.DeviceIdentity);
    }
}
