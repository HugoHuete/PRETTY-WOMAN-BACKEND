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
                    lasname = table.Column<string>(type: "text", nullable: false),
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
                    address = table.Column<string>(type: "character varying(350)", maxLength: 350, nullable: true),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    last_purchase_date = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
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
                    enabled = table.Column<bool>(type: "boolean", nullable: false)
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
                    enabled = table.Column<bool>(type: "boolean", nullable: false)
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
                name: "product_receipts",
                columns: table => new
                {
                    id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    received_date = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_product_receipts", x => x.id);
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
                name: "sizes",
                columns: table => new
                {
                    id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    name = table.Column<string>(type: "character varying(10)", maxLength: 10, nullable: false),
                    display_order = table.Column<int>(type: "integer", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_sizes", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "suppliers",
                columns: table => new
                {
                    id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    name = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    url = table.Column<string>(type: "character varying(300)", maxLength: 300, nullable: true)
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
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    loan_owner_id = table.Column<int>(type: "integer", nullable: false),
                    initial_amount = table.Column<decimal>(type: "numeric(12,2)", precision: 12, scale: 2, nullable: false),
                    initial_amount_usd = table.Column<decimal>(type: "numeric(12,2)", precision: 12, scale: 2, nullable: false),
                    balance = table.Column<decimal>(type: "numeric(12,2)", precision: 12, scale: 2, nullable: false),
                    closed_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    comments = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    exchange_rate = table.Column<decimal>(type: "numeric(10,4)", precision: 10, scale: 4, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_loans", x => x.id);
                    table.CheckConstraint("ck_loans_balance_non_negative", "balance >= 0");
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
                name: "orders",
                columns: table => new
                {
                    id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    order_status_id = table.Column<int>(type: "integer", nullable: false),
                    supplier_id = table.Column<int>(type: "integer", nullable: false),
                    amount = table.Column<decimal>(type: "numeric(12,2)", precision: 12, scale: 2, nullable: false),
                    amount_usd = table.Column<decimal>(type: "numeric(12,2)", precision: 12, scale: 2, nullable: false),
                    received_amount = table.Column<decimal>(type: "numeric(12,2)", precision: 12, scale: 2, nullable: false),
                    total_shipping_cost = table.Column<decimal>(type: "numeric(12,2)", precision: 12, scale: 2, nullable: false),
                    comments = table.Column<string>(type: "character varying(300)", maxLength: 300, nullable: true),
                    exchange_rate = table.Column<decimal>(type: "numeric(10,4)", precision: 10, scale: 4, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_orders", x => x.id);
                    table.CheckConstraint("ck_order_amount_non_negative", "amount >= 0");
                    table.CheckConstraint("ck_order_amount_usd_non_negative", "amount_usd >= 0");
                    table.CheckConstraint("ck_order_exchange_rate_non_negative", "exchange_rate >= 0");
                    table.CheckConstraint("ck_order_received_amount_non_negative", "received_amount >= 0");
                    table.CheckConstraint("ck_order_total_shipping_cost_non_negative", "total_shipping_cost >= 0");
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
                    code = table.Column<string>(type: "character varying(10)", maxLength: 10, nullable: false),
                    name = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
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
                name: "sales",
                columns: table => new
                {
                    id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    sale_channel_id = table.Column<int>(type: "integer", nullable: false),
                    sale_status_id = table.Column<int>(type: "integer", nullable: false),
                    user_id = table.Column<string>(type: "text", nullable: false),
                    subtotal_before_discount = table.Column<decimal>(type: "numeric(12,2)", precision: 12, scale: 2, nullable: false),
                    total_discount = table.Column<decimal>(type: "numeric(12,2)", precision: 12, scale: 2, nullable: false),
                    sub_total = table.Column<decimal>(type: "numeric(12,2)", precision: 12, scale: 2, nullable: false),
                    comission = table.Column<decimal>(type: "numeric(12,2)", precision: 12, scale: 2, nullable: false),
                    total = table.Column<decimal>(type: "numeric(12,2)", precision: 12, scale: 2, nullable: false),
                    comments = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    client_id = table.Column<int>(type: "integer", nullable: true),
                    municipality_id = table.Column<int>(type: "integer", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_sales", x => x.id);
                    table.CheckConstraint("ck_sales_comission_non_negative", "comission >= 0");
                    table.CheckConstraint("ck_sales_subtotal_before_discount_non_negative", "subtotal_before_discount >= 0");
                    table.CheckConstraint("ck_sales_subtotal_matches_components", "sub_total = subtotal_before_discount - total_discount");
                    table.CheckConstraint("ck_sales_subtotal_non_negative", "sub_total >= 0");
                    table.CheckConstraint("ck_sales_total_discount_non_negative", "total_discount >= 0");
                    table.CheckConstraint("ck_sales_total_discount_not_greater_than_subtotal_before_disco~", "total_discount <= subtotal_before_discount");
                    table.CheckConstraint("ck_sales_total_matches_components", "total = sub_total - comission");
                    table.CheckConstraint("ck_sales_total_non_negative", "total >= 0");
                    table.ForeignKey(
                        name: "fk_sales_clients_client_id",
                        column: x => x.client_id,
                        principalTable: "clients",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "fk_sales_municipalities_municipality_id",
                        column: x => x.municipality_id,
                        principalTable: "municipalities",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "fk_sales_sale_channels_sale_channel_id",
                        column: x => x.sale_channel_id,
                        principalTable: "sale_channels",
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
                    product_receipt_id = table.Column<int>(type: "integer", nullable: false),
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
                    unit_cost = table.Column<decimal>(type: "numeric(12,2)", precision: 12, scale: 2, nullable: false),
                    unit_cost_with_shipping = table.Column<decimal>(type: "numeric(12,2)", precision: 12, scale: 2, nullable: false),
                    sale_price = table.Column<decimal>(type: "numeric(12,2)", precision: 12, scale: 2, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_products", x => x.id);
                    table.CheckConstraint("ck_products_available_quantity_non_negative", "available_quantity >= 0");
                    table.CheckConstraint("ck_products_cost_non_negative", "unit_cost >= 0");
                    table.CheckConstraint("ck_products_quantity_non_negative", "quantity >= 0");
                    table.CheckConstraint("ck_products_received_quantity_non_negative", "received_quantity >= 0");
                    table.CheckConstraint("ck_products_reserved_quantity_non_negative", "reserved_quantity >= 0");
                    table.CheckConstraint("ck_products_sale_price_non_negative", "sale_price >= 0");
                    table.CheckConstraint("ck_products_unit_cost_with_shipping_non_negative", "unit_cost_with_shipping >= 0");
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
                    amount_transferred_nio = table.Column<decimal>(type: "numeric(12,2)", precision: 12, scale: 2, nullable: false),
                    amount_transferred_usd = table.Column<decimal>(type: "numeric(12,2)", precision: 12, scale: 2, nullable: false),
                    change_amount = table.Column<decimal>(type: "numeric(12,2)", precision: 12, scale: 2, nullable: false),
                    exchange_rate = table.Column<decimal>(type: "numeric(10,4)", precision: 10, scale: 4, nullable: true),
                    shipping_charged_to_client = table.Column<decimal>(type: "numeric(12,2)", precision: 12, scale: 2, nullable: false),
                    shipping_paid_to_agency = table.Column<decimal>(type: "numeric(12,2)", precision: 12, scale: 2, nullable: false),
                    user_id = table.Column<string>(type: "text", nullable: false),
                    comments = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_sale_deliveries", x => x.id);
                    table.CheckConstraint("ck_sale_deliveries_amount_collected_nio_non_negative", "amount_collected_nio >= 0");
                    table.CheckConstraint("ck_sale_deliveries_amount_collected_usd_non_negative", "amount_collected_usd >= 0");
                    table.CheckConstraint("ck_sale_deliveries_amount_to_collect_non_negative", "amount_to_collect >= 0");
                    table.CheckConstraint("ck_sale_deliveries_amount_transferred_nio_non_negative", "amount_transferred_nio >= 0");
                    table.CheckConstraint("ck_sale_deliveries_amount_transferred_usd_non_negative", "amount_transferred_usd >= 0");
                    table.CheckConstraint("ck_sale_deliveries_change_amount_non_negative", "change_amount >= 0");
                    table.CheckConstraint("ck_sale_deliveries_exchange_rate_required_for_usd", "(\n                    (amount_collected_usd = 0 AND amount_transferred_usd = 0 AND exchange_rate IS NULL)\n                    OR\n                    ((amount_collected_usd > 0 OR amount_transferred_usd > 0) AND exchange_rate > 0)\n                )");
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
                name: "sale_payments",
                columns: table => new
                {
                    id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    sale_id = table.Column<int>(type: "integer", nullable: false),
                    payment_method_id = table.Column<int>(type: "integer", nullable: false),
                    payment_terminal_id = table.Column<int>(type: "integer", nullable: true),
                    amount = table.Column<decimal>(type: "numeric(12,2)", precision: 12, scale: 2, nullable: false),
                    comission_amount = table.Column<decimal>(type: "numeric(12,2)", precision: 12, scale: 2, nullable: false),
                    net_received_amount = table.Column<decimal>(type: "numeric(12,2)", precision: 12, scale: 2, nullable: false),
                    user_id = table.Column<string>(type: "text", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_sale_payments", x => x.id);
                    table.CheckConstraint("ck_sale_payments_amount_positive", "amount > 0");
                    table.CheckConstraint("ck_sale_payments_comission_amount_non_negative", "comission_amount >= 0");
                    table.CheckConstraint("ck_sale_payments_comission_not_greater_than_amount", "comission_amount <= amount");
                    table.CheckConstraint("ck_sale_payments_net_received_amount_matches_components", "net_received_amount = amount - comission_amount");
                    table.CheckConstraint("ck_sale_payments_net_received_amount_non_negative", "net_received_amount >= 0");
                    table.ForeignKey(
                        name: "fk_sale_payments_payment_methods_payment_method_id",
                        column: x => x.payment_method_id,
                        principalTable: "payment_methods",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "fk_sale_payments_payment_terminals_payment_terminal_id",
                        column: x => x.payment_terminal_id,
                        principalTable: "payment_terminals",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "fk_sale_payments_sales_sale_id",
                        column: x => x.sale_id,
                        principalTable: "sales",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "discount_campaign_products",
                columns: table => new
                {
                    id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    product_id = table.Column<int>(type: "integer", nullable: false),
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
                        name: "fk_discount_campaign_products_products_product_id",
                        column: x => x.product_id,
                        principalTable: "products",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
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
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    resolved_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    hold_reason = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    comments = table.Column<string>(type: "character varying(300)", maxLength: 300, nullable: true),
                    product_hold_status_id = table.Column<int>(type: "integer", nullable: false)
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
                    cost_at_sale = table.Column<decimal>(type: "numeric(12,2)", precision: 12, scale: 2, nullable: false),
                    original_sale_price = table.Column<decimal>(type: "numeric(12,2)", precision: 12, scale: 2, nullable: false),
                    discount_source_id = table.Column<int>(type: "integer", nullable: false),
                    discount_campaign_id = table.Column<int>(type: "integer", nullable: true),
                    discount_amount = table.Column<decimal>(type: "numeric(12,2)", precision: 12, scale: 2, nullable: false),
                    final_sale_price = table.Column<decimal>(type: "numeric(12,2)", precision: 12, scale: 2, nullable: false),
                    payment_comission = table.Column<decimal>(type: "numeric(12,2)", precision: 12, scale: 2, nullable: false),
                    gross_profit = table.Column<decimal>(type: "numeric(12,2)", precision: 12, scale: 2, nullable: false),
                    sale_product_status_id = table.Column<int>(type: "integer", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_sale_products", x => x.id);
                    table.CheckConstraint("ck_sale_products_cost_at_sale_non_negative", "cost_at_sale >= 0");
                    table.CheckConstraint("ck_sale_products_discount_amount_non_negative", "discount_amount >= 0");
                    table.CheckConstraint("ck_sale_products_discount_amount_not_greater_than_original_sal~", "discount_amount <= original_sale_price");
                    table.CheckConstraint("ck_sale_products_final_sale_price_non_negative", "final_sale_price >= 0");
                    table.CheckConstraint("ck_sale_products_gross_profit_matches_components", "gross_profit = final_sale_price - payment_comission - cost_at_sale");
                    table.CheckConstraint("ck_sale_products_original_sale_price_non_negative", "original_sale_price >= 0");
                    table.CheckConstraint("ck_sale_products_payment_comission_non_negative", "payment_comission >= 0");
                    table.CheckConstraint("ck_sale_products_quantity_positive", "quantity > 0");
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
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    movement_direction_id = table.Column<int>(type: "integer", nullable: false),
                    financial_movement_type_id = table.Column<int>(type: "integer", nullable: false),
                    expense_category_id = table.Column<int>(type: "integer", nullable: true),
                    order_id = table.Column<int>(type: "integer", nullable: true),
                    sale_payment_id = table.Column<int>(type: "integer", nullable: true),
                    loan_id = table.Column<int>(type: "integer", nullable: true),
                    amount = table.Column<decimal>(type: "numeric(12,2)", precision: 12, scale: 2, nullable: false),
                    exchange_rate = table.Column<decimal>(type: "numeric(10,4)", precision: 10, scale: 4, nullable: false),
                    comments = table.Column<string>(type: "character varying(300)", maxLength: 300, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_financial_movements", x => x.id);
                    table.CheckConstraint("ck_financial_movements_amount_positive", "amount > 0");
                    table.CheckConstraint("ck_financial_movements_exchange_rate_positive", "exchange_rate > 0");
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
                        name: "fk_financial_movements_sale_payments_sale_payment_id",
                        column: x => x.sale_payment_id,
                        principalTable: "sale_payments",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "inventory_movements",
                columns: table => new
                {
                    id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    product_id = table.Column<int>(type: "integer", nullable: false),
                    movement_direction_id = table.Column<int>(type: "integer", nullable: false),
                    inventory_movement_type_id = table.Column<int>(type: "integer", nullable: false),
                    quantity = table.Column<int>(type: "integer", nullable: false),
                    order_id = table.Column<int>(type: "integer", nullable: true),
                    sale_product_id = table.Column<int>(type: "integer", nullable: true),
                    product_hold_id = table.Column<int>(type: "integer", nullable: true),
                    comments = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_inventory_movements", x => x.id);
                    table.CheckConstraint("ck_inventory_movements_quantity_positive", "quantity > 0");
                    table.ForeignKey(
                        name: "fk_inventory_movements_inventory_movement_types_inventory_move",
                        column: x => x.inventory_movement_type_id,
                        principalTable: "inventory_movement_types",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "fk_inventory_movements_movement_directions_movement_direction_",
                        column: x => x.movement_direction_id,
                        principalTable: "movement_directions",
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
                });

            migrationBuilder.InsertData(
                table: "delivery_statuses",
                columns: new[] { "id", "name" },
                values: new object[,]
                {
                    { 1, "Pending" },
                    { 2, "Completed" },
                    { 3, "Cancelled" }
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
                    { 10, "Adjustment" }
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
                    { 6, "Damaged" },
                    { 7, "Repaired" },
                    { 8, "Lost" },
                    { 9, "Found" },
                    { 10, "Discarded" },
                    { 11, "Donation" },
                    { 12, "AdjustmentIncrease" },
                    { 13, "AdjustmentDecrease" },
                    { 14, "ReservationCreated" },
                    { 15, "ReservationReleased" },
                    { 16, "ReservationConvertedToSale" }
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
                    { 4, "Found" },
                    { 5, "Repaired" },
                    { 6, "Discarded" }
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
                    { 2, "Completed" },
                    { 3, "Cancelled" }
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
                column: "instagram_user");

            migrationBuilder.CreateIndex(
                name: "ix_clients_is_blocked",
                table: "clients",
                column: "is_blocked");

            migrationBuilder.CreateIndex(
                name: "ix_clients_phone_number",
                table: "clients",
                column: "phone_number");

            migrationBuilder.CreateIndex(
                name: "ix_delivery_agencies_name",
                table: "delivery_agencies",
                column: "name",
                unique: true);

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
                name: "ix_discount_campaign_products_discount_campaign_id_product_id",
                table: "discount_campaign_products",
                columns: new[] { "discount_campaign_id", "product_id" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_discount_campaign_products_discount_type_id",
                table: "discount_campaign_products",
                column: "discount_type_id");

            migrationBuilder.CreateIndex(
                name: "ix_discount_campaign_products_product_id",
                table: "discount_campaign_products",
                column: "product_id");

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
                name: "ix_financial_movements_expense_category_id",
                table: "financial_movements",
                column: "expense_category_id");

            migrationBuilder.CreateIndex(
                name: "ix_financial_movements_financial_movement_type_id",
                table: "financial_movements",
                column: "financial_movement_type_id");

            migrationBuilder.CreateIndex(
                name: "ix_financial_movements_financial_movement_type_id_created_at",
                table: "financial_movements",
                columns: new[] { "financial_movement_type_id", "created_at" });

            migrationBuilder.CreateIndex(
                name: "ix_financial_movements_loan_id",
                table: "financial_movements",
                column: "loan_id");

            migrationBuilder.CreateIndex(
                name: "ix_financial_movements_movement_direction_id",
                table: "financial_movements",
                column: "movement_direction_id");

            migrationBuilder.CreateIndex(
                name: "ix_financial_movements_movement_direction_id_created_at",
                table: "financial_movements",
                columns: new[] { "movement_direction_id", "created_at" });

            migrationBuilder.CreateIndex(
                name: "ix_financial_movements_order_id",
                table: "financial_movements",
                column: "order_id");

            migrationBuilder.CreateIndex(
                name: "ix_financial_movements_sale_payment_id",
                table: "financial_movements",
                column: "sale_payment_id");

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
                name: "ix_inventory_movements_inventory_movement_type_id",
                table: "inventory_movements",
                column: "inventory_movement_type_id");

            migrationBuilder.CreateIndex(
                name: "ix_inventory_movements_inventory_movement_type_id_created_at",
                table: "inventory_movements",
                columns: new[] { "inventory_movement_type_id", "created_at" });

            migrationBuilder.CreateIndex(
                name: "ix_inventory_movements_movement_direction_id",
                table: "inventory_movements",
                column: "movement_direction_id");

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
                name: "ix_inventory_movements_product_id_created_at",
                table: "inventory_movements",
                columns: new[] { "product_id", "created_at" });

            migrationBuilder.CreateIndex(
                name: "ix_inventory_movements_sale_product_id",
                table: "inventory_movements",
                column: "sale_product_id");

            migrationBuilder.CreateIndex(
                name: "ix_loan_owners_name",
                table: "loan_owners",
                column: "name",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_loans_created_at",
                table: "loans",
                column: "created_at");

            migrationBuilder.CreateIndex(
                name: "ix_loans_loan_owner_id",
                table: "loans",
                column: "loan_owner_id");

            migrationBuilder.CreateIndex(
                name: "ix_loans_loan_owner_id_created_at",
                table: "loans",
                columns: new[] { "loan_owner_id", "created_at" });

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
                name: "ix_orders_order_status_id",
                table: "orders",
                column: "order_status_id");

            migrationBuilder.CreateIndex(
                name: "ix_orders_supplier_id",
                table: "orders",
                column: "supplier_id");

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
                name: "ix_product_receipt_details_product_id",
                table: "product_receipt_details",
                column: "product_id");

            migrationBuilder.CreateIndex(
                name: "ix_product_receipt_details_product_receipt_id",
                table: "product_receipt_details",
                column: "product_receipt_id");

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
                name: "ix_sale_deliveries_delivery_status_id",
                table: "sale_deliveries",
                column: "delivery_status_id");

            migrationBuilder.CreateIndex(
                name: "ix_sale_deliveries_municipality_id",
                table: "sale_deliveries",
                column: "municipality_id");

            migrationBuilder.CreateIndex(
                name: "ix_sale_deliveries_sale_id",
                table: "sale_deliveries",
                column: "sale_id");

            migrationBuilder.CreateIndex(
                name: "ix_sale_deliveries_user_id_created_at",
                table: "sale_deliveries",
                columns: new[] { "user_id", "created_at" });

            migrationBuilder.CreateIndex(
                name: "ix_sale_payments_created_at",
                table: "sale_payments",
                column: "created_at");

            migrationBuilder.CreateIndex(
                name: "ix_sale_payments_payment_method_id",
                table: "sale_payments",
                column: "payment_method_id");

            migrationBuilder.CreateIndex(
                name: "ix_sale_payments_payment_terminal_id",
                table: "sale_payments",
                column: "payment_terminal_id");

            migrationBuilder.CreateIndex(
                name: "ix_sale_payments_sale_id_created_at",
                table: "sale_payments",
                columns: new[] { "sale_id", "created_at" });

            migrationBuilder.CreateIndex(
                name: "ix_sale_payments_user_id_created_at",
                table: "sale_payments",
                columns: new[] { "user_id", "created_at" });

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
                name: "ix_sales_municipality_id",
                table: "sales",
                column: "municipality_id");

            migrationBuilder.CreateIndex(
                name: "ix_sales_sale_channel_id_created_at",
                table: "sales",
                columns: new[] { "sale_channel_id", "created_at" });

            migrationBuilder.CreateIndex(
                name: "ix_sales_sale_status_id_created_at",
                table: "sales",
                columns: new[] { "sale_status_id", "created_at" });

            migrationBuilder.CreateIndex(
                name: "ix_sales_user_id_created_at",
                table: "sales",
                columns: new[] { "user_id", "created_at" });

            migrationBuilder.CreateIndex(
                name: "ix_shipping_companies_name",
                table: "shipping_companies",
                column: "name",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_sizes_name",
                table: "sizes",
                column: "name",
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
                name: "sale_deliveries");

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
                name: "loans");

            migrationBuilder.DropTable(
                name: "sale_payments");

            migrationBuilder.DropTable(
                name: "inventory_movement_types");

            migrationBuilder.DropTable(
                name: "movement_directions");

            migrationBuilder.DropTable(
                name: "product_holds");

            migrationBuilder.DropTable(
                name: "sale_products");

            migrationBuilder.DropTable(
                name: "shipping_companies");

            migrationBuilder.DropTable(
                name: "product_receipts");

            migrationBuilder.DropTable(
                name: "delivery_agencies");

            migrationBuilder.DropTable(
                name: "delivery_statuses");

            migrationBuilder.DropTable(
                name: "loan_owners");

            migrationBuilder.DropTable(
                name: "payment_methods");

            migrationBuilder.DropTable(
                name: "payment_terminals");

            migrationBuilder.DropTable(
                name: "product_hold_statuses");

            migrationBuilder.DropTable(
                name: "discount_campaigns");

            migrationBuilder.DropTable(
                name: "discount_sources");

            migrationBuilder.DropTable(
                name: "products");

            migrationBuilder.DropTable(
                name: "sale_product_statuses");

            migrationBuilder.DropTable(
                name: "sales");

            migrationBuilder.DropTable(
                name: "orders");

            migrationBuilder.DropTable(
                name: "product_details");

            migrationBuilder.DropTable(
                name: "sizes");

            migrationBuilder.DropTable(
                name: "clients");

            migrationBuilder.DropTable(
                name: "municipalities");

            migrationBuilder.DropTable(
                name: "sale_channels");

            migrationBuilder.DropTable(
                name: "sale_statuses");

            migrationBuilder.DropTable(
                name: "order_statuses");

            migrationBuilder.DropTable(
                name: "suppliers");

            migrationBuilder.DropTable(
                name: "subcategories");

            migrationBuilder.DropTable(
                name: "departments");

            migrationBuilder.DropTable(
                name: "categories");
        }
    }
}
