using Score2Stream.Commons.Models.Contents;
using System.Windows.Input;

namespace Score2Stream.VideoModule.ViewModels
{
    public record TabViewModel(
        Input Input, 
        string Name, 
        InputViewModel Content, 
        ICommand CloseCommand);
}