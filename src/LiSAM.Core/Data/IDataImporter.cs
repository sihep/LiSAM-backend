using System.Globalization;
using OpenTK.Mathematics;

namespace LiSAM.Core.Data;

public interface IDataImporter
{
    static abstract Task<PointCloudData> ImportPointCloudData(Stream stream);
    static abstract Task<PointCloudData> ImportPointCloudDataFromFile(string path);
    static abstract Task<PointCloudData> ImportPointCloudDataFromUrl(HttpClient client, string url);

    static abstract Task<CalibrationData> ImportCalibrationData(Stream stream);
    static abstract Task<CalibrationData> ImportCalibrationDataFromFile(string path);
    static abstract Task<CalibrationData> ImportCalibrationDataFromUrl(HttpClient client, string url);

    static abstract Task<PosesData> ImportPosesData(Stream stream);
    static abstract Task<PosesData> ImportPosesDataFromFile(string path);
    static abstract Task<PosesData> ImportPosesDataFromUrl(HttpClient client, string url);

    static abstract Task<LabelData> ImportLabelData(Stream stream);
    static abstract Task<LabelData> ImportLabelDataFromFile(string path);
    static abstract Task<LabelData> ImportLabelDataFromUrl(HttpClient client, string url);

    static abstract void ApplyCalibrationData(PointCloudData pointCloudData, CalibrationData calibrationData,
        Matrix4 transform);

    protected static Matrix3x4 ToMatrix3x4(float[] v)
    {
        if (v.Length != 12)
            throw new InvalidDataException(
                $"Expected 12 values for Matrix3x4, got {v.Length}.");

        return new Matrix3x4(
            v[0], v[1], v[2], v[3],
            v[4], v[5], v[6], v[7],
            v[8], v[9], v[10], v[11]
        );
    }

    protected static Matrix3 ToMatrix3(float[] v)
    {
        if (v.Length != 9)
            throw new InvalidDataException(
                $"Expected 9 values for Matrix3, got {v.Length}.");

        return new Matrix3(
            v[0], v[1], v[2],
            v[3], v[4], v[5],
            v[6], v[7], v[8]
        );
    }

    protected static Matrix4 ToMatrix4(float[] v)
    {
        if (v.Length != 16)
            throw new InvalidDataException(
                $"Expected 16 values for Matrix4, got {v.Length}.");

        return new Matrix4(
            v[0], v[1], v[2], v[3],
            v[4], v[5], v[6], v[7],
            v[8], v[9], v[10], v[11],
            v[12], v[13], v[14], v[15]
        );
    }


    public static Matrix4 Matrix3x4ToMatrix4(Matrix3x4 matrix3x4)
    {
        return new Matrix4(
            matrix3x4.Row0,
            matrix3x4.Row1,
            matrix3x4.Row2,
            new Vector4(0.0f, 0.0f, 0.0f, 1.0f)
        );
    }
}