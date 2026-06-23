using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace PrettyWoman.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class LoanPayments : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropCheckConstraint(
                name: "ck_loans_balance_non_negative",
                table: "loans");

            migrationBuilder.DropColumn(
                name: "balance",
                table: "loans");

            migrationBuilder.AddColumn<int>(
                name: "loan_payment_id",
                table: "financial_movements",
                type: "integer",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "loan_payments",
                columns: table => new
                {
                    id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    loan_id = table.Column<int>(type: "integer", nullable: false),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    principal_amount = table.Column<decimal>(type: "numeric(12,2)", precision: 12, scale: 2, nullable: false),
                    interest_amount = table.Column<decimal>(type: "numeric(12,2)", precision: 12, scale: 2, nullable: false),
                    exchange_rate = table.Column<decimal>(type: "numeric(10,4)", precision: 10, scale: 4, nullable: false),
                    comments = table.Column<string>(type: "character varying(300)", maxLength: 300, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_loan_payments", x => x.id);
                    table.CheckConstraint("ck_loan_payments_exchange_rate_positive", "exchange_rate > 0");
                    table.CheckConstraint("ck_loan_payments_interest_amount_non_negative", "interest_amount >= 0");
                    table.CheckConstraint("ck_loan_payments_principal_amount_positive", "principal_amount > 0");
                    table.ForeignKey(
                        name: "fk_loan_payments_loans_loan_id",
                        column: x => x.loan_id,
                        principalTable: "loans",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.Sql("""
                INSERT INTO loan_payments (id, loan_id, created_at, principal_amount, interest_amount, exchange_rate, comments)
                SELECT principal.id,
                       principal.loan_id,
                       principal.created_at,
                       principal.amount,
                       COALESCE((
                           SELECT SUM(interest.amount)
                           FROM financial_movements interest
                           WHERE interest.loan_id = principal.loan_id
                             AND interest.financial_movement_type_id = 10
                             AND interest.created_at = principal.created_at
                       ), 0),
                       principal.exchange_rate,
                       principal.comments
                FROM financial_movements principal
                WHERE principal.financial_movement_type_id = 9
                  AND principal.loan_id IS NOT NULL;

                UPDATE financial_movements
                SET loan_payment_id = id
                WHERE financial_movement_type_id = 9
                  AND loan_id IS NOT NULL;

                UPDATE financial_movements interest
                SET loan_payment_id = (
                    SELECT principal.id
                    FROM financial_movements principal
                    WHERE principal.loan_id = interest.loan_id
                      AND principal.financial_movement_type_id = 9
                      AND principal.created_at = interest.created_at
                    ORDER BY principal.id
                    LIMIT 1
                )
                WHERE interest.financial_movement_type_id = 10
                  AND interest.loan_id IS NOT NULL;

                SELECT setval(
                    pg_get_serial_sequence('loan_payments', 'id'),
                    COALESCE((SELECT MAX(id) FROM loan_payments), 1),
                    true);
                """);

            migrationBuilder.CreateIndex(
                name: "ix_financial_movements_loan_payment_id",
                table: "financial_movements",
                column: "loan_payment_id");

            migrationBuilder.CreateIndex(
                name: "ix_loan_payments_created_at",
                table: "loan_payments",
                column: "created_at");

            migrationBuilder.CreateIndex(
                name: "ix_loan_payments_loan_id",
                table: "loan_payments",
                column: "loan_id");

            migrationBuilder.CreateIndex(
                name: "ix_loan_payments_loan_id_created_at",
                table: "loan_payments",
                columns: new[] { "loan_id", "created_at" });

            migrationBuilder.AddForeignKey(
                name: "fk_financial_movements_loan_payments_loan_payment_id",
                table: "financial_movements",
                column: "loan_payment_id",
                principalTable: "loan_payments",
                principalColumn: "id",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "fk_financial_movements_loan_payments_loan_payment_id",
                table: "financial_movements");

            migrationBuilder.AddColumn<decimal>(
                name: "balance",
                table: "loans",
                type: "numeric(12,2)",
                precision: 12,
                scale: 2,
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.Sql("""
                UPDATE loans
                SET balance = initial_amount - COALESCE((
                    SELECT SUM(principal_amount)
                    FROM loan_payments
                    WHERE loan_payments.loan_id = loans.id
                ), 0);
                """);

            migrationBuilder.DropTable(
                name: "loan_payments");

            migrationBuilder.DropIndex(
                name: "ix_financial_movements_loan_payment_id",
                table: "financial_movements");

            migrationBuilder.DropColumn(
                name: "loan_payment_id",
                table: "financial_movements");

            migrationBuilder.AddCheckConstraint(
                name: "ck_loans_balance_non_negative",
                table: "loans",
                sql: "balance >= 0");
        }
    }
}
