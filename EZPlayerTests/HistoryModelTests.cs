using System;
using System.IO;
using EZPlayer.History;
using NUnit.Framework;

namespace EZPlayerTests
{
    [TestFixture]
    public class HistoryModelTests
    {
        private readonly static string m_lastPlayInfoPath = "TestLastPlay.xml";
        private readonly static string m_historyInfoPath = "TestHistory.xml";
        private HistoryModel m_model = null;
        [SetUp]
        public void Setup()
        {
            if (File.Exists(m_lastPlayInfoPath))
            {
                File.Delete(m_lastPlayInfoPath);
            }

            if (File.Exists(m_historyInfoPath))
            {
                File.Delete(m_historyInfoPath);
            }
            HistoryModel.HISTORY_INFO_FILE_PATH = m_historyInfoPath;
            m_model = HistoryModel.Instance;
            m_model.Reload();
        }

        [Test]
        public void TestEmptyLastPlayedFile()
        {
            Assert.IsNull(m_model.LastPlayedFile);
        }

        [Test]
        public void TestEmptyHistory()
        {
            Assert.IsNull(m_model.GetHistoryInfo("DummyPath"));
        }

        [Test]
        public void TestSetLastPlayedFile()
        {
            var expected = new HistoryItem()
            {
                Position = 0.1f,
                FilePath = "DummyPath",
                Volume = 1d,
                PlayedDate = DateTime.Now
            };
            m_model.LastPlayedFile = expected;
            Assert2ItemsAreEqual(expected, m_model.LastPlayedFile);

            var historyItem = m_model.GetHistoryInfo(expected.FilePath);
            Assert.NotNull(historyItem);
            Assert2ItemsAreEqual(expected, historyItem);
        }

        [Test]
        public void TestAddFilesToHistory()
        {
            var firstFile = new HistoryItem()
            {
                Position = 0.1f,
                FilePath = "File1",
                Volume = 1d,
                PlayedDate = DateTime.Now
            };
            m_model.LastPlayedFile = firstFile;

            var secondFile = new HistoryItem()
            {
                Position = 0.1f,
                FilePath = "File2",
                Volume = 1d,
                PlayedDate = DateTime.Now
            };
            m_model.LastPlayedFile = secondFile;

            var historyItem = m_model.GetHistoryInfo(firstFile.FilePath);
            Assert.NotNull(historyItem);
            Assert2ItemsAreEqual(firstFile, historyItem);

            historyItem = m_model.GetHistoryInfo(secondFile.FilePath);
            Assert.NotNull(historyItem);
            Assert2ItemsAreEqual(secondFile, historyItem);
        }

        [Test]
        public void TestPlaySameFileServalTimes()
        {
            var playedYesterday = new HistoryItem()
            {
                Position = 0.1f,
                FilePath = "File1",
                Volume = 1d,
                PlayedDate = DateTime.Now - TimeSpan.FromDays(1d)
            };
            
            m_model.LastPlayedFile = playedYesterday;

            var playedToday = new HistoryItem()
            {
                Position = 0.2f,
                FilePath = playedYesterday.FilePath,
                Volume = 0.5d,
                PlayedDate = DateTime.Now
            };
            m_model.LastPlayedFile = playedToday;

            var historyItem = m_model.GetHistoryInfo(playedToday.FilePath);
            Assert.NotNull(historyItem);
            Assert2ItemsAreEqual(playedToday, historyItem);
            Assert2ItemsAreEqual(playedToday, m_model.LastPlayedFile);
        }

        private static void Assert2ItemsAreEqual(HistoryItem expected, HistoryItem historyItem)
        {
            Assert.AreEqual(expected.FilePath, historyItem.FilePath);
            Assert.AreEqual(expected.Position, historyItem.Position);
            Assert.AreEqual(expected.PlayedDate, historyItem.PlayedDate);
            Assert.AreEqual(expected.Volume, historyItem.Volume);
        }
    }
}
