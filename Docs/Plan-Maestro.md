# Plan Maestro — Mini ERP para Microcomercios

Trabajo Final de Grado · Grupo 6 · Universidad Dominicana O&M

**Arranque:** 20 de julio de 2026 · **Entrega estimada:** finales de septiembre de 2026

El calendario asume entre 15 y 20 horas por semana por desarrollador. Si el ritmo real
resulta ser la mitad, esto se estira a cinco meses. Conviene medirlo en la semana 2 y no
descubrirlo en la ocho.

---

## Decisiones tomadas

| Decisión | Elección | Razón |
|----------|----------|-------|
| Stack | Blazor Server · .NET 10 | C# puro, sin JavaScript. Cumple el «ASP.NET Core y C#» que fija el anteproyecto y es lo más rápido de construir en un equipo pequeño. El alcance no pide offline ni móvil, así que la única debilidad seria de Blazor Server no aplica. |
| Base de datos | SQL Server 2022 | Ya instalado. Autenticación de Windows, sin contraseñas en el repositorio. |
| Repositorio | GitHub privado | `jeisonsantoslogictool/MiniERP`, los cuatro integrantes con acceso. |
| Caso piloto | Confirmado | Los requerimientos del Capítulo III salen de su operación real. |

---

## Reparto del grupo

Son cuatro personas y **tres escriben código**. Eso acelera la construcción, pero deja el
informe en manos de una sola persona.

### Track de código

La frontera va por **módulo completo**, no por concepto: cada quien es dueño de carpetas
enteras y nadie edita el módulo de otro. Son tres sesiones de Claude trabajando en
paralelo, y dos manos en el mismo archivo se pagan en el merge.

| Quién | Dueño de | Responsabilidad |
|-------|----------|-----------------|
| **Jeison** | `Inventario/` · `Ventas/` | Arquitectura, inventario, POS y comprobantes fiscales. La cadena crítica. |
| **Dionis** | `Clientes/` · `Compras/` | Los terceros y su crédito, de ambos lados: el cliente que debe y el proveedor a quien se le debe. |
| **Samuel** | `Finanzas/` | La capa analítica: ingresos, egresos, rentabilidad, estado de resultados y flujo de caja. Lee todo lo demás y no modifica nada. |

**Regla que compensa el desbalance:** quien construye un módulo escribe la sección del
Capítulo IV que le corresponde, en la misma semana y con sus capturas. Es también quien
mejor lo puede defender.

### Track de documento — Rangelis

Capítulo II completo, entrevistas y levantamiento en el comercio piloto, y el armado y la
coherencia del informe: recibe las secciones del Capítulo IV que escriben los devs y las
integra.

**Va solo.** Si se atrasa, se atrasa el trabajo completo. Su avance se revisa cada semana,
no en la ocho.

---

## Cronograma

| Semana | Fechas | Fase | Track de código | Track de documento |
|--------|--------|------|-----------------|--------------------|
| S1 | 20–26 jul | F0 | Repo, solución en capas, base de datos, login | Arranca Cap. II · Entrevista al piloto · Corrección Cap. I |
| S2 | 27 jul–2 ago | F1 | Inventario · Clientes | Cap. II · Requerimientos (3.1) |
| S3 | 3–9 ago | F1 | Inventario · Clientes | Cierra Cap. II · Modelo de datos y UML |
| S4 | 10–16 ago | F2 | Compras · POS | Cap. III · Cap. IV: inventario y clientes |
| S5 | 17–23 ago | F2 | Compras · POS | Cierra Cap. III |
| S6 | 24–30 ago | F3 | Finanzas · NCF y e-CF | Cap. IV: compras y POS |
| S7 | 31 ago–6 sep | F3 | Finanzas · NCF y e-CF | Cap. IV: finanzas · Plan de pruebas (5.1) |
| — | antes de F4 | Seg. | **Usuarios y permisos** · 3 ramas · [Seguridad-Permisos.md](Seguridad-Permisos.md) | Sección 4.8 |
| S8 | 7–13 sep | F4 | Pruebas, integración, despliegue | Cierra Cap. IV |
| S9 | 14–20 sep | F4 | Piloto en operación · soporte | Captura de indicadores · arranca Cap. V |
| S10 | 21–27 sep | F5 | Congelado · solo correcciones | Cierra Cap. V · informe final · ensayo de defensa |

