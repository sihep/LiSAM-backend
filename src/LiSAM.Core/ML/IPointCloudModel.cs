namespace LiSAM.Core.ML
{
    /// <summary>
    /// Anything that turns a cropped set of original points into per-point
    /// class logits. TorchSharp models implement this; RandomPointCloudModel
    /// does too, so the pipeline can be wired up and tested before a real
    /// model is trained.
    /// </summary>
    public interface IPointCloudModel
    {
        int FeatureDim { get; }
        int ClassCount { get; }

        ModelOutput Predict(ModelInput input);
    }
}
