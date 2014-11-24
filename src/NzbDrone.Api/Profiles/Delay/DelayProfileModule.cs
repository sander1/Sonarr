﻿using System;
using System.Collections.Generic;
using FluentValidation;
using NzbDrone.Api.Mapping;
using NzbDrone.Api.Validation;
using NzbDrone.Core.Profiles.Delay;

namespace NzbDrone.Api.Profiles.Delay
{
    public class DelayProfileModule : NzbDroneRestModule<DelayProfileResource>
    {
        private readonly IDelayProfileService _delayProfileService;

        public DelayProfileModule(IDelayProfileService delayProfileService)
        {
            _delayProfileService = delayProfileService;

            GetResourceAll = GetAll;
            GetResourceById = GetById;
            UpdateResource = Update;
            CreateResource = Create;
            DeleteResource = DeleteProfile;

            SharedValidator.RuleFor(d => d.Tags).NotEmpty().When(d => d.Id != 1);
            SharedValidator.RuleFor(d => d.Tags).EmptyCollection().When(d => d.Id == 1);

        }

        private int Create(DelayProfileResource resource)
        {
            var model = resource.InjectTo<DelayProfile>();
            model = _delayProfileService.Add(model);

            return model.Id;
        }

        private void DeleteProfile(int id)
        {
            if (id == 1)
            {
                throw new InvalidOperationException();
            }

            _delayProfileService.Delete(id);
        }

        private void Update(DelayProfileResource resource)
        {
            GetNewId<DelayProfile>(_delayProfileService.Update, resource);
        }

        private DelayProfileResource GetById(int id)
        {
            return _delayProfileService.Get(id).InjectTo<DelayProfileResource>();
        }

        private List<DelayProfileResource> GetAll()
        {
            return _delayProfileService.All().InjectTo<List<DelayProfileResource>>();
        }
    }
}