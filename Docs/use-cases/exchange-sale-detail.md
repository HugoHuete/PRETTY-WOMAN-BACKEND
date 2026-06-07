# Cambiar producto vendido

## Objetivo

Registrar el cambio de un producto vendido por otra talla u otro producto.

## Cuándo aplica

- Cambio por otra talla.
- Cambio por otro color.
- Cambio por otro producto.
- Cambio con pago de diferencia.
- Cambio con devolución parcial.

## Tablas involucradas

- `sales`
- `sale_details`
- `sale_detail_statuses`
- `products`
- `inventory_movements`
- `financial_movements`
- `sale_payments`

## Flujo esperado

1. Buscar venta.
2. Buscar línea original.
3. Cambiar estado de línea original a `Exchanged`.
4. Devolver inventario del producto original si regresa en buen estado.
5. Crear movimiento de inventario tipo `ExchangeReturn`.
6. Crear nueva línea en `sale_details` con el nuevo producto.
7. Descontar inventario del nuevo producto.
8. Crear movimiento de inventario tipo `ExchangeSale`.
9. Calcular diferencia de precio:
   - si nuevo producto cuesta más, crear pago adicional
   - si nuevo producto cuesta menos, registrar reembolso o crédito
10. Recalcular totales y ganancia de la venta.

## Reglas de negocio

- No crear una venta nueva si el cambio pertenece a la misma transacción comercial.
- La venta original debe conservar todo el historial.
- El producto original debe quedar con estado `Exchanged`.
- El producto nuevo debe quedar como línea activa.
- Si el producto original vuelve dañado, no debe regresar a stock disponible.

## Errores esperados

- Línea original no activa.
- Stock insuficiente del nuevo producto.
- Diferencia de pago inválida.
- Venta cancelada.
