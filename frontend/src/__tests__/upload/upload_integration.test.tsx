import { describe, it, expect, vi, beforeEach } from 'vitest';
import { UploadNewResource, UploadWithDto } from '@/actions/uploadActions'; // Updated import path
import { resourceCreateFormSchema, UploadTypeEnum } from '@/components/new/NewResource';
import { z } from 'zod';
import {
  ResourceCreateDto,
  PersonCreateDto,
  OrganisationCreateDto,
  RelatedEntry
} from '@/types/uploadTypes';

// This test file focuses on integration tests with more complex scenarios

// Mock the fetch API
const mockFetch = vi.fn();
global.fetch = mockFetch;

// Mock FormData with a complete implementation
const mockAppend = vi.fn();
class MockFormData {
  append = mockAppend;
  delete = vi.fn();
  get = vi.fn();
  getAll = vi.fn();
  has = vi.fn();
  set = vi.fn();
  forEach = vi.fn();
  entries = vi.fn();
  keys = vi.fn();
  values = vi.fn();
  [Symbol.iterator] = vi.fn();
}

global.FormData = MockFormData as unknown as typeof FormData;

// UUID generator
const generateUUID = () => {
  return 'xxxxxxxx-xxxx-4xxx-yxxx-xxxxxxxxxxxx'.replace(/[xy]/g, function(c) {
    const r = Math.random() * 16 | 0;
    const v = c === 'x' ? r : (r & 0x3 | 0x8);
    return v.toString(16);
  });
};

// Create a strongly-typed literal for the upload types to avoid string type errors
type UploadTypeLiteral = "document" | "website" | "audio" | "video";

// UUID pattern for validation
const uuidPattern = /^[0-9a-f]{8}-[0-9a-f]{4}-[0-9a-f]{4}-[0-9a-f]{4}-[0-9a-f]{12}$/i;

