Place `coconut-yolov8s.onnx` here (output of `training/train_and_export.py`).

For a smoke test before you have coconut data, export stock COCO YOLOv8n
(`yolo export model=yolov8n.pt format=onnx`) and point `ModelPath` at it. Perception refuses to start
when the model's class count differs from `DetectionClass`, so a COCO model also needs
`AllowClassCountMismatch` set:

    Perception__ModelPath=../../models/yolov8n.onnx Perception__AllowClassCountMismatch=true dotnet run --project src/CoconutHarvest.Perception

Class names will be wrong (COCO "person" publishes as `bunch_mature`) but the pipeline, tracker, and
ZeroMQ transport will run. Never use this for testing the Human/Wire abort veto.
