# Deliveries Business Rules

## Objetivo

Registrar envíos y reenvíos asociados a una venta, sin obligar a que cada venta tenga envío.

## Tablas principales

- `sale_deliveries`
- `delivery_statuses`
- `delivery_agencies`
- `sales`
- `municipalities`

## Regla: una venta puede tener cero, uno o varios envíos

Venta local:

- No requiere registro en `sale_deliveries`.

Venta con envío:

- Debe tener uno o más registros en `sale_deliveries`.

Reenvío:

- Debe registrarse como un nuevo `sale_delivery` asociado a la misma venta.

## Regla: `sale_deliveries` representa intentos o eventos de envío

Cada registro representa un envío o intento de envío.

Debe guardar:

- venta relacionada
- fecha del envío
- agencia de entrega
- código o tracking interno
- monto a cobrar si aplica
- costo de entrega cobrado a la clienta
- costo realmente pagado a la agencia
- nombre del cliente o receptor
- dirección
- teléfono
- estado de entrega
- comentarios

## Regla: varios envíos en una misma venta

Casos válidos:

- La clienta no contestó y se reprogramó.
- Se envió a dirección incorrecta.
- Hubo cambio de agencia.
- Se hizo un reenvío por error operativo.

No se debe sobrescribir el envío anterior. Se debe registrar un nuevo envío para conservar historial.

## Estados recomendados para `delivery_statuses`

- `Pending`
- `Sent`
- `Delivered`
- `Failed`
- `Returned`
- `Rescheduled`
- `Cancelled`

## Regla: costo cobrado vs costo pagado

Es importante diferenciar:

- monto cobrado a la clienta por envío
- monto realmente pagado a la agencia

La diferencia afecta ganancia.

Ejemplo:

```txt
shipping_charged_to_client = 100
shipping_paid_to_agency = 80
shipping_margin = 20
```

## Regla: productos enviados para selección

Si se envían varias tallas para que la clienta escoja una:

- El envío registra el intento logístico.
- La venta final solo debe incluir el producto escogido.
- Las prendas adicionales deben manejarse con reservas (`product_holds`) o movimientos de inventario.

## Regla: `delivery_agency_id` pertenece al envío

La agencia de entrega debe estar en `sale_deliveries`, no en `sales`.

Razón: una venta puede tener varios envíos, cada uno con agencia diferente.
