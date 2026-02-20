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

echo [1/5] Checking and installing missing pip packages...
pip install torch torchvision --index-url https://download.pytorch.org/whl/cpu --quiet
pip install pillow numpy opencv-python tqdm onnxscript --quiet
echo       Packages OK.
echo.

echo [2/5] Removing old training data...
if exist training-data (
    rmdir /s /q training-data
    echo       Old data removed.
) else (
    echo       No old data found.
)
echo.

echo [3/5] Generating synthetic training data...
python generate_data.py
if errorlevel 1 (
    echo [ERROR] generate_data.py failed.
    pause
    exit /b 1
)
echo       Training data generated.
echo.

echo [4/5] Training model...
python train.py
if errorlevel 1 (
    echo [ERROR] train.py failed.
    pause
    exit /b 1
)
echo       Training complete.
echo.

echo [5/5] Exporting ONNX model...
python export_onnx.py
if errorlevel 1 (
    echo [ERROR] export_onnx.py failed.
    pause
    exit /b 1
)
echo       ONNX export complete.
echo.

echo ============================================
echo  Done! digit_model.onnx is ready.
echo ============================================
pause