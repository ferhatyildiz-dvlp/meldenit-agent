using MeldenIT.Agent.Core.Models;

namespace MeldenIT.Agent.Core.Services;

public interface IInventoryCollector
{
    Task<InventoryData> CollectFullInventoryAsync();
    Task<InventoryData> CollectDeltaInventoryAsync(DateTime lastSync);
    Task<DeviceIdentity> GetDeviceIdentityAsync();
}
