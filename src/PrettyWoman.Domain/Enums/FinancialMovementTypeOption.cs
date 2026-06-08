namespace PrettyWoman.Domain.Enums;

public enum FinancialMovementTypeOption
{
    OwnerInvestment = 1,
    SupplierPayment = 2, // Compras
    SalePayment  = 3,
    Expense = 4,
    OwnerWithdrawal = 5, // Retiro de utilidades,
    SupplierRefund = 6,
    CustomerRefund = 7,
    LoanReceived = 8,
    LoanPayment = 9,
    Adjustment = 10
}