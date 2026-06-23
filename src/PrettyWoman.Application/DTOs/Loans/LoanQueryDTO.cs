namespace PrettyWoman.Application.DTOs.Loans;

public class LoanQueryDTO
{
    public int Page { get; set; } = 1;
    public int PageSize { get; set; } = 20;
    public int? LoanOwnerId { get; set; }
    public bool? IsActive { get; set; }
}
