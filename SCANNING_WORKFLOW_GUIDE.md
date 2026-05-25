# Angular + Dynamsoft Web TWAIN: End-to-End Implementation Guide

This document provides a full, step-by-step code explanation of how to initialize, scan, edit, and upload documents using Dynamsoft Web TWAIN's **native** capabilities inside an Angular application.

---

## Step 1: Initialization & Loading Resources

The scanner must be initialized after the Angular view has loaded (`ngAfterViewInit`) so the UI `<div>` container exists for the viewer. We must point Dynamsoft to the `Resources` folder which contains the core WebAssembly and native driver bridges.

```typescript
import { Component, AfterViewInit, OnDestroy } from '@angular/core';

// Declare Dynamsoft so TypeScript does not throw errors
declare var Dynamsoft: any;

@Component({
  selector: 'app-scanner',
  template: `
    <!-- The native Dynamsoft viewer relies on this specific ID to inject its UI -->
    <div id="dwtcontrolContainer" style="width: 100%; height: 600px; border: 1px solid #ccc;"></div>
  `
})
export class ScannerComponent implements AfterViewInit, OnDestroy {
  DWTObject: any = null;

  ngAfterViewInit(): void {
    // 1. Point to the local Resources folder containing the SDK
    Dynamsoft.DWT.ResourcesPath = '/assets/Resources';
    
    // 2. Force the use of the Local Service to communicate with physical hardware
    // If false, it falls back to WASM-only mode which cannot access your USB/Network scanner
    Dynamsoft.DWT.UseLocalService = true;

    // 3. Register the initialization event
    Dynamsoft.DWT.RegisterEvent('OnWebTwainReady', () => {
      this.DWTObject = Dynamsoft.DWT.GetWebTwain('dwtcontrolContainer');
      this.loadAvailableScanners();
    });
    
    // 4. Trigger the load
    if (typeof Dynamsoft.DWT.Load === 'function') {
      Dynamsoft.DWT.Load();
    }
  }

  ngOnDestroy(): void {
    // Prevent memory leaks when navigating away from the Angular component
    if (this.DWTObject) {
       Dynamsoft.DWT.DeleteDWTObject('dwtcontrolContainer');
    }
  }
}
```

---

## Step 2: Discovering Scanners
Once Dynamsoft is ready, query the Windows machine for installed TWAIN/WIA physical drivers. Note that physical scanners might have multiple drivers installed (e.g., a standard driver, a WIA version, and a 64-bit version).

```typescript
loadAvailableScanners(): void {
  if (!this.DWTObject) return;

  // Query scanners asynchronously
  this.DWTObject.GetSourceNamesAsync().then((names: string[]) => {
    console.log("Scanners installed on this PC: ", names);
    // You would typically bind 'names' to an HTML <select> dropdown here
  }).catch((err: any) => {
    console.error('Failed to query scanners:', err);
  });
}
```

---

## Step 3: Performing the Scan (ADF & Hardware Config)
When the user clicks "Start Scan", we lock the selected device, apply hardware configurations (like enabling the Automatic Document Feeder), and trigger the physical scan.

```typescript
async startNativeScan(useAdf: boolean) {
  if (!this.DWTObject) return;

  try {
    // Important: In Angular, clear the old images out of memory before a new batch starts
    this.DWTObject.RemoveAllImages();

    // Tell the backend Service which scanner we want to use
    await this.DWTObject.SelectSourceAsync();

    // Configure the Physical Scanner Hardware
    const deviceConfig: any = {
      // Show the native scanner software UI block. 
      // Bypassing this (false) crashes many HP and 64-bit drivers.
      IfShowUI: true, 
      IfCloseSourceAfterAcquire: true // Release the hardware lock after scan completes
    };

    // Auto Document Feeder support
    if (useAdf) {
       deviceConfig.IfFeederEnabled = true;
       deviceConfig.IfAutoFeed = true;
    }

    // Trigger the actual physical scan event
    await this.DWTObject.AcquireImageAsync(deviceConfig);
    
    console.log(`Scan success! Images in buffer: ${this.DWTObject.HowManyImagesInBuffer}`);
    
  } catch (error) {
    console.error("Scanner Error. Is it plugged in or locked by Windows Fax and Scan?", error);
    this.DWTObject.CloseSource(); // Ensure locks are released on failure
  }
}
```

