using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Threading.Tasks;
using Avalonia.Media.Imaging;
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
    public class Service(ISettingsService<Session> settingsService, IDialogService dialogService,
        Func<ISampleService> sampleServiceGetter, IEventAggregator eventAggregator)
        : ITemplateService
    {
        #region Private Fields

        private readonly DetectionChangedEvent detectionChangedEvent = eventAggregator.GetEvent<DetectionChangedEvent>();
        private readonly SamplesChangedEvent samplesChangedEvent = eventAggregator.GetEvent<SamplesChangedEvent>();
        private readonly TemplatesChangedEvent templatesChangedEvent = eventAggregator.GetEvent<TemplatesChangedEvent>();
        private readonly TemplateSelectedEvent templateSelectedEvent = eventAggregator.GetEvent<TemplateSelectedEvent>();

        private bool isInitializing;
        private ImmutableList<Template> templates = [];

        #endregion Private Fields

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
                var template = CreateTemplate();

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
                        AddTemplate(template);
                    }
                }
                catch (MaxCountExceededException)
                { }

                templatesChangedEvent.Publish();
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

        private void AddTemplate(Template template)
        {
            if (Templates.Count >= Constants.MaxCountTemplates)
            {
                throw new MaxCountExceededException(
                    type: typeof(Template),
                    maxCount: Constants.MaxCountTemplates);
            }

            if (template.SampleService == default)
            {
                template.SampleService = sampleServiceGetter();

                template.SampleService.Initialize(
                    template: template);
            }

            InitializeSamples(template);

            ImmutableList<Template> add(ImmutableList<Template> c) => !c.Contains(template)
                ? c.Add(template)
                : c;

            ImmutableInterlocked.Update(
                location: ref templates,
                transformer: add);
        }

        private Template CreateTemplate()
        {
            var name = Templates.GetNextName();

            var result = new Template()
            {
                Name = name,
            };

            AddTemplate(result);

            SaveTemplates();

            templatesChangedEvent.Publish();

            return result;
        }

        private void InitializeSamples(Template template)
        {
            if (template.Samples?.Count > 0)
            {
                var samples = template.Samples
                    .Where(s => s.Bytes != default)
                    .OrderBy(s => s.Index).ToList();

                try
                {
                    foreach (var sample in samples)
                    {
                        sample.Image = Mat.FromImageData(
                            imageBytes: sample.Bytes,
                            mode: ImreadModes.Unchanged);
                        sample.Bitmap = new Bitmap(sample.Image.ToMemoryStream());

                        template.SampleService.Add(sample);
                    }
                }
                catch (MaxCountExceededException)
                { }

                template.SampleService.Order();

                samplesChangedEvent.Publish();
            }
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

        private void SaveTemplates()
        {
            if (isInitializing) return;

            settingsService.Contents.Templates = Templates?.ToList();

            settingsService.Save();
        }

        #endregion Private Methods
    }
}