using System;
using System.Collections.Generic;
using NzbDrone.Api.REST;
using NzbDrone.Core.Indexers;
using NzbDrone.Core.Profiles.Delay;

namespace NzbDrone.Api.Profiles.Delay
{
    public class DelayProfileResource : RestResource
    {
        public DownloadProtocol PreferredProtocol { get; set; }
        public int UsenetDelay { get; set; }
        public int TorrentDelay { get; set; }
        public GrabDelayMode UsenetDelayMode { get; set; }
        public GrabDelayMode TorrentDelayMode { get; set; }
        public Int32 Order { get; set; }
        public HashSet<int> Tags { get; set; }
    }
}
