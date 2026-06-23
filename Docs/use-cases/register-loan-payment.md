# Registrar pago de préstamo

## Objetivo

Registrar un pago realizado sobre un préstamo.

## Cuándo aplica

- Se paga parte del capital.
- Se paga interés.
- Se liquida un préstamo.

## Tablas involucradas

- `loans`
- `financial_movements`
- `financial_movement_types`
- `financial_movement_directions`

## Flujo esperado

1. Buscar préstamo.
2. Validar monto.
3. Validar que el préstamo esté activo y tenga saldo pendiente.
4. Registrar movimiento financiero de egreso.
5. Asociar movimiento a `loan_id`.
6. Actualizar saldo pendiente del préstamo.
7. Si el saldo llega a cero, cerrar el préstamo con `closed_at`.

## Endpoint

- `POST /api/v1/loans/{id}/payments`: registra un pago sobre el préstamo.

## Movimiento financiero

Al registrar el pago se debe crear un movimiento:

- `financial_movement_type`: `LoanPayment`
- `movement_direction`: `Out`
- `loan_id`: préstamo pagado
- `amount`: monto pagado en córdobas
- `exchange_rate`: tasa guardada en el préstamo

## Actualización del préstamo

- `balance` disminuye por el monto pagado.
- Si `balance` queda en `0`, el préstamo se marca como cerrado con la fecha del pago.
- No se permite pagar más que el saldo pendiente.

## Reglas de negocio

- El pago de préstamo no debe contarse como gasto operativo común si quieres reportes separados.
- El interés sí puede analizarse como costo financiero.
- No se puede pagar más que el saldo pendiente.
- En esta versión el pago se registra contra capital. El interés puede manejarse como una regla futura.

## Errores esperados

- Préstamo inexistente.
- Monto inválido.
- Préstamo ya pagado.
- Pago mayor que el saldo pendiente.
