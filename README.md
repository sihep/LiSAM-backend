# LiSAM Backend

C# backend for LiSAM. 

current features: 

- velodyne/point cloud data loading
- data display

### Drawing arcs

Use `Visualizer.AddArc` with a center, positive radius, signed sweep angle in
radians, orientation quaternion, and RGBA color. The unrotated arc lies in the
XY plane, starting along +X and sweeping toward +Y for positive angles.
Orientation rotates that plane about the center; `Quaternion.Identity` leaves it
in XY. Negative sweeps reverse direction. Sweeps must be within ±2π; zero draws
nothing and ±2π closes a circle.

```csharp
using LiSAM.Visualization;
using OpenTK.Mathematics;

// Quarter circle of radius 2 about (1, 2, 3), in the XY plane.
visualizer.AddArc(
    center: new Vector3(1, 2, 3),
    radius: 2f,
    sweepAngle: MathF.PI / 2,
    orientation: Quaternion.Identity,
    color: new Vector4(1, 0.5f, 0, 1));

// Rotate the local plane 90 degrees about X; start along local +Y.
visualizer.AddArc(new CloudArc(
    Center: Vector3.Zero,
    Radius: 3f,
    SweepAngle: -MathF.PI,
    Orientation: Quaternion.FromAxisAngle(Vector3.UnitX, MathF.PI / 2),
    Color: new Vector4(0, 1, 1, 1),
    StartAngle: MathF.PI / 2,
    Segments: 64));
```

Arcs are outlines approximated by line segments. `segments: 0` (the default)
uses at most 5° per segment; use an explicit count from 1 to 65536 for finer
control. Nonzero finite quaternions are normalized automatically. Arcs can be
added before or during rendering and are removed by `ClearPrimitives()`.
`CloudArc.ToLines()` exposes the geometry without an OpenGL context.

Run the headless geometry checks with:

```sh
dotnet run --project tests/LiSAM.Visualization.GeometryChecks --configuration Release
```


<img width="1535" height="863" alt="image" src="https://github.com/user-attachments/assets/a9a1e1dd-efc3-45f9-809b-2676c1bcc8a9" />
