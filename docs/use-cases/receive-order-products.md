# Recibir productos de una orden

## Objetivo

Registrar la recepción física de productos comprados, actualizar inventario, registrar costos de envío bodega -> Nicaragua y crear los movimientos financieros e inventario correspondientes.

## Cuándo aplica

- Llegó una orden completa.
- Llegó una orden parcialmente.
- Llegó uno o varios tracking numbers de una orden extranjera.
- Llegó una compra sin tracking y el costo de envío bodega -> Nicaragua se conoce directamente.
- Se abren paquetes y luego se separa la mercadería por producto.

## Endpoint

```http
POST /api/v1/orders/{orderId}/receipts
```

El endpoint cubre recepciones parciales y completas. La diferencia está en las cantidades enviadas.

Si el costo final del envío todavía no está disponible o fue registrado incorrectamente, el administrador puede corregir una recepción existente:

```http
PATCH /api/v1/orders/{orderId}/receipts/{receiptId}
```

Con trackings, el request debe incluir exactamente los trackings asociados a la recepción y su nuevo `shippingCostUsd` individual:

```json
{
  "trackingNumbers": [
    { "id": 12, "shippingCostUsd": 31.5 },
    { "id": 13, "shippingCostUsd": 18 }
  ]
}
```

Sin trackings, se envía el nuevo total directamente:

```json
{
  "warehouseShippingCostUsd": 18
}
```

Para corregir también la distribución, se pueden enviar exactamente los detalles de producto de la recepción con su nuevo peso estimado por unidad:

```json
{
  "warehouseShippingCostUsd": 18,
  "products": [
    { "productReceiptDetailId": 21, "weight": 1.5 },
    { "productReceiptDetailId": 22, "weight": 3 }
  ]
}
```

El peso se multiplica por la cantidad recibida (`weight * quantity`). El campo `products` es opcional; si se envía, debe incluir exactamente todos los detalles de la recepción y cada peso debe ser mayor que cero.

La corrección no cambia la fecha, productos ni cantidades recibidas. Recalcula el costo de los productos y todas sus líneas históricas de venta. Los precios, pagos y movimientos financieros de las ventas permanecen iguales. El movimiento `WarehouseShippingPayment` de la compra se actualiza, crea o elimina según el nuevo costo.

## Tablas involucradas

- `orders`
- `products`
- `order_tracking_numbers`
- `product_receipts`
- `product_receipt_details`
- `inventory_movements`
- `financial_movements`

## Request con tracking numbers

Cuando la orden tiene tracking numbers, el peso y el costo de envío se envían por tracking. El costo total bodega -> Nicaragua sale de la suma de esos trackings.

```json
{
  "receivedDate": "2026-06-21T10:30:00Z",
  "trackingNumbers": [
    {
      "id": 12,
      "weight": 8.5,
      "shippingCostUsd": 24
    }
  ],
  "products": [
    {
      "productId": 30,
      "quantity": 3,
      "weight": 1
    },
    {
      "productId": 31,
      "quantity": 2,
      "weight": 3
    }
  ],
  "comments": "Recepción paquete 1"
}
```

## Request sin tracking numbers

Cuando la orden no tiene tracking numbers, el costo de envío bodega -> Nicaragua se envía directamente.

```json
{
  "warehouseShippingCostUsd": 18,
  "products": [
    {
      "productId": 30,
      "quantity": 5,
      "weight": 1
    }
  ],
  "comments": "Recepción compra local"
}
```

## Flujo esperado

1. Buscar la orden.
2. Validar que la orden no esté cancelada. Si ya está completamente recibida, solo permitir líneas marcadas como sobrante.
3. Validar que los productos pertenezcan a la orden.
4. Validar que las cantidades recibidas no superen la cantidad pendiente, excepto cuando la línea venga marcada explícitamente como sobrante.
5. Si la orden tiene tracking numbers, actualizar peso, costo USD, fecha de entrega y relación con la recepción.
6. Si la orden no tiene tracking numbers, tomar `warehouseShippingCostUsd` directamente del request.
7. Crear `product_receipts`.
8. Crear un `product_receipt_detail` por producto recibido.
9. Aumentar `products.received_quantity`.
10. Aumentar `products.available_quantity`.
11. Convertir el costo bodega -> Nicaragua de USD a NIO usando `orders.exchange_rate`.
12. Sumar ese costo a `orders.warehouse_shipping_cost_usd` y `orders.total_cost_nio`.
13. Distribuir el costo convertido entre los productos recibidos usando `products[].weight * products[].quantity` y actualizar sus costos.
14. Actualizar `orders.received_amount_nio` con el valor de mercadería recibida en córdobas.
15. Crear `inventory_movements` tipo `PurchaseReceived`.
16. Crear `financial_movements` de egreso por el pago de envío bodega -> Nicaragua cuando el costo sea mayor que cero.
17. Actualizar estado de la orden a `PartiallyReceived` o `Received`.

## Reglas de negocio

