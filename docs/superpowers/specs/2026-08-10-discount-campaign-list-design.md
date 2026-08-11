# Diseño: listado paginado de campañas de descuento

## Objetivo

Actualizar el endpoint de listado de campañas de descuento para que sea paginado y no cargue ni exponga los productos asociados. El endpoint de detalle por id conservará los productos.

## Contrato HTTP

### Listado

`GET /api/v1/DiscountCampaigns?page=1&pageSize=20&enabled=true`

Parámetros:

- `page`: página solicitada, inicia en 1.
- `pageSize`: cantidad máxima de elementos, con la misma normalización usada por los listados existentes.
- `enabled`: filtro opcional; cuando no se envía incluye campañas habilitadas y deshabilitadas.

Respuesta: `PaginatedResult<DiscountCampaignSummaryDTO>`.

Cada elemento contiene solo los metadatos de la campaña: `id`, `name`, `startDate`, `endDate`, `enabled` y campos de auditoría. No existe una propiedad `products` en este DTO.

El orden será estable: `startDate` descendente, `name` ascendente e `id` ascendente. El conteo total se calculará sobre los filtros antes de aplicar `Skip` y `Take`.

### Detalle

`GET /api/v1/DiscountCampaigns/{id}` mantiene `DiscountCampaignDTO`, incluyendo la colección `products` con nombre/código del producto y datos del descuento.

## Capas y cambios

- Agregar `DiscountCampaignQueryDTO` y `DiscountCampaignSummaryDTO` en Application.
- Cambiar `IDiscountCampaignService.GetAllAsync` para recibir la consulta y devolver `PaginatedResult<DiscountCampaignSummaryDTO>`.
- Proyectar el listado directamente desde `DiscountCampaigns` sin incluir ni seleccionar `DiscountCampaignProducts`.
- Mantener la proyección actual de `GetByIdAsync` para el detalle.
- Actualizar el controlador para enlazar todos los parámetros de consulta y declarar el tipo paginado.
- Actualizar el handoff del frontend con ejemplos de listado y detalle.

## Validación

Agregar pruebas de servicio para:

1. devolver los metadatos de paginación y respetar `page`/`pageSize`;
2. aplicar `enabled` cuando se envía y devolver ambos estados cuando se omite;
3. mantener el orden estable;
4. devolver productos únicamente en `GetByIdAsync`.

Ejecutar las pruebas de `DiscountCampaignServiceTests`, la suite completa y la verificación de cambios pendientes del modelo si el ajuste no modifica persistencia.

## Compatibilidad

El cambio del listado es una modificación de contrato: los consumidores deben leer `items` en lugar de un arreglo raíz y usar la metadata de paginación. El endpoint de detalle no cambia semánticamente.
