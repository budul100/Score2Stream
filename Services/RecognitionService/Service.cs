using Score2Stream.Core.Interfaces;
using System;
using System.IO;
using TesseractOCR;

namespace Score2Stream.RecognitionService
{
    public class Service
        : IRecognitionService
    {
        #region Private Fields

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

            engine = new Engine(
                dataPath: dataPath,
                languages: TesseractLanguages);
        }

        #endregion Public Constructors

        #region Public Methods

        public string Recognize(byte[] bytes)
        {
            var image = TesseractOCR.Pix.Image.LoadFromMemory(bytes);

            using var page = engine.Process(
                image: image,
                pageSegMode: TesseractOCR.Enums.PageSegMode.SingleLine);

            var result = page.Text?
                .Trim()
                .Replace(
                    oldValue: "\r",
                    newValue: string.Empty)
                .Replace(
                    oldValue: "\n",
                    newValue: string.Empty);

            return result;
        }

        #endregion Public Methods
    }
}