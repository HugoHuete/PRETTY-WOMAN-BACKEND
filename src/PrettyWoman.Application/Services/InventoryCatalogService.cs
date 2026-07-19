using Microsoft.EntityFrameworkCore;
using PrettyWoman.Application.DTOs.InventoryCatalogs;
using PrettyWoman.Application.Interfaces;
using PrettyWoman.Domain.Enums;

namespace PrettyWoman.Application.Services;

public class InventoryCatalogService(IApplicationDbContext context) : IInventoryCatalogService
{
    private readonly IApplicationDbContext _context = context;

    public async Task<IEnumerable<InventoryCatalogItemDTO>> GetAdjustmentReasonsAsync()
    {
        return await _context.InventoryAdjustmentReasons
            .AsNoTracking()
            .OrderBy(reason => reason.Id)
            .Select(reason => new InventoryCatalogItemDTO
            {
                Id = reason.Id,
                Name = reason.Name
            })
            .ToListAsync();
    }

    public async Task<IEnumerable<InventoryCatalogItemDTO>> GetStockBucketsAsync()
    {
        return await _context.InventoryStockBuckets
            .AsNoTracking()
            .OrderBy(bucket => bucket.Id)
            .Select(bucket => new InventoryCatalogItemDTO
            {
                Id = bucket.Id,
                Name = bucket.Name
            })
            .ToListAsync();
    }

    public async Task<IEnumerable<InventoryAdjustmentReasonSuggestionDTO>> GetAdjustmentReasonSuggestionsAsync()
    {
        var reasonNames = await _context.InventoryAdjustmentReasons
            .AsNoTracking()
            .ToDictionaryAsync(reason => reason.Id, reason => reason.Name);
        var bucketNames = await _context.InventoryStockBuckets
            .AsNoTracking()
            .ToDictionaryAsync(bucket => bucket.Id, bucket => bucket.Name);

        return BuildAdjustmentReasonSuggestions(reasonNames, bucketNames);
    }

