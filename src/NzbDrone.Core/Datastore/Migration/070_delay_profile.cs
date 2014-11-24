using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using FluentMigrator;
using NzbDrone.Common.Serializer;
using NzbDrone.Core.Datastore.Migration.Framework;

namespace NzbDrone.Core.Datastore.Migration
{
    [Migration(70)]
    public class delay_profile : NzbDroneMigrationBase
    {
        protected override void MainDbUpgrade()
        {
            Create.TableForModel("DelayProfiles")
                  .WithColumn("PreferredProtocol").AsInt32()
                  .WithColumn("UsenetDelay").AsInt32()
                  .WithColumn("TorrentDelay").AsInt32()
                  .WithColumn("UsenetDelayMode").AsInt32()
                  .WithColumn("TorrentDelayMode").AsInt32()
                  .WithColumn("Order").AsInt32()
                  .WithColumn("Tags").AsString().NotNullable();

            Execute.Sql("INSERT INTO DelayProfiles VALUES (1, 1, 0, 0, 0, 0, 2147483647, '[]')");
            Execute.WithConnection(ConvertProfile);

            Delete.Column("GrabDelay").FromTable("Profiles");
            Delete.Column("GrabDelayMode").FromTable("Profiles");
        }

        private void ConvertProfile(IDbConnection conn, IDbTransaction tran)
        {
            var profiles = GetProfiles(conn, tran);
            var order = 1;

            foreach (var profileClosure in profiles.GroupBy(p => new { p.GrabDelay, p.GrabDelayMode })
                                            .Select(p => new { p.Key.GrabDelay, p.Key.GrabDelayMode }))
            {
                var profile = profileClosure;
                if (profile.GrabDelay == 0) continue;

                var tag = String.Format("delay-{0}-{1}", profile.GrabDelay, GetGrabDelayMode(profile.GrabDelayMode));
                var tagId = InsertTag(conn, tran, tag);
                var tags = String.Format("[{0}]", tagId);

                using (IDbCommand insertDelayProfileCmd = conn.CreateCommand())
                {
                    insertDelayProfileCmd.Transaction = tran;
                    insertDelayProfileCmd.CommandText = "INSERT INTO DelayProfiles (PreferredProtocol, TorrentDelay, TorrentDelayMode, UsenetDelay, UsenetDelayMode, [Order], Tags) VALUES (1, 0, 0, ?, ?, ?, ?)";
                    insertDelayProfileCmd.AddParameter(profile.GrabDelay);
                    insertDelayProfileCmd.AddParameter(profile.GrabDelayMode);
                    insertDelayProfileCmd.AddParameter(order);
                    insertDelayProfileCmd.AddParameter(tags);

                    insertDelayProfileCmd.ExecuteNonQuery();
                }

                var matchingProfileIds = profiles.Where(p => p.GrabDelay == profile.GrabDelay &&
                                                             p.GrabDelayMode == profile.GrabDelayMode)
                                                 .Select(p => p.Id);

                UpdateSeries(conn, tran, matchingProfileIds, tagId);

                order++;
            }
        }

        private List<Profile70> GetProfiles(IDbConnection conn, IDbTransaction tran)
        {
            var profiles = new List<Profile70>();

            using (IDbCommand getProfilesCmd = conn.CreateCommand())
            {
                getProfilesCmd.Transaction = tran;
                getProfilesCmd.CommandText = @"SELECT Id, GrabDelay, GrabDelayMode FROM Profiles";
                
                using (IDataReader profileReader = getProfilesCmd.ExecuteReader())
                {
                    while (profileReader.Read())
                    {
                        var id = profileReader.GetInt32(0);
                        var delay = profileReader.GetInt32(1);
                        var delayMode = profileReader.GetInt32(2);

                        profiles.Add(new Profile70
                        {
                            Id = id,
                            GrabDelay = delay * 60,
                            GrabDelayMode = delayMode
                        });
                    }
                }
            }

            return profiles;
        }

        private Int32 InsertTag(IDbConnection conn, IDbTransaction tran, string tagLabel)
        {
            using (IDbCommand insertCmd = conn.CreateCommand())
            {
                insertCmd.Transaction = tran;
                insertCmd.CommandText = @"INSERT INTO Tags (Label) VALUES (?); SELECT last_insert_rowid()";
                insertCmd.AddParameter(tagLabel);

                var id = insertCmd.ExecuteScalar();

                return Convert.ToInt32(id);
            }
        }

        private void UpdateSeries(IDbConnection conn, IDbTransaction tran, IEnumerable<int> profileIds, int tagId)
        {
            using (IDbCommand getSeriesCmd = conn.CreateCommand())
            {
                getSeriesCmd.Transaction = tran;
                getSeriesCmd.CommandText = "SELECT Id, Tags FROM Series WHERE ProfileId IN (?)";
                getSeriesCmd.AddParameter(String.Join(",", profileIds));

                using (IDataReader seriesReader = getSeriesCmd.ExecuteReader())
                {
                    while (seriesReader.Read())
                    {
                        var id = seriesReader.GetInt32(0);
                        var tagString = seriesReader.GetString(1);

                        var tags = Json.Deserialize<List<int>>(tagString);
                        tags.Add(tagId);

                        using (IDbCommand updateSeriesCmd = conn.CreateCommand())
                        {
                            updateSeriesCmd.Transaction = tran;
                            updateSeriesCmd.CommandText = "UPDATE Series SET Tags = ? WHERE Id = ?";
                            updateSeriesCmd.AddParameter(tags.ToJson());
                            updateSeriesCmd.AddParameter(id);

                            updateSeriesCmd.ExecuteNonQuery();
                        }
                    }
                }

                getSeriesCmd.ExecuteNonQuery();
            }
        }

        private string GetGrabDelayMode(int mode)
        {
            switch (mode)
            {
                case 1:
                    return "cutoff";
                case 2:
                    return "always";
                default:
                    return "first";
            }
        }

        private class Profile70
        {
            public int Id { get; set; }
            public int GrabDelay { get; set; }
            public int GrabDelayMode { get; set; }
        }

        private class Series70
        {
            public int Id { get; set; }
            public HashSet<int> Tags { get; set; }
        }
    }
}
