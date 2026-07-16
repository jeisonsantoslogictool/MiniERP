# CLAUDE.md

Contexto del proyecto para Claude Code. Se carga automáticamente al abrir el repositorio.

---

## Qué es esto

**Trabajo Final de Grado del Grupo 6** — Universidad Dominicana O&M, Moca. Ingeniería en
Sistemas y Computación, 2026.

Mini ERP modular en ASP.NET Core y C# para microcomercios minoristas de la República
Dominicana. Cinco módulos: inventario, ventas (POS), compras, clientes y finanzas.
Se valida con un caso piloto en un **minimarket real, propiedad de Jeison**, que vende
**a crédito y al contado**, y **por peso y por unidad**. Esas cuatro características
explican buena parte del modelo de datos.

**Este NO es un repositorio de Logictool.** No aplica el CLAUDE.md de Onix.Apps. Es un
sistema propio, mucho más pequeño, y debe ser defendible como trabajo del grupo: se
aprovecha el conocimiento de otros sistemas, no se copia su código.

| | |
|---|---|
| **Sustentantes** | Jeison Luis Santos · Samuel Sánchez Rosario · Dionis José Castro Gómez · Rangelis Toribio |
| **Asesor** | Lic. Elvin Germán |
| **Documentos** | [Plan maestro](Docs/Plan-Maestro.md) · [Asignaciones por rama](Docs/Asignaciones.md) |

---

## Dónde vamos

**F0, F1 y F2 completos y verificados en navegador.** Sigue F3.

| Fase | Estado | Qué incluye |
|------|--------|-------------|
| F0 | ✅ | Solución en capas, Identity, arranque autoconfigurable |
| F1 | ✅ | Inventario (productos, existencias, movimientos, alertas) y clientes |
| F2 | ✅ | Compras con recepción, secuencias de NCF, POS con cobro y factura |
| F3 | ⬜ | Finanzas · cobros y pagos · XML e-CF con QR |
| F4 | ⬜ | Integración, pruebas y caso piloto |
| F5 | ⬜ | Capítulo V y cierre del informe |

**El gate de F2 está demostrado de punta a punta:** se compraron 20 LB y el stock subió
de 9.5 a 29.5 con el costo promediado a 30.8729; se vendieron 3 LB y bajó a 26.5, con
NCF `B0200000001`, total 114.00 y cambio 86.00.

### Huecos conocidos

- **No existe la pantalla de facturas emitidas.** El POS enlaza a `/ventas/facturas/{id}`
  y esa ruta da 404. La venta guarda bien; solo falta dónde verla.
- **Los balances suben y nunca bajan.** Una compra a crédito deja deuda con el proveedor
  y no hay forma de pagarla. Le toca a Dionis.
- **Las pruebas son todas del dominio.** Servicios, repositorios y pantallas se verifican
  a mano. Falta prueba de integración de la transacción de emisión: nada demuestra
  automáticamente que un rollback libera el NCF reservado.

---

## Cómo se trabaja

**Cada quien en su rama**, con su nombre. Nadie commitea directo a `main`: se integra por
pull request revisado por otro del grupo.

| Rama | Quién |
|------|-------|
| `jeison/pos` | Punto de venta y comprobantes fiscales |
| `samuel/finanzas` | Finanzas y compras |
| `dionis/cobros-pagos` | Cobros, pagos y clientes |

**Un módulo no está terminado hasta que su sección del Capítulo IV existe**, escrita en la
misma semana y con las capturas del momento.

---

## Levantarlo

```bash
dotnet run --project src/MiniERP.Web
```

Eso es todo. **Arrancar la app es el único paso de instalación**: crea la base si no
existe, aplica migraciones y siembra roles, administrador, unidades de medida, categorías
y secuencias de NCF. Todo idempotente.

La contraseña del administrador (`admin@minierp.local`) **se genera al azar y se escribe
una sola vez en la consola**, dentro de un recuadro. No hay contraseñas en el repositorio.
Para fijar una: `dotnet user-secrets set "Seed:AdminPassword" "..." --project src/MiniERP.Web`.

**Requisitos:** .NET 10 · SQL Server en `localhost` con autenticación de Windows ·
Visual Studio 2026 (por el formato `.slnx`).

```bash
dotnet build MiniERP.slnx
dotnet test                    # 100 pruebas del dominio
```

---

## Arquitectura

Modular en capas. **Modular** por módulo de negocio, **en capas** por responsabilidad.
Las capas internas nunca dependen de las externas.

```
MiniERP.Domain          Entidades y reglas puras. Sin dependencias.
      ^
MiniERP.Application     Casos de uso, contratos de repositorio, DTOs. No conoce EF Core.
      ^
MiniERP.Infrastructure  EF Core, SQL Server, Identity. Lo único que habla con la base.
      ^
MiniERP.Web             Blazor Server. Solo presentación.
```

