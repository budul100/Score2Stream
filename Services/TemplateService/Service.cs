﻿using System.Collections.Generic;
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
    public class Service
        : ITemplateService
    {
        #region Private Fields

        private readonly IContainerProvider containerProvider;
        private readonly IDialogService dialogService;
        private readonly TemplatesChangedEvent templatesChangedEvent;
        private readonly TemplateSelectedEvent templateSelectedEvent;

        #endregion Private Fields

        #region Public Constructors

        public Service(IDialogService dialogService, IContainerProvider containerProvider,
            IEventAggregator eventAggregator)
        {
            this.dialogService = dialogService;
            this.containerProvider = containerProvider;

            templatesChangedEvent = eventAggregator.GetEvent<TemplatesChangedEvent>();
            templateSelectedEvent = eventAggregator.GetEvent<TemplateSelectedEvent>();
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
            try
            {
                var template = GetTemplate();

                AddTemplate(template);

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

                    if (Templates.Any())
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
            if (template != Active || template == default)
            {
                Active = template
                    ?? Templates.FirstOrDefault();

                templateSelectedEvent.Publish(Active);
            }
        }

        #endregion Public Methods

        #region Private Methods

        private void AddTemplate(Template template)
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