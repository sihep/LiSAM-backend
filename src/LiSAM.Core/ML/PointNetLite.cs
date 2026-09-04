using System;
using LiSAM.Core.Data;
using TorchSharp;
using TorchSharp.Modules;
using static TorchSharp.torch;
using static TorchSharp.torch.nn;

namespace LiSAM.Core.ML
{
    /// <summary>
    /// Minimal PointNet-style segmentation network:
    ///   per-point shared MLP -> global max-pool -> concat global feature
    ///   back onto every point -> per-point MLP -> per-class logits.
    ///
    /// Runs only on cropped ROI points (not the whole cloud), so it can
    /// afford to be small. ClassCount defaults to the number of values in
    /// LidarSemanticLabel (24: Unknown=0 .. OtherObject=23), so
    /// ArgmaxLabels() output casts directly to that enum.
    /// </summary>
    public sealed class PointNetLite : Module<Tensor, Tensor>, IPointCloudModel
    {
        public static readonly int DefaultClassCount = Enum.GetValues(typeof(LidarSemanticLabel)).Length;

        public int FeatureDim { get; }
        public int ClassCount { get; }

        private readonly Sequential _shared1; // per-point local features
        private readonly Sequential _shared2; // per-point features after global concat
        private readonly Linear _classifier;

        /// <param name="featureDim">Per-point input feature count (4 = x, y, z, intensity by default — see RoiCropper).</param>
        /// <param name="classCount">Defaults to LidarSemanticLabel's value count.</param>
        public PointNetLite(int featureDim = 4, int? classCount = null, int hiddenDim = 64)
            : base(nameof(PointNetLite))
        {
            FeatureDim = featureDim;
            ClassCount = classCount ?? DefaultClassCount;

            _shared1 = Sequential(
                Linear(featureDim, hiddenDim),
                ReLU(),
                Linear(hiddenDim, hiddenDim),
                ReLU());

            _shared2 = Sequential(
                Linear(hiddenDim * 2, hiddenDim),
                ReLU(),
                Linear(hiddenDim, hiddenDim),
                ReLU());

            _classifier = Linear(hiddenDim, ClassCount);

            RegisterComponents();
        }

        /// <param name="input">Tensor of shape [N, FeatureDim].</param>
        /// <returns>Tensor of shape [N, ClassCount] (logits).</returns>
        public override Tensor forward(Tensor input)
        {
            using var _ = NewDisposeScope();

            var local = _shared1.forward(input);                       // [N, H]
            var global = local.max(0, keepdim: true).values;            // [1, H]
            var globalExpanded = global.expand(local.shape[0], -1);     // [N, H]

            var combined = cat(new[] { local, globalExpanded }, dim: 1); // [N, 2H]
            var refined = _shared2.forward(combined);                   // [N, H]
            var logits = _classifier.forward(refined);                  // [N, C]

            return logits.MoveToOuterDisposeScope();
        }

        public ModelOutput Predict(ModelInput input)
        {
            if (input.FeatureDim != FeatureDim)
                throw new ArgumentException(
                    $"Model expects {FeatureDim} features per point, input has {input.FeatureDim}.");

            this.eval();
            using (no_grad())
            {
                var x = tensor(input.Features, new long[] { input.PointCount, input.FeatureDim });
                var logits = forward(x);

                return new ModelOutput
                {
                    Logits = logits.data<float>().ToArray(),
                    PointCount = input.PointCount,
                    ClassCount = ClassCount
                };
            }
        }
    }
}
