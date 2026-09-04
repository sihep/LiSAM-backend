# Data Importer Implementation Guide

This guide explains how to add a new dataset importer to `LiSAM.Core`.

A dataset importer is responsible for translating a dataset's native representation into LiSAM's common data structures. Dataset-specific parsing and label mappings belong here; spatial processing, ML, and inference do not.

---

## 1. Create the Importer

Create:

```text
src/
└── LiSAM.Core/
    └── Data/
        └── DataImporters/
            └── MyDatasetDataImporter.cs
```

Start from this skeleton:

```csharp
using System.Globalization;
using System.Net.Http;
using OpenTK.Mathematics;

namespace LiSAM.Core.Data;

public static class MyDatasetDataImporter : IDataImporter
{
    // ============================================================
    // Point Cloud
    // ============================================================

    public static async Task<PointCloudData> ImportPointCloudData(
        Stream stream)
    {
        ArgumentNullException.ThrowIfNull(stream);

        // TODO:
        // Parse the dataset's native point-cloud representation.

        throw new NotImplementedException();
    }

    public static async Task<PointCloudData> ImportPointCloudDataFromFile(
        string path)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(path);

        await using var stream = File.OpenRead(path);

        return await ImportPointCloudData(stream);
    }

    public static async Task<PointCloudData> ImportPointCloudDataFromUrl(
        HttpClient client,
        string url)
    {
        ArgumentNullException.ThrowIfNull(client);
        ArgumentException.ThrowIfNullOrWhiteSpace(url);

        await using var stream =
            await client.GetStreamAsync(url);

        return await ImportPointCloudData(stream);
    }


    // ============================================================
    // Calibration
    // ============================================================

    public static async Task<CalibrationData> ImportCalibrationData(
        Stream stream)
    {
        ArgumentNullException.ThrowIfNull(stream);

        // TODO:
        // Parse calibration information.

        throw new NotImplementedException();
    }

    public static async Task<CalibrationData> ImportCalibrationDataFromFile(
        string path)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(path);

        await using var stream = File.OpenRead(path);

        return await ImportCalibrationData(stream);
    }

    public static async Task<CalibrationData> ImportCalibrationDataFromUrl(
        HttpClient client,
        string url)
    {
        ArgumentNullException.ThrowIfNull(client);
        ArgumentException.ThrowIfNullOrWhiteSpace(url);

        await using var stream =
            await client.GetStreamAsync(url);

        return await ImportCalibrationData(stream);
    }


    // ============================================================
    // Poses
    // ============================================================

    public static async Task<PosesData> ImportPosesData(
        Stream stream)
    {
        ArgumentNullException.ThrowIfNull(stream);

        // TODO:
        // Parse poses / trajectory information.

        throw new NotImplementedException();
    }

    public static async Task<PosesData> ImportPosesDataFromFile(
        string path)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(path);

        await using var stream = File.OpenRead(path);

        return await ImportPosesData(stream);
    }

    public static async Task<PosesData> ImportPosesDataFromUrl(
        HttpClient client,
        string url)
    {
        ArgumentNullException.ThrowIfNull(client);
        ArgumentException.ThrowIfNullOrWhiteSpace(url);

        await using var stream =
            await client.GetStreamAsync(url);

        return await ImportPosesData(stream);
    }


    // ============================================================
    // Labels
    // ============================================================

    public static async Task<LabelData> ImportLabelData(
        Stream stream)
    {
        ArgumentNullException.ThrowIfNull(stream);

        // TODO:
        // Parse semantic and/or instance labels.

        throw new NotImplementedException();
    }

    public static async Task<LabelData> ImportLabelDataFromFile(
        string path)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(path);

        await using var stream = File.OpenRead(path);

        return await ImportLabelData(stream);
    }

    public static async Task<LabelData> ImportLabelDataFromUrl(
        HttpClient client,
        string url)
    {
        ArgumentNullException.ThrowIfNull(client);
        ArgumentException.ThrowIfNullOrWhiteSpace(url);

        await using var stream =
            await client.GetStreamAsync(url);

        return await ImportLabelData(stream);
    }


    // ============================================================
    // Calibration Application
    // ============================================================

    public static void ApplyCalibrationData(
        PointCloudData pointCloudData,
        CalibrationData calibrationData,
        Matrix4 transform)
    {
        // TODO:
        // Apply the dataset-specific calibration / transformation.

        throw new NotImplementedException();
    }


    // ============================================================
    // Dataset-specific helpers
    // ============================================================

    // Keep parsing helpers private to this importer.

    private static LidarSemanticLabel MapSemanticLabel(
        int datasetLabel)
    {
        return datasetLabel switch
        {
            // TODO:
            // 0 => LidarSemanticLabel.Unknown,
            // 1 => LidarSemanticLabel.Car,
            // ...

            _ => LidarSemanticLabel.Unknown
        };
    }
}
```

---

# 2. Point Cloud Parser

The most important importer method is:

```csharp
ImportPointCloudData(Stream stream)
```

