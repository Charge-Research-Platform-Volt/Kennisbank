using KnowledgeBank.Models;
using Microsoft.EntityFrameworkCore;
using System.Globalization;
using CsvHelper;
using CsvHelper.Configuration;
using CsvHelper.Configuration.Attributes;

namespace KnowledgeBank.Data
{
    public static class TestDataSeeder
    {
#pragma warning disable CS8618 // Non-nullable field must contain a non-null value when exiting constructor. Consider adding the 'required' modifier or declaring as nullable.
        private static IAzureBlobService blobService;
        private static ResourceManager resourceManager;
#pragma warning restore CS8618 // Non-nullable field must contain a non-null value when exiting constructor. Consider adding the 'required' modifier or declaring as nullable.
        private static string testDataPath = Path.Combine("/app", "testdata") + "/";
        private static readonly string systemAdminId = "00000000-0000-0000-0000-000000000001";

        /// <summary>
        /// Adds test data to the database and blob storage
        /// </summary>
        /// <param name="serviceProvider">All services</param>
        /// <returns></returns>
        public static async Task Seed(IServiceProvider serviceProvider)
        {
            return;
        
            using IServiceScope scope = serviceProvider.CreateScope();
            blobService = scope.ServiceProvider.GetRequiredService<IAzureBlobService>();
            resourceManager = scope.ServiceProvider.GetRequiredService<ResourceManager>();

            await SeedOrganisations();
            await SeedPersons();
            await SeedResources();
        }    
        
        #region Seed Organisations
        
        /// <summary>
        /// Reads organization data from a CSV file and creates Organisation entities in the database.
        /// </summary>
        private static async Task SeedOrganisations()
        {
            // Read the CSV file and map it to the OrganisationCreateDto
            (List<OrganisationCsvRecord> csvRecords, List<OrganisationCreateDto> organisationDtos) = CsvReaderHelper.ReadCsvFile<OrganisationCsvRecord, OrganisationCreateDto>(
                "Organisations.csv",
                record => new OrganisationCreateDto
                {
                    Name = record.Name,
                    Description = record.Description,
                    Website = record.Website,
                    EmailAddress = record.EmailAddress,
                });
            
            foreach (OrganisationCreateDto dto in organisationDtos)
            {
                if (await resourceManager.OrganisationExistsAsync(o => o.Name == dto.Name)) continue;
                await resourceManager.CreateOrganisationAsync(dto);
            }
        }
        
        #endregion
        
        #region Seed Persons
        
        /// <summary>
        /// Reads person data from a CSV file, creates Person entities in the database, and associates them with organizations.
        /// </summary>
        private static async Task SeedPersons()
        {
            // Read the CSV file and map it to the PersonCreateDto
            (List<PersonCsvRecord> csvRecords, List<PersonCreateDto> personDtos) = CsvReaderHelper.ReadCsvFile<PersonCsvRecord, PersonCreateDto>(
                "Persons.csv",
                record => new PersonCreateDto
                {
                    Name = record.Name,
                    Occupation = record.Occupation,
                    Description = record.Description,
                    EmailAddress = record.EmailAddress,
                    Linkedin = record.Linkedin,
                });
            
            // Go through all records and create the persons
            for (int i = 0; i < csvRecords.Count; i++) 
            {
                PersonCsvRecord record = csvRecords[i];
                PersonCreateDto dto = personDtos[i];
            
                // Create the person if it does not exist
                if (await resourceManager.PersonExistsAsync(p => p.Name == dto.Name)) continue;
                Guid personId = await resourceManager.CreatePersonAsync(dto);
                
                // If the person has organisations, add them
                if (record.Organisations != null && record.Organisations.Length > 0)
                {
                    // Get the organisation names
                    List<string> organisations = record.Organisations.Split(',').ToList();
                    
                    // For each organisation, get the organisation and add the person to it
                    foreach (string organisationName in organisations) 
                    {
                        Organisation? organisation = await resourceManager.GetOrganisationAsync(o => o.Name == organisationName);
                        
                        if (organisation != null) 
                        {
                            await resourceManager.AddPersonToOrganisationAsync(personId, organisation.Id);
                        }
                    }
                }
            }
        }
        
        #endregion
        
        #region Seed Resources

