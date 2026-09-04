using System.Linq;
using LiSAM.Core.Data;
using LiSAM.Core.ML;
using LiSAM.Core.Spatial;

namespace LiSAM.Core.Inference
{
    /// <summary>
    /// Crops the *original* points that fall inside an ROI. The polar grid
    /// already recorded per-cell point indices during Build, so this is a
    /// gather over cloud.Points/Intensities — no re-scanning of the cloud.
    /// </summary>
    public sealed class RoiCropper
    {
        /// <summary>Per-point feature layout produced by Crop(): x, y, z, intensity.</summary>
        public const int FeatureDim = 4;

        /// <summary>
        /// Builds a model-ready input for one ROI, plus the index map needed
        /// to scatter predictions back onto the full cloud.
        /// </summary>
        public (ModelInput input, int[] indexMap) Crop(PointCloudData cloud, RoiRegion roi)
        {
            int[] indices = roi.PointIndices().Distinct().ToArray();
            int n = indices.Length;

            var features = new float[n * FeatureDim];
            for (int i = 0; i < n; i++)
            {
                int idx = indices[i];
                var p = cloud.Points[idx];
                float intensity = (cloud.Intensities != null && idx < cloud.Intensities.Length)
                    ? cloud.Intensities[idx]
                    : 0f;

                features[i * FeatureDim + 0] = p.X;
                features[i * FeatureDim + 1] = p.Y;
                features[i * FeatureDim + 2] = p.Z;
                features[i * FeatureDim + 3] = intensity;
            }

            var input = new ModelInput
            {
                Features = features,
                PointCount = n,
                FeatureDim = FeatureDim
            };

            return (input, indices);
        }
    }
}
