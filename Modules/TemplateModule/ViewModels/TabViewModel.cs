using Score2Stream.Commons.Models.Contents;
using System.Windows.Input;

namespace Score2Stream.TemplateModule.ViewModels
{
    public record TabViewModel(
        Template Template,
        string Name,
        TemplateViewModel Content,
        ICommand CloseCommand);
}