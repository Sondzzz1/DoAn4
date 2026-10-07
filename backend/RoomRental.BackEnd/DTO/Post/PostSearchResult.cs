namespace RoomRental.BackEnd.DTO.Post;

public sealed class PostSearchResult
{
    public List<PostListDto> Items { get; init; } = new();
    public int TotalCount { get; init; }
    public int PageNumber { get; init; }
    public int PageSize { get; init; }
    public int TotalPages => (int)Math.Ceiling((double)TotalCount / PageSize);
}
