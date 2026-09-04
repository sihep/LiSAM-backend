using System;
using System.Collections.Generic;

namespace LiSAM.Core.Spatial
{
    /// <summary>
    /// Cheap aggregate statistics for a single polar-grid cell (ring, sector).
    /// Populated in one O(N) pass over the point cloud. Crucially, it keeps
    /// the original point *indices* that fell in the cell, so later stages
    /// can crop the real point cloud without re-scanning it.
    /// </summary>
    public sealed class PolarCell
    {
        public int RingIndex;
        public int SectorIndex;

        public int PointCount;

        public float MinElevation = float.PositiveInfinity;
        public float MaxElevation = float.NegativeInfinity;
        public float SumElevation;

        public float SumIntensity;
        public float SumIntensitySq;

        /// <summary>Indices into the original PointCloudData.Points / Intensities arrays.</summary>
        public List<int> PointIndices { get; } = new List<int>();

        public float MeanElevation => PointCount > 0 ? SumElevation / PointCount : 0f;

        public float ElevationRange => PointCount > 0 ? MaxElevation - MinElevation : 0f;

        public float MeanIntensity => PointCount > 0 ? SumIntensity / PointCount : 0f;

        /// <summary>Cheap proxy for "how varied is this cell" — high variance often means clutter/edges/objects.</summary>
        public float IntensityVariance
        {
            get
            {
                if (PointCount == 0) return 0f;
                float mean = MeanIntensity;
                return Math.Max(0f, SumIntensitySq / PointCount - mean * mean);
            }
        }

        public void Add(int pointIndex, float elevation, float intensity)
        {
            PointCount++;
            SumElevation += elevation;
            if (elevation < MinElevation) MinElevation = elevation;
            if (elevation > MaxElevation) MaxElevation = elevation;

            SumIntensity += intensity;
            SumIntensitySq += intensity * intensity;

            PointIndices.Add(pointIndex);
        }
    }
}
