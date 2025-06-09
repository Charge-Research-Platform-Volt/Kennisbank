"use client";

import Expandable from "./expandable";
import BadgeList from "./BadgeList";
import { ListItem } from "./BadgeList";
import { useSidebar, MetadataTypeEnum } from "@/context/sidebar-provider";
import { getProperties, getRelation } from "@/actions/right-sidebarActions";
import React, { useState, useEffect } from "react";
import Skeleton from "react-loading-skeleton";
import "react-loading-skeleton/dist/skeleton.css";
import ResourceList from "./ResourceList";
import { Button } from "@/components/ui/button";
import { useUserRole } from "@/context/user-role-context";
import ConfirmDeleteDialog from "@/components/ui/confirm-delete-dialog";
import { toast } from "sonner";
import { ApiResponse } from "@/types/apiResponse.type";
import { useArchive } from "@/context/archive-provider";
import Divider from "../divider";

export function ResourceContent() {
  const { currentId, rightSidebarOpen, currentType, setRightSidebarOpen } = useSidebar();
  const { userRole } = useUserRole();
  const { triggerGridReload } = useArchive();
  const [confirmDialogOpen, setConfirmDialogOpen] = useState<boolean>(false);

  const [fileType, setFileType] = useState<string | null>(null);
  const [title, setTitle] = useState<string | null>(null);
  const [url, setUrl] = useState<string | undefined>(undefined);
  const [description, setDescription] = useState<string | null>(null);
  const [note, setNote] = useState<string | null>(null);
  const [authors, setAuthors] = useState<ListItem[] | null>(null);
  const [tags, setTags] = useState<ListItem[] | null>(null);
  const [aiTags, setAiTags] = useState<ListItem[] | null>(null);
  const [organisations, setOrganisations] = useState<ListItem[] | null>(null);
  const [relatedOrganisations, setRelatedOrganisations] = useState<ListItem[] | null>(null);
  const [relatedPersons, setRelatedPersons] = useState<ListItem[] | null>(null);
  const [relatedResources, setRelatedResources] = useState<ListItem[] | null>(null);
  const [sourceList, setSourceList] = useState<ListItem[] | null>(null);
  const [regions, setRegions] = useState<ListItem[] | null>(null);
  const [relatedSourceList, setRelatedSourceList] = useState<ListItem[] | null>(null);
  const [langCode, setLangCode] = useState<string | null>(null); //ToDo
  const [pubCode, setPubCode] = useState<string | null>(null); //ToDo
  const [pubDate, setPubDate] = useState<Date | null>(null); //ToDo
  const [creationDate, setCreationDate] = useState<Date | null>(null); //ToDo
  const [license, setLicense] = useState<string | null>(null); //ToDo

  const [authorsTrigger, setAuthorsTrigger] = useState(false);
  const [tagsTrigger, setTagsTrigger] = useState(false);
  const [organisationsTrigger, setOrganisationsTrigger] = useState(false);
  const [relatedOrganisationTrigger, setRelatedOrganisationTrigger] = useState(false);
  const [relatedPersonsTrigger, setRelatedPersonsTrigger] = useState(false);
  const [regionsTrigger, setRegionsTrigger] = useState(false);
  const [relatedSourceListTrigger, setRelatedSourceListTrigger] = useState(false);
  const [aiTagsTrigger, setAiTagsTrigger] = useState(false);
  const [sourceListTrigger, setSourceListTrigger] = useState(false);

  const triggerAuthorsRefresh = () => setAuthorsTrigger((prev) => !prev);
  const triggerTagsRefresh = () => setTagsTrigger((prev) => !prev);
  const triggerOrganisationsRefresh = () => setOrganisationsTrigger((prev) => !prev);
  const triggerRelatedOrganisationRefresh = () => setRelatedOrganisationTrigger((prev) => !prev);
  const triggerRelatedPersonsRefresh = () => setRelatedPersonsTrigger((prev) => !prev);
  const triggerRegionsRefresh = () => setRegionsTrigger((prev) => !prev);
  const triggerRelatedSourceListRefresh = () => setRelatedSourceListTrigger((prev) => !prev);
  const triggerAiTagsRefresh = () => setAiTagsTrigger((prev) => !prev);
  const triggerSourceListRefresh = () => setSourceListTrigger((prev) => !prev);

  useEffect(() => {
    if (rightSidebarOpen) {
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
      setRelatedSourceList(null);
      setRelatedPersons(null);
      setRelatedResources(null);
      setSourceList(null);
      setRegions(null);
      setLangCode(null);
      setPubCode(null);
      setPubDate(null);
      setCreationDate(null);
      setLicense(null);

      loadProperties();
      loadAuthors();
      loadTags();
      loadOrganisations();
      loadRelatedOrganisation();
      loadRelatedPersons();
      loadRelatedResources();
      loadSourceList();
      loadRegions();
      loadRelatedSourceList();
      loadAiTags();
    }
  }, [currentId, rightSidebarOpen]);

  useEffect(() => {
    if (rightSidebarOpen) {
      setAuthors(null);
      loadAuthors();
    }
  }, [authorsTrigger]);
  useEffect(() => {
    if (rightSidebarOpen) {
      setTags(null);
      loadTags();
    }
  }, [tagsTrigger]);
  useEffect(() => {
    if (rightSidebarOpen) {
      setOrganisations(null);
      loadOrganisations();
    }
  }, [organisationsTrigger]);
  useEffect(() => {
    if (rightSidebarOpen) {
      setRelatedOrganisations(null);
      loadRelatedOrganisation();
    }
  }, [relatedOrganisationTrigger]);
  useEffect(() => {
    if (rightSidebarOpen) {
      setRelatedPersons(null);
      loadRelatedPersons();
    }
  }, [relatedPersonsTrigger]);
  useEffect(() => {
    if (rightSidebarOpen) {
      setRegions(null);
      loadRegions();
    }
  }, [regionsTrigger]);
  useEffect(() => {
    if (rightSidebarOpen) {
      setRelatedSourceList(null);
      loadRelatedSourceList();
    }
  }, [relatedSourceListTrigger]);
  useEffect(() => {
    if (rightSidebarOpen) {
      setAiTags(null);
      loadAiTags();
    }
  }, [aiTagsTrigger]);
  useEffect(() => {
    if (rightSidebarOpen) {
      setSourceList(null);
      loadSourceList();
    }
  }, [sourceListTrigger]);

  const loadProperties = async () => {
    const infoPromise = getProperties(currentId, MetadataTypeEnum.RESOURCE);

    infoPromise
      .then((response) => {
        setFileType(response.body.fileType);
        if (response.body.fileType === "website") {
          const websitePromise = getRelation(currentId, MetadataTypeEnum.RESOURCE, "website");

          websitePromise
            .then((response) => {
              setUrl(response.body.url);
            })
            .catch((error) => {
              console.error("Error loading url: ", error);
            });
        }
        setLangCode(response.body.languageCode);
        setPubDate(response.body.publicationDate);
        setCreationDate(response.body.creationDate);
        setTitle(response.body.title);
        if (response.body.description) {
          setDescription(response.body.description);
        } else {
          setDescription("No description.");
        }

        if (response.body.note) {
          setNote(response.body.note);
        } else {
          setNote("No notes.");
        }

        if (response.body.publicationCode) {
          setPubCode(response.body.publicationCode);
        } else {
          setPubCode("No Publication Code");
        }

        if (response.body.license) {
          setLicense(response.body.license);
        } else {
          setLicense("No License");
        }
      })
      .catch((error) => {
        console.error("Error loading information: ", error);
      });
  };

  const loadAuthors = async () => {
    const authorsPromise = getRelation(currentId, MetadataTypeEnum.RESOURCE, "authors");

    authorsPromise
      .then((response) => {
        const list: ListItem[] = response.body.map((item: { id: string; name: string }) => ({
          id: item.id,
          name: item.name,
          type: "person",
        }));
        setAuthors(list);
      })
      .catch((error) => {
        console.error("Error loading authors: ", error);
      });
  };

  const loadTags = async () => {
    const tagsPromise = getRelation(currentId, MetadataTypeEnum.RESOURCE, "tags");

    tagsPromise
      .then((response) => {
        const list: ListItem[] = response.body.map((item: { id: string; name: string }) => ({
          id: item.id,
          name: item.name,
          type: "tag",
        }));
        setTags(list);
      })
      .catch((error) => {
        console.error("Error loading tags: ", error);
      });
  };

  const loadOrganisations = async () => {
    const organisationsPromise = getRelation(currentId, MetadataTypeEnum.RESOURCE, "organisations");

    organisationsPromise
      .then((response) => {
        const list: ListItem[] = response.body.map((item: { id: string; name: string }) => ({
          id: item.id,
          name: item.name,
          type: "organisation",
        }));
        setOrganisations(list);
      })
      .catch((error) => {
        console.error("Error loading organisations: ", error);
      });
  };

  const loadRelatedOrganisation = async () => {
    const relatedOrganisationsPromise = getRelation(currentId, MetadataTypeEnum.RESOURCE, "related-organisations");

    relatedOrganisationsPromise
      .then((response) => {
        const list: ListItem[] = response.body.map((item: { id: string; name: string }) => ({
          id: item.id,
          name: item.name,
          type: "organisation",
        }));
        setRelatedOrganisations(list);
      })
      .catch((error) => {
        console.error("Error loading related organisations: ", error);
      });
  };

  const loadRelatedPersons = async () => {
    const relatedPersonsPromise = getRelation(currentId, MetadataTypeEnum.RESOURCE, "related-persons");

    relatedPersonsPromise
      .then((response) => {
        const list: ListItem[] = response.body.map((item: { id: any; name: any }) => ({
          id: item.id,
          name: item.name,
          type: "person",
        }));
        setRelatedPersons(list);
      })
      .catch((error) => {
        console.error("Error loading related persons: ", error);
      });
  };

  const loadRelatedResources = async () => {
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
        console.error("Error loading related resources: ", error);
      });
  };

  const loadSourceList = async () => {
    const sourceListPromise = getRelation(currentId, MetadataTypeEnum.RESOURCE, "sources");

    sourceListPromise
      .then((response) => {
        const list: ListItem[] = response.body.map((item: { id: string; name: string }) => ({
          id: decodeURIComponent(item.id),
          name: decodeURIComponent(item.name),
          type: "source",
        }));
        setSourceList(list);
      })
      .catch((error) => {
        console.error("Error loading sources: ", error);
      });
  };

  const loadRegions = async () => {
    const regionsPromise = getRelation(currentId, MetadataTypeEnum.RESOURCE, "regions");

    regionsPromise
      .then((response) => {
        const list: ListItem[] = response.body.map((item: { id: string; name: string }) => ({
          id: item.id,
          name: item.name,
          type: "region",
        }));
        setRegions(list);
      })
      .catch((error) => {
        console.error("Error loading regions: ", error);
      });
  };

  const loadRelatedSourceList = async () => {
    const relatedSourceListPromise = getRelation(currentId, MetadataTypeEnum.RESOURCE, "related-sources");

    relatedSourceListPromise
      .then((response) => {
        const list: ListItem[] = response.body.map((item: { id: any; name: any }) => ({
          id: decodeURIComponent(item.id),
          name: decodeURIComponent(item.name),
          type: "source",
        }));
        setRelatedSourceList(list);
      })
      .catch((error) => {
        console.error("Error loading sources: ", error);
      });
  };

  const loadAiTags = async () => {
    const aiTagsPromise = getRelation(currentId, MetadataTypeEnum.RESOURCE, "ai-tags");

    aiTagsPromise
      .then((response) => {
        console.log("AI Tags Response: ", response);
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
  };

  const confirmDelete = async () => {
    const response = await fetch(`/api/resources/trash/${currentId}`, { method: "PATCH", credentials: "include" });

    if (!response.ok) {
      toast.error("Error Deleting Resource");
      return;
    }

    const data: ApiResponse = await response.json();

    if (data.success) {
      setRightSidebarOpen(false);
      triggerGridReload();
      toast.success("Successfully Deleted Resource");
    } else {
      toast.error("Error Deleting Resource");
      console.error(data.message);
    }
  };

  return (
    <>
      <ConfirmDeleteDialog open={confirmDialogOpen} onOpenChange={setConfirmDialogOpen} onConfirmation={confirmDelete} />

      <h1 className="pb-2 text-2xl font-bold select-none">{title || <Skeleton />}</h1>

      {fileType === "website" && (
        <a href={url} className="select-none" target="_blank" rel="noreferror">
          <h1 className="mb-8 text-blue-500 underline select-none">{url}</h1>
        </a>
      )}

      <Expandable title="Description" collapsedHeight={100}>
        {description || <Skeleton />}
      </Expandable>

      <Expandable variant="horizontal" title="Tags">
        <BadgeList listType="tags" itemList={tags} onUpdate={triggerTagsRefresh} />
      </Expandable>

      <Expandable variant="horizontal" title="Recommended Tags">
        <BadgeList listType="ai-tags" itemList={aiTags} onUpdate={triggerAiTagsRefresh} />
      </Expandable>

      <Expandable variant="horizontal" title="Authors">
        <BadgeList listType="authors" itemList={authors} onUpdate={triggerAuthorsRefresh} />
      </Expandable>

      <Expandable variant="horizontal" title="Organisations">
        <BadgeList listType="organisations" itemList={organisations} onUpdate={triggerOrganisationsRefresh} />
      </Expandable>

      <Expandable variant="horizontal" title="Related People">
        <BadgeList listType="related-persons" itemList={relatedPersons} onUpdate={triggerRelatedPersonsRefresh} />
      </Expandable>

      <Expandable variant="horizontal" title="Related Organisations">
        <BadgeList listType="related-organisations" itemList={relatedOrganisations} onUpdate={triggerRelatedOrganisationRefresh} />
      </Expandable>

      <ResourceList header="Related Resources" resources={relatedResources} />

      <Expandable variant="horizontal" title="Sources">
        <BadgeList listType="sources" itemList={sourceList} onUpdate={triggerSourceListRefresh} />
      </Expandable>

      <Expandable variant="horizontal" title="Regions">
        <BadgeList listType="regions" itemList={regions} onUpdate={triggerRegionsRefresh} />
      </Expandable>

      {/* {resourceType === "Scientific Article" && (
                <Expandable title="Abstract" collapsedHeight={100}>
                    {<>insert abstract</> || <Skeleton />}
                </Expandable> 
            )} */}

      <Expandable title="Notes" collapsedHeight={100}>
        {note || <Skeleton />}
      </Expandable>

      <Expandable variant="horizontal" title="Related Sources">
        <BadgeList listType="related-sources" itemList={relatedSourceList} onUpdate={triggerRelatedSourceListRefresh} />
      </Expandable>

      <Divider name={"Publication Code"} minimize={true} />
      {pubCode}

      <Divider className="mt-2" name={"License Code"} minimize={true} />
      {license}

      {userRole === "admin" && (
        <div className="mt-10 flex w-full justify-center">
          <Button onClick={() => setConfirmDialogOpen(true)} variant="outline" className="border-red-500 text-red-500 hover:border-red-600 hover:bg-red-50 hover:text-red-600">
            Delete Resource
          </Button>
        </div>
      )}
    </>
  );
}

// This program has been developed by students from the bachelor Computer Science at Utrecht
// University within the Software Project course.
// © Copyright Utrecht University (Department of Information and Computing Sciences)