describe('Upload Functions Integration Tests', () => {
  beforeEach(() => {
    mockFetch.mockClear();
    mockAppend.mockClear();
    vi.clearAllMocks();
  });
  
  it('should handle a complete upload workflow', async () => {
    // First, upload an organization
    const orgDto: OrganisationCreateDto = {
      Name: 'Research Institute',
      Description: 'Leading research organization',
      Website: 'https://research-institute.example.com',
      EmailAddress: 'contact@research-institute.example.com',
      OrganisationRelations: []
    };
    
    // Generate mock response UUIDs
    const orgResponseUuid = generateUUID();
    const personResponseUuid = generateUUID();
    const resourceResponseUuid = generateUUID();
    
    // Mock first response for organization
    mockFetch.mockResolvedValueOnce({
      ok: true,
      json: async () => ({ success: true, body: orgResponseUuid }),
    });
    
    // Upload the organization
    const orgResult = await UploadWithDto('/api/organisations/new', orgDto);
    expect(orgResult).toMatch(uuidPattern);
    
    // Then, upload a person related to the organization
    const personDto: PersonCreateDto = {
      Name: 'Jane Smith',
      Occupation: 'Senior Researcher',
      Description: 'Specializes in data analysis',
      EmailAddress: 'jane.smith@research-institute.example.com',
      Linkedin: 'https://linkedin.com/in/janesmith',
      OrganisationRelations: [
        { Id: orgResult, Relation: 'Employee' }
      ],
      PersonRelations: []
    };
    
    // Mock second response for person
    mockFetch.mockResolvedValueOnce({
      ok: true,
      json: async () => ({ success: true, body: personResponseUuid }),
    });
    
    // Upload the person
    const personResult = await UploadWithDto('/api/persons/new', personDto);
    expect(personResult).toMatch(uuidPattern);
    
    // Finally, upload a resource related to both
    const resourceForm = {
      title: 'Research Paper on Data Analysis',
      description: 'Comprehensive analysis of modern data techniques',
      typeId: generateUUID(), // Assume a resource type ID
      languageCode: 'en',
      publicationCode: 'RI-2023-05',
      publicationDate: '2023-05-15',
      license: 'CC BY-NC-SA',
      sources: ['academic-journal-123'],
      note: 'Published in Journal of Data Science',
      tags: [generateUUID(), generateUUID()], // Assume tag IDs
      authors: [personResult], // Use the person we just created
      organisations: [{
        Id: orgResult,
        Relation: 'Publisher'
      }] as RelatedEntry[],
      regions: [generateUUID()], // Assume a region ID
      relatedOrganisations: [] as RelatedEntry[],
      relatedPersons: [] as RelatedEntry[],
      uploadType: "document" as UploadTypeLiteral,
      url: 'https://example.com', // Required by the schema even for documents
      abstract: 'This paper explores the latest advancements in data analysis techniques...',
      file: new File(['test content'], 'research-paper.pdf', { type: 'application/pdf' }),
      hash: 'sha256:abcdef1234567890',
    };
    
    // Mock third response for resource
    mockFetch.mockResolvedValueOnce({
      ok: true,
      json: async () => ({ success: true, body: resourceResponseUuid }),
    });
    
    // Upload the resource
    const resourceResult = await UploadNewResource(resourceForm as z.infer<typeof resourceCreateFormSchema>);
    expect(resourceResult).toMatch(uuidPattern);
    
    // Verify the sequence of API calls
    expect(mockFetch).toHaveBeenCalledTimes(3);
    expect(mockFetch.mock.calls[0][0]).toBe('/api/organisations/new');
    expect(mockFetch.mock.calls[1][0]).toBe('/api/persons/new');
    expect(mockFetch.mock.calls[2][0]).toBe('/api/resources/new');
  });
  
  it('should handle error recovery in a workflow', async () => {
    // First attempt to upload a person fails
    const personDto: PersonCreateDto = {
      Name: 'John Doe',
      Occupation: 'Researcher',
      Description: 'A test person',
      EmailAddress: 'invalid-email', // Invalid email will cause validation error
      Linkedin: 'https://linkedin.com/in/johndoe',
      OrganisationRelations: [],
      PersonRelations: []
    };
    
    // Mock first request to fail with validation error
    mockFetch.mockResolvedValueOnce({
      ok: true, // API responds with 200 but success: false
      json: async () => ({ success: false, message: 'Invalid email format' }),
    });
    
    // Attempt to upload person should fail
    await expect(UploadWithDto('/api/persons/new', personDto)).rejects.toThrow('Upload failed: Invalid email format');
    
    // Fix the person data and retry
    const fixedPersonDto: PersonCreateDto = {
      ...personDto,
      EmailAddress: 'john.doe@example.com' // Fixed email
    };
    
    const personResponseUuid = generateUUID();
    mockFetch.mockResolvedValueOnce({
      ok: true,
      json: async () => ({ success: true, body: personResponseUuid }),
    });
    
    // Second attempt should succeed
    const personResult = await UploadWithDto('/api/persons/new', fixedPersonDto);
    expect(personResult).toMatch(uuidPattern);
    
    // Verify the sequence of API calls
    expect(mockFetch).toHaveBeenCalledTimes(2);
    expect(mockFetch.mock.calls[0][0]).toBe('/api/persons/new');
    expect(mockFetch.mock.calls[1][0]).toBe('/api/persons/new');
  });
  
  it('should handle server-side errors properly', async () => {
    // Create a resource form
    const resourceForm = {
      title: 'Test Resource',
      description: 'A test resource description',
      typeId: generateUUID(),
      languageCode: 'en',
      publicationCode: 'pub123',
      publicationDate: '2023-05-01',
      license: 'MIT',
      sources: ['source1'],
      note: 'Test note',
      tags: [generateUUID()],
      authors: [generateUUID()],
      organisations: [] as RelatedEntry[],
      regions: [generateUUID()],
      relatedOrganisations: [] as RelatedEntry[],
      relatedPersons: [] as RelatedEntry[],
      uploadType: "document" as UploadTypeLiteral,
      url: 'https://example.com', // Required by the schema even for documents
      abstract: 'Test abstract',
      file: new File(['test content'], 'test.pdf', { type: 'application/pdf' }),
      hash: 'testhash123',
    };
    
    // Mock first request to fail with server error
    mockFetch.mockResolvedValueOnce({
      ok: false,
      status: 500,
      json: async () => ({ success: false, message: 'Internal Server Error' }),
    });
    
    // First attempt should fail
    await expect(UploadNewResource(resourceForm as z.infer<typeof resourceCreateFormSchema>)).rejects.toThrow('Upload failed: 500 Internal Server Error');
    
    // Mock second request to time out
    mockFetch.mockRejectedValueOnce(new Error('Network timeout'));
    
    // Second attempt should fail with network error
    await expect(UploadNewResource(resourceForm as z.infer<typeof resourceCreateFormSchema>)).rejects.toThrow('Network timeout');
    
    // Mock third request to succeed
    const resourceResponseUuid = generateUUID();
    mockFetch.mockResolvedValueOnce({
      ok: true,
      json: async () => ({ success: true, body: resourceResponseUuid }),
    });
    
    // Third attempt should succeed
    const resourceResult = await UploadNewResource(resourceForm as z.infer<typeof resourceCreateFormSchema>);
    expect(resourceResult).toMatch(uuidPattern);
    
    // Verify the sequence of API calls
    expect(mockFetch).toHaveBeenCalledTimes(3);
    expect(mockFetch.mock.calls[0][0]).toBe('/api/resources/new');
    expect(mockFetch.mock.calls[1][0]).toBe('/api/resources/new');
    expect(mockFetch.mock.calls[2][0]).toBe('/api/resources/new');
  });
  
  it('should properly handle file uploads with large files', async () => {
    // Create a mock large file (simulation)
    const largeFileContent = new Array(1024 * 1024).fill('a').join(''); // ~1MB of data
    const largeFile = new File([largeFileContent], 'large-document.pdf', { type: 'application/pdf' });
    
    const resourceForm = {
      title: 'Large Document',
      description: 'Testing large file upload',
      typeId: generateUUID(),
      languageCode: 'en',
      publicationCode: 'large-001',
      publicationDate: '2023-05-01',
      license: 'MIT',
      sources: [],
      note: '',
      tags: [generateUUID()],
      authors: [generateUUID()],
      organisations: [] as RelatedEntry[],
      regions: [],
      relatedOrganisations: [] as RelatedEntry[],
      relatedPersons: [] as RelatedEntry[],
      uploadType: "document",
      url: 'https://example.com', // Required by the schema even for documents
      abstract: 'Abstract for large document',
      file: largeFile,
      hash: 'sha256:largefilehash',
    };
    
    const resourceResponseUuid = generateUUID();
    mockFetch.mockResolvedValueOnce({
      ok: true,
      json: async () => ({ success: true, body: resourceResponseUuid }),
    });
    
    const result = await UploadNewResource(resourceForm as z.infer<typeof resourceCreateFormSchema>);
    expect(result).toMatch(uuidPattern);
    
    // Check that FormData was used correctly
    expect(mockAppend).toHaveBeenCalledWith('file', largeFile);
    expect(mockAppend).toHaveBeenCalledWith('uploadType', 'document');
    expect(mockAppend).toHaveBeenCalledWith('dto', expect.stringContaining('"Abstract":"Abstract for large document"'));
  });
});