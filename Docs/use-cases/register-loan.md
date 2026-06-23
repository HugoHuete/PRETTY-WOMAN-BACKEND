# Registrar préstamo

## Objetivo

Registrar un préstamo recibido por la tienda.

## Cuándo aplica

- La tienda recibe dinero prestado.
- El dueño presta capital temporalmente.
- Una tercera persona presta dinero al negocio.

## Tablas involucradas

- `loans`
- `loan_owners`
- `loan_payments`
- `financial_movements`

## Flujo esperado

1. Registrar dueño/prestamista.
2. Crear préstamo con:
   - fecha
   - monto inicial en córdobas
   - comentarios
3. Obtener la tasa bancaria habilitada desde `DollarExchangeRates`.
4. Guardar en el préstamo:
   - `initial_amount` en córdobas
   - `initial_amount_usd` como equivalente usando la tasa bancaria
   - `exchange_rate` usado al registrar el préstamo
5. Crear movimiento financiero de ingreso asociado al préstamo.
6. Calcular el saldo pendiente desde los pagos registrados.

## Endpoints

- `GET /api/v1/loans`: lista préstamos paginados. Permite filtrar por `loanOwnerId` e `isActive`.
- `GET /api/v1/loans/{id}`: obtiene un préstamo con sus pagos.
- `POST /api/v1/loans`: crea un préstamo y su movimiento financiero de ingreso.
- `PUT /api/v1/loans/{id}`: actualiza el préstamo solo si no tiene pagos.
- `DELETE /api/v1/loans/{id}`: elimina el préstamo y su movimiento inicial solo si no tiene pagos.

## Saldo pendiente

`loans` no guarda una columna `balance` como fuente de verdad. El saldo se calcula así:

```txt
Balance = loans.initial_amount - SUM(loan_payments.principal_amount)
```

El interés pagado se calcula separado:

```txt
InterestPaidAmount = SUM(loan_payments.interest_amount)
```

## Movimiento financiero

Al crear el préstamo se debe registrar un movimiento:

- `financial_movement_type`: `LoanReceived`
- `movement_direction`: `In`
- `loan_id`: préstamo creado
- `loan_payment_id`: `null`
- `amount`: monto inicial en córdobas
- `exchange_rate`: tasa bancaria usada por el préstamo

## Actualización y eliminación

- Solo se puede actualizar o eliminar un préstamo que no tenga registros en `loan_payments`.
- Si se actualiza el monto inicial antes de tener pagos, también se actualiza:
  - `initial_amount_usd`
  - el monto del movimiento `LoanReceived`

## Reglas de negocio

- Un préstamo recibido es entrada de dinero, pero no es ganancia.
- Los pagos de préstamo son egresos, pero no gastos operativos normales.
- El interés pagado se guarda en `loan_payments` y se refleja en `financial_movements` como `LoanInterest`.
- El responsable del préstamo debe existir y estar habilitado.

## Errores esperados

- Prestamista inexistente.
- Monto inválido.
- Fecha inválida.
- No existe tasa bancaria habilitada.
- Préstamo con pagos al intentar actualizar o eliminar.
