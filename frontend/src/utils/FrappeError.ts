import axios from "axios";

export interface FrappeErrorData {
    exception?: unknown;
    message?: unknown;
}

export function extractFrappeMessage(err: unknown): string {
    if (axios.isAxiosError(err)) {
        const data = err.response?.data as FrappeErrorData | undefined;
        if (data?.message && typeof data.message === "string") {
            return data.message;
        }
        if (err.message) return err.message;
        // Last resort: Frappe often sets message to a generic
        // "Internal Server Error" while exception holds the real reason.
        // Full text goes to the console for the owner; the UI shows a
        // bounded slice so raw tracebacks never render verbatim.
        if (typeof data?.exception === "string" && data.exception) {
            console.warn("[frappe]", data.exception);
            return data.exception.slice(0, 160);
        }
    }
    if (err instanceof Error && err.message) return err.message;
    return "Something went wrong. Please try again.";
}
