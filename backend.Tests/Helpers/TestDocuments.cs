using System.IO.Compression;
using System.Text;

namespace KnowledgeBank.Tests.Helpers;

/// <summary>Small real documents built in code, so tests don't depend on binary fixture files.</summary>
public static class TestDocuments
{
    /// <summary>A minimal OpenDocument text file: the zip with a content.xml around <paramref name="body"/>.</summary>
    public static byte[] Odt(string body)
    {
        string xml = $"""
            <?xml version="1.0" encoding="UTF-8"?>
            <office:document-content
                xmlns:office="urn:oasis:names:tc:opendocument:xmlns:office:1.0"
                xmlns:text="urn:oasis:names:tc:opendocument:xmlns:text:1.0"
                xmlns:table="urn:oasis:names:tc:opendocument:xmlns:table:1.0">
              <office:body><office:text>{body}</office:text></office:body>
            </office:document-content>
            """;

        using MemoryStream stream = new();
        using (ZipArchive zip = new(stream, ZipArchiveMode.Create, leaveOpen: true))
        {
            using (StreamWriter mimetype = new(zip.CreateEntry("mimetype").Open()))
                mimetype.Write("application/vnd.oasis.opendocument.text");
            using (StreamWriter content = new(zip.CreateEntry("content.xml").Open(), Encoding.UTF8))
                content.Write(xml);
        }
        return stream.ToArray();
    }
}
