using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Threading.Tasks;
using MsBox.Avalonia.Enums;
using OpenCvSharp;
using Prism.Events;
using Prism.Ioc;
using Score2Stream.Commons.Assets;
using Score2Stream.Commons.Events.Menu;
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
        private readonly DetectionChangedEvent detectionChangedEvent;
        private readonly IDialogService dialogService;
        private readonly ISettingsService<Session> settingsService;
        private readonly TemplatesChangedEvent templatesChangedEvent;
        private readonly TemplateSelectedEvent templateSelectedEvent;
        private bool isInitializing;
        private ImmutableList<Template> templates = [];

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

            detectionChangedEvent = eventAggregator.GetEvent<DetectionChangedEvent>();

            eventAggregator.GetEvent<SamplesChangedEvent>().Subscribe(
                action: SaveSamples,
                keepSubscriberReferenceAlive: true);
            eventAggregator.GetEvent<SamplesOrderedEvent>().Subscribe(
                action: SaveSamples,
                keepSubscriberReferenceAlive: true);
            eventAggregator.GetEvent<SampleModifiedEvent>().Subscribe(
                action: _ => SaveSamples(),
                keepSubscriberReferenceAlive: true);
        }

        #endregion Public Constructors

        #region Public Properties

        public Template Active { get; private set; }

        public ISampleService SampleService => Active?.SampleService;

        public IReadOnlyList<Template> Templates => templates;

        #endregion Public Properties

        #region Public Methods

        public void Create()
        {
            try
            {
                var template = GetTemplate();

                InitializeTemplate(template);

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

            if (settingsService.Contents.Templates?.Count > 0)
            {
                try
                {
                    foreach (var template in settingsService.Contents.Templates)
                    {
                        InitializeTemplate(template);
                    }
                }
                catch (MaxCountExceededException)
                { }
            }

            if (Templates.Count > 0)
            {
                var relevant = Templates[0];

                Select(relevant);
            }

            isInitializing = false;
        }

        public async Task RemoveAsync(Template template = default)
        {
            template ??= Active;

            if (template != default)
            {
                var result = await dialogService.GetMessageBoxResultAsync(
                    contentMessage: $"Shall {template.Name} be removed?",
                    contentTitle: "Remove template");

                if (result == ButtonResult.Yes)
                {
                    if (template.SampleService != default)
                    {
                        template.SampleService.Clear();

                        template.SampleService = default;
                    }

                    RemoveTemplate(template);
                }
            }
        }

        public void Select(Template template)
        {
            if (template == Active) return;

            Active = template;

            if (SampleService?.IsDetection == true)
            {
                SampleService.IsDetection = false;

                detectionChangedEvent.Publish();
            }

            templateSelectedEvent.Publish(Active);
        }

        #endregion Public Methods

        #region Private Methods

        private static void InitializeSamples(Template template)
        {
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

                        template.SampleService.Add(sample);
                    }
                }
                catch (MaxCountExceededException)
                { }

                template.SampleService.Order();
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

        private void InitializeService(Template template)
        {
            if (template.SampleService == default)
            {
                template.SampleService = containerProvider
                    .Resolve<ISampleService>();

                template.SampleService.Initialize(
                    template: template);
            }
        }

        private void InitializeTemplate(Template template)
        {
            if (template.SampleService != default) return;

            if (Templates.Count >= Constants.MaxCountTemplates)
            {
                throw new MaxCountExceededException(
                    type: typeof(Template),
                    maxCount: Constants.MaxCountTemplates);
            }

            InitializeService(template);

            InitializeSamples(template);

            ImmutableList<Template> add(ImmutableList<Template> c) => !c.Contains(template)
                ? c.Add(template)
                : c;

            ImmutableInterlocked.Update(
                location: ref templates,
                transformer: add);

            SaveTemplates();

            templatesChangedEvent.Publish();
        }

        private void RemoveTemplate(Template template)
        {
            if (template == default) return;

            if (template == Active)
            {
                var next = Templates.Count > 1
                    ? Templates.GetNext(template)
                    : default;

                Select(next);
            }

            ImmutableList<Template> remove(ImmutableList<Template> c) => c.Contains(template)
                ? c.Remove(template)
                : c;

            ImmutableInterlocked.Update(
                location: ref templates,
                transformer: remove);

            SaveTemplates();

            templatesChangedEvent.Publish();
        }

        private void SaveSamples()
        {
            if (isInitializing || Active == default) return;

            var relevants = Active.Samples?
                .Where(s => s.Mat != default
                    && s.Image == default).ToArray();

            if (relevants?.Length > 0)
            {
                foreach (var relevant in relevants)
                {
                    relevant.Image = relevant.Mat.ToBytes();
                }
            }

            settingsService.Save();
        }

        private void SaveTemplates()
        {
            if (isInitializing) return;

            settingsService.Contents.Templates = Templates.ToList();
            settingsService.Save();
        }

        #endregion Private Methods
    }
}