> El código corre en **tres ramas en paralelo** — `jeison/pos`, `samuel/finanzas` y
> `dionis/cobros-pagos` — no en un solo track. El detalle por rama, con tareas y "gate",
> está en [Asignaciones.md](Asignaciones.md); las fases de abajo ya dicen qué rama
> construye cada módulo.

> **Addendum — Seguridad y permisos (transversal, antes de F4).** La gestión de usuarios y
> **permisos por usuario** se nos pasó en la planificación: hoy los roles están definidos
> pero no gobiernan nada, y cualquier sesión llega a todo. Se reparte en tres ramas nuevas
> —`jeison/seguridad`, `dionis/permisos` y `samuel/permisos`—: Jeison construye el núcleo
> primero, y Dionis y Samuel gatean sus módulos después. Amplía la sección 4.8. Plan,
> tareas y "gate" por rama en [Seguridad-Permisos.md](Seguridad-Permisos.md).

---

## Fases

### F0 — Arranque · S1 · base común del equipo

- Repositorio privado en GitHub, los cuatro con acceso.
- Solución en cuatro capas: Domain, Application, Infrastructure y Web. Dentro de cada
  capa, una carpeta por módulo. La estructura de carpetas *es* la «arquitectura modular
  en capas» que promete el objetivo específico, y eso se puede fotografiar para el
  Capítulo III.
- EF Core sobre SQL Server, base `MiniERP`.
- Login, usuarios y roles con ASP.NET Core Identity. Resuelve la sección 4.8 del informe.

**Gate:** levantas la aplicación, entras con usuario y contraseña, y ves el menú con los
cinco módulos.

### F1 — Catálogos · S2–S3

- **Inventario — `jeison/pos`:** productos, categorías, existencias, movimientos y alertas de
  reabastecimiento.
- **Clientes — `dionis/cobros-pagos`:** registro, RNC o cédula, y tipo de comprobante preferido.
  Dionis adopta lo ya construido y lo extiende.

**Gate:** registras un producto, ajustas su existencia y salta la alerta al bajar del
mínimo. Registras un cliente con su RNC.

### F2 — Movimiento · S4–S5 · la fase que decide el proyecto

- **Compras — `samuel/finanzas`:** proveedores, órdenes de compra y recepción de mercancía, que
  suma al inventario y fija el costo. Samuel adopta lo ya construido y le añade la devolución a proveedor.
- **POS — `jeison/pos`:** búsqueda por código o nombre, carrito, cobro, asignación de NCF y
  factura impresa, que resta del inventario.

**Gate:** compras diez unidades y el stock sube a diez. Vendes tres y baja a siete, con su
NCF y su factura. Esa es, literalmente, la integración que promete el anteproyecto.

### F3 — Dinero y fiscal · S6–S7

- **Cobros y pagos — `dionis/cobros-pagos`:** abonos de cliente y pagos a proveedor, estado de
  cuenta, cuentas por cobrar y por pagar, y devolución a proveedor. **Va primero:** sin bajar los
  balances, el reporte de finanzas mostraría una deuda que crece para siempre.
- **Finanzas — `samuel/finanzas`:** ingresos, egresos, rentabilidad, estado de resultados y flujo
  de caja. Dos reglas que no se negocian: usa `LineaFactura.CostoUnitario` (congelado) y nunca
  `Producto.Costo`; y **un pago a proveedor no es un egreso** — contarlo como gasto duplica el
  costo y produce una utilidad falsa. Ver CLAUDE.md.
- **Comprobantes — `jeison/pos`:** secuencias de NCF, estructura del XML e-CF y código QR. Sin
  certificación ante la DGII, que el alcance ya excluye explícitamente.

