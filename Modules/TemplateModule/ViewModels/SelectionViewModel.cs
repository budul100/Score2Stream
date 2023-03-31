using Prism.Regions;
using ScoreboardOCR.Core.Interfaces;
using ScoreboardOCR.Core.Models;
using ScoreboardOCR.Core.Mvvm;
using System.Windows.Media.Imaging;

namespace TemplateModule.ViewModels
{
    public class SelectionViewModel
        : RegionViewModelBase
    {
        #region Private Fields

        private readonly ITemplateService templateService;

        private BitmapSource content;

        private Template current;
        private string name;

        #endregion Private Fields

        #region Public Constructors

        public SelectionViewModel(IWebcamService webcamService, IClipService clipService, ITemplateService templateService,
            IRegionManager regionManager)
            : base(regionManager)
        {
            this.templateService = templateService;

            clipService.OnClipsUpdatedEvent += OnClipsUpdated;
            templateService.OnTemplatesUpdatedEvent += OnTemplatesUpdated;
            webcamService.OnContentUpdatedEvent += OnContentUpdated;

            UpdateTemplate();
        }

        #endregion Public Constructors

        #region Public Properties

        public BitmapSource Content
        {
            get { return content; }
            set { SetProperty(ref content, value); }
        }

        public string Name
        {
            get { return name; }
            set { SetProperty(ref name, value); }
        }

        #endregion Public Properties

        #region Private Methods

        private void OnClipsUpdated(object sender, System.EventArgs e)
        {
            UpdateTemplate();
        }

        private void OnContentUpdated(object sender, System.EventArgs e)
        {
            UpdateTemplate();
        }

        private void OnTemplatesUpdated(object sender, System.EventArgs e)
        {
            UpdateTemplate();
        }

        private void UpdateTemplate()
        {
            current = templateService.Selection;

            Content = current?.Clip?.Content;
            Name = current?.Clip?.Name;
        }

        #endregion Private Methods
    }
}