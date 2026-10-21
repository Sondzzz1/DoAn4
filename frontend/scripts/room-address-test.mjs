import assert from 'node:assert/strict';
import { buildLocationQuery, completeRoomAddress, mergeLocatedAddress } from '../src/utils/roomAddress.ts';

// Fictional address fixtures; no external requests are made.
const fields = { diaChi: '9999 P. Kiểm Thử, Phường Mẫu, Hà Nội 100000, Việt Nam', phuong: '', quan: '', thanhPho: '' };
const completed = completeRoomAddress(fields);
assert.deepEqual(completed, { diaChi: '9999 Phố Kiểm Thử', phuong: 'Phường Mẫu', quan: '', thanhPho: 'Hà Nội' });
assert.equal(buildLocationQuery(fields.diaChi, 'Phường Mẫu', '', 'Hà Nội'), '9999 Phố Kiểm Thử, Phường Mẫu, Hà Nội, Việt Nam');
assert.equal(buildLocationQuery(completed.diaChi, completed.phuong, completed.quan, completed.thanhPho), '9999 Phố Kiểm Thử, Phường Mẫu, Hà Nội, Việt Nam');
const local = { ...completed, quan: 'Quận Mẫu' };
const road = { address: 'Phố Kiểm Thử', displayName: 'Phố Kiểm Thử, Hà Nội', street: 'Phố Kiểm Thử', houseNumber: '', province: 'Hà Nội', ward: 'Phường Mẫu', district: '' };
assert.deepEqual(mergeLocatedAddress(local, road, 'search'), local);
assert.deepEqual(mergeLocatedAddress(local, { ...road, address: '8888 Phố Kiểm Thử', houseNumber: '8888' }, 'reverse'), local);
assert.equal(mergeLocatedAddress({ ...local, diaChi: '9999 Phố Kiểm Thử, căn 201' }, road, 'search').diaChi, '9999 Phố Kiểm Thử, căn 201');
assert.equal(mergeLocatedAddress({ ...local, diaChi: '9999 Phố Kiểm Thử, căn 201' }, { ...road, address: '9999 Phố Kiểm Thử', houseNumber: '9999' }, 'search').diaChi, '9999 Phố Kiểm Thử, căn 201');
assert.deepEqual(mergeLocatedAddress(local, { ...road, address: 'Đường mới', street: 'Đường mới', ward: '', province: 'Hưng Yên' }, 'reverse'),
  { diaChi: 'Đường mới', phuong: '', quan: '', thanhPho: 'Hưng Yên' });
assert.equal(buildLocationQuery('42 Nguyễn Lân, Phường Phương Liệt, Hà Nội', 'Phương Liệt', '', 'Hà Nội'), '42 Nguyễn Lân, Phường Phương Liệt, Hà Nội, Việt Nam');
assert.deepEqual(completeRoomAddress({ ...fields, diaChi: 'Phòng 201 không có địa chỉ đầy đủ' }), { ...fields, diaChi: 'Phòng 201 không có địa chỉ đầy đủ' });
console.log('PASS: address splitting, postal-code removal, abbreviation, deduplication, house/unit preservation, partial responses and cross-province selection.');
