import React, { useEffect, useRef } from 'react';
import L from 'leaflet';
import 'leaflet/dist/leaflet.css';
import { PostListItem } from '../../types/post.types';
import { formatPrice } from '../../utils/helpers';

// Fix Leaflet default marker icon in Vite/Webpack
delete (L.Icon.Default.prototype as any)._getIconUrl;
L.Icon.Default.mergeOptions({
  iconRetinaUrl: 'https://unpkg.com/leaflet@1.9.4/dist/images/marker-icon-2x.png',
  iconUrl: 'https://unpkg.com/leaflet@1.9.4/dist/images/marker-icon.png',
  shadowUrl: 'https://unpkg.com/leaflet@1.9.4/dist/images/marker-shadow.png',
});

// Custom pin icons
const blueIcon = new L.Icon({
  iconUrl: 'https://raw.githubusercontent.com/pointhi/leaflet-color-markers/master/img/marker-icon-2x-blue.png',
  shadowUrl: 'https://cdnjs.cloudflare.com/ajax/libs/leaflet/1.9.4/images/marker-shadow.png',
  iconSize: [25, 41],
  iconAnchor: [12, 41],
  popupAnchor: [1, -34],
  shadowSize: [41, 41],
});

const redIcon = new L.Icon({
  iconUrl: 'https://raw.githubusercontent.com/pointhi/leaflet-color-markers/master/img/marker-icon-2x-red.png',
  shadowUrl: 'https://cdnjs.cloudflare.com/ajax/libs/leaflet/1.9.4/images/marker-shadow.png',
  iconSize: [25, 41],
  iconAnchor: [12, 41],
  popupAnchor: [1, -34],
  shadowSize: [41, 41],
});

interface LeafletMapProps {
  center?: [number, number]; // [lat, lng] - Mặc định Hà Nội [21.0285, 105.8542]
  zoom?: number;
  height?: string;
  rooms?: PostListItem[]; // Danh sách phòng hiển thị nhiều markers
  singleRoom?: {
    latitude: number;
    longitude: number;
    title: string;
    address?: string;
  };
  radiusCircle?: {
    center: [number, number];
    radiusInKm: number;
  };
  enablePicker?: boolean;
  onLocationSelect?: (lat: number, lng: number) => void;
}

