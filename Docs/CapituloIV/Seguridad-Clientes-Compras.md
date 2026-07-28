# 4.8 Autorización de Clientes y Compras

## 4.8.1 Problema atendido

La autenticación confirma quién inició sesión, pero por sí sola no determina qué operaciones
puede realizar. Antes de este incremento, las páginas de Clientes y Compras utilizaban
únicamente `[Authorize]`. En consecuencia, cualquier usuario autenticado podía consultar y
también intentar ejecutar operaciones sensibles como editar clientes, cobrar, recibir
mercancía o pagar a proveedores.

Para separar esas responsabilidades se aplicó autorización por políticas en dos niveles:

1. **Ruta:** impide abrir directamente una página sin el permiso requerido.
2. **Acción:** oculta botones y enlaces que iniciarían una operación no autorizada.

Los permisos se almacenan como claims del usuario y son resueltos por el núcleo de
autorización descrito en la sección general de seguridad. Este módulo consume las políticas;
no crea tablas ni migraciones propias.

## 4.8.2 Políticas del módulo de Clientes

| Política | Alcance |
|----------|---------|
| `clientes.ver` | Listado de clientes y cuentas por cobrar. |
| `clientes.editar` | Crear o editar la ficha de un cliente. |
| `clientes.cobrar` | Abrir y ejecutar el registro de un cobro. |

Las páginas `Clientes` y `CuentasPorCobrar` requieren `clientes.ver`.
`ClienteEditor` requiere `clientes.editar`, mientras que `RegistrarCobro` requiere
`clientes.cobrar`.

En los listados también se aplicó autorización a nivel de acción. El enlace **Nuevo cliente**
y los enlaces **Editar** se muestran únicamente con `clientes.editar`. Los botones
**Cobrar** se muestran únicamente con `clientes.cobrar`.

## 4.8.3 Políticas del módulo de Compras

| Política | Alcance |
|----------|---------|
| `compras.ver` | Listados de compras, proveedores y cuentas por pagar. |
| `compras.recibir` | Crear y editar compras o proveedores, y recibir mercancía. |
| `compras.pagar` | Abrir y ejecutar el pago a un proveedor. |
| `compras.devolver` | Reservada para la operación de devolución al proveedor. |

Las páginas de consulta (`Compras`, `Proveedores` y `CuentasPorPagar`) requieren
`compras.ver`. Los editores de compras y proveedores requieren `compras.recibir`.
`RegistrarPago` requiere `compras.pagar`.

Los enlaces **Nueva compra**, **Nuevo proveedor** y **Editar**, junto con las acciones de
guardar, recibir y anular un borrador, solo aparecen con `compras.recibir`. Los enlaces y
botones de pago solo aparecen con `compras.pagar`.

El permiso `compras.devolver` forma parte del catálogo acordado, pero no se aplicó a una
pantalla inexistente en esta rama. Se utilizará cuando la interfaz de devolución al proveedor
sea integrada.

## 4.8.4 Criterios de comprobación

La verificación funcional debe realizarse después de integrar el núcleo de permisos:

1. Un cajero sin permisos de Clientes o Compras no puede abrir `/clientes`,
   `/clientes/cuentas-por-cobrar`, `/compras` ni `/compras/cuentas-por-pagar`.
2. Un usuario con `clientes.ver`, pero sin `clientes.editar` ni `clientes.cobrar`, puede
   consultar los listados y no ve las acciones **Nuevo cliente**, **Editar** o **Cobrar**.
3. Un usuario sin `clientes.cobrar` es rechazado al escribir manualmente una URL de cobro.
4. Un usuario con `compras.ver`, pero sin `compras.recibir` ni `compras.pagar`, consulta los
   listados sin ver acciones de creación, edición, recepción o pago.
5. Un usuario sin `compras.pagar` es rechazado al escribir manualmente una URL de pago.
6. El Administrador conserva acceso completo mediante el bypass definido en el núcleo.

Las capturas del rechazo de ruta y de los listados sin acciones se incorporarán al ejecutar
esta matriz con usuarios reales, una vez que el núcleo de Jeison esté integrado.