Dentro de cada capa hay una carpeta por módulo. **Esa estructura es la evidencia del
Capítulo III**: no la reorganices sin motivo.

Nombres de dominio en español (`Producto`, `Factura`, `Compra`); vocabulario de framework
en inglés (`ApplicationUser`, `MiniErpDbContext`, `DependencyInjection`).

---

## Reglas del proyecto

Estas no son preferencias de estilo: cada una responde a algo que ya pasó o que el
anteproyecto exige.

- **Ningún saldo se edita.** Existencia, balance de cliente y balance de proveedor solo
  cambian por una operación que deja rastro. Incluso la existencia inicial de un producto
  entra como movimiento de apertura.
- **Todo lo que el sistema necesite para arrancar se siembra** en `DatabaseInitializer`.
  Nunca se documenta como paso manual.
- **Sin contraseñas en el repositorio.** Autenticación de Windows para SQL Server.
- **Versiones de paquetes solo en `Directory.Packages.props`**, jamás en un `.csproj`.
- **Las migraciones salen de Infrastructure:**
  ```bash
  dotnet ef migrations add <Nombre> \
    --project src/MiniERP.Infrastructure \
    --startup-project src/MiniERP.Web \
    --output-dir Persistence/Migrations
  ```
- **Un solo `MiniErpDbContext`** para identidad y los cinco módulos: una sola línea de
  migraciones para tres desarrolladores.
- **`HasData` solo para catálogo inmutable** (unidades de medida). Lo que exige lógica
  (hash de contraseña) o lo que el usuario editará va en el sembrador de arranque.
  Si usas `HasData`, la fecha va **fija**: un `DateTime.UtcNow` ahí hace que el modelo
  cambie en cada compilación y las migraciones nunca cierren.

---

## Decisiones de dominio que no se cambian sin discutirlas

**Costo promedio ponderado, no último costo.** Comprar 10 a 30 y luego 10 a 40 deja el
costo en 35. Con último costo quedaría en 40, inflando el costo y escondiendo el margen
real. El anteproyecto habla de no conocer *"los costos reales de adquisición"*: esto es lo
que responde esa pregunta. Vive en `Compra.CalcularCostoPromedio`.

**El costo se congela en la línea de factura.** `LineaFactura.CostoUnitario` guarda el
costo al momento de vender. Sin eso, una compra posterior cambiaría retroactivamente el
margen de ventas ya hechas, y los resultados del Capítulo V se moverían solos. **Los
reportes de rentabilidad deben usar `LineaFactura.CostoUnitario`, nunca `Producto.Costo`.**

**El NCF se asigna con un `UPDATE ... OUTPUT` atómico.** Leer el contador, sumarle uno y
guardar deja una ventana en la que dos cajas obtienen el mismo número: dos comprobantes
con el mismo NCF es una infracción ante la DGII, no un bug cosmético. Verificado con 20
procesos concurrentes. Vive en `SecuenciaNcfRepositorio.AsignarSiguienteAsync`.

**La emisión corre en una transacción explícita** para que un fallo libere el NCF
reservado. El precio: la fila de la secuencia se bloquea durante la venta, así que dos
cajas se serializan un instante. Aceptable para un local con una o dos cajas.

**La merma es un tipo de movimiento propio**, no un ajuste negativo. El anteproyecto
nombra las *"mermas no detectadas"* como uno de los problemas a resolver; mezclarlas con
los ajustes las volvería invisibles otra vez. Exigen motivo por escrito.

**El redondeo es aritmético, no bancario.** `RetailConstants.RedondearImporte` usa
`AwayFromZero`. .NET redondea a par por defecto, y que 2.505 dé 2.50 le hace perder la
confianza al comerciante en su propia caja.

**Los códigos de `TipoComprobante` son los de la DGII** (01, 02, 14, 15) porque forman
parte del NCF. Hay una prueba que los fija.

**Documentos congelados.** La descripción, la tasa de ITBIS y el nombre del cliente se
copian al documento al capturarlo. Si mañana el producto cambia de gravado a exento, la
factura de ayer debe seguir mostrando lo que se imprimió.

---

## Alcance — lo que NO se hace

Definido así en el anteproyecto y sostenido a lo largo del proyecto. Si alguien lo pide,
citar esto y decir que no:

- **Certificación fiscal ante la DGII.** Se genera el XML e-CF y el QR, y ahí se para.
- Analítica predictiva.
- Integración con comercio electrónico.
- Nómina, producción industrial y logística de distribución.
- Múltiples almacenes: el comercio opera en una sola ubicación, y la existencia vive en
  `Producto`.

Las secuencias de NCF que siembra el sistema **no son rangos autorizados por la DGII**.
Son de arranque, para que el sistema funcione desde el primer día. Antes del piloto real
hay que cargar los que la DGII haya autorizado al comercio.
