# Discount Campaign Listing Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Convert the discount campaign listing endpoint to a paginated summary response while keeping products available only from the detail endpoint.

**Architecture:** Add a query DTO and a product-free summary DTO. The service will count and page a filtered, stable-ordered EF query, while `GetByIdAsync` will retain the existing full DTO projection. The controller and frontend handoff will expose the new typed contract.

**Tech Stack:** .NET 10, ASP.NET Core controllers, EF Core, xUnit, EF Core InMemory tests.

## Global Constraints

- Keep the optional `enabled` filter.
- Use `PaginatedResult<T>` with `page`, `pageSize`, `totalCount`, `totalPages`, `hasPreviousPage`, and `hasNextPage`.
- Do not add `Products` to the summary DTO or select campaign products in the listing query.
- Preserve `DiscountCampaignDTO` and product projection for `GET /api/v1/DiscountCampaigns/{id}`.
- Preserve unrelated working-tree changes in `src/PrettyWoman.Application/DTOs/Clients/CreateClientDTO.cs`.
- Do not add a database migration; this is a projection/API-contract change only.

---

### Task 1: Add query and summary contracts

**Files:**
- Create: `src/PrettyWoman.Application/DTOs/Discounts/DiscountCampaignQueryDTO.cs`
- Create: `src/PrettyWoman.Application/DTOs/Discounts/DiscountCampaignSummaryDTO.cs`

**Interfaces:**
- Produces `DiscountCampaignQueryDTO.Page`, `.PageSize`, and nullable `.Enabled` for the service and controller.
- Produces `DiscountCampaignSummaryDTO` with campaign metadata only: `Id`, `Name`, `StartDate`, `EndDate`, `Enabled`, `CreatedAt`, `UpdatedAt`, `CreatedById`, and `UpdatedById`.

- [ ] **Step 1: Write the query DTO**

```csharp
public class DiscountCampaignQueryDTO
{
    public int Page { get; set; } = 1;
    public int PageSize { get; set; } = 20;
    public bool? Enabled { get; set; }
}
```

- [ ] **Step 2: Write the summary DTO**

```csharp
public class DiscountCampaignSummaryDTO
{
    public int Id { get; set; }
    public required string Name { get; set; }
    public DateTime StartDate { get; set; }
    public DateTime EndDate { get; set; }
    public bool Enabled { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime? UpdatedAt { get; set; }
    public string? CreatedById { get; set; }
    public string? UpdatedById { get; set; }
}
```

- [ ] **Step 3: Verify the new contracts compile**

Run: `/home/hugohuete/.dotnet/dotnet build src/PrettyWoman.Application/PrettyWoman.Application.csproj --no-restore`

Expected: successful build with no errors.

### Task 2: Change the application service contract and implementation

**Files:**
- Modify: `src/PrettyWoman.Application/Interfaces/IDiscountCampaignService.cs`
- Modify: `src/PrettyWoman.Application/Services/DiscountCampaignService.cs`

**Interfaces:**
- Consumes `DiscountCampaignQueryDTO`.
- Produces `Task<PaginatedResult<DiscountCampaignSummaryDTO>> GetAllAsync(DiscountCampaignQueryDTO query)`.
- Keeps `Task<DiscountCampaignDTO> GetByIdAsync(int id)` unchanged.

- [ ] **Step 1: Add a failing service test for pagination and product-free items**

Add a test that seeds at least three campaigns, calls `GetAllAsync(new DiscountCampaignQueryDTO { Page = 2, PageSize = 1 })`, and asserts `Page`, `PageSize`, `TotalCount`, `TotalPages`, one item, and that the returned item is a `DiscountCampaignSummaryDTO` with no product property.

- [ ] **Step 2: Add a failing service test for `enabled`**

Seed enabled and disabled campaigns, call the query with `Enabled = false`, assert only disabled campaigns are returned, then call with `Enabled = null` and assert both are counted.

- [ ] **Step 3: Run the focused tests and verify they fail against the old signature**

Run: `/home/hugohuete/.dotnet/dotnet test tests/PrettyWoman.Application.Tests/PrettyWoman.Application.Tests.csproj --no-restore --filter FullyQualifiedName~DiscountCampaignServiceTests`

Expected: compilation failures because the old service signature returns `IEnumerable<DiscountCampaignDTO>`.

- [ ] **Step 4: Update the interface and service signature**

Import `PrettyWoman.Application.Common.Models` and replace the old list signature with:

```csharp
Task<PaginatedResult<DiscountCampaignSummaryDTO>> GetAllAsync(DiscountCampaignQueryDTO query);
```

- [ ] **Step 5: Implement the filtered count and stable page query**

Normalize invalid pagination using the existing service convention (`Page < 1` becomes `1`; `PageSize < 1` becomes the default `20`), then use:

