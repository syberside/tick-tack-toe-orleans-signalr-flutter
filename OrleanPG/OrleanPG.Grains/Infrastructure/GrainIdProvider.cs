using Orleans;
using System;

namespace OrleanPG.Grains.Infrastructure
{
    public class GrainIdProvider : IGrainIdProvider
    {
        public Guid GetGrainId(IGrainWithGuidKey grain) => grain.GetPrimaryKey();
    }
}
