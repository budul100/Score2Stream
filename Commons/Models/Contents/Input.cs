﻿using Score2Stream.Commons.Interfaces;
using System;
using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace Score2Stream.Commons.Models.Contents
{
    public class Input
    {
        #region Public Constructors

        public Input(bool isDevice)
        {
            IsDevice = isDevice;
        }

        #endregion Public Constructors

        #region Public Properties

        public List<Clip> Clips { get; set; }

        [JsonIgnore]
        public IClipService ClipService => VideoService?.ClipService;

        public int? DeviceId { get; set; }

        public string FileName { get; set; }

        [JsonIgnore]
        public Guid Guid { get; set; }

        [JsonIgnore]
        public bool IsActive => VideoService?.IsActive ?? false;

        public bool IsDevice { get; }

        [JsonIgnore]
        public bool IsEnded => VideoService?.IsEnded ?? false;

        public string Name { get; set; }

        public List<Template> Templates { get; set; }

        [JsonIgnore]
        public ITemplateService TemplateService => ClipService?.TemplateService;

        [JsonIgnore]
        public IVideoService VideoService { get; set; }

        #endregion Public Properties
    }
}