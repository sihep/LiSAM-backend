using LiSAM.Core.Data;

namespace LiSAM.Core.Inference
{
    /// <summary>
    /// Global result after merging all ROI predictions back onto the full
    /// cloud. Points never covered by any ROI keep DefaultLabel
    /// (LidarSemanticLabel.Unknown by default).
    /// </summary>
    public sealed class InferenceResult
    {
        /// <summary>Raw predicted class index per point (0..ClassCount-1), length == full cloud point count.</summary>
        public int[] Labels;

        public int PointsProcessed; // how many points actually went through the network
        public int PointsTotal;
        public int RoiCount;

        public float FractionProcessed => PointsTotal > 0 ? (float)PointsProcessed / PointsTotal : 0f;

        /// <summary>
        /// Casts the raw class indices to LidarSemanticLabel (valid since
        /// PointNetLite's default ClassCount == LidarSemanticLabel's value
        /// count and enum values start at 0). InstanceIDs are left at 0 —
        /// instance grouping isn't produced by this segmentation-only model
        /// and would be a separate step (e.g. connected components per ROI).
        /// </summary>
        public LabelData ToLabelData()
        {
            var labels = new LidarSemanticLabel[Labels.Length];
            for (int i = 0; i < Labels.Length; i++)
                labels[i] = (LidarSemanticLabel)Labels[i];

            var instanceIds = new int[Labels.Length]; // all 0 (no instance segmentation yet)

            return new LabelData(labels, instanceIds);
        }
    }
}
