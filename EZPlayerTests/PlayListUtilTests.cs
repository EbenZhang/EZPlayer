using System.Collections.Generic;
using System.IO;
using EZPlayer;
using EZPlayer.PlayList;
using NSubstitute;
using NUnit.Framework;

namespace EZPlayerTests
{
    [TestFixture]
    public class PlayListUtilTests
    {
        [Test]
        public void TestInit()
        {
            var dirSearcher = Substitute.For<IDirectorySearcher>();
            dirSearcher.SearchFiles(Arg.Any<string>(),
                Arg.Any<string>(),
                Arg.Any<SearchOption>()).Returns(new string[] { "a", "b", "c" });

            var result = PlayListUtil.GetPlayList("b", dirSearcher);
            var expected = new List<string>{"b","c"};
            Assert.AreEqual(expected, result);
        }
    }
}
