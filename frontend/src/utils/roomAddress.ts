export interface RoomAddressFields {
  diaChi: string;
  phuong: string;
  quan: string;
  thanhPho: string;
}

interface LocatedAddress {
  address: string;
  displayName: string;
  province: string;
  district: string;
  ward: string;
  street?: string;
  houseNumber?: string;
}

const fold = (value: string) => value.normalize('NFD').replace(/[\u0300-\u036f]/g, '').replace(/đ/gi, 'd').toLowerCase().replace(/\s+/g, ' ').trim();
const country = (value: string) => /^(viet nam|vietnam|vn)$/.test(fold(value));
const cleanPart = (value: string) => value.trim().replace(/\s+/g, ' ').replace(/\s+\d{5,6}$/, '');
const areaKey = (value: string) => fold(cleanPart(value).replace(/^(thành phố|tp\.?|tỉnh|quận|q\.|huyện|h\.|phường|p\.|xã)\s+/iu, ''));

export function streetAddress(value: string) {
  const first = value.split(',')[0].trim().replace(/^(\d[\w/-]*\s+)P\.\s*/iu, '$1Phố ').replace(/^P\.\s*/iu, 'Phố ');
  const match = first.match(/^(\d[\w/-]*)\s+(.+)$/u);
  return { address: first, houseNumber: match?.[1] || '', street: match?.[2] || first };
}

const streetKey = (value: string) => fold(value).replace(/^(pho|duong|p\.|d\.)\s+/, '');

export function buildLocationQuery(address = '', ward = '', district = '', province = '') {
  const seen = new Set<string>();
  const parts: string[] = [];
  for (const raw of [address, ward, district, province].flatMap(part => part.split(','))) {
    const part = cleanPart(raw);
    if (!part || country(part)) continue;
    const value = parts.length === 0 ? streetAddress(part).address : part;
    const key = areaKey(value);
    if (!seen.has(key)) { parts.push(value); seen.add(key); }
  }
  return [...parts, 'Việt Nam'].join(', ');
}

export function completeRoomAddress(fields: RoomAddressFields): RoomAddressFields {
  const parts = fields.diaChi.split(',').map(cleanPart).filter(Boolean);
  const hasCountry = country(parts.at(-1) || '');
  if (hasCountry) parts.pop();
  if (parts.length < 3 || (!hasCountry && !/^(tỉnh|thành phố|tp\.)\s/iu.test(parts.at(-1)!))) return fields;
  const province = parts.pop()!;
  let district = '';
  if (parts.length >= 3 || /^(quận|huyện|thị xã|q\.|h\.)\s/iu.test(parts.at(-1)!)) district = parts.pop()!;
  const ward = parts.length > 1 ? parts.pop()! : '';
  const provinceChanged = !!fields.thanhPho && areaKey(province) !== areaKey(fields.thanhPho);
  return { diaChi: [streetAddress(parts[0]).address, ...parts.slice(1)].join(', '),
    thanhPho: province, quan: district || (provinceChanged ? '' : fields.quan), phuong: ward || (provinceChanged ? '' : fields.phuong) };
}

export function mergeLocatedAddress(fields: RoomAddressFields, location: LocatedAddress, source: 'search' | 'reverse'): RoomAddressFields {
  const current = streetAddress(fields.diaChi);
  const found = streetAddress(location.address || location.displayName);
  const foundStreet = location.street || found.street;
  const foundHouse = location.houseNumber || found.houseNumber;
  const provinceChanged = !!fields.thanhPho && !!location.province && areaKey(fields.thanhPho) !== areaKey(location.province);
  const districtChanged = !!fields.quan && !!location.district && areaKey(fields.quan) !== areaKey(location.district);
  // Reverse geocoding describes the nearest mapped object, not necessarily this house.
  const preserveHouse = !!current.houseNumber && !provinceChanged && streetKey(current.street) === streetKey(foundStreet)
    && (!foundHouse || fold(foundHouse) === fold(current.houseNumber) || source === 'reverse');
  return {
    diaChi: preserveHouse ? fields.diaChi : location.address || fields.diaChi,
    thanhPho: location.province || fields.thanhPho,
    quan: location.district || (provinceChanged ? '' : fields.quan),
    phuong: location.ward || (provinceChanged || districtChanged ? '' : fields.phuong),
  };
}
