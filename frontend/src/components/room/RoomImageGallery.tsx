import React, { useState } from 'react';
import { FiChevronLeft, FiChevronRight, FiHeart, FiShare2, FiX } from 'react-icons/fi';

interface RoomImageGalleryProps {
  images: string[];
  title: string;
  isFavorite: boolean;
  onToggleFavorite: () => void;
  onShare: () => void;
}

const RoomImageGallery: React.FC<RoomImageGalleryProps> = ({
  images,
  title,
  isFavorite,
  onToggleFavorite,
  onShare,
}) => {
  const [currentIndex, setCurrentIndex] = useState(0);
  const [isLightboxOpen, setIsLightboxOpen] = useState(false);

  const hasImages = images && images.length > 0;
  const displayImages = hasImages ? images : ['https://via.placeholder.com/1200x800?text=No+Image'];

  const handlePrevious = (e?: React.MouseEvent) => {
    e?.stopPropagation();
    setCurrentIndex((prev) => (prev === 0 ? displayImages.length - 1 : prev - 1));
  };

  const handleNext = (e?: React.MouseEvent) => {
    e?.stopPropagation();
    setCurrentIndex((prev) => (prev === displayImages.length - 1 ? 0 : prev + 1));
  };

  const openLightbox = () => {
    setIsLightboxOpen(true);
  };

  const closeLightbox = () => {
    setIsLightboxOpen(false);
  };

  return (
    <>
      {/* Main Gallery Container */}
      <div className="bg-white rounded-2xl overflow-hidden shadow-sm border border-gray-100">
        {/* Main Image */}
        <div className="relative aspect-[4/3] sm:aspect-video bg-gray-100 overflow-hidden group">
          <img
            src={displayImages[currentIndex]}
            alt={`${title} - Hình ${currentIndex + 1}`}
            className="w-full h-full object-cover cursor-pointer transition-transform duration-300 group-hover:scale-105"
            onClick={openLightbox}
            loading="lazy"
          />

          {/* Top Right Actions */}
          <div className="absolute top-4 right-4 flex items-center gap-2 z-10">
            <button
              onClick={(e) => {
                e.stopPropagation();
                onToggleFavorite();
              }}
              className="w-10 h-10 rounded-full bg-white/90 backdrop-blur-sm hover:bg-white transition-all flex items-center justify-center shadow-md group/btn"
              aria-label={isFavorite ? 'Bỏ lưu tin' : 'Lưu tin'}
            >
              <FiHeart
                className={`w-5 h-5 transition-all ${
                  isFavorite
                    ? 'fill-red-500 text-red-500'
                    : 'text-gray-600 group-hover/btn:text-red-500'
                }`}
              />
            </button>
            <button
              onClick={(e) => {
                e.stopPropagation();
                onShare();
              }}
              className="w-10 h-10 rounded-full bg-white/90 backdrop-blur-sm hover:bg-white transition-all flex items-center justify-center shadow-md group/btn"
              aria-label="Chia sẻ"
            >
              <FiShare2 className="w-5 h-5 text-gray-600 group-hover/btn:text-[#0084ff]" />
            </button>
          </div>

          {/* Navigation Arrows */}
          {displayImages.length > 1 && (
            <>
              <button
                onClick={handlePrevious}
                className="absolute left-4 top-1/2 -translate-y-1/2 w-10 h-10 rounded-full bg-white/90 backdrop-blur-sm hover:bg-white transition-all flex items-center justify-center shadow-md opacity-0 group-hover:opacity-100 z-10"
                aria-label="Ảnh trước"
              >
                <FiChevronLeft className="w-6 h-6 text-gray-800" />
              </button>
              <button
                onClick={handleNext}
                className="absolute right-4 top-1/2 -translate-y-1/2 w-10 h-10 rounded-full bg-white/90 backdrop-blur-sm hover:bg-white transition-all flex items-center justify-center shadow-md opacity-0 group-hover:opacity-100 z-10"
                aria-label="Ảnh tiếp"
              >
                <FiChevronRight className="w-6 h-6 text-gray-800" />
              </button>
            </>
          )}

          {/* Image Counter */}
          {displayImages.length > 1 && (
            <div className="absolute bottom-4 right-4 px-3 py-1.5 rounded-full bg-black/60 backdrop-blur-sm text-white text-sm font-medium z-10">
              {currentIndex + 1} / {displayImages.length}
            </div>
          )}
        </div>

        {/* Thumbnail Strip */}
        {displayImages.length > 1 && (
          <div className="p-4 border-t border-gray-100">
            <div className="flex gap-2 overflow-x-auto scrollbar-thin scrollbar-thumb-gray-300 scrollbar-track-gray-100">
              {displayImages.map((img, index) => (
                <button
                  key={index}
                  onClick={() => setCurrentIndex(index)}
                  className={`relative flex-shrink-0 w-20 h-16 rounded-lg overflow-hidden border-2 transition-all ${
                    index === currentIndex
                      ? 'border-[#0084ff] ring-2 ring-[#0084ff]/20'
                      : 'border-gray-200 hover:border-gray-300'
                  }`}
                >
                  <img
                    src={img}
                    alt={`Thumbnail ${index + 1}`}
                    className="w-full h-full object-cover"
                    loading="lazy"
                  />
                </button>
              ))}
            </div>
          </div>
        )}
      </div>

      {/* Lightbox Modal */}
      {isLightboxOpen && (
        <div
          className="fixed inset-0 z-[9999] bg-black/95 flex items-center justify-center p-4"
          onClick={closeLightbox}
        >
          {/* Close Button */}
          <button
            onClick={closeLightbox}
            className="absolute top-4 right-4 w-12 h-12 rounded-full bg-white/10 hover:bg-white/20 backdrop-blur-sm transition-all flex items-center justify-center z-10"
            aria-label="Đóng"
          >
            <FiX className="w-6 h-6 text-white" />
          </button>

          {/* Image Counter */}
          <div className="absolute top-4 left-4 px-4 py-2 rounded-full bg-white/10 backdrop-blur-sm text-white text-sm font-medium z-10">
            {currentIndex + 1} / {displayImages.length}
          </div>

          {/* Main Lightbox Image */}
          <img
            src={displayImages[currentIndex]}
            alt={`${title} - Hình ${currentIndex + 1}`}
            className="max-w-full max-h-full object-contain"
            onClick={(e) => e.stopPropagation()}
          />

          {/* Navigation in Lightbox */}
          {displayImages.length > 1 && (
            <>
              <button
                onClick={handlePrevious}
                className="absolute left-4 top-1/2 -translate-y-1/2 w-12 h-12 rounded-full bg-white/10 hover:bg-white/20 backdrop-blur-sm transition-all flex items-center justify-center z-10"
                aria-label="Ảnh trước"
              >
                <FiChevronLeft className="w-7 h-7 text-white" />
              </button>
              <button
                onClick={handleNext}
                className="absolute right-4 top-1/2 -translate-y-1/2 w-12 h-12 rounded-full bg-white/10 hover:bg-white/20 backdrop-blur-sm transition-all flex items-center justify-center z-10"
                aria-label="Ảnh tiếp"
              >
                <FiChevronRight className="w-7 h-7 text-white" />
              </button>
            </>
          )}
        </div>
      )}
    </>
  );
};

export default RoomImageGallery;
