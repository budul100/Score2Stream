using System;
using System.IO;
using OpenCvSharp;
using Score2Stream.Commons.Interfaces;
using TesseractOCR;
using TesseractOCR.Enums;
using TesseractOCR.Pix;

namespace Score2Stream.RecognitionService
{
    public class TesseractRecognizer
        : IRecognitionService
    {
        #region Private Fields

        private const PageSegMode PageModeDefault = PageSegMode.SingleChar;

        private const string TesseractLanguages = "letsgodigital";
        private const string TrainedDataFolder = "TrainedData";

        private readonly Engine engine;

        #endregion Private Fields

        #region Public Constructors

        public TesseractRecognizer()
        {
            var dataPath = Path.Combine(
                path1: Environment.CurrentDirectory,
                path2: TrainedDataFolder);

            if (Directory.Exists(dataPath))
            {
                engine = new Engine(
                    dataPath: dataPath,
                    languages: TesseractLanguages);
            }
        }

        #endregion Public Constructors

        #region Public Methods

        public (string, float) Recognize(Mat image)
        {
            var text = GetText(image);

            var trimmed = text?.Trim()
                .Replace(
                    oldValue: "\r",
                    newValue: string.Empty)
                .Replace(
                    oldValue: "\n",
                    newValue: string.Empty);

            var result = (trimmed, 1f);

            return result;
        }

        #endregion Public Methods

        #region Private Methods

        private string GetText(Mat image)
        {
            var result = default(string);

            try
            {
                var memoryStream = image.ToMemoryStream().ToArray();
                var pixImage = Image.LoadFromMemory(memoryStream);

                using var page = engine.Process(
                    image: pixImage,
                    pageSegMode: PageModeDefault);

                result = page?.Text;
            }
            catch
            { }

            return result;
        }

        #endregion Private Methods
    }
}