It must ultimately produce:

```csharp
PointCloudData
```

with:

```text
Points[i]
Intensities[i]
```

referring to the same LiDAR return.

For a dataset storing:

```text
x y z intensity
```

the resulting mapping is:

$$
\text{record}_i
\rightarrow
(\text{Points}[i],\text{Intensities}[i]).
$$

For binary formats, parse the native record structure explicitly.

Example:

```csharp
var points = new List<Vector3>();
var intensities = new List<float>();

using var reader = new BinaryReader(
    stream,
    System.Text.Encoding.UTF8,
    leaveOpen: true);

while (stream.Position < stream.Length)
{
    float x = reader.ReadSingle();
    float y = reader.ReadSingle();
    float z = reader.ReadSingle();
    float intensity = reader.ReadSingle();

    points.Add(new Vector3(x, y, z));
    intensities.Add(intensity);
}

if (points.Count != intensities.Count)
{
    throw new InvalidDataException(
        "Point and intensity counts do not match.");
}

return new PointCloudData(
    points.ToArray(),
    intensities.ToArray());
```

The actual binary layout must be replaced with the dataset's documented format.

---

# 3. Text-Based Point Clouds

For line-oriented text formats:

```csharp
using var reader = new StreamReader(
    stream,
    leaveOpen: true);

var points = new List<Vector3>();
var intensities = new List<float>();

string? line;

while ((line = await reader.ReadLineAsync()) is not null)
{
    if (string.IsNullOrWhiteSpace(line))
        continue;

    var values = line.Split(
        ' ',
        StringSplitOptions.RemoveEmptyEntries);

    if (values.Length < 4)
    {
        throw new InvalidDataException(
            $"Expected at least 4 values, got {values.Length}.");
    }

    float x = float.Parse(
        values[0],
        CultureInfo.InvariantCulture);

    float y = float.Parse(
        values[1],
        CultureInfo.InvariantCulture);

    float z = float.Parse(
        values[2],
        CultureInfo.InvariantCulture);

    float intensity = float.Parse(
        values[3],
        CultureInfo.InvariantCulture);

    points.Add(new Vector3(x, y, z));
    intensities.Add(intensity);
}

return new PointCloudData(
    points.ToArray(),
    intensities.ToArray());
```

Always use:

```csharp
CultureInfo.InvariantCulture
```

for dataset numerical values.

This prevents locale-dependent parsing such as:

```text
1.25
```

being interpreted incorrectly on systems using a comma decimal separator.

---

# 4. Label Parser

Labels must remain aligned with points.

For every point:

```text
Points[i]
Labels[i]
InstanceIDs[i]
```

must refer to the same point.

Example:

```csharp
var labels = new LidarSemanticLabel[count];
var instanceIds = new int[count];

for (int i = 0; i < count; i++)
{
    int datasetLabel = ...;
    int instanceId = ...;

    labels[i] = MapSemanticLabel(datasetLabel);
    instanceIds[i] = instanceId;
}

return new LabelData(
    labels,
    instanceIds);
```

Before returning:

```csharp
if (labels.Length != instanceIds.Length)
{
    throw new InvalidDataException(
        "Semantic and instance label counts do not match.");
}
```

When labels correspond to a point cloud:

```csharp
if (labels.Length != pointCount)
{
    throw new InvalidDataException(
        $"Expected {pointCount} labels, got {labels.Length}.");
}
```

---

# 5. Semantic Label Mapping

Do not expose dataset-specific numeric IDs to the rest of LiSAM.

Use an explicit mapping:

```csharp
private static LidarSemanticLabel MapSemanticLabel(
    int datasetLabel)
{
    return datasetLabel switch
    {
        0  => LidarSemanticLabel.Unknown,

        1  => LidarSemanticLabel.Car,
        2  => LidarSemanticLabel.Truck,
        3  => LidarSemanticLabel.Bus,

        4  => LidarSemanticLabel.Pedestrian,

        // TODO: complete dataset-specific mapping

        _ => LidarSemanticLabel.Unknown
    };
}
```

If multiple source classes map to one LiSAM class, that is acceptable:

```text
Dataset A: Sedan
Dataset A: SUV
Dataset A: Van
        ↓
LidarSemanticLabel.Car
```

Document such mappings in comments.

---

# 6. Calibration Parser

For a text calibration format, parse the values first:

```csharp
private static float[] ParseFloatValues(
    string line)
{
    var tokens = line.Split(
        ' ',
        StringSplitOptions.RemoveEmptyEntries);

    var values = new float[tokens.Length];

    for (int i = 0; i < tokens.Length; i++)
    {
        values[i] = float.Parse(
            tokens[i],
            CultureInfo.InvariantCulture);
    }

    return values;
}
```

Then construct the appropriate matrix using the helper methods supplied by `IDataImporter`.

For example:

```csharp
var values = ParseFloatValues(line);

var matrix =
    IDataImporter.ToMatrix3x4(values);
```