**Gate:** el reporte de rentabilidad cuadra contra las compras y ventas de F2; un cliente salda
su deuda y el balance baja; se registra un pago a proveedor y la utilidad **no** se mueve; y se
genera el XML e-CF de una factura real.

### F4 — Integración y piloto · S8–S9 · los cuatro

- Pruebas por módulo y de integración. Es el plan de pruebas de la sección 5.1, y cumple
  la metodología incremental que el anteproyecto se compromete a seguir.
- Instalación en el comercio y carga de datos reales.
- Operación real y captura de los indicadores que pide el Capítulo V.

**Gate:** el comercio factura un día completo con el sistema, sin volver al cuaderno.

### F5 — Cierre · S10

- Código congelado. Solo se corrigen fallos que el piloto haya dejado al descubierto.
- Capítulo V con los resultados, armado final, revisión con el asesor y ensayo de defensa.

**Gate:** informe completo entregado.

---

## Los capítulos, y cuándo se escriben

| Capítulo | Cuándo | Nota |
|----------|--------|------|
| I — Aspectos introductorios | **Hecho** | Queda una corrección: el documento habla del 15 de mayo de 2026 como fecha futura, y ya pasó. Reencuadrarlo en presente, que además fortalece el argumento. |
| II — Marco teórico | S1–S3 | Ocho secciones que no dependen del código. Empieza hoy. |
| III — Análisis y diseño | S2–S5 | Requerimientos de la entrevista al piloto, modelo de datos, casos de uso, clases y secuencia. |
| IV — Desarrollo de los módulos | S4–S8 | **Se escribe al cerrar cada módulo, nunca al final**, con las capturas tomadas en el momento. Esto es lo que hace posible el plazo. |
| V — Validación y resultados | S9–S10 | Con los datos del piloto. El único capítulo que no se puede adelantar. |

---

## Riesgos

| Riesgo | Mitigación |
|--------|------------|
| **Rangelis solo con el informe.** Al pasar Samuel y Dionis a código, el track de documento perdió la mitad de su gente. El informe es la mitad de la nota. | Cada dev escribe la sección del Capítulo IV del módulo que construyó, en la misma semana. Rangelis integra en vez de escribirlo todo. Revisar su avance cada semana. |
| **Tres devs editando los mismos archivos.** `DependencyInjection.cs`, `MiniErpDbContext.cs` y las migraciones los tocan los tres: no hay diseño que lo evite. | La frontera por módulo elimina el resto de los choques. Para estos tres, traer `main` antes de empezar el día y antes del pull request. Nunca al final. |
| **Confundir utilidad con efectivo.** Registrar pagos a proveedor como egresos duplica el costo y da una utilidad falsa. No lanza error: produce números creíbles y equivocados. | Regla escrita en CLAUDE.md, y un gate explícito: registrar un pago y comprobar que la utilidad no se mueve. |
| **Dejar el Capítulo IV para el final.** El asesino clásico: el código termina y quedan ochenta páginas por escribir sin memoria de cómo se hizo. | Escribirlo por módulo, y hacerlo parte del gate. Un módulo no está cerrado hasta que su sección del informe existe. |
| **El alcance creciendo por los bordes.** Analítica, e-commerce, app móvil, offline. | El anteproyecto ya los excluye por escrito. Citarlo y decir que no, sin culpa. |
| **Perseguir la certificación de la DGII.** Proceso largo, externo y fuera de su control. | Está fuera de alcance por escrito. Se genera el XML e-CF y el QR, y ahí se para. |
| **Que el piloto se enfríe.** Está confirmado hoy, pero a un comerciante ocupado se le pasa el entusiasmo en dos meses. | Entrevistarlo en la semana 1 y volver con algo que se vea en la cinco. |
| **Reciclar código de sistemas existentes.** La tentación es obvia y el riesgo también. | El conocimiento se aprovecha; el código no se copia. Debe ser un sistema propio y defendible como trabajo del grupo. |
