# Crear movimiento financiero desde pago de venta

## Objetivo

Registrar la entrada real de dinero generada por un pago.

## Cuándo aplica

- Se registra un pago de venta.
- Se recibe efectivo.
- Se recibe transferencia.
- Se recibe pago con POS con comisión.

## Tablas involucradas

- `sale_payments`
- `financial_movements`
- `financial_movement_types`
- `financial_movement_directions`

## Flujo esperado

1. Crear o buscar `sale_payment`.
2. Tomar `net_received_amount`.
3. Crear `financial_movements` con dirección `Income`.
4. Asociar el movimiento a `sale_payment_id`.
5. Guardar comentario o referencia.

## Reglas de negocio

- El movimiento financiero debe representar dinero real recibido.
- Si hay comisión POS, el monto financiero puede ser el neto recibido.
- La comisión puede registrarse como parte del pago o como egreso separado, según decisión contable.
- La venta no genera movimiento financiero directamente; el pago sí.

## Errores esperados

- Pago inexistente.
- Movimiento duplicado para el mismo pago.
- Monto inválido.
