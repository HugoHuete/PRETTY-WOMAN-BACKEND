# Frontend Handoff - Pretty Woman Admin

Este documento sirve como puente entre el backend y el futuro frontend administrativo.
Por ahora vive en el repo del backend porque aqui estan las reglas de negocio, casos de uso y contratos API.
Cuando exista `Pretty-Woman_Frontend_Admin`, puede copiarse o dividirse alla en documentos de producto/UI.

## Alcance inicial

El frontend inicial sera una aplicacion administrativa interna en React.
No incluye tienda publica para clientas en esta etapa.

Si mas adelante se construye una experiencia publica para clientas, conviene crear otro proyecto separado, por ejemplo `Pretty-Woman_Frontend_Store`, porque tendra objetivos distintos: catalogo visual, carrito, checkout, SEO y rutas publicas.

## Usuarios

### Admin

Responsable de administrar el sistema completo.

Puede trabajar con:

- Usuarios y accesos.
- Productos, categorias, subcategorias y tallas.
- Proveedores y compras.
- Inventario.
- Ventas, reservas, descuentos, cambios y cancelaciones.
- Clientes.
- Finanzas, gastos, prestamos y reportes.
- Catalogos operativos como agencias de envio, terminales de pago y categorias de gasto.

### Vendedor

Responsable de la operacion diaria de venta.

Puede trabajar con:

- Login.
- Busqueda de productos.
- Consulta de stock disponible.
- Clientes.
- Creacion de ventas.
- Reservas de productos.
- Pagos de venta.
- Entregas/envios.
- Cambios o cancelaciones segun permisos definidos.

## Modulos del frontend admin

| Modulo | Objetivo | Roles principales | Estado esperado |
|---|---|---|---|
| Autenticacion | Iniciar sesion y administrar acceso | Admin, Vendedor | Necesario para MVP |
| Dashboard | Ver resumen operativo del negocio | Admin, Vendedor | Necesario para MVP |
| Productos | Consultar catalogo, detalle, filtros y stock | Admin, Vendedor | Necesario para MVP |
| Catalogos | Gestionar categorias, subcategorias y tallas | Admin | Necesario para inventario |
| Clientes | Crear, editar, bloquear y consultar clientas | Admin, Vendedor | Necesario para ventas |
| Compras | Crear ordenes, tracking y recepcion de productos | Admin | Necesario para inventario |
| Inventario | Ver disponibilidad y problemas de inventario | Admin, Vendedor | Necesario para operacion |
| Ventas | Crear ventas, detalles, pagos y entregas | Admin, Vendedor | Necesario para MVP |
| Reservas | Registrar ventas confirmadas para retiro o envío futuro | Admin, Vendedor | Necesario para operacion |
| Descuentos | Gestionar campanas y descuentos manuales | Admin, Vendedor con permisos | Necesario para ventas |
| Finanzas | Ver balance, movimientos, gastos y prestamos | Admin | Posterior al MVP si se prioriza ventas primero |
| Configuracion | Agencias, terminales, proveedores y categorias de gasto | Admin | Segun necesidad operativa |

## Mapa inicial de pantallas

