﻿using OpenCvSharp;
using System;
using System.Collections.Generic;
using System.Linq;

namespace Score2Stream.Core.Extensions
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
                        x: 0,
                        y: 0,
                        width: image.Width,
                        height: image.Height);
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
                    method: TemplateMatchModes.CCoeffNormed);

                match.MinMaxLoc(
                    minVal: out _,
                    maxVal: out double max);

                result = Math.Abs(max);
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
                var eroded = image.Erode(
                    element: default,
                    anchor: new Point(-1, -1),
                    iterations: erodeIterations,
                    borderType: BorderTypes.Default,
                    borderValue: new Scalar(1));

                result = eroded.Dilate(
                    element: default,
                    anchor: new Point(-1, -1),
                    iterations: dilateIterations,
                    borderType: BorderTypes.Default,
                    borderValue: new Scalar(1));
            }

            return result;
        }

        #endregion Public Methods
    }
}