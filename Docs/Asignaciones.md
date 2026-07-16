# Asignaciones por rama

Trabajo Final de Grado · Grupo 6 · Mini ERP

Cada quien trabaja en **su rama**, no en `main`. Nadie commitea directo a `main`: se
integra por pull request, y quien revisa es otro del grupo. Así todos ven el código de
todos, que es lo que hace falta para poder defenderlo en septiembre.

La rama lleva el nombre de su dueño: `<nombre>/<módulo>`. Así el historial dice quién hizo
qué sin que nadie tenga que preguntarlo, que es justo lo que un asesor va a querer ver.

| Rama | Quién | Módulo |
|------|-------|--------|
| `jeison/pos` | **Jeison** | Punto de venta y comprobantes fiscales |
| `samuel/finanzas` | **Samuel** | Finanzas y compras |
| `dionis/cobros-pagos` | **Dionis** | Cobros, pagos y clientes |
| — | **Rangelis** | Capítulo II, entrevistas y armado del informe |

---

## Regla que aplica a los tres

**Un módulo no está terminado hasta que su sección del Capítulo IV existe.** Se escribe
en la misma semana en que se construye, con las capturas tomadas en el momento. No al
final: al final nadie recuerda por qué tomó cada decisión, y ese "por qué" es justamente
lo que Elvin va a preguntar.

Cada dev entrega **dos cosas** por módulo: el código y su sección del informe.

---

## Jeison — `jeison/pos`

Ya construido: inventario, POS con carrito y cobro, secuencias de NCF con asignación
atómica, modelo de facturación con costo congelado.

### Pendiente

| # | Tarea | Terminado cuando |
|---|-------|------------------|
| 1 | **Pantalla de facturas emitidas** | Se listan las facturas con filtro por fecha y estado, y se abre el detalle de una. Hoy el enlace `/ventas/facturas/{id}` da 404. |
| 2 | **Impresión de la factura** | Sale un documento imprimible con el NCF, el detalle, el ITBIS desglosado y los datos del cliente. |
| 3 | **Anulación desde la pantalla** | Se anula una factura con motivo, el inventario se repone y el balance del cliente baja si era a crédito. La lógica ya existe en `Factura.Anular`, falta la UI. |
| 4 | **Administración de secuencias NCF** | El administrador registra los rangos que le autorizó la DGII y ve cuántos quedan. Hoy solo existen los rangos sembrados, que **no son válidos ante la DGII**. |
| 5 | **XML e-CF y código QR** (F3) | Se genera el XML del comprobante electrónico según la Ley 32-23 y su QR. **Sin certificación ante la DGII**: eso está fuera del alcance por escrito. |

### Prueba pendiente que importa

La emisión corre dentro de una transacción explícita para que un fallo libere el NCF
reservado. **Eso no tiene prueba automatizada.** Es la pieza más delicada del módulo y
debe entrar al plan de pruebas del 5.1.

---

## Samuel — `samuel/finanzas`

### Primero: adoptar compras

El módulo de compras existe pero **no lo escribiste tú**. Antes de tomar finanzas,
apropiátelo:

1. Lee `Domain/Compras/Compra.cs`, `Application/Compras/` e `Infrastructure/.../ComprasRepositorios.cs`.
2. **Explícale a Jeison**, en voz alta, estas tres cosas:
   - Por qué el costo se promedia ponderado y no se toma el último.
   - Por qué una compra recibida no se anula sino que se devuelve.
   - Por qué recibir corre en una sola transacción.
3. Si no las sabes explicar, no estás listo para defenderlo. Mejor descubrirlo ahora.

Luego extiéndelo con lo que le falta:

| # | Tarea | Terminado cuando |
|---|-------|------------------|
| 1 | **Devolución a proveedor** | Se devuelve mercancía de una compra recibida, el inventario baja y el balance del proveedor se ajusta. El tipo `TipoMovimiento.DevolucionProveedor` ya existe y nadie lo usa. |

### Módulo de finanzas — nada construido

| # | Tarea | Terminado cuando |
|---|-------|------------------|
| 2 | **Registro de egresos** | Se registran gastos que no son compras de mercancía: alquiler, luz, sueldos, transporte. Con categoría y fecha. |
| 3 | **Reporte de ingresos** | Ventas de un rango de fechas, separando contado de crédito, con su ITBIS. Sale de `Facturas`. |
| 4 | **Reporte de rentabilidad** | Margen por período, por producto y por categoría. **Usa `LineaFactura.CostoUnitario`, que está congelado** — no leas `Producto.Costo`, que cambia con cada compra y te daría márgenes falsos. |
| 5 | **Estado de resultados simple** | Ingresos − costo de lo vendido − egresos = utilidad del período. Es el número que el comerciante nunca ha visto. |
| 6 | **Panel de finanzas** | Ventas del día, margen del día, egresos del mes y utilidad. Lo primero que el dueño mira al llegar. |

