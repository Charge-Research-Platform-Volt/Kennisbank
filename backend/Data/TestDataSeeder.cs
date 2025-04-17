using KnowledgeBank.Models;
using Microsoft.EntityFrameworkCore;

namespace KnowledgeBank.Data
{
    public static class TestDataSeeder
    {
#pragma warning disable CS8618 // Non-nullable field must contain a non-null value when exiting constructor. Consider adding the 'required' modifier or declaring as nullable.
        private static IAzureBlobService blobService;
        private static DatabaseContext database;
        private static ResourceManager resourceManager;
#pragma warning restore CS8618 // Non-nullable field must contain a non-null value when exiting constructor. Consider adding the 'required' modifier or declaring as nullable.
        private static string testDataPath = Path.Combine("/app", "testdata") + "/";
        /// <summary>
        /// Adds test data to the database and blob storage
        /// </summary>
        /// <param name="serviceProvider">All services</param>
        /// <returns></returns>
        public static async Task Seed(IServiceProvider serviceProvider)
        {
            using IServiceScope scope = serviceProvider.CreateScope();
            blobService = scope.ServiceProvider.GetRequiredService<IAzureBlobService>();
            database = scope.ServiceProvider.GetRequiredService<DatabaseContext>();
            resourceManager = scope.ServiceProvider.GetRequiredService<ResourceManager>();

            // await ShamelessCopyOfUpload("path", "title", "");
            await AddTestAuthor("William Shakespeare", "Librarian", "Lived a long time ago", "william.shakespeare@gmail.com", "@WilliamShakespear");
            await AddTestAuthor("Donal Trump", "Entertainer", "Hates everyone", "americanumberone@trump.com", "@Idiot");
            await AddTestAuthor("Ozzy Osbourne", "Rockstar", "Loves drugs", "Ozz.Bourne@gmail.com", "@OzzyOsbourne");
            await AddTestAuthor("Jan Adriaanszoon Leeghwater", "Windmills", "Insanely good at creating land from oceans", "leeghwater@gmail.com", "@LeeghwaterJan");
            await AddTestAuthor("Mark Rutte", "NATO BAAS", "Committed treason against the Dutch people", "Markie.Rutte@gmail.com", "@MarkRutte");
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

        /// <summary>
        /// Adds a test data author to the database
        /// </summary>
        /// <param name="name">Name of author</param>
        /// <param name="occupation">Occupation of author</param>
        /// <param name="description">Description of author</param>
        /// <param name="emailaddress">Email of author</param>
        /// <param name="linkedin">Linkedin of author</param>
        /// <returns></returns>
        private static async Task AddTestAuthor(string name, string occupation, string description, string emailaddress, string linkedin)
        {
            if (await resourceManager.PersonExistsAsync(p => p.Name == name)) return;
            try
            {
                PersonCreateDto dto = new()
                {
                    Name = name,
                    Occupation = occupation,
                    Description = description,
                    EmailAddress = emailaddress,
                    Linkedin = linkedin,
                };

                await resourceManager.CreatePersonAsync(dto);
            }
            catch (Exception e)
            {
                Serilog.Log.Logger.Error(e, "Error adding person to document: {name}.");
            }
        }

        /// <summary>
        /// Makes a shameless copy of an uploaded file in the testdata and updates the database and blob storage accordingly
        /// </summary>
        /// <param name="path">Path to the test file</param>
        /// <param name="title">Name of test file</param>
        /// <param name="description">Description of test file</param>
        /// <returns></returns>
        private static async Task ShamelessCopyOfUpload(string path, string title, string description)
        {
            path = testDataPath + path;

            if (await resourceManager.ResourceExistsAsync(r => r.Title == title)) return;

            ResourceCreateDto dto = new()
            {
                Title = title,
                Description = description,
                LanguageCode = "??",
                TypeId = DatabaseSeeder.UnknownResourceTypeId,
                PublicationDate = DateTime.UtcNow,
            };

            string extension = Path.GetExtension(path);

            string fileType = Filetype.ConvertExtensionToFiletype(extension);

            await resourceManager.BeginTransaction();

            Guid id = await resourceManager.CreateResourceAsync(dto);
            await resourceManager.UpdateResourceAsync(id, r => r.FileType, fileType);

            try
            {
                Dictionary<string, string> metadata = new Dictionary<string, string> { { "extension", extension } };
                BLOB_STATUSCODE result = await blobService.UploadBlobAsync(fileType, id.ToString(), metadata, File.OpenRead(path), false);

                if (result == BLOB_STATUSCODE.OK)
                {
                    await resourceManager.Commit();
                }else
                {
                    await resourceManager.Rollback();
                }
            }
            catch (Exception e)
            {
                await resourceManager.Rollback();
                Serilog.Log.Logger.Error(e, "Error uploading file {FileName}.", path);
            }
        }
    }
}
