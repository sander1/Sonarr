using System;
using System.IO;
using System.Linq;
using System.Collections.Generic;
using FizzWare.NBuilder;
using FluentAssertions;
using Moq;
using NUnit.Framework;
using NzbDrone.Common;
using NzbDrone.Common.Http;
using NzbDrone.Core.MediaFiles.TorrentInfo;
using NzbDrone.Core.Tv;
using NzbDrone.Core.Download;
using NzbDrone.Core.Download.Clients.UTorrent;
using NzbDrone.Core.Parser.Model;
using NzbDrone.Test.Common;
using NzbDrone.Core.Test.Framework;

namespace NzbDrone.Core.Test.Download.DownloadClientTests.UTorrentTests
{
    [TestFixture]
    public class UTorrentFixture : DownloadClientFixtureBase<UTorrent>
    {
        protected UTorrentTorrent _queued;
        protected UTorrentTorrent _downloading;
        protected UTorrentTorrent _failed;
        protected UTorrentTorrent _completed;

        [SetUp]
        public void Setup()
        {
            Subject.Definition = new DownloadClientDefinition();
            Subject.Definition.Settings = new UTorrentSettings
                                          {
                                              Host = "127.0.0.1",
                                              Port = 2222,
                                              Username = "admin",
                                              Password = "pass",
                                              TvCategory = "tv"
                                          };

            _queued = new UTorrentTorrent
                    {
                        Hash = "HASH",
                        Status = UTorrentTorrentStatus.Queued | UTorrentTorrentStatus.Loaded,
                        Name = _title,
                        Size = 1000,
                        Remaining = 1000,
                        Progress = 0,
                        Label = "tv",
                        DownloadUrl = _downloadUrl,
                        RootDownloadPath = "somepath"
                    };

            _downloading = new UTorrentTorrent
                    {
                        Hash = "HASH",
                        Status = UTorrentTorrentStatus.Started | UTorrentTorrentStatus.Loaded,
                        Name = _title,
                        Size = 1000,
                        Remaining = 100,
                        Progress = 0.9,
                        Label = "tv",
                        DownloadUrl = _downloadUrl,
                        RootDownloadPath = "somepath"
                    };

            _failed = new UTorrentTorrent
                    {
                        Hash = "HASH",
                        Status = UTorrentTorrentStatus.Error,
                        Name = _title,
                        Size = 1000,
                        Remaining = 100,
                        Progress = 0.9,
                        Label = "tv",
                        DownloadUrl = _downloadUrl,
                        RootDownloadPath = "somepath"
                    };

            _completed = new UTorrentTorrent
                    {
                        Hash = "HASH",
                        Status = UTorrentTorrentStatus.Checked | UTorrentTorrentStatus.Loaded,
                        Name = _title,
                        Size = 1000,
                        Remaining = 0,
                        Progress = 1.0,
                        Label = "tv",
                        DownloadUrl = _downloadUrl,
                        RootDownloadPath = "somepath"
                    };
            
            Mocker.GetMock<ITorrentFileInfoReader>()
                  .Setup(s => s.GetHashFromTorrentFile(It.IsAny<Byte[]>()))
                  .Returns("CBC2F069FE8BB2F544EAE707D75BCD3DE9DCF951");

            Mocker.GetMock<IHttpClient>()
                  .Setup(s => s.Get(It.IsAny<HttpRequest>()))
                  .Returns<HttpRequest>(r => new HttpResponse(r, new HttpHeader(), new Byte[0]));
        }

        protected void GivenRedirectToMagnet()
        {
            var httpHeader = new HttpHeader();
            httpHeader["Location"] = "magnet:?xt=urn:btih:ZPBPA2P6ROZPKRHK44D5OW6NHXU5Z6KR&tr=udp";

            Mocker.GetMock<IHttpClient>()
                  .Setup(s => s.Get(It.IsAny<HttpRequest>()))
                  .Returns<HttpRequest>(r => new HttpResponse(r, httpHeader, new Byte[0], System.Net.HttpStatusCode.SeeOther));
        }

        protected void GivenFailedDownload()
        {
            Mocker.GetMock<IUTorrentProxy>()
                .Setup(s => s.AddTorrentFromUrl(It.IsAny<string>(), It.IsAny<UTorrentSettings>()))
                .Throws<InvalidOperationException>();
        }

