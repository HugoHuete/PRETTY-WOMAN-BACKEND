# Cambiar productos después de una venta

Un cambio no modifica los productos ni los pagos históricos de la venta original. Se registra como un `SaleExchange` vinculado a ella.

## Crear el cambio

`POST /api/v1/sales/{saleId}/exchanges` recibe las prendas retornadas, identificadas por su `originalSaleProductId`, y las prendas de salida. Estas últimas se clasifican como `Replacement` o `AdditionalPurchase`.

Al crearlo, las prendas de salida se mueven de disponible a reservado. El saldo del cambio es el total de salida menos el crédito reconocido a la clienta. Si se indica una agencia y el saldo (más el envío cobrado) es positivo, la agencia debe permitir cobro contra entrega; el envío postventa expone ese total como `amountToCollect`.

### Crédito reconocido por prenda retornada

`recognizedUnitAmount` es el crédito acordado **por cada unidad** que la clienta devuelve. Se descuenta del total de las prendas de salida para calcular `balanceToCollect`. En un cambio normal debe ser igual al precio final que la clienta pagó por esa prenda; se puede indicar un valor menor solamente cuando exista una excepción acordada (por ejemplo, una política de cambio o una condición especial de la prenda). Nunca puede ser negativo ni mayor al precio final cobrado originalmente.

## Retorno por agencia

El intercambio físico se registra una sola vez con `handover`: la agencia entrega las prendas nuevas y recibe las prendas originales en el mismo acto. Cada prenda original vuelve de `OutOfInventory` a `Available` y queda identificada por el item de retorno como pendiente de retorno físico. Al llegar a tienda se registra `received`; si no llega, se registra `missing` y se revierte su disponibilidad.

## Entrega y ganancia

La entrega de las prendas de salida convierte su reserva en salida de inventario. La ganancia neta del cambio combina la ganancia de esas prendas con la reversión económica de cada prenda retornada que la agencia aceptó. Los `SaleProducts` originales no se editan, por lo que se preserva la trazabilidad de la venta.

La agencia registra el cobro del cambio en la conciliación usando `postSaleDeliveries[].amountCollectedNio`, `amountCollectedUsd`, `collectionExchangeRate` y `changeGivenNio`. Debe recaudar el `amountToCollect` completo. El cobro queda asociado al envío postventa y no se asigna a los pagos de la venta original.

Al crear el cambio por agencia, solo se registra el envío cobrado a la clienta. `shippingPaidToAgency` se informa en `POST /api/v1/delivery-agency-reconciliations` dentro de `postSaleDeliveries`, cuando se conoce el costo real de la agencia.

## Liquidación de cambios en tienda

`POST /api/v1/sales/{saleId}/exchanges/{exchangeId}/settlement` liquida el saldo completo después de registrar el intercambio físico. Si `balanceToCollect` es positivo, registra un cobro; si es negativo, registra un reembolso por su valor absoluto. Los cambios enviados por agencia continúan liquidándose mediante conciliación.

La liquidación admite varias líneas en NIO, por ejemplo efectivo más tarjeta. La suma debe ser exactamente igual al saldo absoluto y se registra en `SaleExchangeSettlement` con los medios de pago y movimientos financieros. Una línea con tarjeta requiere una terminal habilitada; el personal realiza la operación en la terminal y luego la registra. Los cobros con tarjeta guardan la comisión e impuesto configurados para esa terminal. Ninguna línea modifica los pagos ni el saldo de la venta original.

Ejemplo para cobrar un saldo de C$200, dividido entre efectivo y tarjeta:

```json
{
  "movementDate": "2026-11-07T18:00:00Z",
  "comments": "Diferencia del cambio",
  "tenders": [
    { "paymentMethodId": 1, "amount": 100.00 },
    { "paymentMethodId": 3, "paymentTerminalId": 2, "amount": 100.00 }
  ]
}
```
