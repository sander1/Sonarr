using System.Linq;
using NLog;
using NzbDrone.Core.Download.Pending;
using NzbDrone.Core.Indexers;
using NzbDrone.Core.IndexerSearch.Definitions;
using NzbDrone.Core.Parser.Model;
using NzbDrone.Core.Profiles.Delay;
using NzbDrone.Core.Qualities;

namespace NzbDrone.Core.DecisionEngine.Specifications.RssSync
{
    public class DelaySpecification : IDecisionEngineSpecification
    {
        private readonly IPendingReleaseService _pendingReleaseService;
        private readonly IQualityUpgradableSpecification _qualityUpgradableSpecification;
        private readonly IDelayProfileService _delayProfileService;
        private readonly Logger _logger;

        public DelaySpecification(IPendingReleaseService pendingReleaseService,
                                  IQualityUpgradableSpecification qualityUpgradableSpecification,
                                  IDelayProfileService delayProfileService,
                                  Logger logger)
        {
            _pendingReleaseService = pendingReleaseService;
            _qualityUpgradableSpecification = qualityUpgradableSpecification;
            _delayProfileService = delayProfileService;
            _logger = logger;
        }

        public RejectionType Type { get { return RejectionType.Temporary; } }

        public virtual Decision IsSatisfiedBy(RemoteEpisode subject, SearchCriteriaBase searchCriteria)
        {
            //How do we want to handle drone being off and the automatic search being triggered?
            //TODO: Add a flag to the search to state it is a "scheduled" search

            if (searchCriteria != null)
            {
                _logger.Debug("Ignore delay for searches");
                return Decision.Accept();
            }

            var profile = subject.Series.Profile.Value;
            var delayProfiles = _delayProfileService.AllForTags(subject.Series.Tags);
            var delayProfile = delayProfiles.OrderBy(d => d.Order).First();
            var delay = subject.Release.DownloadProtocol == DownloadProtocol.Torrent ? delayProfile.TorrentDelay : delayProfile.UsenetDelay;
            var delayMode = subject.Release.DownloadProtocol == DownloadProtocol.Torrent ? delayProfile.TorrentDelayMode : delayProfile.UsenetDelayMode;

            if (delay == 0)
            {
                _logger.Debug("Profile does not delay before download");
                return Decision.Accept();
            }

            var comparer = new QualityModelComparer(profile);

            foreach (var file in subject.Episodes.Where(c => c.EpisodeFileId != 0).Select(c => c.EpisodeFile.Value))
            {
                var upgradable = _qualityUpgradableSpecification.IsUpgradable(profile, file.Quality, subject.ParsedEpisodeInfo.Quality);

                if (upgradable)
                {
                    var revisionUpgrade = _qualityUpgradableSpecification.IsRevisionUpgrade(file.Quality, subject.ParsedEpisodeInfo.Quality);

                    if (revisionUpgrade)
                    {
                        _logger.Debug("New quality is a better revision for existing quality, skipping delay");
                        return Decision.Accept();
                    }
                }
            }

            //If quality meets or exceeds the best allowed quality in the profile accept it immediately
            var bestQualityInProfile = new QualityModel(profile.Items.Last(q => q.Allowed).Quality);
            var bestCompare = comparer.Compare(subject.ParsedEpisodeInfo.Quality, bestQualityInProfile);

            if (bestCompare >= 0)
            {
                _logger.Debug("Quality is highest in profile, will not delay");
                return Decision.Accept();
            }

            if (delayMode == GrabDelayMode.Cutoff)
            {
                var cutoff = new QualityModel(profile.Cutoff);
                var cutoffCompare = comparer.Compare(subject.ParsedEpisodeInfo.Quality, cutoff);

                if (cutoffCompare >= 0)
                {
                    _logger.Debug("Quality meets or exceeds the cutoff, will not delay");
                    return Decision.Accept();
                }
            }

            if (delayMode == GrabDelayMode.First)
            {
                var episodeIds = subject.Episodes.Select(e => e.Id);

                var oldest = _pendingReleaseService.GetPendingRemoteEpisodes(subject.Series.Id)
                                                            .Where(r => r.Episodes.Select(e => e.Id).Intersect(episodeIds).Any())
                                                            .OrderByDescending(p => p.Release.AgeHours)
                                                            .FirstOrDefault();

                if (oldest != null && oldest.Release.AgeHours > delay)
                {
                    return Decision.Accept();
                }
            }

            if (subject.Release.AgeHours < delay)
            {
                _logger.Debug("Age ({0}) is less than delay {1}, delaying", subject.Release.AgeHours, delay);
                return Decision.Reject("Waiting for better quality release");
            }

            return Decision.Accept();
        }
    }
}
