using System;

namespace LiSAM.Core.ML
{
    /// <summary>
    /// Untrained stand-in that returns random per-point logits. Useful for
    /// wiring up and testing Spatial -> ROI -> Inference end-to-end before
    /// PointNetLite has been trained on real data.
    /// </summary>
    public sealed class RandomPointCloudModel : IPointCloudModel
    {
        public int FeatureDim { get; }
        public int ClassCount { get; }

        private readonly Random _rng;

        public RandomPointCloudModel(int featureDim = 4, int? classCount = null, int? seed = null)
        {
            FeatureDim = featureDim;
            ClassCount = classCount ?? PointNetLite.DefaultClassCount;
            _rng = seed.HasValue ? new Random(seed.Value) : new Random();
        }

        public ModelOutput Predict(ModelInput input)
        {
            var logits = new float[input.PointCount * ClassCount];
            for (int i = 0; i < logits.Length; i++)
                logits[i] = (float)(_rng.NextDouble() * 2 - 1);

            return new ModelOutput
            {
                Logits = logits,
                PointCount = input.PointCount,
                ClassCount = ClassCount
            };
        }
    }
}
