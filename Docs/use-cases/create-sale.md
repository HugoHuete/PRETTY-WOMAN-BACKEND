# Crear venta

## Objetivo

Registrar una venta con sus productos, descuentos aplicados, subtotal, total y estado inicial.

## Cuándo aplica

- Venta en local.
- Venta por WhatsApp, Instagram u otro canal.
- Venta con productos ya confirmados.
- Venta que luego podrá tener uno o varios pagos.
- Venta que luego podrá tener uno o varios envíos.

## Tablas involucradas

- `sales`
- `sale_details`
- `sale_statuses`
- `sales_channels`
- `clients`
- `products`
- `inventory_movements`
- `discount_campaigns`, si aplica
- `discount_campaign_products`, si aplica

## Flujo esperado

1. Validar que el canal de venta exista y esté habilitado.
2. Validar o crear el cliente, si aplica.
3. Validar que cada producto tenga stock disponible suficiente.
4. Calcular precio original de cada producto.
5. Aplicar descuento de campaña o manual, si corresponde.
6. Guardar en cada `sale_detail`:
   - `original_sale_price`
   - `discount_amount`
   - `final_sale_price`
   - `cost_at_sale`
   - `gross_profit`
   - `discount_source_id`
   - `discount_campaign_id`, si aplica
   - `discount_reason`, si aplica
7. Calcular en `sales`:
   - `subtotal_before_discount`
   - `total_discount`
   - `subtotal_products`
   - `shipping_cost`, si aplica
   - `total`
   - `gross_profit`
8. Crear la venta con estado inicial definido, por ejemplo `Pending` o `Confirmed`.
9. Crear las líneas en `sale_details`.
10. Descontar inventario si la venta queda confirmada.
11. Crear movimientos de inventario tipo `Sale` por cada producto vendido.

## Reglas de negocio

- Una venta no representa necesariamente dinero recibido. El dinero se registra en `sale_payments`.
- Una venta puede existir sin pago completo.
- Una venta puede tener múltiples pagos.
- Una venta puede tener múltiples envíos.
- El costo histórico del producto debe quedar congelado en `sale_details.cost_at_sale`.
- La ganancia por línea debe quedar congelada en `sale_details.gross_profit`.

## Inventario

Si la venta queda confirmada:

- `products.available_quantity` disminuye.
- Se crea `inventory_movements` tipo `Sale`.

Si la venta queda pendiente y no debe apartar stock, no se descuenta inventario todavía.

Si la venta debe apartar stock sin confirmarse, usar `product_holds`.

## Errores esperados

- Producto sin stock suficiente.
- Producto inexistente.
- Cliente inválido.
- Canal de venta inexistente.
- Descuento inválido.
- Total calculado no coincide con total enviado por el frontend.
