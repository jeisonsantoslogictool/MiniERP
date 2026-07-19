# Asignaciones por rama

Trabajo Final de Grado · Grupo 6 · Mini ERP

Cada quien trabaja en **su rama**, no en `main`. Nadie commitea directo a `main`: se
integra por pull request, y quien revisa es otro del grupo. Así todos ven el código de
todos, que es lo que hace falta para poder defenderlo en septiembre.

La rama lleva el nombre de su dueño: `<nombre>/<módulo>`. Así el historial dice quién hizo
qué sin que nadie tenga que preguntarlo, que es justo lo que un asesor va a querer ver.

| Rama | Quién | Dueño de | Módulos del anteproyecto |
|------|-------|----------|--------------------------|
| `jeison/pos` | **Jeison** | `Inventario/` · `Ventas/` | Inventario y ventas (POS) |
| `dionis/cobros-pagos` | **Dionis** | `Clientes/` · `Compras/` | Clientes y compras |
| `samuel/finanzas` | **Samuel** | `Finanzas/` | Finanzas |
| — | **Rangelis** | El informe | Capítulo II, entrevistas y armado |

## La frontera: carpetas completas, no conceptos

**Nadie edita el módulo de otro.** La primera versión de este documento partía el trabajo
por concepto —"el ciclo del dinero" para Dionis— y eso lo obligó a escribir dentro de
`Compras/`, que era de Samuel. Los dos iban a chocar en `Proveedor.cs` y en
`Proveedores.razor`, y ninguno podría defender su módulo entero en la sustentación.

Corregido: la frontera va por **módulo del anteproyecto**.

- **Jeison** — la mercancía y su salida.
- **Dionis** — los terceros y su crédito, de ambos lados: el cliente que debe y el
  proveedor a quien se le debe. Cobros y pagos son suyos porque son simétricos.
- **Samuel** — la capa analítica. **Lee todo lo anterior y no modifica nada.**

¿Necesitas algo del módulo de otro? **Pídelo, no lo escribas.**

> **¿No sabes en qué rama estás?** `git branch --show-current`. Si dice `main`, cámbiate a la
> tuya: `git checkout <nombre>/<módulo>`. Al abrir el repo con Claude, esto se detecta solo
> (ver [CLAUDE.md](../CLAUDE.md) → "Empieza aquí").

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

**Dueño de `Finanzas/` completo.** Nada construido: lo construyes todo tú, y por eso lo
puedes defender entero. **Lees los demás módulos, no los modificas.**

> **Antes de escribir una línea, lee en [CLAUDE.md](../CLAUDE.md) la sección
> "Un pago a proveedor NO es un egreso".** Es la trampa que te está esperando: registrar
> los pagos como gastos cuenta el costo dos veces y produce una utilidad falsa que nadie
> notaría hasta la defensa.

| # | Tarea | Terminado cuando |
|---|-------|------------------|
| 1 | **`Egreso`** | Se registra un gasto operativo con categoría y fecha: alquiler, luz, agua, sueldos, transporte. **Nunca compras de mercancía ni pagos a proveedor.** |
| 2 | **Reporte de ingresos** | Ventas de un rango, separando contado de crédito, con su ITBIS aparte. Sale de `Factura`. |
| 3 | **Reporte de rentabilidad** | Margen por período, por producto y por categoría. **Usa `LineaFactura.CostoUnitario`, que está congelado — nunca `Producto.Costo`**, que cambia con cada compra y te daría márgenes falsos. |
| 4 | **Estado de resultados** | Ingresos − costo de lo vendido − egresos = utilidad. Es el número que el comerciante nunca ha visto. |
| 5 | **Flujo de caja** | Ventas de contado + cobros − egresos − pagos a proveedor. **Distinto del estado de resultados**, y ambos hacen falta: se puede tener utilidad sin efectivo. |
| 6 | **Panel de finanzas** | Ventas del día, margen del día, egresos del mes, utilidad y efectivo. Lo primero que el dueño mira al llegar. |

### Gate de Samuel

El reporte de rentabilidad cuadra contra las ventas reales: si se vendieron 3 LB con
margen 21.38, el reporte del día dice 21.38. Ni un centavo de diferencia.

Y la prueba que de verdad importa: **registra un pago a proveedor y comprueba que la
utilidad NO se mueve.** Si se mueve, contaste el costo dos veces.

---

## Dionis — `dionis/cobros-pagos`

**Dueño de `Clientes/` y `Compras/`.** Los terceros y su crédito, de ambos lados: el
cliente que debe y el proveedor a quien se le debe.

