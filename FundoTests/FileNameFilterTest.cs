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

            var result = filter.IsAllowed(null);

            Assert.IsFalse(result);
        }

        [TestMethod]
        public void IsAllowed_AllowsAll_WhenPatternIsEmpty()
        {
            var filter = new FileNameFilter(string.Empty);
            var file = new FileInfo("test.anything");

            var result = filter.IsAllowed(file);

            Assert.IsTrue(result);
        }

        [TestMethod]
        public void IsAllowed_MatchesSimpleWildcardStar()
        {
            var filter = new FileNameFilter("*.txt");
            var matching = new FileInfo("report.txt");
            var notMatching = new FileInfo("image.jpg");

            Assert.IsTrue(filter.IsAllowed(matching));
            Assert.IsFalse(filter.IsAllowed(notMatching));
        }

        [TestMethod]
        public void IsAllowed_MatchesSingleCharacterQuestionMark()
        {
            var filter = new FileNameFilter("file?.txt");
            var matching = new FileInfo("file1.txt");
            var notMatching = new FileInfo("file10.txt");

            Assert.IsTrue(filter.IsAllowed(matching));
            Assert.IsFalse(filter.IsAllowed(notMatching));
        }

        [TestMethod]
        public void IsAllowed_IsCaseInsensitive()
        {
            var filter = new FileNameFilter("*.TXT");
            var file = new FileInfo("readme.txt");

            var result = filter.IsAllowed(file);

            Assert.IsTrue(result);
        }

        [TestMethod]
        public void IsAllowed_MatchesPatternWithStarInMiddle()
        {
            var filter = new FileNameFilter("log*.txt");
            var matching1 = new FileInfo("log.txt");
            var matching2 = new FileInfo("log_2026_01.txt");
            var notMatching = new FileInfo("mylog.txt");

            Assert.IsTrue(filter.IsAllowed(matching1));
            Assert.IsTrue(filter.IsAllowed(matching2));
            Assert.IsFalse(filter.IsAllowed(notMatching));
        }

        [TestMethod]
        public void Regex_ReturnsFalse_WhenFileInfoIsNull()
        {
            var filter = new FileNameFilter(@"\.txt$", useRegex: true);

            Assert.IsFalse(filter.IsAllowed(null));
        }

        [TestMethod]
        public void Regex_AllowsAll_WhenPatternIsEmpty()
        {
            var filter = new FileNameFilter(string.Empty, useRegex: true);
            var file = new FileInfo("test.anything");

            Assert.IsTrue(filter.IsAllowed(file));
        }

        [TestMethod]
        public void Regex_MatchesSimplePattern()
        {
            var filter = new FileNameFilter(@"\.txt$", useRegex: true);
            var matching = new FileInfo("report.txt");
            var notMatching = new FileInfo("image.jpg");

            Assert.IsTrue(filter.IsAllowed(matching));
            Assert.IsFalse(filter.IsAllowed(notMatching));
        }

        [TestMethod]
        public void Regex_IsCaseInsensitive()
        {
            var filter = new FileNameFilter(@"\.TXT$", useRegex: true);
            var file = new FileInfo("readme.txt");

            Assert.IsTrue(filter.IsAllowed(file));
        }

        [TestMethod]
        public void Regex_MatchesComplexPattern()
        {
            var filter = new FileNameFilter(@"^log_\d{4}_\d{2}\.txt$", useRegex: true);
            var matching = new FileInfo("log_2026_01.txt");
            var notMatching1 = new FileInfo("log.txt");
            var notMatching2 = new FileInfo("log_abcd_01.txt");

            Assert.IsTrue(filter.IsAllowed(matching));
            Assert.IsFalse(filter.IsAllowed(notMatching1));
            Assert.IsFalse(filter.IsAllowed(notMatching2));
        }

        [TestMethod]
        public void Regex_MatchesAlternation()
        {
            var filter = new FileNameFilter(@"\.(txt|log)$", useRegex: true);
            var matching1 = new FileInfo("report.txt");
            var matching2 = new FileInfo("app.log");
            var notMatching = new FileInfo("image.jpg");

            Assert.IsTrue(filter.IsAllowed(matching1));
            Assert.IsTrue(filter.IsAllowed(matching2));
            Assert.IsFalse(filter.IsAllowed(notMatching));
        }

        [TestMethod]
        public void Regex_PartialMatchIsAllowed()
        {
            var filter = new FileNameFilter("report", useRegex: true);
            var matching = new FileInfo("annual_report_2026.pdf");
            var notMatching = new FileInfo("summary.pdf");

            Assert.IsTrue(filter.IsAllowed(matching));
            Assert.IsFalse(filter.IsAllowed(notMatching));
        }
    }
}
