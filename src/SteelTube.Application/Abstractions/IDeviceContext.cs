using System.Threading;
using System.Threading.Tasks;

namespace SteelTube.Application.Abstractions
{
    /// <summary>
    /// Exposes this installation's persistent device identity and local
    /// operation sequence counter (SAD 28, SAD 29). DeviceId is generated
    /// once during installation and must never be regenerated on ordinary
    /// restarts.
    /// </summary>
    public interface IDeviceContext
    {
        System.Guid DeviceId { get; }

        /// <summary>Atomically increments and returns the next local sequence number.</summary>
        Task<long> NextSequenceNumberAsync(CancellationToken ct = default);
    }
}