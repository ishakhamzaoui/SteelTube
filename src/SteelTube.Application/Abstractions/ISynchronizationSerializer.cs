using SteelTube.Application.Synchronization;

namespace SteelTube.Application.Abstractions
{
    /// <summary>
    /// Isolates JSON manipulation behind an abstraction (SAD 42: "the
    /// application should not directly manipulate JSON strings throughout
    /// the business logic"). Application handlers work with
    /// <see cref="SynchronizationPackage"/> objects; only this
    /// Infrastructure-implemented interface knows it's JSON underneath.
    /// </summary>
    public interface ISynchronizationSerializer
    {
        string Serialize(SynchronizationPackage package);

        /// <summary>
        /// Throws <see cref="Common.SynchronizationException"/> if the text
        /// isn't valid JSON, or doesn't match the expected package shape
        /// (SAD 51 -- "The synchronization file is invalid or was created
        /// by an unsupported version"). Format-version compatibility
        /// itself is checked by the calling use case, not here, since it's
        /// a business policy rather than a parsing concern.
        /// </summary>
        SynchronizationPackage Deserialize(string json);
    }
}