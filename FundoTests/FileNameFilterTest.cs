using System.IO;
using fundo.core.Search.Filter;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace fundo.tests.Search.Native
{
    [TestClass]
    public class FileNameFilterTests
    {
        [TestMethod]
        public void IsAllowed_ReturnsFalse_WhenFileInfoIsNull()
        {
            var filter = new FileNameFilter("*.txt");

            var result = filter.isAllowed(null);

            Assert.IsFalse(result);
        }

        [TestMethod]
        public void IsAllowed_AllowsAll_WhenPatternIsEmpty()
        {
            var filter = new FileNameFilter(string.Empty);
            var file = new FileInfo("test.anything");

            var result = filter.isAllowed(file);

            Assert.IsTrue(result);
        }

        [TestMethod]
        public void IsAllowed_MatchesSimpleWildcardStar()
        {
            var filter = new FileNameFilter("*.txt");
            var matching = new FileInfo("report.txt");
            var notMatching = new FileInfo("image.jpg");

            Assert.IsTrue(filter.isAllowed(matching));
            Assert.IsFalse(filter.isAllowed(notMatching));
        }

        [TestMethod]
        public void IsAllowed_MatchesSingleCharacterQuestionMark()
        {
            var filter = new FileNameFilter("file?.txt");
            var matching = new FileInfo("file1.txt");
            var notMatching = new FileInfo("file10.txt");

            Assert.IsTrue(filter.isAllowed(matching));
            Assert.IsFalse(filter.isAllowed(notMatching));
        }

        [TestMethod]
        public void IsAllowed_IsCaseInsensitive()
        {
            var filter = new FileNameFilter("*.TXT");
            var file = new FileInfo("readme.txt");

            var result = filter.isAllowed(file);

            Assert.IsTrue(result);
        }

        [TestMethod]
        public void IsAllowed_MatchesPatternWithStarInMiddle()
        {
            var filter = new FileNameFilter("log*.txt");
            var matching1 = new FileInfo("log.txt");
            var matching2 = new FileInfo("log_2026_01.txt");
            var notMatching = new FileInfo("mylog.txt");

            Assert.IsTrue(filter.isAllowed(matching1));
            Assert.IsTrue(filter.isAllowed(matching2));
            Assert.IsFalse(filter.isAllowed(notMatching));
        }
    }
}
