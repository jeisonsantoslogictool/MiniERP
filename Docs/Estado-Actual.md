# Estado actual del proyecto

Última revisión: **22 de julio de 2026**.

Este documento contrasta el Plan Maestro y las asignaciones con el contenido real de
`main`. Se creó porque algunos textos de trabajo describen el estado que tenía el proyecto
antes de integrar las ramas y, por tanto, ya no son una fuente fiable por sí solos.

## Resumen ejecutivo

El núcleo funcional planificado para **F0, F1, F2 y F3 está construido**.
La solución compila sin errores ni advertencias y la suite tiene **183 pruebas en verde**.

La devolución a proveedor se cerró en la rama `dionis/cierre-asignaciones`: incluye
dominio, ajuste del saldo, repositorio y configuración EF Core, migración, registro de
dependencias, pantallas y pruebas. Su verificación en navegador requiere aplicar la
migración en un ambiente donde SQL Server esté disponible.

También faltan pruebas de integración, documentación académica de varios módulos y las
fases de piloto/cierre. Estas últimas requieren trabajo del equipo y datos externos; no
se pueden completar solamente modificando este repositorio.

## Inventario por fase

| Fase | Estado verificado | Evidencia principal |
|---|---|---|
| F0 — base | **Completa** | Cuatro capas, SQL Server/EF Core, Identity, roles, siembra y login en español. |
| F1 — catálogos | **Completa** | Productos, categorías, unidades, existencias, movimientos, alertas y clientes. |
| F2 — movimiento | **Completa** | Proveedores, compras/recepción, POS, NCF, factura, impresión y anulación. |
| F3 — dinero y fiscal | **Completa en código** | Cobros, pagos, CxC, CxP, estados de cuenta, devolución, egresos, cinco reportes financieros y e-CF/QR simulado. |
| F4 — integración y piloto | **Pendiente** | Hay pruebas de dominio, pero no un plan formal de integración ejecutado ni evidencia de un día completo en el comercio piloto. |
| F5 — cierre | **Pendiente** | Faltan validación final, resultados del piloto, Capítulo V, revisión y defensa. |

## Funcionalidad existente

### Inventario

- Productos, categorías y unidades de medida.
- Existencia, costo promedio ponderado y kardex/movimientos.
- Ajustes y alertas de reabastecimiento.
- Integración con recepción de compras, ventas y anulación de facturas.

### Ventas

- POS con búsqueda, carrito, contado/crédito y cliente.
- Emisión atómica de factura y NCF.
- Lista y detalle de facturas, impresión y anulación con motivo.
- Administración de secuencias de NCF.
- XML e-CF y QR **simulados**, rotulados como no válidos ante la DGII.

### Clientes

- Registro y edición, documento y comprobante preferido.
- Ventas a crédito, cobros, balance, cuentas por cobrar y vencimiento.
- Estado de cuenta/historial con facturas y abonos.

### Compras

- Proveedores, compras, líneas y recepción de mercancía.
- Actualización de inventario y costo promedio.
- Compras a crédito, pagos, balance, cuentas por pagar y vencimiento.
- Devolución a proveedor con inventario, saldo, persistencia y pantallas.

### Finanzas

- Categorías y registro de egresos.
- Reportes de ingresos y rentabilidad.
- Estado de resultados y flujo de caja.
- Panel financiero.
- Separación correcta entre costo/gasto y salida de efectivo: un pago a proveedor afecta
  el flujo de caja, pero no vuelve a reducir la utilidad.

### Seguridad e interfaz

- ASP.NET Core Identity, roles y administrador inicial.
- Login y administración de cuenta en español.
- Tema y navegación propios.
- Los últimos textos secundarios de la plantilla (ayuda de acceso externo y página
  técnica de error) se tradujeron durante esta revisión.

## Calidad verificada

Comandos ejecutados sobre `main` durante esta revisión:

```text
dotnet build MiniERP.slnx --no-restore
Resultado: 0 errores, 0 advertencias

dotnet test MiniERP.slnx --no-build --no-restore
Resultado: 183 superadas, 0 fallidas, 0 omitidas
```

La cobertura actual se concentra en dominio y en el servicio e-CF. No sustituye estas
pruebas que aún hacen falta:

1. Integración de la transacción de emisión: si falla después de reservar el NCF, el
   rollback debe dejarlo disponible.
2. Integración compra → inventario → venta → anulación.
3. Integración crédito → cobro/pago → balances y reportes.
4. Persistencia y confirmación de devolución a proveedor contra SQL Server.
5. Pruebas funcionales de las pantallas críticas dentro del plan formal del Capítulo 5.1.

## Documentación académica

Dentro del repositorio sí existen:

- Plan Maestro y asignaciones.
- Diseños/planes técnicos de devolución, egresos y reportes financieros.
- Capítulo IV de Egresos.
- Capítulo IV de Reportes de Finanzas, con capturas de verificación.

No se encontraron dentro del repositorio:

- El informe principal con los capítulos I, II y III.
- Secciones del Capítulo IV de Inventario y Ventas.
- Capturas funcionales para las nuevas secciones de Clientes y Compras.
- Capítulo V y resultados del piloto.
- Entrevista, instrumentos, diagramas UML/modelo de datos y evidencia del día piloto.

Es posible que esos archivos vivan fuera de GitHub. Si existen en Word, Drive u otra
ubicación, deben incorporarse o enlazarse para que el repositorio represente todo el
estado del trabajo.

## Orden recomendado de cierre

1. **Aplicar la migración y verificar la devolución en navegador** con SQL Server activo.
2. **Ejecutar el plan de pruebas 5.1**, empezando por los escenarios de
   integración indicados arriba.
3. **Escribir Inventario/Ventas y completar capturas de Clientes/Compras**.
4. **Reunir el informe externo** (I–III), corregir la referencia temporal del Capítulo I
   y consolidar diagramas/requerimientos.
5. **Preparar el piloto**, cargar NCF autorizados y datos reales, capacitar al usuario y
   operar un día completo.
6. **Capturar indicadores y cerrar el Capítulo V**; después congelar código y ensayar la
   defensa.

## Límites que no son faltantes

- La certificación fiscal real y la firma digital ante la DGII están fuera del alcance.
- Analítica predictiva, comercio electrónico, aplicación móvil/offline, nómina,
  producción y logística de distribución también están fuera del alcance acordado.
