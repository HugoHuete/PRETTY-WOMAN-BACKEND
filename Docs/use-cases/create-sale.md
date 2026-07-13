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
   - `subtotal`
   - `total_discount`
   - `subtotal_products`
   - `shipping_cost`, si aplica
   - `total`
   - `gross_profit`
8. Crear la venta con estado operativo inicial definido:
   - `Pending`, si todavía no queda apartada para la clienta.
   - `Reserved`, si la clienta ya confirmó y el producto queda apartado para retiro o envío futuro.
   - `ReadyForDelivery`, si ya puede retirarse o enviarse.
9. Crear las líneas en `sale_details`.
11. Para ventas en local, descontar inventario únicamente cuando los pagos de productos alcancen el total y la venta pase a `Completed`.
12. Crear movimientos de inventario tipo `Sale` por cada producto vendido al completarse.

## Reglas de negocio

- Una venta no representa necesariamente dinero recibido. El dinero se registra en `sale_payments`.
- Una venta puede existir sin pago completo. En local, queda pendiente y no descuenta inventario hasta que los pagos de productos completen el total; entonces pasa automáticamente a `Completed`.
- El estado de pago no debe mezclarse con `sale_statuses`; se guarda en `sale_payment_status_id` e inicia como `Unpaid` si no se registra pago inicial.
- Una venta puede tener múltiples pagos.
- Una venta puede tener múltiples envíos.
- El costo histórico del producto debe quedar congelado en `sale_details.cost_at_sale`.
- La ganancia por línea debe quedar congelada en `sale_details.gross_profit`.

## Inventario

En venta local, el inventario disminuye y se crea `inventory_movements` tipo `Sale` al completarse el pago. Si queda pendiente, no se descuenta inventario todavía.

Para una venta reservada o de entrega, se aplican las transiciones operativas de estado que correspondan.

Si hay pago previo para retiro o envio futuro, crear una venta en estado `Reserved`. No usar `product_holds` para reservas con pago.

Si solo se envian productos para seleccion de talla, usar `product_holds` y afectar `unavailable_quantity`.

## Errores esperados

- Producto sin stock suficiente.
- Producto inexistente.
- Cliente inválido.
- Canal de venta inexistente.
- Descuento inválido.
- Total calculado no coincide con total enviado por el frontend.

