# CoconutHarvest Perception (.NET 10 / C# 14)

The perception service from a coconut-harvesting drone project: camera frames go through a YOLOv8 ONNX
model and an IoU tracker, and tracked detections are published as JSON over ZeroMQ.

```
CoconutHarvest.slnx
├── src/CoconutHarvest.Contracts    DetectionFrame schema, DetectionClass enum, JSON source-gen
├── src/CoconutHarvest.Perception   Camera → YOLOv8 (ONNX Runtime) → IoU tracker → ZeroMQ PUB
├── tests/CoconutHarvest.Tests      xUnit: letterbox, tracker, camera geometry, model shape, serialization
└── models/                         put your ONNX model here (not committed)
```

This repository contains the perception side only. Flight control and mission logic are not included.

## Demo

A narrated walkthrough of the project:

https://github.com/user-attachments/assets/9a3685f6-76ec-430c-ba36-9c20e7118a56

## Pipeline

```
VideoCapture.Read
  → Letterbox to 640×640
  → ONNX inference (YOLOv8 output [1, 4+classes, N])
  → per-class NMS with per-class confidence thresholds
  → IouTracker (stable track IDs, age in frames)
  → bearing/elevation from camera field of view
  → DetectionFrame JSON on tcp://127.0.0.1:5556 (topic "det")
```

Example frame:

```json
{
  "schema_version": 1,
  "frame_id": 48211,
  "timestamp_us": 1724900000123456,
  "camera_id": "front",
  "model_version": "coconut-yolov8s-dev",
  "inference_ms": 18.4,
  "detections": [
    {
      "track_id": 7,
      "class": "bunch_mature",
      "confidence": 0.91,
      "bbox_norm": { "x": 0.42, "y": 0.31, "w": 0.11, "h": 0.14 },
      "bearing_deg": -3.2,
      "elevation_deg": 12.8,
      "age_frames": 22
    }
  ]
}
```

Classes, in model index order: `bunch_mature`, `bunch_tender`, `bunch_dry`, `trunk`, `frond`, `human`, `wire`.

## Build and test

Requires the .NET 10 SDK. Tests run on Windows, because the OpenCV native runtime is referenced for Windows.

```
dotnet build CoconutHarvest.slnx
dotnet test CoconutHarvest.slnx
```

## Run

No model is included. Perception refuses to start if the model's class count differs from `DetectionClass`,
so a model trained on these 7 classes is expected at `models/coconut-yolov8s.onnx`.

```
dotnet run --project src/CoconutHarvest.Perception
```

To test only the camera, inference and transport path with a stock COCO YOLOv8 model, see
[`models/README.md`](models/README.md). Classifications from a COCO model are meaningless.

Settings live in `src/CoconutHarvest.Perception/appsettings.json` and can be overridden with environment
variables such as `Perception__ModelPath` and `Perception__CameraIndex`.

## Limitations

- `CameraGeometry` uses a pinhole approximation from field of view; calibrate the camera for accurate angles.
- `IRangeSource` is a stub that returns no range; a depth sensor is needed for distance.
- CPU inference by default. On a Jetson, switch to `Microsoft.ML.OnnxRuntime.Gpu` and the Linux ARM OpenCV runtime.

## License

MIT. See [LICENSE](LICENSE).
