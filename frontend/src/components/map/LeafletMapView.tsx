import React from 'react';
import { MapContainer, Marker, Popup } from 'react-leaflet';
import L from 'leaflet';
import 'leaflet/dist/leaflet.css';
import './LeafletMapView.css';
import TileLayerWithFallback from './TileLayerWithFallback';

// Fix Leaflet icon issue
delete (L.Icon.Default.prototype as any)._getIconUrl;
L.Icon.Default.mergeOptions({
  iconRetinaUrl: 'https://cdnjs.cloudflare.com/ajax/libs/leaflet/1.7.1/images/marker-icon-2x.png',
  iconUrl: 'https://cdnjs.cloudflare.com/ajax/libs/leaflet/1.7.1/images/marker-icon.png',
  shadowUrl: 'https://cdnjs.cloudflare.com/ajax/libs/leaflet/1.7.1/images/marker-shadow.png',
});

interface LeafletMapViewProps {
  latitude: number;
  longitude: number;
  address?: string;
}

const LeafletMapView: React.FC<LeafletMapViewProps> = ({ 
  latitude, 
  longitude, 
  address 
}) => {
  const position: [number, number] = [latitude, longitude];

  // Tạo URL để mở Google Maps (cho chỉ đường)
  const openInMaps = () => {
    const url = `https://www.google.com/maps/search/?api=1&query=${latitude},${longitude}`;
    window.open(url, '_blank');
  };

  return (
    <div className="leaflet-map-view">
      <div style={{ height: '350px', width: '100%', position: 'relative' }}>
        <MapContainer
          center={position}
          zoom={15}
          style={{ height: '100%', width: '100%', zIndex: 1 }}
          scrollWheelZoom={false}
          dragging={true}
          zoomControl={true}
        >
          <TileLayerWithFallback maxZoom={19} />
          <Marker position={position}>
            <Popup>
              <div className="map-popup-content">
                {address && (
                  <>
                    <strong>📍 Địa chỉ:</strong>
                    <br />
                    {address}
                    <br />
                    <br />
                  </>
                )}
                <strong>Tọa độ:</strong>
                <br />
                {latitude.toFixed(6)}, {longitude.toFixed(6)}
              </div>
            </Popup>
          </Marker>
        </MapContainer>
      </div>

      <div className="map-view-actions">
        <button onClick={openInMaps} className="open-maps-button">
          🗺️ Xem đường đi trên Google Maps
        </button>
      </div>
    </div>
  );
};

export default LeafletMapView;
