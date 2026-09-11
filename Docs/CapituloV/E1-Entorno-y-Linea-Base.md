# E1 — Entorno reproducible y línea base

Piloto de validación del Mini ERP · Capítulo V · Trabajo Final de Grado Grupo 6

---

## 1. Versión auditada

Toda la validación se ejecuta contra un estado del repositorio fijado y verificable.

| | |
|---|---|
| **Commit** | `b3dff8188376b6a94ebeebdfbb37f7d4d94e8668` |
| **Rama** | `samuel/permisos` |
| **Fecha del commit** | 27/07/2026 22:15 (−04:00) |
| **Asunto** | Finanzas: gatea las pantallas y las acciones por permiso (4.8) |

### Advertencia de estado: el repositorio no está en un solo punto

Esto condiciona toda la validación de seguridad y **debe declararse en el informe**:

| Commit | Contenido | ¿Está en `main`? |
|---|---|---|
| `b3dff81` | Gateo por permiso de las 7 pantallas de Finanzas | **No** |
| `2824c9c` | Andamio provisional del núcleo de permisos | **No** |
| `5014b22` | Plan de usuarios y permisos (documentación) | **No** |

Además, en ramas **no integradas** existe trabajo que cambia el veredicto de seguridad:

- `jeison/seguridad` — núcleo de permisos completo, pantallas de usuarios, menú gateado y
  enforcement de Inventario y Ventas.
- `dionis/permisos` — enforcement de Clientes y Compras.

**Consecuencia:** en `main`, hoy, el sistema **no tiene ningún control de acceso por rol o
permiso**: toda pantalla exige únicamente sesión iniciada. El árbol auditado aquí sí protege
Finanzas. Ambos hechos se reportan por separado y no deben confundirse.

---

## 2. Camino elegido: Camino A (Windows + SQL Server local)

Se elige el **Camino A** por tres razones:

1. Es el entorno para el que el proyecto fue escrito: la cadena de conexión usa autenticación
   integrada de Windows, y el propio `CLAUDE.md` del proyecto lo fija como requisito.
2. La asignación atómica del NCF usa `UPDATE ... OUTPUT`, sintaxis propia de SQL Server. Un
   motor distinto invalidaría el cuadre nº 5 de la Fase 5.
3. Evita introducir una variable (contenedor, red, latencia) en las mediciones de rendimiento
   de la Fase 7, que deben reflejar el equipo real del comercio piloto.

El Camino B queda descartado y esa decisión se declara: los tiempos medidos corresponden a
una máquina de desarrollo, no a un equipo de mostrador. Ver §6.

---

## 3. Entorno medido

### 3.1 Equipo

| Componente | Valor |
|---|---|
| Sistema operativo | Microsoft Windows 11 Pro · versión 10.0.26200 · 64 bits |
| Procesador | 13th Gen Intel Core i7-13650HX |
| Núcleos | 14 físicos / 20 lógicos |
| Memoria RAM | 23.6 GB |

### 3.2 Plataforma

| Componente | Versión |
|---|---|
| SDK de .NET | 10.0.302 |
| Runtime `Microsoft.NETCore.App` | 10.0.10 |
| Runtime `Microsoft.AspNetCore.App` | 10.0.10 |
| Motor de base de datos | Microsoft SQL Server 2022 (RTM) — 16.0.1000.6 (X64) |
| Edición | Developer Edition (64-bit) |
| Autenticación | Integrada de Windows |

**Nota sobre el hardware.** El equipo de medición es sensiblemente más potente que el de un
colmado. Los tiempos de la Fase 7 deben leerse como **cota inferior**: en el equipo real serán
iguales o peores. Esta limitación se declara expresamente en el informe final.

---

## 4. Pasos exactos de instalación

Reproducibles por un tercero desde cero.

```bash
# 1. Clonar y situarse en la versión auditada
git clone https://github.com/jeisonsantoslogictool/MiniERP.git
cd MiniERP
git checkout b3dff81

# 2. Apuntar a la base desechable del piloto (NUNCA en appsettings.json)
dotnet user-secrets set "ConnectionStrings:DefaultConnection" \
  "Server=<instancia>;Database=MiniERP_Piloto;Trusted_Connection=True;TrustServerCertificate=True" \
  --project src/MiniERP.Web

# 3. Fijar la contraseña del administrador para que sea reproducible
dotnet user-secrets set "Seed:AdminPassword" "<clave>" --project src/MiniERP.Web

# 4. Compilar
dotnet build MiniERP.slnx

# 5. Arrancar (crea la base, migra y siembra en un solo paso)
dotnet run --project src/MiniERP.Web
```

