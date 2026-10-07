import React, { useRef, useState } from 'react';
import { TileLayer } from 'react-leaflet';

/**
 * TileLayer với fallback providers
 * Nếu OpenStreetMap không load, tự động thử providers khác
 */

const TILE_PROVIDERS = [
  {
    name: 'OpenStreetMap',
    url: 'https://{s}.tile.openstreetmap.org/{z}/{x}/{y}.png',
    attribution: '&copy; <a href="https://www.openstreetmap.org/copyright">OpenStreetMap</a> contributors',
    subdomains: ['a', 'b', 'c'],
  },
  {
    name: 'OpenStreetMap.DE',
    url: 'https://{s}.tile.openstreetmap.de/{z}/{x}/{y}.png',
    attribution: '&copy; <a href="https://www.openstreetmap.org/copyright">OpenStreetMap</a> contributors',
    subdomains: ['a', 'b', 'c'],
  },
  {
    name: 'CyclOSM',
    url: 'https://{s}.tile-cyclosm.openstreetmap.fr/cyclosm/{z}/{x}/{y}.png',
    attribution: '&copy; <a href="https://www.openstreetmap.org/copyright">OpenStreetMap</a> | <a href="https://www.cyclosm.org">CyclOSM</a>',
    subdomains: ['a', 'b', 'c'],
  },
];

interface TileLayerWithFallbackProps {
  maxZoom?: number;
}

const TileLayerWithFallback: React.FC<TileLayerWithFallbackProps> = ({ maxZoom = 19 }) => {
  const [providerIndex, setProviderIndex] = useState(0);
  const [error, setError] = useState(false);
  const activeProviderRef = useRef(0);

  const currentProvider = TILE_PROVIDERS[providerIndex];

  const handleTileError = () => {
    // A provider can emit several tile errors before React renders the next layer.
    if (activeProviderRef.current !== providerIndex) return;
    activeProviderRef.current = providerIndex + 1;
    if (providerIndex < TILE_PROVIDERS.length - 1) {
      setProviderIndex(providerIndex + 1);
    } else {
      setError(true);
    }
  };

  if (error) {
    return <div role="status" className="absolute inset-x-3 bottom-3 z-[600] rounded-md border border-slate-200 bg-white/95 px-3 py-2 text-center text-sm text-slate-600 shadow-sm">Bản đồ tạm thời không khả dụng.</div>;
  }

  return (
    <TileLayer
      key={currentProvider.name}
      attribution={currentProvider.attribution}
      url={currentProvider.url}
      subdomains={currentProvider.subdomains}
      maxZoom={maxZoom}
      crossOrigin={true}
      eventHandlers={{
        tileerror: handleTileError,
      }}
    />
  );
};

export default TileLayerWithFallback;
