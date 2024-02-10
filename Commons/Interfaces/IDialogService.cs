using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Avalonia.Controls;
using Avalonia.Platform.Storage;
using MsBox.Avalonia.Enums;

namespace Score2Stream.Commons.Interfaces
{
    public interface IDialogService
    {
        #region Public Methods

        Task<IStorageFolder> GetFolderAsync(string path = default,
            Environment.SpecialFolder? alternative = Environment.SpecialFolder.UserProfile);

        Task<IStorageFolder> GetFolderAsync(IEnumerable<string> paths = default,
            Environment.SpecialFolder? alternative = Environment.SpecialFolder.UserProfile);

        Task<ButtonResult> GetMessageBoxResultAsync(string contentMessage, string contentTitle,
            ButtonEnum buttonDefinitions = ButtonEnum.YesNo, ClickEnum enterDefaultButton = ClickEnum.Yes,
            ClickEnum escDefaultButton = ClickEnum.No, Icon icon = Icon.Question, bool showInCenter = true,
            WindowStartupLocation windowStartupLocation = WindowStartupLocation.CenterOwner);

        void Initialize(Window window, string iconUri = default);

        Task<IEnumerable<IStorageFile>> OpenFilePickerAsync(string title,
            IReadOnlyList<FilePickerFileType> fileTypeFilter = default, bool allowMultiple = false,
            IStorageFolder startLocation = default);

        Task<IEnumerable<IStorageFolder>> OpenFolderPickerAsync(string title, bool allowMultiple = false,
            IStorageFolder startLocation = default);

        Task<IStorageFile> SaveFilePickerAsync(string title, string defaultExtension = default,
            IReadOnlyList<FilePickerFileType> fileTypeChoices = default, bool showOverwritePrompt = default,
            string suggestedFileName = default, IStorageFolder suggestedStartLocation = default);

        Task ShowMessageBoxAsync(string contentMessage, string contentTitle, Icon icon = Icon.Question,
            bool showInCenter = true, WindowStartupLocation windowStartupLocation = WindowStartupLocation.CenterOwner);

        #endregion Public Methods
    }
}