Ya construiste `Cobro`, `Pago`, el estado de cuenta y sus pantallas — y seguiste los
patrones del proyecto sin que nadie te lo dijera: `Cliente.AplicarCobro` es el espejo de
`Producto.AplicarMovimiento`, con balance anterior y resultante para auditar. Eso está
bien hecho.

**El módulo de compras pasa a ser tuyo** porque ya estabas trabajando dentro de él con
los pagos. Antes de extenderlo, apropiátelo:

1. Lee `Domain/Compras/Compra.cs`, `Application/Compras/` e `Infrastructure/.../ComprasRepositorios.cs`.
2. **Explícale a Jeison**, en voz alta, estas tres cosas:
   - Por qué el costo se promedia ponderado y no se toma el último.
   - Por qué una compra recibida no se anula sino que se devuelve.
   - Por qué recibir corre en una sola transacción.
3. Si no las sabes explicar, no estás listo para defenderlo. Mejor descubrirlo ahora.

| # | Tarea | Terminado cuando |
|---|-------|------------------|
| 1 | **Cobro a cliente** | ✅ Hecho. |
| 2 | **Pago a proveedor** | ✅ Hecho. |
| 3 | **Estado de cuenta del cliente** | Facturas a crédito, abonos y saldo. Es lo que hoy vive en el cuaderno de fiados. |
| 4 | **Cuentas por cobrar** | Quién debe, cuánto y desde hace cuántos días. Usa `Cliente.DiasCredito` para marcar lo vencido. |
| 5 | **Cuentas por pagar** | Lo mismo con los proveedores, usando `Proveedor.DiasCredito`. |
| 6 | **Devolución a proveedor** | Se devuelve mercancía de una compra recibida, el inventario baja y el balance del proveedor se ajusta. `TipoMovimiento.DevolucionProveedor` ya existe y nadie lo usa. |
| 7 | **Historial de compras del cliente** | En su ficha, para saber qué le vendes y con qué frecuencia. |

### Gate de Dionis

Un cliente compra a crédito por 500, su balance sube a 500, le cobras 200, y baja a 300.
El estado de cuenta muestra las tres líneas, y cuentas por cobrar lo lista con sus días.

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

### Cómo no chocar en el merge

Los conflictos no salen *dentro* de tu módulo —la frontera por carpeta ya lo evita—, sino
en los **archivos compartidos que nadie posee**. Estas cinco reglas los cortan:

1. **Integra a `main` a diario, no al final.** Tarea terminada = merge a `main` el mismo
   día. Una rama que vive una semana acumula una semana de choques; una que vive un día,
   casi ninguno.

2. **Al terminar una tarea, re-ramifica.** Cuando tu rama entra a `main`, bórrala y crea
   una nueva desde `main` para lo siguiente. Seguir sobre la rama vieja te hace divergir
   otra vez.
   ```bash
   git checkout main && git pull
   git branch -d <tu-rama-vieja>
   git checkout -b <nombre>/<siguiente-tarea>
   ```

3. **Migraciones en serie, nunca dos a la vez.** El `ModelSnapshot` de EF es un solo
   archivo: dos migraciones sin integrar chocan seguro. Antes de crear la tuya, trae
   `main`; créala, compílala y **súbela a `main` el mismo día**, avisando "migré, hagan
   pull". El siguiente que migre trae `main` primero.

4. **Tus `@using` y tus registros de servicios van en TU archivo, no en el compartido:**
   - `@using` de tu módulo → `Components/Pages/<TuModulo>/_Imports.razor`, no el raíz.
   - Registrar un servicio → `DependencyInjection.<TuModulo>.cs` (método `Add<TuModulo>`),
     no el `AddInfrastructure` raíz.

   Así el `_Imports.razor` raíz y el `DependencyInjection.cs` raíz casi nunca se tocan y
   dejan de ser fuente de conflictos.

5. **La cadena de conexión no lleva datos de tu máquina.** `appsettings.json` se queda con
   `Server=localhost` —el objetivo real y el del piloto—. Si tu SQL Server es una instancia
   con nombre (p. ej. `SQLEXPRESS`), ponla en **tus user-secrets**, no en `appsettings.json`:
   es compartido y le rompe el arranque a los demás (ya pasó una vez).
   ```bash
   dotnet user-secrets set "ConnectionStrings:DefaultConnection" "Server=localhost\SQLEXPRESS;Database=MiniERP;Trusted_Connection=True;TrustServerCertificate=True;MultipleActiveResultSets=true" --project src/MiniERP.Web
   ```

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
