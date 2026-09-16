import React from 'react';
import { FiMapPin, FiExternalLink } from 'react-icons/fi';
import './RoomLocation.css';

interface RoomLocationProps {
  address?: string;
  ward?: string;
  district?: string;
  province?: string;
}

const RoomLocation: React.FC<RoomLocationProps> = ({
  address,
  ward,
  district,
  province,
}) => {
  const fullAddress = [address, ward, district, province]
    .filter(Boolean)
    .join(', ');

  const mapUrl = `https://www.google.com/maps?q=${encodeURIComponent(
    fullAddress
  )}&output=embed`;

  const googleMapsUrl = `https://www.google.com/maps/search/?api=1&query=${encodeURIComponent(
    fullAddress
  )}`;

  return (
    <section className="room-location-card">
      <div className="room-location-header">
        <div className="room-location-title-wrapper">
          <div className="room-location-icon">
            <FiMapPin />
          </div>

          <div>
            <h2 className="room-location-title">Vị trí phòng trọ</h2>

            <p className="room-location-subtitle">Vị trí trên bản đồ</p>
          </div>
        </div>
      </div>

      <div className="room-location-address">
        <FiMapPin className="room-location-address-icon" />

        <span>{fullAddress || 'Chưa cập nhật địa chỉ'}</span>
      </div>

      {fullAddress ? (
        <>
          <div className="room-location-map">
            <iframe
              src={mapUrl}
              title="Vị trí phòng trọ trên Google Maps"
              loading="lazy"
              allowFullScreen
              referrerPolicy="no-referrer-when-downgrade"
            />
          </div>

          <a
            href={googleMapsUrl}
            target="_blank"
            rel="noopener noreferrer"
            className="room-location-google-link"
          >
            <FiExternalLink />
            <span>Xem vị trí trên Google Maps</span>
          </a>
        </>
      ) : (
        <div className="room-location-empty">
          <FiMapPin />
          <p>Phòng trọ chưa cập nhật vị trí.</p>
        </div>
      )}
    </section>
  );
};

export default RoomLocation;
