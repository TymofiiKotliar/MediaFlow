# Project Overview
## What is the project?
**PhotoSaver** is a desktop application for managing and processing media files from cameras and other recording devices. The user registers any number of devices, browses the files on each device from within the app, and runs a configurable processing pipeline that can rotate, rename, back up, and forward media to a Telegram chat — all in one click.

The application is built around the concept of a **device profile**: a named configuration that records where source files live, where the backup copy should go, how the output files should be named, and which Telegram destination should receive them. Device profiles persist between sessions, so routine offload workflows require no repeated configuration.

## What problem does it solve?
Photographers and videographers who shoot regularly face a repetitive manual workflow after each session: copy files, rename them consistently, apply orientation fixes, and distribute them. Doing this by hand is slow and error-prone; generic file managers offer no per-device defaults or automation.

**PhotoSaver** compresses this multi-step routine into a single action: select files, confirm the per-file actions via checkboxes, click run, and watch a progress view complete the work. The destination is always pre-configured and the naming scheme is always consistent, eliminating the most common mistakes.

## Who is it for?
The application targets **individual photographers, videographers, and content creators** who:
- shoot with multiple cameras or devices (DSLRs, action cameras, smartphones)
- offload media to a local backup regularly
- share selected shots directly to Telegram channels or group chats
- need consistent file naming across sessions

It is a standalone desktop tool, not a library or service. It is intended for personal or small-team use on a single workstation.

## Why is it better?
- **Per-device memory** — source folder, backup folder, naming scheme, and Telegram destination are stored per device; no re-entry between sessions.
- **Inline preview with thumbnails** — images and videos are shown with thumbnails before processing, so the user can curate the selection.
- **Per-file action flags** — each file can carry its own combination of actions (rotate, backup, Telegram, delete), set via checkboxes on multi-selection.
- **Smart renaming** — naming templates support auto-incrementing sequence numbers and either the current date or the photo's EXIF capture date.
- **Integrated Telegram delivery** — photos and videos are sent directly to a Telegram chat without leaving the app or opening a browser.
- **Offline-first** — all processing runs locally; the only outbound network call is the optional Telegram send.

# Core Features
- **Device profile management**
    Users can create, edit, and delete device profiles. Each profile stores a device name, source folder, backup folder, naming template, Telegram bot token, Telegram chat ID, and a per-session file-load limit. Profiles persist between app launches as JSON.

- **Media browser with thumbnails**
    When a device profile is opened, the app loads images and videos from the configured source folder into a scrollable list. Each entry shows a filename and a thumbnail — extracted directly from images or generated from the first video frame via FFmpeg.

- **Paginated loading**
    Files are loaded in configurable batches (10–1000 per load). A "Load more" action appends the next batch, keeping the UI responsive for large source folders.

- **Per-file action assignment**
    The user selects one or more files in the list. Checkboxes let them assign any combination of: rotate left, rotate right, flip 180°, save to backup, send to Telegram, and delete original after processing.

- **Processing pipeline**
    Running the pipeline executes all assigned actions for every file in sequence: video conversion → rotation → backup copy → Telegram send → deletion. A progress dialog shows the current file name and a progress bar, and can be cancelled.

