using MessageBox.Avalonia.DTO;
using MessageBox.Avalonia.Enums;
using System.Threading.Tasks;

namespace Score2Stream.Core.Interfaces
{
    public interface IMessageBoxService
    {
        #region Public Methods

        public Task<ButtonResult> GetMessageBoxResultAsync(MessageBoxStandardParams messageBoxParams);

        #endregion Public Methods
    }
}