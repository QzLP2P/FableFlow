/** Erreur de stockage local (indisponible, refusé, plein) avec un message affichable. */
export class LocalDataError extends Error {
  constructor(message: string, options?: { cause?: unknown }) {
    super(message, options);
    this.name = "LocalDataError";
  }
}

export function describeStorageFailure(error: unknown): string {
  if (error instanceof LocalDataError) {
    return error.message;
  }

  if (error instanceof DOMException && error.name === "QuotaExceededError") {
    return "L'espace de stockage de ce navigateur est plein. Supprimez d'anciennes histoires puis réessayez.";
  }

  return "Le stockage de ce navigateur est indisponible (navigation privée ou données bloquées).";
}

export function createId(): string {
  if (typeof crypto !== "undefined" && typeof crypto.randomUUID === "function") {
    return crypto.randomUUID();
  }

  return `${Date.now().toString(36)}-${Math.random().toString(36).slice(2, 10)}`;
}

export function isRecord(value: unknown): value is Record<string, unknown> {
  return typeof value === "object" && value !== null && !Array.isArray(value);
}

export function isBoundedString(value: unknown, maxLength: number, allowEmpty = false): value is string {
  return typeof value === "string" && value.length <= maxLength && (allowEmpty || value.trim().length > 0);
}

export function isOptionalString(value: unknown, maxLength: number): value is string | null {
  return value === null || isBoundedString(value, maxLength, true);
}
