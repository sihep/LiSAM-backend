namespace LiSAM.Core.ML
{
    /// <summary>
    /// Model input: N cropped points, each with F features (x, y, z, intensity
    /// by default). Row-major: Features[i * FeatureDim + f].
    /// </summary>
    public sealed class ModelInput
    {
        public float[] Features;
        public int PointCount;
        public int FeatureDim;
    }

    /// <summary>
    /// Model output: per-point class logits, shape [PointCount, ClassCount].
    /// </summary>
    public sealed class ModelOutput
    {
        public float[] Logits;
        public int PointCount;
        public int ClassCount;

        public int[] ArgmaxLabels()
        {
            var labels = new int[PointCount];
            for (int i = 0; i < PointCount; i++)
            {
                int best = 0;
                float bestVal = Logits[i * ClassCount];
                for (int c = 1; c < ClassCount; c++)
                {
                    float v = Logits[i * ClassCount + c];
                    if (v > bestVal) { bestVal = v; best = c; }
                }
                labels[i] = best;
            }
            return labels;
        }
    }
}
