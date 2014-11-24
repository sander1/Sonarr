using System.Collections.Generic;
using NzbDrone.Core.Datastore;
using NzbDrone.Core.Indexers;

namespace NzbDrone.Core.Profiles.Delay
{
    public class DelayProfile : ModelBase
    {
        public DownloadProtocol PreferredProtocol { get; set; }
        public int UsenetDelay { get; set; }
        public int TorrentDelay { get; set; }
        public GrabDelayMode UsenetDelayMode { get; set; }
        public GrabDelayMode TorrentDelayMode { get; set; }
        public int Order { get; set; }
        public HashSet<int> Tags { get; set; }

        public DelayProfile()
        {
            Tags = new HashSet<int>();
        }
    }
}
