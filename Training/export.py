# export.py

import torch
import torch.nn as nn
from model import DigitCNN

model = DigitCNN()
model.load_state_dict(torch.load("digit_model.pth"))
model.eval()

dummy = torch.zeros(1, 1, 96, 64)

# --- Full model ---
torch.onnx.export(
    model, dummy, "digit_model.onnx",  # type: ignore[arg-type]
    input_names=["image"], output_names=["logits"],
    dynamic_axes={"image": {0: "batch"}},
    opset_version=17
)
print("Exported: digit_model.onnx")

# --- Feature extractor (for fine-tuning in C#) ---
class DigitFeatureExtractor(nn.Module):
    def __init__(self, base: DigitCNN):
        super().__init__()
        self.features  = base.features
        self.flatten   = nn.Flatten()
        self.fc1       = base.classifier[1]  # Linear 128
        self.relu      = base.classifier[2]  # ReLU

    def forward(self, x):
        return self.relu(self.fc1(self.flatten(self.features(x))))

extractor = DigitFeatureExtractor(model)
extractor.eval()

torch.onnx.export(
    extractor, dummy, "digit_features.onnx",  # type: ignore[arg-type]
    input_names=["image"], output_names=["features"],
    dynamic_axes={"image": {0: "batch"}},
    opset_version=17
)
print("Exported: digit_features.onnx")

# --- Merge .onnx + .data into single files ---
import onnx

for name in ["digit_model.onnx", "digit_features.onnx"]:
    proto = onnx.load(name)
    onnx.save_model(proto, name, save_as_external_data=False)
    print(f"Merged into single file: {name}")

print("Export complete.")