A calibration parser should validate the expected number of values.

For a `3×4` transform:

$$
3\times4=12
$$

values are required.

---

# 7. Pose Parser

A pose is commonly represented as a rigid transformation:

$$
T=
\begin{bmatrix}
R & t\\
0 & 1
\end{bmatrix}
$$

where:

$$
R\in\mathbb{R}^{3\times3}
$$

is the rotation and:

$$
t\in\mathbb{R}^{3}
$$

is the translation.

If the dataset stores a `3×4` matrix directly, convert each pose into:

```csharp
Matrix3x4
```

and preserve the source frame ordering.

Example structure:

```csharp
var transforms = new List<Matrix3x4>();

while (...)
{
    var values = ParseFloatValues(line);

    if (values.Length != 12)
    {
        throw new InvalidDataException(
            "Expected 12 pose values.");
    }

    transforms.Add(
        IDataImporter.ToMatrix3x4(values));
}

return new PosesData(
    transforms.ToArray());
```

---

# 8. Calibration Application

The exact implementation depends on the dataset.

The method exists so that the importer can define how its calibration should be applied:

```csharp
public static void ApplyCalibrationData(
    PointCloudData pointCloudData,
    CalibrationData calibrationData,
    Matrix4 transform)
{
    // dataset-specific transformation
}
```

When modifying points, preserve the point/intensity relationship.

For a homogeneous point:

$$
p=
\begin{bmatrix}
x\\y\\z\\1
\end{bmatrix}
$$

and transformation:

$$
T\in\mathbb{R}^{4\times4},
$$

the transformed point is:

$$
p'=Tp.
$$

Be explicit about which coordinate system is the input and which coordinate system is the output.

---

# 9. Keep Helpers Inside the Importer

Dataset-specific parsing helpers should normally remain private:

```csharp
private static ...
```

Examples:

```text
ParseLine()
ParseBinaryRecord()
ParseFloatValues()
MapSemanticLabel()
DecodeInstanceId()
ParsePose()
ParseCalibrationLine()
```

This keeps dataset-specific implementation details out of `DataModels.cs` and the rest of `LiSAM.Core`.

---

# 10. File → Stream → Parser

The preferred relationship is:

```text
ImportFromFile
      ↓
   FileStream
      ↓
ImportFromStream
      ↓
 dataset parser
```

and:

```text
ImportFromUrl
      ↓
 HTTP response stream
      ↓
ImportFromStream
      ↓
 dataset parser
```

Do not implement the same parsing logic separately three times.

---

# 11. Example Final Importer Layout

A complete importer might eventually look like:

```text
DataImporters/
└── MyDatasetDataImporter.cs
    │
    ├── ImportPointCloudData()
    ├── ImportPointCloudDataFromFile()
    ├── ImportPointCloudDataFromUrl()
    │
    ├── ImportCalibrationData()
    ├── ImportCalibrationDataFromFile()
    ├── ImportCalibrationDataFromUrl()
    │
    ├── ImportPosesData()
    ├── ImportPosesDataFromFile()
    ├── ImportPosesDataFromUrl()
    │
    ├── ImportLabelData()
    ├── ImportLabelDataFromFile()
    ├── ImportLabelDataFromUrl()
    │
    ├── ApplyCalibrationData()
    │
    └── private dataset-specific helpers
```

---

# 12. Validation

Before considering the importer complete, test at least one real frame/sequence and verify:

```text
PointCloudData.Points.Length
==
PointCloudData.Intensities.Length
```

and, for labelled data:

```text
PointCloudData.Points.Length
==
LabelData.Labels.Length
==
LabelData.InstanceIDs.Length
```

Also inspect:

```text
point count
X/Y/Z ranges
intensity ranges
semantic-label distribution
instance count
pose count
calibration values
```

A point-cloud visualization through `LiSAM.Visualization` is useful for detecting coordinate-system and parsing errors that numerical checks may not reveal.

---

# 13. What Belongs Here

```text
✅ Native file parsing
✅ Native binary decoding
✅ Native label decoding
✅ Dataset → LiSAM label mapping
✅ Calibration parsing
✅ Pose parsing
✅ Dataset coordinate transformations
✅ Dataset-specific validation
```

## What Does Not

```text
❌ Polar grids
❌ Quadtrees
❌ ROI generation
❌ Point-cloud cropping for inference
❌ TorchSharp models
❌ Neural-network inference
❌ Segmentation
❌ Mask merging
```

The boundary remains:

```text
Dataset
   │
   ▼
DataImporter
   │
   ▼
PointCloudData
LabelData
CalibrationData
PosesData
   │
   ▼
LiSAM Spatial / ML / Inference
```

---

## Existing Importers

Use the existing implementations as references when adding another importer:

```text
LiSAM.Core/
└── Data/
    └── DataImporters/
        ├── SemanticKITTIDataImporter.cs
        └── HeLiMOSDataImporter.cs
```

A new importer should follow the same public contract while keeping all dataset-specific assumptions inside its own implementation.
