# Devolver productos después de la venta

## Objetivo

Registrar devoluciones parciales o totales sin alterar los productos, pagos ni el estado histórico de la venta original.

## Endpoints

- `GET /api/v1/sales/{saleId}/returns`
- `POST /api/v1/sales/{saleId}/returns`
- `POST /api/v1/sales/{saleId}/returns/{returnId}/pickup`
- `POST /api/v1/sales/{saleId}/returns/{returnId}/receive`
- `POST /api/v1/sales/{saleId}/returns/{returnId}/cancel`

## Crear la devolución

Una devolución usa `SaleReturn` y `SaleReturnItem`, vinculados a la venta y a la línea original. Cada ítem congela tanto el monto reconocido a la clienta como `originalUnitCost`, tomado de `sale_products.unit_cost_at_sale`; así la utilidad histórica no depende del costo actual del catálogo.

Solo se pueden devolver prendas de ventas `SentForDelivery` o `Completed` cuyo inventario ya haya salido. La suma de devoluciones y cambios activos nunca puede superar la cantidad vendida de una línea.

El motivo es uno de:

- `CustomerPreference`: no le quedó, cambió de opinión u otra razón no atribuible a la tienda.
- `ProductDefect` o `StoreError`: defecto del producto o error atribuible a la tienda.

El método es `InStore` o `DeliveryAgency`. El método por agencia exige agencia; el método en local no permite agencia ni montos de envío.

## Reembolso y envíos

`refundTotal = productRefundTotal + originalShippingRefund - returnShippingChargedToClient`.

- Por preferencia de la clienta, se devuelve el valor reconocido de las prendas y, si la agencia retorna el producto, el envío de retorno puede descontarse del reembolso. El envío original no se devuelve.
- Cuando la causa es de la tienda, la tienda asume el envío de retorno. Solo se devuelve también el envío original si la venta original contenía exactamente una prenda.
- El costo que se pagará a la agencia se guarda en `returnShippingPaidToAgency`; no es un cobro adicional a la clienta.
- La venta original conserva su estado de pago. El reembolso crea un `FinancialMovement` de salida `CustomerRefund`, ligado a la devolución, y guarda el método de pago usado.

## Recepción física e inventario

Para una devolución por agencia, `pickup` registra la recogida y ejecuta el reembolso. La prenda continúa fuera de inventario hasta que llegue al local. Para una devolución en local, `receive` registra la recepción y el reembolso en el mismo acto; debe indicar el método de reembolso.

Al recibir se debe reportar el estado de todos los ítems:

- En buen estado: se crea un movimiento `CustomerReturn` desde `OutOfInventory` a `Available` y la cantidad vuelve a estar disponible.
- Dañada: se crea un movimiento `CustomerReturn` hacia `Unavailable` y un `ProductInventoryIssue` abierto de tipo `Damaged`. La cantidad no puede venderse hasta resolver el issue.

Una devolución solo puede cancelarse mientras esté `Requested`, antes de recogida o recepción.

## Conciliación de agencia

Una devolución recogida por agencia (`PickedUpAndRefunded`) o ya recibida (`Completed`) puede incluirse una sola vez en `POST /api/v1/delivery-agency-reconciliations`, dentro de `returns`. Debe pertenecer a la misma agencia que la liquidación. Su `returnShippingPaidToAgency` se suma al monto pagado a la agencia y queda enlazada a la conciliación.
