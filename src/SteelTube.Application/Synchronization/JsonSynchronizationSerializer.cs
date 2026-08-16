using Newtonsoft.Json;
using Newtonsoft.Json.Serialization;
using SteelTube.Application.Abstractions;
using SteelTube.Application.Common;
using SteelTube.Application.Synchronization;

namespace SteelTube.Infrastructure.Synchronization
{
    /// <inheritdoc cref="ISynchronizationSerializer"/>
    public sealed class JsonSynchronizationSerializer : ISynchronizationSerializer
    {
        private static readonly JsonSerializerSettings Settings = new JsonSerializerSettings
        {
            Formatting = Formatting.Indented,
            DateTimeZoneHandling = DateTimeZoneHandling.Utc,
            NullValueHandling = NullValueHandling.Include, // SAD 42 -- keep the format human-readable and explicit rather than omitting absent fields.
            ContractResolver = new CamelCasePropertyNamesContractResolver() // matches the field casing in the SRS 13 example (formatVersion, packageId, ...).
        };

        public string Serialize(SynchronizationPackage package) =>
            JsonConvert.SerializeObject(package, Settings);

        public SynchronizationPackage Deserialize(string json)
        {
            if (string.IsNullOrWhiteSpace(json))
                throw new SynchronizationException("The synchronization file is empty.");

            try
            {
                var package = JsonConvert.DeserializeObject<SynchronizationPackage>(json, Settings);
                if (package is null)
                    throw new SynchronizationException("The synchronization file is invalid or was created by an unsupported version.");
                return package;
            }
            catch (JsonException ex)
            {
                throw new SynchronizationException("The synchronization file is invalid or was created by an unsupported version.", ex);
            }
        }
    }
}