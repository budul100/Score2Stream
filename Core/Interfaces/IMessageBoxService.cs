using Avalonia.Controls;
using MessageBox.Avalonia.Enums;
using System.Threading.Tasks;

namespace Score2Stream.Core.Interfaces
{
    public interface IMessageBoxService
    {
        #region Public Methods

        Task<ButtonResult> GetMessageBoxResultAsync(string contentMessage, string contentTitle,
            ButtonEnum buttonDefinitions = ButtonEnum.YesNo, ClickEnum enterDefaultButton = ClickEnum.Yes,
            ClickEnum escDefaultButton = ClickEnum.No, Icon icon = Icon.Question, bool ShowInCenter = true,
            WindowStartupLocation windowStartupLocation = WindowStartupLocation.CenterOwner);

        #endregion Public Methods
    }
}