using NzbDrone.Core.Datastore;

namespace NzbDrone.Core.Organizer
{
    public class NamingConfig : ModelBase
    {
        public static NamingConfig Default
        {
            get
            {
                return new NamingConfig
                    {
                        RenameEpisodes = false,
                        MultiEpisodeStyle = 0,
                        StandardEpisodeFormat = "{Series Title} - S{season:00}E{episode:00} - {Episode Title} {Quality Title} {Quality Proper}",
                        DailyEpisodeFormat = "{Series Title} - {Air-Date} - {Episode Title} {Quality Title} {Quality Proper}",
                        AnimeEpisodeFormat = "{Series Title} - S{season:00}E{episode:00} - {Episode Title} {Quality Title} {Quality Proper}",
                        SeriesFolderFormat = "{Series Title}",
                        SeasonFolderFormat = "Season {season}"
                    };
            }
        }

        public bool RenameEpisodes { get; set; }
        public int MultiEpisodeStyle { get; set; }
        public string StandardEpisodeFormat { get; set; }
        public string DailyEpisodeFormat { get; set; }
        public string AnimeEpisodeFormat { get; set; }
        public string SeriesFolderFormat { get; set; }
        public string SeasonFolderFormat { get; set; }
    }
}