---

## Step 4: Native Image Editing (Zoom, Crop, Brightness)
Once scanned, the image is held securely in Dynamsoft's memory buffer. 

### Option A: The Full Modal Editor (Fastest Implementation)
The easiest way to let users edit is to launch Dynamsoft's built-in modal UI. It floats over Angular and handles everything out of the box. You don't have to code any brightness or cropping logic yourself!

```typescript
openDynamsoftEditor() {
    // Shows the full feature-rich Image Editor. 
    // When the user hits "Save" or closes it, changes sync directly back to the DWT buffer.
    this.DWTObject.ShowImageEditor("Land Registration - Document Editor", (errCode: number, errStr: string) => {
        if(errCode !== 0) console.error(errStr);
    });
}
```

### Option B: Custom Angular Buttons + Native APIs
If you want to keep your own HTML Angular buttons on the sidebar, wire your buttons directly to the Dynamsoft Buffer APIs. **This removes the need for `<image-cropper>` entirely.**

```typescript
// --- ZOOM ---
zoomIn() {
   this.DWTObject.Viewer.zoomIn();
}
zoomOut() {
   this.DWTObject.Viewer.zoomOut();
}

// --- ROTATE ---
rotateLeft() {
   const currentIndex = this.DWTObject.CurrentImageIndexInBuffer;
   this.DWTObject.RotateLeft(currentIndex);
}

// --- BRIGHTNESS ---
changeBrightness(value: number) {
   // Brightness values range mathematically from -1000 to +1000
   const currentIndex = this.DWTObject.CurrentImageIndexInBuffer;
   this.DWTObject.ChangeBrightness(
       currentIndex, 
       value, 
       () => console.log('Brightness updated'),
       (errStr: string) => console.log(errStr)
   );
}

// --- CROP ---
cropSelection() {
   const currentIndex = this.DWTObject.CurrentImageIndexInBuffer;
   
   // Check if the user drove their mouse to draw a blue rectangle on the viewer natively
   const selection = this.DWTObject.GetSelectionRect(currentIndex);
   
   if (selection) {
       // Cut out exactly what they selected
       this.DWTObject.Crop(currentIndex, selection.left, selection.top, selection.right, selection.bottom);
   } else {
       alert("Please drag a selection rectangle on the image first!");
   }
}
```

---

## Step 5: Generating the PDF and Uploading
Because the Dynamsoft buffer handles all the image processing natively in WebAssembly/C++, it's incredibly fast. Once the user finishes editing, instruct Dynamsoft to bundle all the edited buffer images into a standard PDF so you can send it to your backend.

```typescript
generatePDFAndUpload() {
   // 1. Get indexes of all images currently inside the Dynamsoft buffer (Page 1, Page 2, Page 3...)
   const totalImages = this.DWTObject.HowManyImagesInBuffer;
   const imageIndexes = Array.from({length: totalImages}, (_, i) => i);

   // 2. Instruct Dynamsoft to natively convert these buffer images into a standard PDF File
   this.DWTObject.ConvertToBlob(
       imageIndexes, 
       Dynamsoft.DWT.EnumDWT_ImageType.IT_PDF, 
       (pdfBlob: Blob) => {
           // 3. Prepare the standard form payload for your Angular HttpClient
           const finalFile = new File([pdfBlob], 'Scanned_Document.pdf', { type: 'application/pdf' });
           
           const formData = new FormData();
           formData.append('File', finalFile);
           formData.append('AppRegId', '112233'); 
           
           // 4. Send to Backend
           this.documentService.uploadDocument(formData).subscribe(res => {
               console.log("Upload complete and securely stored!", res);
           });
       }, 
       (errCode: number, errStr: string) => {
           console.error("PDF Blob Conversion Failed", errStr);
       }
   );
}
```
