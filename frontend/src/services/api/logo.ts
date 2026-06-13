const apiBase = (import.meta as any).env.VITE_API_URL as string;

export function getLogoProxyUrl(siteId: string, companyName: string): string {
  const sanitizedBaseUrl = apiBase.endsWith("/") ? apiBase.slice(0, -1) : apiBase;
  return `${sanitizedBaseUrl}/sites/${siteId}/logo?company=${encodeURIComponent(companyName)}`;
}
