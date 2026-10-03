import React from 'react';
import { FiMapPin } from 'react-icons/fi';
import LeafletMapView from '../map/LeafletMapView';
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

  // Check if we have valid coordinates
  const hasValidCoords = 
    latitude !== null && 
    latitude !== undefined && 
    longitude !== null && 
    longitude !== undefined &&
    latitude >= -90 && latitude <= 90 && 
    longitude >= -180 && longitude <= 180;

  return (
    <section className="room-location-card">
      <div className="room-location-header">
        <div className="room-location-title-wrapper">
          <div className="room-location-icon">
            <FiMapPin />
          </div>

          <div>
            <h2 className="room-location-title">{title}</h2>
            <p className="room-location-subtitle">
              {hasValidCoords ? 'Bản đồ Google Maps' : 'Thông tin địa chỉ'}
            </p>
          </div>
        </div>
      </div>

      <div className="room-location-address">
        <FiMapPin className="room-location-address-icon" />
        <span>{fullAddress || 'Chưa cập nhật địa chỉ'}</span>
      </div>

      <div className="my-4">
        {hasValidCoords ? (
          <LeafletMapView
            latitude={latitude!}
            longitude={longitude!}
            address={fullAddress}
          />
        ) : (
          <div className="room-location-no-map">
            <FiMapPin size={48} />
            <p className="no-map-title">Vị trí bản đồ chưa được cập nhật</p>
            <p className="no-map-subtitle">Chủ trọ chưa chọn vị trí trên bản đồ</p>
            <p className="no-map-address">{fullAddress}</p>
          </div>
        )}
      </div>
    </section>
  );
};

export default RoomLocation;
