import axios from "axios";

export interface FrappeErrorData {
    exception?: unknown;
    message?: unknown;
}

export function extractFrappeMessage(err: unknown): string {
    if (axios.isAxiosError(err)) {
        const data = err.response?.data as FrappeErrorData | undefined;
        if (data) {
            if (typeof data.message === "string" && data.message) {
                return data.message;
            }
        }
        if (err.message) return err.message;
    }
    if (err instanceof Error && err.message) return err.message;
    return "Something went wrong. Please try again.";
}
