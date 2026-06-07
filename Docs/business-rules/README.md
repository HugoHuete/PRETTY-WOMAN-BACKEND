# Business Rules - PrettyWoman

Esta carpeta documenta las reglas de negocio principales del sistema PrettyWoman. La intención es que cada regla sirva como guía para implementar servicios en C#/.NET y para entender el comportamiento esperado del sistema meses después.

## Módulos documentados

- `inventory.md`: reglas de stock, inventario disponible, reservado, dañado, perdido y movimientos de inventario.
- `purchases.md`: reglas para compras, órdenes, trackings y recepción de productos.
- `sales.md`: reglas generales de ventas, estados, detalles y totales.
- `payments.md`: reglas para pagos parciales, métodos de pago, POS, comisiones y neto recibido.
- `deliveries.md`: reglas para envíos, reenvíos, costos de envío y estados de entrega.
- `discounts.md`: reglas para promociones, descuentos manuales, prorrateo y descuentos por línea.
- `returns-and-exchanges.md`: reglas para cancelaciones, reembolsos, cambios de talla/producto y efectos en inventario/finanzas.
- `financial-movements.md`: reglas para movimientos reales de dinero, gastos, préstamos, inversiones y relación con pagos.
- `clients.md`: reglas básicas para clientes, bloqueo y datos de contacto.

## Principio general

El sistema separa tres conceptos:

1. **Venta**: lo que se vendió o se intentó vender.
2. **Inventario**: qué productos entran, salen, se reservan, se dañan o se pierden.
3. **Dinero**: pagos reales, gastos, préstamos, inversiones y comisiones.

No se debe usar una sola tabla para representar estos tres conceptos al mismo tiempo.
