﻿using System.Collections.Generic;
using System.Linq;
using NzbDrone.Common;

namespace NzbDrone.Core.Profiles.Delay
{
    public interface IDelayProfileService
    {
        DelayProfile Add(DelayProfile profile);
        DelayProfile Update(DelayProfile profile);
        void Delete(int id);
        List<DelayProfile> All();
        DelayProfile Get(int id);
        List<DelayProfile> AllForTags(HashSet<int> tagIds);
    }

    public class DelayProfileService : IDelayProfileService
    {
        private readonly IDelayProfileRepository _repo;

        public DelayProfileService(IDelayProfileRepository repo)
        {
            _repo = repo;
        }

        public DelayProfile Add(DelayProfile profile)
        {
            return _repo.Insert(profile);
        }

        public DelayProfile Update(DelayProfile profile)
        {
            return _repo.Update(profile);
        }

        public void Delete(int id)
        {
            _repo.Delete(id);
        }

        public List<DelayProfile> All()
        {
            return _repo.All().ToList();
        }

        public DelayProfile Get(int id)
        {
            return _repo.Get(id);
        }

        public List<DelayProfile> AllForTags(HashSet<int> tagIds)
        {
            return _repo.All().Where(r => r.Tags.Intersect(tagIds).Any() || r.Tags.Empty()).ToList();
        }
    }
}
