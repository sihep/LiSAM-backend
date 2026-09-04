using System;
using System.Collections.Generic;
using System.Linq;

namespace LiSAM.Core.Spatial
{
    /// <summary>
    /// First-pass ROI selector. Scores each occupied cell from elevation
    /// range, intensity variance and point density, keeps cells above a
    /// threshold, then flood-fills adjacent surviving cells into connected
    /// regions (with angular wraparound, since sectors form a ring).
    ///
    /// This is intentionally simple — it exists so cropping, inference and
    /// merging can be built and tested end-to-end before a learned selector
    /// replaces it behind the same IRoiSelector interface.
    /// </summary>
    public sealed class HeuristicRoiSelector : IRoiSelector
    {
        public float ScoreThreshold { get; set; } = 0.3f;
        public int MinPointsPerCell { get; set; } = 3;

        public float WeightElevationRange { get; set; } = 1.0f;
        public float WeightIntensityVariance { get; set; } = 0.5f;
        public float WeightDensity { get; set; } = 0.2f;

        public List<RoiRegion> SelectRois(PolarGrid grid)
        {
            int rings = grid.Config.RingCount;
            int sectors = grid.Config.SectorCount;

            var scores = new float[rings, sectors];
            var keep = new bool[rings, sectors];

            // Pass 1: find normalization maxima among cells with enough points.
            float maxRange = 0f, maxVar = 0f, maxDensity = 0f;
            for (int r = 0; r < rings; r++)
                for (int s = 0; s < sectors; s++)
                {
                    var c = grid.Cells[r, s];
                    if (c.PointCount < MinPointsPerCell) continue;
                    maxRange = Math.Max(maxRange, c.ElevationRange);
                    maxVar = Math.Max(maxVar, c.IntensityVariance);
                    maxDensity = Math.Max(maxDensity, c.PointCount);
                }

            // Pass 2: score and threshold.
            for (int r = 0; r < rings; r++)
                for (int s = 0; s < sectors; s++)
                {
                    var c = grid.Cells[r, s];
                    if (c.PointCount < MinPointsPerCell) continue;

                    float nRange = maxRange > 0 ? c.ElevationRange / maxRange : 0f;
                    float nVar = maxVar > 0 ? c.IntensityVariance / maxVar : 0f;
                    float nDensity = maxDensity > 0 ? c.PointCount / maxDensity : 0f;

                    float score = WeightElevationRange * nRange
                                + WeightIntensityVariance * nVar
                                + WeightDensity * nDensity;

                    scores[r, s] = score;
                    keep[r, s] = score >= ScoreThreshold;
                }

            // Pass 3: connected-component merge of surviving cells.
            var visited = new bool[rings, sectors];
            var regions = new List<RoiRegion>();

            for (int r = 0; r < rings; r++)
                for (int s = 0; s < sectors; s++)
                {
                    if (!keep[r, s] || visited[r, s]) continue;

                    var region = new RoiRegion();
                    var stack = new Stack<(int r, int s)>();
                    stack.Push((r, s));
                    visited[r, s] = true;

                    float scoreSum = 0f;
                    int cellCount = 0;

                    while (stack.Count > 0)
                    {
                        var (cr, cs) = stack.Pop();
                        region.Cells.Add(grid.Cells[cr, cs]);
                        scoreSum += scores[cr, cs];
                        cellCount++;

                        foreach (var (nr, ns) in Neighbors(cr, cs, rings, sectors))
                        {
                            if (!visited[nr, ns] && keep[nr, ns])
                            {
                                visited[nr, ns] = true;
                                stack.Push((nr, ns));
                            }
                        }
                    }

                    region.Score = cellCount > 0 ? scoreSum / cellCount : 0f;
                    regions.Add(region);
                }

            return regions.OrderByDescending(r => r.Score).ToList();
        }

        private static IEnumerable<(int, int)> Neighbors(int r, int s, int rings, int sectors)
        {
            if (r + 1 < rings) yield return (r + 1, s);
            if (r - 1 >= 0) yield return (r - 1, s);
            yield return (r, (s + 1) % sectors);              // angular wraparound
            yield return (r, (s - 1 + sectors) % sectors);
        }
    }
}
