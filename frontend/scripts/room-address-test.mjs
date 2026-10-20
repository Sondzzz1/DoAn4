import assert from 'node:assert/strict';
import { buildLocationQuery, completeRoomAddress, mergeLocatedAddress } from '../src/utils/roomAddress.ts';

const fields = { diaChi: '18 P. Hai Bà Trưng, Cửa Nam, Hà Nội 100000, Việt Nam', phuong: '', quan: '', thanhPho: '' };
const completed = completeRoomAddress(fields);
assert.deepEqual(completed, { diaChi: '18 Phố Hai Bà Trưng', phuong: 'Cửa Nam', quan: '', thanhPho: 'Hà Nội' });
assert.equal(buildLocationQuery(fields.diaChi, 'Cửa Nam', '', 'Hà Nội'), '18 Phố Hai Bà Trưng, Cửa Nam, Hà Nội, Việt Nam');
assert.equal(buildLocationQuery(completed.diaChi, completed.phuong, completed.quan, completed.thanhPho), '18 Phố Hai Bà Trưng, Cửa Nam, Hà Nội, Việt Nam');
const local = { ...completed, quan: 'Hoàn Kiếm' };
const road = { address: 'Phố Hai Bà Trưng', displayName: 'Phố Hai Bà Trưng, Hà Nội', street: 'Phố Hai Bà Trưng', houseNumber: '', province: 'Hà Nội', ward: 'Cửa Nam', district: '' };
assert.deepEqual(mergeLocatedAddress(local, road, 'search'), local);
assert.deepEqual(mergeLocatedAddress(local, { ...road, address: '49 Phố Hai Bà Trưng', houseNumber: '49' }, 'reverse'), local);
assert.equal(mergeLocatedAddress({ ...local, diaChi: '18 Phố Hai Bà Trưng, căn 201' }, road, 'search').diaChi, '18 Phố Hai Bà Trưng, căn 201');
assert.equal(mergeLocatedAddress({ ...local, diaChi: '18 Phố Hai Bà Trưng, căn 201' }, { ...road, address: '18 Phố Hai Bà Trưng', houseNumber: '18' }, 'search').diaChi, '18 Phố Hai Bà Trưng, căn 201');
assert.deepEqual(mergeLocatedAddress(local, { ...road, address: 'Đường mới', street: 'Đường mới', ward: '', province: 'Hưng Yên' }, 'reverse'),
  { diaChi: 'Đường mới', phuong: '', quan: '', thanhPho: 'Hưng Yên' });
assert.equal(buildLocationQuery('42 Nguyễn Lân, Phường Phương Liệt, Hà Nội', 'Phương Liệt', '', 'Hà Nội'), '42 Nguyễn Lân, Phường Phương Liệt, Hà Nội, Việt Nam');
assert.deepEqual(completeRoomAddress({ ...fields, diaChi: 'Phòng 201 không có địa chỉ đầy đủ' }), { ...fields, diaChi: 'Phòng 201 không có địa chỉ đầy đủ' });
console.log('PASS: address splitting, postal-code removal, abbreviation, deduplication, house/unit preservation, partial responses and cross-province selection.');
