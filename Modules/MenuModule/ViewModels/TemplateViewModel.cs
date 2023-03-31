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

        private readonly Clip clip;

        #endregion Private Fields

        #region Public Constructors

        public TemplateViewModel(ITemplateService templateService, Clip clip)
        {
            OnClickCommand = new DelegateCommand(() => templateService.Select(clip));
            this.clip = clip;
        }

        #endregion Public Constructors

        #region Public Properties

        public string Name => clip.Name;

        public DelegateCommand OnClickCommand { get; }

        #endregion Public Properties

        #region Public Methods

        public void Update()
        {
            RaisePropertyChanged(nameof(Name));
        }

        #endregion Public Methods
    }
}