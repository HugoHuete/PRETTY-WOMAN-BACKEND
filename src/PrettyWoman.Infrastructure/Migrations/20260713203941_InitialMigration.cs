using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

#pragma warning disable CA1814 // Prefer jagged arrays over multidimensional

namespace PrettyWoman.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class InitialMigration : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "AspNetRoles",
                columns: table => new
                {
                    id = table.Column<string>(type: "text", nullable: false),
                    name = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: true),
                    normalized_name = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: true),
                    concurrency_stamp = table.Column<string>(type: "text", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_asp_net_roles", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "AspNetUsers",
                columns: table => new
                {
                    id = table.Column<string>(type: "text", nullable: false),
                    name = table.Column<string>(type: "text", nullable: false),
                    lastname = table.Column<string>(type: "text", nullable: false),
                    user_name = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: true),
                    normalized_user_name = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: true),
                    email = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: true),
                    normalized_email = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: true),
                    email_confirmed = table.Column<bool>(type: "boolean", nullable: false),
                    password_hash = table.Column<string>(type: "text", nullable: true),
                    security_stamp = table.Column<string>(type: "text", nullable: true),
                    concurrency_stamp = table.Column<string>(type: "text", nullable: true),
                    phone_number = table.Column<string>(type: "text", nullable: true),
                    phone_number_confirmed = table.Column<bool>(type: "boolean", nullable: false),
                    two_factor_enabled = table.Column<bool>(type: "boolean", nullable: false),
                    lockout_end = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    lockout_enabled = table.Column<bool>(type: "boolean", nullable: false),
                    access_failed_count = table.Column<int>(type: "integer", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_asp_net_users", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "categories",
                columns: table => new
                {
                    id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    name = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_categories", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "clients",
                columns: table => new
                {
                    id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    name = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    phone_number = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: true),
                    instagram_user = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: true),
                    messenger_user = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: true),
                    address = table.Column<string>(type: "character varying(350)", maxLength: 350, nullable: true),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    is_blocked = table.Column<bool>(type: "boolean", nullable: false, defaultValue: false),
                    is_friend = table.Column<bool>(type: "boolean", nullable: false, defaultValue: false),
                    blocked_reason = table.Column<string>(type: "character varying(350)", maxLength: 350, nullable: true),
                    comments = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_clients", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "delivery_agencies",
                columns: table => new
                {
                    id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    name = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    phone_number = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    enabled = table.Column<bool>(type: "boolean", nullable: false),
                    can_collect_cash_on_delivery = table.Column<bool>(type: "boolean", nullable: false, defaultValue: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_delivery_agencies", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "delivery_statuses",
                columns: table => new
                {
                    id = table.Column<int>(type: "integer", nullable: false),
                    name = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_delivery_statuses", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "departments",
                columns: table => new
                {
                    id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    name = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_departments", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "discount_campaigns",
                columns: table => new
                {
                    id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    name = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    start_date = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    end_date = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    enabled = table.Column<bool>(type: "boolean", nullable: false),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    updated_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    created_by_id = table.Column<string>(type: "text", nullable: true),
                    updated_by_id = table.Column<string>(type: "text", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_discount_campaigns", x => x.id);
                    table.CheckConstraint("ck_discount_campaigns_end_date_after_start_date", "end_date > start_date");
                });

            migrationBuilder.CreateTable(
                name: "discount_sources",
                columns: table => new
                {
                    id = table.Column<int>(type: "integer", nullable: false),
                    name = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_discount_sources", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "discount_types",
                columns: table => new
                {
                    id = table.Column<int>(type: "integer", nullable: false),
                    name = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_discount_types", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "dollar_exchange_rates",
                columns: table => new
                {
                    id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    store_rate = table.Column<decimal>(type: "numeric(10,4)", precision: 10, scale: 4, nullable: false),
                    bank_rate = table.Column<decimal>(type: "numeric(10,4)", precision: 10, scale: 4, nullable: false),
                    start_date = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    enabled = table.Column<bool>(type: "boolean", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_dollar_exchange_rates", x => x.id);
                    table.CheckConstraint("ck_dollar_exchange_rates_bank_rate_positive", "bank_rate > 0");
                    table.CheckConstraint("ck_dollar_exchange_rates_store_rate_positive", "store_rate > 0");
                });

            migrationBuilder.CreateTable(
                name: "expense_categories",
                columns: table => new
                {
                    id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    name = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    enabled = table.Column<bool>(type: "boolean", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_expense_categories", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "financial_movement_types",
                columns: table => new
                {
                    id = table.Column<int>(type: "integer", nullable: false),
                    name = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_financial_movement_types", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "inventory_movement_types",
                columns: table => new
                {
                    id = table.Column<int>(type: "integer", nullable: false),
                    name = table.Column<string>(type: "character varying(60)", maxLength: 60, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_inventory_movement_types", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "inventory_stock_buckets",
                columns: table => new
                {
                    id = table.Column<int>(type: "integer", nullable: false),
                    name = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_inventory_stock_buckets", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "loan_owners",
                columns: table => new
                {
                    id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    name = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    enabled = table.Column<bool>(type: "boolean", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_loan_owners", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "movement_directions",
                columns: table => new
                {
                    id = table.Column<int>(type: "integer", nullable: false),
                    name = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_movement_directions", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "order_statuses",
                columns: table => new
                {
                    id = table.Column<int>(type: "integer", nullable: false),
                    name = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_order_statuses", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "payment_methods",
                columns: table => new
                {
                    id = table.Column<int>(type: "integer", nullable: false),
                    name = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_payment_methods", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "payment_terminals",
                columns: table => new
                {
                    id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    name = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    comission_percentage = table.Column<decimal>(type: "numeric(5,2)", precision: 5, scale: 2, nullable: false),
                    income_tax_percentage = table.Column<decimal>(type: "numeric(5,2)", precision: 5, scale: 2, nullable: false),
                    enabled = table.Column<bool>(type: "boolean", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_payment_terminals", x => x.id);
                    table.CheckConstraint("ck_payment_terminals_comission_percentage_range", "comission_percentage >= 0 AND comission_percentage <= 100");
                });

            migrationBuilder.CreateTable(
                name: "product_hold_statuses",
                columns: table => new
                {
                    id = table.Column<int>(type: "integer", nullable: false),
                    name = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_product_hold_statuses", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "product_inventory_issue_statuses",
                columns: table => new
                {
                    id = table.Column<int>(type: "integer", nullable: false),
                    name = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_product_inventory_issue_statuses", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "product_inventory_issue_types",
                columns: table => new
                {
                    id = table.Column<int>(type: "integer", nullable: false),
                    name = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_product_inventory_issue_types", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "sale_channels",
                columns: table => new
                {
                    id = table.Column<int>(type: "integer", nullable: false),
                    name = table.Column<string>(type: "character varying(30)", maxLength: 30, nullable: false),
                    enabled = table.Column<bool>(type: "boolean", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_sale_channels", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "sale_payment_statuses",
                columns: table => new
                {
                    id = table.Column<int>(type: "integer", nullable: false),
                    name = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_sale_payment_statuses", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "sale_product_statuses",
                columns: table => new
                {
                    id = table.Column<int>(type: "integer", nullable: false),
                    name = table.Column<string>(type: "character varying(30)", maxLength: 30, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_sale_product_statuses", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "sale_statuses",
                columns: table => new
                {
                    id = table.Column<int>(type: "integer", nullable: false),
                    name = table.Column<string>(type: "character varying(30)", maxLength: 30, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_sale_statuses", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "shipping_companies",
                columns: table => new
                {
                    id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    name = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    url = table.Column<string>(type: "character varying(300)", maxLength: 300, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_shipping_companies", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "size_groups",
                columns: table => new
                {
                    id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    name = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_size_groups", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "suppliers",
                columns: table => new
                {
                    id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    name = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    url = table.Column<string>(type: "character varying(300)", maxLength: 300, nullable: true),
                    is_national = table.Column<bool>(type: "boolean", nullable: false),
                    enabled = table.Column<bool>(type: "boolean", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_suppliers", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "AspNetRoleClaims",
                columns: table => new
                {
                    id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    role_id = table.Column<string>(type: "text", nullable: false),
                    claim_type = table.Column<string>(type: "text", nullable: true),
                    claim_value = table.Column<string>(type: "text", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_asp_net_role_claims", x => x.id);
                    table.ForeignKey(
                        name: "fk_asp_net_role_claims_asp_net_roles_role_id",
                        column: x => x.role_id,
                        principalTable: "AspNetRoles",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "AspNetUserClaims",
                columns: table => new
                {
                    id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    user_id = table.Column<string>(type: "text", nullable: false),
                    claim_type = table.Column<string>(type: "text", nullable: true),
                    claim_value = table.Column<string>(type: "text", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_asp_net_user_claims", x => x.id);
                    table.ForeignKey(
                        name: "fk_asp_net_user_claims_asp_net_users_user_id",
                        column: x => x.user_id,
                        principalTable: "AspNetUsers",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "AspNetUserLogins",
                columns: table => new
                {
                    login_provider = table.Column<string>(type: "text", nullable: false),
                    provider_key = table.Column<string>(type: "text", nullable: false),
                    provider_display_name = table.Column<string>(type: "text", nullable: true),
                    user_id = table.Column<string>(type: "text", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_asp_net_user_logins", x => new { x.login_provider, x.provider_key });
                    table.ForeignKey(
                        name: "fk_asp_net_user_logins_asp_net_users_user_id",
                        column: x => x.user_id,
                        principalTable: "AspNetUsers",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "AspNetUserRoles",
                columns: table => new
                {
                    user_id = table.Column<string>(type: "text", nullable: false),
                    role_id = table.Column<string>(type: "text", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_asp_net_user_roles", x => new { x.user_id, x.role_id });
                    table.ForeignKey(
                        name: "fk_asp_net_user_roles_asp_net_roles_role_id",
                        column: x => x.role_id,
                        principalTable: "AspNetRoles",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "fk_asp_net_user_roles_asp_net_users_user_id",
                        column: x => x.user_id,
                        principalTable: "AspNetUsers",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "AspNetUserTokens",
                columns: table => new
                {
                    user_id = table.Column<string>(type: "text", nullable: false),
                    login_provider = table.Column<string>(type: "text", nullable: false),
                    name = table.Column<string>(type: "text", nullable: false),
                    value = table.Column<string>(type: "text", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_asp_net_user_tokens", x => new { x.user_id, x.login_provider, x.name });
                    table.ForeignKey(
                        name: "fk_asp_net_user_tokens_asp_net_users_user_id",
                        column: x => x.user_id,
                        principalTable: "AspNetUsers",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "subcategories",
                columns: table => new
                {
                    id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    category_id = table.Column<int>(type: "integer", nullable: false),
                    name = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_subcategories", x => x.id);
                    table.ForeignKey(
                        name: "fk_subcategories_categories_category_id",
                        column: x => x.category_id,
                        principalTable: "categories",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "delivery_agency_reconciliations",
                columns: table => new
                {
                    id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    delivery_agency_id = table.Column<int>(type: "integer", nullable: false),
                    reconciliation_date = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    settlement_exchange_rate = table.Column<decimal>(type: "numeric(10,4)", precision: 10, scale: 4, nullable: false),
                    amount_received_nio = table.Column<decimal>(type: "numeric(12,2)", precision: 12, scale: 2, nullable: false),
                    amount_received_usd = table.Column<decimal>(type: "numeric(12,2)", precision: 12, scale: 2, nullable: false),
                    amount_paid_to_agency_nio = table.Column<decimal>(type: "numeric(12,2)", precision: 12, scale: 2, nullable: false),
                    comments = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    updated_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    created_by_id = table.Column<string>(type: "text", nullable: true),
                    updated_by_id = table.Column<string>(type: "text", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_delivery_agency_reconciliations", x => x.id);
                    table.CheckConstraint("ck_delivery_agency_reconciliations_amount_paid_nio_non_negative", "amount_paid_to_agency_nio >= 0");
                    table.CheckConstraint("ck_delivery_agency_reconciliations_amount_received_nio_non_neg~", "amount_received_nio >= 0");
                    table.CheckConstraint("ck_delivery_agency_reconciliations_amount_received_usd_non_neg~", "amount_received_usd >= 0");
                    table.CheckConstraint("ck_delivery_agency_reconciliations_settlement_exchange_rate_po~", "settlement_exchange_rate > 0");
                    table.ForeignKey(
                        name: "fk_delivery_agency_reconciliations_delivery_agencies_delivery_",
                        column: x => x.delivery_agency_id,
                        principalTable: "delivery_agencies",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "municipalities",
                columns: table => new
                {
                    id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    name = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    department_id = table.Column<int>(type: "integer", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_municipalities", x => x.id);
                    table.ForeignKey(
                        name: "fk_municipalities_departments_department_id",
                        column: x => x.department_id,
                        principalTable: "departments",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "loans",
                columns: table => new
                {
                    id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    loan_owner_id = table.Column<int>(type: "integer", nullable: false),
                    initial_amount = table.Column<decimal>(type: "numeric(12,2)", precision: 12, scale: 2, nullable: false),
                    initial_amount_usd = table.Column<decimal>(type: "numeric(12,2)", precision: 12, scale: 2, nullable: false),
                    closed_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    comments = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    exchange_rate = table.Column<decimal>(type: "numeric(10,4)", precision: 10, scale: 4, nullable: false),
                    loan_date = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    updated_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    created_by_id = table.Column<string>(type: "text", nullable: true),
                    updated_by_id = table.Column<string>(type: "text", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_loans", x => x.id);
                    table.CheckConstraint("ck_loans_exchange_rate_positive", "exchange_rate > 0");
                    table.CheckConstraint("ck_loans_initial_amount_non_negative", "initial_amount >= 0");
                    table.CheckConstraint("ck_loans_initial_amount_usd_non_negative", "initial_amount_usd >= 0");
                    table.ForeignKey(
                        name: "fk_loans_loan_owners_loan_owner_id",
                        column: x => x.loan_owner_id,
                        principalTable: "loan_owners",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "sales",
                columns: table => new
                {
                    id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    sale_date = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    sale_channel_id = table.Column<int>(type: "integer", nullable: false),
                    sale_status_id = table.Column<int>(type: "integer", nullable: false, defaultValue: 1),
                    sale_payment_status_id = table.Column<int>(type: "integer", nullable: false, defaultValue: 1),
                    user_id = table.Column<string>(type: "text", nullable: false),
                    subtotal = table.Column<decimal>(type: "numeric(12,2)", precision: 12, scale: 2, nullable: false),
                    total_discount = table.Column<decimal>(type: "numeric(12,2)", precision: 12, scale: 2, nullable: false),
                    total = table.Column<decimal>(type: "numeric(12,2)", precision: 12, scale: 2, nullable: false),
                    comments = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    client_id = table.Column<int>(type: "integer", nullable: true),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    updated_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    created_by_id = table.Column<string>(type: "text", nullable: true),
                    updated_by_id = table.Column<string>(type: "text", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_sales", x => x.id);
                    table.CheckConstraint("ck_sales_subtotal_non_negative", "subtotal >= 0");
                    table.CheckConstraint("ck_sales_total_discount_non_negative", "total_discount >= 0");
                    table.CheckConstraint("ck_sales_total_discount_not_greater_than_subtotal", "total_discount <= subtotal");
                    table.CheckConstraint("ck_sales_total_matches_components", "total = subtotal - total_discount");
                    table.CheckConstraint("ck_sales_total_non_negative", "total >= 0");
                    table.ForeignKey(
                        name: "fk_sales_clients_client_id",
                        column: x => x.client_id,
                        principalTable: "clients",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "fk_sales_sale_channels_sale_channel_id",
                        column: x => x.sale_channel_id,
                        principalTable: "sale_channels",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "fk_sales_sale_payment_statuses_sale_payment_status_id",
                        column: x => x.sale_payment_status_id,
                        principalTable: "sale_payment_statuses",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "fk_sales_sale_statuses_sale_status_id",
                        column: x => x.sale_status_id,
                        principalTable: "sale_statuses",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "sizes",
                columns: table => new
                {
                    id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    name = table.Column<string>(type: "character varying(10)", maxLength: 10, nullable: false),
                    size_group_id = table.Column<int>(type: "integer", nullable: false),
                    display_order = table.Column<int>(type: "integer", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_sizes", x => x.id);
                    table.ForeignKey(
                        name: "fk_sizes_size_groups_size_group_id",
                        column: x => x.size_group_id,
                        principalTable: "size_groups",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "orders",
                columns: table => new
                {
                    id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    purchase_date = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    order_status_id = table.Column<int>(type: "integer", nullable: false),
                    supplier_id = table.Column<int>(type: "integer", nullable: false),
                    purchase_currency_id = table.Column<int>(type: "integer", nullable: false, defaultValue: 1),
                    amount_usd = table.Column<decimal>(type: "numeric(14,2)", precision: 14, scale: 2, nullable: false),
                    exchange_rate = table.Column<decimal>(type: "numeric(10,4)", precision: 10, scale: 4, nullable: false),
                    merchandise_total_nio = table.Column<decimal>(type: "numeric(14,2)", precision: 14, scale: 2, nullable: false),
                    received_amount_nio = table.Column<decimal>(type: "numeric(14,2)", precision: 14, scale: 2, nullable: false),
                    supplier_shipping_cost_usd = table.Column<decimal>(type: "numeric(14,2)", precision: 14, scale: 2, nullable: false),
                    warehouse_shipping_cost_usd = table.Column<decimal>(type: "numeric(14,2)", precision: 14, scale: 2, nullable: false),
                    total_cost_nio = table.Column<decimal>(type: "numeric(14,2)", precision: 14, scale: 2, nullable: false),
                    comments = table.Column<string>(type: "character varying(300)", maxLength: 300, nullable: true),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    updated_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    created_by_id = table.Column<string>(type: "text", nullable: true),
                    updated_by_id = table.Column<string>(type: "text", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_orders", x => x.id);
                    table.CheckConstraint("ck_orders_amount_usd_non_negative", "amount_usd >= 0");
                    table.CheckConstraint("ck_orders_exchange_rate_positive", "exchange_rate > 0");
                    table.CheckConstraint("ck_orders_merchandise_total_nio_non_negative", "merchandise_total_nio >= 0");
                    table.CheckConstraint("ck_orders_purchase_currency_valid", "purchase_currency_id IN (1, 2)");
                    table.CheckConstraint("ck_orders_received_amount_nio_non_negative", "received_amount_nio >= 0");
                    table.CheckConstraint("ck_orders_supplier_shipping_cost_usd_non_negative", "supplier_shipping_cost_usd >= 0");
                    table.CheckConstraint("ck_orders_total_cost_nio_non_negative", "total_cost_nio >= 0");
                    table.CheckConstraint("ck_orders_warehouse_shipping_cost_usd_non_negative", "warehouse_shipping_cost_usd >= 0");
                    table.ForeignKey(
                        name: "fk_orders_order_statuses_order_status_id",
                        column: x => x.order_status_id,
                        principalTable: "order_statuses",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "fk_orders_suppliers_supplier_id",
                        column: x => x.supplier_id,
                        principalTable: "suppliers",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "product_details",
                columns: table => new
                {
                    id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    supplier_product_code = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    code = table.Column<int>(type: "integer", nullable: false),
                    name = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    subcategory_id = table.Column<int>(type: "integer", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_product_details", x => x.id);
                    table.ForeignKey(
                        name: "fk_product_details_subcategories_subcategory_id",
                        column: x => x.subcategory_id,
                        principalTable: "subcategories",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "loan_payments",
                columns: table => new
                {
                    id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    loan_id = table.Column<int>(type: "integer", nullable: false),
                    principal_amount = table.Column<decimal>(type: "numeric(12,2)", precision: 12, scale: 2, nullable: false),
                    interest_amount = table.Column<decimal>(type: "numeric(12,2)", precision: 12, scale: 2, nullable: false),
                    exchange_rate = table.Column<decimal>(type: "numeric(10,4)", precision: 10, scale: 4, nullable: false),
                    comments = table.Column<string>(type: "character varying(300)", maxLength: 300, nullable: true),
                    payment_date = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    updated_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    created_by_id = table.Column<string>(type: "text", nullable: true),
                    updated_by_id = table.Column<string>(type: "text", nullable: true)
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

            migrationBuilder.CreateTable(
                name: "sale_deliveries",
                columns: table => new
                {
                    id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    code = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    sale_id = table.Column<int>(type: "integer", nullable: false),
                    municipality_id = table.Column<int>(type: "integer", nullable: false),
                    delivery_agency_id = table.Column<int>(type: "integer", nullable: false),
                    delivery_status_id = table.Column<int>(type: "integer", nullable: false),
                    client_id = table.Column<int>(type: "integer", nullable: true),
                    amount_to_collect = table.Column<decimal>(type: "numeric(12,2)", precision: 12, scale: 2, nullable: false),
                    amount_collected_nio = table.Column<decimal>(type: "numeric(12,2)", precision: 12, scale: 2, nullable: false),
                    amount_collected_usd = table.Column<decimal>(type: "numeric(12,2)", precision: 12, scale: 2, nullable: false),
                    change_given_nio = table.Column<decimal>(type: "numeric(12,2)", precision: 12, scale: 2, nullable: false),
                    collection_exchange_rate = table.Column<decimal>(type: "numeric(10,4)", precision: 10, scale: 4, nullable: true),
                    shipping_charged_to_client = table.Column<decimal>(type: "numeric(12,2)", precision: 12, scale: 2, nullable: false),
                    shipping_paid_to_agency = table.Column<decimal>(type: "numeric(12,2)", precision: 12, scale: 2, nullable: false),
                    delivery_address = table.Column<string>(type: "character varying(350)", maxLength: 350, nullable: true),
                    user_id = table.Column<string>(type: "text", nullable: false),
                    comments = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    delivery_agency_reconciliation_id = table.Column<int>(type: "integer", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_sale_deliveries", x => x.id);
                    table.CheckConstraint("ck_sale_deliveries_amount_collected_nio_non_negative", "amount_collected_nio >= 0");
                    table.CheckConstraint("ck_sale_deliveries_amount_collected_usd_non_negative", "amount_collected_usd >= 0");
                    table.CheckConstraint("ck_sale_deliveries_amount_to_collect_non_negative", "amount_to_collect >= 0");
                    table.CheckConstraint("ck_sale_deliveries_change_given_nio_non_negative", "change_given_nio >= 0");
                    table.CheckConstraint("ck_sale_deliveries_collection_exchange_rate_required_for_usd", "(\n                    (amount_collected_usd = 0 AND collection_exchange_rate IS NULL)\n                    OR\n                    (amount_collected_usd > 0 AND collection_exchange_rate > 0)\n                )");
                    table.CheckConstraint("ck_sale_deliveries_shipping_charged_to_client_non_negative", "shipping_charged_to_client >= 0");
                    table.CheckConstraint("ck_sale_deliveries_shipping_paid_to_agency_non_negative", "shipping_paid_to_agency >= 0");
                    table.ForeignKey(
                        name: "fk_sale_deliveries_clients_client_id",
                        column: x => x.client_id,
                        principalTable: "clients",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "fk_sale_deliveries_delivery_agencies_delivery_agency_id",
                        column: x => x.delivery_agency_id,
                        principalTable: "delivery_agencies",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "fk_sale_deliveries_delivery_agency_reconciliations_delivery_ag",
                        column: x => x.delivery_agency_reconciliation_id,
                        principalTable: "delivery_agency_reconciliations",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "fk_sale_deliveries_delivery_statuses_delivery_status_id",
                        column: x => x.delivery_status_id,
                        principalTable: "delivery_statuses",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "fk_sale_deliveries_municipalities_municipality_id",
                        column: x => x.municipality_id,
                        principalTable: "municipalities",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "fk_sale_deliveries_sales_sale_id",
                        column: x => x.sale_id,
                        principalTable: "sales",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "sale_exchanges",
                columns: table => new
                {
                    id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    original_sale_id = table.Column<int>(type: "integer", nullable: false),
                    status_id = table.Column<int>(type: "integer", nullable: false),
                    recognized_return_total = table.Column<decimal>(type: "numeric(14,2)", precision: 14, scale: 2, nullable: false),
                    outbound_items_total = table.Column<decimal>(type: "numeric(14,2)", precision: 14, scale: 2, nullable: false),
                    balance_to_collect = table.Column<decimal>(type: "numeric(14,2)", precision: 14, scale: 2, nullable: false),
                    comments = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    updated_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    created_by_id = table.Column<string>(type: "text", nullable: true),
                    updated_by_id = table.Column<string>(type: "text", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_sale_exchanges", x => x.id);
                    table.CheckConstraint("ck_sale_exchanges_totals_non_negative", "recognized_return_total >= 0 AND outbound_items_total >= 0");
                    table.ForeignKey(
                        name: "fk_sale_exchanges_sales_original_sale_id",
                        column: x => x.original_sale_id,
                        principalTable: "sales",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "sale_returns",
                columns: table => new
                {
                    id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    original_sale_id = table.Column<int>(type: "integer", nullable: false),
                    reason_id = table.Column<int>(type: "integer", nullable: false),
                    method_id = table.Column<int>(type: "integer", nullable: false),
                    status_id = table.Column<int>(type: "integer", nullable: false),
                    delivery_agency_id = table.Column<int>(type: "integer", nullable: true),
                    return_code = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    product_refund_total = table.Column<decimal>(type: "numeric(14,2)", precision: 14, scale: 2, nullable: false),
                    return_shipping_charged_to_client = table.Column<decimal>(type: "numeric(14,2)", precision: 14, scale: 2, nullable: false),
                    return_shipping_paid_to_agency = table.Column<decimal>(type: "numeric(14,2)", precision: 14, scale: 2, nullable: false),
                    original_shipping_refund = table.Column<decimal>(type: "numeric(14,2)", precision: 14, scale: 2, nullable: false),
                    refund_total = table.Column<decimal>(type: "numeric(14,2)", precision: 14, scale: 2, nullable: false),
                    refund_payment_method_id = table.Column<int>(type: "integer", nullable: true),
                    delivery_agency_reconciliation_id = table.Column<int>(type: "integer", nullable: true),
                    refunded_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    picked_up_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    received_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    comments = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    updated_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    created_by_id = table.Column<string>(type: "text", nullable: true),
                    updated_by_id = table.Column<string>(type: "text", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_sale_returns", x => x.id);
                    table.CheckConstraint("ck_sale_returns_totals_non_negative", "product_refund_total >= 0 AND return_shipping_charged_to_client >= 0 AND return_shipping_paid_to_agency >= 0 AND original_shipping_refund >= 0 AND refund_total >= 0");
                    table.ForeignKey(
                        name: "fk_sale_returns_delivery_agencies_delivery_agency_id",
                        column: x => x.delivery_agency_id,
                        principalTable: "delivery_agencies",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "fk_sale_returns_delivery_agency_reconciliations_delivery_agenc",
                        column: x => x.delivery_agency_reconciliation_id,
                        principalTable: "delivery_agency_reconciliations",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "fk_sale_returns_payment_methods_refund_payment_method_id",
                        column: x => x.refund_payment_method_id,
                        principalTable: "payment_methods",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "fk_sale_returns_sales_original_sale_id",
                        column: x => x.original_sale_id,
                        principalTable: "sales",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "product_receipts",
                columns: table => new
                {
                    id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    order_id = table.Column<int>(type: "integer", nullable: false),
                    received_date = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    updated_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    created_by_id = table.Column<string>(type: "text", nullable: true),
                    updated_by_id = table.Column<string>(type: "text", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_product_receipts", x => x.id);
                    table.ForeignKey(
                        name: "fk_product_receipts_orders_order_id",
                        column: x => x.order_id,
                        principalTable: "orders",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "discount_campaign_products",
                columns: table => new
                {
                    id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    product_detail_id = table.Column<int>(type: "integer", nullable: false),
                    discount_campaign_id = table.Column<int>(type: "integer", nullable: false),
                    discount_type_id = table.Column<int>(type: "integer", nullable: false),
                    discount_value = table.Column<decimal>(type: "numeric(12,2)", precision: 12, scale: 2, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_discount_campaign_products", x => x.id);
                    table.CheckConstraint("ck_discount_campaign_product_value_non_negative", "discount_value > 0");
                    table.ForeignKey(
                        name: "fk_discount_campaign_products_discount_campaigns_discount_camp",
                        column: x => x.discount_campaign_id,
                        principalTable: "discount_campaigns",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "fk_discount_campaign_products_discount_types_discount_type_id",
                        column: x => x.discount_type_id,
                        principalTable: "discount_types",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "fk_discount_campaign_products_product_details_product_detail_id",
                        column: x => x.product_detail_id,
                        principalTable: "product_details",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "product_images",
                columns: table => new
                {
                    id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    product_detail_id = table.Column<int>(type: "integer", nullable: false),
                    image_url = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: false),
                    is_primary = table.Column<bool>(type: "boolean", nullable: false),
                    sort_order = table.Column<int>(type: "integer", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_product_images", x => x.id);
                    table.CheckConstraint("ck_product_images_sort_order_non_negative", "sort_order >= 0");
                    table.ForeignKey(
                        name: "fk_product_images_product_details_product_detail_id",
                        column: x => x.product_detail_id,
                        principalTable: "product_details",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "products",
                columns: table => new
                {
                    id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    order_id = table.Column<int>(type: "integer", nullable: false),
                    product_detail_id = table.Column<int>(type: "integer", nullable: false),
                    size_id = table.Column<int>(type: "integer", nullable: false),
                    color = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: true),
                    quantity = table.Column<int>(type: "integer", nullable: false),
                    received_quantity = table.Column<int>(type: "integer", nullable: false),
                    available_quantity = table.Column<int>(type: "integer", nullable: false),
                    reserved_quantity = table.Column<int>(type: "integer", nullable: false),
                    unavailable_quantity = table.Column<int>(type: "integer", nullable: false),
                    unit_cost_usd = table.Column<decimal>(type: "numeric(14,2)", precision: 14, scale: 2, nullable: false),
                    merchandise_total_cost_nio = table.Column<decimal>(type: "numeric(14,2)", precision: 14, scale: 2, nullable: false),
                    allocated_shipping_cost_nio = table.Column<decimal>(type: "numeric(14,2)", precision: 14, scale: 2, nullable: false),
                    total_cost_nio = table.Column<decimal>(type: "numeric(14,2)", precision: 14, scale: 2, nullable: false),
                    unit_cost_nio = table.Column<decimal>(type: "numeric(18,6)", precision: 18, scale: 6, nullable: false),
                    sale_price = table.Column<decimal>(type: "numeric(12,2)", precision: 12, scale: 2, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_products", x => x.id);
                    table.CheckConstraint("ck_products_allocated_shipping_cost_nio_non_negative", "allocated_shipping_cost_nio >= 0");
                    table.CheckConstraint("ck_products_available_quantity_non_negative", "available_quantity >= 0");
                    table.CheckConstraint("ck_products_merchandise_total_cost_nio_non_negative", "merchandise_total_cost_nio >= 0");
                    table.CheckConstraint("ck_products_quantity_positive", "quantity > 0");
                    table.CheckConstraint("ck_products_received_quantity_non_negative", "received_quantity >= 0");
                    table.CheckConstraint("ck_products_received_quantity_not_greater_than_quantity", "received_quantity <= quantity");
                    table.CheckConstraint("ck_products_reserved_quantity_non_negative", "reserved_quantity >= 0");
                    table.CheckConstraint("ck_products_sale_price_positive", "sale_price >= 0");
                    table.CheckConstraint("ck_products_stock_not_greater_than_received", "available_quantity + reserved_quantity + unavailable_quantity <= received_quantity");
                    table.CheckConstraint("ck_products_total_cost_nio_non_negative", "total_cost_nio >= 0");
                    table.CheckConstraint("ck_products_unavailable_quantity_non_negative", "unavailable_quantity >= 0");
                    table.CheckConstraint("ck_products_unit_cost_nio_non_negative", "unit_cost_nio >= 0");
                    table.CheckConstraint("ck_products_unit_cost_usd_non_negative", "unit_cost_usd >= 0");
                    table.ForeignKey(
                        name: "fk_products_orders_order_id",
                        column: x => x.order_id,
                        principalTable: "orders",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "fk_products_product_details_product_detail_id",
                        column: x => x.product_detail_id,
                        principalTable: "product_details",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "fk_products_sizes_size_id",
                        column: x => x.size_id,
                        principalTable: "sizes",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "sale_payment_movements",
                columns: table => new
                {
                    id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    movement_date = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    sale_id = table.Column<int>(type: "integer", nullable: false),
                    movement_direction_id = table.Column<int>(type: "integer", nullable: false, defaultValue: 1),
                    payment_method_id = table.Column<int>(type: "integer", nullable: false),
                    payment_terminal_id = table.Column<int>(type: "integer", nullable: true),
                    reversed_sale_payment_movement_id = table.Column<int>(type: "integer", nullable: true),
                    gross_amount = table.Column<decimal>(type: "numeric(12,2)", precision: 12, scale: 2, nullable: false),
                    product_amount = table.Column<decimal>(type: "numeric(12,2)", precision: 12, scale: 2, nullable: false),
                    shipping_amount = table.Column<decimal>(type: "numeric(12,2)", precision: 12, scale: 2, nullable: false),
                    sale_delivery_id = table.Column<int>(type: "integer", nullable: true),
                    delivery_agency_reconciliation_id = table.Column<int>(type: "integer", nullable: true),
                    commission_percentage = table.Column<decimal>(type: "numeric(5,2)", precision: 5, scale: 2, nullable: false),
                    commission_amount = table.Column<decimal>(type: "numeric(12,2)", precision: 12, scale: 2, nullable: false),
                    income_tax_percentage = table.Column<decimal>(type: "numeric(5,2)", precision: 5, scale: 2, nullable: false),
                    income_tax_amount = table.Column<decimal>(type: "numeric(12,2)", precision: 12, scale: 2, nullable: false),
                    net_received_amount = table.Column<decimal>(type: "numeric(12,2)", precision: 12, scale: 2, nullable: false),
                    user_id = table.Column<string>(type: "text", nullable: false),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    updated_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    created_by_id = table.Column<string>(type: "text", nullable: true),
                    updated_by_id = table.Column<string>(type: "text", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_sale_payment_movements", x => x.id);
                    table.CheckConstraint("ck_sale_payment_movements_amount_matches_allocations", "gross_amount = product_amount + shipping_amount");
                    table.CheckConstraint("ck_sale_payment_movements_card_refund_reverses_original", "movement_direction_id <> 2 OR payment_method_id <> 3 OR reversed_sale_payment_movement_id IS NOT NULL");
                    table.CheckConstraint("ck_sale_payment_movements_card_requires_terminal", "payment_method_id <> 3 OR payment_terminal_id IS NOT NULL");
                    table.CheckConstraint("ck_sale_payment_movements_commission_amount_non_negative", "commission_amount >= 0");
                    table.CheckConstraint("ck_sale_payment_movements_commission_not_greater_than_amount", "commission_amount + income_tax_amount <= gross_amount");
                    table.CheckConstraint("ck_sale_payment_movements_commission_percentage_non_negative", "commission_percentage >= 0");
                    table.CheckConstraint("ck_sale_payment_movements_gross_amount_positive", "gross_amount > 0");
                    table.CheckConstraint("ck_sale_payment_movements_in_does_not_reverse", "movement_direction_id <> 1 OR reversed_sale_payment_movement_id IS NULL");
                    table.CheckConstraint("ck_sale_payment_movements_income_tax_amount_non_negative", "income_tax_amount >= 0");
                    table.CheckConstraint("ck_sale_payment_movements_income_tax_percentage_non_negative", "income_tax_percentage >= 0");
                    table.CheckConstraint("ck_sale_payment_movements_net_received_amount_matches_componen~", "net_received_amount = gross_amount - commission_amount - income_tax_amount");
                    table.CheckConstraint("ck_sale_payment_movements_net_received_amount_non_negative", "net_received_amount >= 0");
                    table.CheckConstraint("ck_sale_payment_movements_only_card_uses_terminal", "payment_method_id = 3 OR payment_terminal_id IS NULL");
                    table.CheckConstraint("ck_sale_payment_movements_product_amount_non_negative", "product_amount >= 0");
                    table.CheckConstraint("ck_sale_payment_movements_shipping_amount_non_negative", "shipping_amount >= 0");
                    table.CheckConstraint("ck_sale_payment_movements_shipping_requires_delivery", "(shipping_amount = 0 AND sale_delivery_id IS NULL) OR (shipping_amount > 0 AND sale_delivery_id IS NOT NULL)");
                    table.ForeignKey(
                        name: "fk_sale_payment_movements_delivery_agency_reconciliations_deli",
                        column: x => x.delivery_agency_reconciliation_id,
                        principalTable: "delivery_agency_reconciliations",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "fk_sale_payment_movements_movement_directions_movement_directi",
                        column: x => x.movement_direction_id,
                        principalTable: "movement_directions",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "fk_sale_payment_movements_payment_methods_payment_method_id",
                        column: x => x.payment_method_id,
                        principalTable: "payment_methods",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "fk_sale_payment_movements_payment_terminals_payment_terminal_id",
                        column: x => x.payment_terminal_id,
                        principalTable: "payment_terminals",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "fk_sale_payment_movements_sale_deliveries_sale_delivery_id",
                        column: x => x.sale_delivery_id,
                        principalTable: "sale_deliveries",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "fk_sale_payment_movements_sale_payment_movements_reversed_sale",
                        column: x => x.reversed_sale_payment_movement_id,
                        principalTable: "sale_payment_movements",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "fk_sale_payment_movements_sales_sale_id",
                        column: x => x.sale_id,
                        principalTable: "sales",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "order_tracking_numbers",
                columns: table => new
                {
                    id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    order_id = table.Column<int>(type: "integer", nullable: false),
                    shipping_company_id = table.Column<int>(type: "integer", nullable: false),
                    tracking_number = table.Column<string>(type: "character varying(60)", maxLength: 60, nullable: false),
                    supplier_shipment_date = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    warehouse_delivery_date = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    product_receipt_id = table.Column<int>(type: "integer", nullable: true),
                    weight = table.Column<decimal>(type: "numeric(12,2)", precision: 12, scale: 2, nullable: false),
                    shipping_cost = table.Column<decimal>(type: "numeric(12,2)", precision: 12, scale: 2, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_order_tracking_numbers", x => x.id);
                    table.CheckConstraint("ck_order_tracking_number_shipping_cost_non_negative", "shipping_cost >= 0");
                    table.CheckConstraint("ck_order_tracking_number_weight_non_negative", "weight >= 0");
                    table.ForeignKey(
                        name: "fk_order_tracking_numbers_orders_order_id",
                        column: x => x.order_id,
                        principalTable: "orders",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "fk_order_tracking_numbers_product_receipts_product_receipt_id",
                        column: x => x.product_receipt_id,
                        principalTable: "product_receipts",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "fk_order_tracking_numbers_shipping_companies_shipping_company_",
                        column: x => x.shipping_company_id,
                        principalTable: "shipping_companies",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "exchange_outbound_items",
                columns: table => new
                {
                    id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    sale_exchange_id = table.Column<int>(type: "integer", nullable: false),
                    product_id = table.Column<int>(type: "integer", nullable: false),
                    quantity = table.Column<int>(type: "integer", nullable: false),
                    item_type_id = table.Column<int>(type: "integer", nullable: false),
                    unit_price = table.Column<decimal>(type: "numeric(14,2)", precision: 14, scale: 2, nullable: false),
                    unit_cost = table.Column<decimal>(type: "numeric(18,6)", precision: 18, scale: 6, nullable: false),
                    line_total = table.Column<decimal>(type: "numeric(14,2)", precision: 14, scale: 2, nullable: false),
                    total_cost = table.Column<decimal>(type: "numeric(18,6)", precision: 18, scale: 6, nullable: false),
                    delivered = table.Column<bool>(type: "boolean", nullable: false),
                    delivered_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    comments = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    updated_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    created_by_id = table.Column<string>(type: "text", nullable: true),
                    updated_by_id = table.Column<string>(type: "text", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_exchange_outbound_items", x => x.id);
                    table.CheckConstraint("ck_exchange_outbound_items_quantity_positive", "quantity > 0");
                    table.ForeignKey(
                        name: "fk_exchange_outbound_items_products_product_id",
                        column: x => x.product_id,
                        principalTable: "products",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "fk_exchange_outbound_items_sale_exchanges_sale_exchange_id",
                        column: x => x.sale_exchange_id,
                        principalTable: "sale_exchanges",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "product_holds",
                columns: table => new
                {
                    id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    product_id = table.Column<int>(type: "integer", nullable: false),
                    sale_id = table.Column<int>(type: "integer", nullable: true),
                    quantity = table.Column<int>(type: "integer", nullable: false),
                    hold_date = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    resolved_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    hold_reason = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    comments = table.Column<string>(type: "character varying(300)", maxLength: 300, nullable: true),
                    product_hold_status_id = table.Column<int>(type: "integer", nullable: false),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    updated_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    created_by_id = table.Column<string>(type: "text", nullable: true),
                    updated_by_id = table.Column<string>(type: "text", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_product_holds", x => x.id);
                    table.CheckConstraint("ck_product_holds_quantity_non_negative", "quantity >= 1");
                    table.ForeignKey(
                        name: "fk_product_holds_product_hold_statuses_product_hold_status_id",
                        column: x => x.product_hold_status_id,
                        principalTable: "product_hold_statuses",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "fk_product_holds_products_product_id",
                        column: x => x.product_id,
                        principalTable: "products",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "fk_product_holds_sales_sale_id",
                        column: x => x.sale_id,
                        principalTable: "sales",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "product_inventory_issues",
                columns: table => new
                {
                    id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    product_id = table.Column<int>(type: "integer", nullable: false),
                    product_inventory_issue_type_id = table.Column<int>(type: "integer", nullable: false),
                    product_inventory_issue_status_id = table.Column<int>(type: "integer", nullable: false),
                    quantity = table.Column<int>(type: "integer", nullable: false),
                    issue_date = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    resolved_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    comments = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    updated_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    created_by_id = table.Column<string>(type: "text", nullable: true),
                    updated_by_id = table.Column<string>(type: "text", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_product_inventory_issues", x => x.id);
                    table.CheckConstraint("ck_product_inventory_issues_quantity_positive", "quantity > 0");
                    table.ForeignKey(
                        name: "fk_product_inventory_issues_product_inventory_issue_statuses_p",
                        column: x => x.product_inventory_issue_status_id,
                        principalTable: "product_inventory_issue_statuses",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "fk_product_inventory_issues_product_inventory_issue_types_prod",
                        column: x => x.product_inventory_issue_type_id,
                        principalTable: "product_inventory_issue_types",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "fk_product_inventory_issues_products_product_id",
                        column: x => x.product_id,
                        principalTable: "products",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "product_receipt_details",
                columns: table => new
                {
                    id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    product_receipt_id = table.Column<int>(type: "integer", nullable: false),
                    product_id = table.Column<int>(type: "integer", nullable: false),
                    quantity = table.Column<decimal>(type: "numeric", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_product_receipt_details", x => x.id);
                    table.CheckConstraint("ck_product_receipt_detail_quantity_non_negative", "quantity >= 0");
                    table.ForeignKey(
                        name: "fk_product_receipt_details_product_receipts_product_receipt_id",
                        column: x => x.product_receipt_id,
                        principalTable: "product_receipts",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "fk_product_receipt_details_products_product_id",
                        column: x => x.product_id,
                        principalTable: "products",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "sale_products",
                columns: table => new
                {
                    id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    sale_id = table.Column<int>(type: "integer", nullable: false),
                    product_id = table.Column<int>(type: "integer", nullable: false),
                    quantity = table.Column<int>(type: "integer", nullable: false),
                    unit_cost_at_sale = table.Column<decimal>(type: "numeric(18,6)", precision: 18, scale: 6, nullable: false),
                    original_unit_price = table.Column<decimal>(type: "numeric(14,2)", precision: 14, scale: 2, nullable: false),
                    discount_source_id = table.Column<int>(type: "integer", nullable: false),
                    discount_campaign_id = table.Column<int>(type: "integer", nullable: true),
                    discount_amount = table.Column<decimal>(type: "numeric(14,2)", precision: 14, scale: 2, nullable: false),
                    final_unit_price = table.Column<decimal>(type: "numeric(14,2)", precision: 14, scale: 2, nullable: false),
                    line_total = table.Column<decimal>(type: "numeric(14,2)", precision: 14, scale: 2, nullable: false),
                    total_cost_at_sale = table.Column<decimal>(type: "numeric(18,6)", precision: 18, scale: 6, nullable: false),
                    gross_profit = table.Column<decimal>(type: "numeric(18,6)", precision: 18, scale: 6, nullable: false),
                    sale_product_status_id = table.Column<int>(type: "integer", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_sale_products", x => x.id);
                    table.CheckConstraint("ck_sale_details_discount_amount_non_negative", "discount_amount >= 0");
                    table.CheckConstraint("ck_sale_details_discount_amount_not_greater_than_original_unit~", "discount_amount <= original_unit_price");
                    table.CheckConstraint("ck_sale_details_final_unit_price_non_negative", "final_unit_price >= 0");
                    table.CheckConstraint("ck_sale_details_gross_profit_matches_components", "gross_profit = line_total - total_cost_at_sale");
                    table.CheckConstraint("ck_sale_details_line_total_non_negative", "line_total >= 0");
                    table.CheckConstraint("ck_sale_details_original_unit_price_non_negative", "original_unit_price >= 0");
                    table.CheckConstraint("ck_sale_details_quantity_positive", "quantity > 0");
                    table.CheckConstraint("ck_sale_details_total_cost_at_sale_non_negative", "total_cost_at_sale >= 0");
                    table.CheckConstraint("ck_sale_details_unit_cost_at_sale_non_negative", "unit_cost_at_sale >= 0");
                    table.ForeignKey(
                        name: "fk_sale_products_discount_campaigns_discount_campaign_id",
                        column: x => x.discount_campaign_id,
                        principalTable: "discount_campaigns",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "fk_sale_products_discount_sources_discount_source_id",
                        column: x => x.discount_source_id,
                        principalTable: "discount_sources",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "fk_sale_products_products_product_id",
                        column: x => x.product_id,
                        principalTable: "products",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "fk_sale_products_sale_product_statuses_sale_product_status_id",
                        column: x => x.sale_product_status_id,
                        principalTable: "sale_product_statuses",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "fk_sale_products_sales_sale_id",
                        column: x => x.sale_id,
                        principalTable: "sales",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "financial_movements",
                columns: table => new
                {
                    id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    description = table.Column<string>(type: "character varying(300)", maxLength: 300, nullable: false),
                    movement_date = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    movement_direction_id = table.Column<int>(type: "integer", nullable: false),
                    financial_movement_type_id = table.Column<int>(type: "integer", nullable: false),
                    expense_category_id = table.Column<int>(type: "integer", nullable: true),
                    order_id = table.Column<int>(type: "integer", nullable: true),
                    product_receipt_id = table.Column<int>(type: "integer", nullable: true),
                    sale_payment_movement_id = table.Column<int>(type: "integer", nullable: true),
                    delivery_agency_reconciliation_id = table.Column<int>(type: "integer", nullable: true),
                    sale_return_id = table.Column<int>(type: "integer", nullable: true),
                    loan_id = table.Column<int>(type: "integer", nullable: true),
                    loan_payment_id = table.Column<int>(type: "integer", nullable: true),
                    amount = table.Column<decimal>(type: "numeric(12,2)", precision: 12, scale: 2, nullable: false),
                    exchange_rate = table.Column<decimal>(type: "numeric(10,4)", precision: 10, scale: 4, nullable: false),
                    comments = table.Column<string>(type: "character varying(300)", maxLength: 300, nullable: true),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    updated_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    created_by_id = table.Column<string>(type: "text", nullable: true),
                    updated_by_id = table.Column<string>(type: "text", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_financial_movements", x => x.id);
                    table.CheckConstraint("ck_financial_movements_amount_positive", "amount > 0");
                    table.CheckConstraint("ck_financial_movements_exchange_rate_positive", "exchange_rate > 0");
                    table.ForeignKey(
                        name: "fk_financial_movements_delivery_agency_reconciliations_deliver",
                        column: x => x.delivery_agency_reconciliation_id,
                        principalTable: "delivery_agency_reconciliations",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "fk_financial_movements_expense_categories_expense_category_id",
                        column: x => x.expense_category_id,
                        principalTable: "expense_categories",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "fk_financial_movements_financial_movement_types_financial_move",
                        column: x => x.financial_movement_type_id,
                        principalTable: "financial_movement_types",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "fk_financial_movements_loan_payments_loan_payment_id",
                        column: x => x.loan_payment_id,
                        principalTable: "loan_payments",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "fk_financial_movements_loans_loan_id",
                        column: x => x.loan_id,
                        principalTable: "loans",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "fk_financial_movements_movement_directions_movement_direction_",
                        column: x => x.movement_direction_id,
                        principalTable: "movement_directions",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "fk_financial_movements_orders_order_id",
                        column: x => x.order_id,
                        principalTable: "orders",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "fk_financial_movements_product_receipts_product_receipt_id",
                        column: x => x.product_receipt_id,
                        principalTable: "product_receipts",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "fk_financial_movements_sale_payment_movements_sale_payment_mov",
                        column: x => x.sale_payment_movement_id,
                        principalTable: "sale_payment_movements",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "fk_financial_movements_sale_returns_sale_return_id",
                        column: x => x.sale_return_id,
                        principalTable: "sale_returns",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "exchange_return_items",
                columns: table => new
                {
                    id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    sale_exchange_id = table.Column<int>(type: "integer", nullable: false),
                    original_sale_product_id = table.Column<int>(type: "integer", nullable: false),
                    product_id = table.Column<int>(type: "integer", nullable: false),
                    quantity = table.Column<int>(type: "integer", nullable: false),
                    recognized_unit_amount = table.Column<decimal>(type: "numeric(14,2)", precision: 14, scale: 2, nullable: false),
                    original_unit_cost = table.Column<decimal>(type: "numeric(18,6)", precision: 18, scale: 6, nullable: false),
                    status_id = table.Column<int>(type: "integer", nullable: false),
                    handed_to_agency_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    received_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    comments = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    updated_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    created_by_id = table.Column<string>(type: "text", nullable: true),
                    updated_by_id = table.Column<string>(type: "text", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_exchange_return_items", x => x.id);
                    table.CheckConstraint("ck_exchange_return_items_quantity_positive", "quantity > 0");
                    table.ForeignKey(
                        name: "fk_exchange_return_items_products_product_id",
                        column: x => x.product_id,
                        principalTable: "products",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "fk_exchange_return_items_sale_exchanges_sale_exchange_id",
                        column: x => x.sale_exchange_id,
                        principalTable: "sale_exchanges",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "fk_exchange_return_items_sale_products_original_sale_product_id",
                        column: x => x.original_sale_product_id,
                        principalTable: "sale_products",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "sale_return_items",
                columns: table => new
                {
                    id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    sale_return_id = table.Column<int>(type: "integer", nullable: false),
                    original_sale_product_id = table.Column<int>(type: "integer", nullable: false),
                    product_id = table.Column<int>(type: "integer", nullable: false),
                    quantity = table.Column<int>(type: "integer", nullable: false),
                    recognized_unit_amount = table.Column<decimal>(type: "numeric(14,2)", precision: 14, scale: 2, nullable: false),
                    original_unit_cost = table.Column<decimal>(type: "numeric(18,6)", precision: 18, scale: 6, nullable: false),
                    product_inventory_issue_id = table.Column<int>(type: "integer", nullable: true),
                    received_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    comments = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    updated_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    created_by_id = table.Column<string>(type: "text", nullable: true),
                    updated_by_id = table.Column<string>(type: "text", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_sale_return_items", x => x.id);
                    table.CheckConstraint("ck_sale_return_items_quantity_positive", "quantity > 0");
                    table.ForeignKey(
                        name: "fk_sale_return_items_product_inventory_issues_product_inventor",
                        column: x => x.product_inventory_issue_id,
                        principalTable: "product_inventory_issues",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "fk_sale_return_items_products_product_id",
                        column: x => x.product_id,
                        principalTable: "products",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "fk_sale_return_items_sale_products_original_sale_product_id",
                        column: x => x.original_sale_product_id,
                        principalTable: "sale_products",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "fk_sale_return_items_sale_returns_sale_return_id",
                        column: x => x.sale_return_id,
                        principalTable: "sale_returns",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "inventory_movements",
                columns: table => new
                {
                    id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    product_id = table.Column<int>(type: "integer", nullable: false),
                    inventory_movement_type_id = table.Column<int>(type: "integer", nullable: false),
                    from_stock_bucket_id = table.Column<int>(type: "integer", nullable: false),
                    to_stock_bucket_id = table.Column<int>(type: "integer", nullable: false),
                    quantity = table.Column<int>(type: "integer", nullable: false),
                    order_id = table.Column<int>(type: "integer", nullable: true),
                    sale_product_id = table.Column<int>(type: "integer", nullable: true),
                    product_hold_id = table.Column<int>(type: "integer", nullable: true),
                    product_inventory_issue_id = table.Column<int>(type: "integer", nullable: true),
                    exchange_return_item_id = table.Column<int>(type: "integer", nullable: true),
                    exchange_outbound_item_id = table.Column<int>(type: "integer", nullable: true),
                    sale_return_item_id = table.Column<int>(type: "integer", nullable: true),
                    comments = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    movement_date = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    updated_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    created_by_id = table.Column<string>(type: "text", nullable: true),
                    updated_by_id = table.Column<string>(type: "text", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_inventory_movements", x => x.id);
                    table.CheckConstraint("ck_inventory_movements_quantity_positive", "quantity > 0");
                    table.ForeignKey(
                        name: "fk_inventory_movements_exchange_outbound_items_exchange_outbou",
                        column: x => x.exchange_outbound_item_id,
                        principalTable: "exchange_outbound_items",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "fk_inventory_movements_exchange_return_items_exchange_return_i",
                        column: x => x.exchange_return_item_id,
                        principalTable: "exchange_return_items",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "fk_inventory_movements_inventory_movement_types_inventory_move",
                        column: x => x.inventory_movement_type_id,
                        principalTable: "inventory_movement_types",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "fk_inventory_movements_inventory_stock_buckets_from_stock_buck",
                        column: x => x.from_stock_bucket_id,
                        principalTable: "inventory_stock_buckets",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "fk_inventory_movements_inventory_stock_buckets_to_stock_bucket",
                        column: x => x.to_stock_bucket_id,
                        principalTable: "inventory_stock_buckets",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "fk_inventory_movements_orders_order_id",
                        column: x => x.order_id,
                        principalTable: "orders",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "fk_inventory_movements_product_holds_product_hold_id",
                        column: x => x.product_hold_id,
                        principalTable: "product_holds",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "fk_inventory_movements_product_inventory_issues_product_invent",
                        column: x => x.product_inventory_issue_id,
                        principalTable: "product_inventory_issues",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "fk_inventory_movements_products_product_id",
                        column: x => x.product_id,
                        principalTable: "products",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "fk_inventory_movements_sale_products_sale_product_id",
                        column: x => x.sale_product_id,
                        principalTable: "sale_products",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "fk_inventory_movements_sale_return_items_sale_return_item_id",
                        column: x => x.sale_return_item_id,
                        principalTable: "sale_return_items",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.InsertData(
                table: "delivery_statuses",
                columns: new[] { "id", "name" },
                values: new object[,]
                {
                    { 1, "Pending" },
                    { 2, "Completed" },
                    { 3, "Cancelled" },
                    { 4, "Sent" },
                    { 5, "Failed" },
                    { 6, "DeliveredPendingSelection" }
                });

            migrationBuilder.InsertData(
                table: "discount_sources",
                columns: new[] { "id", "name" },
                values: new object[,]
                {
                    { 1, "None" },
                    { 2, "Campaign" },
                    { 3, "Manual" },
                    { 4, "Employee" }
                });

            migrationBuilder.InsertData(
                table: "discount_types",
                columns: new[] { "id", "name" },
                values: new object[,]
                {
                    { 1, "FixedAmount" },
                    { 2, "Percentage" },
                    { 3, "FixedPrice" }
                });

            migrationBuilder.InsertData(
                table: "financial_movement_types",
                columns: new[] { "id", "name" },
                values: new object[,]
                {
                    { 1, "OwnerInvestment" },
                    { 2, "SupplierPayment" },
                    { 3, "SalePayment" },
                    { 4, "Expense" },
                    { 5, "OwnerWithdrawal" },
                    { 6, "SupplierRefund" },
                    { 7, "CustomerRefund" },
                    { 8, "LoanReceived" },
                    { 9, "LoanPayment" },
                    { 10, "WarehouseShippingPayment" },
                    { 11, "Adjustment" },
                    { 12, "DeliveryAgencyReconciliation" }
                });

            migrationBuilder.InsertData(
                table: "inventory_movement_types",
                columns: new[] { "id", "name" },
                values: new object[,]
                {
                    { 1, "PurchaseReceived" },
                    { 2, "Sale" },
                    { 3, "SaleCancelled" },
                    { 4, "CustomerReturn" },
                    { 5, "ExchangeReturn" },
                    { 6, "IssueOpened" },
                    { 7, "IssueReturnedToAvailable" },
                    { 8, "IssueRemovedFromInventory" },
                    { 9, "ReservationCreated" },
                    { 10, "ReservationReleased" },
                    { 11, "ReservationConvertedToSale" },
                    { 12, "Donation" },
                    { 13, "AdjustmentIncrease" },
                    { 14, "AdjustmentDecrease" },
                    { 15, "SelectionSent" },
                    { 16, "SelectionConvertedToSale" },
                    { 17, "SelectionReturned" },
                    { 18, "ExchangeReplacementReserved" },
                    { 19, "ExchangeReplacementDelivered" },
                    { 20, "ExchangeReplacementReservationReleased" },
                    { 21, "ExchangeReturnReceivedByAgency" },
                    { 22, "ExchangeReturnMissing" }
                });

            migrationBuilder.InsertData(
                table: "inventory_stock_buckets",
                columns: new[] { "id", "name" },
                values: new object[,]
                {
                    { 1, "External" },
                    { 2, "Available" },
                    { 3, "Reserved" },
                    { 4, "Unavailable" },
                    { 5, "OutOfInventory" }
                });

            migrationBuilder.InsertData(
                table: "movement_directions",
                columns: new[] { "id", "name" },
                values: new object[,]
                {
                    { 1, "In" },
                    { 2, "Out" }
                });

            migrationBuilder.InsertData(
                table: "order_statuses",
                columns: new[] { "id", "name" },
                values: new object[,]
                {
                    { 1, "Pending" },
                    { 2, "PartiallyReceived" },
                    { 3, "Received" },
                    { 4, "Cancelled" }
                });

            migrationBuilder.InsertData(
                table: "payment_methods",
                columns: new[] { "id", "name" },
                values: new object[,]
                {
                    { 1, "Cash" },
                    { 2, "Transfer" },
                    { 3, "Card" }
                });

            migrationBuilder.InsertData(
                table: "product_hold_statuses",
                columns: new[] { "id", "name" },
                values: new object[,]
                {
                    { 1, "Active" },
                    { 2, "ConvertedToSale" },
                    { 3, "NotSelected" },
                    { 4, "AwaitingReturn" }
                });

            migrationBuilder.InsertData(
                table: "product_inventory_issue_statuses",
                columns: new[] { "id", "name" },
                values: new object[,]
                {
                    { 1, "Open" },
                    { 2, "ResolvedToAvailable" },
                    { 3, "Discarded" },
                    { 4, "ConfirmedLost" },
                    { 5, "Cancelled" }
                });

            migrationBuilder.InsertData(
                table: "product_inventory_issue_types",
                columns: new[] { "id", "name" },
                values: new object[,]
                {
                    { 1, "Damaged" },
                    { 2, "Dirty" },
                    { 3, "Missing" },
                    { 4, "UnderReview" },
                    { 5, "Repairing" }
                });

            migrationBuilder.InsertData(
                table: "sale_channels",
                columns: new[] { "id", "enabled", "name" },
                values: new object[,]
                {
                    { 1, true, "InStoreSale" },
                    { 2, true, "Whatsapp" },
                    { 3, true, "Instagram" },
                    { 4, true, "Messenger" },
                    { 5, true, "Other" }
                });

            migrationBuilder.InsertData(
                table: "sale_payment_statuses",
                columns: new[] { "id", "name" },
                values: new object[,]
                {
                    { 1, "Unpaid" },
                    { 2, "PartiallyPaid" },
                    { 3, "Paid" },
                    { 4, "RefundPending" }
                });

            migrationBuilder.InsertData(
                table: "sale_product_statuses",
                columns: new[] { "id", "name" },
                values: new object[,]
                {
                    { 1, "Pending" },
                    { 2, "Completed" },
                    { 3, "Refunded" },
                    { 4, "Changed" },
                    { 5, "Cancelled" }
                });

            migrationBuilder.InsertData(
                table: "sale_statuses",
                columns: new[] { "id", "name" },
                values: new object[,]
                {
                    { 1, "Pending" },
                    { 2, "Reserved" },
                    { 3, "ReadyForDelivery" },
                    { 4, "SentForDelivery" },
                    { 5, "Completed" },
                    { 6, "Cancelled" }
                });

            migrationBuilder.CreateIndex(
                name: "ix_asp_net_role_claims_role_id",
                table: "AspNetRoleClaims",
                column: "role_id");

            migrationBuilder.CreateIndex(
                name: "RoleNameIndex",
                table: "AspNetRoles",
                column: "normalized_name",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_asp_net_user_claims_user_id",
                table: "AspNetUserClaims",
                column: "user_id");

            migrationBuilder.CreateIndex(
                name: "ix_asp_net_user_logins_user_id",
                table: "AspNetUserLogins",
                column: "user_id");

            migrationBuilder.CreateIndex(
                name: "ix_asp_net_user_roles_role_id",
                table: "AspNetUserRoles",
                column: "role_id");

            migrationBuilder.CreateIndex(
                name: "EmailIndex",
                table: "AspNetUsers",
                column: "normalized_email");

            migrationBuilder.CreateIndex(
                name: "UserNameIndex",
                table: "AspNetUsers",
                column: "normalized_user_name",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_categories_name",
                table: "categories",
                column: "name",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_clients_instagram_user",
                table: "clients",
                column: "instagram_user",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_clients_is_blocked",
                table: "clients",
                column: "is_blocked");

            migrationBuilder.CreateIndex(
                name: "ix_clients_messenger_user",
                table: "clients",
                column: "messenger_user",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_clients_phone_number",
                table: "clients",
                column: "phone_number",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_delivery_agencies_name",
                table: "delivery_agencies",
                column: "name",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_delivery_agency_reconciliations_delivery_agency_id_reconcil",
                table: "delivery_agency_reconciliations",
                columns: new[] { "delivery_agency_id", "reconciliation_date" });

            migrationBuilder.CreateIndex(
                name: "ix_delivery_agency_reconciliations_reconciliation_date",
                table: "delivery_agency_reconciliations",
                column: "reconciliation_date");

            migrationBuilder.CreateIndex(
                name: "ix_delivery_statuses_name",
                table: "delivery_statuses",
                column: "name",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_departments_name",
                table: "departments",
                column: "name",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_discount_campaign_products_discount_campaign_id_product_det",
                table: "discount_campaign_products",
                columns: new[] { "discount_campaign_id", "product_detail_id" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_discount_campaign_products_discount_type_id",
                table: "discount_campaign_products",
                column: "discount_type_id");

            migrationBuilder.CreateIndex(
                name: "ix_discount_campaign_products_product_detail_id",
                table: "discount_campaign_products",
                column: "product_detail_id");

            migrationBuilder.CreateIndex(
                name: "ix_discount_campaigns_enabled_start_date_end_date",
                table: "discount_campaigns",
                columns: new[] { "enabled", "start_date", "end_date" });

            migrationBuilder.CreateIndex(
                name: "ix_discount_sources_name",
                table: "discount_sources",
                column: "name",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_discount_types_name",
                table: "discount_types",
                column: "name",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_dollar_exchange_rates_start_date",
                table: "dollar_exchange_rates",
                column: "start_date",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_exchange_outbound_items_product_id",
                table: "exchange_outbound_items",
                column: "product_id");

            migrationBuilder.CreateIndex(
                name: "ix_exchange_outbound_items_sale_exchange_id",
                table: "exchange_outbound_items",
                column: "sale_exchange_id");

            migrationBuilder.CreateIndex(
                name: "ix_exchange_return_items_original_sale_product_id",
                table: "exchange_return_items",
                column: "original_sale_product_id");

            migrationBuilder.CreateIndex(
                name: "ix_exchange_return_items_product_id",
                table: "exchange_return_items",
                column: "product_id");

            migrationBuilder.CreateIndex(
                name: "ix_exchange_return_items_sale_exchange_id",
                table: "exchange_return_items",
                column: "sale_exchange_id");

            migrationBuilder.CreateIndex(
                name: "ix_expense_categories_enabled",
                table: "expense_categories",
                column: "enabled");

            migrationBuilder.CreateIndex(
                name: "ix_expense_categories_name",
                table: "expense_categories",
                column: "name",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_financial_movement_types_name",
                table: "financial_movement_types",
                column: "name",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_financial_movements_created_at",
                table: "financial_movements",
                column: "created_at");

            migrationBuilder.CreateIndex(
                name: "ix_financial_movements_delivery_agency_reconciliation_id",
                table: "financial_movements",
                column: "delivery_agency_reconciliation_id");

            migrationBuilder.CreateIndex(
                name: "ix_financial_movements_expense_category_id",
                table: "financial_movements",
                column: "expense_category_id");

            migrationBuilder.CreateIndex(
                name: "ix_financial_movements_financial_movement_type_id",
                table: "financial_movements",
                column: "financial_movement_type_id");

            migrationBuilder.CreateIndex(
                name: "ix_financial_movements_financial_movement_type_id_movement_date",
                table: "financial_movements",
                columns: new[] { "financial_movement_type_id", "movement_date" });

            migrationBuilder.CreateIndex(
                name: "ix_financial_movements_loan_id",
                table: "financial_movements",
                column: "loan_id");

            migrationBuilder.CreateIndex(
                name: "ix_financial_movements_loan_payment_id",
                table: "financial_movements",
                column: "loan_payment_id");

            migrationBuilder.CreateIndex(
                name: "ix_financial_movements_movement_date",
                table: "financial_movements",
                column: "movement_date");

            migrationBuilder.CreateIndex(
                name: "ix_financial_movements_movement_direction_id",
                table: "financial_movements",
                column: "movement_direction_id");

            migrationBuilder.CreateIndex(
                name: "ix_financial_movements_movement_direction_id_movement_date",
                table: "financial_movements",
                columns: new[] { "movement_direction_id", "movement_date" });

            migrationBuilder.CreateIndex(
                name: "ix_financial_movements_order_id",
                table: "financial_movements",
                column: "order_id");

            migrationBuilder.CreateIndex(
                name: "ix_financial_movements_product_receipt_id",
                table: "financial_movements",
                column: "product_receipt_id");

            migrationBuilder.CreateIndex(
                name: "ix_financial_movements_sale_payment_movement_id",
                table: "financial_movements",
                column: "sale_payment_movement_id",
                unique: true,
                filter: "sale_payment_movement_id IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "ix_financial_movements_sale_return_id",
                table: "financial_movements",
                column: "sale_return_id",
                unique: true,
                filter: "sale_return_id IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "ix_inventory_movement_types_name",
                table: "inventory_movement_types",
                column: "name",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_inventory_movements_created_at",
                table: "inventory_movements",
                column: "created_at");

            migrationBuilder.CreateIndex(
                name: "ix_inventory_movements_exchange_outbound_item_id",
                table: "inventory_movements",
                column: "exchange_outbound_item_id");

            migrationBuilder.CreateIndex(
                name: "ix_inventory_movements_exchange_return_item_id",
                table: "inventory_movements",
                column: "exchange_return_item_id");

            migrationBuilder.CreateIndex(
                name: "ix_inventory_movements_from_stock_bucket_id",
                table: "inventory_movements",
                column: "from_stock_bucket_id");

            migrationBuilder.CreateIndex(
                name: "ix_inventory_movements_inventory_movement_type_id",
                table: "inventory_movements",
                column: "inventory_movement_type_id");

            migrationBuilder.CreateIndex(
                name: "ix_inventory_movements_inventory_movement_type_id_movement_date",
                table: "inventory_movements",
                columns: new[] { "inventory_movement_type_id", "movement_date" });

            migrationBuilder.CreateIndex(
                name: "ix_inventory_movements_movement_date",
                table: "inventory_movements",
                column: "movement_date");

            migrationBuilder.CreateIndex(
                name: "ix_inventory_movements_order_id",
                table: "inventory_movements",
                column: "order_id");

            migrationBuilder.CreateIndex(
                name: "ix_inventory_movements_product_hold_id",
                table: "inventory_movements",
                column: "product_hold_id");

            migrationBuilder.CreateIndex(
                name: "ix_inventory_movements_product_id",
                table: "inventory_movements",
                column: "product_id");

            migrationBuilder.CreateIndex(
                name: "ix_inventory_movements_product_id_movement_date",
                table: "inventory_movements",
                columns: new[] { "product_id", "movement_date" });

            migrationBuilder.CreateIndex(
                name: "ix_inventory_movements_product_inventory_issue_id",
                table: "inventory_movements",
                column: "product_inventory_issue_id");

            migrationBuilder.CreateIndex(
                name: "ix_inventory_movements_sale_product_id",
                table: "inventory_movements",
                column: "sale_product_id");

            migrationBuilder.CreateIndex(
                name: "ix_inventory_movements_sale_return_item_id",
                table: "inventory_movements",
                column: "sale_return_item_id");

            migrationBuilder.CreateIndex(
                name: "ix_inventory_movements_to_stock_bucket_id",
                table: "inventory_movements",
                column: "to_stock_bucket_id");

            migrationBuilder.CreateIndex(
                name: "ix_inventory_stock_buckets_name",
                table: "inventory_stock_buckets",
                column: "name",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_loan_owners_name",
                table: "loan_owners",
                column: "name",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_loan_payments_created_at",
                table: "loan_payments",
                column: "created_at");

            migrationBuilder.CreateIndex(
                name: "ix_loan_payments_loan_id",
                table: "loan_payments",
                column: "loan_id");

            migrationBuilder.CreateIndex(
                name: "ix_loan_payments_loan_id_payment_date",
                table: "loan_payments",
                columns: new[] { "loan_id", "payment_date" });

            migrationBuilder.CreateIndex(
                name: "ix_loan_payments_payment_date",
                table: "loan_payments",
                column: "payment_date");

            migrationBuilder.CreateIndex(
                name: "ix_loans_created_at",
                table: "loans",
                column: "created_at");

            migrationBuilder.CreateIndex(
                name: "ix_loans_loan_date",
                table: "loans",
                column: "loan_date");

            migrationBuilder.CreateIndex(
                name: "ix_loans_loan_owner_id",
                table: "loans",
                column: "loan_owner_id");

            migrationBuilder.CreateIndex(
                name: "ix_loans_loan_owner_id_loan_date",
                table: "loans",
                columns: new[] { "loan_owner_id", "loan_date" });

            migrationBuilder.CreateIndex(
                name: "ix_movement_directions_name",
                table: "movement_directions",
                column: "name",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_municipalities_department_id_name",
                table: "municipalities",
                columns: new[] { "department_id", "name" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_order_statuses_name",
                table: "order_statuses",
                column: "name",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_order_tracking_numbers_order_id",
                table: "order_tracking_numbers",
                column: "order_id");

            migrationBuilder.CreateIndex(
                name: "ix_order_tracking_numbers_product_receipt_id",
                table: "order_tracking_numbers",
                column: "product_receipt_id");

            migrationBuilder.CreateIndex(
                name: "ix_order_tracking_numbers_shipping_company_id",
                table: "order_tracking_numbers",
                column: "shipping_company_id");

            migrationBuilder.CreateIndex(
                name: "ix_order_tracking_numbers_tracking_number",
                table: "order_tracking_numbers",
                column: "tracking_number",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_orders_created_at",
                table: "orders",
                column: "created_at");

            migrationBuilder.CreateIndex(
                name: "ix_orders_order_status_id_purchase_date",
                table: "orders",
                columns: new[] { "order_status_id", "purchase_date" });

            migrationBuilder.CreateIndex(
                name: "ix_orders_purchase_date",
                table: "orders",
                column: "purchase_date");

            migrationBuilder.CreateIndex(
                name: "ix_orders_supplier_id_purchase_date",
                table: "orders",
                columns: new[] { "supplier_id", "purchase_date" });

            migrationBuilder.CreateIndex(
                name: "ix_payment_methods_name",
                table: "payment_methods",
                column: "name",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_payment_terminals_enabled",
                table: "payment_terminals",
                column: "enabled");

            migrationBuilder.CreateIndex(
                name: "ix_payment_terminals_name",
                table: "payment_terminals",
                column: "name",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_product_details_code",
                table: "product_details",
                column: "code",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_product_details_subcategory_id",
                table: "product_details",
                column: "subcategory_id");

            migrationBuilder.CreateIndex(
                name: "ix_product_hold_statuses_name",
                table: "product_hold_statuses",
                column: "name",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_product_holds_product_hold_status_id",
                table: "product_holds",
                column: "product_hold_status_id");

            migrationBuilder.CreateIndex(
                name: "ix_product_holds_product_id",
                table: "product_holds",
                column: "product_id");

            migrationBuilder.CreateIndex(
                name: "ix_product_holds_sale_id",
                table: "product_holds",
                column: "sale_id");

            migrationBuilder.CreateIndex(
                name: "ix_product_images_product_detail_id",
                table: "product_images",
                column: "product_detail_id");

            migrationBuilder.CreateIndex(
                name: "ix_product_images_product_detail_id_is_primary",
                table: "product_images",
                columns: new[] { "product_detail_id", "is_primary" },
                unique: true,
                filter: "is_primary = true");

            migrationBuilder.CreateIndex(
                name: "ix_product_images_product_detail_id_sort_order",
                table: "product_images",
                columns: new[] { "product_detail_id", "sort_order" });

            migrationBuilder.CreateIndex(
                name: "ix_product_inventory_issue_statuses_name",
                table: "product_inventory_issue_statuses",
                column: "name",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_product_inventory_issue_types_name",
                table: "product_inventory_issue_types",
                column: "name",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_product_inventory_issues_created_at",
                table: "product_inventory_issues",
                column: "created_at");

            migrationBuilder.CreateIndex(
                name: "ix_product_inventory_issues_issue_date",
                table: "product_inventory_issues",
                column: "issue_date");

            migrationBuilder.CreateIndex(
                name: "ix_product_inventory_issues_product_id",
                table: "product_inventory_issues",
                column: "product_id");

            migrationBuilder.CreateIndex(
                name: "ix_product_inventory_issues_product_inventory_issue_status_id",
                table: "product_inventory_issues",
                column: "product_inventory_issue_status_id");

            migrationBuilder.CreateIndex(
                name: "ix_product_inventory_issues_product_inventory_issue_type_id",
                table: "product_inventory_issues",
                column: "product_inventory_issue_type_id");

            migrationBuilder.CreateIndex(
                name: "ix_product_receipt_details_product_id",
                table: "product_receipt_details",
                column: "product_id");

            migrationBuilder.CreateIndex(
                name: "ix_product_receipt_details_product_receipt_id",
                table: "product_receipt_details",
                column: "product_receipt_id");

            migrationBuilder.CreateIndex(
                name: "ix_product_receipts_created_at",
                table: "product_receipts",
                column: "created_at");

            migrationBuilder.CreateIndex(
                name: "ix_product_receipts_order_id",
                table: "product_receipts",
                column: "order_id");

            migrationBuilder.CreateIndex(
                name: "ix_product_receipts_order_id_received_date",
                table: "product_receipts",
                columns: new[] { "order_id", "received_date" });

            migrationBuilder.CreateIndex(
                name: "ix_product_receipts_received_date",
                table: "product_receipts",
                column: "received_date");

            migrationBuilder.CreateIndex(
                name: "ix_products_order_id",
                table: "products",
                column: "order_id");

            migrationBuilder.CreateIndex(
                name: "ix_products_product_detail_id_size_id_color",
                table: "products",
                columns: new[] { "product_detail_id", "size_id", "color" });

            migrationBuilder.CreateIndex(
                name: "ix_products_size_id",
                table: "products",
                column: "size_id");

            migrationBuilder.CreateIndex(
                name: "ix_sale_channels_name",
                table: "sale_channels",
                column: "name",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_sale_deliveries_client_id",
                table: "sale_deliveries",
                column: "client_id");

            migrationBuilder.CreateIndex(
                name: "ix_sale_deliveries_created_at",
                table: "sale_deliveries",
                column: "created_at");

            migrationBuilder.CreateIndex(
                name: "ix_sale_deliveries_delivery_agency_id",
                table: "sale_deliveries",
                column: "delivery_agency_id");

            migrationBuilder.CreateIndex(
                name: "ix_sale_deliveries_delivery_agency_reconciliation_id",
                table: "sale_deliveries",
                column: "delivery_agency_reconciliation_id",
                filter: "delivery_agency_reconciliation_id IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "ix_sale_deliveries_delivery_status_id",
                table: "sale_deliveries",
                column: "delivery_status_id");

            migrationBuilder.CreateIndex(
                name: "ix_sale_deliveries_municipality_id",
                table: "sale_deliveries",
                column: "municipality_id");

            migrationBuilder.CreateIndex(
                name: "ix_sale_deliveries_user_id_created_at",
                table: "sale_deliveries",
                columns: new[] { "user_id", "created_at" });

            migrationBuilder.CreateIndex(
                name: "ux_sale_deliveries_sale_id_active",
                table: "sale_deliveries",
                column: "sale_id",
                unique: true,
                filter: "delivery_status_id <> 2 AND delivery_status_id <> 3 AND delivery_status_id <> 5");

            migrationBuilder.CreateIndex(
                name: "ix_sale_exchanges_original_sale_id",
                table: "sale_exchanges",
                column: "original_sale_id");

            migrationBuilder.CreateIndex(
                name: "ix_sale_exchanges_original_sale_id_status_id",
                table: "sale_exchanges",
                columns: new[] { "original_sale_id", "status_id" });

            migrationBuilder.CreateIndex(
                name: "ix_sale_payment_movements_created_at",
                table: "sale_payment_movements",
                column: "created_at");

            migrationBuilder.CreateIndex(
                name: "ix_sale_payment_movements_delivery_agency_reconciliation_id",
                table: "sale_payment_movements",
                column: "delivery_agency_reconciliation_id");

            migrationBuilder.CreateIndex(
                name: "ix_sale_payment_movements_movement_date",
                table: "sale_payment_movements",
                column: "movement_date");

            migrationBuilder.CreateIndex(
                name: "ix_sale_payment_movements_movement_direction_id",
                table: "sale_payment_movements",
                column: "movement_direction_id");

            migrationBuilder.CreateIndex(
                name: "ix_sale_payment_movements_payment_method_id",
                table: "sale_payment_movements",
                column: "payment_method_id");

            migrationBuilder.CreateIndex(
                name: "ix_sale_payment_movements_payment_terminal_id",
                table: "sale_payment_movements",
                column: "payment_terminal_id");

            migrationBuilder.CreateIndex(
                name: "ix_sale_payment_movements_reversed_sale_payment_movement_id",
                table: "sale_payment_movements",
                column: "reversed_sale_payment_movement_id",
                unique: true,
                filter: "movement_direction_id = 2 AND payment_method_id = 3");

            migrationBuilder.CreateIndex(
                name: "ix_sale_payment_movements_sale_delivery_id",
                table: "sale_payment_movements",
                column: "sale_delivery_id");

            migrationBuilder.CreateIndex(
                name: "ix_sale_payment_movements_sale_id_movement_date",
                table: "sale_payment_movements",
                columns: new[] { "sale_id", "movement_date" });

            migrationBuilder.CreateIndex(
                name: "ix_sale_payment_movements_user_id_movement_date",
                table: "sale_payment_movements",
                columns: new[] { "user_id", "movement_date" });

            migrationBuilder.CreateIndex(
                name: "ix_sale_payment_statuses_name",
                table: "sale_payment_statuses",
                column: "name",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_sale_product_statuses_name",
                table: "sale_product_statuses",
                column: "name",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_sale_products_discount_campaign_id",
                table: "sale_products",
                column: "discount_campaign_id");

            migrationBuilder.CreateIndex(
                name: "ix_sale_products_discount_source_id",
                table: "sale_products",
                column: "discount_source_id");

            migrationBuilder.CreateIndex(
                name: "ix_sale_products_product_id",
                table: "sale_products",
                column: "product_id");

            migrationBuilder.CreateIndex(
                name: "ix_sale_products_sale_id",
                table: "sale_products",
                column: "sale_id");

            migrationBuilder.CreateIndex(
                name: "ix_sale_products_sale_id_product_id",
                table: "sale_products",
                columns: new[] { "sale_id", "product_id" });

            migrationBuilder.CreateIndex(
                name: "ix_sale_products_sale_id_sale_product_status_id",
                table: "sale_products",
                columns: new[] { "sale_id", "sale_product_status_id" });

            migrationBuilder.CreateIndex(
                name: "ix_sale_products_sale_product_status_id",
                table: "sale_products",
                column: "sale_product_status_id");

            migrationBuilder.CreateIndex(
                name: "ix_sale_return_items_original_sale_product_id",
                table: "sale_return_items",
                column: "original_sale_product_id");

            migrationBuilder.CreateIndex(
                name: "ix_sale_return_items_product_id",
                table: "sale_return_items",
                column: "product_id");

            migrationBuilder.CreateIndex(
                name: "ix_sale_return_items_product_inventory_issue_id",
                table: "sale_return_items",
                column: "product_inventory_issue_id");

            migrationBuilder.CreateIndex(
                name: "ix_sale_return_items_sale_return_id",
                table: "sale_return_items",
                column: "sale_return_id");

            migrationBuilder.CreateIndex(
                name: "ix_sale_returns_delivery_agency_id",
                table: "sale_returns",
                column: "delivery_agency_id");

            migrationBuilder.CreateIndex(
                name: "ix_sale_returns_delivery_agency_reconciliation_id",
                table: "sale_returns",
                column: "delivery_agency_reconciliation_id");

            migrationBuilder.CreateIndex(
                name: "ix_sale_returns_original_sale_id",
                table: "sale_returns",
                column: "original_sale_id");

            migrationBuilder.CreateIndex(
                name: "ix_sale_returns_original_sale_id_status_id",
                table: "sale_returns",
                columns: new[] { "original_sale_id", "status_id" });

            migrationBuilder.CreateIndex(
                name: "ix_sale_returns_refund_payment_method_id",
                table: "sale_returns",
                column: "refund_payment_method_id");

            migrationBuilder.CreateIndex(
                name: "ix_sale_statuses_name",
                table: "sale_statuses",
                column: "name",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_sales_client_id",
                table: "sales",
                column: "client_id");

            migrationBuilder.CreateIndex(
                name: "ix_sales_created_at",
                table: "sales",
                column: "created_at");

            migrationBuilder.CreateIndex(
                name: "ix_sales_sale_channel_id_sale_date",
                table: "sales",
                columns: new[] { "sale_channel_id", "sale_date" });

            migrationBuilder.CreateIndex(
                name: "ix_sales_sale_date",
                table: "sales",
                column: "sale_date");

            migrationBuilder.CreateIndex(
                name: "ix_sales_sale_payment_status_id_sale_date",
                table: "sales",
                columns: new[] { "sale_payment_status_id", "sale_date" });

            migrationBuilder.CreateIndex(
                name: "ix_sales_sale_status_id_sale_date",
                table: "sales",
                columns: new[] { "sale_status_id", "sale_date" });

            migrationBuilder.CreateIndex(
                name: "ix_sales_user_id_sale_date",
                table: "sales",
                columns: new[] { "user_id", "sale_date" });

            migrationBuilder.CreateIndex(
                name: "ix_shipping_companies_name",
                table: "shipping_companies",
                column: "name",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_size_groups_name",
                table: "size_groups",
                column: "name",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_sizes_size_group_id_name",
                table: "sizes",
                columns: new[] { "size_group_id", "name" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_subcategories_category_id",
                table: "subcategories",
                column: "category_id");

            migrationBuilder.CreateIndex(
                name: "ix_subcategories_name",
                table: "subcategories",
                column: "name",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_suppliers_name",
                table: "suppliers",
                column: "name",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "AspNetRoleClaims");

            migrationBuilder.DropTable(
                name: "AspNetUserClaims");

            migrationBuilder.DropTable(
                name: "AspNetUserLogins");

            migrationBuilder.DropTable(
                name: "AspNetUserRoles");

            migrationBuilder.DropTable(
                name: "AspNetUserTokens");

            migrationBuilder.DropTable(
                name: "discount_campaign_products");

            migrationBuilder.DropTable(
                name: "dollar_exchange_rates");

            migrationBuilder.DropTable(
                name: "financial_movements");

            migrationBuilder.DropTable(
                name: "inventory_movements");

            migrationBuilder.DropTable(
                name: "order_tracking_numbers");

            migrationBuilder.DropTable(
                name: "product_images");

            migrationBuilder.DropTable(
                name: "product_receipt_details");

            migrationBuilder.DropTable(
                name: "AspNetRoles");

            migrationBuilder.DropTable(
                name: "AspNetUsers");

            migrationBuilder.DropTable(
                name: "discount_types");

            migrationBuilder.DropTable(
                name: "expense_categories");

            migrationBuilder.DropTable(
                name: "financial_movement_types");

            migrationBuilder.DropTable(
                name: "loan_payments");

            migrationBuilder.DropTable(
                name: "sale_payment_movements");

            migrationBuilder.DropTable(
                name: "exchange_outbound_items");

            migrationBuilder.DropTable(
                name: "exchange_return_items");

            migrationBuilder.DropTable(
                name: "inventory_movement_types");

            migrationBuilder.DropTable(
                name: "inventory_stock_buckets");

            migrationBuilder.DropTable(
                name: "product_holds");

            migrationBuilder.DropTable(
                name: "sale_return_items");

            migrationBuilder.DropTable(
                name: "shipping_companies");

            migrationBuilder.DropTable(
                name: "product_receipts");

            migrationBuilder.DropTable(
                name: "loans");

            migrationBuilder.DropTable(
                name: "movement_directions");

            migrationBuilder.DropTable(
                name: "payment_terminals");

            migrationBuilder.DropTable(
                name: "sale_deliveries");

            migrationBuilder.DropTable(
                name: "sale_exchanges");

            migrationBuilder.DropTable(
                name: "product_hold_statuses");

            migrationBuilder.DropTable(
                name: "product_inventory_issues");

            migrationBuilder.DropTable(
                name: "sale_products");

            migrationBuilder.DropTable(
                name: "sale_returns");

            migrationBuilder.DropTable(
                name: "loan_owners");

            migrationBuilder.DropTable(
                name: "delivery_statuses");

            migrationBuilder.DropTable(
                name: "municipalities");

            migrationBuilder.DropTable(
                name: "product_inventory_issue_statuses");

            migrationBuilder.DropTable(
                name: "product_inventory_issue_types");

            migrationBuilder.DropTable(
                name: "discount_campaigns");

            migrationBuilder.DropTable(
                name: "discount_sources");

            migrationBuilder.DropTable(
                name: "products");

            migrationBuilder.DropTable(
                name: "sale_product_statuses");

            migrationBuilder.DropTable(
                name: "delivery_agency_reconciliations");

            migrationBuilder.DropTable(
                name: "payment_methods");

            migrationBuilder.DropTable(
                name: "sales");

            migrationBuilder.DropTable(
                name: "departments");

            migrationBuilder.DropTable(
                name: "orders");

            migrationBuilder.DropTable(
                name: "product_details");

            migrationBuilder.DropTable(
                name: "sizes");

            migrationBuilder.DropTable(
                name: "delivery_agencies");

            migrationBuilder.DropTable(
                name: "clients");

            migrationBuilder.DropTable(
                name: "sale_channels");

            migrationBuilder.DropTable(
                name: "sale_payment_statuses");

            migrationBuilder.DropTable(
                name: "sale_statuses");

            migrationBuilder.DropTable(
                name: "order_statuses");

            migrationBuilder.DropTable(
                name: "suppliers");

            migrationBuilder.DropTable(
                name: "subcategories");

            migrationBuilder.DropTable(
                name: "size_groups");

            migrationBuilder.DropTable(
                name: "categories");
        }
    }
}
