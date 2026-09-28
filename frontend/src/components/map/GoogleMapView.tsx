import React, { useEffect, useRef, useState } from 'react';
import { FiMapPin, FiExternalLink, FiAlertCircle } from 'react-icons/fi';
import L from 'leaflet';
import 'leaflet/dist/leaflet.css';
import './GoogleMapView.css';

// Fix Leaflet icons
delete (L.Icon.Default.prototype as any)._getIconUrl;
L.Icon.Default.mergeOptions({
  iconRetinaUrl: 'https://unpkg.com/leaflet@1.9.4/dist/images/marker-icon-2x.png',
  iconUrl: 'https://unpkg.com/leaflet@1.9.4/dist/images/marker-icon.png',
  shadowUrl: 'https://unpkg.com/leaflet@1.9.4/dist/images/marker-shadow.png',
});

interface GoogleMapViewProps {
  latitude: number;
  longitude: number;
  title?: string;
  address?: string;
  height?: string;
  zoom?: number;
}

const GoogleMapView: React.FC<GoogleMapViewProps> = ({
  latitude,
  longitude,
  title = 'Vị trí phòng trọ',
  address,
  height = '360px',
  zoom = 15,
}) => {
  const containerRef = useRef<HTMLDivElement>(null);
  const lMapInstanceRef = useRef<L.Map | null>(null);

  const [mapEngine, setMapEngine] = useState<'google' | 'leaflet'>('google');
  const [isGoogleLoaded, setIsGoogleLoaded] = useState(false);

  const API_KEY = import.meta.env.VITE_GOOGLE_MAPS_API_KEY;

  // Validate coordinates
  const isValidCoords =
    latitude !== null &&
    latitude !== undefined &&
    longitude !== null &&
    longitude !== undefined &&
    !isNaN(latitude) &&
    !isNaN(longitude) &&
    latitude >= -90 &&
    latitude <= 90 &&
    longitude >= -180 &&
    longitude <= 180;

  // 1. Listen for Google Maps Auth Failure
  useEffect(() => {
    window.gm_authFailure = () => {
      console.warn('Google Maps auth failed trong GoogleMapView. Chuyển sang Leaflet/OpenStreetMap.');
      setMapEngine('leaflet');
    };

    return () => {
      delete window.gm_authFailure;
    };
  }, []);

  // 2. Load Google Maps Script
  useEffect(() => {
    if (!API_KEY || API_KEY === 'YOUR_GOOGLE_MAPS_API_KEY_HERE') {
      setMapEngine('leaflet');
      return;
    }

    if (window.google && window.google.maps) {
      setIsGoogleLoaded(true);
      return;
    }

    const existingScript = document.querySelector('script[src*="maps.googleapis.com"]');
    if (existingScript) {
      const checkGoogle = setInterval(() => {
        if (window.google && window.google.maps) {
          clearInterval(checkGoogle);
          setIsGoogleLoaded(true);
        }
      }, 100);
      return;
    }

    const script = document.createElement('script');
    script.src = `https://maps.googleapis.com/maps/api/js?key=${API_KEY}&language=vi`;
    script.async = true;
    script.defer = true;

    script.onload = () => {
      setIsGoogleLoaded(true);
    };

    script.onerror = () => {
      setMapEngine('leaflet');
    };

    document.head.appendChild(script);
  }, [API_KEY]);

  // 3. Render Google Maps
  useEffect(() => {
    if (mapEngine !== 'google' || !isGoogleLoaded || !containerRef.current || !window.google?.maps || !isValidCoords) {
      return;
    }

    try {
      const map = new window.google.maps.Map(containerRef.current, {
        center: { lat: latitude, lng: longitude },
        zoom: zoom,
        mapTypeControl: false,
        streetViewControl: false,
        fullscreenControl: true,
        zoomControl: true,
      });

      const marker = new window.google.maps.Marker({
        position: { lat: latitude, lng: longitude },
        map: map,
        title: title,
      });

      if (title || address) {
        const infoWindow = new window.google.maps.InfoWindow({
          content: `
            <div style="padding: 8px; min-width: 180px;">
              ${title ? `<h3 style="margin: 0 0 6px; font-size: 14px; font-weight: 600; color: #1e293b;">${title}</h3>` : ''}
              ${address ? `<p style="margin: 0; font-size: 13px; color: #64748b; line-height: 1.4;">${address}</p>` : ''}
            </div>
          `,
        });

        marker.addListener('click', () => {
          infoWindow.open(map, marker);
        });
      }
    } catch {
      setMapEngine('leaflet');
    }
  }, [mapEngine, isGoogleLoaded, latitude, longitude, title, address, zoom, isValidCoords]);

  // 4. Render Leaflet Map (Fallback)
  useEffect(() => {
    if (mapEngine !== 'leaflet' || !containerRef.current || !isValidCoords) {
      return;
    }

    if (lMapInstanceRef.current) {
      lMapInstanceRef.current.remove();
      lMapInstanceRef.current = null;
    }

    const map = L.map(containerRef.current, {
      center: [latitude, longitude],
      zoom: zoom,
      scrollWheelZoom: true,
    });

    L.tileLayer('https://{s}.tile.openstreetmap.org/{z}/{x}/{y}.png', {
      attribution: '&copy; <a href="https://www.openstreetmap.org/copyright">OpenStreetMap</a>',
      maxZoom: 19,
    }).addTo(map);

    const marker = L.marker([latitude, longitude]).addTo(map);
    if (title || address) {
      marker.bindPopup(`
        <div style="font-size: 13px;">
          <strong>${title}</strong><br/>
          <span style="color: #666;">${address || ''}</span>
        </div>
      `).openPopup();
    }

    lMapInstanceRef.current = map;

    return () => {
      map.remove();
      lMapInstanceRef.current = null;
    };
  }, [mapEngine, latitude, longitude, title, address, zoom, isValidCoords]);

  const googleMapsUrl = `https://www.google.com/maps/search/?api=1&query=${latitude},${longitude}`;

  if (!isValidCoords) {
    return (
      <div className="google-map-view-placeholder">
        <FiMapPin size={48} />
        <p className="placeholder-title">Vị trí bản đồ chưa được cập nhật</p>
        <p className="placeholder-subtitle">Chủ trọ chưa chọn vị trí trên bản đồ</p>
      </div>
    );
  }

  return (
    <div className="google-map-view">
      <div
        ref={containerRef}
        className="google-map-view-container"
        style={{ height, minHeight: '300px' }}
      />

      <div className="flex items-center justify-between mt-3">
        <a
          href={googleMapsUrl}
          target="_blank"
          rel="noopener noreferrer"
          className="google-map-view-link"
        >
          <FiExternalLink />
          <span>🗺️ Xem đường đi trên Google Maps</span>
        </a>

        <div className="flex items-center gap-1.5 text-xs text-slate-500">
          <span>Bản đồ:</span>
          <button
            type="button"
            onClick={() => setMapEngine(mapEngine === 'google' ? 'leaflet' : 'google')}
            className="underline hover:text-blue-600 font-medium"
          >
            {mapEngine === 'google' ? 'Đổi sang OpenStreetMap' : 'Đổi sang Google Maps'}
          </button>
        </div>
      </div>
    </div>
  );
};

export default GoogleMapView;
