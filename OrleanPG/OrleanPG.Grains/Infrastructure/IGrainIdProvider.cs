using Orleans;
using System;

namespace OrleanPG.Grains.Infrastructure
{
    public interface IGrainIdProvider
    {
        Guid GetGrainId(IGrainWithGuidKey grain);
    }
}
