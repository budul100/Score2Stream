using Prism.Mvvm;
using Prism.Navigation;

namespace Core.Mvvm
{
    public abstract class ViewModelBase
        : BindableBase, IDestructible
    {
        #region Protected Constructors

        protected ViewModelBase()
        { }

        #endregion Protected Constructors

        #region Public Methods

        public virtual void Destroy()
        { }

        #endregion Public Methods
    }
}