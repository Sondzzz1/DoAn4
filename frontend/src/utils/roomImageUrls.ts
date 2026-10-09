export function isRoomImageUrl(value: string): boolean {
  const url = value.trim();
  if (!url || url.length > 1000 || url.includes('\\')) return false;
  try {
    if (/^https?:\/\//i.test(url)) {
      const parsed = new URL(url);
      return !!parsed.hostname && ['http:', 'https:'].includes(parsed.protocol);
    }
    return url.startsWith('/uploads/') && new URL(url, 'https://local.invalid').pathname.startsWith('/uploads/');
  } catch { return false; }
}

export function normalizeRoomImageUrls(values: string[]): string[] {
  const urls = values.map(value => value.trim());
  if (urls.some(url => !isRoomImageUrl(url))) {
    throw new Error('Ảnh phòng chỉ chấp nhận URL http://, https:// hoặc /uploads/... (tối đa 1000 ký tự). Không chấp nhận ảnh Base64.');
  }
  return [...new Set(urls)];
}
