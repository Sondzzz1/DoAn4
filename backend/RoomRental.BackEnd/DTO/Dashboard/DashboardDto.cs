namespace RoomRental.BackEnd.DTO.Dashboard;

public class LandlordDashboardDto
{
    public int TotalRooms { get; set; }
    public int AvailableRooms { get; set; }
    public int RentedRooms { get; set; }
    public int MaintenanceRooms { get; set; }

    public int TotalPosts { get; set; }
    public int PendingPosts { get; set; }
    public int ApprovedPosts { get; set; }
    public int RejectedPosts { get; set; }

    public int TodayAppointments { get; set; }
    public int PendingAppointments { get; set; }
    public int TotalAppointments { get; set; }

    public int TotalViewCount { get; set; }
}

public class AdminDashboardDto
{
    public int TotalUsers { get; set; }
    public int TotalTenants { get; set; }
    public int TotalLandlords { get; set; }

    public int TotalRooms { get; set; }
    public int TotalPosts { get; set; }
    public int PendingPosts { get; set; }
    public int ApprovedPosts { get; set; }
    public int RejectedPosts { get; set; }
    public int HiddenPosts { get; set; }

    public int TotalAppointments { get; set; }
    public int TotalReports { get; set; }
    public int PendingReports { get; set; }
    public int TotalBlogs { get; set; }
}
