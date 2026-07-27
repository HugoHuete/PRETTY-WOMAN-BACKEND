# Clients Business Rules

## Objetivo

Guardar datos básicos de clientas para ventas, entregas, historial y control de bloqueos.

## Tablas principales

- `clients`
- `sales`
- `sale_deliveries`

## Regla: cliente puede ser opcional en venta

Una venta puede tener `client_id` nulo si no se desea registrar cliente, especialmente para ventas rápidas en local.

## Regla: datos de contacto

Datos útiles:

- nombre
- teléfono
- usuario de Instagram
- dirección
- comentarios

Recomendación: dirección y última fecha de compra deberían poder ser nulas para clientes nuevos o ventas locales.

## Regla: cliente bloqueado

Si `is_blocked = true`, el sistema debe advertir o impedir nuevas ventas/envíos según política del negocio.

Motivos posibles:

- no contestó entregas repetidamente
- pagos pendientes
- comportamiento problemático

El motivo debe registrarse en `comments` o en una tabla de historial si se implementa después.

## Regla: última compra

`last_purchase_date` debe actualizarse cuando una venta válida se confirme o pague, según la política elegida.

No debe actualizarse por ventas canceladas.

## Regla: datos de entrega pueden diferir del cliente

Aunque la venta tenga `client_id`, el envío puede tener:

- otro nombre receptor
- otro teléfono
- otra dirección

Por eso `sale_deliveries` debe guardar datos de envío específicos del intento de entrega.
