# Registrar producto descartado

## Objetivo

Registrar la baja de un producto que ya no podrá venderse, sin manejarlo como una venta de monto cero.

Este caso cubre productos dañados, perdidos, manchados, defectuosos o descartados por cualquier motivo operativo.

## Cuándo aplica

- Producto dañado en tienda.
- Producto manchado o roto.
- Producto defectuoso.
- Producto perdido.
- Producto devuelto por una clienta, pero no apto para volver a venderse.
- Diferencia de inventario detectada en conteo físico.
- Producto que se decide descartar porque ya no tiene valor comercial.

## Tablas involucradas

- `products`
- `inventory_movements`
- `inventory_movement_types`
- `product_holds`, si el producto estaba reservado
- `sales`, opcional si el descarte viene de una devolución
- `sale_details`, opcional si el descarte viene de una venta
- `financial_movements`, no recomendado inicialmente para este caso

## Concepto importante

Descartar un producto no representa una salida de dinero en ese momento.

Sin embargo, sí representa una pérdida económica porque el negocio pierde el valor del costo del producto.

Por eso:

```txt
Descartar producto != salida de caja
Descartar producto = pérdida de inventario
```

La pérdida debe afectar los reportes de ganancia del periodo, pero no debe afectar el flujo de caja como si fuera un pago realizado en ese momento.

## Flujo esperado

1. Buscar el producto.
2. Validar que la cantidad a descartar sea mayor que cero.
3. Validar que exista stock disponible o reservado suficiente, según el caso.
4. Determinar de dónde saldrá el producto:
   - `available_quantity`, si estaba disponible en tienda.
   - `reserved_quantity`, si estaba reservado o fuera de tienda.
5. Obtener el costo histórico del producto:
   - normalmente `unit_cost_with_shipping`.
6. Calcular la pérdida:
   - `total_cost = unit_cost * quantity`.
7. Disminuir la cantidad correspondiente en `products`.
8. Crear un registro en `inventory_movements`.
9. Guardar motivo en comentarios.
10. Usar ese movimiento en reportes de utilidad del periodo.

## Cambios esperados en inventario

### Si el producto estaba disponible

Antes:

```txt
available_quantity = 5
reserved_quantity = 0
```

Se descarta 1 unidad:

```txt
available_quantity = 4
reserved_quantity = 0
```

### Si el producto estaba reservado

Antes:

```txt
available_quantity = 3
reserved_quantity = 1
```

Se descarta la unidad reservada:

```txt
available_quantity = 3
reserved_quantity = 0
```

## Movimiento de inventario esperado

Crear un registro en `inventory_movements` con un tipo como:

```txt
Damaged
Lost
Discarded
```

Ejemplo:

```txt
product_id = 25
inventory_movement_type = Discarded
quantity = -1
unit_cost = 300
total_cost = 300
comments = "Vestido manchado, no apto para venta"
date = 2026-06-06
```

## Campos recomendados en `inventory_movements`

Para que el reporte histórico no dependa del costo actual del producto, se recomienda guardar el costo en el momento del descarte:

```sql
"unit_cost" numeric(10,2),
"total_cost" numeric(10,2)
```

## Impacto en reportes de ganancia

La pérdida por descarte debe restarse de la utilidad del periodo.

Ejemplo:

```txt
Ganancia bruta de ventas: C$20,000
Gastos operativos: C$2,500
Pérdidas por inventario descartado: C$600

Utilidad operativa: C$16,900
```

Fórmula:

```txt
utilidad_operativa =
SUM(sales.gross_profit)
- SUM(gastos_operativos)
- SUM(perdidas_por_inventario)
```

Donde:

```txt
perdidas_por_inventario =
SUM(inventory_movements.total_cost)
WHERE movement_type IN ('Damaged', 'Lost', 'Discarded')
```

## Regla sobre movimientos financieros

Inicialmente, no se recomienda crear un `financial_movement` para este caso.

Motivo:

- No hubo salida de dinero en ese momento.
- El dinero ya salió cuando se compró la mercadería.
- El descarte representa pérdida de inventario, no flujo de caja.

Si más adelante se quiere reflejar contablemente como pérdida, se podría crear un movimiento financiero no-caja con un campo como:

```sql
"affects_cash" bool NOT NULL DEFAULT true
```

Ejemplo:

```txt
financial_movement_type = InventoryLoss
direction = Expense
amount = 300
affects_cash = false
```

Pero para la primera versión, basta con usar `inventory_movements`.

## Reglas de negocio

- No registrar productos descartados como ventas de monto cero.
- Todo descarte debe crear un movimiento de inventario.
- Todo descarte debe guardar motivo o comentario.
- El costo del producto descartado debe quedar congelado en el movimiento.
- El producto descartado debe reducir la utilidad del periodo.
- El producto descartado no debe afectar el flujo de caja del periodo.
- Si el producto estaba reservado, primero debe resolverse la reserva o descontarse desde `reserved_quantity`.
- Si el descarte viene de una devolución, la línea de venta debe reflejar su estado correspondiente, pero el producto no debe volver a stock disponible.

## Casos especiales

### Producto devuelto en buen estado

No se descarta.

Debe registrarse como devolución y volver a inventario disponible:

```txt
available_quantity += quantity
```

### Producto devuelto dañado

Debe registrarse la devolución, pero el producto no debe volver a disponible.

Se crea movimiento de inventario tipo:

```txt
ReturnedDamaged
```

o:

```txt
Discarded
```

### Producto perdido y luego encontrado

Si luego aparece, no se borra el movimiento original.

Se registra un nuevo movimiento:

```txt
Found
```

o:

```txt
Adjustment
```

y se aumenta el inventario disponible.

## Errores esperados

- Producto inexistente.
- Cantidad inválida.
- Stock insuficiente.
- Producto reservado no asociado correctamente.
- Tipo de movimiento inexistente.
- Costo no disponible para calcular la pérdida.

## Resultado esperado

Después de registrar el descarte:

- El stock disponible o reservado baja.
- Existe historial en `inventory_movements`.
- El producto no aparece como vendido.
- La pérdida puede restarse en reportes de ganancia.
- El flujo de caja no se ve afectado como si hubiera ocurrido un pago.
