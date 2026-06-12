const apiBase = (import.meta as any).env.VITE_API_URL as string;

export function getLogoProxyUrl(siteId: string, companyName: string): string {
  return `${apiBase}/sites/${siteId}/logo?company=${encodeURIComponent(companyName)}`;
}
