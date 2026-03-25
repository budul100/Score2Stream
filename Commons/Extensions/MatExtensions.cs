using System;
using System.Collections.Generic;
using System.Linq;
using Avalonia.Media.Imaging;
using Avalonia.Platform;
using OpenCvSharp;

namespace Score2Stream.Commons.Extensions
{
    public static class MatExtensions
    {
        #region Private Fields

        private const float MinMeanBrightness = 0.05f;

        #endregion Private Fields

        #region Public Methods

        public static Mat AsBlended(this IEnumerable<Mat> images)
        {
            var list = images
                .Where(i => i.HasValue()).ToList();

            if (list.Count == 0) return default;

            var convert = new Mat();

            list[0].ConvertTo(
                m: convert,
                rtype: MatType.CV_32F);

            for (var i = 1; i < list.Count; i++)
            {
                if (list[i].Width != list[0].Width
                    || list[i].Height != list[0].Height) continue;

                using var floatMat = new Mat();

                list[i].ConvertTo(
                    m: floatMat,
                    rtype: MatType.CV_32F);

                Cv2.AddWeighted(
                    src1: convert,
                    alpha: (double)i / (i + 1),
                    src2: floatMat,
                    beta: 1.0 / (i + 1),
                    gamma: 0,
                    dst: convert);
            }

            var result = new Mat();

            convert.ConvertTo(
                m: result,
                rtype: list[0].Type());

            convert.Dispose();

            return result;
        }

        public static Mat AsCentered(this Mat image, double fullWidth, double fullHeight)
        {
            var result = image;

            if (image.HasValue())
            {
                var horizontal = (int)Math.Ceiling((double)Math.Abs(fullWidth - image.Width) / 2);
                var vertical = (int)Math.Ceiling((double)Math.Abs(fullHeight - image.Height) / 2);

                result = image.CopyMakeBorder(
                    top: vertical,
                    bottom: vertical,
                    left: horizontal,
                    right: horizontal,
                    borderType: BorderTypes.Constant);
            }

            return result;
        }

        public static Mat AsCropped(this Mat image, Rect contourRectangle)
        {
            var result = image
                .Clone(contourRectangle);

            return result;
        }

        public static Mat AsInverted(this Mat image)
        {
            var result = image;

            if (image.HasValue())
            {
                result = new Mat(
                    rows: image.Rows,
                    cols: image.Cols,
                    type: image.Type());

                Cv2.BitwiseNot(
                    src: image,
                    dst: result);
            }

            return result;
        }

        public static Mat AsMonochrome(this Mat image, double threshold)
        {
            var result = image;

            if (image.HasValue())
            {
                var monochromeImage = image.Channels() > 1
                    ? image.CvtColor(ColorConversionCodes.BGR2GRAY)
                    : image;

                var thresh = threshold * 255;

                result = monochromeImage.Threshold(
                    thresh: thresh,
                    maxval: 255,
                    type: ThresholdTypes.Binary);

                if (!ReferenceEquals(
                    objA: monochromeImage,
                    objB: image))
                {
                    monochromeImage.Dispose();
                }
            }

            return result;
        }

        public static Mat AsRotated(this Mat image, float angle)
        {
            var result = image;

            if (angle != 0)
            {
                var size = image.Size();

                result = new Mat(
                    size: size,
                    type: image.Depth());

                var cornersImage = new Point2f[]
                {
                    new(0F, size.Height),
                    new(0F, 0F),
                    new(size.Width, 0F),
                    new(size.Width, size.Height)
                };

                var center = new Point2f(
                    X: Convert.ToSingle(image.Width) / 2,
                    Y: Convert.ToSingle(image.Height) / 2);

                var rotated = new RotatedRect(
                    center: center,
                    size: size,
                    angle: angle);

                var cornersResult = rotated.Points();

                using var transformed = Cv2.GetAffineTransform(
                    src: cornersImage,
                    dst: cornersResult);

                Cv2.WarpAffine(
                    src: image,
                    dst: result,
                    m: transformed,
                    dsize: size);
            }

            return result;
        }

