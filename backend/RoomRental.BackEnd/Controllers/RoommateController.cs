using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using RoomRental.BackEnd.BLL.Interfaces;
using RoomRental.BackEnd.DTO.Common;
using RoomRental.BackEnd.DTO.Roommate;

namespace RoomRental.BackEnd.Controllers;

[ApiController]
[Route("api/o-ghep")]
public class RoommateController : ControllerBase
{
    private readonly IRoommateService _service;
    public RoommateController(IRoommateService service) => _service = service;
    private int AccountId => int.Parse(User.FindFirst("UserId")?.Value ?? User.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? "0");
    [HttpGet]
    public async Task<IActionResult> Search(string? keyword, int pageNumber = 1, int pageSize = 12) => Ok(ApiResponse<RoommateSearchResult>.SuccessResponse(await _service.SearchAsync(keyword, pageNumber, pageSize)));
    [HttpGet("{id:int}")]
    public async Task<IActionResult> Detail(int id) => Ok(ApiResponse<RoommatePostDto>.SuccessResponse(await _service.PublicDetailAsync(id)));
    [HttpGet("cua-toi"), Authorize(Roles = "Tenant")]
    public async Task<IActionResult> Mine() => Ok(ApiResponse<List<RoommatePostDto>>.SuccessResponse(await _service.MyPostsAsync(AccountId, false)));
    [HttpGet("chu-tro"), Authorize(Roles = "Landlord")]
    public async Task<IActionResult> Landlord() => Ok(ApiResponse<List<RoommatePostDto>>.SuccessResponse(await _service.MyPostsAsync(AccountId, true)));
    [HttpGet("hop-dong"), Authorize(Roles = "Tenant")]
    public async Task<IActionResult> Contracts() => Ok(ApiResponse<List<RoommateContractDto>>.SuccessResponse(await _service.MyContractsAsync(AccountId)));
    [HttpGet("hop-dong/{id:int}"), Authorize(Roles = "Tenant,Landlord")]
    public async Task<IActionResult> Contract(int id) => Ok(ApiResponse<RoommateContractDto>.SuccessResponse(await _service.ContractAsync(AccountId, id)));
    [HttpPost, Authorize(Roles = "Tenant")]
    public async Task<IActionResult> Create(CreateRoommatePostDto dto) => Ok(ApiResponse<RoommatePostDto>.SuccessResponse(await _service.CreateAsync(AccountId, dto)));
    [HttpPut("{id:int}"), Authorize(Roles = "Tenant")]
    public async Task<IActionResult> Update(int id, CreateRoommatePostDto dto) => Ok(ApiResponse<RoommatePostDto>.SuccessResponse(await _service.UpdateAsync(AccountId, id, dto)));
    [HttpPut("{id:int}/duyet"), Authorize(Roles = "Landlord")]
    public async Task<IActionResult> Moderate(int id, RoommateDecisionDto dto) => Ok(ApiResponse<RoommatePostDto>.SuccessResponse(await _service.ModerateAsync(AccountId, id, dto)));
    [HttpPut("{id:int}/dong"), Authorize(Roles = "Tenant")]
    public async Task<IActionResult> Close(int id) => Ok(ApiResponse<RoommatePostDto>.SuccessResponse(await _service.CloseAsync(AccountId, id)));
    [HttpPost("{id:int}/yeu-cau"), Authorize(Roles = "Tenant")]
    public async Task<IActionResult> Apply(int id, CreateRoommateRequestDto dto) => Ok(ApiResponse<RoommateRequestDto>.SuccessResponse(await _service.ApplyAsync(AccountId, id, dto)));
    [HttpGet("{id:int}/yeu-cau"), Authorize(Roles = "Tenant,Landlord")]
    public async Task<IActionResult> Requests(int id) => Ok(ApiResponse<List<RoommateRequestDto>>.SuccessResponse(await _service.RequestsAsync(AccountId, id)));
    [HttpGet("ung-tuyen"), Authorize(Roles = "Tenant")]
    public async Task<IActionResult> Applications() => Ok(ApiResponse<List<RoommateRequestDto>>.SuccessResponse(await _service.MyApplicationsAsync(AccountId)));
    [HttpPut("yeu-cau/{id:int}/tiep-nhan"), Authorize(Roles = "Tenant")]
    public async Task<IActionResult> PrimaryDecision(int id, RoommateDecisionDto dto) => Ok(ApiResponse<RoommateRequestDto>.SuccessResponse(await _service.DecideAsync(AccountId, id, false, dto)));
    [HttpPut("yeu-cau/{id:int}/xac-nhan"), Authorize(Roles = "Landlord")]
    public async Task<IActionResult> LandlordDecision(int id, RoommateDecisionDto dto) => Ok(ApiResponse<RoommateRequestDto>.SuccessResponse(await _service.DecideAsync(AccountId, id, true, dto)));
    [HttpPut("yeu-cau/{id:int}/huy"), Authorize(Roles = "Tenant")]
    public async Task<IActionResult> Cancel(int id) => Ok(ApiResponse<RoommateRequestDto>.SuccessResponse(await _service.CancelAsync(AccountId, id)));
    [HttpPut("nguoi-o/{id:int}/roi-phong"), Authorize(Roles = "Tenant,Landlord")]
    public async Task<IActionResult> Leave(int id) => Ok(ApiResponse<RoommateContractDto>.SuccessResponse(await _service.LeaveAsync(AccountId, id)));
}
