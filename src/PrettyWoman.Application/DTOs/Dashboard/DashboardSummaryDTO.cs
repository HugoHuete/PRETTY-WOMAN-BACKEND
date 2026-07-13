using System.Text.Json.Serialization;

namespace PrettyWoman.Application.DTOs.Dashboard;

public class DashboardSummaryDTO
{
    public DateOnly FromDate { get; set; }
    public DateOnly ToDate { get; set; }
    public DashboardSalesSummaryDTO Sales { get; set; } = new();
    public DashboardPaymentsSummaryDTO Payments { get; set; } = new();
    public DashboardOperationalSummaryDTO Operations { get; set; } = new();

    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public DashboardFinancialSummaryDTO? Financial { get; set; }
}

public class DashboardSalesSummaryDTO
{
    public int Count { get; set; }
    public decimal TotalNio { get; set; }
    public decimal PendingCollectionNio { get; set; }
}

public class DashboardPaymentsSummaryDTO
{
    public decimal CollectedNio { get; set; }
    public IEnumerable<DashboardPaymentMethodSummaryDTO> ByPaymentMethod { get; set; } = [];
}

public class DashboardPaymentMethodSummaryDTO
{
    public int PaymentMethodId { get; set; }
    public string PaymentMethodName { get; set; } = string.Empty;
    public decimal CollectedNio { get; set; }
}

public class DashboardOperationalSummaryDTO
{
    /// <summary>Reservas activas creadas dentro del período.</summary>
    public int ActiveReservationCount { get; set; }
    public int ActiveReservedUnitCount { get; set; }
    public IEnumerable<DashboardDeliveryStatusSummaryDTO> DeliveriesByStatus { get; set; } = [];
    /// <summary>Incidencias que continúan abiertas y fueron registradas dentro del período.</summary>
    public int OpenInventoryIssueCount { get; set; }
    public int OpenInventoryIssueUnitCount { get; set; }
}

public class DashboardDeliveryStatusSummaryDTO
{
    public int DeliveryStatusId { get; set; }
    public string DeliveryStatusName { get; set; } = string.Empty;
    public int Count { get; set; }
}

public class DashboardFinancialSummaryDTO
{
    public decimal IncomeNio { get; set; }
    public decimal ExpenseNio { get; set; }
    public decimal BalanceNio { get; set; }
}
