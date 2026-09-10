using MathNet.Numerics.LinearAlgebra;
using MathNet.Numerics.LinearAlgebra.Factorization;
using OpenTK.Mathematics;

namespace LiSAM.Core.Spatial.Quadtree;

public static class Mathematics
{
    public static (Vector3 centroid, Vector3 normal, float meanSquaredError) FitPlane(List<Vector3> points)
    {
        Vector3 centroid = points.Aggregate(Vector3.Zero, (current, point) => current + point) / points.Count;

        float xx = 0, yy = 0, zz = 0, xy = 0, xz = 0, yz = 0;

        foreach (Vector3 point in points)
        {
            Vector3 centeredPoint = point - centroid;

            xx += centeredPoint.X * centeredPoint.X;
            yy += centeredPoint.Y * centeredPoint.Y;
            zz += centeredPoint.Z * centeredPoint.Z;
            xy += centeredPoint.X * centeredPoint.Y;
            xz += centeredPoint.X * centeredPoint.Z;
            yz += centeredPoint.Y * centeredPoint.Z;
        }

        Matrix<float> C = Matrix<float>.Build.DenseOfArray(new[,]
        {
            { xx, xy, xz },
            { xy, yy, yz },
            { xz, yz, zz }
        });

        Evd<float> evd = C.Evd();

        int smallest = 0;
        for (int i = 1; i < 3; i++)
            if (evd.EigenValues[i].Real < evd.EigenValues[smallest].Real)
                smallest = i;
        Vector<float> smallestColumn = evd.EigenVectors.Column(smallest);

        return (
            centroid,
            normal: new Vector3(smallestColumn[0], smallestColumn[1], smallestColumn[2]),
            meanSquaredError: (float)Math.Max(0, evd.EigenValues[smallest].Real) / points.Count
        );
    }
}