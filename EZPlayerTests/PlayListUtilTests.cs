using System.IO;
using System.Linq;
using EZPlayer;
using EZPlayer.PlayList;
using NSubstitute;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace EZPlayerTests
{
    [TestClass]
    public class PlayListUtilTests
    {
        [TestMethod]
        public void TestGetPlayList()
        {
            var filesInTheSameDir = new string[] 
            { 
                "The Dark Knight 1.avi",
                "The Dark Knight 2.avi",
                "The Dark Knight 3.avi" 
            };
            var expected = filesInTheSameDir.Skip(1).ToArray();
            DoTestPlayList(filesInTheSameDir[1], filesInTheSameDir, expected);
        }

        [TestMethod]
        public void TestCaseInsensative()
        {
            var filesInTheSameDir = new string[] 
            { 
                "The Dark Knight 1.avi",
                "the Dark Knight 2.avi",
                "the Dark Knight 3.avi",
                "The Dark Knight 4.avi"
            };
            var expected = filesInTheSameDir.Skip(1).ToArray();
            DoTestPlayList(filesInTheSameDir[1], filesInTheSameDir, expected);
        }

        [TestMethod]
        public void TestNotSimilarFile()
        {
            var filesInTheSameDir = new string[] 
            { 
                "The Dark Knight 1.avi",
                "The Dark Knight 2.avi",
                "The Fifth Element 1.avi",
                "The Fifth Element 2.avi"
            };
            var expected = filesInTheSameDir.Skip(1).ToArray();
            DoTestPlayList(filesInTheSameDir[1], filesInTheSameDir, expected);
        }

        [TestMethod]
        public void TestNotSimilarFileOnlyGreaterFilesShouldBeIncluded()
        {
            var filesInTheSameDir = new string[] 
            { 
                "The Dark Knight 1.avi",
                "The Dark Knight 2.avi",
                "The Fifth Element 1.avi",
                "The Fifth Element 2.avi",
                "Independence Day.avi",
            };
            var expected = filesInTheSameDir.Skip(1).Take(3).ToArray();
            DoTestPlayList(filesInTheSameDir[1], filesInTheSameDir, expected);
        }

        private static void DoTestPlayList(string fileToGetPlayListFor, string[] filesInTheSameDir, string[] expected)
        {
            var dirSearcher = Substitute.For<IDirectorySearcher>();
            dirSearcher.SearchFiles(Arg.Any<string>(),
                Arg.Any<string>(),
                Arg.Any<SearchOption>()).Returns(filesInTheSameDir);

            var result = PlayListUtil.GetPlayList(fileToGetPlayListFor, dirSearcher);

            CollectionAssert.AreEqual(expected, result);
        }
    }
}
