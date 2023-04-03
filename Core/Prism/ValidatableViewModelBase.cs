using MvvmValidation;
using Prism.Mvvm;
using System;
using System.Collections;
using System.ComponentModel;
using System.Threading.Tasks;

namespace Core.Prism
{
    public class ValidatableViewModelBase
        : BindableBase, IValidatable, INotifyDataErrorInfo
    {
        #region Public Constructors

        public ValidatableViewModelBase()
        {
            Validator = new ValidationHelper();

            NotifyDataErrorInfoAdapter = new NotifyDataErrorInfoAdapter(Validator);
        }

        #endregion Public Constructors

        #region Public Events

        public event EventHandler<DataErrorsChangedEventArgs> ErrorsChanged
        {
            add { NotifyDataErrorInfoAdapter.ErrorsChanged += value; }
            remove { NotifyDataErrorInfoAdapter.ErrorsChanged -= value; }
        }

        #endregion Public Events

        #region Public Properties

        public bool HasErrors
        {
            get { return NotifyDataErrorInfoAdapter.HasErrors; }
        }

        #endregion Public Properties

        #region Protected Properties

        protected ValidationHelper Validator { get; private set; }

        #endregion Protected Properties

        #region Private Properties

        private NotifyDataErrorInfoAdapter NotifyDataErrorInfoAdapter { get; set; }

        #endregion Private Properties

        #region Public Methods

        public IEnumerable GetErrors(string propertyName)
        {
            return NotifyDataErrorInfoAdapter.GetErrors(propertyName);
        }

        Task<ValidationResult> IValidatable.Validate()
        {
            return Validator.ValidateAllAsync();
        }

        #endregion Public Methods
    }
}