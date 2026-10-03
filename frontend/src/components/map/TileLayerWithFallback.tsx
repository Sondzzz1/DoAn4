import React, { useState } from 'react';
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

  const currentProvider = TILE_PROVIDERS[providerIndex];

  const handleTileError = () => {
    console.error(`Tile provider ${currentProvider.name} failed`);
    
    // Try next provider
    if (providerIndex < TILE_PROVIDERS.length - 1) {
      console.log(`Switching to ${TILE_PROVIDERS[providerIndex + 1].name}...`);
      setProviderIndex(prev => prev + 1);
    } else {
      console.error('All tile providers failed');
      setError(true);
    }
  };

  if (error) {
    console.error('Cannot load any tile provider');
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
