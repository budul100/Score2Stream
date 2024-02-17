using System;
using System.IO;
using OpenCvSharp;
using Score2Stream.Commons.Interfaces;
using TesseractOCR;
using TesseractOCR.Enums;
using TesseractOCR.Pix;

namespace Score2Stream.RecognitionService
{
    public class Service
        : IRecognitionService
    {
        #region Private Fields

        private const PageSegMode PageModeDefault = PageSegMode.SingleChar;
        private const string TesseractLanguages = "letsgodigital";
        private const string TesseractTrainedData = "TrainedData";

        private readonly Engine engine;

        #endregion Private Fields

        #region Public Constructors

        public Service()
        {
            var dataPath = Path.Combine(
                path1: Environment.CurrentDirectory,
                path2: TesseractTrainedData);

            if (Directory.Exists(dataPath))
            {
                engine = new Engine(
                    dataPath: dataPath,
                    languages: TesseractLanguages);
            }
        }

        #endregion Public Constructors

        #region Public Methods

        public string Recognize(Mat image)
        {
            var text = GetText(image);

            var result = text?.Trim()
                .Replace(
                    oldValue: "\r",
                    newValue: string.Empty)
                .Replace(
                    oldValue: "\n",
                    newValue: string.Empty);

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