const LeafletMap: React.FC<LeafletMapProps> = ({
  center = [21.0285, 105.8542],
  zoom = 13,
  height = '400px',
  rooms = [],
  singleRoom,
  radiusCircle,
  enablePicker = false,
  onLocationSelect,
}) => {
  const mapContainerRef = useRef<HTMLDivElement>(null);
  const mapInstanceRef = useRef<L.Map | null>(null);
  const markersLayerRef = useRef<L.LayerGroup | null>(null);
  const circleLayerRef = useRef<L.Circle | null>(null);
  const pickerMarkerRef = useRef<L.Marker | null>(null);

  useEffect(() => {
    if (!mapContainerRef.current) return;

    // Khởi tạo map nếu chưa có
    if (!mapInstanceRef.current) {
      const initialCenter: [number, number] = singleRoom
        ? [singleRoom.latitude, singleRoom.longitude]
        : radiusCircle
        ? radiusCircle.center
        : center;

      const map = L.map(mapContainerRef.current, {
        center: initialCenter,
        zoom: zoom,
        scrollWheelZoom: true,
      });

      L.tileLayer('https://{s}.tile.openstreetmap.org/{z}/{x}/{y}.png', {
        attribution: '&copy; <a href="https://www.openstreetmap.org/copyright">OpenStreetMap</a>',
        maxZoom: 19,
      }).addTo(map);

      markersLayerRef.current = L.layerGroup().addTo(map);
      mapInstanceRef.current = map;

      // Click to pick location
      if (enablePicker && onLocationSelect) {
        map.on('click', (e: L.LeafletMouseEvent) => {
          const { lat, lng } = e.latlng;
          if (pickerMarkerRef.current) {
            pickerMarkerRef.current.setLatLng([lat, lng]);
          } else {
            pickerMarkerRef.current = L.marker([lat, lng], { icon: redIcon }).addTo(map);
          }
          onLocationSelect(lat, lng);
        });
      }
    }

    return () => {
      if (mapInstanceRef.current) {
        mapInstanceRef.current.remove();
        mapInstanceRef.current = null;
      }
    };
  }, []);

  // Cập nhật markers & circle khi props thay đổi
  useEffect(() => {
    const map = mapInstanceRef.current;
    const markersLayer = markersLayerRef.current;
    if (!map || !markersLayer) return;

    markersLayer.clearLayers();

    // 1. Single Room Marker
    if (singleRoom && singleRoom.latitude && singleRoom.longitude) {
      const marker = L.marker([singleRoom.latitude, singleRoom.longitude], { icon: redIcon });
      marker.bindPopup(`
        <div style="font-family: sans-serif; font-size: 13px; max-width: 220px;">
          <b style="color: #0f172a; font-size: 14px;">${singleRoom.title}</b>
          <p style="color: #64748b; margin: 4px 0 0 0;">${singleRoom.address || ''}</p>
        </div>
      `);
      markersLayer.addLayer(marker);
      map.setView([singleRoom.latitude, singleRoom.longitude], zoom);
    }

    // 2. Radius Circle
    if (radiusCircle) {
      if (circleLayerRef.current) {
        circleLayerRef.current.remove();
      }

      const circle = L.circle(radiusCircle.center, {
        color: '#0084ff',
        fillColor: '#0084ff',
        fillOpacity: 0.15,
        radius: radiusCircle.radiusInKm * 1000, // đổi km sang meters
      }).addTo(map);

      circleLayerRef.current = circle;

      // Center marker
      const centerMarker = L.marker(radiusCircle.center, { icon: redIcon });
      centerMarker.bindPopup(`<b>Vị trí tìm kiếm</b><br/>Bán kính: ${radiusCircle.radiusInKm} km`);
      markersLayer.addLayer(centerMarker);

      map.setView(radiusCircle.center, Math.max(11, 15 - Math.round(radiusCircle.radiusInKm / 3)));
    }

    // 3. Multi Room Markers
    if (rooms && rooms.length > 0) {
      const validRooms = rooms.filter((r) => r.latitude && r.longitude);

      validRooms.forEach((room) => {
        if (!room.latitude || !room.longitude) return;

        const marker = L.marker([Number(room.latitude), Number(room.longitude)], { icon: blueIcon });
        const distanceText = room.distanceInKm ? `<span style="color: #059669; font-weight: bold;">📍 Cách bạn: ${room.distanceInKm} km</span><br/>` : '';

        marker.bindPopup(`
          <div style="font-family: sans-serif; font-size: 12px; max-width: 220px;">
            ${room.thumbnailUrl ? `<img src="${room.thumbnailUrl}" style="width: 100%; height: 90px; object-fit: cover; border-radius: 8px; margin-bottom: 6px;" />` : ''}
            <b style="color: #0f172a; font-size: 13px; line-height: 1.3; display: block; margin-bottom: 4px;">${room.title}</b>
            <span style="color: #0084ff; font-weight: bold; font-size: 13px;">${formatPrice(room.price)}/tháng</span>
            <p style="color: #64748b; margin: 4px 0 6px 0;">${room.address || ''}</p>
            ${distanceText}
            <a href="/rooms/${room.id}" style="display: inline-block; padding: 4px 8px; background: #0084ff; color: #fff; text-decoration: none; border-radius: 6px; font-weight: bold; font-size: 11px;">Xem chi tiết</a>
          </div>
        `);

        markersLayer.addLayer(marker);
      });
    }
  }, [rooms, singleRoom, radiusCircle, zoom]);

  return (
    <div className="relative w-full rounded-3xl overflow-hidden border border-slate-200 shadow-sm z-0">
      <div ref={mapContainerRef} style={{ width: '100%', height }} />
    </div>
  );
};

export default LeafletMap;
