using System;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Threading;

namespace TemplateModule.Views
{
    public partial class SampleView
        : UserControl
    {
        #region Private Fields

        private const string TextBoxName = "ValueTextBox";

        #endregion Private Fields

        #region Public Constructors

        public SampleView()
        {
            InitializeComponent();
        }

        #endregion Public Constructors

        #region Public Methods

        public void OnClick(object sender, MouseButtonEventArgs e)
        {
            var textBox = (sender as StackPanel)?.FindName(TextBoxName) as TextBox;

            if (textBox != default)
            {
                Dispatcher.BeginInvoke(DispatcherPriority.Input,
                    new Action(() =>
                    {
                        textBox.Focus();
                        Keyboard.Focus(textBox);
                    }));
            }
        }

        #endregion Public Methods
    }
}