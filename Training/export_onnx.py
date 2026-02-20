import torch
from train import DigitCNN

model = DigitCNN()
model.load_state_dict(torch.load("digit_model.pth", map_location="cpu"))
model.eval()

dummy = torch.zeros(1, 1, 96, 64)  # Batch=1, grayscale, H=96, W=64

torch.onnx.export(
    model,
    dummy,  # type: ignore[arg-type]
    "digit_model.onnx",
    input_names=["image"],
    output_names=["logits"],
    dynamic_axes={"image": {0: "batch"}},
    opset_version=17
)

print("digit_model.onnx exported.")