| Modulo | Pantalla | Roles | Acciones principales | Datos necesarios | Endpoints relacionados |
|---|---|---|---|---|---|
| Autenticacion | Login | Admin, Vendedor | Iniciar sesion | Credenciales | `POST /api/v1/auth/login` |
| Autenticacion | Usuarios | Admin | Crear usuario, desbloquear usuario | Usuarios, roles/permisos | `POST /api/v1/auth/users`, `POST /api/v1/auth/users/{id}/unlock` |
| Dashboard | Resumen | Admin, Vendedor | Ver ventas, pagos, reservas, entregas e incidencias | Ventas, cobros, reservas, entregas e incidencias; el bloque financiero es exclusivo de Admin | `GET /api/v1/dashboard/summary` |
| Productos | Lista de productos | Admin, Vendedor | Buscar, filtrar, paginar, abrir detalle | Productos, categoria, subcategoria, talla, stock | `GET /api/v1/product-details` |
| Productos | Detalle de producto | Admin, Vendedor | Ver informacion completa y disponibilidad | Producto, variantes/detalle, stock | `GET /api/v1/product-details/{productDetailId}` |
| Catalogos | Categorias | Admin | Listar, crear, editar | Categorias | `GET /api/v1/categories`, `POST /api/v1/categories`, `PUT /api/v1/categories/{id}` |
| Catalogos | Subcategorias | Admin | Listar, filtrar por categoria, crear, editar | Subcategorias, categorias | `GET /api/v1/subcategories`, `GET /api/v1/categories/{id}/subcategories`, `POST /api/v1/subcategories`, `PUT /api/v1/subcategories/{id}` |
| Catalogos | Tallas | Admin | Listar, crear, editar | Tallas | `GET /api/v1/sizes`, `POST /api/v1/sizes`, `PUT /api/v1/sizes/{id}` |
| Clientes | Lista de clientes | Admin, Vendedor | Buscar, ver detalle, crear, editar | Clientes | `GET /api/v1/clients`, `POST /api/v1/clients`, `PUT /api/v1/clients/{id}` |
| Clientes | Estado de cliente | Admin | Bloquear, desbloquear | Cliente, motivo de bloqueo | `PATCH /api/v1/clients/{id}/block`, `PATCH /api/v1/clients/{id}/unblock` |
| Compras | Ordenes | Admin | Listar, crear, editar, ver detalle | Ordenes, proveedores, tracking | `GET /api/v1/orders`, `POST /api/v1/orders`, `PUT /api/v1/orders/{id}` |
| Compras | Tracking de orden | Admin | Agregar, editar, eliminar tracking | Orden, numeros de tracking | `GET /api/v1/orders/{id}/tracking-numbers`, `POST /api/v1/orders/{id}/tracking-numbers`, `PUT /api/v1/orders/{id}/tracking-numbers/{trackingId}`, `DELETE /api/v1/orders/{id}/tracking-numbers/{trackingId}` |
| Compras | Recepcion de productos | Admin | Registrar recepcion de productos de una orden | Orden, productos recibidos, cantidades | `POST /api/v1/orders/{orderId}/receipts` |
| Proveedores | Proveedores | Admin | Listar, crear, editar | Proveedores | `GET /api/v1/suppliers`, `POST /api/v1/suppliers`, `PUT /api/v1/suppliers/{id}` |
| Descuentos | Campanas | Admin | Listar, crear, editar, deshabilitar | Campanas de descuento | `GET /api/v1/discountcampaigns`, `POST /api/v1/discountcampaigns`, `PUT /api/v1/discountcampaigns/{id}`, `PATCH /api/v1/discountcampaigns/{id}/disable` |
| Finanzas | Balance | Admin | Ver balance actual | Balance financiero | `GET /api/v1/finances/current-balance` |
| Finanzas | Movimientos | Admin | Listar, filtrar, crear, editar, eliminar | Movimientos, tipos de movimiento | `GET /api/v1/finances/movements`, `POST /api/v1/finances/movements`, `PUT /api/v1/finances/movements/{id}`, `DELETE /api/v1/finances/movements/{id}` |
| Finanzas | Prestamos | Admin | Listar, crear, editar, eliminar, registrar pagos | Prestamos, duenos, pagos | `GET /api/v1/loans`, `POST /api/v1/loans`, `PUT /api/v1/loans/{id}`, `DELETE /api/v1/loans/{id}`, `POST /api/v1/loans/{id}/payments` |
| Finanzas | Duenos de prestamo | Admin | Listar, crear, editar | Duenos de prestamo | `GET /api/v1/loanowners`, `POST /api/v1/loanowners`, `PUT /api/v1/loanowners/{id}` |
| Configuracion | Agencias de envio | Admin | Listar, crear, editar | Agencias de envio | `GET /api/v1/deliveryagencies`, `POST /api/v1/deliveryagencies`, `PUT /api/v1/deliveryagencies/{id}` |
| Configuracion | Terminales de pago | Admin | Listar, crear, editar | Terminales POS | `GET /api/v1/paymentterminals`, `POST /api/v1/paymentterminals`, `PUT /api/v1/paymentterminals/{id}` |
| Configuracion | Categorias de gasto | Admin | Listar, crear, editar | Categorias de gasto | `GET /api/v1/expensecategories`, `POST /api/v1/expensecategories`, `PUT /api/v1/expensecategories/{id}` |