        /// <summary>
        /// Reads resource data from a CSV file, creates Resource entities in the database, 
        /// and associates them with tags, authors, and organizations.
        /// </summary>
        private static async Task SeedResources()
        {
            string extension = ".pdf";
            string fileType = Filetype.ConvertExtensionToFiletype(extension);
        
            // Read the CSV file and map it to the ResourceCreateDto
            (List<ResourceCsvRecord> csvRecords, List<ResourceCreateDto> resourceDtos) = CsvReaderHelper.ReadCsvFile<ResourceCsvRecord, ResourceCreateDto>(
                "Resources.csv",
                record => new ResourceCreateDto
                {
                    Title = record.Title, 
                    Description = record.Description,
                    TypeId = DatabaseSeeder.UnknownResourceTypeId,
                    LanguageCode = "EN",
                    PublicationDate = DateTime.SpecifyKind(DateTime.ParseExact(record.PublicationDate, "yyyy", CultureInfo.InvariantCulture), DateTimeKind.Utc),
                    CreationDate = DateTime.UtcNow,
                    License = record.License,
                    Note = record.Note
                });
            
            for (int i = 0; i < csvRecords.Count; i++)
            {
                ResourceCsvRecord record = csvRecords[i];
                ResourceCreateDto dto = resourceDtos[i];
                string filePath = testDataPath + record.Title.Replace(" ", " ") + extension;

                await resourceManager.BeginTransaction();
                
                if (File.Exists(filePath))
                {
                    try
                    {
                        // Create resource if it does not exist
                        if (await resourceManager.ResourceExistsAsync(r => r.Title == dto.Title)) continue;
                        Guid resourceId = await resourceManager.CreateResourceAsync(dto);

                        // Then upload to blob storage
                        Dictionary<string, string> metadata = new Dictionary<string, string> { { "extension", extension } };
                        BLOB_STATUSCODE result = await blobService.UploadBlobAsync(fileType, resourceId.ToString(), metadata, File.OpenRead(filePath), false);


                        // If the resource has tags, add them
                        if (record.Tags != null && record.Tags.Length > 0)
                        {
                            // Get the tag names and capitalize all words
                            List<string> tags = record.Tags.Split(',').Select(s => CapitalizeWords(s.Trim())).ToList();

                            foreach (string tagName in tags)
                            {
                                Tag? tag = await resourceManager.GetTagAsync(t => t.Name == tagName);

                                // If the tag does not exist, create it
                                Guid tagId = tag?.Id
                                    ?? await resourceManager.CreateTagAsync(new TagCreateDto
                                    {
                                        Name = tagName,
                                        CreatedBy = systemAdminId,
                                    }, isStandardized: true);

                                // Add the tag to the resource
                                await resourceManager.AddTagToResourceAsync(resourceId, tagId);
                            }
                        }

                        // If the resource has authors, add them
                        if (record.Authors != null && record.Authors.Length > 0)
                        {
                            // Get the author names and capitalize all words
                            List<string> authors = record.Authors.Split(',').Select(s => CapitalizeWords(s.Trim())).ToList();

                            foreach (string authorName in authors)
                            {
                                // Get the author and add it to the resource
                                Person? author = await resourceManager.GetPersonAsync(p => p.Name == authorName);

                                // Add the person to the resource
                                if (author != null)
                                    await resourceManager.AddAuthorToResourceAsync(resourceId, author.Id);
                            }
                        }

                        // If the resource has organisations, add them
                        if (record.ResourceOrganisationRelations != null && record.ResourceOrganisationRelations.Length > 0)
                        {
                            // Get the organisation names and capitalize all words
                            List<string> organisations = record.ResourceOrganisationRelations.Split(',').ToList();

                            foreach (string organisationName in organisations)
                            {
                                // Get the organisation and add it to the resource
                                Organisation? organisation = await resourceManager.GetOrganisationAsync(o => o.Name == organisationName);

                                // Add the organisation to the resource
                                if (organisation != null)
                                    await resourceManager.AddOrganisationToResourceAsync(resourceId, organisation.Id);
                            }
                        }

                        await resourceManager.Commit();
                        await resourceManager.UpdateResourceAsync(resourceId, r => r.FileExt, extension.Replace(".", ""));
                        await resourceManager.UpdateResourceAsync(resourceId, r => r.FileType, fileType);
                    }
                    catch (Exception e)
                    {
                        await resourceManager.Rollback();
                        Serilog.Log.Logger.Error(e, "Error uploading file {FileName}.", record.Title);
                    }
                }
                else
                {
                    Console.WriteLine($"File {filePath} does not exist.");
                }
            }
        }
        
