import React, { useState, useRef, useEffect } from 'react';
import { MapContainer, Marker, Popup, useMap, useMapEvents } from 'react-leaflet';
import { FiMapPin, FiNavigation, FiSearch } from 'react-icons/fi';
import L from 'leaflet';
import 'leaflet/dist/leaflet.css';
import './LeafletMapPicker.css';
import TileLayerWithFallback from './TileLayerWithFallback';
import { buildLocationQuery, locationService, type LocationResult } from '../../services/locationService';
import { getApiErrorMessage } from '../../utils/apiError';
import markerIcon from 'leaflet/dist/images/marker-icon.png';
import markerIconRetina from 'leaflet/dist/images/marker-icon-2x.png';
import markerShadow from 'leaflet/dist/images/marker-shadow.png';

const pinIcon = L.icon({ iconUrl: markerIcon, iconRetinaUrl: markerIconRetina, shadowUrl: markerShadow,
  iconSize: [25, 41], iconAnchor: [12, 41], popupAnchor: [1, -34], shadowSize: [41, 41] });
const defaultPosition: [number, number] = [20.9432, 106.1032];
const validCoordinates = (lat?: number, lng?: number) =>
  lat !== undefined && lng !== undefined && Number.isFinite(lat) && Number.isFinite(lng) && Math.abs(lat) <= 90 && Math.abs(lng) <= 180;

interface LeafletMapPickerProps {
  latitude?: number;
  longitude?: number;
  onLocationChange: (lat: number, lng: number, location?: LocationResult) => void;
  onBusyChange?: (busy: boolean) => void;
  searchRequest?: { query: string; revision: number };
  address?: string;
  ward?: string;
  district?: string;
  province?: string;
}

type Lookup = { id: number; key: string } & (
  { kind: 'search'; query: string } | { kind: 'reverse'; latitude: number; longitude: number }
);
type Feedback = { id: number; pending: boolean; error: boolean; message: string; results: LocationResult[] };

function MapPosition({ latitude, longitude }: { latitude?: number; longitude?: number }) {
  const map = useMap();
  useEffect(() => {
    if (validCoordinates(latitude, longitude)) {
      map.stop();
      map.setView([latitude!, longitude!], 16, { animate: false });
    }
  }, [latitude, longitude, map]);
  return null;
}

function MapClickHandler({ onSelect }: { onSelect: (lat: number, lng: number) => void }) {
  useMapEvents({ click: event => onSelect(event.latlng.lat, event.latlng.lng) });
  return null;
}

