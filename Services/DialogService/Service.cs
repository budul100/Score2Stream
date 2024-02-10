using Avalonia.Controls;
using Avalonia.Media.Imaging;
using Avalonia.Platform;
using Avalonia.Platform.Storage;
using MsBox.Avalonia;
using MsBox.Avalonia.Dto;
using MsBox.Avalonia.Enums;
using Score2Stream.Commons.Interfaces;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;

namespace Score2Stream.DialogService
{
    public class Service
        : IDialogService
    {
        #region Private Fields

        private TopLevel topLevel;
        private Window window;
        private WindowIcon windowIcon;

        #endregion Private Fields

        #region Public Methods

        public async Task<IStorageFolder> GetFolderAsync(IEnumerable<string> paths = default,
            Environment.SpecialFolder? alternative = Environment.SpecialFolder.UserProfile)
        {
            var result = default(IStorageFolder);

            if (topLevel != default)
            {
                var path = default(string);

                if (paths?.Any() == true)
                {
                    path = paths?.FirstOrDefault(p => Directory.Exists(p));

                    if (string.IsNullOrEmpty(path))
                    {
                        path = paths?
                            .Select(p => Path.GetDirectoryName(p))
                            .FirstOrDefault(p => Directory.Exists(p));
                    }
                }

                result = await GetFolderAsync(
                    path: path,
                    alternative: alternative);
            }

            return result;
        }

        public async Task<IStorageFolder> GetFolderAsync(string path = default,
            Environment.SpecialFolder? alternative = Environment.SpecialFolder.UserProfile)
        {
            var result = default(IStorageFolder);

            if (topLevel != default)
            {
                if (!Directory.Exists(path)
                    && File.Exists(path))
                {
                    path = Path.GetDirectoryName(path);
                }

                if (!Directory.Exists(path)
                    && alternative != default)
                {
                    path = Environment.GetFolderPath(alternative.Value);
                }

                if (Directory.Exists(path))
                {
                    result = await topLevel.StorageProvider.TryGetFolderFromPathAsync(path);
                }
            }

            return result;
        }

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
                WindowIcon = windowIcon,
            };

            var dialog = MessageBoxManager.GetMessageBoxStandard(messageBoxParams);

            var result = await dialog.ShowWindowDialogAsync(window);

            return result;
        }

        public void Initialize(Window window, string iconUri = default)
        {
            if (window is null)
            {
                throw new ArgumentNullException(nameof(window));
            }

            this.window = window;

            topLevel = Window.GetTopLevel(window);
            windowIcon = GetWindowIcon(iconUri);
        }

        public async Task<IEnumerable<IStorageFile>> OpenFilePickerAsync(string title,
            IReadOnlyList<FilePickerFileType> fileTypeFilter = default, bool allowMultiple = false,
            IStorageFolder suggestedStartLocation = default)
        {
            var result = default(IEnumerable<IStorageFile>);

            if (topLevel != default)
            {
                var options = new FilePickerOpenOptions
                {
                    AllowMultiple = allowMultiple,
                    FileTypeFilter = fileTypeFilter,
                    SuggestedStartLocation = suggestedStartLocation,
                    Title = title,
                };

                result = await topLevel?.StorageProvider.OpenFilePickerAsync(options);
            }

            return result;
        }

        public async Task<IEnumerable<IStorageFolder>> OpenFolderPickerAsync(string title, bool allowMultiple = false,
            IStorageFolder suggestedStartLocation = default)
        {
            var result = default(IEnumerable<IStorageFolder>);

            if (topLevel != default)
            {
                var options = new FolderPickerOpenOptions
                {
                    AllowMultiple = allowMultiple,
                    SuggestedStartLocation = suggestedStartLocation,
                    Title = title,
                };

                result = await topLevel?.StorageProvider.OpenFolderPickerAsync(options);
            }

            return result;
        }

        public async Task<IStorageFile> SaveFilePickerAsync(string title, string defaultExtension = default,
            IReadOnlyList<FilePickerFileType> fileTypeChoices = default, bool showOverwritePrompt = default,
            string suggestedFileName = default, IStorageFolder suggestedStartLocation = default)
        {
            var result = default(IStorageFile);

            if (topLevel != default)
            {
                var options = new FilePickerSaveOptions
                {
                    DefaultExtension = defaultExtension,
                    FileTypeChoices = fileTypeChoices,
                    ShowOverwritePrompt = showOverwritePrompt,
                    SuggestedFileName = suggestedFileName,
                    SuggestedStartLocation = suggestedStartLocation,
                    Title = title,
                };

                result = await topLevel?.StorageProvider.SaveFilePickerAsync(options);
            }

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
                WindowIcon = windowIcon,
            };

            var dialog = MessageBoxManager.GetMessageBoxStandard(messageBoxParams);

            await dialog.ShowWindowDialogAsync(window);
        }

        #endregion Public Methods

        #region Private Methods

        private static WindowIcon GetWindowIcon(string iconUri)
        {
            var result = default(WindowIcon);

            if (!string.IsNullOrWhiteSpace(iconUri))
            {
                var uri = new Uri(iconUri);

                if (AssetLoader.Exists(uri))
                {
                    var stream = AssetLoader.Open(uri);

                    if (stream != default)
                    {
                        var bitmap = new Bitmap(stream);
                        result = new WindowIcon(bitmap);
                    }
                }
            }

            return result;
        }

        #endregion Private Methods
    }
}