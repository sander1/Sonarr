﻿using System;
using System.Data;
using System.Linq;
using System.Text.RegularExpressions;
using FluentMigrator;
using NzbDrone.Core.Datastore.Migration.Framework;

namespace NzbDrone.Core.Datastore.Migration
{
    [Migration(69)]
    public class quality_proper : NzbDroneMigrationBase
    {
        protected override void MainDbUpgrade()
        {
            Execute.WithConnection(ConvertQualityTitle);
        }

        private static readonly Regex QualityTitleRegex = new Regex(@"\{(?<prefix>[- ._\[(]*)(?<token>(?:quality)(?:(?<separator>[- ._]+)(?:title))?)(?<suffix>[- ._)\]]*)\}",
                                                             RegexOptions.Compiled | RegexOptions.IgnoreCase);

        private void ConvertQualityTitle(IDbConnection conn, IDbTransaction tran)
        {
            using (IDbCommand namingConfigCmd = conn.CreateCommand())
            {
                namingConfigCmd.Transaction = tran;
                namingConfigCmd.CommandText = @"SELECT StandardEpisodeFormat, DailyEpisodeFormat, AnimeEpisodeFormat FROM NamingConfig LIMIT 1";

                using (IDataReader configReader = namingConfigCmd.ExecuteReader())
                {
                    while (configReader.Read())
                    {
                        var currentStandard = configReader.GetString(0);
                        var currentDaily = configReader.GetString(1);
                        var currentAnime = configReader.GetString(1);

                        var newStandard = GetNewFormat(currentStandard);
                        var newDaily = GetNewFormat(currentDaily);
                        var newAnime = GetNewFormat(currentAnime);

                        using (IDbCommand updateCmd = conn.CreateCommand())
                        {
                            updateCmd.Transaction = tran;

                            updateCmd.CommandText = "UPDATE NamingConfig SET StandardEpisodeFormat = ?, DailyEpisodeFormat = ?, AnimeEpisodeFormat =?";
                            updateCmd.AddParameter(newStandard);
                            updateCmd.AddParameter(newDaily);
                            updateCmd.AddParameter(newAnime);

                            updateCmd.ExecuteNonQuery();
                        }
                    }
                }
            }
        }

        private string GetNewFormat(string currentFormat)
        {
            var matches = QualityTitleRegex.Matches(currentFormat);
            var result = currentFormat;

            foreach (Match match in matches)
            {
                var tokenMatch = GetTokenMatch(match);
                var qualityProperToken = String.Format("Quality{0}Proper", tokenMatch.Separator); ;

                if (tokenMatch.Token.All(t => !Char.IsLetter(t) || Char.IsLower(t)))
                {
                    qualityProperToken = String.Format("quality{0}proper", tokenMatch.Separator);
                }
                else if (tokenMatch.Token.All(t => !Char.IsLetter(t) || Char.IsUpper(t)))
                {
                    qualityProperToken = String.Format("QUALITY{0}PROPER", tokenMatch.Separator);
                }

                var qualityTokens = String.Format("{0}{1}{{{2}{3}{4}}}", match.Value, tokenMatch.Separator, tokenMatch.Prefix, qualityProperToken, tokenMatch.Suffix);
                result = result.Replace(match.Value, qualityTokens);
            }

            return result;
        }

        private TokenMatch69 GetTokenMatch(Match match)
        {
            return new TokenMatch69
                             {
                                 Prefix = match.Groups["prefix"].Value,
                                 Token = match.Groups["token"].Value,
                                 Separator = match.Groups["separator"].Value,
                                 Suffix = match.Groups["suffix"].Value,
                             };
        }

        private class TokenMatch69
        {
            public string Prefix { get; set; }
            public string Token { get; set; }
            public string Separator { get; set; }
            public string Suffix { get; set; }
        }
    }
}
