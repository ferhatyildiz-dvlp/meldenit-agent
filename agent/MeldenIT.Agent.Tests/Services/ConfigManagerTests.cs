using MeldenIT.Agent.Core.Models;
using MeldenIT.Agent.Core.Services;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;

namespace MeldenIT.Agent.Tests.Services;

public class ConfigManagerTests
{
    private readonly Mock<ILogger<ConfigManager>> _mockLogger;
    private readonly ConfigManager _configManager;
    private readonly string _testConfigPath;

    public ConfigManagerTests()
    {
        _mockLogger = new Mock<ILogger<ConfigManager>>();
        _configManager = new ConfigManager(_mockLogger.Object);
        _testConfigPath = Path.Combine(Path.GetTempPath(), "MeldenIT", "Agent", "test-agent.json");
    }

    [Fact]
    public async Task LoadConfigAsync_WhenFileDoesNotExist_ShouldCreateDefaultConfig()
    {
        // Arrange
        if (File.Exists(_testConfigPath))
        {
            File.Delete(_testConfigPath);
        }

        // Act
        var result = await _configManager.LoadConfigAsync();

        // Assert
        Assert.NotNull(result);
        Assert.NotEmpty(result.ApiUrl);
        Assert.NotEmpty(result.SiteCode);
    }

    [Fact]
    public async Task SaveConfigAsync_ShouldSaveConfigToFile()
    {
        // Arrange
        var config = new AgentConfig
        {
            ApiUrl = "https://test.example.com",
            SiteCode = "TEST"
        };

        // Act
        await _configManager.SaveConfigAsync(config);

        // Assert
        Assert.True(File.Exists(_testConfigPath));
    }

    [Fact]
    public async Task GetOrCreateAgentGuidAsync_ShouldReturnGuid()
    {
        // Act
        var result = await _configManager.GetOrCreateAgentGuidAsync();

        // Assert
        Assert.NotEmpty(result);
        Assert.True(Guid.TryParse(result, out _));
    }

    [Fact]
    public async Task IsFirstRunAsync_WhenNoDeviceToken_ShouldReturnTrue()
    {
        // Arrange
        var config = new AgentConfig();
        await _configManager.SaveConfigAsync(config);

        // Act
        var result = await _configManager.IsFirstRunAsync();

        // Assert
        Assert.True(result);
    }

    [Fact]
    public async Task SaveDeviceTokenAsync_ShouldSaveToken()
    {
        // Arrange
        var token = "test-device-token";

        // Act
        await _configManager.SaveDeviceTokenAsync(token);

        // Assert
        var savedToken = await _configManager.GetDeviceTokenAsync();
        Assert.Equal(token, savedToken);
    }
}
