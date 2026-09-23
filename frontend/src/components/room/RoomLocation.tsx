import React from 'react';
import { FiMapPin, FiExternalLink } from 'react-icons/fi';
import LeafletMap from '../map/LeafletMap';
import './RoomLocation.css';

interface RoomLocationProps {
  address?: string;
  ward?: string;
  district?: string;
  province?: string;
  latitude?: number | null;
  longitude?: number | null;
  title?: string;
}

const RoomLocation: React.FC<RoomLocationProps> = ({
  address,
  ward,
  district,
  province,
  latitude,
  longitude,
  title = 'Vị trí phòng trọ',
}) => {
  const fullAddress = [address, ward, district, province]
    .filter(Boolean)
    .join(', ');

  const googleMapsUrl = `https://www.google.com/maps/search/?api=1&query=${encodeURIComponent(
    fullAddress
  )}`;

  // Default coordinate if lat/lng is missing (Hanoi Center)
  const defaultLat = 21.0285;
  const defaultLng = 105.8542;

  const lat = latitude || defaultLat;
  const lng = longitude || defaultLng;

  return (
    <section className="room-location-card">
      <div className="room-location-header">
        <div className="room-location-title-wrapper">
          <div className="room-location-icon">
            <FiMapPin />
          </div>

          <div>
            <h2 className="room-location-title">Vị trí phòng trọ</h2>
            <p className="room-location-subtitle">Bản đồ số OpenStreetMap</p>
          </div>
        </div>
      </div>

      <div className="room-location-address">
        <FiMapPin className="room-location-address-icon" />
        <span>{fullAddress || 'Chưa cập nhật địa chỉ'}</span>
      </div>

      <div className="my-4">
        <LeafletMap
          height="320px"
          singleRoom={{
            latitude: lat,
            longitude: lng,
            title: title,
            address: fullAddress,
          }}
          zoom={15}
        />
      </div>

      <div className="pt-2">
        <a
          href={googleMapsUrl}
          target="_blank"
          rel="noopener noreferrer"
          className="room-location-google-link"
        >
          <FiExternalLink />
          <span>Mở chỉ đường trên Google Maps</span>
        </a>
      </div>
    </section>
  );
};

export default RoomLocation;
