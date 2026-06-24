# Sales model

## Sales

`sales` representa la cabecera de la venta.

Debe contener:

- fecha
- cliente
- canal
- estado operativo
- estado de pago
- totales
- descuentos totales
- ganancia bruta
- comentarios

No debe contener:

- método de pago único
- terminal POS única
- agencia de envío única

Porque una venta puede tener varios pagos y varios envíos.

Estados operativos de `sale_statuses`:

```txt
Pending
Reserved
ReadyForDelivery
SentForDelivery
Completed
Cancelled
```

El estado operativo no indica si la venta está pagada. El pago se obtiene desde `sale_payments`.

## Sale details

`sale_details` representa las líneas vendidas.

Cada línea debe conservar:

- producto vendido
- cantidad
- precio original
- descuento aplicado
- precio final
- costo histórico
- ganancia de línea
- estado de línea

## Sale payments

Una venta puede tener varios pagos.

Ejemplo:

```txt
Venta C$1000
- C$500 efectivo
- C$500 tarjeta
```

Por eso los pagos viven en `sale_payments`.

Estados de `sale_payment_statuses`:

```txt
Unpaid
PartiallyPaid
Paid
```

`SalePaymentStatusId` debe recalcularse cada vez que se crea, edita o elimina un pago de la venta.

## Sale deliveries

Una venta puede tener varios envíos.

Ejemplo:

```txt
Envío 1: clienta no contestó
Envío 2: entregado
```

Por eso los envíos viven en `sale_deliveries`.

## Cancelaciones

No borrar ventas.

Cambiar estado:

```txt
sale_status = Cancelled
```

y ajustar inventario/finanzas según corresponda.

## Cambios

Si una clienta cambia producto o talla:

- línea original queda `Exchanged`
- nueva línea se agrega a la misma venta
- inventario se ajusta con movimientos
- diferencia de dinero se registra como pago o reembolso

No crear una venta nueva si pertenece al mismo proceso de venta.

## Devoluciones

Una devolución parcial debe afectar:

- estado de la línea
- inventario, si el producto regresa disponible
- movimiento financiero, si hay reembolso

## Descuentos

Los descuentos aplicados deben quedar distribuidos por línea en `sale_details`.

Esto permite calcular ganancia por producto incluso si el descuento fue global.
