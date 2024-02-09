using System;
using System.Collections.Generic;
using System.Linq;
using OpenCvSharp;

namespace Score2Stream.Commons.Extensions
{
    public static class MatExtensions
    {
        #region Public Methods

        public static Mat AsBlended(this IEnumerable<Mat> images)
        {
            var result = default(Mat);

            if (images.Any())
            {
                result = images.First();

                var others = images.Skip(1)
                    .Where(i => i.Width == result.Width
                        && i.Height == result.Height).ToArray();

                for (var index = 0; index < others.Length; index++)
                {
                    var alpha = 1.0 / (index + 1);
                    var beta = 1.0 - alpha;

                    Cv2.AddWeighted(
                        src1: others[index],
                        alpha: alpha,
                        src2: result,
                        beta: beta,
                        gamma: 0.0,
                        dst: result);
                }
            }

            return result;
        }

        public static Mat AsRotated(this Mat image, float angle)
        {
            Mat result;

            if (angle == 0)
            {
                result = image;
            }
            else
            {
                var size = image.Size();

                result = new Mat(
                    size: size,
                    type: image.Depth(),
                    s: image.Channels());

                var cornersImage = new Point2f[]
                {
                    new(0F, size.Height),
                    new(0F, 0F),
                    new(size.Width, 0F),
                    new(size.Width, size.Height)
                };

                var center = new Point2f(
                    x: Convert.ToSingle(image.Width) / 2,
                    y: Convert.ToSingle(image.Height) / 2);

                var rotated = new RotatedRect(
                    center: center,
                    size: size,
                    angle: angle);

                var cornersResult = rotated.Points();

                var transformed = Cv2.GetAffineTransform(
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

        public static Rect? GetContour(this Mat image)
        {
            var result = default(Rect?);

            if (image != default
                && image?.Step(0) > 0
                && image.Rows > 0)
            {
                image.FindContours(
                    contours: out var contours,
                    hierarchy: out _,
                    mode: RetrievalModes.Tree,
                    method: ContourApproximationModes.ApproxSimple);

                if (contours.Length > 0)
                {
                    var relevant = contours
                        .Where(c => c.All(p => p.X > 0 && p.Y > 0 && p.X < image.Width && p.Y < image.Height))
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

        public static double GetSimilarityTo(this Mat image, Mat template)
        {
            var result = default(double);

            if (image?.Step(0) > 0
                && image.Rows > 0
                && template?.Step(0) > 0
                && template.Rows > 0)
            {
                var compare = image.Resize(
                    dsize: template.Size(),
                    interpolation: InterpolationFlags.Nearest);

                var match = compare.MatchTemplate(
                    templ: template,
                    method: TemplateMatchModes.SqDiffNormed);

                match.MinMaxLoc(
                    minVal: out double min,
                    maxVal: out double _);

                result = 1 - Math.Abs(min);
            }

            return result;
        }

        public static Mat ToCentered(this Mat image, int fullWidth, int fullHeight)
        {
            var horizontal = (int)Math.Ceiling((double)Math.Abs(fullWidth - image.Width) / 2);
            var vertical = (int)Math.Ceiling((double)Math.Abs(fullHeight - image.Height) / 2);

            var result = image.CopyMakeBorder(
                top: vertical,
                bottom: vertical,
                left: horizontal,
                right: horizontal,
                borderType: BorderTypes.Constant,
                value: 0);

            return result;
        }

        public static Mat ToCropped(this Mat image, Rect contourRectangle)
        {
            var result = image
                .Clone(contourRectangle);

            return result;
        }

        public static Mat ToInverted(this Mat image)
        {
            var result = default(Mat);

            if (image?.Step(0) > 0
                && image.Rows > 0)
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

        public static Mat ToMonochrome(this Mat image, double threshold)
        {
            var result = default(Mat);

            if (image?.Step(0) > 0
                && image.Rows > 0)
            {
                var monochromeImage = image.Channels() > 1
                    ? image.CvtColor(ColorConversionCodes.BGR2GRAY)
                    : image;

                var thresh = threshold * 255;

                result = monochromeImage.Threshold(
                    thresh: thresh,
                    maxval: 255,
                    type: ThresholdTypes.Binary);
            }

            return result;
        }

        public static Mat WithoutNoise(this Mat image, int erodeIterations, int dilateIterations)
        {
            var result = default(Mat);

            if (image?.Step(0) > 0
                && image.Rows > 0)
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
            }

            return result;
        }

        #endregion Public Methods
    }
}