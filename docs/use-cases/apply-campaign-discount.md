# Aplicar descuento de campaña

## Objetivo

Aplicar automáticamente un descuento vigente configurado en una campaña.

## Cuándo aplica

- Producto participa en una promoción activa.
- La fecha actual está entre `start_date` y `end_date`.
- La campaña está habilitada.

## Tablas involucradas

- `discount_campaigns`
- `discount_campaign_products`
- `discount_types`
- `discount_sources`
- `sale_details`
- `sales`

## Flujo esperado

1. Buscar campañas activas por fecha.
2. Validar que el producto participe en una campaña vigente.
3. Validar que no haya conflictos de descuentos activos para el mismo producto.
4. Calcular descuento según tipo:
   - porcentaje
   - monto fijo
   - precio final fijo
5. Guardar en `sale_details`:
   - `original_sale_price`
   - `discount_amount`
   - `final_sale_price`
   - `discount_source_id = Campaign`
   - `discount_campaign_id`
6. Recalcular totales de `sales`.

## Reglas de negocio

- No debe haber dos descuentos activos para el mismo producto en el mismo rango de fechas.
- El descuento aplicado debe quedar congelado en `sale_details`.
- Aunque la campaña termine después, la venta debe conservar el precio histórico.
- El descuento no debe producir precio negativo.

## Errores esperados

- Campaña inexistente.
- Campaña deshabilitada.
- Producto no participa.
- Descuento inválido.
- Precio final menor que cero.
