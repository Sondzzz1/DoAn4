import React, { useState, useRef, useCallback, useEffect } from 'react';
import { MapContainer, Marker, Popup, useMapEvents } from 'react-leaflet';
import L from 'leaflet';
import 'leaflet/dist/leaflet.css';
import './LeafletMapPicker.css';
import TileLayerWithFallback from './TileLayerWithFallback';
import api from '../../services/api';

// Fix Leaflet icon issue with Webpack/Vite
delete (L.Icon.Default.prototype as any)._getIconUrl;
L.Icon.Default.mergeOptions({
  iconRetinaUrl: 'https://cdnjs.cloudflare.com/ajax/libs/leaflet/1.7.1/images/marker-icon-2x.png',
  iconUrl: 'https://cdnjs.cloudflare.com/ajax/libs/leaflet/1.7.1/images/marker-icon.png',
  shadowUrl: 'https://cdnjs.cloudflare.com/ajax/libs/leaflet/1.7.1/images/marker-shadow.png',
});

interface LeafletMapPickerProps {
  latitude?: number;
  longitude?: number;
  onLocationChange: (lat: number, lng: number) => void;
  // Props để tự động geocoding từ địa chỉ
  address?: string;
  ward?: string;
  district?: string;
  province?: string;
}

// Component để xử lý click trên map
const MapClickHandler: React.FC<{ onLocationSelect: (lat: number, lng: number) => void }> = ({ 
  onLocationSelect 
}) => {
  useMapEvents({
    click: (e) => {
      onLocationSelect(e.latlng.lat, e.latlng.lng);
    },
  });
  return null;
};

