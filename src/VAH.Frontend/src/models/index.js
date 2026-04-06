/**
 * Frontend domain models — OOP classes wrapping plain API objects.
 *
 * Benefits:
 * - Computed properties (type checks, display helpers, validation)
 * - Encapsulation (behaviour attached to data)
 * - Single source of truth for asset/collection/tag logic in FE
 *
 * These classes are intentionally light — they wrap API data rather
 * than copying it, so serialization back to API is trivial.
 */

import { staticUrl } from '../api/client';

// ─────────────────────────────────────────────
//  Asset
// ─────────────────────────────────────────────

/** Content type constants mirroring backend AssetContentType enum. */
export const ContentType = Object.freeze({
  File: 'file',
  Image: 'image',
  Link: 'link',
  Color: 'color',
  ColorGroup: 'colorGroup',
  Folder: 'folder',
});

const CONTENT_TYPE_LABELS = {
  [ContentType.Image]: 'Hình ảnh',
  [ContentType.Link]: 'Liên kết',
  [ContentType.Color]: 'Màu sắc',
  [ContentType.ColorGroup]: 'Nhóm màu',
  [ContentType.Folder]: 'Thư mục',
  [ContentType.File]: 'Tệp tin',
};

/**
 * Asset domain model — wraps a plain asset object from the API.
 */
export class Asset {
  /** @param {object} data - Raw API response object */
  constructor(data = {}) {
    Object.assign(this, data);
  }

  // ── Type checks (computed) ──

  get isImage() { return this.contentType === ContentType.Image; }
  get isLink() { return this.contentType === ContentType.Link; }
  get isColor() { return this.contentType === ContentType.Color; }
  get isColorGroup() { return this.contentType === ContentType.ColorGroup; }
  get isFile() { return this.contentType === ContentType.File; }

  /** Whether this asset has a physical uploaded file. */
  get hasPhysicalFile() {
    return this.isImage || this.isFile;
  }

  /** Whether thumbnails can exist for this asset. */
  get canHaveThumbnails() { return this.isImage; }

  // ── Display helpers ──

  /** Localized label for the content type. */
  get contentTypeLabel() {
    return CONTENT_TYPE_LABELS[this.contentType] || this.contentType;
  }

  /** Full URL for the asset's main file/image. */
  get fileUrl() {
    return staticUrl(this.filePath);
  }

  /** Best available thumbnail URL (lg > md > sm > fileUrl). */
  get thumbnailUrl() {
    if (this.thumbnailLg) return staticUrl(this.thumbnailLg);
    if (this.thumbnailMd) return staticUrl(this.thumbnailMd);
    if (this.thumbnailSm) return staticUrl(this.thumbnailSm);
    return this.isImage ? this.fileUrl : '';
  }

  /** Small thumbnail URL. */
  get thumbnailSmUrl() {
    return this.thumbnailSm ? staticUrl(this.thumbnailSm) : this.thumbnailUrl;
  }

  /** Display icon for non-image types. */
  get icon() {
    if (this.isFolder) return '📁';
    if (this.isLink) return '🔗';
    if (this.isColor) return '🎨';
    if (this.isColorGroup) return '🎯';
    return '📄';
  }

  /** Tag list (parsed from comma-separated string). */
  get tagList() {
    if (!this.tags) return [];
    return this.tags.split(',').map(t => t.trim()).filter(Boolean);
  }

  /** Whether this asset has any tags. */
  get hasTags() { return this.tagList.length > 0; }

  /** Formatted creation date (vi-VN locale). */
  get formattedDate() {
    if (!this.createdAt) return '—';
    return new Date(this.createdAt).toLocaleDateString('vi-VN', {
      day: '2-digit', month: '2-digit', year: 'numeric',
      hour: '2-digit', minute: '2-digit',
    });
  }

  // ── Validation ──

  /** Whether the asset data looks valid (has required fields). */
  get isValid() {
    return !!(this.id && this.fileName && this.contentType);
  }

  // ── Serialization ──

  /** Return plain object (for sending back to API). */
  toJSON() {
    const obj = { ...this };
    // Remove computed getter keys (they'll be recomputed)
    return obj;
  }
}

// ─────────────────────────────────────────────
//  Collection
// ─────────────────────────────────────────────

/** Collection type constants mirroring backend CollectionType enum. */
export const CollectionType = Object.freeze({
  Default: 'default',
  Image: 'image',
  Link: 'link',
  Color: 'color',
});

/**
 * Collection domain model.
 */
export class Collection {
  constructor(data = {}) {
    Object.assign(this, data);
  }

  // ── Type checks ──

  get isColorCollection() { return this.type === CollectionType.Color; }
  get isImageCollection() { return this.type === CollectionType.Image; }
  get isLinkCollection() { return this.type === CollectionType.Link; }
  get isDefaultCollection() { return this.type === CollectionType.Default; }

  /** Whether folder creation makes sense in this collection type. */
  get supportsFolders() {
    return this.isImageCollection || this.isDefaultCollection;
  }

  /** Whether link creation makes sense in this collection type. */
  get supportsLinks() {
    return this.isLinkCollection || this.isDefaultCollection;
  }

  /** Whether this is a system/shared collection (no owner). */
  get isSystemCollection() { return !this.userId; }

  /** Whether this collection is owned by the given user ID. */
  isOwnedBy(userId) { return this.userId === userId; }

  get isValid() {
    return !!(this.id && this.name);
  }

  toJSON() { return { ...this }; }
}

// ─────────────────────────────────────────────
//  Tag
// ─────────────────────────────────────────────

/**
 * Tag domain model.
 */
export class Tag {
  constructor(data = {}) {
    Object.assign(this, data);
  }

  /** Display badge style based on tag color. */
  get badgeStyle() {
    if (!this.color) return {};
    return {
      backgroundColor: this.color,
      color: this.#contrastColor(this.color),
    };
  }

  /** Whether this tag has a custom color. */
  get hasColor() { return !!this.color; }

  get isValid() {
    return !!(this.id && this.name);
  }

  /** Compute contrasting text color for readability. */
  #contrastColor(hex) {
    if (!hex || !hex.startsWith('#')) return '#fff';
    const r = parseInt(hex.slice(1, 3), 16);
    const g = parseInt(hex.slice(3, 5), 16);
    const b = parseInt(hex.slice(5, 7), 16);
    // W3C luminance formula
    const luminance = (0.299 * r + 0.587 * g + 0.114 * b) / 255;
    return luminance > 0.5 ? '#000' : '#fff';
  }

  toJSON() { return { ...this }; }
}

// ─────────────────────────────────────────────
//  Mapping helpers (API response → domain object)
// ─────────────────────────────────────────────

/** Map a single API asset object to an Asset domain model. */
export const toAsset = (data) => data instanceof Asset ? data : new Asset(data);

/** Map an array of API assets to Asset domain models. */
export const toAssets = (arr) => (arr || []).map(toAsset);

/** Map a single API collection object to a Collection domain model. */
export const toCollection = (data) => data instanceof Collection ? data : new Collection(data);

/** Map an array of API collections. */
export const toCollections = (arr) => (arr || []).map(toCollection);

/** Map a single API tag object to a Tag domain model. */
export const toTag = (data) => data instanceof Tag ? data : new Tag(data);

/** Map an array of API tags. */
export const toTags = (arr) => (arr || []).map(toTag);