## Contratos operativos confirmados

Los endpoints siguientes ya están implementados y son la base de las pantallas críticas. Todos requieren JWT. `Employee` es el rol técnico de Vendedor.

### Ventas, pagos y envíos

| Flujo | Endpoint | Rol | Resultado |
|---|---|---|---|
| Listar / abrir venta | `GET /api/v1/sales`, `GET /api/v1/sales/{id}` | Admin, Vendedor | La consulta individual incluye productos, pagos, prendas en selección y `deliveries`. |
| Crear venta o reserva | `POST /api/v1/sales` | Admin, Vendedor | Devuelve `201` y el id. Para una reserva confirmada enviar `saleStatusId: 2` (`Reserved`). |
| Registrar abono o pago | `POST /api/v1/sales/{id}/payment-movements` | Admin, Vendedor | Devuelve `201` y el id. El backend calcula el total y estado de pago. |
| Crear envío | `POST /api/v1/sales/{id}/deliveries` | Admin, Vendedor | Devuelve `201` y el id; deja la venta `ReadyForDelivery` y reserva productos si estaba `Pending`. |
| Despachar | `POST /api/v1/sales/{id}/deliveries/{deliveryId}/send` | Admin, Vendedor | Cambia el envío a `Sent` y mueve la reserva fuera de inventario. |
| Completar, fallar o cancelar envío | `.../complete`, `.../fail`, `.../cancel` | Admin | Son transiciones posteriores o sensibles. |
| Conciliar cobro COD | `GET /api/v1/deliveryagencyreconciliations/pending-deliveries`, `POST /api/v1/deliveryagencyreconciliations` | Admin | Un envío `Sent` solo se completa si la agencia recauda el saldo total. |
| Corregir o cancelar venta | `PATCH /api/v1/sales/{id}`, `PUT /api/v1/sales/{id}/products`, `POST /api/v1/sales/{id}/cancel` | Admin | No mostrar estas acciones a Vendedor. |

Para crear una venta, `products` y `selectionProducts` son listas distintas. Cada elemento de `products` requiere `productId`, `quantity`, `discountAmount` y `discountSourceId`. El pago inicial es opcional en `paymentMovements`; cada pago requiere `paymentMethodId`, `productAmount` y `shippingAmount`.

En recepcion de compras, la UI debe bloquear cantidades mayores a lo pendiente salvo que el usuario marque explícitamente la línea como sobrante. Para sobrantes enviar `isSurplus: true` y `comments`; conviene mostrar confirmacion visual porque esa acción puede dejar `receivedQuantity` mayor que `quantity`.

`PUT /api/v1/sales/{id}/products` recibe la composición final completa. Cada línea existente que se desee conservar debe incluir su `saleProductId`, obtenido de `GET /api/v1/sales/{id}`; omitir una línea existente la elimina y enviar una línea sin `saleProductId` agrega un producto nuevo. El backend conserva los precios y costos congelados de las líneas identificadas y mueve inventario únicamente por la diferencia de cantidad.

La cantidad de una línea sigue representando lo vendido originalmente aun cuando parte haya sido devuelta. Al reemplazar productos, el backend excluye automáticamente del compromiso de inventario las unidades ya recibidas por devoluciones o cambios; el frontend no debe restarlas del `quantity` enviado.

