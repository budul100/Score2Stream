@echo off
setlocal enabledelayedexpansion

echo ============================================
echo  Score2Stream - Training Pipeline
echo ============================================
echo.

:: Check Python
python --version >nul 2>&1
if errorlevel 1 (
    echo [ERROR] Python not found. Please install Python first.
    pause
    exit /b 1
)

echo [1/6] Checking and installing missing pip packages...
pip install torch torchvision --index-url https://download.pytorch.org/whl/cpu --quiet
pip install pillow numpy opencv-python tqdm onnxscript onnx --quiet
echo       Packages OK.
echo.

echo [2/6] Checking fonts folder...
if not exist fonts (
    mkdir fonts
    echo       Created fonts\ folder. Place TTF files there to enable font-based training.
) else (
    for %%f in (fonts\*.ttf) do set FONT_FOUND=1
    if defined FONT_FOUND (
        echo       Fonts found OK.
    ) else (
        echo       [WARN] No TTF files found in fonts\. Font-based training will be skipped.
    )
)
echo.

echo [3/6] Removing old training data...
if exist training-data (
    rmdir /s /q training-data
    echo       Old data removed.
) else (
    echo       No old data found.
)
echo.

echo [4/6] Generating synthetic training data...
python generate.py
if errorlevel 1 (
    echo [ERROR] generate.py failed.
    pause
    exit /b 1
)
echo       Training data generated.
echo.

echo [5/6] Training model...
python train.py
if errorlevel 1 (
    echo [ERROR] train.py failed.
    pause
    exit /b 1
)
echo       Training complete.
echo.

echo [6/6] Exporting ONNX model...
python export.py
if errorlevel 1 (
    echo [ERROR] export.py failed.
    pause
    exit /b 1
)
echo       ONNX export complete.
echo.

echo ============================================
echo  Done! digit_model.onnx is ready.
echo ============================================
pause