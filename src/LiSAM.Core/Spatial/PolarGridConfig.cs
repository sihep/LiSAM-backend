using System;

namespace LiSAM.Core.Spatial
{
    /// <summary>
    /// Parameters for the polar spatial grid. Tune RingCount/SectorCount to
    /// trade off "how cheap is the front end" against "how fine-grained can
    /// an ROI be".
    /// </summary>
    public sealed class PolarGridConfig
    {
        /// <summary>Radial cutoff in meters; points beyond this are ignored by the grid.</summary>
        public float MaxRange { get; set; } = 80f;

        /// <summary>Number of radial bins from 0 to MaxRange.</summary>
        public int RingCount { get; set; } = 40;

        /// <summary>Number of angular bins covering the full 360 degrees.</summary>
        public int SectorCount { get; set; } = 360;

        public float RingWidth => MaxRange / RingCount;

        public float SectorWidth => 2f * MathF.PI / SectorCount;
    }
}
