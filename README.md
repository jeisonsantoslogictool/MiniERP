# Mini ERP para Microcomercios Minoristas

Trabajo Final de Grado — **Grupo 6**
Universidad Dominicana O&M, Moca · Escuela de Ingeniería
Ingeniería en Sistemas y Computación · 2026

> Diseño e implementación de un mini sistema de planificación de recursos empresariales
> (Mini ERP) modular basado en ASP.NET Core y C# para la optimización operativa y
> financiera de microcomercios minoristas en la República Dominicana.

| | |
|---|---|
| **Sustentantes** | Jeison Luis Santos · Dionis José Castro Gómez · Rangelis Toribio · Samuel Sánchez Rosario |
| **Asesor** | Lic. Elvin Germán |
| **Stack** | .NET 10 · Blazor Server · Entity Framework Core · SQL Server 2022 |

---

## Módulos

| Módulo | Alcance |
|--------|---------|
| **Inventario** | Productos, categorías, existencias, movimientos y alertas de reabastecimiento |
| **Ventas (POS)** | Punto de venta, cobro, NCF y emisión de comprobantes |
| **Compras** | Proveedores, órdenes de compra y recepción de mercancía |
| **Clientes** | Cartera de clientes, RNC o cédula y tipo de comprobante |
| **Finanzas** | Ingresos, egresos, costos y reportes de rentabilidad |

---

## Arquitectura

Modular en capas: **modular** por módulo de negocio, **en capas** por responsabilidad.
Las capas internas nunca dependen de las externas.

```
MiniERP.Domain          Entidades y reglas de negocio puras. Sin dependencias.
      ^
MiniERP.Application     Casos de uso, DTOs y contratos.
      ^
MiniERP.Infrastructure  EF Core, SQL Server, Identity, repositorios.
      ^
MiniERP.Web             Blazor Server. Solo presentación.
```

Dentro de cada capa hay una carpeta por módulo, de modo que la estructura de
directorios refleja directamente la arquitectura descrita en el Capítulo III.

```
src/
├── MiniERP.Domain/          Core · Clientes · Inventario · Compras · Ventas · Finanzas · Shared
├── MiniERP.Application/     Core · Clientes · Inventario · Compras · Ventas · Finanzas · Common
├── MiniERP.Infrastructure/  Persistence · Identity · Services
└── MiniERP.Web/             Components/Pages/{Clientes, Inventario, Compras, Ventas, Finanzas}
tests/
└── MiniERP.Tests/           Una carpeta de pruebas por módulo
```

---

## Cómo levantarlo

**Requisitos:** .NET 10 SDK · SQL Server 2022 en `localhost` con autenticación de Windows.

```bash
git clone https://github.com/jeisonsantoslogictool/MiniERP.git
cd MiniERP
dotnet run --project src/MiniERP.Web
```

Eso es todo. **Arrancar la aplicación es el único paso de instalación.** Al iniciar, el
sistema crea la base de datos si no existe, aplica las migraciones pendientes y siembra
lo indispensable para operar:

| Se siembra | Contenido |
|---|---|
| Roles | Administrador, Cajero, Almacen, Supervisor |
| Usuario administrador | `admin@minierp.local` |
| Unidades de medida | UND, LB, KG, GAL, LT, CAJ, PAQ |
| Categorías | Catálogo típico de minimarket (10) |

Todo el proceso es idempotente: correrlo mil veces produce el mismo resultado que
correrlo una.

### La contraseña del administrador

No hay ninguna contraseña escrita en este repositorio. La primera vez que arranca, el
sistema **genera una al azar y la escribe en la consola dentro de un recuadro**:

```
===============================================================
 ADMINISTRADOR INICIAL CREADO
   Usuario:    admin@minierp.local
   Contrasena: ................
===============================================================
```

Solo se muestra esa vez. Guárdala y cámbiala al entrar.

Para fijar una propia sin que llegue al repositorio:

```bash
dotnet user-secrets set "Seed:AdminPassword" "TuClave" --project src/MiniERP.Web
```

Si pierdes la contraseña, borra el usuario de `AspNetUsers` y vuelve a arrancar: se
genera una nueva.

### Notas de entorno

- La solución usa el formato **`.slnx`**. Requiere Visual Studio 2022 17.14 o superior,
  Rider, o VS Code. Con `dotnet build` funciona en cualquier caso.
- Las versiones de todos los paquetes NuGet se declaran en **`Directory.Packages.props`**,
  nunca en los `.csproj`. Así ambos desarrolladores compilan contra lo mismo.
- El SDK está fijado en `global.json` para evitar diferencias entre máquinas.
- La cadena de conexión usa autenticación de Windows: **no hay contraseñas en el repositorio**.

---

## Comandos útiles

```bash
dotnet build MiniERP.slnx                   # compilar todo
dotnet test                                 # correr las pruebas
dotnet run --project src/MiniERP.Web        # levantar la aplicación

# Nueva migración (siempre desde Infrastructure)
dotnet ef migrations add <Nombre> \
  --project src/MiniERP.Infrastructure \
  --startup-project src/MiniERP.Web \
  --output-dir Persistence/Migrations
```

---

## Documentación

- [Plan maestro](Docs/Plan-Maestro.md) — fases, gates, reparto del equipo y cronograma.

## Fuera de alcance

Definido así en el anteproyecto y sostenido a lo largo del proyecto:

- Certificación fiscal definitiva ante la DGII (se genera el XML e-CF y el QR, hasta ahí)
- Analítica predictiva
- Integración con plataformas de comercio electrónico
- Nómina, producción industrial y logística de distribución
