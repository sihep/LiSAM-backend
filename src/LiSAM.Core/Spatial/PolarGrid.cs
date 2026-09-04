using System;
using OpenTK.Mathematics;
using LiSAM.Core.Data;

namespace LiSAM.Core.Spatial
{
    /// <summary>
    /// The "cheap spatial representation" step of the pipeline: bins the raw
    /// cloud into (r, theta) cells and accumulates per-cell statistics in a
    /// single pass. Never touches a neural network — this is the part that
    /// decides where the network should even bother looking.
    /// </summary>
    public sealed class PolarGrid
    {
        public PolarGridConfig Config { get; }

        /// <summary>[ring, sector] — Cells[r, s] is always non-null, even if empty.</summary>
        public PolarCell[,] Cells { get; }

        public PolarGrid(PolarGridConfig config)
        {
            Config = config;
            Cells = new PolarCell[config.RingCount, config.SectorCount];
            for (int r = 0; r < config.RingCount; r++)
                for (int s = 0; s < config.SectorCount; s++)
                    Cells[r, s] = new PolarCell { RingIndex = r, SectorIndex = s };
        }

        public static PolarGrid Build(PointCloudData cloud, PolarGridConfig config)
        {
            var grid = new PolarGrid(config);

            for (int i = 0; i < cloud.Points.Length; i++)
            {
                Vector3 p = cloud.Points[i];

                float range = MathF.Sqrt(p.X * p.X + p.Y * p.Y);
                if (range >= config.MaxRange || range <= 0f) continue;

                float theta = MathF.Atan2(p.Y, p.X);
                if (theta < 0f) theta += 2f * MathF.PI;

                int ring = (int)(range / config.RingWidth);
                int sector = (int)(theta / config.SectorWidth);

                ring = Math.Clamp(ring, 0, config.RingCount - 1);
                sector = Math.Clamp(sector, 0, config.SectorCount - 1);

                float intensity = (cloud.Intensities != null && i < cloud.Intensities.Length)
                    ? cloud.Intensities[i]
                    : 0f;

                grid.Cells[ring, sector].Add(i, p.Z, intensity);
            }

            return grid;
        }
    }
}