```csharp
var campaignsQuery = _context.DiscountCampaigns
    .AsNoTracking()
    .Where(campaign => !query.Enabled.HasValue || campaign.Enabled == query.Enabled.Value);

var totalCount = await campaignsQuery.CountAsync();
var campaigns = await campaignsQuery
    .OrderByDescending(campaign => campaign.StartDate)
    .ThenBy(campaign => campaign.Name)
    .ThenBy(campaign => campaign.Id)
    .Skip((query.Page - 1) * query.PageSize)
    .Take(query.PageSize)
    .Select(campaign => new DiscountCampaignSummaryDTO
    {
        Id = campaign.Id,
        Name = campaign.Name,
        StartDate = campaign.StartDate,
        EndDate = campaign.EndDate,
        Enabled = campaign.Enabled,
        CreatedAt = campaign.CreatedAt,
        UpdatedAt = campaign.UpdatedAt,
        CreatedById = campaign.CreatedById,
        UpdatedById = campaign.UpdatedById
    })
    .ToListAsync();
```

Return those values in `PaginatedResult<DiscountCampaignSummaryDTO>`. Do not reference `DiscountCampaignProducts` in this query.

- [ ] **Step 6: Run focused tests and verify they pass**

Run: `/home/hugohuete/.dotnet/dotnet test tests/PrettyWoman.Application.Tests/PrettyWoman.Application.Tests.csproj --no-restore --filter FullyQualifiedName~DiscountCampaignServiceTests`

Expected: all discount campaign service tests pass, including existing create/update/disable/detail tests.

### Task 3: Update the API controller contract

**Files:**
- Modify: `src/PrettyWoman.Api/Controllers/DiscountCampaignsController.cs`

**Interfaces:**
- Consumes `DiscountCampaignQueryDTO` from query binding.
- Returns `ActionResult<PaginatedResult<DiscountCampaignSummaryDTO>>` from the list endpoint.
- Keeps detail action return type `ActionResult<DiscountCampaignDTO>`.

- [ ] **Step 1: Add the paginated model import and update `GetAll`**

Use:

```csharp
[HttpGet]
public async Task<ActionResult<PaginatedResult<DiscountCampaignSummaryDTO>>> GetAll(
    [FromQuery] DiscountCampaignQueryDTO query)
{
    var discountCampaigns = await _discountCampaignService.GetAllAsync(query);
    return Ok(discountCampaigns);
}
```

- [ ] **Step 2: Build the API project**

Run: `/home/hugohuete/.dotnet/dotnet build src/PrettyWoman.Api/PrettyWoman.Api.csproj --no-restore`

Expected: successful build with no errors.

### Task 4: Update documentation and API regression coverage

**Files:**
- Modify: `docs/frontend-handoff.md`
- Inspect: `tests/PrettyWoman.Api.IntegrationTests/` for an existing campaign API fixture; modify it only if campaign setup is already available.
- Test: `tests/PrettyWoman.Application.Tests/Services/Discounts/DiscountCampaignServiceTests.cs` for the mandatory regression coverage.

- [ ] **Step 1: Document the new list response**

Document query examples using `page`, `pageSize`, and `enabled`; show `items` with campaign metadata and explicitly state that `products` is absent from list items.

- [ ] **Step 2: Document the unchanged detail response**

State that `GET /api/v1/DiscountCampaigns/{id}` continues returning `products`.

- [ ] **Step 3: Add API coverage when the existing integration fixture supports campaigns**

If the existing integration fixture already supports campaign setup, assert that the list response deserializes as `PaginatedResult<DiscountCampaignSummaryDTO>` and that the detail response still includes products. Otherwise, retain the service tests as the regression boundary and do not introduce unrelated integration infrastructure.

### Task 5: Full verification and handoff

**Files:**
- No additional production files.

- [ ] **Step 1: Check model state and formatting**

Run: `git diff --check` and `/home/hugohuete/.dotnet/dotnet ef migrations has-pending-model-changes --project src/PrettyWoman.Infrastructure --startup-project src/PrettyWoman.Api` with the configured design-time JWT key.

Expected: no whitespace errors and no pending model changes.

- [ ] **Step 2: Run the complete test suite**

Run: `make test`

Expected: all test projects pass.

- [ ] **Step 3: Inspect the final diff**

Run: `git status --short` and `git diff --stat HEAD~1` while confirming the pre-existing `CreateClientDTO.cs` change remains unstaged and untouched.

- [ ] **Step 4: Commit the implementation**

Use a focused Conventional Commit such as:

```bash
git add src/PrettyWoman.Application/DTOs/Discounts src/PrettyWoman.Application/Interfaces/IDiscountCampaignService.cs src/PrettyWoman.Application/Services/DiscountCampaignService.cs src/PrettyWoman.Api/Controllers/DiscountCampaignsController.cs tests/PrettyWoman.Application.Tests/Services/Discounts/DiscountCampaignServiceTests.cs docs/frontend-handoff.md
git commit -m "feat: paginate discount campaign listing"
```