- No es obligatorio separar productos por tracking. Los trackings controlan paquetes; la recepción real se registra por producto.
- Si una orden tiene tracking numbers, el request debe enviar al menos un tracking y el costo de envío debe venir por tracking.
- Si una orden no tiene tracking numbers, el request no debe enviar tracking IDs y puede enviar `warehouseShippingCostUsd` directamente.
- `order_tracking_numbers.shipping_cost` guarda el costo USD del envío bodega -> Nicaragua para ese paquete.
- `order_tracking_numbers.weight` guarda el peso del paquete recibido.
- `orders.warehouse_shipping_cost_usd` acumula los costos de envío bodega -> Nicaragua de todas las recepciones.
- El costo de envío bodega -> Nicaragua se convierte a córdobas con `orders.exchange_rate`.
- `products[].weight` representa el peso físico estimado por unidad para distribuir el envío; si no se envía, el backend usa `1`.
- `products[].isSurplus` permite registrar una unidad recibida de más. Debe marcarse por línea y requiere `products[].comments`.
- El costo convertido se distribuye proporcionalmente a `products[].weight * products[].quantity`.
- El costo convertido se suma a `products.allocated_shipping_cost_nio`, `products.total_cost_nio` y recalcula `products.unit_cost_nio` y `products.unit_cost_usd`. Si hay sobrantes, el costo unitario se reparte entre la mayor cantidad entre unidades compradas y unidades recibidas.
- `orders.received_amount_nio` refleja solamente mercadería recibida, sin envíos; en recepción completa debe quedar igual a `orders.merchandise_total_nio`.
- Cada recepción crea un `ProductReceipt`.
- Cada producto recibido crea un `ProductReceiptDetail`.
- Cada producto recibido crea un `InventoryMovement` de tipo `PurchaseReceived` con dirección `In`.
- Cada pago de envío mayor que cero crea un `FinancialMovement` de tipo `WarehouseShippingPayment` con dirección `Out`.
- `received_quantity` puede superar `quantity` solamente cuando la recepción se marca explícitamente como sobrante.
- El inventario disponible aumenta solamente por la cantidad recibida.
- Una orden cancelada no permite recepciones.
- Una orden completamente recibida no permite nuevas recepciones normales; solo permite sobrantes explícitos.

## Cerrar faltantes confirmados

Cuando el proveedor confirme que las cantidades pendientes no llegarán, usar:

```http
POST /api/v1/orders/{orderId}/shortages/close
```

El request debe listar exactamente las variantes que aún tienen cantidad pendiente. La API calcula la cantidad faltante y la pérdida histórica de cada una, ajusta la cantidad final de la variante a la cantidad recibida y deja la orden en `PendingRefund` cuando existe pérdida. En ese estado no admite más recepciones normales.

Si posteriormente el proveedor devuelve dinero, usar un único reembolso por orden:

```http
POST /api/v1/orders/{orderId}/supplier-refund
```

```json
{
  "amountNio": 250,
  "reference": "CR-001",
  "comments": "Crédito recibido del proveedor"
}
```

El monto no puede superar la pérdida total de faltantes de la orden. Al registrarlo, la orden pasa a `Received`. La respuesta de `GET /api/v1/orders/{orderId}` incluye el total faltante, el reembolso, la pérdida neta y el estado calculado de cada línea.

Si el proveedor confirma que no habrá reembolso, usar en lugar del reembolso monetario:

```http
POST /api/v1/orders/{orderId}/supplier-refund/decline
```

```json
{
  "declinedAt": "2026-07-20T15:30:00Z",
  "comments": "Proveedor confirmó que no emitirá crédito."
}
```

Este endpoint no crea un movimiento financiero ni un `supplier_refund`; deja los faltantes con estado calculado `NotRefunded` y pasa la orden a `Received`. Solo puede usarse una vez y no puede combinarse con un reembolso monetario.

## Errores esperados

- Orden inexistente.
- Orden cancelada.
- Orden ya recibida completamente.
- Producto no pertenece a la orden.
- Producto duplicado en el request.
- Cantidad recibida inválida.
- Cantidad recibida excede cantidad pendiente.
- Orden con tracking numbers sin trackings en el request.
- Orden con tracking numbers y costo directo en el request.
- Tracking inexistente o que no pertenece a la orden.
- Tracking duplicado en el request.
- Tracking ya recepcionado.

## Corregir el costo de una recepción

- Solo Admin puede corregir costos.
- La recepción debe pertenecer a la orden indicada y la orden no puede estar cancelada.
- No se pueden agregar o quitar trackings, productos ni cantidades.
- El costo proveedor -> bodega no cambia; solo se corrige el envío bodega -> Nicaragua.
- Cada detalle conserva el peso usado y el costo asignado para permitir correcciones exactas.
- Se puede corregir el peso estimado por unidad enviando `products[].productReceiptDetailId` y `products[].weight`; si se envía la lista, debe incluir todos los detalles de la recepción.
- La redistribución usa `weight * quantity` y conserva la cantidad recibida.
- El costo corregido se redistribuye entre los detalles y actualiza `products.unit_cost_nio`, `products.unit_cost_usd` y las líneas `sale_details`/`sale_products` relacionadas.
- Las ventas conservan su precio, descuentos, total cobrado, pagos y movimientos financieros.
