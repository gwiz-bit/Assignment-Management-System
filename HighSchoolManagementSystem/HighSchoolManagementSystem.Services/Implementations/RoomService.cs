using HighSchoolManagementSystem.Models.Entities;
using HighSchoolManagementSystem.Repositories.Interfaces;
using HighSchoolManagementSystem.Services.Common;
using HighSchoolManagementSystem.Services.Interfaces;

namespace HighSchoolManagementSystem.Services.Implementations;

public class RoomService : IRoomService
{
    private readonly IRoomRepository _roomRepository;

    public RoomService(IRoomRepository roomRepository)
    {
        _roomRepository = roomRepository;
    }

    public async Task<ServiceResult<IReadOnlyList<Room>>> GetByFiltersAsync(string? status, string? roomType)
    {
        try
        {
            var rooms = await _roomRepository.GetByFiltersAsync(status, roomType);
            return ServiceResult<IReadOnlyList<Room>>.Ok(rooms);
        }
        catch
        {
            return ServiceResult<IReadOnlyList<Room>>.Fail(
                ErrorCodes.DatabaseError,
                "Không thể tải danh sách phòng học. Vui lòng thử lại sau.");
        }
    }

    public async Task<ServiceResult<IReadOnlyList<string>>> GetRoomTypesAsync()
    {
        try
        {
            var types = await _roomRepository.GetDistinctRoomTypesAsync();
            return ServiceResult<IReadOnlyList<string>>.Ok(types);
        }
        catch
        {
            return ServiceResult<IReadOnlyList<string>>.Fail(
                ErrorCodes.DatabaseError,
                "Không thể tải danh sách loại phòng học.");
        }
    }
}