- **Custom file renaming with visual template builder**
    Backup files are renamed according to a template built through a guided UI rather than a raw syntax string. The user types a prefix, then inserts structured tokens from a set of buttons: **Sequence Number** (auto-incrementing integer, seeded from the most recently modified file in the backup folder), **Current Date** (date the pipeline runs), and **Photo Date** (capture date read from the file's EXIF data). Tokens appear as labelled placeholders in the template field. A live preview label below the field shows the exact filename that would be produced for the next file, including the extension. The original filename can also be kept unchanged by leaving the template empty.

- **Video normalisation**
    Non-MP4 videos (AVI, MOV) are re-encoded to MP4 (H.264 / AAC) before other pipeline steps, ensuring all output is in a consistent, universally playable format.

- **Telegram delivery**
    Photos are sent as `SendPhoto` and videos as `SendVideo` via the Telegram Bot API. The bot token and chat ID are taken from the device profile. Network errors are reported back to the UI without stopping the rest of the pipeline.

- **Configurable per-device load limit**
    Each device profile stores how many files to load per batch. This allows lightweight browsing on devices with thousands of files.

- **Apply actions to all files**
    A single "Apply to all" button propagates the current checkbox state to every loaded file at once, eliminating the need to select files and set checkboxes one group at a time. Useful when the intended workflow is uniform across the whole batch (e.g. backup and delete everything).

- **Media browser sort and filter**
    The media list can be sorted by filename, last-modified date, or file size (ascending or descending), and filtered to show only images, only videos, or all files. Both controls are available as a toolbar above the list and operate on the already-loaded set without re-reading the source folder.

- **Single-click file preview**
    Clicking a cell in the media list opens a full-size preview overlay. For images, the photo is displayed at the largest size that fits the screen. For videos, the same FFmpeg-extracted thumbnail is shown enlarged. The overlay is dismissed by clicking outside it or pressing Escape.

- **EXIF auto-rotation**
    When loading files, the system reads the EXIF orientation tag from each image (using the already-present `metadata-extractor` dependency) and applies the corresponding rotation to the thumbnail and to the temp copy before pipeline processing, so files appear correctly oriented without manual checkbox use. If no EXIF orientation tag is present or the value is "normal", no rotation is applied.

- **Per-run summary**
    After the pipeline completes, a summary dialog displays the outcome: number of files backed up, sent to Telegram, deleted from source, and failed. Failed files are listed by name with a one-line reason. The dialog is dismissed with a single button and replaces the current silent close of the progress dialog.

- **Duplicate guard on backup**
    Before copying a file to the backup folder, the system checks whether a file with the generated name already exists there. If it does, the file is skipped and counted as a duplicate in the per-run summary rather than silently overwriting the existing backup.

# Use Cases
## Main Use Cases

### UC00 Process Device Media (Abstract)
**Name:** Process Device Media
**ID:** UC00
**Description:** Abstract use case representing the general flow of selecting a device and acting on its media files.
**Actors:** User
**Relationships:**
- `<<generalize>>` UC01 Register Device
- `<<generalize>>` UC02 Browse Device Media
- `<<generalize>>` UC03 Run Processing Pipeline

### UC01 Register Device
**Name:** Register Device
**ID:** UC01
**Description:** The user creates a new device profile by providing all required configuration fields.
**Actors:** User
**Preconditions:** None (app is running).
**Postconditions:**
- **Success:** A new device profile is saved to disk and appears in the device list.
- **Failure:** Validation errors are shown and no profile is saved.

**Main Scenario:**
1. User opens the Add Device form.
2. User fills in: device name, source folder, backup folder, naming template, Telegram bot token, chat ID, and files-per-load limit.
3. `<<include>>` **UC06 Validate Device Form**
4. System saves the profile to JSON storage.
5. System returns to the device list, which now includes the new entry.

**Alternative:**
- 2.1 User opens an existing profile to edit it → fields are pre-populated. On save, the profile is updated in place (see UC05).

**Exceptions:**
- 3.1 Any required field is empty → display field-level error message, stay on form.
- 3.2 Chat ID contains non-digit characters → display specific error.
- 3.3 Naming template has unbalanced `*` tokens → display specific error.

### UC02 Browse Device Media
**Name:** Browse Device Media
**ID:** UC02
**Description:** The user selects a device and views its source files as a thumbnail list.
**Actors:** User
**Preconditions:** At least one device profile exists with a valid source folder.
**Postconditions:**
- **Success:** Files from the source folder are displayed with thumbnails.
- **Failure:** Error shown if source folder is inaccessible.

**Main Scenario:**
1. User selects a device from the list.
2. System validates the source folder path.
3. System copies files to a temporary folder in batches.
4. `<<include>>` **UC07 Extract Thumbnails**
5. System populates the media list view.
6. A progress dialog tracks loading; user may cancel.
7. User may `<<extend>>` **UC08 Load More Files** to append additional batches.

**Exceptions:**
- 2.1 Source folder does not exist or is not accessible → show error indicator on the device list entry.

### UC03 Run Processing Pipeline
**Name:** Run Processing Pipeline
**ID:** UC03
**Description:** The user assigns actions to selected files and runs the pipeline to execute them.
**Actors:** User
**Preconditions:** At least one file is loaded in the media browser.
**Postconditions:**
- **Success:** All assigned actions have been applied; pipeline completes without error.
- **Partial success:** Failed actions are logged; pipeline continues to next file.
- **Failure:** User is shown a pipeline error label.

**Main Scenario:**
1. User selects one or more files from the media list.
2. User sets action checkboxes: rotate left / right / flip, save to backup, send to Telegram, delete after.
3. User clicks Run.
4. For each file in the full list:
   *a.* `<<include>>` **UC09 Convert Video to MP4** (if the file is a non-MP4 video)
   *b.* `<<include>>` **UC10 Rotate Media** (if any rotation action is set)
   *c.* `<<include>>` **UC04 Save to Backup** (if save action is set)
   *d.* `<<include>>` **UC11 Send to Telegram** (if Telegram action is set)
   *e.* File is marked for deletion (if delete action is set)
5. After all files are processed, marked files are deleted.
6. Progress dialog closes.

**Exceptions:**
- 3.1 Telegram bot initialisation fails → show "Telegram set up error", block pipeline start.
- 4.x Any step fails → log error, continue to next file.

### UC04 Save to Backup
**Name:** Save to Backup
**ID:** UC04
**Description:** The system copies a processed file to the configured backup folder under a name generated from the device's naming template.
**Actors:** System (called from UC03)
**Main Scenario:**
1. `<<include>>` **UC12 Generate File Name**
2. System copies the (possibly modified) temp file to the backup folder with the generated name.

**Exceptions:**
- 2.1 Backup folder does not exist or is not writable → throw I/O error, fail this file.

### UC05 Edit Device Profile
**Name:** Edit Device Profile
**ID:** UC05
**Description:** The user modifies an existing device profile.
**Actors:** User
**Preconditions:** A device profile exists and is selected.
**Postconditions:**
- **Success:** Updated profile is saved; device list reflects the changes.

**Main Scenario:**
1. User selects a device and clicks Edit.
2. System opens the form pre-populated with existing values.
3. User modifies any field.
4. `<<include>>` **UC06 Validate Device Form**
5. System replaces the old profile entry and saves JSON.
6. System returns to device list.

## Assisting Use Cases

### UC06 Validate Device Form
**Name:** Validate Device Form
**ID:** UC06
**Description:** Checks that all required fields are present and syntactically correct before a profile is saved.
**Main Scenario:**
1. System verifies none of: name, source path, backup path, naming example, bot token, chat ID are empty.
2. System verifies chat ID contains only digits.
3. System verifies naming template has zero or two `*` tokens (one opening, one closing).
4. System verifies naming template has at most one `#` token.
5. Validation passes.

**Exceptions:**
- 1.1–4.x Any check fails → display a specific warning text near the form, return false.

### UC07 Extract Thumbnails
**Name:** Extract Thumbnails
**ID:** UC07
**Description:** Generates a preview image for each file in the media list.
**Main Scenario:**
1. For image files (JPG, JPEG, PNG): load image directly as a JavaFX `Image`.
2. For video files: invoke FFmpeg via `ProcessBuilder` to extract the frame at ~1 s, scale to 320 px wide, output as MJPEG to stdout, decode into a `BufferedImage`, convert to JavaFX `Image`.

**Exceptions:**
- 2.1 FFmpeg exits non-zero or returns no data → log error, throw `IOException`.

### UC08 Load More Files
**Name:** Load More Files
**ID:** UC08
**Description:** Appends the next batch of files from the source folder to the already-loaded list.
**Condition:** Triggered by the user clicking "Load more" in the media browser.
**Main Scenario:**
1. System advances the file-walk offset by the per-device limit.
2. System copies the next batch to the temp folder and generates thumbnails.
3. New entries are appended to the existing list view.

### UC09 Convert Video to MP4
**Name:** Convert Video to MP4
**ID:** UC09
**Description:** Re-encodes non-MP4 video files to MP4 (H.264 / AAC) before further pipeline steps.
**Condition:** File is a video and does not already have an `.mp4` extension.
**Main Scenario:**
1. System opens the input with `FFmpegFrameGrabber`.
2. System creates an `FFmpegFrameRecorder` targeting a new `.mp4` path in the temp folder.
3. System configures H.264 video, AAC audio, ultrafast preset, faststart moov atom.
4. System transfers frames one by one and finalises the output.
5. Cell's temp path and name are updated to point at the new MP4.

**Exceptions:**
- Any JavaCV exception → log "FAILED TO CONVERT VIDEO TO MP4", leave original cell unchanged.

### UC10 Rotate Media
**Name:** Rotate Media
**ID:** UC10
**Description:** Applies a rotation to an image or video file in-place using FFmpeg.
**Condition:** One or more of left rotation, right rotation, or flip rotation is assigned to the file.
**Main Scenario:**
1. System builds an FFmpeg command using the appropriate `transpose` filter value (left: 2, right: 1, flip: two passes of 1).
2. System writes output to a `_rotated` temp file in the same folder.
3. System atomically replaces the original temp file with the rotated output.

**Exceptions:**
- FFmpeg exits non-zero → log failure, leave file unchanged.

### UC11 Send to Telegram
**Name:** Send to Telegram
**ID:** UC11
**Description:** Sends a photo or video file to a Telegram chat using the device's bot configuration.
**Main Scenario:**
1. System determines file type (image or video).
2. System sends `SendPhoto` or `SendVideo` via `TelegramBot.execute()`.
3. System verifies the response is OK.

**Exceptions:**
- 3.1 Response is not OK → log warning, throw exception (pipeline logs and continues).
- Bot initialisation fails at scene open → UI shows "Telegram set up error", pipeline is blocked.

### UC12 Generate File Name
**Name:** Generate File Name
**ID:** UC12
**Description:** Produces a backup filename by resolving each token in the device's stored template against the current pipeline context.
**Main Scenario:**
1. System reads the ordered token list from the device profile (built via the template builder in Scene 2).
2. System resolves each token in sequence:
   - **Prefix text** — copied verbatim.
   - **Sequence Number** — auto-incrementing integer, seeded from the highest number found in the most recently modified filename in the backup folder; incremented once per file processed.
   - **Current Date** — today's date formatted as `dd-MM-yyyy`.
   - **Photo Date** — `DateTimeOriginal` from the file's EXIF metadata, same format.
3. System concatenates resolved tokens and appends the original file extension.
4. Returns the final filename string.

**Alternative:**
- Template is empty → original filename is used unchanged.

**Exceptions:**
- Photo Date token requested but EXIF data is absent or unreadable → log warning, skip the date token, continue with remaining tokens.

### UC13 Build Naming Template
**Name:** Build Naming Template
**ID:** UC13
**Description:** The user constructs a filename template through the guided builder in the profile editor, without writing raw syntax.
**Actors:** User
**Preconditions:** Profile editor (Scene 2) is open.
**Main Scenario:**
1. User optionally types a prefix string into the template field.
2. User clicks one or more token buttons to insert tokens at the current cursor position: **Sequence Number**, **Current Date**, **Photo Date**.
3. System renders inserted tokens as labelled placeholders in the template field.
4. System updates the live preview label in real time, showing the resolved filename for a hypothetical next file (e.g. `Holiday-001-11-06-2026.jpg`).
5. User adjusts prefix text or token order until the preview matches the desired format.

**Alternative:**
- User leaves the template field empty → original filenames are preserved on backup.

### UC14 Preview File
**Name:** Preview File
**ID:** UC14
**Description:** The user opens a full-size preview of a single file from the media browser.
**Actors:** User
**Preconditions:** At least one file is loaded in the media browser.
**Main Scenario:**
1. User clicks a cell in the media list.
2. System opens a full-screen overlay.
3. For images: system displays the photo at the largest size that fits the screen, centred.
4. For videos: system displays the FFmpeg-extracted thumbnail enlarged with a play-icon indicator.
5. User dismisses the overlay by clicking outside it or pressing Escape.

### UC15 Apply EXIF Auto-Rotation
**Name:** Apply EXIF Auto-Rotation
**ID:** UC15
**Description:** The system corrects image orientation using the EXIF tag when a file is loaded into the media browser, so it appears upright without manual rotation.
**Condition:** Triggered during thumbnail extraction for image files (UC07).
**Main Scenario:**
1. System reads the EXIF `Orientation` tag from the image file using `metadata-extractor`.
2. System maps the tag value to the equivalent FFmpeg `transpose` operation.
3. System applies the rotation to the temp copy of the file.
4. Thumbnail is generated from the already-corrected temp copy.

**Alternative:**
- 1.1 EXIF tag is absent, unreadable, or value is "normal" (1) → no rotation applied, continue normally.

### UC16 Check for Backup Duplicate
**Name:** Check for Backup Duplicate
**ID:** UC16
**Description:** Before copying a file to the backup folder, the system verifies that no file with the generated name already exists there.
**Condition:** Invoked within UC04 Save to Backup, after UC12 generates the filename.
**Main Scenario:**
1. System checks whether a file with the generated name exists in the backup folder.
2. File does not exist → backup copy proceeds normally.

**Alternative:**
- 1.1 File already exists → skip the copy, record the file as a duplicate in the run summary, do not increment the sequence counter.

### UC17 Display Run Summary
**Name:** Display Run Summary
**ID:** UC17
**Description:** After the pipeline finishes, the system presents a structured report of outcomes to the user.
**Condition:** Triggered when UC03 Run Processing Pipeline completes (success or partial success).
**Main Scenario:**
1. System collects pipeline counters: backed up, sent to Telegram, deleted, duplicates skipped, failed.
2. System replaces the progress dialog with a summary dialog listing each counter.
3. If any files failed, system lists each failed filename with a one-line reason.
4. User dismisses the dialog with a confirmation button.

# System Architecture

The system follows **Clean Architecture**. Dependencies point strictly inward: the outer layers know about the inner layers, but the inner layers know nothing about the outer ones. This means the core business logic has no dependency on the UI framework, the media processing tool, the messaging service, or the persistence format — any of those can be replaced without touching the domain.


## 1. Domain Layer

The innermost layer. Contains pure business logic and has zero dependencies on any external library or framework.

**Entities** are the core data models: a device profile (name, source location, backup location, naming template, messaging destination, load limit), a media file descriptor (name, type, locations, assigned actions, accumulated metadata), and a run summary (counters for backed up, sent, deleted, skipped, failed files with per-file failure reasons).

**Pipeline stage interface** — the central abstraction. Every per-file action implements a single interface that receives a file context, performs its work, and returns a modified context. The domain defines this contract; the infrastructure provides the concrete implementations. Adding a new action means adding a new implementation of this interface — nothing else changes.

**Naming token interface** — each token type (sequence number, current date, photo date, literal text) implements a resolver interface that takes a file context and returns a string segment. The domain defines the contract; resolvers are composable into an ordered list stored on the device profile.

**Repository interface** — a persistence contract for loading and saving device profiles. The domain defines what it needs; the infrastructure provides the implementation.

**Progress observer interface** — a callback contract that the pipeline calls during execution (`fileStarted`, `fileCompleted`, `fileFailed`, `pipelineCancelled`, `pipelineFinished`). The presentation layer registers a concrete observer; the domain never references the UI framework.

## 2. Application Layer

Orchestrates domain objects into complete workflows. One use-case class per user-facing operation: register a device, edit a device, delete a device, load media from a device, run the processing pipeline, build a naming template. Each use-case class:
- accepts plain input data (no UI objects),
- calls domain logic and infrastructure through domain interfaces,
- returns a plain result or fires progress events through the observer interface.

The application layer is the only place that sequences steps: it tells the pipeline engine which stages to assemble for a given file, based on the action flags set on that file's descriptor.

## 3. Infrastructure Layer

Contains one adapter per external concern. Each adapter implements a domain interface and wraps exactly one external dependency.

- **Media processor adapter** — implements pipeline stage interfaces for rotation, format conversion, thumbnail extraction, and image resizing by delegating to an external media processing tool. The domain has no knowledge of that tool.
- **Metadata reader adapter** — implements the interface used to read EXIF orientation and capture date from image files.
- **Messaging adapter** — implements the pipeline stage interface for sending files to a messaging destination, delegating to an external messaging API.
- **File system adapter** — implements file copy, move, delete, and directory operations used by backup and deletion stages.
- **Device repository adapter** — implements the repository interface by reading and writing device profile documents to a LiteDB collection. Supports LINQ-based queries for sorting and filtering the device list without loading all records into memory first.
- **Thumbnail cache adapter** — manages the storage and retrieval of generated thumbnail images in the application's local data folder.

## 4. Presentation Layer

The outermost layer. Contains all UI screens, controllers, and view models. It depends on the application layer to trigger use cases and on the progress observer interface to receive pipeline events.

- **Device list screen** — displays registered device profiles, allows selection, add, edit, and delete.
- **Profile editor screen** — a form for creating and modifying a device profile. Includes the naming template builder, which constructs an ordered token list by responding to user button clicks and updating a live preview.
- **Media browser screen** — displays loaded media files as a thumbnail list with sort, filter, multi-select, and per-file action checkboxes. Triggers pipeline execution and registers a progress observer.
- **Progress overlay** — a modal panel that receives progress events from the observer and updates a progress bar and status label. Exposes a cancel action that signals the pipeline to stop after the current file.
- **Run summary screen** — displayed after pipeline completion; shows outcome counters and a list of failed files with reasons. Populated from the run summary entity returned by the application layer.

# Technology Choices

1. **Language & Runtime:** C# 12 / .NET 9
   - Records map directly onto immutable domain entities (`record DeviceProfile`, `record FileContext`). Sealed class hierarchies with pattern matching model pipeline stage results and naming tokens exhaustively — the compiler enforces that every case is handled. Nullable reference types catch missing EXIF data and unset paths at compile time rather than at runtime.

2. **UI Framework:** Avalonia UI 11
   - Cross-platform declarative UI defined in XAML with full data binding, control templates, and CSS-like styling. Runs natively on Windows, macOS, and Linux without a webview. The visual tree and layout model are close enough to WPF that existing .NET knowledge transfers directly. Avalonia's `Dispatcher` handles UI thread marshalling for progress updates from background tasks.

3. **Reactive / MVVM Layer:** ReactiveUI
   - View models expose `ObservableAsPropertyHelper` properties bound directly to the UI. Pipeline progress events, cancellation state, and run summary data flow from the application layer to the presentation layer as `IObservable<T>` streams — no manual `PropertyChanged` wiring. `ReactiveCommand` ties button actions to application-layer use cases with built-in can-execute logic and async execution.

4. **Concurrency:** .NET Task Parallel Library + `CancellationToken`
   - Each pipeline stage is an `async` method that accepts a `CancellationToken`. Cancelling the token mid-pipeline propagates immediately through all in-flight stages without extra coordination code. `IProgress<T>` carries typed progress reports from the pipeline executor to the UI thread safely.

5. **Dependency Injection:** `Microsoft.Extensions.DependencyInjection`
   - Built into .NET. Infrastructure adapters are registered against their domain interfaces at the composition root. The application layer receives its dependencies through constructor injection — no service locator, no static state.

6. **Video Processing:** FFMpegCore
   - A managed .NET wrapper around the FFmpeg binary. Covers rotation (`transpose` filters), thumbnail frame extraction, and format conversion (AVI/MOV → MP4) through a fluent API. The FFmpeg binary is bundled alongside the application at distribution time so no separate installation is required.

7. **EXIF Metadata:** MetadataExtractor
   - A direct C# port of the same library used conceptually in the original design, by the same author. Reads `Orientation` and `DateTimeOriginal` tags from JPEG and other formats. Used by the EXIF auto-rotation stage and the photo-date naming token resolver.

8. **Telegram Integration:** Telegram.Bot
   - The official .NET client for the Telegram Bot API. Fully async — `SendPhotoAsync` and `SendVideoAsync` accept a `CancellationToken` and fit naturally into the pipeline stage model.

9. **Device Storage:** LiteDB
   - An embedded document database for .NET stored as a single `.db` file in the application data folder. Device profiles are stored as documents and queried via a LINQ-compatible API — sorting by name, filtering by source path, or any future query is a one-liner against the collection. No schema definition, no migrations, and no ORM mapping layer required. The infrastructure adapter remains thin and the domain model is untouched.

10. **Distribution:** `dotnet publish --self-contained --single-file`
    - Produces a single native executable per target platform with the .NET runtime bundled. The user downloads one file; no runtime installation is required. Code signing and notarisation for Windows and macOS follow standard platform tooling.

# Dependencies

## Core Runtime

| Package                                    | Purpose                                              |
|--------------------------------------------|------------------------------------------------------|
| .NET 9 SDK                                 | Language runtime, BCL, async/await, TPL              |
| Avalonia 11                                | Cross-platform desktop UI framework                  |
| Avalonia.Desktop                           | Native windowing host for Windows, macOS, Linux      |
| Avalonia.Themes.Fluent                     | Built-in Fluent design theme                         |

## Reactive / MVVM

| Package                                    | Purpose                                              |
|--------------------------------------------|------------------------------------------------------|
| ReactiveUI                                 | MVVM framework — reactive commands, observable props |
| ReactiveUI.Avalonia                        | ReactiveUI bindings for Avalonia view base classes   |

## Dependency Injection

| Package                                    | Purpose                                              |
|--------------------------------------------|------------------------------------------------------|
| Microsoft.Extensions.DependencyInjection   | IoC container — wires domain interfaces to adapters  |

## Processing

| Package                                    | Purpose                                              |
|--------------------------------------------|------------------------------------------------------|
| FFMpegCore                                 | Managed FFmpeg wrapper — rotation, conversion, thumbnails |
| FFMpegCore.Binaries                        | Bundled FFmpeg binary — no separate install required |
| MetadataExtractor                          | EXIF orientation and capture date reading            |

## Persistence

| Package                                    | Purpose                                              |
|--------------------------------------------|------------------------------------------------------|
| LiteDB                                     | Embedded document database — device profile storage and querying |

## Integration

| Package                                    | Purpose                                              |
|--------------------------------------------|------------------------------------------------------|
| Telegram.Bot                               | Telegram Bot API — send photos and videos            |

## Testing

| Package                                    | Purpose                                              |
|--------------------------------------------|------------------------------------------------------|
| xUnit                                      | Unit test framework                                  |
| NSubstitute                                | Interface mocking for domain and application tests   |
| FluentAssertions                           | Readable assertion syntax for test output            |

# Development Plan

## Phase 1 — Solution Structure and Domain Layer
> *Build the innermost layer first. Nothing here depends on a UI framework, a file system, or a network. Everything is testable in isolation.*

**Tasks:**
1. Create the .NET solution with four projects matching the architecture: `Domain`, `Application`, `Infrastructure`, `Presentation`.
2. Define domain entities as C# records: `DeviceProfile`, `FileContext`, `NamingToken` (sealed hierarchy), `PipelineStageResult` (sealed hierarchy: `Success`, `Skipped`, `Failed`), `RunSummary`.
3. Define all domain interfaces: `IPipelineStage`, `INamingTokenResolver`, `IDeviceRepository`, `IMediaLoader`, `IProgressObserver`.
4. Implement application-layer use cases: `RegisterDeviceUseCase`, `EditDeviceUseCase`, `DeleteDeviceUseCase`, `LoadMediaUseCase`, `RunPipelineUseCase`, `BuildNamingTemplateUseCase`. Each receives its dependencies through constructor injection and calls only domain interfaces — no concrete infrastructure types.

**Testing:**
- Unit test every use case with NSubstitute mocks standing in for all domain interfaces.
- Cover the main scenario, each documented alternative, and each exception path from the use case definitions.
- Test `RunPipelineUseCase` with a mock stage that always succeeds, one that always fails, and one that returns `Skipped` — verify the run summary counters are correct in each case.
- Test naming token resolution: sequence number seeding, current date formatting, graceful fallback when the photo date token has no EXIF data.

**Exit criteria:** All use cases pass their unit tests with no infrastructure or UI code compiled into the test project.

---

## Phase 2 — Infrastructure Layer
> *Implement one adapter at a time. Each adapter is independently testable against the real external tool.*

**Tasks:**
1. **Device repository adapter** — store and query `DeviceProfile` documents in a LiteDB collection in the application data folder. The adapter exposes insert, update, delete, and query methods; callers pass LINQ expressions for filtering and ordering. The LiteDB dependency is confined to this adapter — the domain interface accepts and returns plain `DeviceProfile` records.
2. **File system adapter** — implement file copy (backup), move, delete, directory scan, and temp folder management. Initialise the application folders (`~/.PhotoSaver/images/`, `~/.PhotoSaver/temp/`) on first run.
3. **Metadata adapter** — use MetadataExtractor to read EXIF `Orientation` and `DateTimeOriginal`. Return typed results; never throw into the domain.
4. **Media processor adapter** — use FFMpegCore to implement: thumbnail extraction (image and video), left/right/flip rotation, AVI/MOV → MP4 conversion. Each operation is a separate method implementing `IPipelineStage`.
5. **Telegram adapter** — implement the send stage using Telegram.Bot's `SendPhotoAsync` / `SendVideoAsync`. Accept a `CancellationToken`; map API errors to `PipelineStageResult.Failed`.
6. **Composition root** — wire all adapters to their domain interfaces via `Microsoft.Extensions.DependencyInjection`.

**Testing:**
- Integration test each adapter individually against real resources: a fixture folder of sample images and videos, a real FFmpeg binary, a real MetadataExtractor call.
- For the device repository: insert several profiles with different names and source paths; read them back and assert round-trip equality; query with an ordering and a filter and assert only the expected subset is returned in the correct order.
- For the file system adapter: copy a file to a temp folder, verify it exists; delete it, verify it is gone.
- For the media processor: rotate a known image 90° right, read back its dimensions and verify they are swapped; extract a thumbnail from a known video and assert the output file is non-empty.
- For the Telegram adapter: mock the HTTP client; assert the correct request type (`SendPhoto` vs `SendVideo`) is constructed for each file type.
- Do not test the Telegram adapter against the live API in automated tests.

**Exit criteria:** All infrastructure integration tests pass against real files on the local machine. The composition root compiles with all interfaces resolved.

---

## Phase 3 — Device Management UI
> *First visible screen. Establishes the Avalonia + ReactiveUI + DI wiring pattern that all subsequent screens follow.*

**Tasks:**
1. Set up the Avalonia application host with the DI container from Phase 2 as the service provider.
2. Implement the device list screen: displays registered profiles, each showing device name and thumbnail. Add, Edit, Delete buttons bound to ReactiveCommands that invoke the corresponding application-layer use cases.
3. Implement the profile editor screen: form fields for all `DeviceProfile` properties, OS directory chooser dialogs for source and backup folder paths, thumbnail picker.
4. Implement the naming template builder (UC13): token-insert buttons render labelled chips in the template field; a live preview label resolves the current token list against a dummy file context and updates on every keystroke.
5. Implement form validation: all required fields, chat ID digit check, empty template interpreted as "keep original name".
6. Persist on every save; reload from JSON on app launch.

**Testing:**
- Manually create a device profile, close the app, reopen it — verify the profile is restored exactly.
- Manually edit a profile — verify the list updates and the change is persisted.
- Manually delete a profile — verify it disappears from the list and the JSON file.
- In the template builder: insert a Sequence Number token, type a prefix, insert a Current Date token — verify the live preview shows the correct resolved filename with the correct extension.
- Attempt to save a profile with each required field empty in turn — verify the correct validation message appears and the form does not close.

**Exit criteria:** Device profiles can be created, edited, and deleted. Data survives an app restart. The template builder preview matches the output that `BuildNamingTemplateUseCase` would produce.

---

## Phase 4 — Media Browser
> *The most visually complex screen. Build it in layers: loading first, then thumbnails, then selection and actions.*

**Tasks:**
1. Implement the media browser screen: clicking a device in the list opens the browser for that device.
2. Implement background file loading via `LoadMediaUseCase` on a `Task`, reporting progress through `IProgress<T>` to the progress overlay.
3. Display loaded files as a thumbnail list with filename labels. Copy source files to the temp folder during loading; clean the temp folder on browser close.
4. Implement EXIF auto-rotation (UC15) during load: read orientation tag and apply correction to the temp copy before displaying the thumbnail.
5. Implement paginated loading: initial batch from the device's load-limit setting, "Load more" button appends the next batch.
6. Implement thumbnail extraction for videos via the media processor adapter.
7. Implement the sort and filter toolbar: sort by name / date / size, filter to images / videos / all. Operates on the in-memory list.
8. Implement single-click file preview overlay (UC14): full-size image or enlarged video thumbnail, dismissed by click-outside or Escape.
9. Implement the progress overlay with cancellation: a close gesture sets the cancellation token; the loading task checks it between files.

**Testing:**
- Load a folder containing images, videos, a file with an incorrect EXIF orientation, and an unsupported file type. Verify: images and videos appear; unsupported files are silently skipped; the rotated image displays correctly oriented.
- Verify the "Load more" button loads exactly the next batch and appends without duplicating existing entries.
- Sort by date descending — verify the most recently modified file appears first. Filter to images only — verify no video thumbnails remain in the list.
- Click a cell — verify the preview overlay opens with the correct file. Press Escape — verify it closes.
- Start loading a large folder and cancel mid-way — verify loading stops and the app remains responsive.

**Exit criteria:** The media browser loads real images and videos from a folder, displays correct thumbnails with correct orientation, and all sort/filter/preview/pagination interactions work without UI freezing.

---

## Phase 5 — Processing Pipeline
> *Wire the pipeline stages to the UI. At this point all stages exist (Phase 2); this phase connects them to the media browser's selection state and runs them.*

**Tasks:**
1. Add per-file action checkboxes to the media browser: rotate left, rotate right, flip, save to backup, send to Telegram, delete after. Checkboxes reflect the union of flags across the current multi-selection and write back on change.
2. Implement the "Apply to all" button: propagates current checkbox state to every loaded file.
3. Implement the Run button: invokes `RunPipelineUseCase` with the full file list and the device profile. The pipeline assembles the stage chain per file based on each file's action flags.
4. Implement the duplicate guard (UC16) inside the backup stage: check for an existing file with the generated name; skip and record as duplicate if found.
5. Wire the progress overlay to the pipeline: update the progress bar and current filename label via `IProgressObserver` on each `FileStarted` and `FileCompleted` event. Cancellation token flows from the overlay close gesture into the pipeline executor.
6. Implement the run summary screen (UC17): shown after pipeline completion, populated from the `RunSummary` entity returned by the use case. Lists per-counter totals and failed files with reasons.
7. Add a confirmation dialog before any deletion of source files.

**Testing:**
- Run the pipeline on a folder of mixed images and videos with all actions enabled. Verify: videos are converted to MP4; files are rotated as flagged; backup copies appear in the backup folder with correctly generated names; files are sent to a test Telegram chat; source files are deleted only after all other steps succeed.
- Run the pipeline with a file that already exists in the backup folder — verify it is counted as a duplicate in the summary and the existing backup is not overwritten.
- Run the pipeline and cancel mid-way — verify the already-processed files are saved and the remaining files are untouched.
- Run the pipeline with an invalid Telegram bot token — verify the send step fails gracefully, the run summary lists the file under failed with a reason, and the rest of the pipeline continues.
- Verify the run summary counts match the actual outcomes in the backup folder and source folder.

**Exit criteria:** A complete end-to-end run on a real folder of photos and videos produces correct output files, a correct run summary, and leaves the source folder in the expected state.

---

## Phase 6 — Distribution
> *Make the app installable and runnable on a clean machine with no development tools installed.*

**Tasks:**
1. Configure `dotnet publish` with `--self-contained --single-file` for Windows (x64), macOS (arm64 + x64), and Linux (x64) targets.
2. Bundle the FFmpeg binary alongside the published executable via a post-build copy step.
3. Create a Windows installer (`.msi`) using WiX Toolset or a simple NSIS script wrapping the single-file publish output.
4. Create a macOS `.dmg` with a standard app bundle structure.
5. Sign the Windows installer and notarise the macOS bundle.
6. Write a one-page quick-start guide covering: installation, creating a first device profile, and running the first pipeline.

**Testing:**
- Install the app on a clean virtual machine (Windows, macOS) with no .NET SDK, no FFmpeg, and no development tools present. Launch the app — verify it starts without errors.
- On the clean machine: create a device profile, load a folder of real media, run the full pipeline. Verify output matches Phase 5 acceptance criteria.
- Uninstall the app — verify no leftover files remain outside the application data folder.

**Exit criteria:** A non-developer user can install the app from the distributed file, complete a full pipeline run, and uninstall it cleanly, without touching a terminal.
