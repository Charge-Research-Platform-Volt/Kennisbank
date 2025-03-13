using backend.Responses;
using KnowledgeBank.Data;
using KnowledgeBank.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Scaffolding.Metadata;

namespace backend.Data
{
    public static class TestDataSeeder
    {
        private static IAzureBlobService blobService;
        private static DatabaseContext database;
        private static string testDataPath = Path.Combine("/app", "testdata") + "/";

        public static async Task Seed(IServiceProvider serviceProvider)
        {
            using IServiceScope scope = serviceProvider.CreateScope();
            blobService = scope.ServiceProvider.GetRequiredService<IAzureBlobService>();
            database = scope.ServiceProvider.GetRequiredService<DatabaseContext>();

            // await ShamelessCopyOfUpload("path", "title", "description");
            await ShamelessCopyOfUpload("WRRRaport - Opgave AI.pdf", "Opgave AI. De nieuwe systeemtechnologie", "WRR reageert op de regeringsaanvraag over de impact van AI op publieke waarden. AI wordt gezien als een systeemtechnologie met langdurige, grootschalige en onvoorspelbare effecten, daarom pleit de WRR voor een integrale aanpak met sterke overheidsbetrokkenheid");
        }

        private static async Task ShamelessCopyOfUpload(string path, string title, string description)
        {
            path = testDataPath + path;

            FileItem? item = await database.Files.Where(f => f.Name == title).FirstOrDefaultAsync();

            if (item != null) return;

            string extension = Path.GetExtension(path);

            string fileType = Filetype.ConvertExtensionToFiletype(extension);

            Guid id = Guid.NewGuid();

            try
            {
                Dictionary<string, string> metadata = new Dictionary<string, string> { { "extension", extension } };
                BLOB_STATUSCODE result = await blobService.UploadBlobAsync(fileType, id.ToString(), metadata, File.OpenRead(path), false);

                if (result == BLOB_STATUSCODE.OK)
                {
                    FileItem drive = new()
                    {
                        Id = id,
                        Name = title,
                        Description = description,
                        FileType = fileType,
                    };

                    await database.Files.AddAsync(drive);
                    await database.SaveChangesAsync();
                }
            }
            catch (Exception e)
            {
                Serilog.Log.Logger.Error(e, "Error uploading file {FileName}.", path);
            }
        }
    }
}
