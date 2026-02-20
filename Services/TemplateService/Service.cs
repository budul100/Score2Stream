using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using MsBox.Avalonia.Enums;
using OpenCvSharp;
using Prism.Events;
using Prism.Ioc;
using Score2Stream.Commons.Assets;
using Score2Stream.Commons.Events.Template;
using Score2Stream.Commons.Exceptions;
using Score2Stream.Commons.Extensions;
using Score2Stream.Commons.Interfaces;
using Score2Stream.Commons.Models.Contents;

namespace Score2Stream.TemplateService
{
    public class Service(IDialogService dialogService, IContainerProvider containerProvider,
        IEventAggregator eventAggregator)
        : ITemplateService
    {
        #region Private Fields

        private readonly TemplatesChangedEvent templatesChangedEvent = eventAggregator.GetEvent<TemplatesChangedEvent>();
        private readonly TemplateSelectedEvent templateSelectedEvent = eventAggregator.GetEvent<TemplateSelectedEvent>();

        #endregion Private Fields

        #region Public Properties

        public ISampleService SampleService => Template?.SampleService;

        public Template Template { get; private set; }

        public List<Template> Templates { get; } = [];

        #endregion Public Properties

        #region Public Methods

        public void Add(Template template)
        {
            if (template != default)
            {
                if (Templates.Count >= Constants.MaxCountTemplates)
                {
                    throw new MaxCountExceededException(
                        type: typeof(Template),
                        maxCount: Constants.MaxCountTemplates);
                }

                if (template.SampleService == default)
                {
                    template.SampleService = containerProvider
                        .Resolve<ISampleService>();

                    template.SampleService.Initialize(
                        template: template);
                }

                if (template.Samples?.Count > 0)
                {
                    var samples = template.Samples
                        .Where(s => s.Image != default)
                        .OrderBy(s => s.Index).ToList();

                    try
                    {
                        foreach (var sample in samples)
                        {
                            sample.Mat = Mat.FromImageData(
                                imageBytes: sample.Image,
                                mode: ImreadModes.Unchanged);

                            sample.Template = template;

                            template.SampleService.Add(sample);
                        }
                    }
                    catch (MaxCountExceededException)
                    { }

                    template.SampleService.Order();
                }

                Templates.Add(template);
            }
        }

        public void Create()
        {
            try
            {
                var template = GetTemplate();

                Add(template);

                templatesChangedEvent.Publish();

                Select(template);
            }
            catch (MaxCountExceededException exception)
            {
                dialogService.ShowMessageBoxAsync(
                    contentMessage: exception.Message,
                    contentTitle: "Maximum count exceeded",
                    icon: Icon.Error);
            }
        }

        public async Task RemoveAsync()
        {
            if (Template != default)
            {
                var result = await dialogService.GetMessageBoxResultAsync(
                    contentMessage: "Shall the selected template be removed?",
                    contentTitle: "Remove template");

                if (result == ButtonResult.Yes)
                {
                    var next = Templates.GetNext(Template);

                    Template.SampleService.Clear();
                    Templates.Remove(Template);

                    if (Templates.Count > 0)
                    {
                        templatesChangedEvent.Publish();

                        Select(next);
                    }
                    else
                    {
                        try
                        {
                            Create();
                        }
                        catch (MaxCountExceededException)
                        { }
                    }
                }
            }
        }

        public void Select(Template template)
        {
            if (template != Template || template == default)
            {
                Template = template
                    ?? Templates.FirstOrDefault();

                templateSelectedEvent.Publish(Template);
            }
        }

        #endregion Public Methods

        #region Private Methods

        private Template GetTemplate()
        {
            var name = Templates.GetNextName();

            var result = new Template()
            {
                Name = name,
            };

            return result;
        }

        #endregion Private Methods
    }
}