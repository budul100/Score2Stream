using MessageBox.Avalonia.Enums;
using OpenCvSharp;
using Prism.Events;
using Prism.Ioc;
using Score2Stream.Core.Events.Template;
using Score2Stream.Core.Extensions;
using Score2Stream.Core.Interfaces;
using Score2Stream.Core.Models.Contents;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Score2Stream.TemplateService
{
    public class Service
        : ITemplateService
    {
        #region Private Fields

        private readonly IContainerProvider containerProvider;
        private readonly IEventAggregator eventAggregator;
        private readonly IMessageBoxService messageBoxService;

        #endregion Private Fields

        #region Public Constructors

        public Service(IMessageBoxService messageBoxService, IContainerProvider containerProvider,
            IEventAggregator eventAggregator)
        {
            this.messageBoxService = messageBoxService;
            this.containerProvider = containerProvider;
            this.eventAggregator = eventAggregator;
        }

        #endregion Public Constructors

        #region Public Properties

        public Template Active { get; private set; }

        public ISampleService SampleService => Active?.SampleService;

        public List<Template> Templates { get; } = new List<Template>();

        #endregion Public Properties

        #region Public Methods

        public void Add(Template template)
        {
            if (template != default)
            {
                AddTemplate(template);

                if (template.Samples?.Any() == true)
                {
                    template.Samples = template.Samples
                        .Where(s => s.Image != default).ToList();

                    foreach (var sample in template.Samples)
                    {
                        sample.Mat = Mat.FromImageData(
                            imageBytes: sample.Image,
                            mode: ImreadModes.Unchanged);
                        sample.Template = template;

                        template.SampleService.Add(sample);
                    }

                    template.SampleService.Order();
                }
            }
        }

        public void Create()
        {
            var template = GetTemplate();

            AddTemplate(template);

            eventAggregator
                .GetEvent<TemplatesChangedEvent>()
                .Publish();

            Select(template);
        }

        public async Task RemoveAsync()
        {
            if (Active != default)
            {
                var result = await messageBoxService.GetMessageBoxResultAsync(
                    contentMessage: "Shall the selected template be removed?",
                    contentTitle: "Remove template");

                if (result == ButtonResult.Yes)
                {
                    var next = Templates.GetNext(Active);

                    Active.SampleService.Clear();
                    Templates.Remove(Active);

                    if (Templates.Any())
                    {
                        eventAggregator.GetEvent<TemplatesChangedEvent>()
                            .Publish();

                        Select(next);
                    }
                    else
                    {
                        Create();
                    }
                }
            }
        }

        public void Select(Template template)
        {
            if (template != Active || template == default)
            {
                Active = template
                    ?? Templates.FirstOrDefault();

                eventAggregator
                    .GetEvent<TemplateSelectedEvent>()
                    .Publish(Active);
            }
        }

        #endregion Public Methods

        #region Private Methods

        private void AddTemplate(Template template)
        {
            if (template != default)
            {
                if (template.SampleService == default)
                {
                    template.SampleService = containerProvider
                        .Resolve<ISampleService>();

                    template.SampleService.Initialize(
                        template: template);
                }

                Templates.Add(template);
            }
        }

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