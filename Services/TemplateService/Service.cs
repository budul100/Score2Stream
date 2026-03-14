using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using MsBox.Avalonia.Enums;
using OpenCvSharp;
using Prism.Events;
using Prism.Ioc;
using Score2Stream.Commons.Assets;
using Score2Stream.Commons.Events.Sample;
using Score2Stream.Commons.Events.Template;
using Score2Stream.Commons.Exceptions;
using Score2Stream.Commons.Extensions;
using Score2Stream.Commons.Interfaces;
using Score2Stream.Commons.Models.Contents;
using Score2Stream.Commons.Models.Settings;

namespace Score2Stream.TemplateService
{
    public class Service
        : ITemplateService
    {
        #region Private Fields

        private readonly IContainerProvider containerProvider;
        private readonly IDialogService dialogService;
        private readonly ISettingsService<Session> settingsService;
        private readonly TemplatesChangedEvent templatesChangedEvent;
        private readonly TemplateSelectedEvent templateSelectedEvent;
        private bool isInitializing;

        #endregion Private Fields

        #region Public Constructors

        public Service(ISettingsService<Session> settingsService, IDialogService dialogService,
            IContainerProvider containerProvider, IEventAggregator eventAggregator)
        {
            this.settingsService = settingsService;
            this.dialogService = dialogService;
            this.containerProvider = containerProvider;

            templatesChangedEvent = eventAggregator.GetEvent<TemplatesChangedEvent>();
            templateSelectedEvent = eventAggregator.GetEvent<TemplateSelectedEvent>();

            eventAggregator.GetEvent<SamplesChangedEvent>().Subscribe(
                action: SaveTemplates,
                keepSubscriberReferenceAlive: true);
            eventAggregator.GetEvent<SamplesOrderedEvent>().Subscribe(
                action: SaveTemplates,
                keepSubscriberReferenceAlive: true);
            eventAggregator.GetEvent<SampleModifiedEvent>().Subscribe(
                action: _ => SaveTemplates(),
                keepSubscriberReferenceAlive: true);
        }

        #endregion Public Constructors

        #region Public Properties

        public Template Active { get; private set; }

        public ISampleService SampleService => Active?.SampleService;

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

        public void Initialize()
        {
            isInitializing = true;

            try
            {
                if (settingsService.Contents.Templates?.Count > 0)
                {
                    foreach (var template in settingsService.Contents.Templates)
                    {
                        Add(template);
                    }
                }
                else
                {
                    Create();
                }
            }
            catch (MaxCountExceededException)
            { }

            Select(Templates?.FirstOrDefault());

            isInitializing = false;
        }

        public async Task RemoveAsync()
        {
            if (Active != default)
            {
                var result = await dialogService.GetMessageBoxResultAsync(
                    contentMessage: "Shall the selected template be removed?",
                    contentTitle: "Remove template");

                if (result == ButtonResult.Yes)
                {
                    var next = Templates.GetNext(Active);

                    Active.SampleService.Clear();
                    Templates.Remove(Active);

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
            if (isInitializing) return;

            if (template != Active || template == default)
            {
                Active = template
                    ?? Templates.FirstOrDefault();

                templateSelectedEvent.Publish(Active);
            }

            SaveTemplates();
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

        private void SaveTemplates()
        {
            if (Templates?.Count > 0)
            {
                foreach (var template in Templates)
                {
                    var relevants = template.Samples?
                        .Where(s => s.Mat != default
                            && s.Image == null).ToArray();

                    if (relevants?.Length > 0)
                    {
                        foreach (var relevant in relevants)
                        {
                            relevant.Image = relevant.Mat.ToBytes();
                        }
                    }
                }
            }

            settingsService.Save();
        }

        #endregion Private Methods
    }
}