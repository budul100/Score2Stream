using Prism.Commands;
using ScoreboardOCR.Core.Interfaces;
using ScoreboardOCR.Core.Models;
using ScoreboardOCR.Core.Mvvm;

namespace MenuModule.ViewModels
{
    public class TemplateViewModel
        : ValidatableViewModelBase
    {
        #region Private Fields

        private readonly ITemplateService templateService;

        private bool isActive;

        #endregion Private Fields

        #region Public Constructors

        public TemplateViewModel(ITemplateService templateService, Clip clip, bool isActive)
        {
            this.templateService = templateService;

            Clip = clip;
            IsActive = isActive;

            OnClickCommand = new DelegateCommand(OnClick);
        }

        #endregion Public Constructors

        #region Public Properties

        public Clip Clip { get; }

        public bool IsActive
        {
            get { return isActive; }
            set { SetProperty(ref isActive, value); }
        }

        public string Name => Clip.Name;

        public DelegateCommand OnClickCommand { get; }

        #endregion Public Properties

        #region Private Methods

        private void OnClick()
        {
            templateService.Activate(Clip);
        }

        #endregion Private Methods
    }
}