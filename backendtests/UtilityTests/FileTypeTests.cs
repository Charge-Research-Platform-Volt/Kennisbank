using KnowledgeBank.Data;

namespace backend.Tests
{
    [TestFixture]
    [Category("UnitTest")]
    public class FileTypeTests
    {
        [TestCase(".pdf", Filetype.UploadType.Document)]
        [TestCase(".docx", Filetype.UploadType.Document)]
        [TestCase(".txt", Filetype.UploadType.Document)]
        [Description("Tests if the conversion results in correct trimming and returns the full filetype from the dictionary")]
        public void extensionConversionTest(string input, string output)
        {
            Assert.That(Filetype.ConvertExtensionToFiletype(input), Is.EqualTo(output));
        }

        [TestCase(".wrong")]
        public void invalidExtensionConversionTest(string input)
        {
            Assert.Throws<KeyNotFoundException>(() => Filetype.ConvertExtensionToFiletype(input));
        }

        [TestCase(".pdf", true)]
        [TestCase(".docx", true)]
        [TestCase(".ppt", true)]
        [TestCase(".txt", true)]
        [TestCase(".what", false)]
        [TestCase(".135", false)]
        [TestCase(".dlfkaj", false)]
        [TestCase("", false)]
        [Description("Tests if supported file formats are accepted and invalid file formats rejected")]
        public void supportedFileType(string filetype, bool judgment)
        {
            Assert.That(Filetype.Supported(filetype), Is.EqualTo(judgment));
        }
    }
}

// This program has been developed by students from the bachelor Computer Science at Utrecht
// University within the Software Project course.
// © Copyright Utrecht University (Department of Information and Computing Sciences)