`shippingAmount` mayor que cero exige `saleDeliveryId`; primero debe existir el envío. Para pago con tarjeta (`paymentMethodId: 3`) también es obligatorio `paymentTerminalId`; en efectivo o transferencia no debe enviarse terminal. La respuesta de detalle expone el total calculado y cada `paymentMovement.grossAmount`; la UI no debe recalcularlos.

Una reserva con pago no usa `ProductHold`: es una venta con `saleStatusId: 2` (`Reserved`). Permanece activa hasta que el negocio la cancele. `selectionHolds` representa únicamente prendas enviadas para prueba, talla o selección.

Para mostrar existencias, considerar que `Reserved` y `ReadyForDelivery` usan `reservedQuantity`; `SentForDelivery` ya está fuera del inventario activo. Un envío fallido devuelve a reservado solamente las unidades que todavía continúan fuera. La API rechazará esta transición si hay una devolución pendiente de recepción física o un cambio solicitado pendiente de entrega física; la UI debe resolver o cancelar primero esa operación.

### Incidencias de inventario

| Flujo | Endpoint | Rol | Resultado |
|---|---|---|---|
| Listar / consultar | `GET /api/v1/product-inventory-issues`, `GET /api/v1/product-inventory-issues/{id}` | Admin, Vendedor | Filtros: producto, detalle, tipo y estado. |
| Abrir incidencia | `POST /api/v1/product-inventory-issues` | Admin | Devuelve `201` y el id; mueve la cantidad de disponible a no disponible. |
| Resolver | `PATCH /api/v1/product-inventory-issues/{id}/resolution` | Admin | Devuelve la incidencia resuelta. |
| Cancelar incidencia abierta | `DELETE /api/v1/product-inventory-issues/{id}` | Admin | La deja en estado `Cancelled` y repone disponibilidad. |

Al crear una incidencia se envían `productId`, `productInventoryIssueTypeId`, `quantity`, `issueDate` opcional y `comments` opcional. Los tipos son `Damaged=1`, `Dirty=2`, `Missing=3`, `UnderReview=4` y `Repairing=5`. Mientras esté `Open=1`, la cantidad queda automáticamente fuera de disponibilidad. Al resolver, usar `ResolvedToAvailable=2`, `Discarded=3`, `ConfirmedLost=4` o `Cancelled=5`.

Para ajustes de inventario, cargar los catalogos con `GET /api/v1/inventory-catalogs/adjustment-reasons` y `GET /api/v1/inventory-catalogs/stock-buckets`. Ambos devuelven `{ id, name }` y evitan hardcodear motivos o buckets en la UI. Para precargar origen/destino segun el motivo seleccionado, usar `GET /api/v1/inventory-catalogs/adjustment-reason-suggestions`; devuelve cada motivo con `description` y `suggestedMovements`, donde cada sugerencia incluye bucket origen, bucket destino y una descripcion corta. Estas sugerencias ayudan a la UI, pero el backend sigue validando la transicion final. Si un motivo viene sin sugerencias, como `PurchaseSurplus`, la UI debe mostrar la descripcion del flujo recomendado en vez de prellenar buckets.

### Errores y estados que debe manejar la UI

- Un `400` devuelve `ProblemDetails`: usar `title` y `detail` para el mensaje, no asumir un texto fijo. Suele indicar regla de negocio: saldo insuficiente, envío inválido o monto incorrecto.
- Un `404` indica que el id enviado no existe o ya no pertenece al recurso indicado. Un `401`/`403` implica sesión expirada o falta de permiso.
- Tras cualquier `POST`, `PATCH` o transición, recargar `GET /api/v1/sales/{id}` o actualizar con su resultado. Así la interfaz refleja los estados que calcula el backend.
- Swagger ya muestra los resúmenes, DTOs y respuestas principales de estos endpoints en desarrollo mediante `/swagger`.

## Pendiente que no bloquea estas pantallas

