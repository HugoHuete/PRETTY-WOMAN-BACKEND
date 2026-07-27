# Business Rules - PrettyWoman

Esta carpeta documenta las reglas de negocio principales del sistema PrettyWoman. La intencion es que cada regla sirva como guia para implementar servicios en C#/.NET y para entender el comportamiento esperado del sistema meses despues.

## Modulos documentados

- `inventory.md`: reglas de stock, inventario disponible, reservado, no disponible y movimientos de inventario.
- `product-holds.md`: reglas para reservas comerciales de productos para clientas.
- `product-inventory-issues.md`: reglas para productos danados, sucios, no encontrados, reparados, encontrados o descartados.
- `purchases.md`: reglas para compras, ordenes, trackings y recepcion de productos.
- `sales.md`: reglas generales de ventas, estados, detalles y totales.
- `payments.md`: reglas para pagos parciales, metodos de pago, POS, comisiones y neto recibido.
- `deliveries.md`: reglas para envios, reenvios, costos de envio y estados de entrega.
- `discounts.md`: reglas para promociones, descuentos manuales, prorrateo y descuentos por linea.
- `returns-and-exchanges.md`: reglas para cancelaciones, reembolsos, cambios de talla/producto y efectos en inventario/finanzas.
- `financial-movements.md`: reglas para movimientos reales de dinero, gastos, prestamos, inversiones y relacion con pagos.
- `clients.md`: reglas basicas para clientes, bloqueo y datos de contacto.

## Principio general

El sistema separa cuatro conceptos:

1. **Venta**: lo que se vendio o se intento vender.
2. **Reservas**: productos apartados para clientas, sin considerarlos vendidos todavia.
3. **Inventario operativo**: productos que entran, salen, se danan, se pierden, se reparan, se encuentran o se descartan.
4. **Dinero**: pagos reales, gastos, prestamos, inversiones y comisiones.

No se debe usar una sola tabla para representar estos conceptos al mismo tiempo.
