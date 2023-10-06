using Avalonia.Controls;
using Avalonia.Controls.ApplicationLifetimes;
using MessageBox.Avalonia;
using MessageBox.Avalonia.DTO;
using MessageBox.Avalonia.Enums;
using Score2Stream.Core.Interfaces;
using System;
using System.Threading.Tasks;

namespace Score2Stream.MessageBoxService
{
    public class Service
        : IMessageBoxService
    {
        #region Private Fields

        private readonly IClassicDesktopStyleApplicationLifetime desktop;

        #endregion Private Fields

        #region Public Constructors

        public Service(Avalonia.Application application)
        {
            if (application is null)
            {
                throw new ArgumentNullException(nameof(application));
            }

            desktop = application?.ApplicationLifetime as IClassicDesktopStyleApplicationLifetime;
        }

        #endregion Public Constructors

        #region Public Methods

        public async Task<ButtonResult> GetMessageBoxResultAsync(string contentMessage, string contentTitle,
            ButtonEnum buttonDefinitions = ButtonEnum.YesNo, ClickEnum enterDefaultButton = ClickEnum.Yes,
            ClickEnum escDefaultButton = ClickEnum.No, Icon icon = Icon.Question, bool showInCenter = true,
            WindowStartupLocation windowStartupLocation = WindowStartupLocation.CenterOwner)
        {
            var messageBoxParams = new MessageBoxStandardParams
            {
                ButtonDefinitions = buttonDefinitions,
                ContentMessage = contentMessage,
                ContentTitle = contentTitle,
                EnterDefaultButton = enterDefaultButton,
                EscDefaultButton = escDefaultButton,
                Icon = icon,
                ShowInCenter = showInCenter,
                WindowStartupLocation = windowStartupLocation,
            };

            var dialog = MessageBoxManager.GetMessageBoxStandardWindow(messageBoxParams);

            var result = await dialog.ShowDialog(desktop.MainWindow);

            return result;
        }

        public async Task ShowMessageBoxAsync(string contentMessage, string contentTitle, Icon icon = Icon.Question,
            bool showInCenter = true, WindowStartupLocation windowStartupLocation = WindowStartupLocation.CenterOwner)
        {
            var messageBoxParams = new MessageBoxStandardParams
            {
                ButtonDefinitions = ButtonEnum.Ok,
                ContentMessage = contentMessage,
                ContentTitle = contentTitle,
                EnterDefaultButton = ClickEnum.Ok,
                EscDefaultButton = ClickEnum.Ok,
                Icon = icon,
                ShowInCenter = showInCenter,
                WindowStartupLocation = windowStartupLocation,
            };

            var dialog = MessageBoxManager.GetMessageBoxStandardWindow(messageBoxParams);

            await dialog.ShowDialog(desktop.MainWindow);
        }

        #endregion Public Methods
    }
}