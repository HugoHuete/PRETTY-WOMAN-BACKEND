# Registrar pago de préstamo

## Objetivo

Registrar un pago realizado sobre un préstamo.

## Cuándo aplica

- Se paga parte del capital.
- Se paga interés.
- Se liquida un préstamo.

## Tablas involucradas

- `loans`
- `loan_payments`
- `financial_movements`
- `financial_movement_types`
- `financial_movement_directions`

## Flujo esperado

1. Buscar préstamo.
2. Calcular saldo pendiente desde `loan_payments`.
3. Validar monto de capital.
4. Validar monto de interés, si se envía.
5. Validar que el préstamo esté activo y tenga saldo pendiente.
6. Crear registro en `loan_payments`.
7. Registrar movimiento financiero de egreso por capital.
8. Si hay interés, registrar movimiento financiero de egreso por interés.
9. Asociar ambos movimientos a `loan_id` y `loan_payment_id`.
10. Si el saldo calculado llega a cero, cerrar el préstamo con `closed_at`.

## Endpoints

- `POST /api/v1/loans/{id}/payments`: registra un pago sobre el préstamo.
- `PUT /api/v1/loans/{id}/payments/{paymentId}`: edita un pago de préstamo existente.

## Request

`amount` representa el pago a capital. `interestAmount` representa el interés pagado.

```json
{
  "createdAt": "2026-06-22T10:00:00Z",
  "amount": 1000,
  "interestAmount": 50,
  "comments": "Abono con interés"
}
```

## LoanPayment

Cada pago crea un registro en `loan_payments`:

- `loan_id`: préstamo pagado
- `created_at`: fecha del pago
- `principal_amount`: monto pagado a capital
- `interest_amount`: monto pagado de interés
- `exchange_rate`: tasa guardada en el préstamo
- `comments`: comentario del pago

## Movimiento financiero

Al registrar el pago se crea un unico movimiento:

- `financial_movement_type`: `LoanPayment`
- `movement_direction`: `Out`
- `loan_id`: prestamo pagado
- `loan_payment_id`: pago creado
- `amount`: `principal_amount + interest_amount` en cordobas
- `exchange_rate`: tasa guardada en el prestamo

## Saldo pendiente

`loans` no guarda `balance`. El saldo se calcula siempre desde los pagos:

```txt
Balance = loans.initial_amount - SUM(loan_payments.principal_amount)
```

El interes no disminuye el saldo; queda registrado en `loan_payments.interest_amount`, mientras que el movimiento financiero refleja el total pagado.

## Edición de pagos

- El `paymentId` corresponde al id de `loan_payments`.
- Al editar un pago se actualiza `loan_payments` y su unico movimiento financiero asociado, cuyo monto es `amount + interestAmount`.
- Después de editar, `closed_at` se recalcula con base en el saldo resultante.

## Reglas de negocio

- El pago de préstamo no debe contarse como gasto operativo común si quieres reportes separados.
- El interés sí puede analizarse como costo financiero.
- No se puede pagar más capital que el saldo pendiente.
- El interés pagado se reporta sumando `loan_payments.interest_amount`.

## Errores esperados

- Préstamo inexistente.
- Pago inexistente.
- Monto de capital inválido.
- Monto de interés negativo.
- Préstamo ya pagado.
- Pago de capital mayor que el saldo pendiente.