No existe todavía una ruta específica para cancelar una sola línea de venta. Por ahora, el frontend no debe mostrar esa acción; puede usar cancelación completa de venta o los flujos administrativos de devolución/cambio cuando correspondan.

## Reglas que el frontend debe respetar

- La UI no debe calcular reglas criticas de negocio como totales finales, stock disponible, prorrateos de descuento, comisiones o estados finales. Debe mostrar lo que devuelve el backend.
- El frontend puede hacer validaciones rapidas de formulario, pero el backend sigue siendo la fuente de verdad.
- Las acciones destructivas o sensibles deben pedir confirmacion visual: cancelar venta, eliminar movimiento financiero, desbloquear usuario, bloquear cliente, descartar producto.
- El vendedor debe ver solo acciones operativas necesarias para vender, reservar, cobrar y entregar.
- El admin debe poder ver y gestionar configuraciones, finanzas y catalogos.
- Las pantallas de venta y reserva deben dejar claro el estado del producto: disponible, reservado, vendido, no disponible o pendiente de recepcion.

## Decisiones tomadas

- El frontend inicial sera solo administrativo.
- Los roles iniciales son Admin y Vendedor.
- En la API actual, el rol tecnico `Employee` representa al Vendedor. No se debe asumir que un
  Vendedor puede usar cualquier endpoint autenticado: cada modulo y accion se valida en el backend.
- El Vendedor puede consultar productos, inventario e incidencias; consultar categorias, subcategorias
  y tallas para filtrar el catalogo; crear y editar clientes; consultar agencias de envio y terminales
  de pago; y consultar o crear ventas, pagos y entregas. Puede crear envios (`ReadyForDelivery`) y
  marcar como enviados los envios pendientes (`Sent`). Un descuento manual se aplica como parte de
  la creacion de una venta.
- El Vendedor no puede crear ni actualizar catalogos, compras, configuracion, campanas de descuento,
  finanzas, incidencias de inventario, ni bloquear clientes. Las correcciones, cancelaciones,
  devoluciones, reembolsos, cambios y transiciones posteriores a `Sent` de una venta son exclusivas
  de Admin.
- Una venta con pago parcial no se entrega a la clienta. Una agencia con cobro contra entrega puede
  transportar el pedido, pero solo se completa cuando recauda el total pendiente; de lo contrario el
  envio se marca fallido, sin cobro parcial.
- Admin conserva acceso completo a todos los modulos.
- La tienda publica para clientas, si se construye, ira en otro proyecto.
- El mapa de pantallas puede empezar en este backend como handoff y moverse al repo frontend cuando exista.

## Pendientes para iniciar React

- Confirmar stack: Vite + React, TypeScript, router, libreria UI y manejo de estado.
- Definir si se usara Tailwind/shadcn, MUI u otra libreria.
- Definir permisos exactos por rol.
- Confirmar endpoints faltantes para ventas, reservas e inventario operativo.
- Crear `Pretty-Woman_Frontend_Admin`.
- Mover o copiar este documento a `Pretty-Woman_Frontend_Admin/Docs/product/admin-screen-map.md`.
- Definir layout base: sidebar, topbar, area de contenido, tablas, formularios y modales.

## Contrato del dashboard

- `GET /api/v1/dashboard/summary`
- Requiere JWT de Admin o Vendedor (`Employee`).
- Query opcional: `fromDate` y `toDate` en formato `YYYY-MM-DD`. Ambas fechas son inclusivas y, si se omiten, el período es el día actual en hora de Nicaragua.
- Devuelve ventas no canceladas, cobros registrados, reservas activas creadas en el período, entregas creadas en el período e incidencias de inventario abiertas registradas en el período.
- El bloque `financial` solo se devuelve a Admin e incluye ingresos, egresos y balance del período. El frontend de Vendedor no debe depender de ese campo.
- Las listas `payments.byPaymentMethod` y `operations.deliveriesByStatus` pueden estar vacías; el frontend debe representarlas como cero, no como error.
