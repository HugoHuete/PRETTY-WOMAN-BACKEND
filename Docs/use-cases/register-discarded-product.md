# Registrar producto descartado

## Objetivo

Registrar la baja definitiva de un producto que ya no podra venderse, sin manejarlo como una venta de monto cero.

Este caso cubre productos danados, perdidos, manchados, defectuosos o descartados por cualquier motivo operativo.

## Cuando aplica

- Producto danado en tienda.
- Producto manchado o roto.
- Producto defectuoso.
- Producto perdido y confirmado como irrecuperable.
- Producto devuelto por una clienta, pero no apto para volver a venderse.
- Diferencia de inventario detectada en conteo fisico.
- Producto que se decide descartar porque ya no tiene valor comercial.

## Tablas involucradas

- `products`
- `product_inventory_issues`, si el producto ya estaba no disponible temporalmente
- `inventory_movements`
- `inventory_movement_types`
- `sales`, opcional si el descarte viene de una devolucion
- `sale_products`, opcional si el descarte viene de una venta
- `financial_movements`, no recomendado inicialmente para este caso

## Concepto importante

Descartar un producto no representa una salida de dinero en ese momento.

Sin embargo, si representa una perdida economica porque el negocio pierde el valor del costo del producto.

Por eso:

```txt
Descartar producto != salida de caja
Descartar producto = perdida de inventario
```

La perdida debe afectar los reportes de ganancia del periodo, pero no debe afectar el flujo de caja como si fuera un pago realizado en ese momento.

## Flujo esperado

1. Buscar el producto.
2. Validar que la cantidad a descartar sea mayor que cero.
3. Determinar de donde saldra el producto:
   - `available_quantity`, si todavia estaba disponible para venta.
   - `unavailable_quantity`, si ya tenia un `product_inventory_issue` abierto.
4. Validar que exista cantidad suficiente en el origen correspondiente.
5. Obtener el costo historico del producto, normalmente `unit_cost_nio`.
6. Calcular la perdida: `total_cost = unit_cost * quantity`.
7. Disminuir la cantidad correspondiente en `products`.
8. Crear `inventory_movement` tipo `Discarded`.
9. Si existe issue, cambiarlo a `Discarded`, colocar `resolved_at` y relacionar el movimiento con `product_inventory_issue_id`.
10. Guardar motivo en comentarios.
11. Usar ese movimiento en reportes de utilidad del periodo.

## Cambios esperados en inventario

### Si el producto estaba disponible

Antes:

```txt
available_quantity = 5
reserved_quantity = 0
unavailable_quantity = 0
```

Se descarta 1 unidad:

```txt
available_quantity = 4
reserved_quantity = 0
unavailable_quantity = 0
```

### Si el producto estaba en un issue operativo

Antes:

```txt
available_quantity = 3
reserved_quantity = 0
unavailable_quantity = 1
```

Se descarta la unidad no disponible:

```txt
available_quantity = 3
reserved_quantity = 0
unavailable_quantity = 0
```

No se vuelve a disminuir `available_quantity`, porque ya se disminuyo al abrir el issue.

## Movimiento de inventario esperado

Crear un registro en `inventory_movements` con tipo:

```txt
Discarded
```

Ejemplo:

```txt
product_id = 25
product_inventory_issue_id = 8
inventory_movement_type = Discarded
quantity = 1
unit_cost = 300
total_cost = 300
comments = "Vestido manchado, no apto para venta"
date = 2026-06-06
```

La cantidad del movimiento debe guardarse positiva. El tipo de movimiento define que representa una salida o perdida.

## Campos recomendados en `inventory_movements`

Para que el reporte historico no dependa del costo actual del producto, se recomienda guardar el costo en el momento del descarte:

```sql
"unit_cost" numeric(10,2),
"total_cost" numeric(10,2)
```

## Impacto en reportes de ganancia

La perdida por descarte debe restarse de la utilidad del periodo.

Ejemplo:

```txt
Ganancia bruta de ventas: C$20,000
Gastos operativos: C$2,500
Perdidas por inventario descartado: C$600

Utilidad operativa: C$16,900
```

Formula:

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
- El dinero ya salio cuando se compro la mercaderia.
- El descarte representa perdida de inventario, no flujo de caja.

Si mas adelante se quiere reflejar contablemente como perdida, se podria crear un movimiento financiero no-caja con un campo como:

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

Pero para la primera version, basta con usar `inventory_movements`.

## Reglas de negocio

- No registrar productos descartados como ventas de monto cero.
- Todo descarte debe crear un movimiento de inventario.
- Todo descarte debe guardar motivo o comentario.
- El costo del producto descartado debe quedar congelado en el movimiento.
- El producto descartado debe reducir la utilidad del periodo.
- El producto descartado no debe afectar el flujo de caja del periodo.
- Si el producto estaba reservado, primero debe resolverse la reserva comercial.
- Si el producto estaba en `unavailable_quantity`, debe cerrarse su `product_inventory_issue`.
- Si el descarte viene de una devolucion, la linea de venta debe reflejar su estado correspondiente, pero el producto no debe volver a stock disponible.

## Casos especiales

### Producto devuelto en buen estado

No se descarta.

Debe registrarse como devolucion y volver a inventario disponible:

```txt
available_quantity += quantity
```

### Producto devuelto danado

Debe registrarse la devolucion, pero el producto no debe volver a disponible.

Se abre un `product_inventory_issue` tipo `Damaged` o se registra directamente `Discarded` si ya se sabe que no puede venderse.

### Producto perdido y luego encontrado

Si luego aparece, no se borra el movimiento original.

Se registra un nuevo movimiento:

```txt
Found
```

y se aumenta el inventario disponible desde el issue:

```txt
unavailable_quantity -= quantity
available_quantity += quantity
```

## Errores esperados

- Producto inexistente.
- Cantidad invalida.
- Stock insuficiente.
- Issue inexistente al cerrar.
- Issue ya cerrado.
- Tipo de movimiento inexistente.
- Costo no disponible para calcular la perdida.

## Resultado esperado

Despues de registrar el descarte:

- El stock disponible o no disponible baja.
- Existe historial en `inventory_movements`.
- El producto no aparece como vendido.
- La perdida puede restarse en reportes de ganancia.
- El flujo de caja no se ve afectado como si hubiera ocurrido un pago.