        protected void GivenSuccessfulDownload()
        {
            Mocker.GetMock<IUTorrentProxy>()
                .Setup(s => s.AddTorrentFromUrl(It.IsAny<String>(), It.IsAny<UTorrentSettings>()))
                .Callback(() =>
                {
                    PrepareClientToReturnQueuedItem();
                });
        }
        
        protected virtual void GivenTorrents(List<UTorrentTorrent> torrents)
        {
            if (torrents == null)
            {
                torrents = new List<UTorrentTorrent>();
            }

            Mocker.GetMock<IUTorrentProxy>()
                .Setup(s => s.GetTorrents(It.IsAny<UTorrentSettings>()))
                .Returns(torrents);
        }

        protected void PrepareClientToReturnQueuedItem()
        {
            GivenTorrents(new List<UTorrentTorrent> 
                {
                    _queued
                });
        }

        protected void PrepareClientToReturnDownloadingItem()
        {
            GivenTorrents(new List<UTorrentTorrent> 
                {
                    _downloading
                });
        }

        protected void PrepareClientToReturnFailedItem()
        {
            GivenTorrents(new List<UTorrentTorrent> 
                {
                    _failed
                });
        }

        protected void PrepareClientToReturnCompletedItem()
        {
            GivenTorrents(new List<UTorrentTorrent>
                {
                    _completed
                });
        }

        [Test]
        public void queued_item_should_have_required_properties()
        {
            PrepareClientToReturnQueuedItem();
            var item = Subject.GetItems().Single();
            VerifyQueued(item);
        }

        [Test]
        public void downloading_item_should_have_required_properties()
        {
            PrepareClientToReturnDownloadingItem();
            var item = Subject.GetItems().Single();
            VerifyDownloading(item);
        }

        [Test]
        public void failed_item_should_have_required_properties()
        {
            PrepareClientToReturnFailedItem();
            var item = Subject.GetItems().Single();
            VerifyWarning(item);
        }

        [Test]
        public void completed_download_should_have_required_properties()
        {
            PrepareClientToReturnCompletedItem();
            var item = Subject.GetItems().Single();
            VerifyCompleted(item);
        }

        [Test]
        public void Download_should_return_unique_id()
        {
            GivenSuccessfulDownload();

            var remoteEpisode = CreateRemoteEpisode();

            var id = Subject.Download(remoteEpisode);

            id.Should().NotBeNullOrEmpty();
        }

        [Test]
        public void GetItems_should_ignore_downloads_from_other_categories()
        {
            _completed.Label = "myowncat";
            PrepareClientToReturnCompletedItem();

            var items = Subject.GetItems();

            items.Should().BeEmpty();
        }

        // Proxy.GetTorrents does not return original url. So item has to be found via magnet url.
        [TestCase("magnet:?xt=urn:btih:ZPBPA2P6ROZPKRHK44D5OW6NHXU5Z6KR&tr=udp", "CBC2F069FE8BB2F544EAE707D75BCD3DE9DCF951")]
        public void Download_should_get_hash_from_magnet_url(String magnetUrl, String expectedHash)
        {
            GivenSuccessfulDownload();

            var remoteEpisode = CreateRemoteEpisode();
            remoteEpisode.Release.DownloadUrl = magnetUrl;

            var id = Subject.Download(remoteEpisode);

            id.Should().Be(expectedHash);
        }

        [TestCase(UTorrentTorrentStatus.Loaded, DownloadItemStatus.Queued)]
        [TestCase(UTorrentTorrentStatus.Loaded | UTorrentTorrentStatus.Checking, DownloadItemStatus.Queued)]
        [TestCase(UTorrentTorrentStatus.Loaded | UTorrentTorrentStatus.Queued, DownloadItemStatus.Queued)]
        [TestCase(UTorrentTorrentStatus.Loaded | UTorrentTorrentStatus.Started, DownloadItemStatus.Downloading)]
        [TestCase(UTorrentTorrentStatus.Loaded | UTorrentTorrentStatus.Queued | UTorrentTorrentStatus.Started, DownloadItemStatus.Downloading)]
        public void GetItems_should_return_queued_item_as_downloadItemStatus(UTorrentTorrentStatus apiStatus, DownloadItemStatus expectedItemStatus)
        {
            _queued.Status = apiStatus;

            PrepareClientToReturnQueuedItem();

            var item = Subject.GetItems().Single();

            item.Status.Should().Be(expectedItemStatus);
        }

