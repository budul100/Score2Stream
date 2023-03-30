using OpenCvSharp;
using Xunit;

namespace WebcamService.Tests
{
    public class Tests
    {
        #region Public Methods

        [Fact]
        public void TestBack()
        {
            var imageComparer = new WebcamService.Service(default);
            imageComparer.ThresholdMonochrome = 0.8;

            var image08 = Cv2.ImRead(@"..\..\..\..\Additionals\test_images\test_video_08.png", ImreadModes.Color);
            imageComparer.Set(image08, 430, 0, 600, 200, "8");

            var image09 = Cv2.ImRead(@"..\..\..\..\Additionals\test_images\test_video_09.png", ImreadModes.Color);
            imageComparer.Set(image09, 430, 0, 600, 200, "9");

            var image = Cv2.ImRead(@"..\..\..\..\Additionals\test_images\test_video_example.png", ImreadModes.Color);
            var result = imageComparer.Get(image, 780, 0, 850, 200);

            Assert.True(result == "9");
        }

        [Fact]
        public void TestFront()
        {
            var imageComparer = new WebcamService.Service(default);
            imageComparer.ThresholdMonochrome = 0.8;

            var image08 = Cv2.ImRead(@"..\..\..\..\Additionals\test_images\test_video_08.png", ImreadModes.Color);
            imageComparer.Set(image08, 430, 0, 600, 200, "8");

            var image09 = Cv2.ImRead(@"..\..\..\..\Additionals\test_images\test_video_09.png", ImreadModes.Color);
            imageComparer.Set(image09, 430, 0, 600, 200, "9");

            var image = Cv2.ImRead(@"..\..\..\..\Additionals\test_images\test_video_example.png", ImreadModes.Color);
            var result = imageComparer.Get(image, 430, 0, 600, 200);

            Assert.True(result == "8");
        }

        #endregion Public Methods
    }
}