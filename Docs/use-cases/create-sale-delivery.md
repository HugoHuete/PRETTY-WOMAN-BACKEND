# Crear envío de venta

## Objetivo

Registrar un intento de envío asociado a una venta.

## Cuándo aplica

- Venta por WhatsApp, Instagram u otro canal no-local.
- Reenvío porque la clienta no contestó.
- Cambio de agencia.
- Reenvío por dirección incorrecta.
- Entrega fallida que se intenta nuevamente.

## Tablas involucradas

- `sales`
- `sale_deliveries`
- `delivery_statuses`
- `shipping_agencies`
- `municipalities`
- `clients`

## Flujo esperado

1. Buscar la venta.
2. Validar que la venta no esté cancelada.
3. Validar datos de destinatario.
4. Validar municipio.
5. Validar agencia de envío, si aplica.
6. Crear registro en `sale_deliveries`.
7. Asignar estado inicial, por ejemplo `Pending` o `Sent`.
8. Guardar costos:
   - monto cobrado al cliente por envío
   - monto pagado a la agencia, si ya se conoce
   - monto a recolectar, si aplica
9. Si es reenvío, mantener el envío anterior con su estado histórico.

## Reglas de negocio

- Una venta puede tener varios envíos.
- El envío pertenece a `sale_deliveries`, no directamente a `sales`.
- La agencia de envío pertenece al envío, no a la venta.
- No se debe borrar un envío fallido. Debe quedar con estado histórico.
- La ganancia por envío se calcula comparando lo cobrado al cliente versus lo pagado realmente.

## Estados sugeridos

- `Pending`
- `Sent`
- `Delivered`
- `Failed`
- `Returned`
- `Rescheduled`
- `Cancelled`

## Errores esperados

- Venta no existe.
- Venta cancelada.
- Municipio inexistente.
- Agencia inexistente.
- Datos de destinatario incompletos.