        public static Mat AsTranslated(this Mat source, int offsetX, int offsetY)
        {
            if (offsetX == 0 && offsetY == 0)
                return source;

            // Build 2x3 affine translation matrix
            var translationMatrix = Mat.FromArray(new float[,]
            {
                { 1, 0, offsetX },
                { 0, 1, offsetY }
            });

            var result = new Mat();

            Cv2.WarpAffine(
                src: source,
                dst: result,
                m: translationMatrix,
                dsize: source.Size());

            source.Dispose();

            return result;
        }

        public static Bitmap GetBitmap(this Mat image, Mat rotated)
        {
            Cv2.CvtColor(
                src: rotated,
                dst: image,
                code: ColorConversionCodes.BGR2BGRA);

            var bitmapSize = new Avalonia.PixelSize(
                width: image.Width,
                height: image.Height);
            var bitmapDPI = new Avalonia.Vector(96, 96);

            var result = new Bitmap(
                format: PixelFormat.Bgra8888,
                alphaFormat: AlphaFormat.Opaque,
                data: image.Data,
                size: bitmapSize,
                dpi: bitmapDPI,
                stride: (int)image.Step());

            return result;
        }

        public static Rect? GetContour(this Mat image)
        {
            var result = default(Rect?);

            if (image.HasValue())
            {
                image.FindContours(
                    contours: out var contours,
                    hierarchy: out _,
                    mode: RetrievalModes.Tree,
                    method: ContourApproximationModes.ApproxSimple);

                if (contours.Length > 0)
                {
                    var relevant = contours
                        .Where(c => c.All(p => p.X > 0
                            && p.Y > 0
                            && p.X < image.Width
                            && p.Y < image.Height))
                        .SelectMany(c => c);

                    result = Cv2.BoundingRect(relevant);
                }

                if (result == default)
                {
                    result = new Rect(
                        X: 0,
                        Y: 0,
                        Width: image.Width,
                        Height: image.Height);
                }
            }

            return result;
        }

        public static float[] GetNormalized(this Mat image, int height, int width)
        {
            var result = default(float[]);

            if (!image.IsEmpty())
            {
                using var gray = new Mat();

                if (image.Channels() > 1)
                {
                    Cv2.CvtColor(
                        src: image,
                        dst: gray,
                        code: ColorConversionCodes.BGR2GRAY);
                }
                else
                {
                    image.CopyTo(gray);
                }

                using var resized = new Mat();

                var size = new Size(
                    Width: width,
                    Height: height);

                Cv2.Resize(
                    src: gray,
                    dst: resized,
                    dsize: size);

                result = new float[height * width];

                for (var y = 0; y < height; y++)
                {
                    for (var x = 0; x < width; x++)
                    {
                        var pixel = resized.At<byte>(y, x) / 255f;
                        result[y * width + x] = (pixel - 0.5f) / 0.5f;
                    }
                }
            }

            return result;
        }

        public static bool HasValue(this Mat image)
        {
            var result = image?.IsDisposed == false
                && !image.Empty()
                && image.Rows > 0
                && image.Cols > 0
                && image.Step(0) > 0;

            return result;
        }

        public static Mat WithoutNoise(this Mat image, int erodeIterations, int dilateIterations)
        {
            var result = image;

            if (image.HasValue())
            {
                var anchor = new Point(-1, -1);
                var border = new Scalar(1);

                var eroded = image.Erode(
                    element: default,
                    anchor: anchor,
                    iterations: erodeIterations,
                    borderType: BorderTypes.Default,
                    borderValue: border);

                result = eroded.Dilate(
                    element: default,
                    anchor: anchor,
                    iterations: dilateIterations,
                    borderType: BorderTypes.Default,
                    borderValue: border);

                eroded.Dispose();
            }

            return result;
        }

        #endregion Public Methods

        #region Private Methods

        private static bool IsEmpty(this Mat image)
        {
            if (image?.HasValue() == true)
            {
                using var gray = image.Channels() > 1
                    ? image.CvtColor(ColorConversionCodes.BGR2GRAY)
                    : image.Clone();

                var mean = Cv2.Mean(gray);

                return (mean.Val0 / 255f) < MinMeanBrightness;
            }

            return true;
        }

        #endregion Private Methods
    }
}