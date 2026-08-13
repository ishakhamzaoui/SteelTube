using System;
using SteelTube.Application.Common;

namespace SteelTube.Infrastructure.Common
{
    /// <inheritdoc cref="IClock"/>
    public sealed class SystemClock : IClock
    {
        public DateTime UtcNow => DateTime.UtcNow;
    }
}