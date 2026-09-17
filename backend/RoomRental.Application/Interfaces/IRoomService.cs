using RoomRental.Application.DTOs.Room;
using RoomRental.Domain.Enums;

namespace RoomRental.Application.Interfaces;

public interface IRoomService
{
    Task<List<RoomDto>> GetLandlordRoomsAsync(int landlordAccountId, RoomStatus? status = null);
    Task<RoomDto> GetRoomByIdAsync(int roomId);
    Task<RoomDto> CreateRoomAsync(int landlordAccountId, CreateRoomDto createDto);
    Task<RoomDto> UpdateRoomAsync(int landlordAccountId, int roomId, UpdateRoomDto updateDto);
    Task<RoomDto> UpdateRoomStatusAsync(int landlordAccountId, int roomId, RoomStatus status);
    Task DeleteRoomAsync(int landlordAccountId, int roomId);
}
