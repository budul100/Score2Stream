using OpenCvSharp;
using System;
using System.Linq;

namespace WebcamService.Extensions
{
    internal static class ImageExtensions
    {
        #region Public Methods

        public static double DiffTo(this Mat image, Mat template)
        {
            var compare = image.Resize(
                dsize: template.Size(),
                interpolation: InterpolationFlags.Nearest);

            var match = compare.MatchTemplate(
                templ: template,
                method: TemplateMatchModes.CCoeffNormed);

            match.MinMaxLoc(
                minVal: out _,
                maxVal: out double result);

            return Math.Abs(result);
        }

        public static Rect? GetContour(this Mat image)
        {
            var result = default(Rect?);

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

            return result;
        }

        public static Mat ToMonochrome(this Mat image, double threshold)
        {
            var monochromeImage = image.Channels() > 1
                ? image.CvtColor(ColorConversionCodes.BGR2GRAY)
                : image;

            var thresh = threshold * 255;

            var result = monochromeImage.Threshold(
                thresh: thresh,
                maxval: 255,
                type: ThresholdTypes.Binary);

            return result;
        }

        #endregion Public Methods
    }
}