**Cumplimiento de la regla 7 del encargo:** la cadena de conexión y la contraseña del
administrador viven en `dotnet user-secrets`, fuera del repositorio. No se escribe ningún
secreto en archivos versionados. `appsettings.json` no se modifica.

**Base desechable:** el piloto usa `MiniERP_Piloto`, distinta de la base `MiniERP` que emplean
los desarrolladores. Ninguna operación del piloto toca datos de trabajo del equipo.

---

## 5. Línea base de compilación y pruebas

Medida sobre el commit auditado, antes de introducir ningún cambio.

### 5.1 Compilación

| Medición | Resultado |
|---|---|
| Compilación limpia (`dotnet clean` + `dotnet build`) | **12.6 s** |
| Errores | **0** |
| Advertencias | **0** |

### 5.2 Pruebas automatizadas existentes

| Medición | Resultado |
|---|---|
| Pruebas ejecutadas | **180** |
| Aprobadas | **180** |
| Fallidas | **0** |
| Omitidas | **0** |
| Tiempo de ejecución de la serie | **76 ms** |
| Tiempo total de `dotnet test` | 5.0 s |

**Nada falla antes de que el equipo de pruebas toque el sistema.** La línea base está limpia,
de modo que cualquier fallo posterior es atribuible al piloto y no a un defecto preexistente
en la suite.

### 5.3 Composición de la suite existente

154 métodos marcados con `[Fact]` o `[Theory]`, que producen 180 casos ejecutados
(las teorías aportan varias filas cada una).

| Archivo | Casos declarados |
|---|---|
| `Ventas/FacturaTests.cs` | 19 |
| `Compras/CompraTests.cs` | 19 |
| `Ventas/EcfServiceTests.cs` | 18 |
| `Inventario/ProductoTests.cs` | 13 |
| `Compras/DevolucionCompraTests.cs` | 13 |
| `Ventas/SecuenciaNcfTests.cs` | 12 |
| `Ventas/EcfTests.cs` | 12 |
| `Clientes/ClienteTests.cs` | 12 |
| `Finanzas/ResumenRentabilidadTests.cs` | 8 |
| `Finanzas/ResumenIngresosTests.cs` | 6 |
| `Inventario/UnidadMedidaTests.cs` | 5 |
| `Finanzas/EgresoTests.cs` | 5 |
| `Finanzas/FlujoCajaTests.cs` | 3 |
| `Finanzas/EstadoResultadosTests.cs` | 3 |
| `Compras/PagoTests.cs` | 3 |
| `Clientes/CobroTests.cs` | 3 |

### 5.4 Hallazgo de partida: no existe ninguna prueba de integración

Verificado por búsqueda en todo `tests/`: **no aparece ni una sola referencia** a
`MiniErpDbContext`, `DbContextOptions`, `UseSqlServer`, `UseInMemory`, `Sqlite` ni
`WebApplicationFactory`.

Es decir:

- **Cero pruebas tocan la base de datos.**
- **Cero pruebas ejercitan un repositorio.**
- **Cero pruebas ejercitan una pantalla.**

Las 180 pruebas son unitarias de dominio y de servicios puros (calculadoras y validaciones),
que se ejecutan en 76 milisegundos precisamente porque no hay entrada/salida.

**Qué significa esto para el piloto.** Todo lo que este trabajo debe demostrar —que el NCF no
se duplica bajo concurrencia, que una transacción fallida no deja inventario a medias, que los
saldos cuadran tras 20 días, que los reportes coinciden con la base— **está fuera del alcance
de la suite existente por construcción**. No es un defecto de las pruebas actuales: es que
cubren otra cosa.

En consecuencia, el arnés de integración de la Fase 3 **se construye desde cero**, en un
proyecto nuevo bajo `tests/`, sin tocar `src/`.

---

## 6. Limitaciones declaradas del entorno

Se declaran por anticipado para que no se confundan con hallazgos:

1. **El hardware no es el del comercio.** Los tiempos son cota inferior optimista.
2. **Una sola instancia, un solo equipo.** No se valida despliegue en red ni varios terminales
   físicos; la concurrencia de la Fase 6 se simula con sesiones simultáneas contra el mismo
   servidor.
3. **Las secuencias NCF sembradas no son autorizaciones reales de la DGII.** El propio sistema
   lo advierte en pantalla en `/ventas/ncf`. Ninguna factura emitida en el piloto es un
   comprobante fiscal válido.
4. **El e-CF es simulado por decisión de alcance del proyecto**, no por defecto del sistema:
   no hay firma digital ni envío a la DGII. Se clasifica como *limitación declarada*, nunca
   como defecto.
5. **Las fechas de 20 días se simulan.** La técnica y su verificación se documentan en E2.
