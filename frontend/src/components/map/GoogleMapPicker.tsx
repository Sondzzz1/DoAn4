import React, { useState, useEffect, useRef, useCallback } from 'react';
import { FiMapPin, FiSearch, FiNavigation, FiAlertCircle } from 'react-icons/fi';
import L from 'leaflet';
import 'leaflet/dist/leaflet.css';
import './GoogleMapPicker.css';

// Fix Leaflet icons
type LeafletDefaultIconPrototype = { _getIconUrl?: unknown };
const defaultIconPrototype = L.Icon.Default.prototype as unknown as LeafletDefaultIconPrototype;
delete defaultIconPrototype._getIconUrl;
L.Icon.Default.mergeOptions({
  iconRetinaUrl: 'https://unpkg.com/leaflet@1.9.4/dist/images/marker-icon-2x.png',
  iconUrl: 'https://unpkg.com/leaflet@1.9.4/dist/images/marker-icon.png',
  shadowUrl: 'https://unpkg.com/leaflet@1.9.4/dist/images/marker-shadow.png',
});

interface GoogleMapPickerProps {
  latitude?: number;
  longitude?: number;
  onLocationChange: (lat: number, lng: number, address?: string) => void;
  height?: string;
  defaultAddress?: string;
}

type GoogleLatLng = { lat: () => number; lng: () => number };
type GoogleMapClickEvent = { latLng: GoogleLatLng };
type GoogleMapInstance = {
  setCenter: (position: { lat: number; lng: number }) => void;
  setZoom: (zoom: number) => void;
  addListener: (event: string, handler: (event: GoogleMapClickEvent) => void) => void;
};
type GoogleMarker = {
  getPosition: () => GoogleLatLng | null;
  setPosition: (position: { lat: number; lng: number }) => void;
  addListener: (event: string, handler: () => void) => void;
};
type GoogleGeocodeResult = {
  formatted_address: string;
  geometry: { location: GoogleLatLng };
};
type GoogleGeocoder = {
  geocode: (
    request: { location?: { lat: number; lng: number }; address?: string; componentRestrictions?: { country: string } },
    callback: (results: GoogleGeocodeResult[], status: string) => void,
  ) => void;
};
type GoogleAutocomplete = {
  addListener: (event: string, handler: () => void) => void;
  getPlace: () => { geometry?: { location?: GoogleLatLng }; formatted_address?: string; name?: string };
};
type GoogleMapsNamespace = {
  maps: {
    Geocoder: new () => GoogleGeocoder;
    Map: new (container: HTMLElement, options: object) => GoogleMapInstance;
    Marker: new (options: object) => GoogleMarker;
    InfoWindow: new (options: object) => { open: (map: GoogleMapInstance, marker: GoogleMarker) => void };
    Animation: { DROP: unknown };
    places: { Autocomplete: new (input: HTMLInputElement, options: object) => GoogleAutocomplete };
  };
};
type NominatimReverseResult = { display_name?: string };
type NominatimSearchResult = { lat: string; lon: string; display_name: string };

declare global {
  interface Window {
    google?: GoogleMapsNamespace;
    gm_authFailure?: () => void;
  }
}

const DEFAULT_LAT = 21.0285;
const DEFAULT_LNG = 105.8542;

