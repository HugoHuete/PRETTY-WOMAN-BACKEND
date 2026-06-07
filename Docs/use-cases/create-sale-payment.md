# Crear pago de venta

## Objetivo

Registrar un pago total o parcial asociado a una venta.

## Cuándo aplica

- La clienta paga toda la venta.
- La clienta paga una parte.
- La venta se paga con varios métodos.
- La venta se paga con efectivo y tarjeta.
- La venta usa un POS con comisión.

## Tablas involucradas

- `sale_payments`
- `sales`
- `payment_methods`
- `payment_terminals`
- `financial_movements`
- `financial_movement_types`
- `financial_movement_directions`

## Flujo esperado

1. Buscar la venta.
2. Validar que la venta no esté cancelada.
3. Validar el método de pago.
4. Si el método requiere terminal POS:
   - validar `payment_terminal_id`
   - obtener porcentaje de comisión
5. Calcular:
   - `amount`
   - `commission_amount`
   - `net_received_amount`
6. Crear registro en `sale_payments`.
7. Crear movimiento financiero de ingreso asociado al `sale_payment`.
8. Recalcular estado de pago de la venta, si manejas estados como:
   - `Unpaid`
   - `PartiallyPaid`
   - `Paid`

## Reglas de negocio

- El pago representa entrada real de dinero.
- La venta no debe usarse como movimiento financiero directo.
- `financial_movements` debe referenciar `sale_payment_id`, no solo `sale_id`.
- El neto recibido puede ser menor que el monto cobrado si hay comisión POS.
- Las comisiones deben quedar congeladas al momento del pago.

## Ejemplo

Venta total:

```txt
C$1,000
```

Pagos:

```txt
C$500 efectivo
C$500 tarjeta POS 4.5%
```

Resultado:

```txt
Pago 1:
amount = 500
commission_amount = 0
net_received_amount = 500

Pago 2:
amount = 500
commission_amount = 22.50
net_received_amount = 477.50
```

## Errores esperados

- Venta no existe.
- Venta cancelada.
- Monto inválido.
- Método de pago inexistente.
- Terminal POS requerido pero no enviado.
- Pago excede saldo pendiente, si decides bloquear sobrepagos.
