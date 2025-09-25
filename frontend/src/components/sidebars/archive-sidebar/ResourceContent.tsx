"use client";

import Expandable from "./expandable";
import BadgeList from "./BadgeList";
import { ListItem } from "./BadgeList";
import { useArchiveSidebar, MetadataTypeEnum } from "@/context/archive-sidebar-provider";
import { getRelation } from "@/actions/right-sidebarActions";
import React from "react";
import Skeleton from "react-loading-skeleton";
import "react-loading-skeleton/dist/skeleton.css";
import ResourceList from "./ResourceList";
import { Button } from "@/components/ui/button";
import { useUserRole } from "@/context/user-role-context";
import ConfirmDeleteDialog from "@/components/ui/confirm-delete-dialog";
import { ApiResponse } from "@/types/apiResponse.type";
import { useArchive } from "@/context/archive-provider";
import Edit from "./Edit";
import { TrashResource } from "@/actions/trashResourceActions";
import { Badge } from "@/components/ui/badge";
import GetFileIcon from "@/components/getFileIcon";

interface ResourceContentProps {
  fileType: string | null;
  setFileType: (type: string | null) => void;
}

export function ResourceContent({ fileType, setFileType }: ResourceContentProps) {
  const { currentId, archiveSidebarOpen, setArchiveSidebarOpen, setCreationDate, setPublicationDate, setEditMode, editMode } = useArchiveSidebar();
  const { userRole } = useUserRole();
  const { triggerGridReload, trashOpen } = useArchive();
  const [confirmDialogOpen, setConfirmDialogOpen] = React.useState<boolean>(false);

  const [title, setTitle] = React.useState<string | null>(null);
  const [url, setUrl] = React.useState<string | undefined>(undefined);
  const [description, setDescription] = React.useState<string | null>(null);
  const [note, setNote] = React.useState<string | null>(null);
  const [authors, setAuthors] = React.useState<ListItem[] | null>(null);
  const [tags, setTags] = React.useState<ListItem[] | null>(null);
  const [aiTags, setAiTags] = React.useState<ListItem[] | null>(null);
  const [organisations, setOrganisations] = React.useState<ListItem[] | null>(null);
  const [relatedOrganisations, setRelatedOrganisations] = React.useState<ListItem[] | null>(null);
  const [relatedPersons, setRelatedPersons] = React.useState<ListItem[] | null>(null);
  const [relatedResources, setRelatedResources] = React.useState<ListItem[] | null>(null);
  const [sourceList, setSourceList] = React.useState<ListItem[] | null>(null);
  const [regions, setRegions] = React.useState<ListItem[] | null>(null);
  const [langCode, setLangCode] = React.useState<string | null>(null);
  const [pubCode, setPubCode] = React.useState<string | null>(null);
  const [license, setLicense] = React.useState<string | null>(null);
  const [trashed, setTrashed] = React.useState<boolean>(false);
  const [abstract, setAbstract] = React.useState<string | null>(null);

  // Loads all content at once
  const loadContent = React.useCallback(async () => {
    const params: URLSearchParams = new URLSearchParams();

    // Define properties to get
    params.append(
      "properties",
      `
            Title,
            Description,
            LanguageCode,
            PublicationCode,
            PublicationDate,
            License,
            CreationDate,
            Note,
            FileType,
            Trashed,
            WebsiteMetadata.Url as Url,
            DocumentMetadata.Abstract as Abstract,
            ResourceAuthorRelations.Select(new(Author.Id, Author.Name)) as Authors,
            ResourceOrganisationRelations.Select(new(Organisation.Id, Organisation.Name)) as Organisations,
            ResourceRegionRelations.Select(new(Region.Id, Region.Name)) as Regions,
            ResourceRelatedPersonRelations.Select(new(Person.Id, Person.Name)) as RelatedPersons,
            ResourceRelatedOrganisationRelations.Select(new(Organisation.Id, Organisation.Name)) as RelatedOrganisations,
            ResourceSourceRelations.Select(new(Url as Id, Url as Name)) as Sources,
            ResourceTagRelations.Select(new(Tag.Id, Tag.Name)) as Tags
        `,
    );

    // Fetch
    const response = await fetch(`/api/resources/info/${currentId}?${params.toString()}`, {
      method: "GET",
      credentials: "include",
    });

    // Set all states on success
    if (response.ok) {
      const data: ApiResponse = await response.json();

      setTitle(data.body.title || "Title missing.");
      setDescription(data.body.description || "No description.");
      setLangCode(data.body.languageCode || " Language unknown.");
      setPubCode(data.body.publicationCode || "Unknown.");
      setPublicationDate(data.body.publicationDate || "Unknown");
      setCreationDate(data.body.creationDate || "Unknown.");
      setNote(data.body.note || "No notes.");
      setFileType(data.body.fileType || "Unknown.");
      setTrashed(data.body.trashed);
      setUrl(data.body.url || "No URL found.");
      setAbstract(data.body.abstract || null);
      setAuthors(data.body.authors || []);
      setOrganisations(data.body.organisations || []);
      setRegions(data.body.regions || []);
      setRelatedPersons(data.body.relatedPersons || []);
      setRelatedOrganisations(data.body.relatedOrganisations || []);
      setSourceList(data.body.sources || []);
      setTags(data.body.tags || []);
      setLicense(data.body.license || "Unknown.");
    }
  }, [currentId, setCreationDate, setFileType, setPublicationDate]);

  // Loads the related resources
  const loadRelatedResources = React.useCallback(async () => {
    const relatedResourcesPromise = getRelation(currentId, MetadataTypeEnum.RESOURCE, "resource-related-resources");

    relatedResourcesPromise
      .then((response) => {
        const list: ListItem[] = response.body.map((item: { id: string; title: string; fileType: string }) => ({
          id: item.id,
          name: item.title,
          type: item.fileType,
        }));
        setRelatedResources(list);
      })
      .catch((error) => {
        console.log("Error loading related resources: ", error);
        setRelatedResources([]);
      });
  }, [currentId]);

  // Loads the AI tags
  const loadAiTags = React.useCallback(async () => {
    const aiTagsPromise = getRelation(currentId, MetadataTypeEnum.RESOURCE, "ai-tags");

    aiTagsPromise
      .then((response) => {
        // Check if we got an array back, if not dont continue
        if (!Array.isArray(response.body.tags)) {
          console.log("This document is not processed. No AI tags available.");
          return;
        }

        //Add translation logic from ai tags to ListItem[] here
        const list: ListItem[] = response.body.tags.map((item: string) => ({
          id: item,
          name: item,
          type: "ai-tag",
        }));

        const finalList = list.length > 10 ? list.sort(() => 0.5 - Math.random()).slice(0, 10) : list;

        setAiTags(finalList);
      })
      .catch((error) => {
        console.error("Error loading tags: ", error);
      });
  }, [currentId]);

  // Reload content on sidebar open
  React.useEffect(() => {
    if (archiveSidebarOpen) {
      // Clear everything
      setFileType(null);
      setUrl(undefined);
      setTitle(null);
      setDescription(null);
      setNote(null);
      setAuthors(null);
      setTags(null);
      setAiTags(null);
      setOrganisations(null);
      setRelatedOrganisations(null);
      setRelatedPersons(null);
      setRelatedResources(null);
      setSourceList(null);
      setRegions(null);
      setLangCode(null);
      setPubCode(null);
      setPublicationDate(null);
      setCreationDate(null);
      setLicense(null);

      // Load content
      loadContent();
      loadRelatedResources();
      loadAiTags();
    }
  }, [currentId, loadContent, loadRelatedResources, loadAiTags, archiveSidebarOpen, setPublicationDate, setCreationDate, setFileType]);

  // Deletes resource
  const confirmDelete = async () => {
    if (await TrashResource(currentId, MetadataTypeEnum.RESOURCE)) {
      setArchiveSidebarOpen(false);
      triggerGridReload();
    }
  };

  return (
    <>
      <ConfirmDeleteDialog open={confirmDialogOpen} onOpenChange={setConfirmDialogOpen} onConfirmation={confirmDelete} />

      {/* Banner for when resource is in trash */}
      {trashed && (
        <Badge variant="outline" className="mb-5 flex w-full flex-col border-red-500 text-red-500">
          <h1 className="text-xl">This item is in the trash.</h1>
          <span className="mb-1 flex-1">Contact an admin if you think this is a mistake.</span>
        </Badge>
      )}

      <div className="flex items-center justify-start gap-2 pb-2">
        <GetFileIcon fileType={fileType ?? ""} className="h-5 w-5" />
        {editMode && (
          <div className="mt-1 flex justify-center text-sm select-none">
            <Edit setNewText={setTitle} currentText={title} property="title" />
          </div>
        )}
        <h1 className="text-2xl font-bold select-none">{title || <Skeleton />}</h1>
      </div>

      {fileType === "website" && (
        <a href={url} className="select-none" target="_blank" rel="noreferror">
          <h1 className="mb-8 text-blue-500 underline select-none">{url}</h1>
        </a>
      )}

      {langCode}

      <Expandable title="Description" collapsedHeight={100}>
        {editMode && (
          <div className="mt-1 flex justify-center text-sm select-none">
            <Edit setNewText={setDescription} currentText={description} property="description" />
          </div>
        )}
        {description || <Skeleton />}
      </Expandable>

      <Expandable variant="horizontal" title="Tags">
        <BadgeList
          listType="tags"
          itemList={tags}
          onNew={(newItems) => setTags(tags ? tags.concat(newItems) : newItems)}
          onRemove={(removedItem) => setTags(tags ? tags.filter((item) => item != removedItem) : [])}
        />
      </Expandable>

      <Expandable variant="horizontal" title="Recommended Tags">
        <BadgeList listType="ai-tags" itemList={aiTags} onNew={(newItems) => setTags(tags ? tags.concat(newItems) : newItems)} onRemove={() => {}} />
      </Expandable>

      <Expandable variant="horizontal" title="Authors">
        <BadgeList
          listType="authors"
          itemList={authors}
          onNew={(newItems) => setAuthors(authors ? authors.concat(newItems) : newItems)}
          onRemove={(removedItem) => setAuthors(authors ? authors.filter((item) => item != removedItem) : [])}
        />
      </Expandable>

      <Expandable variant="horizontal" title="Organisations">
        <BadgeList
          listType="organisations"
          itemList={organisations}
          onNew={(newItems) => setOrganisations(organisations ? organisations.concat(newItems) : newItems)}
          onRemove={(removedItem) => setOrganisations(organisations ? organisations.filter((item) => item != removedItem) : [])}
        />
      </Expandable>

      <Expandable variant="horizontal" title="Related People">
        <BadgeList
          listType="related-persons"
          itemList={relatedPersons}
          onNew={(newItems) => setRelatedPersons(relatedPersons ? relatedPersons.concat(newItems) : newItems)}
          onRemove={(removedItem) => setRelatedPersons(relatedPersons ? relatedPersons.filter((item) => item != removedItem) : [])}
        />
      </Expandable>

      <Expandable variant="horizontal" title="Related Organisations">
        <BadgeList
          listType="related-organisations"
          itemList={relatedOrganisations}
          onNew={(newItems) => setRelatedOrganisations(relatedOrganisations ? relatedOrganisations.concat(newItems) : newItems)}
          onRemove={(removedItem) => setRelatedOrganisations(relatedOrganisations ? relatedOrganisations.filter((item) => item != removedItem) : [])}
        />
      </Expandable>

      <ResourceList header="Related Resources" resources={relatedResources} />

      <Expandable variant="horizontal" title="Sources">
        <BadgeList
          listType="sources"
          itemList={sourceList}
          onNew={(newItems) => setSourceList(sourceList ? sourceList.concat(newItems) : newItems)}
          onRemove={(removedItem) => setSourceList(sourceList ? sourceList.filter((item) => item != removedItem) : [])}
        />
      </Expandable>

      <Expandable variant="horizontal" title="Regions">
        <BadgeList
          listType="regions"
          itemList={regions}
          onNew={(newItems) => setRegions(regions ? regions.concat(newItems) : newItems)}
          onRemove={(removedItem) => setRegions(regions ? regions.filter((item) => item != removedItem) : [])}
        />
      </Expandable>

      {abstract && (
        <Expandable title="Abstract" collapsedHeight={100}>
          {editMode && (
            <div className="mt-1 flex justify-center text-sm select-none">
              <Edit setNewText={setAbstract} currentText={abstract} property="abstract" />
            </div>
          )}
          {abstract || <Skeleton />}
        </Expandable>
      )}

      <Expandable title="Notes" collapsedHeight={100}>
        {editMode && (
          <div className="mt-1 flex justify-center text-sm select-none">
            <Edit setNewText={setNote} currentText={note} property="note" />
          </div>
        )}
        {note || <Skeleton />}
      </Expandable>

      <Expandable title="Publication Code" collapsedHeight={100}>
        {editMode && (
          <div className="mt-1 flex justify-center text-sm select-none">
            <Edit setNewText={setPubCode} currentText={pubCode} property="publicationCode" />
          </div>
        )}
        {pubCode || <Skeleton />}
      </Expandable>

      <Expandable title="License Code" collapsedHeight={100}>
        {editMode && (
          <div className="mt-1 flex justify-center text-sm select-none">
            <Edit setNewText={setLicense} currentText={license} property="license" />
          </div>
        )}
        {license || <Skeleton />}
      </Expandable>

      <div className="mt-10 flex w-full justify-center">
        <div className="flex gap-4">
          <Button
            onClick={() => setEditMode(!editMode)}
            variant={editMode ? "default" : "outline"}
            className={editMode ? "bg-green-600 text-white hover:bg-green-700" : "border-gray-300 text-gray-700 hover:bg-gray-100"}
          >
            {editMode ? "Disable Edit Mode" : "Enable Edit Mode"}
          </Button>

          {userRole === "admin" && !trashOpen && (
            <Button onClick={() => setConfirmDialogOpen(true)} variant="outline" className="border-red-500 text-red-500 hover:border-red-600 hover:bg-red-50 hover:text-red-600">
              Delete Resource
            </Button>
          )}
        </div>
      </div>
    </>
  );
}

// This program has been developed by students from the bachelor Computer Science at Utrecht
// University within the Software Project course.
// © Copyright Utrecht University (Department of Information and Computing Sciences)
