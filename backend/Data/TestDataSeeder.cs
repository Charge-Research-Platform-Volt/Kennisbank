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
            
            // await ShamelessCopyOfUpload("path", "title", "");
            await ShamelessCopyOfUpload("WRRRaport - Opgave AI.pdf", "Opgave AI. De nieuwe systeemtechnologie", "WRR reageert op de regeringsaanvraag over de impact van AI op publieke waarden. AI wordt gezien als een systeemtechnologie met langdurige, grootschalige en onvoorspelbare effecten, daarom pleit de WRR voor een integrale aanpak met sterke overheidsbetrokkenheid");
            await ShamelessCopyOfUpload("WP+50v2_+AI+van+repliek+gediend_DEF_DT.pdf", "AI van repliek gediend? Een verkenning van tegenmacht vanuit maatschappelijke organisaties", "");
            await ShamelessCopyOfUpload("Aandacht+voor+media.+Naar+nieuwe+waarborgen+voor+hun+democratische++functies.pdf", "Aandacht voor media. Naar nieuwe waarborgen voor hun democratische functies", "In dit rapport onderzoekt de WRR de kenmerken en werking van het nieuwe mediasysteem en wat de impact ervan is op de democratie. We concluderen dat de drie democratische functies van media onder druk staan en dat nieuw beleid noodzakelijk is");
            await ShamelessCopyOfUpload("Mensbeelden+bij+beleid.pdf", "Mensbeelden bij beleid", "Dit essay benadrukt dat beleidsmakers vaak impliciete aannames hebben over wat mensen willen, kunnen en hoe ze zich gedragen. Deze mensbeelden zijn niet altijd realistisch en kunnen leiden tot beleid dat niet aansluit bij de diversiteit van de samenleving. Meer betrekken van burgers bij het beleid dus van belang.");
            await ShamelessCopyOfUpload("Burgerperspectieven+3e+editie+2024.pdf", "Burgerperspectieven 2024 (bericht 3)", "");
            await ShamelessCopyOfUpload("Onderzoek+Somber+over+de+samenleving.pdf", "Somber over de samenleving ", "SCP bepleit hier dat mensen die maatschappelijk onbehagen voelen meer een plek moeten krijgen in besluitvorming");
            await ShamelessCopyOfUpload("Europes-Democracy-Blind-Spots-1.pdf", "Europe's democracy blind spots", "Dit rapport laat het belang zien van democratische vernieuwing voor Europese welvaart en veiligheid.");
            await ShamelessCopyOfUpload("46e50ca21cbc23de3296d50e6c804b12d4188e2618578283b5c29179924c2096.pdf", "Strengthening democracy through participatory and deliberative processes ", "");
            await ShamelessCopyOfUpload("Participatory_Democracy_paper_v4.pdf", "Participatory democracy at the EU level: How to break the invisible ceiling", "");
            await ShamelessCopyOfUpload("FIDE+-+Including+the+underrepresented.pdf", "Including the underrepresented", "");
            await ShamelessCopyOfUpload("Handreiking-Burgerberaden_okt-2024-v2.pdf", "Handreiking Burgerberaden", "");
            await ShamelessCopyOfUpload("Essay+Burgers+gelijkwaardig+aan+ontwerptafel+van+beleid.pdf", "Burgers gelijkwaardig aan de ontwerptafel", "");
            await ShamelessCopyOfUpload("kennedy-et-al-2020-demographics-and-(equal-)-voice-assessing-participation-in-online-deliberative-sessions.pdf", "Demographics and (Equal?) Voice: Assessing Participation in Online Deliberative Sessions", "");
            await ShamelessCopyOfUpload("1887_3731030-Full Text.pdf", "Stimulering en facilitering van burgerinitiatieven door de overheid: over de invulling van de ‘dienende overheid’ bij derde generatie burgerparticipatie", "");
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
                    await database.SaveFileChangesAsync();
                }
            }
            catch (Exception e)
            {
                Serilog.Log.Logger.Error(e, "Error uploading file {FileName}.", path);
            }
        }
    }
}
