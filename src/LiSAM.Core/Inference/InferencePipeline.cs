using System.Collections.Generic;
using LiSAM.Core.Data;
using LiSAM.Core.ML;
using LiSAM.Core.Spatial;

namespace LiSAM.Core.Inference
{
    /// <summary>
    /// Orchestrates the full flow:
    ///   PointCloudData -> PolarGrid -> ROI proposals -> crop original points
    ///   -> TorchSharp model -> merge back into a full-cloud label array.
    ///
    /// Deliberately knows nothing about *how* ROIs are scored or *how* the
    /// model is built — swap IRoiSelector or IPointCloudModel independently
    /// (e.g. HeuristicRoiSelector -> a learned selector, or
    /// RandomPointCloudModel -> a trained PointNetLite).
    /// </summary>
    public sealed class InferencePipeline
    {
        private readonly PolarGridConfig _gridConfig;
        private readonly IRoiSelector _roiSelector;
        private readonly IPointCloudModel _model;
        private readonly RoiCropper _cropper = new RoiCropper();

        /// <summary>Class index for points not covered by any ROI. 0 == LidarSemanticLabel.Unknown.</summary>
        public int DefaultLabel { get; set; } = (int)LidarSemanticLabel.Unknown;

        public InferencePipeline(PolarGridConfig gridConfig, IRoiSelector roiSelector, IPointCloudModel model)
        {
            _gridConfig = gridConfig;
            _roiSelector = roiSelector;
            _model = model;
        }

        public InferenceResult Run(PointCloudData cloud)
        {
            int total = cloud.Points.Length;
            var labels = new int[total];
            for (int i = 0; i < total; i++) labels[i] = DefaultLabel;

            // 1. Cheap spatial representation.
            var grid = PolarGrid.Build(cloud, _gridConfig);

            // 2. Candidate ROIs from the coarse representation.
            List<RoiRegion> rois = _roiSelector.SelectRois(grid);

            int processed = 0;

            // 3-6. Crop -> infer -> scatter back, per ROI.
            foreach (var roi in rois)
            {
                var (input, indexMap) = _cropper.Crop(cloud, roi);
                if (input.PointCount == 0) continue;

                ModelOutput output = _model.Predict(input);
                int[] predicted = output.ArgmaxLabels();

                for (int i = 0; i < indexMap.Length; i++)
                    labels[indexMap[i]] = predicted[i];

                processed += input.PointCount;
            }

            return new InferenceResult
            {
                Labels = labels,
                PointsProcessed = processed,
                PointsTotal = total,
                RoiCount = rois.Count
            };
        }
    }
}