        [TestCase(UTorrentTorrentStatus.Loaded | UTorrentTorrentStatus.Checking, DownloadItemStatus.Queued)]
        [TestCase(UTorrentTorrentStatus.Loaded | UTorrentTorrentStatus.Checked | UTorrentTorrentStatus.Queued, DownloadItemStatus.Queued)]
        [TestCase(UTorrentTorrentStatus.Loaded | UTorrentTorrentStatus.Started, DownloadItemStatus.Downloading)]
        [TestCase(UTorrentTorrentStatus.Loaded | UTorrentTorrentStatus.Queued | UTorrentTorrentStatus.Started, DownloadItemStatus.Downloading)]
        public void GetItems_should_return_downloading_item_as_downloadItemStatus(UTorrentTorrentStatus apiStatus, DownloadItemStatus expectedItemStatus)
        {
            _downloading.Status = apiStatus;

            PrepareClientToReturnDownloadingItem();

            var item = Subject.GetItems().Single();

            item.Status.Should().Be(expectedItemStatus);
        }

        [TestCase(UTorrentTorrentStatus.Loaded | UTorrentTorrentStatus.Checking, DownloadItemStatus.Queued, false)]
        [TestCase(UTorrentTorrentStatus.Loaded | UTorrentTorrentStatus.Checked, DownloadItemStatus.Completed, false)]
        [TestCase(UTorrentTorrentStatus.Loaded | UTorrentTorrentStatus.Checked | UTorrentTorrentStatus.Queued, DownloadItemStatus.Completed, true)]
        [TestCase(UTorrentTorrentStatus.Loaded | UTorrentTorrentStatus.Checked | UTorrentTorrentStatus.Started, DownloadItemStatus.Completed, true)]
        [TestCase(UTorrentTorrentStatus.Loaded | UTorrentTorrentStatus.Checked | UTorrentTorrentStatus.Queued | UTorrentTorrentStatus.Paused, DownloadItemStatus.Completed, true)]
        public void GetItems_should_return_completed_item_as_downloadItemStatus(UTorrentTorrentStatus apiStatus, DownloadItemStatus expectedItemStatus, Boolean expectedReadOnly)
        {
            _completed.Status = apiStatus;

            PrepareClientToReturnCompletedItem();

            var item = Subject.GetItems().Single();

            item.Status.Should().Be(expectedItemStatus);
            item.IsReadOnly.Should().Be(expectedReadOnly);
        }

        [Test]
        public void should_return_status_with_outputdirs()
        {
            var configItems = new Dictionary<String, String>();
            
            configItems.Add("dir_active_download_flag", "true");
            configItems.Add("dir_active_download", @"C:\Downloads\Downloading\utorrent".AsOsAgnostic());
            configItems.Add("dir_completed_download", @"C:\Downloads\Finished\utorrent".AsOsAgnostic());
            configItems.Add("dir_completed_download_flag", "true");
            configItems.Add("dir_add_label", "true");
            
            Mocker.GetMock<IUTorrentProxy>()
                .Setup(v => v.GetConfig(It.IsAny<UTorrentSettings>()))
                .Returns(configItems);

            var result = Subject.GetStatus();

            result.IsLocalhost.Should().BeTrue();
            result.OutputRootFolders.Should().NotBeNull();
            result.OutputRootFolders.First().Should().Be(@"C:\Downloads\Finished\utorrent\tv".AsOsAgnostic());
        }

        [Test]
        public void should_combine_drive_letter()
        {
            WindowsOnly();

            _completed.RootDownloadPath = "D:";

            PrepareClientToReturnCompletedItem();

            var item = Subject.GetItems().Single();

            item.OutputPath.Should().Be(@"D:\" + _title);
        }

        [Test]
        public void Download_should_handle_http_redirect_to_magnet()
        {
            GivenRedirectToMagnet();
            GivenSuccessfulDownload();

            var remoteEpisode = CreateRemoteEpisode();

            var id = Subject.Download(remoteEpisode);

            id.Should().NotBeNullOrEmpty();
        }
    }
}
