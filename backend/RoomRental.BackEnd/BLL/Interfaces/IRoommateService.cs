using RoomRental.BackEnd.DTO.Roommate;

namespace RoomRental.BackEnd.BLL.Interfaces;

public interface IRoommateService
{
    Task<RoommateSearchResult> SearchAsync(string? keyword, int pageNumber, int pageSize);
    Task<RoommatePostDto> PublicDetailAsync(int id);
    Task<List<RoommatePostDto>> MyPostsAsync(int accountId, bool landlord);
    Task<RoommatePostDto> CreateAsync(int accountId, CreateRoommatePostDto dto);
    Task<RoommatePostDto> UpdateAsync(int accountId, int id, CreateRoommatePostDto dto);
    Task<RoommatePostDto> ModerateAsync(int accountId, int id, RoommateDecisionDto dto);
    Task<RoommatePostDto> CloseAsync(int accountId, int id);
    Task<RoommateRequestDto> ApplyAsync(int accountId, int id, CreateRoommateRequestDto dto);
    Task<List<RoommateRequestDto>> RequestsAsync(int accountId, int postId);
    Task<List<RoommateRequestDto>> MyApplicationsAsync(int accountId);
    Task<RoommateRequestDto> DecideAsync(int accountId, int requestId, bool landlord, RoommateDecisionDto dto);
    Task<RoommateRequestDto> CancelAsync(int accountId, int requestId);
    Task<List<RoommateContractDto>> MyContractsAsync(int accountId);
    Task<RoommateContractDto> ContractAsync(int accountId, int contractId);
    Task<RoommateContractDto> LeaveAsync(int accountId, int occupantId);
    Task ReconcileLifecycleAsync(CancellationToken cancellationToken = default);
}
