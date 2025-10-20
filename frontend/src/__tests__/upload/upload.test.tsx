import { describe, it, expect, vi, beforeEach } from 'vitest';
import { UploadNewResource, UploadWithDto } from '@/actions/uploadActions';
import { resourceCreateFormSchema } from '@/components/new/NewResource';
import { z } from 'zod';
import {
  PersonCreateDto,
  OrganisationCreateDto,
  ResourceTypeCreateDto,
  RegionCreateDto,
  ResourceCreateDto
} from '@/types/uploadTypes';

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

// UUID generator for testing - creates valid UUIDs
const generateUUID = () => {
  return 'xxxxxxxx-xxxx-4xxx-yxxx-xxxxxxxxxxxx'.replace(/[xy]/g, function(c) {
    const r = Math.random() * 16 | 0;
    const v = c === 'x' ? r : (r & 0x3 | 0x8);
    return v.toString(16);
  });
};

// UUID pattern for testing
const uuidPattern = /^[0-9a-f]{8}-[0-9a-f]{4}-[0-9a-f]{4}-[0-9a-f]{4}-[0-9a-f]{12}$/i;

describe('UploadNewResource', () => {
  // Sample form data for testing that matches the Zod schema requirements
  const mockForm: z.infer<typeof resourceCreateFormSchema> = {
    title: 'Test Resource',
    description: 'A test resource description',
    typeId: generateUUID(),
    languageCode: 'en',
    publicationCode: 'pub123',
    publicationDate: '2023-05-01',
    license: 'MIT',
    sources: ['source1', 'source2'],
    note: 'Test note',
    tags: [generateUUID(), generateUUID()],
    authors: [generateUUID(), generateUUID()],
    organisations: [{
      Id: generateUUID(),
      Relation: 'Publisher'
    }],
    regions: [generateUUID(), generateUUID()],
    relatedOrganisations: [{
      Id: generateUUID(),
      Relation: 'Sponsor'
    }],
    relatedPersons: [{
      Id: generateUUID(),
      Relation: 'Contributor'
    }],
    uploadType: 'document',
    abstract: 'Test abstract',
    file: new File(['test content'], 'test.pdf', { type: 'application/pdf' }),
    hash: 'testhash123',
    url: 'http://no.url/',
  };

  beforeEach(() => {
    mockFetch.mockClear();
    vi.clearAllMocks();
  });

  it('should upload a document resource and return a UUID', async () => {
    // Mock successful response with UUID
    const mockResponseUuid = generateUUID();
    mockFetch.mockResolvedValueOnce({
      ok: true,
      json: async () => ({ success: true, body: mockResponseUuid }),
    });

    const result = await UploadNewResource(mockForm);
    
    // Check that fetch was called with correct arguments
    expect(mockFetch).toHaveBeenCalledWith('/api/resources/new', expect.objectContaining({
      method: 'PUT',
      credentials: 'include',
      body: expect.any(FormData),
    }));
    
    // Check that result matches UUID pattern
    expect(result).toMatch(uuidPattern);
  });

  it('should upload a website resource correctly', async () => {
    // Create a form for website upload
    const websiteForm = {
      ...mockForm,
      uploadType: 'website' as const,
      url: 'https://example.com',
      accessedOn: '2023-05-02',
    };
    
    const mockResponseUuid = generateUUID();
    mockFetch.mockResolvedValueOnce({
      ok: true,
      json: async () => ({ success: true, body: mockResponseUuid }),
    });

    const result = await UploadNewResource(websiteForm);
    
    // Verify the result matches UUID pattern
    expect(result).toMatch(uuidPattern);
    
    // Check that FormData includes the website-specific properties
    expect(mockFetch).toHaveBeenCalled();
    // Check that the append method was called with the right arguments
    expect(mockAppend).toHaveBeenCalledWith('uploadType', 'website');
    expect(mockAppend).toHaveBeenCalledWith('dto', expect.stringContaining('"Url":"https://example.com"'));
  });

  it('should upload a video resource correctly', async () => {
    // Create a form for video upload
    const videoForm = {
      ...mockForm,
      uploadType: 'video' as const,
      length: 120, // 2 minutes in seconds
    };
    
    const mockResponseUuid = generateUUID();
    mockFetch.mockResolvedValueOnce({
      ok: true,
      json: async () => ({ success: true, body: mockResponseUuid }),
    });

    const result = await UploadNewResource(videoForm);
    
    // Verify the result matches UUID pattern
    expect(result).toMatch(uuidPattern);
    
    // Check that FormData includes the video-specific properties
    expect(mockAppend).toHaveBeenCalledWith('uploadType', 'video');
    expect(mockAppend).toHaveBeenCalledWith('dto', expect.stringContaining('"Length":120'));
  });

  it('should upload an audio resource correctly', async () => {
    // Create a form for audio upload
    const audioForm = {
      ...mockForm,
      uploadType: 'audio' as const,
      length: 180, // 3 minutes in seconds
    };
    
    const mockResponseUuid = generateUUID();
    mockFetch.mockResolvedValueOnce({
      ok: true,
      json: async () => ({ success: true, body: mockResponseUuid }),
    });

    const result = await UploadNewResource(audioForm);
    
    // Verify the result matches UUID pattern
    expect(result).toMatch(uuidPattern);
    
    // Check that FormData includes the audio-specific properties
    expect(mockAppend).toHaveBeenCalledWith('uploadType', 'audio');
    expect(mockAppend).toHaveBeenCalledWith('dto', expect.stringContaining('"Length":180'));
  });

  it('should properly convert dates to UTC format', async () => {
    // Create a form with date to test conversion
    const formWithDate = {
      ...mockForm,
      publicationDate: '2023-05-01',
      accessedOn: '2023-05-02',
      uploadType: 'website' as const,
      url: 'https://example.com',
    };
    
    const mockUuid = generateUUID();
    mockFetch.mockResolvedValueOnce({
      ok: true,
      json: async () => ({ success: true, body: mockUuid }),
    });

    await UploadNewResource(formWithDate);
    
    // Check that the date was converted by inspecting the DTO JSON
    const dtoCall = mockAppend.mock.calls.find(call => call[0] === 'dto');
    
    expect(dtoCall).toBeDefined();
    
    // The date should be in ISO format (contains 'T' and 'Z')
    if (dtoCall) 
    {
        const dtoJson = dtoCall[1];
        expect(dtoJson).toContain('T');
        expect(dtoJson).toContain('Z');
    }
  });

  it('should throw an error when the response is not ok', async () => {
    mockFetch.mockResolvedValueOnce({
      ok: false,
      status: 400,
      json: async () => ({ success: false, message: 'Bad Request' }),
    });

    await expect(UploadNewResource(mockForm)).rejects.toThrow('Upload failed: 400 Bad Request');
  });

  it('should throw an error when the result is not successful', async () => {
    mockFetch.mockResolvedValueOnce({
      ok: true,
      json: async () => ({ success: false, message: 'Validation failed' }),
    });

    await expect(UploadNewResource(mockForm)).rejects.toThrow('Upload failed: Validation failed');
  });

  it('should handle network errors', async () => {
    mockFetch.mockRejectedValueOnce(new Error('Network error'));
    await expect(UploadNewResource(mockForm)).rejects.toThrow('Network error');
  });
});