const LeafletMapPicker: React.FC<LeafletMapPickerProps> = ({
  latitude = 20.9432, // Mỹ Hào, Hưng Yên default
  longitude = 106.1032,
  onLocationChange,
  address = '',
  ward = '',
  district = '',
  province = '',
}) => {
  // Ensure valid coordinates
  const validLat = (latitude && !isNaN(latitude) && latitude >= -90 && latitude <= 90) ? latitude : 20.9432;
  const validLng = (longitude && !isNaN(longitude) && longitude >= -180 && longitude <= 180) ? longitude : 106.1032;
  
  const [position, setPosition] = useState<[number, number]>([validLat, validLng]);
  const [isGeocoding, setIsGeocoding] = useState(false);
  const mapRef = useRef<L.Map>(null);
  const debounceTimerRef = useRef<number | null>(null);

  // Update position when props change (for edit mode)
  useEffect(() => {
    if (latitude && longitude && !isNaN(latitude) && !isNaN(longitude)) {
      const newLat = latitude >= -90 && latitude <= 90 ? latitude : 20.9432;
      const newLng = longitude >= -180 && longitude <= 180 ? longitude : 106.1032;
      setPosition([newLat, newLng]);
      
      // Move map to new position
      if (mapRef.current) {
        mapRef.current.setView([newLat, newLng], 13);
      }
    }
  }, [latitude, longitude]);

  // Auto geocoding khi địa chỉ thay đổi (debounced)
  useEffect(() => {
    // Clear previous timer
    if (debounceTimerRef.current) {
      window.clearTimeout(debounceTimerRef.current);
    }

    // Build full address
    const fullAddress = [address, ward, district, province, 'Việt Nam']
      .filter(Boolean)
      .join(', ')
      .trim();

    // Only geocode if we have meaningful address (at least address + province)
    if (address && province && fullAddress.length > 10) {
      debounceTimerRef.current = window.setTimeout(() => {
        handleGeocoding(fullAddress);
      }, 800); // Debounce 800ms
    }

    // Cleanup
    return () => {
      if (debounceTimerRef.current) {
        window.clearTimeout(debounceTimerRef.current);
      }
    };
  }, [address, ward, district, province]);

  // Geocoding function
  const handleGeocoding = async (fullAddress: string) => {
    setIsGeocoding(true);
    try {
      const response = await api.get('/location/search', {
        params: { q: fullAddress }
      });

      const result = response.data;

      if (result.success && result.data && result.data.length > 0) {
        const location = result.data[0];
        const newPosition: [number, number] = [location.latitude, location.longitude];
        setPosition(newPosition);
        onLocationChange(location.latitude, location.longitude);

        // Di chuyển map đến vị trí mới với animation
        if (mapRef.current) {
          mapRef.current.flyTo(newPosition, 16, {
            duration: 1.5,
          });
        }
      }
    } catch (error: any) {
      console.error('Geocoding error:', error);
      // Silent fail - không hiện alert để không làm phiền người dùng
    } finally {
      setIsGeocoding(false);
    }
  };

  // Xử lý khi click chọn vị trí trên map
  const handleLocationSelect = useCallback((lat: number, lng: number) => {
    const newPosition: [number, number] = [lat, lng];
    setPosition(newPosition);
    onLocationChange(lat, lng);
  }, [onLocationChange]);

  // Xử lý khi kéo marker
  const handleMarkerDragEnd = useCallback((e: L.DragEndEvent) => {
    const marker = e.target;
    const newPosition = marker.getLatLng();
    setPosition([newPosition.lat, newPosition.lng]);
    onLocationChange(newPosition.lat, newPosition.lng);
  }, [onLocationChange]);

  // Lấy vị trí hiện tại
  const handleGetCurrentLocation = () => {
    if (navigator.geolocation) {
      navigator.geolocation.getCurrentPosition(
        (pos) => {
          const { latitude, longitude } = pos.coords;
          const newPosition: [number, number] = [latitude, longitude];
          setPosition(newPosition);
          onLocationChange(latitude, longitude);

          if (mapRef.current) {
            mapRef.current.flyTo(newPosition, 16);
          }
        },
        (error) => {
          console.error('Geolocation error:', error);
          alert('Không thể lấy vị trí hiện tại. Vui lòng kiểm tra quyền truy cập vị trí.');
        }
      );
    } else {
      alert('Trình duyệt của bạn không hỗ trợ Geolocation.');
    }
  };

  return (
    <div className="leaflet-map-picker">
      {isGeocoding && (
        <div className="absolute top-4 right-4 z-[1000] bg-blue-500 text-white px-4 py-2 rounded-lg shadow-lg flex items-center gap-2">
          <div className="animate-spin rounded-full h-4 w-4 border-2 border-white border-t-transparent"></div>
          <span className="text-sm font-medium">Đang tìm vị trí...</span>
        </div>
      )}

      <div style={{ height: '400px', width: '100%', position: 'relative' }}>
        <MapContainer
          center={position}
          zoom={13}
          style={{ height: '100%', width: '100%', zIndex: 1 }}
          scrollWheelZoom={true}
          ref={mapRef}
        >
          <TileLayerWithFallback maxZoom={19} />
          <MapClickHandler onLocationSelect={handleLocationSelect} />
          <Marker 
            position={position}
            draggable={true}
            eventHandlers={{
              dragend: handleMarkerDragEnd,
            }}
          >
            <Popup>
              <strong>Vị trí đã chọn</strong>
              <br />
              Lat: {position[0].toFixed(6)}
              <br />
              Lng: {position[1].toFixed(6)}
            </Popup>
          </Marker>
        </MapContainer>
      </div>

      <div className="map-picker-footer">
        <div className="selected-location">
          📍 <strong>Vị trí đã chọn:</strong> {position[0].toFixed(6)}, {position[1].toFixed(6)}
        </div>
        <button onClick={handleGetCurrentLocation} className="current-location-button">
          📍 Lấy vị trí hiện tại
        </button>
      </div>

      <div className="map-picker-hint">
        💡 <strong>Hướng dẫn:</strong> Click vào bản đồ hoặc kéo marker để điều chỉnh vị trí chính xác
      </div>
    </div>
  );
};

export default LeafletMapPicker;
