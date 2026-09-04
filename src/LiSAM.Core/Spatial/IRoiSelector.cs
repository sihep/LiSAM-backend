using System.Collections.Generic;

namespace LiSAM.Core.Spatial
{
    /// <summary>
    /// Turns a coarse spatial representation into a set of candidate ROIs.
    /// The initial implementation (HeuristicRoiSelector) is purely
    /// geometric/statistical. A learned selector can implement this same
    /// interface later without changing InferencePipeline at all.
    /// </summary>
    public interface IRoiSelector
    {
        List<RoiRegion> SelectRois(PolarGrid grid);
    }
}
