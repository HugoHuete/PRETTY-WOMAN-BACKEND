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
3. Registrar movimiento financiero de egreso.
4. Asociar movimiento a `loan_id`.
5. Actualizar saldo pendiente del préstamo.
6. Si incluye interés, registrar interés pagado o desglose según el modelo.

## Reglas de negocio

- El pago de préstamo no debe contarse como gasto operativo común si quieres reportes separados.
- El interés sí puede analizarse como costo financiero.
- No se puede pagar más que el saldo pendiente salvo que se permita sobrepago.

## Errores esperados

- Préstamo inexistente.
- Monto inválido.
- Préstamo ya pagado.
