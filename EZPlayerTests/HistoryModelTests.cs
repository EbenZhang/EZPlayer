using System;
using System.IO;
using EZPlayer.History;
using NUnit.Framework;

namespace EZPlayerTests
{
    [TestFixture]
    public class HistoryModelTests
    {
        private readonly static string m_lastPlayInfoPath = "LastPlay.xml";
        private readonly static string m_historyInfoPath = "History.xml";
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
        }

        [Test]
        public void TestEmptyLastPlayedFile()
        {
            HistoryModel m = new HistoryModel(m_lastPlayInfoPath, m_historyInfoPath);
            Assert.IsNull(m.LastPlayedFile);
        }

        [Test]
        public void TestEmptyHistory()
        {
            HistoryModel m = new HistoryModel(m_lastPlayInfoPath, m_historyInfoPath);
            Assert.IsNull(m.GetHistoryInfo("DummyPath"));
        }

        [Test]
        public void TestSetLastPlayedFile()
        {
            HistoryModel m = new HistoryModel(m_lastPlayInfoPath, m_historyInfoPath);
            var expected = new HistoryItem()
            {
                Position = 0.1f,
                FilePath = "DummyPath",
                Volume = 1d,
                PlayedDate = DateTime.Now
            };
            m.LastPlayedFile = expected;
            Assert2ItemsAreEqual(expected, m.LastPlayedFile);

            var historyItem = m.GetHistoryInfo(expected.FilePath);
            Assert.NotNull(historyItem);
            Assert2ItemsAreEqual(expected, historyItem);
        }

        [Test]
        public void TestAddFilesToHistory()
        {
            HistoryModel m = new HistoryModel(m_lastPlayInfoPath, m_historyInfoPath);
            var firstFile = new HistoryItem()
            {
                Position = 0.1f,
                FilePath = "File1",
                Volume = 1d,
                PlayedDate = DateTime.Now
            };
            m.LastPlayedFile = firstFile;

            var secondFile = new HistoryItem()
            {
                Position = 0.1f,
                FilePath = "File2",
                Volume = 1d,
                PlayedDate = DateTime.Now
            };
            m.LastPlayedFile = secondFile;

            var historyItem = m.GetHistoryInfo(firstFile.FilePath);
            Assert.NotNull(historyItem);
            Assert2ItemsAreEqual(firstFile, historyItem);

            historyItem = m.GetHistoryInfo(secondFile.FilePath);
            Assert.NotNull(historyItem);
            Assert2ItemsAreEqual(secondFile, historyItem);
        }

        [Test]
        public void TestPlaySameFileServalTimes()
        {
            HistoryModel m = new HistoryModel(m_lastPlayInfoPath, m_historyInfoPath);
            var playedYesterday = new HistoryItem()
            {
                Position = 0.1f,
                FilePath = "File1",
                Volume = 1d,
                PlayedDate = DateTime.Now - TimeSpan.FromDays(-1d)
            };
            m.LastPlayedFile = playedYesterday;

            var playedToday = new HistoryItem()
            {
                Position = 0.2f,
                FilePath = playedYesterday.FilePath,
                Volume = 0.5d,
                PlayedDate = DateTime.Now
            };
            m.LastPlayedFile = playedToday;

            var historyItem = m.GetHistoryInfo(playedToday.FilePath);
            Assert.NotNull(historyItem);
            Assert2ItemsAreEqual(playedToday, historyItem);
            Assert2ItemsAreEqual(playedToday, m.LastPlayedFile);
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