    private static List<InventoryAdjustmentReasonSuggestionDTO> BuildAdjustmentReasonSuggestions(
        IReadOnlyDictionary<int, string> reasonNames,
        IReadOnlyDictionary<int, string> bucketNames)
        =>
        [
            CreateSuggestion(
                InventoryAdjustmentReasonOption.ManualCorrection,
                reasonNames,
                "Correcciones manuales de conteo o revision que no pertenecen a un flujo especifico.",
                [
                    CreateMovement(
                        InventoryStockBucketOption.Available,
                        InventoryStockBucketOption.Unavailable,
                        bucketNames,
                        "Mover una unidad disponible a no disponible cuando el conteo o revision fisica muestra que no debe venderse."),
                    CreateMovement(
                        InventoryStockBucketOption.Unavailable,
                        InventoryStockBucketOption.Available,
                        bucketNames,
                        "Regresar una unidad no disponible a disponible cuando la revision confirma que puede venderse."),
                    CreateMovement(
                        InventoryStockBucketOption.Available,
                        InventoryStockBucketOption.OutOfInventory,
                        bucketNames,
                        "Sacar una unidad disponible del inventario activo cuando el conteo fisico confirma que ya no esta."),
                    CreateMovement(
                        InventoryStockBucketOption.OutOfInventory,
                        InventoryStockBucketOption.Available,
                        bucketNames,
                        "Regresar una unidad encontrada al inventario disponible.")
                ]),
            CreateSuggestion(
                InventoryAdjustmentReasonOption.ProductCodeMixUp,
                reasonNames,
                "Correcciones por cruce de codigo entre variantes.",
                [
                    CreateMovement(
                        InventoryStockBucketOption.OutOfInventory,
                        InventoryStockBucketOption.Available,
                        bucketNames,
                        "Reponer la variante incorrecta que habia salido por cruce de codigo."),
                    CreateMovement(
                        InventoryStockBucketOption.Available,
                        InventoryStockBucketOption.OutOfInventory,
                        bucketNames,
                        "Sacar la variante correcta que debio haberse registrado en la operacion original.")
                ]),
            CreateSuggestion(
                InventoryAdjustmentReasonOption.PurchaseSurplus,
                reasonNames,
                "Si el sobrante no estaba contemplado en la compra, primero se debe aumentar/corregir la cantidad comprada y luego recibirlo por el flujo de compras.",
                []),
            CreateSuggestion(
                InventoryAdjustmentReasonOption.PurchaseShortage,
                reasonNames,
                "Correcciones por faltantes detectados despues de haber recibido inventario.",
                [
                    CreateMovement(
                        InventoryStockBucketOption.Available,
                        InventoryStockBucketOption.OutOfInventory,
                        bucketNames,
                        "Sacar una unidad faltante detectada despues de haber sido recibida.")
                ]),
            CreateSuggestion(
                InventoryAdjustmentReasonOption.LostItem,
                reasonNames,
                "Baja de unidades que ya no se encontraron fisicamente.",
                [
                    CreateMovement(
                        InventoryStockBucketOption.Available,
                        InventoryStockBucketOption.OutOfInventory,
                        bucketNames,
                        "Dar de baja una unidad disponible que se perdio o no se encontro en conteo fisico.")
                ]),
            CreateSuggestion(
                InventoryAdjustmentReasonOption.FoundItem,
                reasonNames,
                "Reposicion de unidades que se habian dado de baja y luego aparecieron.",
                [
                    CreateMovement(
                        InventoryStockBucketOption.OutOfInventory,
                        InventoryStockBucketOption.Available,
                        bucketNames,
                        "Regresar a disponible una unidad que se habia dado de baja y luego aparecio.")
                ]),
            CreateSuggestion(
                InventoryAdjustmentReasonOption.Donation,
                reasonNames,
                "Salida no comercial de unidades disponibles.",
                [
                    CreateMovement(
                        InventoryStockBucketOption.Available,
                        InventoryStockBucketOption.OutOfInventory,
                        bucketNames,
                        "Sacar del inventario una unidad disponible entregada como donacion.")
                ]),
            CreateSuggestion(
                InventoryAdjustmentReasonOption.Other,
                reasonNames,
                "Casos excepcionales que no encajan en los motivos especificos.",
                [
                    CreateMovement(
                        InventoryStockBucketOption.Available,
                        InventoryStockBucketOption.Unavailable,
                        bucketNames,
                        "Usar cuando la unidad sigue existiendo pero temporalmente no debe venderse."),
                    CreateMovement(
                        InventoryStockBucketOption.Available,
                        InventoryStockBucketOption.OutOfInventory,
                        bucketNames,
                        "Usar cuando la unidad debe salir del inventario activo."),
                    CreateMovement(
                        InventoryStockBucketOption.OutOfInventory,
                        InventoryStockBucketOption.Available,
                        bucketNames,
                        "Usar cuando una unidad que estaba fuera vuelve a estar disponible.")
                ])
        ];

    private static InventoryAdjustmentReasonSuggestionDTO CreateSuggestion(
        InventoryAdjustmentReasonOption reason,
        IReadOnlyDictionary<int, string> reasonNames,
        string description,
        List<InventoryAdjustmentBucketSuggestionDTO> movements)
        => new()
        {
            InventoryAdjustmentReasonId = (int)reason,
            InventoryAdjustmentReasonName = GetName(reasonNames, (int)reason, reason),
            Description = description,
            SuggestedMovements = movements
        };

    private static InventoryAdjustmentBucketSuggestionDTO CreateMovement(
        InventoryStockBucketOption from,
        InventoryStockBucketOption to,
        IReadOnlyDictionary<int, string> bucketNames,
        string description)
        => new()
        {
            FromStockBucketId = (int)from,
            FromStockBucketName = GetName(bucketNames, (int)from, from),
            ToStockBucketId = (int)to,
            ToStockBucketName = GetName(bucketNames, (int)to, to),
            Description = description
        };

    private static string GetName<TEnum>(IReadOnlyDictionary<int, string> names, int id, TEnum fallback)
        where TEnum : struct, Enum
        => names.TryGetValue(id, out var name) ? name : fallback.ToString();
}
