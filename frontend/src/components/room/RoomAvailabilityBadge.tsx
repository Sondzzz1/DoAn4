import { RoomStatus } from '../../types/post.types';
import { getRoomAvailabilityLabel } from '../../utils/roomAvailability';
import './RoomAvailabilityBadge.css';

export default function RoomAvailabilityBadge({ status, className = '' }: { status: RoomStatus; className?: string }) {
  return <span className={`room-availability-badge ${className}`} data-status={status}>
    {getRoomAvailabilityLabel(status)}
  </span>;
}
