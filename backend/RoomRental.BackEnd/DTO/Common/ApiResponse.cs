namespace RoomRental.BackEnd.DTO.Common;

/// <summary>
/// Response chuẩn cho tất cả API
/// </summary>
public class ApiResponse<T>
{
    /// <summary>
    /// Trạng thái thành công hay thất bại
    /// </summary>
    public bool Success { get; set; }

    /// <summary>
    /// Thông báo
    /// </summary>
    public string Message { get; set; } = string.Empty;

    /// <summary>
    /// Dữ liệu trả về
    /// </summary>
    public T? Data { get; set; }

    /// <summary>
    /// Danh sách lỗi (nếu có)
    /// </summary>
    public List<string>? Errors { get; set; }

    /// <summary>
    /// Constructor cho response thành công
    /// </summary>
    public static ApiResponse<T> SuccessResponse(T data, string message = "Thành công")
    {
        return new ApiResponse<T>
        {
            Success = true,
            Message = message,
            Data = data
        };
    }

    /// <summary>
    /// Constructor cho response thất bại
    /// </summary>
    public static ApiResponse<T> ErrorResponse(string message, List<string>? errors = null)
    {
        return new ApiResponse<T>
        {
            Success = false,
            Message = message,
            Errors = errors
        };
    }
}
