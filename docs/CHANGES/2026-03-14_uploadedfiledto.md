# UploadedFileDto improvements — 2026-03-14

Summary
- Convert `UploadedFileDto` to implement `IUploadedFile` for testability.
- Add `UploadedFileMetadataDto` (serializable) and `CreateMetadata()` to produce a metadata-only DTO for transport boundaries.
- Add `OpenStreamAsync` overload so callers can obtain async streams via `Func<CancellationToken, Task<Stream>>`.
- Add validation: non-negative `Length`, filename policy (no invalid chars, no directory separators, max length 260), and constructor guards.
- Add `ValidateLengthAsync` to optionally verify that `Length` matches actual stream bytes (supports both sync and async factories).
- Add XML docs clarifying ownership: stream factories return Streams that the caller MUST dispose.

Why
- Move away from ASP.NET Core types so higher layers remain transport-agnostic.
- Improve testability (via `IUploadedFile`) and serialization boundaries (via `UploadedFileMetadataDto`).
- Support async I/O patterns and explicit ownership semantics.

Files changed
- VAH.Backend/Models/UploadedFileDto.cs
- VAH.Backend/Models/IUploadedFile.cs (new)
- VAH.Backend/Models/UploadedFileMetadataDto.cs (new)
- docs/CHANGES/2026-03-14_uploadedfiledto.md (this file)

Migration / Usage notes
- If code previously serialized `UploadedFileDto` directly, switch to passing `UploadedFileMetadataDto` across the wire instead.
- Callers receiving a sync/async stream factory must dispose the returned `Stream`.
- To verify length programmatically, call `ValidateLengthAsync()`; this will open and consume the stream internally.

Tests
- Unit tests should cover constructor validation, `CreateMetadata()`, and `ValidateLengthAsync()` behaviours.

Follow-up
- Add unit tests in the upcoming sprint before merging to `main`.
- Consider adjusting the 260-char filename limit to match platform policy if needed.
