"use client";

import React, { useCallback, useEffect, useLayoutEffect, useMemo, useRef, useState } from "react";
import Cropper, { Area, MediaSize, Size } from "react-easy-crop";
import { Button } from "@/components/ui/button";
import { DialogHeader, DialogTitle, DialogDescription, DialogFooter, DialogClose } from "@/components/ui/dialog";

export default function AvatarEditorDialog({ url, onConfirm, onDefault, animating }: { url: string; onConfirm: (blob: Blob) => void; onDefault: () => void; animating: boolean }) {
  const [cropSize, setCropSize] = useState<Size>({ height: 0, width: 0 });
  const [crop, setCrop] = useState({ x: 0, y: 0 });
  const [zoom, setZoom] = useState(1);
  const [croppedAreaPixels, setCroppedAreaPixels] = useState<Area | null>(null);
  const [key, setKey] = useState(0);

  const cropperContainerRef = useRef<HTMLDivElement>(null);
  const cropperRef = useRef<Cropper>(null);

  // This cropper is kind off stupid, but atleast it doesn't cause epilepsy...
  // We need to manually set the crop size because the library uses the CSS scaled container size,
  // meaning the cropper is too small if the container zooms out after mounting
  const handleImageLoad = (mediaSize: MediaSize) => {
    const checkAndSetSize = () => {
      const container = cropperContainerRef.current;
      if (!container) {
        requestAnimationFrame(checkAndSetSize); // Try again on next frame
      } else {
        const imageHeight = mediaSize.naturalHeight;
        const containerHeight = container.offsetHeight;
        const scale = Math.max(1, mediaSize.naturalWidth / container.offsetWidth);
        const size = Math.min(imageHeight / scale, containerHeight);
        setCropSize({ width: size, height: size });
      }
    };

    checkAndSetSize();
  };

  // Cropper size calculations are all messed up, so reset the entire cropper when it stops animating
  useEffect(() => {
    setKey((prevKey) => prevKey + 1);
  }, [animating]);

  const onCropComplete = useCallback((_: Area, croppedAreaPixels: Area) => {
    setCroppedAreaPixels(croppedAreaPixels);
  }, []);

  const handleConfirm = async () => {
    const newBlob = (await getCroppedImg())!;
    onConfirm(newBlob);
  };

  const handleDefault = () => {
    onDefault();
  };

  async function getCroppedImg(): Promise<Blob | null> {
    if (!croppedAreaPixels) return null;

    const image = await createImage(url);
    const canvas = document.createElement("canvas");
    canvas.width = croppedAreaPixels.width;
    canvas.height = croppedAreaPixels.height;

    const ctx = canvas.getContext("2d");

    ctx!.drawImage(image, croppedAreaPixels.x, croppedAreaPixels.y, croppedAreaPixels.width, croppedAreaPixels.height, 0, 0, canvas.width, canvas.height);

    return new Promise((resolve) => {
      canvas.toBlob((blob) => resolve(blob), "image/png");
    });
  }

  function createImage(url: string): Promise<HTMLImageElement> {
    return new Promise((resolve, reject) => {
      const img = new Image();
      img.crossOrigin = "anonymous";
      img.src = url;
      img.onload = () => resolve(img);
      img.onerror = reject;
    });
  }

  return (
    <>
      <DialogHeader>
        <DialogTitle>Avatar Editor</DialogTitle>
        <DialogDescription>Move, scale and crop you avatar image to your liking.</DialogDescription>
      </DialogHeader>
      <div ref={cropperContainerRef} className="checkerboard relative aspect-square max-h-64 w-full p-40">
        <Cropper
          key={key}
          ref={cropperRef}
          image={url}
          crop={crop}
          zoom={zoom}
          aspect={1}
          cropShape="round"
          showGrid={false}
          onCropChange={setCrop}
          minZoom={0.1}
          maxZoom={5}
          zoomSpeed={0.1}
          onZoomChange={setZoom}
          cropSize={cropSize} // Set a default crop size
          onMediaLoaded={handleImageLoad}
          onCropComplete={onCropComplete}
          restrictPosition={false}
          objectFit="contain"
        />
      </div>

      <DialogFooter>
        <div className="mt-4 flex w-full items-center justify-between gap-0 gap-6">
          <div className="flex flex-1 items-center justify-center">
            <input className="accent-purple w-full" type="range" min={1} max={5} step={0.001} value={zoom} onChange={(e) => setZoom(Number(e.target.value))} />
          </div>
          <DialogClose asChild>
            <Button onClick={handleConfirm} className="w-full flex-1">
              Confirm
            </Button>
          </DialogClose>
        </div>
      </DialogFooter>
    </>
  );
}
