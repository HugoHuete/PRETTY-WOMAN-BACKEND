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
- `financial_movements`

## Flujo esperado

1. Registrar dueño/prestamista.
2. Crear préstamo con:
   - fecha
   - monto inicial
   - moneda o monto USD si aplica
   - comentarios
3. Crear movimiento financiero de ingreso asociado al préstamo.
4. Mantener saldo pendiente del préstamo.

## Reglas de negocio

- Un préstamo recibido es entrada de dinero, pero no es ganancia.
- Los pagos de préstamo son egresos, pero no gastos operativos normales.
- El interés pagado puede tratarse separado del capital.

## Errores esperados

- Prestamista inexistente.
- Monto inválido.
- Fecha inválida.