describe('UploadWithDto', () => {
  beforeEach(() => {
    mockFetch.mockClear();
  });

  it('should upload a PersonCreateDto and return a UUID', async () => {
    const personDto: PersonCreateDto = {
      Name: 'John Doe',
      Occupation: 'Researcher',
      Description: 'A test person',
      EmailAddress: 'john.doe@example.com',
      Linkedin: 'https://linkedin.com/in/johndoe',
      OrganisationRelations: [
        { Id: generateUUID(), Relation: 'Employee' }
      ],
      PersonRelations: [
        { Id: generateUUID(), Relation: 'Colleague' }
      ]
    };
    
    // The actual UUID that would be returned by the backend
    const mockResponseUuid = generateUUID();
    mockFetch.mockResolvedValueOnce({
      ok: true,
      json: async () => ({ success: true, body: mockResponseUuid }),
    });

    const result = await UploadWithDto('/api/persons/new', personDto);
    
    // Check that fetch was called with correct arguments
    expect(mockFetch).toHaveBeenCalledWith('/api/persons/new', {
      method: 'PUT',
      credentials: 'include',
      headers: { 'Content-Type': 'application/json' },
      body: JSON.stringify(personDto),
    });
    
    // We don't expect a specific UUID, just that it's a valid UUID
    expect(result).toMatch(uuidPattern);
  });

  it('should upload an OrganisationCreateDto and return a UUID', async () => {
    const organisationDto: OrganisationCreateDto = {
      Name: 'Test Organisation',
      Description: 'A test organisation',
      Website: 'https://testorg.example.com',
      EmailAddress: 'info@testorg.example.com',
      OrganisationRelations: [
        { Id: generateUUID(), Relation: 'Partner' }
      ]
    };
    
    // The actual UUID that would be returned by the backend
    const mockResponseUuid = generateUUID();
    mockFetch.mockResolvedValueOnce({
      ok: true,
      json: async () => ({ success: true, body: mockResponseUuid }),
    });

    const result = await UploadWithDto('/api/organisations/new', organisationDto);
    
    expect(mockFetch).toHaveBeenCalledWith('/api/organisations/new', {
      method: 'PUT',
      credentials: 'include',
      headers: { 'Content-Type': 'application/json' },
      body: JSON.stringify(organisationDto),
    });
    
    // Just verify it's a valid UUID pattern
    expect(result).toMatch(uuidPattern);
  });

  it('should upload a ResourceTypeCreateDto and return a UUID', async () => {
    const resourceTypeDto: ResourceTypeCreateDto = {
      Name: 'Academic Paper'
    };
    
    // The actual UUID that would be returned by the backend
    const mockResponseUuid = generateUUID();
    mockFetch.mockResolvedValueOnce({
      ok: true,
      json: async () => ({ success: true, body: mockResponseUuid }),
    });

    const result = await UploadWithDto('/api/resourcetypes/new', resourceTypeDto);
    
    expect(mockFetch).toHaveBeenCalledWith('/api/resourcetypes/new', {
      method: 'PUT',
      credentials: 'include',
      headers: { 'Content-Type': 'application/json' },
      body: JSON.stringify(resourceTypeDto),
    });
    
    // Just verify it's a valid UUID
    expect(result).toMatch(uuidPattern);
  });

  it('should upload a RegionCreateDto and return a UUID', async () => {
    const regionDto: RegionCreateDto = {
      Name: 'Western Europe'
    };
    
    // The actual UUID that would be returned by the backend
    const mockResponseUuid = generateUUID();
    mockFetch.mockResolvedValueOnce({
      ok: true,
      json: async () => ({ success: true, body: mockResponseUuid }),
    });

    const result = await UploadWithDto('/api/regions/new', regionDto);
    
    expect(mockFetch).toHaveBeenCalledWith('/api/regions/new', {
      method: 'PUT',
      credentials: 'include',
      headers: { 'Content-Type': 'application/json' },
      body: JSON.stringify(regionDto),
    });
    
    // Just verify it's a valid UUID
    expect(result).toMatch(uuidPattern);
  });

  it('should throw an error when the response is not ok', async () => {
    const resourceTypeDto: ResourceTypeCreateDto = {
      Name: 'Invalid Type'
    };

    mockFetch.mockResolvedValueOnce({
      ok: false,
      status: 500,
      json: async () => ({ success: false, message: 'Internal Server Error' }),
    });

    await expect(UploadWithDto('/api/resourcetypes/new', resourceTypeDto)).rejects.toThrow('Upload failed: 500 Internal Server Error');
  });

  it('should throw an error when the result is not successful', async () => {
    const regionDto: RegionCreateDto = {
      Name: 'Duplicate Region'
    };

    mockFetch.mockResolvedValueOnce({
      ok: true,
      json: async () => ({ success: false, message: 'Entity already exists' }),
    });

    await expect(UploadWithDto('/api/regions/new', regionDto)).rejects.toThrow('Upload failed: Entity already exists');
  });

  it('should handle network errors', async () => {
    const organisationDto: OrganisationCreateDto = {
      Name: 'Network Error Test',
      Description: 'Testing network errors',
      OrganisationRelations: []
    };

    mockFetch.mockRejectedValueOnce(new Error('Network error'));
    await expect(UploadWithDto('/api/organisations/new', organisationDto)).rejects.toThrow('Network error');
  });
});

// This program has been developed by students from the bachelor Computer Science at Utrecht
// University within the Software Project course.
// © Copyright Utrecht University (Department of Information and Computing Sciences)


