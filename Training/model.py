# model.py

import torch.nn as nn


class DigitCNN(nn.Module):
    """Lightweight CNN for 7-segment and dot-matrix digit recognition (classes 0-9)."""

    def __init__(self):
        super().__init__()
        self.features = nn.Sequential(
            nn.Conv2d(1, 16, 3, padding=1), nn.ReLU(), nn.MaxPool2d(2),  # → 32x48
            nn.Conv2d(16, 32, 3, padding=1), nn.ReLU(), nn.MaxPool2d(2), # → 16x24
            nn.Conv2d(32, 64, 3, padding=1), nn.ReLU(), nn.MaxPool2d(2), # → 8x12
        )
        self.classifier = nn.Sequential(
            nn.Flatten(),
            nn.Linear(64 * 8 * 12, 128), # [0] Flatten, [1] Linear
            nn.ReLU(),                    # [2] ReLU
            nn.Dropout(0.3),             # [3] Dropout
            nn.Linear(128, 10)           # [4] Output
        )

    def forward(self, x):
        return self.classifier(self.features(x))


class DigitFeatureExtractor(nn.Module):
    """Extracts 128-dimensional features from a DigitCNN for fine-tuning in C# via ML.NET."""

    def __init__(self, base: DigitCNN):
        super().__init__()
        self.features = base.features
        self.flatten  = nn.Flatten()
        self.fc1      = base.classifier[1]  # Linear(64*8*12, 128)
        self.relu     = base.classifier[2]  # ReLU

    def forward(self, x):
        return self.relu(self.fc1(self.flatten(self.features(x))))