### Gate de Samuel

El reporte de rentabilidad cuadra contra las ventas reales: si vendiste 3 LB con margen
21.38, el reporte del día dice 21.38. Ni un centavo de diferencia.

---

## Dionis — `dionis/cobros-pagos`

### Primero: adoptar clientes

Igual que Samuel. Lee `Domain/Clientes/Cliente.cs` y `Application/Clientes/`, y
**explícale a Jeison**:

- Por qué el crédito fiscal exige RNC y no basta una cédula.
- Por qué el documento se guarda sin guiones.
- Por qué el balance no se puede editar desde la ficha del cliente.

### El hueco real que te toca cerrar

**Hoy los balances suben y nunca bajan.** Una compra a crédito deja al proveedor con
RD$ 640 que no hay forma de pagar. Una venta a crédito deja al cliente con una deuda que
no hay forma de cobrar. Sin esto, el módulo de finanzas de Samuel reportaría una deuda
que crece para siempre.

| # | Tarea | Terminado cuando |
|---|-------|------------------|
| 1 | **Cobro a cliente** | Se registra un abono, el balance baja y queda el rastro de quién cobró, cuándo y cuánto. Mismo criterio que el inventario: **ningún saldo cambia sin un asiento que lo explique**. |
| 2 | **Pago a proveedor** | Lo mismo al revés: se salda lo que se le debe y su balance baja. |
| 3 | **Estado de cuenta del cliente** | Facturas a crédito, abonos y saldo. Es lo que hoy vive en el cuaderno de fiados. |
| 4 | **Cuentas por cobrar** | Quién debe, cuánto y desde hace cuántos días. Usa `Cliente.DiasCredito` para marcar lo vencido. |
| 5 | **Cuentas por pagar** | Lo mismo con los proveedores. |
| 6 | **Extender clientes** | Historial de compras del cliente en su ficha. |

### Gate de Dionis

Un cliente compra a crédito por 500, su balance sube a 500, le cobras 200, y baja a 300.
El estado de cuenta muestra las tres líneas.

---

## Rangelis — informe

| # | Tarea | Cuándo |
|---|-------|--------|
| 1 | **Capítulo II completo** | Ya. No depende de una línea de código. |
| 2 | **Entrevista al piloto** | Esta semana. Alimenta el 3.1. |
| 3 | **Corrección del Capítulo I** | El documento trata el 15 de mayo de 2026 como fecha futura, y ya pasó. Reencuadrar en presente. |
| 4 | **Capítulo III** | Requerimientos, modelo de datos y diagramas. La estructura de carpetas del repo *es* la arquitectura: fotografíala. |
| 5 | **Integrar el Capítulo IV** | Recibe las secciones que escriben los devs y cuida la coherencia. No lo escribas tú solo. |

---

## Cómo trabajar la rama

```bash
git clone https://github.com/jeisonsantoslogictool/MiniERP.git
cd MiniERP

git checkout samuel/finanzas       # o la que te toque
dotnet run --project src/MiniERP.Web
```

Arrancar la app es el único paso: crea la base sola, la siembra, y **escribe tu
contraseña de administrador en la consola** dentro de un recuadro. Solo se muestra una vez.

**Requisitos:** Visual Studio 2026 (por el formato `.slnx` y .NET 10) y SQL Server en
`localhost` con autenticación de Windows.

### Antes de pedir el pull request

- [ ] `dotnet build MiniERP.slnx` sin errores ni advertencias
- [ ] `dotnet test` en verde, con pruebas nuevas de lo que construiste
- [ ] Probado en el navegador, no solo compilando
- [ ] Traer `main` a tu rama y resolver conflictos tú, no quien revisa
- [ ] La sección del Capítulo IV escrita, con capturas

```bash
git fetch origin
git merge origin/main
```

### Convenciones que ya están en el repo

- **Nada de contraseñas en el código.** Autenticación de Windows para SQL Server.
- **Versiones de paquetes solo en `Directory.Packages.props`**, nunca en un `.csproj`.
- **Migraciones desde Infrastructure:**
  ```bash
  dotnet ef migrations add <Nombre> \
    --project src/MiniERP.Infrastructure \
    --startup-project src/MiniERP.Web \
    --output-dir Persistence/Migrations
  ```
- **Todo lo que el sistema necesite para arrancar se siembra** en `DatabaseInitializer`,
  no se documenta como paso manual.
- **Los saldos no se editan.** Existencia, balance de cliente y balance de proveedor solo
  cambian por una operación que deja rastro.