const LeafletMapPicker: React.FC<LeafletMapPickerProps> = ({
  latitude, longitude, onLocationChange, onBusyChange, searchRequest,
  address = '', ward = '', district = '', province = '',
}) => {
  const key = buildLocationQuery(address, ward, district, province);
  const [lookup, setLookup] = useState<Lookup | null>(null);
  const [feedback, setFeedback] = useState<Feedback | null>(null);
  const [geoError, setGeoError] = useState('');
  const [locating, setLocating] = useState(false);
  const sequence = useRef(0);
  const mounted = useRef(false);
  const reportedSearch = useRef(searchRequest);
  const callback = useRef(onLocationChange);
  const latestKey = useRef(key);
  const hasPin = validCoordinates(latitude, longitude);
  const position: [number, number] = hasPin ? [latitude!, longitude!] : defaultPosition;
  const currentFeedback = lookup?.key === key && feedback?.id === lookup?.id ? feedback : null;
  const busy = !!currentFeedback?.pending || locating;

  useEffect(() => { callback.current = onLocationChange; }, [onLocationChange]);
  useEffect(() => { latestKey.current = key; }, [key]);
  useEffect(() => {
    mounted.current = true;
    return () => { mounted.current = false; };
  }, []);
  useEffect(() => { onBusyChange?.(busy); }, [busy, onBusyChange]);

  // Only completed address edits trigger searches, never individual keystrokes.
  useEffect(() => {
    if (!searchRequest || reportedSearch.current === searchRequest) return;
    reportedSearch.current = searchRequest;
    if (searchRequest.query !== key) return;
    const id = ++sequence.current;
    void Promise.resolve().then(() => {
      if (!mounted.current || sequence.current !== id) return;
      setLookup({ id, key, kind: 'search', query: key });
      setFeedback({ id, pending: true, error: false, message: 'Đang tìm vị trí...', results: [] });
    });
  }, [key, searchRequest]);

  useEffect(() => {
    if (!lookup || lookup.key !== key) return;
    const controller = new AbortController();
    let active = true;
    const run = async () => {
      try {
        if (lookup.kind === 'search') {
          const results = (await locationService.search(lookup.query, controller.signal))
            .filter(result => validCoordinates(result.latitude, result.longitude));
          if (!active || sequence.current !== lookup.id) return;
          if (!results.length) {
            setFeedback({ id: lookup.id, pending: false, error: true, message: 'Không tìm thấy vị trí. Vui lòng kiểm tra địa chỉ hoặc chọn trên bản đồ.', results: [] });
            return;
          }
          callback.current(results[0].latitude, results[0].longitude);
          setFeedback({ id: lookup.id, pending: false, error: false, message: results[0].displayName, results });
        } else {
          const location = await locationService.reverse(lookup.latitude, lookup.longitude, controller.signal);
          if (!active || sequence.current !== lookup.id) return;
          setFeedback({ id: lookup.id, pending: false, error: false, message: location.displayName, results: [] });
          callback.current(lookup.latitude, lookup.longitude, location);
        }
      } catch (error) {
        if (!active || controller.signal.aborted || sequence.current !== lookup.id) return;
        setFeedback({ id: lookup.id, pending: false, error: true,
          message: (lookup.kind === 'reverse' ? 'Đã chọn tọa độ, nhưng chưa cập nhật được địa chỉ. ' : '')
            + getApiErrorMessage(error, 'Không thể tra cứu lúc này. Vui lòng kiểm tra địa chỉ hoặc thử lại.'), results: [] });
      }
    };
    void run();
    return () => { active = false; controller.abort(); };
  }, [key, lookup]);

  const search = () => {
    setGeoError('');
    if (!address.trim()) {
      setGeoError('Vui lòng nhập địa chỉ cụ thể trước khi tìm vị trí.');
      return;
    }
    const id = ++sequence.current;
    setLookup({ id, key, kind: 'search', query: key });
    setFeedback({ id, pending: true, error: false, message: 'Đang tìm vị trí...', results: [] });
  };

  const selectLocation = (lat: number, lng: number) => {
    if (!validCoordinates(lat, lng)) return;
    setGeoError('');
    const id = ++sequence.current;
    callback.current(lat, lng);
    setLookup({ id, key, kind: 'reverse', latitude: lat, longitude: lng });
    setFeedback({ id, pending: true, error: false, message: 'Đang tra địa chỉ...', results: [] });
  };

  const getCurrentLocation = () => {
    setGeoError('');
    if (!navigator.geolocation) {
      setGeoError('Trình duyệt không hỗ trợ định vị.');
      return;
    }
    const id = ++sequence.current;
    setLookup(null);
    setFeedback(null);
    setLocating(true);
    navigator.geolocation.getCurrentPosition(
      result => {
        if (!mounted.current) return;
        setLocating(false);
        if (sequence.current === id && latestKey.current === key) selectLocation(result.coords.latitude, result.coords.longitude);
      },
      () => {
        if (!mounted.current) return;
        setLocating(false);
        if (sequence.current === id) setGeoError('Không thể lấy vị trí hiện tại. Vui lòng kiểm tra quyền truy cập vị trí.');
      },
      { timeout: 10000, maximumAge: 60000 }
    );
  };

  return (
    <div className="leaflet-map-picker">
      <div className="map-picker-toolbar">
        <button type="button" onClick={search} disabled={busy} className="search-button">
          <FiSearch aria-hidden="true" /> Tìm địa chỉ trên bản đồ
        </button>
        <button type="button" onClick={getCurrentLocation} disabled={locating} className="current-location-button">
          <FiNavigation aria-hidden="true" /> {locating ? 'Đang định vị...' : 'Vị trí hiện tại'}
        </button>
      </div>
      <div style={{ height: '400px', width: '100%', position: 'relative' }} aria-label="Bản đồ chọn vị trí phòng trọ" aria-busy={busy}>
        <MapContainer center={position} zoom={hasPin ? 16 : 13} zoomAnimation={false} style={{ height: '100%', width: '100%', zIndex: 1 }} scrollWheelZoom>
          <TileLayerWithFallback maxZoom={19} />
          <MapPosition latitude={latitude} longitude={longitude} />
          <MapClickHandler onSelect={selectLocation} />
          {hasPin && <Marker position={position} icon={pinIcon} draggable eventHandlers={{
            dragend: event => { const point = event.target.getLatLng(); selectLocation(point.lat, point.lng); },
          }}>
            <Popup><strong>{address || 'Vị trí đã chọn'}</strong><br />{latitude!.toFixed(6)}, {longitude!.toFixed(6)}</Popup>
          </Marker>}
        </MapContainer>
      </div>
      <div className="map-picker-footer">
        <div className="selected-location">
          <FiMapPin aria-hidden="true" />
          <span>{hasPin ? latitude!.toFixed(6) + ', ' + longitude!.toFixed(6) : 'Chưa chọn vị trí'}</span>
        </div>
        {geoError && <p className="map-picker-message error" role="alert">{geoError}</p>}
        {currentFeedback && <p className={'map-picker-message' + (currentFeedback.error ? ' error' : '')}
          role={currentFeedback.error ? 'alert' : 'status'}>{currentFeedback.message}</p>}
        {!currentFeedback && address && hasPin && <p className="map-picker-message" role="status">{[address, ward, district, province].filter(Boolean).join(', ')}</p>}
        {currentFeedback && currentFeedback.results.length > 1 && <ul className="map-picker-results">
          {currentFeedback.results.map((result, index) => <li key={result.latitude + ':' + result.longitude + ':' + index}>
            <button type="button" onClick={() => {
              callback.current(result.latitude, result.longitude);
              setFeedback({ ...currentFeedback, message: result.displayName });
            }}><FiMapPin aria-hidden="true" />{result.displayName}</button>
          </li>)}
        </ul>}
      </div>
    </div>
  );
};

export default LeafletMapPicker;
