# Fine-Tuning in C# mit ML.NET

```csharp
var mlContext = new MLContext();

// Samples laden
var data = mlContext.Data.LoadFromEnumerable(LoadSamples(samplesPath));

var pipeline = mlContext.Transforms
    // Bild laden und auf Modellgröße bringen
    .LoadRawImageBytes("ImageBytes", null, "ImagePath")
    // Feature-Extraktion via ONNX (eingefroren, wird nicht trainiert)
    .Append(mlContext.Transforms.ApplyOnnxModel(
        modelFile: "digit_features.onnx",
        outputColumnNames: new[] { "features" },
        inputColumnNames:  new[] { "image" }))
    // Nur dieser kleine Klassifikator wird trainiert
    .Append(mlContext.MulticlassClassification.Trainers
        .LbfgsMaximumEntropy(
            labelColumnName:   "Label",
            featureColumnName: "features"))
    .Append(mlContext.Transforms.Conversion
        .MapKeyToValue("PredictedLabel"));

var finetuned = pipeline.Fit(data);
mlContext.Model.Save(finetuned, data.Schema, "digit_finetuned.zip");

```

# Inferenz nach dem Fine-Tuning

Du hast dann zwei Modelle, die hintereinander laufen:

```csharp
public class DigitRecognizer : IDisposable
{
    private readonly InferenceSession _extractor;   // digit_features.onnx
    private readonly PredictionEngine<DigitFeatures, DigitPrediction> _classifier;

    public (string Value, float Confidence) Predict(Mat crop)
    {
        // Schritt 1: Features via ONNX
        float[] image   = Preprocess(crop);
        float[] features = RunOnnx(_extractor, image);
    
        // Schritt 2: Klassifikation via ML.NET
        var result = _classifier.Predict(new DigitFeatures { Features = features });
    
        return (result.PredictedLabel, result.Score.Max());
    }

}

```

# Deployment

```
/deine-app
    digit_model.onnx         ← volle Inferenz (kein Fine-Tuning)
    digit_features.onnx      ← Feature-Extraktor für Fine-Tuning
    digit_finetuned.zip      ← wird erst nach Fine-Tuning erzeugt
    Microsoft.ML.OnnxRuntime.dll
    Microsoft.ML.dll
```