using System.Collections.Generic;

namespace LiSAM.Core.Spatial
{
    /// <summary>
    /// A candidate region of interest: a connected set of grid cells plus a
    /// score. Point indices are derived lazily from the cells it contains,
    /// since the cells already hold that information.
    /// </summary>
    public sealed class RoiRegion
    {
        public float Score;
        public List<PolarCell> Cells { get; } = new List<PolarCell>();

        public IEnumerable<int> PointIndices()
        {
            foreach (var cell in Cells)
                foreach (var idx in cell.PointIndices)
                    yield return idx;
        }

        public int PointCount
        {
            get
            {
                int total = 0;
                foreach (var cell in Cells) total += cell.PointCount;
                return total;
            }
        }
    }
}