        #endregion
        
        #region Helper methods
        
        /// <summary>
        /// Helper method to capitalize the first letter of each word in a string
        /// </summary>
        /// <param name="input"></param>
        /// <returns></returns>
        private static string CapitalizeWords(string input)
        {
            return string.Join(" ", input
                .Split(' ', StringSplitOptions.RemoveEmptyEntries)
                .Select(word => char.ToUpper(word[0]) + word.Substring(1).ToLower()));
        }
        
        /// <summary>
        /// Helper class to read CSV files and map them to DTOs
        /// </summary>
        private static class CsvReaderHelper
        {        
            public static (List<TInput> Records, List<TOutput> Dtos) ReadCsvFile<TInput, TOutput>(
                string filePath,
                Func<TInput, TOutput> mapFunc)
            {
                CsvConfiguration config = new CsvConfiguration(CultureInfo.InvariantCulture)
                {
                    PrepareHeaderForMatch = args => args.Header.ToLower(),
                    HeaderValidated = null,
                    MissingFieldFound = null
                };

                using StreamReader reader = new StreamReader(testDataPath + filePath);
                using CsvReader csv = new CsvReader(reader, config);

                List<TInput> records = csv.GetRecords<TInput>().ToList();
                List<TOutput> dtos = records.Select(mapFunc).ToList();
                
                return (records, dtos);
            }
        }
        
        /// <summary>
        /// Helper class to map CSV records to PersonCreateDtos
        /// </summary>
        private class PersonCsvRecord
        {
            public string Name { get; set; } = string.Empty;
            public string Occupation { get; set; } = string.Empty;
            [Name("Description")]
            public string Description { get; set; } = string.Empty;
            [Name("Publishing organisation")]
            public string Organisations { get; set; } = string.Empty;
            public string EmailAddress { get; set; } = string.Empty;
            public string Linkedin { get; set; } = string.Empty;
        }

        /// <summary>
        /// Helper class to map CSV records to OrganisationCreateDtos
        /// </summary>
        private class OrganisationCsvRecord
        {
            public string Name { get; set; } = string.Empty;
            public string Description { get; set; } = string.Empty;
            public string Website { get; set; } = string.Empty;
            public string EmailAddress { get; set; } = string.Empty;
            [Name("Geographical scope / region")]
            public string Region { get; set; } = string.Empty;
        }

        /// <summary>
        /// Helper class to map CSV records to ResourceCreateDtos
        /// </summary>
        private class ResourceCsvRecord
        {
            [Name("Title / Name")]
            public string Title { get; set; } = string.Empty;
            public string Description { get; set; } = string.Empty;
            [Name("Source Type")]
            public string TypeId { get; set; } = string.Empty;
            public string LanguageCode { get; set; } = string.Empty;
            [Name("Geographical scope / region")]
            public string GeographicalScope { get; set; } = string.Empty;
            public string Authors { get; set; } = string.Empty;
            public string ResourceOrganisationRelations { get; set; } = string.Empty;
            [Name("Date of publication / recording")]
            public string PublicationDate { get; set; } = string.Empty;
            public string Tags { get; set; } = string.Empty;
            public string Abstract { get; set; } = string.Empty;
            [Name("DOI / ISBN / ISSN")]
            public string Identifier { get; set; } = string.Empty;
            [Name("URL / Source reference")]
            public string SourceUrl { get; set; } = string.Empty;
            [Name("Related persons")]
            public string RelatedPersons { get; set; } = string.Empty;
            [Name("Related Organization")]
            public string RelatedOrganisations { get; set; } = string.Empty;
            [Name("Related Sources")]
            public string RelatedSources { get; set; } = string.Empty;
            [Name("License / Usage Rights")]
            public string License { get; set; } = string.Empty;
            public string Note { get; set; } = string.Empty;
        }
        
        #endregion
    }  
}


// This program has been developed by students from the bachelor Computer Science at Utrecht
// University within the Software Project course.
// © Copyright Utrecht University (Department of Information and Computing Sciences)


