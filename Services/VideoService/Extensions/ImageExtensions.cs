﻿using OpenCvSharp;
using System;
using System.Linq;

namespace VideoService.Extensions
{
    internal static class ImageExtensions
    {
        #region Public Methods

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
                        .OrderByDescending(c => c.Length).First();

                    result = Cv2.BoundingRect(relevant);
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
                    maxVal: out double value);

                result = Math.Abs(value);
            }

            return result;
        }

        public static Mat ToCentered(this Mat image, int fullWidth, int fullHeight)
        {
            var horizontal = (int)Math.Ceiling((double)(fullWidth - image.Width) / 2);
            var vertical = (int)Math.Ceiling((double)(fullHeight - image.Height) / 2);

            var result = image
                .CopyMakeBorder(
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

        #endregion Public Methods
    }
}