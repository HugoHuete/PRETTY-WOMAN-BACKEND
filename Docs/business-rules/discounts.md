# Discounts Business Rules

## Objetivo

Manejar promociones temporales y descuentos manuales, permitiendo calcular precio final, descuentos por producto y ganancia por línea.

## Tablas principales

- `discount_campaigns`
- `discount_campaign_products`
- `discount_type`
- `discount_source`
- `sale_details`
- `sales`

## Regla: promociones temporales

Una promoción temporal se registra en `discount_campaigns`.

Debe tener:

- nombre
- fecha de inicio
- fecha final
- estado habilitado/deshabilitado

Solo debe aplicar si:

```txt
enabled = true
current_date >= start_date
current_date <= end_date
```

## Regla: productos en promoción

`discount_campaign_products` define que productos generales (`product_details`) participan en una campania y que descuento aplica.

Cada registro debe indicar:

- campaña
- producto general (`product_detail_id`)
- tipo de descuento
- valor del descuento

Tipos sugeridos:

- `Percentage`: porcentaje.
- `FixedAmount`: monto fijo descontado.
- `FixedPrice`: precio final fijo.

## Regla: evitar promociones traslapadas

Un producto general no deberia tener dos promociones activas al mismo tiempo.

La primera versión puede validar esto desde la app antes de crear una promoción.

Regla recomendada:

```txt
No permitir dos descuentos activos para el mismo producto general (`product_detail_id`) en rangos de fecha que se traslapan.
```

Si en el futuro se decide permitirlo, debe existir una regla de prioridad clara.

## Regla: descuentos manuales

Un descuento manual es aquel dado por decisión de venta en el momento.

Ejemplos:

- Clienta compró varias prendas.
- Producto con detalle menor.
- Descuento autorizado por encargada.

No requiere campaña.

Debe guardarse en la línea de venta con:

- `discount_source_id = Manual`
- `discount_campaign_id = NULL`
- `discount_amount`
- comentario o razón si aplica

## Regla: descuentos globales deben prorratearse

Aunque el descuento se dé sobre la compra completa, debe distribuirse entre las líneas de venta para poder calcular ganancia por producto.

Ejemplo:

```txt
Producto A: C$500
Producto B: C$300
Producto C: C$200
Subtotal: C$1000
Descuento global: C$100
```

Prorrateo proporcional:

```txt
Producto A: C$50
Producto B: C$30
Producto C: C$20
```

Cada línea debe guardar su propio `discount_amount`.

## Regla: descuentos se guardan en `sale_details`

Para la primera versión no se usarán tablas `sale_discounts` ni `sale_detail_discounts`.

Cada línea debe guardar:

- precio original
- fuente del descuento
- campaña si aplica
- monto descontado
- precio final

## Regla: totales de descuento en `sales`

`sales` debe guardar resumen:

- `subtotal_before_discount`
- `total_discount`
- `subtotal`

Donde:

```txt
subtotal_before_discount = suma de precios originales * cantidad
total_discount = suma de descuentos por línea
subtotal = subtotal_before_discount - total_discount
```

## Regla: precio histórico

La venta debe guardar el precio final al momento de vender.

Nunca se debe recalcular una venta histórica usando el precio actual del producto o la campaña actual.

## Regla: productos vendidos con descuento

Para saber cuántos productos se vendieron con descuento:

- contar líneas con `discount_amount > 0`
- o sumar unidades de esas líneas

Esto funciona porque todo descuento global debe quedar prorrateado por línea.