const GoogleMapPicker: React.FC<GoogleMapPickerProps> = ({
  latitude,
  longitude,
  onLocationChange,
  height = '420px',
  defaultAddress = '',
}) => {
  const containerRef = useRef<HTMLDivElement>(null);
  const searchInputRef = useRef<HTMLInputElement>(null);
  const onLocationChangeRef = useRef(onLocationChange);

  // Google Maps refs
  const gMapInstanceRef = useRef<GoogleMapInstance | null>(null);
  const gMarkerRef = useRef<GoogleMarker | null>(null);
  const gGeocoderRef = useRef<GoogleGeocoder | null>(null);
  const gAutocompleteRef = useRef<GoogleAutocomplete | null>(null);

  // Leaflet refs
  const lMapInstanceRef = useRef<L.Map | null>(null);
  const lMarkerRef = useRef<L.Marker | null>(null);

  const API_KEY = import.meta.env.VITE_GOOGLE_MAPS_API_KEY;
  const hasConfiguredGoogleMapsKey = Boolean(API_KEY && API_KEY !== 'YOUR_GOOGLE_MAPS_API_KEY_HERE');
  const [mapEngine, setMapEngine] = useState<'google' | 'leaflet'>(() => (
    hasConfiguredGoogleMapsKey ? 'google' : 'leaflet'
  ));
  const [isGoogleLoaded, setIsGoogleLoaded] = useState(false);
  const [googleAuthError, setGoogleAuthError] = useState<string | null>(null);
  const [searchValue, setSearchValue] = useState(defaultAddress);
  const [selectedAddress, setSelectedAddress] = useState(defaultAddress);
  const [isSearching, setIsSearching] = useState(false);

  const reportLocation = useCallback((lat: number, lng: number, address?: string) => {
    onLocationChangeRef.current(lat, lng, address);
  }, []);

  const handleGoogleReverseGeocode = useCallback((lat: number, lng: number) => {
    if (gGeocoderRef.current) {
      gGeocoderRef.current.geocode({ location: { lat, lng } }, (results, status) => {
        if (status === 'OK' && results[0]) {
          const addr = results[0].formatted_address;
          setSelectedAddress(addr);
          setSearchValue(addr);
          reportLocation(lat, lng, addr);
        } else {
          reportLocation(lat, lng);
        }
      });
    } else {
      reportLocation(lat, lng);
    }
  }, [reportLocation]);

  const handleLeafletReverseGeocode = useCallback(async (lat: number, lng: number) => {
    try {
      const res = await fetch(`https://nominatim.openstreetmap.org/reverse?format=json&lat=${lat}&lon=${lng}&accept-language=vi`);
      const data = await res.json() as NominatimReverseResult;
      if (data.display_name) {
        setSelectedAddress(data.display_name);
        setSearchValue(data.display_name);
        reportLocation(lat, lng, data.display_name);
      } else {
        reportLocation(lat, lng);
      }
    } catch {
      reportLocation(lat, lng);
    }
  }, [reportLocation]);

  useEffect(() => {
    onLocationChangeRef.current = onLocationChange;
  }, [onLocationChange]);

  // 1. Listen for Google Maps Auth Failure
  useEffect(() => {
    window.gm_authFailure = () => {
      console.warn('Google Maps Authentication failed (Key chưa kích hoạt hoặc thiếu Billing). Đang chuyển sang OpenStreetMap.');
      setGoogleAuthError('Google Maps API Key chưa được kích hoạt dịch vụ Maps/Places hoặc chưa liên kết Billing trên Google Cloud.');
      setMapEngine('leaflet');
    };

    return () => {
      delete window.gm_authFailure;
    };
  }, []);

  // 2. Load Google Maps Script
  useEffect(() => {
    if (!hasConfiguredGoogleMapsKey) return;

    if (window.google && window.google.maps) {
      void Promise.resolve().then(() => setIsGoogleLoaded(true));
      return;
    }

    // Check if script is already in document
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
    script.src = `https://maps.googleapis.com/maps/api/js?key=${API_KEY}&libraries=places&language=vi`;
    script.async = true;
    script.defer = true;

    script.onload = () => {
      setIsGoogleLoaded(true);
    };

    script.onerror = () => {
      console.warn('Không thể tải Google Maps script. Chuyển sang Leaflet/OpenStreetMap.');
      setMapEngine('leaflet');
    };

    document.head.appendChild(script);
  }, [API_KEY, hasConfiguredGoogleMapsKey]);

  // 3. Initialize Google Map (ONLY ONCE)
  useEffect(() => {
    if (mapEngine !== 'google' || !isGoogleLoaded || !containerRef.current || !window.google?.maps) {
      return;
    }

    if (gMapInstanceRef.current) return; // Already initialized

    const initialLat = latitude && !isNaN(latitude) ? latitude : DEFAULT_LAT;
    const initialLng = longitude && !isNaN(longitude) ? longitude : DEFAULT_LNG;

    try {
      gGeocoderRef.current = new window.google.maps.Geocoder();

      const map = new window.google.maps.Map(containerRef.current, {
        center: { lat: initialLat, lng: initialLng },
        zoom: latitude && longitude ? 16 : 13,
        mapTypeControl: true,
        streetViewControl: false,
        fullscreenControl: true,
      });

      gMapInstanceRef.current = map;

      const marker = new window.google.maps.Marker({
        position: { lat: initialLat, lng: initialLng },
        map: map,
        draggable: true,
        title: 'Kéo marker để chọn vị trí chính xác',
        animation: window.google.maps.Animation.DROP,
      });

      gMarkerRef.current = marker;

      // Handle marker dragend
      marker.addListener('dragend', () => {
        const pos = marker.getPosition();
        if (pos) {
          const lat = pos.lat();
          const lng = pos.lng();
          handleGoogleReverseGeocode(lat, lng);
        }
      });

      // Handle map click
      map.addListener('click', (e) => {
        const lat = e.latLng.lat();
        const lng = e.latLng.lng();
        marker.setPosition({ lat, lng });
        handleGoogleReverseGeocode(lat, lng);
      });

      // Handle Places Autocomplete
      if (searchInputRef.current) {
        try {
          const autocomplete = new window.google.maps.places.Autocomplete(
            searchInputRef.current,
            {
              componentRestrictions: { country: 'vn' },
              fields: ['geometry', 'formatted_address', 'name'],
            }
          );

          autocomplete.addListener('place_changed', () => {
            const place = autocomplete.getPlace();
            if (!place.geometry?.location) return;

            const lat = place.geometry.location.lat();
            const lng = place.geometry.location.lng();
            const addr = place.formatted_address || place.name || '';

            marker.setPosition({ lat, lng });
            map.setCenter({ lat, lng });
            map.setZoom(16);

            setSelectedAddress(addr);
            setSearchValue(addr);
            reportLocation(lat, lng, addr);
          });

          gAutocompleteRef.current = autocomplete;
        } catch {
          // Places API not active, search will use Nominatim
        }
      }
    } catch (e) {
      console.warn('Lỗi khởi tạo Google Maps, chuyển sang Leaflet:', e);
      void Promise.resolve().then(() => setMapEngine('leaflet'));
    }
  }, [handleGoogleReverseGeocode, isGoogleLoaded, latitude, longitude, mapEngine, reportLocation]);

  // 4. Initialize Leaflet Map (Fallback Engine)
  useEffect(() => {
    if (mapEngine !== 'leaflet' || !containerRef.current) return;

    if (lMapInstanceRef.current) {
      lMapInstanceRef.current.remove();
      lMapInstanceRef.current = null;
    }

    const initialLat = latitude && !isNaN(latitude) ? latitude : DEFAULT_LAT;
    const initialLng = longitude && !isNaN(longitude) ? longitude : DEFAULT_LNG;

    const map = L.map(containerRef.current, {
      center: [initialLat, initialLng],
      zoom: latitude && longitude ? 16 : 13,
      scrollWheelZoom: true,
    });

    L.tileLayer('https://{s}.tile.openstreetmap.org/{z}/{x}/{y}.png', {
      attribution: '&copy; <a href="https://www.openstreetmap.org/copyright">OpenStreetMap</a>',
      maxZoom: 19,
    }).addTo(map);

    const marker = L.marker([initialLat, initialLng], {
      draggable: true,
    }).addTo(map);

    marker.bindPopup('📍 Kéo thả marker để chọn vị trí').openPopup();

    marker.on('dragend', () => {
      const { lat, lng } = marker.getLatLng();
      handleLeafletReverseGeocode(lat, lng);
    });

    map.on('click', (e: L.LeafletMouseEvent) => {
      const { lat, lng } = e.latlng;
      marker.setLatLng([lat, lng]);
      handleLeafletReverseGeocode(lat, lng);
    });

    lMapInstanceRef.current = map;
    lMarkerRef.current = marker;

    return () => {
      map.remove();
      lMapInstanceRef.current = null;
      lMarkerRef.current = null;
    };
  }, [handleLeafletReverseGeocode, latitude, longitude, mapEngine]);

  // Search Address handler (supports Nominatim for both engines)
  const handleManualSearch = async () => {
    if (!searchValue.trim()) return;
    setIsSearching(true);

    try {
      // If Google Autocomplete active and Google geocoder available
      if (mapEngine === 'google' && gGeocoderRef.current) {
        gGeocoderRef.current.geocode({ address: searchValue, componentRestrictions: { country: 'VN' } }, (results, status) => {
          setIsSearching(false);
          if (status === 'OK' && results[0]) {
            const loc = results[0].geometry.location;
            const lat = loc.lat();
            const lng = loc.lng();
            const addr = results[0].formatted_address;

            if (gMarkerRef.current && gMapInstanceRef.current) {
              gMarkerRef.current.setPosition({ lat, lng });
              gMapInstanceRef.current.setCenter({ lat, lng });
              gMapInstanceRef.current.setZoom(16);
            }
            setSelectedAddress(addr);
            reportLocation(lat, lng, addr);
            return;
          }
          // Fallback to Nominatim search
          void searchWithNominatim(searchValue);
        });
      } else {
        await searchWithNominatim(searchValue);
        setIsSearching(false);
      }
    } catch {
      await searchWithNominatim(searchValue);
      setIsSearching(false);
    }
  };

  const searchWithNominatim = async (query: string) => {
    try {
      const res = await fetch(`https://nominatim.openstreetmap.org/search?format=json&q=${encodeURIComponent(query)}&countrycodes=vn&limit=1&accept-language=vi`);
      const results = await res.json() as NominatimSearchResult[];
      if (results.length > 0) {
        const lat = parseFloat(results[0].lat);
        const lng = parseFloat(results[0].lon);
        const addr = results[0].display_name;

        if (mapEngine === 'google' && gMapInstanceRef.current && gMarkerRef.current) {
          gMarkerRef.current.setPosition({ lat, lng });
          gMapInstanceRef.current.setCenter({ lat, lng });
          gMapInstanceRef.current.setZoom(16);
        } else if (mapEngine === 'leaflet' && lMapInstanceRef.current && lMarkerRef.current) {
          lMarkerRef.current.setLatLng([lat, lng]);
          lMapInstanceRef.current.setView([lat, lng], 16);
        }

        setSelectedAddress(addr);
        reportLocation(lat, lng, addr);
      } else {
        alert('Không tìm thấy vị trí phù hợp với địa chỉ này. Vui lòng click trực tiếp trên bản đồ.');
      }
    } catch (e) {
      console.error('Lỗi tìm kiếm địa chỉ:', e);
    }
  };

  // GPS Current Location
  const handleGetCurrentLocation = useCallback(() => {
    if (!navigator.geolocation) {
      alert('Trình duyệt của bạn không hỗ trợ định vị GPS');
      return;
    }

    navigator.geolocation.getCurrentPosition(
      (pos) => {
        const lat = pos.coords.latitude;
        const lng = pos.coords.longitude;

        if (mapEngine === 'google' && gMapInstanceRef.current && gMarkerRef.current) {
          gMarkerRef.current.setPosition({ lat, lng });
          gMapInstanceRef.current.setCenter({ lat, lng });
          gMapInstanceRef.current.setZoom(16);
          handleGoogleReverseGeocode(lat, lng);
        } else if (lMapInstanceRef.current && lMarkerRef.current) {
          lMarkerRef.current.setLatLng([lat, lng]);
          lMapInstanceRef.current.setView([lat, lng], 16);
          void handleLeafletReverseGeocode(lat, lng);
        }
      },
      (err) => {
        alert(`Không thể lấy vị trí hiện tại: ${err.message}`);
      }
    );
  }, [handleGoogleReverseGeocode, handleLeafletReverseGeocode, mapEngine]);

  return (
    <div className="google-map-picker">
      {/* Map Engine Switcher & Status Bar */}
      <div className="flex flex-wrap items-center justify-between gap-2 mb-3 px-1">
        <div className="flex items-center gap-2">
          <span className="text-xs font-semibold text-slate-500 uppercase tracking-wider">Chế độ bản đồ:</span>
          <button
            type="button"
            onClick={() => {
              if (hasConfiguredGoogleMapsKey) {
                setMapEngine('google');
                setGoogleAuthError(null);
              } else {
                setGoogleAuthError('Chưa cấu hình Google Maps API Key.');
              }
            }}
            disabled={!hasConfiguredGoogleMapsKey}
            className={`px-3 py-1 text-xs font-semibold rounded-lg transition-all ${
              mapEngine === 'google'
                ? 'bg-blue-600 text-white shadow-sm'
                : 'bg-slate-100 text-slate-600 hover:bg-slate-200'
            }`}
          >
            Google Maps
          </button>
          <button
            type="button"
            onClick={() => setMapEngine('leaflet')}
            className={`px-3 py-1 text-xs font-semibold rounded-lg transition-all ${
              mapEngine === 'leaflet'
                ? 'bg-emerald-600 text-white shadow-sm'
                : 'bg-slate-100 text-slate-600 hover:bg-slate-200'
            }`}
          >
            OpenStreetMap (Miễn phí)
          </button>
        </div>

        {googleAuthError && (
          <div className="text-xs text-amber-700 bg-amber-50 border border-amber-200 px-3 py-1 rounded-lg flex items-center gap-1.5">
            <FiAlertCircle />
            <span>Đang sử dụng bản đồ vệ tinh OpenStreetMap</span>
          </div>
        )}
      </div>

      {/* Search Bar */}
      <div className="google-map-search">
        <div className="search-input-wrapper">
          <FiSearch className="search-icon" />
          <input
            ref={searchInputRef}
            type="text"
            value={searchValue}
            onChange={(e) => setSearchValue(e.target.value)}
            onKeyDown={(e) => {
              if (e.key === 'Enter') {
                e.preventDefault();
                void handleManualSearch();
              }
            }}
            placeholder="Nhập địa chỉ, số nhà, quận huyện để tìm vị trí..."
            className="search-input"
          />
        </div>
        <button
          type="button"
          onClick={() => void handleManualSearch()}
          disabled={isSearching}
          className="px-4 py-2 bg-slate-700 hover:bg-slate-800 text-white font-medium text-sm rounded-xl transition-all"
        >
          {isSearching ? 'Đang tìm...' : 'Tìm kiếm'}
        </button>
        <button
          type="button"
          onClick={handleGetCurrentLocation}
          className="location-button"
          title="Lấy vị trí hiện tại của thiết bị"
        >
          <FiNavigation />
          <span>Vị trí hiện tại</span>
        </button>
      </div>

      {/* Map Container */}
      <div
        ref={containerRef}
        className="google-map-container"
        style={{ height, minHeight: '350px' }}
      />

      {/* Selected Location Display */}
      {selectedAddress && (
        <div className="selected-location">
          <FiMapPin className="location-icon" />
          <div className="location-info">
            <span className="location-label">Vị trí đã chọn:</span>
            <span className="location-address">{selectedAddress}</span>
          </div>
        </div>
      )}

      {/* Instructions */}
      <div className="map-instructions">
        <p><strong>Hướng dẫn chọn vị trí:</strong></p>
        <ul>
          <li>🔍 Nhập địa chỉ vào ô tìm kiếm phía trên rồi nhấn <strong>Tìm kiếm</strong> hoặc Enter.</li>
          <li>📍 Click trực tiếp lên bất kỳ điểm nào trên bản đồ để đặt vị trí.</li>
          <li>🖱️ Kéo thả marker màu xanh/đỏ để điều chỉnh vị trí chính xác đến từng mét.</li>
          <li>📱 Bấm nút <strong>"Vị trí hiện tại"</strong> để tự động lấy tọa độ qua GPS.</li>
        </ul>
      </div>
    </div>
  );
};

export default GoogleMapPicker;
