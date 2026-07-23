# Plan de pruebas — Sección 5.1

Versión: 22 de julio de 2026.

## Objetivo

Comprobar que los cinco módulos funcionan individualmente y que las operaciones que cruzan
módulos mantienen inventario, balances, rentabilidad y efectivo consistentes.

## Niveles

| Nivel | Alcance | Evidencia |
|---|---|---|
| Unitario | Entidades y cálculos de dominio | Resultado de `dotnet test`. |
| Servicio | Coordinación de casos de uso | Pruebas con repositorios controlados. |
| Integración | EF Core, transacciones y SQL Server | Resultado y datos antes/después. |
| Funcional | Flujos completos en Blazor | Capturas y lista firmada por responsable. |
| Piloto | Jornada real en el comercio | Incidencias e indicadores del Capítulo V. |

## Casos críticos automatizados

| ID | Caso | Resultado esperado | Estado |
|---|---|---|---|
| A-01 | Reglas de inventario, clientes, compras, ventas y finanzas | Toda la suite pasa | Verificado |
| A-02 | Devolución baja inventario | Existencia disminuye exactamente | Verificado |
| A-03 | Devolución ajusta saldo | Guarda balance anterior/resultante | Verificado |
| A-04 | Confirmación de devolución | Un solo guardado coordina todos los cambios | Verificado |
| A-05 | Pago no cambia utilidad | Solo afecta flujo de caja | Verificado |
| A-06 | e-CF/QR simulado | Documento válido internamente y rotulado | Verificado |

## Casos de integración pendientes de ejecutar con SQL Server

| ID | Preparación y pasos | Criterio de aceptación |
|---|---|---|
| I-01 | Forzar un fallo después de reservar NCF durante emisión | La transacción revierte factura, stock, balance y NCF. |
| I-02 | Recibir 10 unidades y vender 3 | Stock final 7 y costo congelado en la factura. |
| I-03 | Venta a crédito 500; cobro 200 | Balance 300 y estado de cuenta con ambas operaciones. |
| I-04 | Compra a crédito; pago total | Balance 0; utilidad intacta; flujo disminuye. |
| I-05 | Compra 10; devolver 3 | Stock baja 3, saldo baja por total, movimiento y documento confirmados. |
| I-06 | Intentar devolver más de comprado o disponible | No persiste ningún cambio parcial. |

## Casos funcionales de Dionis

### Clientes

1. Crear cliente con RNC válido.
2. Emitir venta a crédito.
3. Verificar cuentas por cobrar y vencimiento.
4. Registrar abono parcial.
5. Verificar balance e historial.
6. Intentar cobrar más que el saldo.

### Compras

1. Crear proveedor y compra.
2. Recibir mercancía y comprobar existencia/costo.
3. Registrar pago y comprobar saldo.
4. Consultar cuentas por pagar.
5. Crear devolución parcial desde la compra.
6. Confirmar y comprobar inventario, kardex y balance.
7. Intentar una segunda devolución que exceda el máximo.

## Registro de ejecución

Por cada caso funcional se debe guardar:

- fecha, ambiente, versión/commit y responsable;
- datos usados y resultado esperado/obtenido;
- captura antes y después cuando cambie un saldo;
- incidencia encontrada y commit que la corrige;
- firma o aceptación del usuario piloto para la jornada real.

## Comandos de regresión

```bash
dotnet build MiniERP.slnx
dotnet test MiniERP.slnx
dotnet ef database update \
  --project src/MiniERP.Infrastructure \
  --startup-project src/MiniERP.Web
dotnet run --project src/MiniERP.Web
```

La prueba funcional no se declara aprobada únicamente porque el proyecto compile. Requiere
ejecutar el caso contra SQL Server y conservar su evidencia.

