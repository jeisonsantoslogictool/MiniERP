/* ============================================================================
   E6 - VERIFICACIONES DE INTEGRIDAD DEL PILOTO
   Mini ERP - Capitulo V - Trabajo Final de Grado Grupo 6

   Ejecutar al cierre de CADA dia simulado y al final de los 20 dias.

   COMO SE LEE: cada cuadre devuelve una fila con Cuadre, Titulo, Descuadres y
   Veredicto. Veredicto = 'OK' significa que cuadra. Cualquier otro valor es un
   DEFECTO CRITICO: no hay descuadres aceptables.

   Las consultas de detalle (marcadas DETALLE) solo devuelven filas cuando hay
   problema; si no devuelven nada, es que todo cuadro.

   Valores de enumeracion usados (verificados en el codigo):
     TipoMovimiento : 1 Entrada, 2 Salida, 3 Ajuste+, 4 Ajuste-, 5 Merma,
                      6 DevolucionCliente, 7 DevolucionProveedor
     EstadoFactura  : 1 Emitida, 2 Anulada
     EstadoCompra   : 1 Borrador, 2 Recibida, 3 Anulada
     CondicionPago  : 1 Contado, 2 Credito
   ============================================================================ */

SET NOCOUNT ON;

DECLARE @Resultados TABLE (
    Cuadre     INT,
    Titulo     NVARCHAR(200),
    Descuadres INT,
    Veredicto  NVARCHAR(40)
);

/* Criterio de signo usado en todo el script:
   SUMAN existencia   -> Entrada (1), Ajuste+ (3), DevolucionCliente (6)
   RESTAN existencia  -> Salida (2), Ajuste- (4), Merma (5), DevolucionProveedor (7) */


/* ----------------------------------------------------------------------------
   CUADRE 1 - La existencia almacenada es igual a la suma algebraica de sus
   movimientos, y tambien a la existencia resultante de su ultimo movimiento.
   ---------------------------------------------------------------------------- */
;WITH Suma AS (
    SELECT m.ProductoId,
           SUM(CASE WHEN m.Tipo IN (1,3,6) THEN m.Cantidad ELSE -m.Cantidad END) AS Calculada
    FROM MovimientosInventario m
    GROUP BY m.ProductoId
),
Ultimo AS (
    SELECT ProductoId, ExistenciaResultante
    FROM (SELECT m.ProductoId, m.ExistenciaResultante,
                 ROW_NUMBER() OVER (PARTITION BY m.ProductoId ORDER BY m.Fecha DESC, m.Id DESC) AS rn
          FROM MovimientosInventario m) t
    WHERE rn = 1
)
INSERT INTO @Resultados
SELECT 1,
       N'Existencia = suma de movimientos = saldo del ultimo movimiento',
       COUNT(*),
       CASE WHEN COUNT(*) = 0 THEN 'OK' ELSE 'DESCUADRE' END
FROM Productos p
LEFT JOIN Suma   s ON s.ProductoId = p.Id
LEFT JOIN Ultimo u ON u.ProductoId = p.Id
WHERE p.ManejaInventario = 1
  AND (p.Existencia <> ISNULL(s.Calculada, 0)
       OR p.Existencia <> ISNULL(u.ExistenciaResultante, 0));

-- DETALLE 1
;WITH Suma AS (
    SELECT m.ProductoId,
           SUM(CASE WHEN m.Tipo IN (1,3,6) THEN m.Cantidad ELSE -m.Cantidad END) AS Calculada
    FROM MovimientosInventario m GROUP BY m.ProductoId
),
Ultimo AS (
    SELECT ProductoId, ExistenciaResultante FROM
    (SELECT m.ProductoId, m.ExistenciaResultante,
            ROW_NUMBER() OVER (PARTITION BY m.ProductoId ORDER BY m.Fecha DESC, m.Id DESC) rn
     FROM MovimientosInventario m) t WHERE rn = 1
)
SELECT p.Codigo, p.Descripcion,
       p.Existencia AS Almacenada,
       ISNULL(s.Calculada, 0) AS SumaDeMovimientos,
       ISNULL(u.ExistenciaResultante, 0) AS SaldoUltimoMovimiento
FROM Productos p
LEFT JOIN Suma s ON s.ProductoId = p.Id
LEFT JOIN Ultimo u ON u.ProductoId = p.Id
WHERE p.ManejaInventario = 1
  AND (p.Existencia <> ISNULL(s.Calculada,0) OR p.Existencia <> ISNULL(u.ExistenciaResultante,0));


/* ----------------------------------------------------------------------------
   CUADRE 2 - En cada movimiento, la existencia resultante es la anterior mas o
   menos la cantidad, segun el tipo.
   ---------------------------------------------------------------------------- */
INSERT INTO @Resultados
SELECT 2,
       N'Cada movimiento: resultante = anterior +/- cantidad',
       COUNT(*),
       CASE WHEN COUNT(*) = 0 THEN 'OK' ELSE 'DESCUADRE' END
FROM MovimientosInventario m
WHERE m.ExistenciaResultante <>
      m.ExistenciaAnterior + CASE WHEN m.Tipo IN (1,3,6) THEN m.Cantidad ELSE -m.Cantidad END;

-- DETALLE 2
SELECT m.Id, p.Codigo, m.Fecha, m.Tipo, m.Cantidad,
       m.ExistenciaAnterior, m.ExistenciaResultante,
       m.ExistenciaAnterior + CASE WHEN m.Tipo IN (1,3,6) THEN m.Cantidad ELSE -m.Cantidad END AS Esperada
FROM MovimientosInventario m
JOIN Productos p ON p.Id = m.ProductoId
WHERE m.ExistenciaResultante <>
      m.ExistenciaAnterior + CASE WHEN m.Tipo IN (1,3,6) THEN m.Cantidad ELSE -m.Cantidad END;


/* ----------------------------------------------------------------------------
   CUADRE 3 - Ninguna existencia quedo negativa en ningun momento de los 20 dias.
   Se revisan los saldos historicos, no solo el actual.
   ---------------------------------------------------------------------------- */
INSERT INTO @Resultados
SELECT 3,
       N'Ninguna existencia negativa, ni actual ni historica',
       (SELECT COUNT(*) FROM MovimientosInventario WHERE ExistenciaResultante < 0)
     + (SELECT COUNT(*) FROM Productos WHERE Existencia < 0),
       CASE WHEN (SELECT COUNT(*) FROM MovimientosInventario WHERE ExistenciaResultante < 0)
               + (SELECT COUNT(*) FROM Productos WHERE Existencia < 0) = 0
            THEN 'OK' ELSE 'CRITICO' END;

-- DETALLE 3
SELECT p.Codigo, m.Id AS MovimientoId, m.Fecha, m.Tipo, m.ExistenciaResultante
FROM MovimientosInventario m JOIN Productos p ON p.Id = m.ProductoId
WHERE m.ExistenciaResultante < 0;


/* ----------------------------------------------------------------------------
   CUADRE 4 - Todo movimiento con referencia apunta a un documento que existe.
   ---------------------------------------------------------------------------- */
/* Tipos que SI deben traer un documento detras: FACTURA, ANULACION y COMPRA.
   APERTURA y AJUSTE nacen sin documento y llevan ReferenciaId nulo: es correcto. */
INSERT INTO @Resultados
SELECT 4,
       N'Las referencias de los movimientos apuntan a documentos existentes',
       COUNT(*),
       CASE WHEN COUNT(*) = 0 THEN 'OK' ELSE 'DESCUADRE' END
FROM MovimientosInventario m
WHERE m.ReferenciaTipo IN ('FACTURA', 'ANULACION', 'COMPRA')
  AND (ISNULL(m.ReferenciaId, 0) = 0
    OR (m.ReferenciaTipo IN ('FACTURA','ANULACION')
        AND NOT EXISTS (SELECT 1 FROM Facturas f WHERE f.Id = m.ReferenciaId))
    OR (m.ReferenciaTipo = 'COMPRA'
        AND NOT EXISTS (SELECT 1 FROM Compras c WHERE c.Id = m.ReferenciaId)));

-- DETALLE 4
SELECT m.Id, m.ReferenciaTipo, m.ReferenciaId, LEFT(m.Motivo, 60) AS Motivo,
       CASE WHEN ISNULL(m.ReferenciaId,0) = 0 THEN 'Referencia sin resolver (id en cero)'
            ELSE 'Apunta a un documento inexistente' END AS Problema
FROM MovimientosInventario m
WHERE m.ReferenciaTipo IN ('FACTURA', 'ANULACION', 'COMPRA')
  AND (ISNULL(m.ReferenciaId, 0) = 0
    OR (m.ReferenciaTipo IN ('FACTURA','ANULACION')
        AND NOT EXISTS (SELECT 1 FROM Facturas f WHERE f.Id = m.ReferenciaId))
    OR (m.ReferenciaTipo = 'COMPRA'
        AND NOT EXISTS (SELECT 1 FROM Compras c WHERE c.Id = m.ReferenciaId)));


/* ----------------------------------------------------------------------------
   CUADRE 5 - NCF: ninguno repetido, y el contador de cada secuencia coincide
   con el mayor correlativo emitido de su prefijo.
   ---------------------------------------------------------------------------- */
INSERT INTO @Resultados
SELECT 5,
       N'NCF sin duplicados y contador de secuencia coherente',
       (SELECT COUNT(*) FROM (SELECT Ncf FROM Facturas WHERE Ncf IS NOT NULL
                              GROUP BY Ncf HAVING COUNT(*) > 1) d),
       CASE WHEN (SELECT COUNT(*) FROM (SELECT Ncf FROM Facturas WHERE Ncf IS NOT NULL
                                        GROUP BY Ncf HAVING COUNT(*) > 1) d) = 0
            THEN 'OK' ELSE 'CRITICO' END;

-- DETALLE 5a - NCF duplicados (un duplicado es infraccion ante la DGII)
SELECT Ncf, COUNT(*) AS Veces
FROM Facturas WHERE Ncf IS NOT NULL
GROUP BY Ncf HAVING COUNT(*) > 1;

-- DETALLE 5b - Contador frente al mayor emitido, y saltos en la numeracion
SELECT s.Prefijo, s.Desde, s.Hasta, s.Actual AS ContadorSecuencia,
       MAX(TRY_CAST(SUBSTRING(f.Ncf, 4, 20) AS BIGINT)) AS MayorEmitido,
       COUNT(f.Id)  AS ComprobantesEmitidos,
       MAX(TRY_CAST(SUBSTRING(f.Ncf, 4, 20) AS BIGINT))
         - MIN(TRY_CAST(SUBSTRING(f.Ncf, 4, 20) AS BIGINT)) + 1 - COUNT(f.Id) AS SaltosSinExplicar
FROM SecuenciasNcf s
LEFT JOIN Facturas f ON LEFT(f.Ncf, 3) = s.Prefijo
GROUP BY s.Prefijo, s.Desde, s.Hasta, s.Actual;


/* ----------------------------------------------------------------------------
   CUADRE 6 - Balance del cliente = facturas a credito no anuladas - cobros.
   ---------------------------------------------------------------------------- */
;WITH Fact AS (
    SELECT ClienteId, SUM(Total) AS Debe FROM Facturas
    WHERE Condicion = 2 AND Estado = 1 AND ClienteId IS NOT NULL GROUP BY ClienteId
),
Cob AS (SELECT ClienteId, SUM(Monto) AS Pago FROM Cobros GROUP BY ClienteId)
INSERT INTO @Resultados
SELECT 6,
       N'Balance del cliente = facturas a credito no anuladas - cobros',
       COUNT(*),
       CASE WHEN COUNT(*) = 0 THEN 'OK' ELSE 'CRITICO' END
FROM Clientes c
LEFT JOIN Fact f ON f.ClienteId = c.Id
LEFT JOIN Cob  o ON o.ClienteId = c.Id
WHERE c.BalanceActual <> ISNULL(f.Debe,0) - ISNULL(o.Pago,0);

-- DETALLE 6
;WITH Fact AS (
    SELECT ClienteId, SUM(Total) AS Debe FROM Facturas
    WHERE Condicion = 2 AND Estado = 1 AND ClienteId IS NOT NULL GROUP BY ClienteId
),
Cob AS (SELECT ClienteId, SUM(Monto) AS Pago FROM Cobros GROUP BY ClienteId)
SELECT c.Codigo, c.Nombre, c.BalanceActual,
       ISNULL(f.Debe,0) AS FacturadoCredito, ISNULL(o.Pago,0) AS Cobrado,
       ISNULL(f.Debe,0) - ISNULL(o.Pago,0) AS BalanceEsperado
FROM Clientes c
LEFT JOIN Fact f ON f.ClienteId = c.Id
LEFT JOIN Cob  o ON o.ClienteId = c.Id
WHERE c.BalanceActual <> ISNULL(f.Debe,0) - ISNULL(o.Pago,0);


/* ----------------------------------------------------------------------------
   CUADRE 7 - Balance del proveedor = compras a credito recibidas - pagos.
   ---------------------------------------------------------------------------- */
;WITH Comp AS (
    SELECT ProveedorId, SUM(Total) AS Debe FROM Compras
    WHERE Condicion = 2 AND Estado = 2 GROUP BY ProveedorId
),
Pag AS (SELECT ProveedorId, SUM(Monto) AS Pagado FROM Pagos GROUP BY ProveedorId)
INSERT INTO @Resultados
SELECT 7,
       N'Balance del proveedor = compras a credito recibidas - pagos',
       COUNT(*),
       CASE WHEN COUNT(*) = 0 THEN 'OK' ELSE 'CRITICO' END
FROM Proveedores p
LEFT JOIN Comp c ON c.ProveedorId = p.Id
LEFT JOIN Pag  g ON g.ProveedorId = p.Id
WHERE p.BalanceActual <> ISNULL(c.Debe,0) - ISNULL(g.Pagado,0);

-- DETALLE 7
;WITH Comp AS (
    SELECT ProveedorId, SUM(Total) AS Debe FROM Compras
    WHERE Condicion = 2 AND Estado = 2 GROUP BY ProveedorId
),
Pag AS (SELECT ProveedorId, SUM(Monto) AS Pagado FROM Pagos GROUP BY ProveedorId)
SELECT p.Codigo, p.Nombre, p.BalanceActual,
       ISNULL(c.Debe,0) AS CompradoCredito, ISNULL(g.Pagado,0) AS Pagado,
       ISNULL(c.Debe,0) - ISNULL(g.Pagado,0) AS BalanceEsperado
FROM Proveedores p
LEFT JOIN Comp c ON c.ProveedorId = p.Id
LEFT JOIN Pag  g ON g.ProveedorId = p.Id
WHERE p.BalanceActual <> ISNULL(c.Debe,0) - ISNULL(g.Pagado,0);


/* ----------------------------------------------------------------------------
   CUADRE 8 - La cadena de balances es continua: el balance anterior de cada
   asiento es el resultante del asiento previo del mismo tercero.
   ---------------------------------------------------------------------------- */
;WITH CadenaCobros AS (
    SELECT c.Id, c.ClienteId, c.Fecha, c.Monto, c.BalanceAnterior, c.BalanceResultante,
           LAG(c.BalanceResultante) OVER (PARTITION BY c.ClienteId ORDER BY c.Fecha, c.Id) AS PrevioResultante
    FROM Cobros c
),
CadenaPagos AS (
    SELECT p.Id, p.ProveedorId, p.Fecha, p.Monto, p.BalanceAnterior, p.BalanceResultante,
           LAG(p.BalanceResultante) OVER (PARTITION BY p.ProveedorId ORDER BY p.Fecha, p.Id) AS PrevioResultante
    FROM Pagos p
)
INSERT INTO @Resultados
SELECT 8,
       N'Cadena de balances continua en cobros y pagos',
       (SELECT COUNT(*) FROM CadenaCobros WHERE PrevioResultante IS NOT NULL AND BalanceAnterior <> PrevioResultante)
     + (SELECT COUNT(*) FROM CadenaPagos  WHERE PrevioResultante IS NOT NULL AND BalanceAnterior <> PrevioResultante)
     + (SELECT COUNT(*) FROM Cobros WHERE BalanceResultante <> BalanceAnterior - Monto)
     + (SELECT COUNT(*) FROM Pagos  WHERE BalanceResultante <> BalanceAnterior - Monto),
       CASE WHEN (SELECT COUNT(*) FROM CadenaCobros WHERE PrevioResultante IS NOT NULL AND BalanceAnterior <> PrevioResultante)
                + (SELECT COUNT(*) FROM CadenaPagos  WHERE PrevioResultante IS NOT NULL AND BalanceAnterior <> PrevioResultante)
                + (SELECT COUNT(*) FROM Cobros WHERE BalanceResultante <> BalanceAnterior - Monto)
                + (SELECT COUNT(*) FROM Pagos  WHERE BalanceResultante <> BalanceAnterior - Monto) = 0
            THEN 'OK' ELSE 'CRITICO' END;


/* ----------------------------------------------------------------------------
   CUADRE 9 - Factura: total = subtotal + ITBIS, y subtotal = suma de sus lineas.
   ---------------------------------------------------------------------------- */
;WITH Lin AS (
    SELECT l.FacturaId,
           SUM(ROUND(l.Cantidad * l.PrecioUnitario, 2)) AS SumaLineas,
           SUM(ROUND(ROUND(l.Cantidad * l.PrecioUnitario, 2) * l.TasaItbis, 2)) AS SumaItbis
    FROM LineasFactura l GROUP BY l.FacturaId
)
INSERT INTO @Resultados
SELECT 9,
       N'Factura: total = subtotal + ITBIS, y subtotal = suma de lineas',
       COUNT(*),
       CASE WHEN COUNT(*) = 0 THEN 'OK' ELSE 'CRITICO' END
FROM Facturas f
LEFT JOIN Lin l ON l.FacturaId = f.Id
WHERE f.Total <> f.Subtotal + f.Itbis
   OR ABS(f.Subtotal - ISNULL(l.SumaLineas, 0)) > 0.01;

-- DETALLE 9
;WITH Lin AS (
    SELECT l.FacturaId, SUM(ROUND(l.Cantidad * l.PrecioUnitario, 2)) AS SumaLineas
    FROM LineasFactura l GROUP BY l.FacturaId
)
SELECT f.Numero, f.Ncf, f.Subtotal, f.Itbis, f.Total,
       f.Subtotal + f.Itbis AS TotalEsperado, ISNULL(l.SumaLineas,0) AS SumaDeLineas
FROM Facturas f LEFT JOIN Lin l ON l.FacturaId = f.Id
WHERE f.Total <> f.Subtotal + f.Itbis OR ABS(f.Subtotal - ISNULL(l.SumaLineas,0)) > 0.01;


/* ----------------------------------------------------------------------------
   CUADRE 10 - Costo promedio ponderado tras cada recepcion.
   Esta consulta ENTREGA LOS INSUMOS; la comprobacion se hace a mano en hoja
   aparte para al menos 15 recepciones (exigencia del encargo).
   ---------------------------------------------------------------------------- */
SELECT c.Numero AS Compra, c.FechaRecepcion, p.Codigo, p.Descripcion,
       lc.Cantidad AS CantidadRecibida, lc.CostoUnitario AS CostoDeCompra,
       m.ExistenciaAnterior, m.ExistenciaResultante,
       p.Costo AS CostoActualDelProducto
FROM Compras c
JOIN LineasCompra lc ON lc.CompraId = c.Id
JOIN Productos p ON p.Id = lc.ProductoId
LEFT JOIN MovimientosInventario m
       ON m.ReferenciaTipo = 'COMPRA' AND m.ReferenciaId = c.Id AND m.ProductoId = p.Id
WHERE c.Estado = 2
ORDER BY c.FechaRecepcion, c.Numero, p.Codigo;


/* ----------------------------------------------------------------------------
   CUADRE 11 - El margen de una factura antigua no cambia tras una compra
   posterior que subio el costo. Se apoya en que LineaFactura congela el costo.
   ---------------------------------------------------------------------------- */
INSERT INTO @Resultados
SELECT 11,
       N'El costo congelado de las lineas nunca es nulo ni cero indebido',
       COUNT(*),
       CASE WHEN COUNT(*) = 0 THEN 'OK' ELSE 'DESCUADRE' END
FROM LineasFactura l
JOIN Productos p ON p.Id = l.ProductoId
WHERE p.ManejaInventario = 1 AND (l.CostoUnitario IS NULL OR l.CostoUnitario <= 0);

-- DETALLE 11 - Lineas cuyo costo congelado difiere del costo ACTUAL del producto.
-- Que difieran es lo CORRECTO si hubo compras posteriores; sirve de evidencia.
SELECT f.Numero, f.Fecha, p.Codigo, l.CostoUnitario AS CostoCongelado,
       p.Costo AS CostoActual,
       CASE WHEN l.CostoUnitario <> p.Costo THEN 'Difiere (correcto si hubo compra posterior)'
            ELSE 'Igual' END AS Lectura
FROM LineasFactura l
JOIN Facturas f ON f.Id = l.FacturaId
JOIN Productos p ON p.Id = l.ProductoId
ORDER BY f.Fecha, f.Numero;


/* ----------------------------------------------------------------------------
   CUADRE 12 - La anulacion repuso exactamente lo que la emision desconto,
   con el costo congelado de la linea.
   ---------------------------------------------------------------------------- */
/* IMPORTANTE: este cuadre NO se apoya en ReferenciaId, porque los movimientos de
   emision lo dejan en cero (defecto D-03). Se empareja por el NCF que ambos
   movimientos escriben en Motivo, que si es fiable. Si D-03 se corrige, este
   cuadre se puede simplificar volviendo a ReferenciaId. */
;WITH Salidas AS (
    SELECT f.Id AS FacturaId, m.ProductoId, SUM(m.Cantidad) AS Descontado
    FROM MovimientosInventario m
    JOIN Facturas f ON m.Motivo = 'Factura ' + f.Ncf
    WHERE m.Tipo = 2
    GROUP BY f.Id, m.ProductoId
),
Reposiciones AS (
    SELECT f.Id AS FacturaId, m.ProductoId, SUM(m.Cantidad) AS Repuesto
    FROM MovimientosInventario m
    JOIN Facturas f ON m.Motivo LIKE 'Anulacion de ' + f.Ncf + '%'
    WHERE m.Tipo = 6
    GROUP BY f.Id, m.ProductoId
)
INSERT INTO @Resultados
SELECT 12,
       N'La anulacion repone exactamente lo descontado',
       COUNT(*),
       CASE WHEN COUNT(*) = 0 THEN 'OK' ELSE 'CRITICO' END
FROM Facturas f
JOIN Salidas s ON s.FacturaId = f.Id
LEFT JOIN Reposiciones r ON r.FacturaId = f.Id AND r.ProductoId = s.ProductoId
WHERE f.Estado = 2 AND ISNULL(r.Repuesto, 0) <> s.Descontado;

-- DETALLE 12 - cada factura anulada, con lo que descontó y lo que repuso
SELECT f.Numero, f.Ncf, f.Total,
       CASE f.Condicion WHEN 1 THEN 'Contado' ELSE 'Credito' END AS Condicion,
       (SELECT COUNT(*) FROM LineasFactura l WHERE l.FacturaId = f.Id) AS Lineas,
       (SELECT ISNULL(SUM(m.Cantidad),0) FROM MovimientosInventario m
        WHERE m.Tipo = 2 AND m.Motivo = 'Factura ' + f.Ncf)              AS TotalDescontado,
       (SELECT ISNULL(SUM(m.Cantidad),0) FROM MovimientosInventario m
        WHERE m.Tipo = 6 AND m.Motivo LIKE 'Anulacion de ' + f.Ncf + '%') AS TotalRepuesto,
       LEFT(f.MotivoAnulacion, 50) AS Motivo
FROM Facturas f WHERE f.Estado = 2;


/* ----------------------------------------------------------------------------
   CUADRE 13 - Estado de resultados calculado de forma INDEPENDIENTE.
   Se compara a mano con lo que muestra la pantalla para el mismo rango.
   Ajustar las dos fechas antes de ejecutar.
   ---------------------------------------------------------------------------- */
DECLARE @Desde DATETIME2 = '2026-06-08T00:00:00';
DECLARE @Hasta DATETIME2 = '2026-07-01T00:00:00';   -- exclusivo

SELECT
    (SELECT ISNULL(SUM(f.Subtotal),0) FROM Facturas f
     WHERE f.Estado = 1 AND f.Fecha >= @Desde AND f.Fecha < @Hasta)                       AS Ingresos,
    (SELECT ISNULL(SUM(ROUND(l.Cantidad * l.CostoUnitario, 2)),0)
     FROM LineasFactura l JOIN Facturas f ON f.Id = l.FacturaId
     WHERE f.Estado = 1 AND f.Fecha >= @Desde AND f.Fecha < @Hasta)                       AS CostoDeLoVendido,
    (SELECT ISNULL(SUM(e.Monto),0) FROM Egresos e
     WHERE e.Fecha >= @Desde AND e.Fecha < @Hasta)                                        AS Egresos,
    (SELECT ISNULL(SUM(f.Subtotal),0) FROM Facturas f
     WHERE f.Estado = 1 AND f.Fecha >= @Desde AND f.Fecha < @Hasta)
  - (SELECT ISNULL(SUM(ROUND(l.Cantidad * l.CostoUnitario, 2)),0)
     FROM LineasFactura l JOIN Facturas f ON f.Id = l.FacturaId
     WHERE f.Estado = 1 AND f.Fecha >= @Desde AND f.Fecha < @Hasta)
  - (SELECT ISNULL(SUM(e.Monto),0) FROM Egresos e
     WHERE e.Fecha >= @Desde AND e.Fecha < @Hasta)                                        AS UtilidadEsperada;


/* ----------------------------------------------------------------------------
   CUADRE 14 - Flujo de caja calculado de forma INDEPENDIENTE.
   IMPORTANTE: aqui el contado entra CON ITBIS, porque es efectivo que si entro.
   ---------------------------------------------------------------------------- */
SELECT
    (SELECT ISNULL(SUM(f.Total),0) FROM Facturas f
     WHERE f.Estado = 1 AND f.Condicion = 1 AND f.Fecha >= @Desde AND f.Fecha < @Hasta)   AS VentasContado,
    (SELECT ISNULL(SUM(c.Monto),0) FROM Cobros c
     WHERE c.Fecha >= @Desde AND c.Fecha < @Hasta)                                        AS Cobros,
    (SELECT ISNULL(SUM(e.Monto),0) FROM Egresos e
     WHERE e.Fecha >= @Desde AND e.Fecha < @Hasta)                                        AS Egresos,
    (SELECT ISNULL(SUM(p.Monto),0) FROM Pagos p
     WHERE p.Fecha >= @Desde AND p.Fecha < @Hasta)                                        AS PagosAProveedor,
    (SELECT ISNULL(SUM(f.Total),0) FROM Facturas f
     WHERE f.Estado = 1 AND f.Condicion = 1 AND f.Fecha >= @Desde AND f.Fecha < @Hasta)
  + (SELECT ISNULL(SUM(c.Monto),0) FROM Cobros c WHERE c.Fecha >= @Desde AND c.Fecha < @Hasta)
  - (SELECT ISNULL(SUM(e.Monto),0) FROM Egresos e WHERE e.Fecha >= @Desde AND e.Fecha < @Hasta)
  - (SELECT ISNULL(SUM(p.Monto),0) FROM Pagos  p WHERE p.Fecha >= @Desde AND p.Fecha < @Hasta)
                                                                                          AS EfectivoNetoEsperado;


/* ----------------------------------------------------------------------------
   CUADRE 15 - Las cinco cifras del panel. Se comparan a mano con la pantalla.
   El panel usa UtcNow: OJO con el desfase de UTC-4 (hallazgo H-03).
   ---------------------------------------------------------------------------- */
DECLARE @Hoy DATE = CAST(SYSUTCDATETIME() AS DATE);
DECLARE @IniMes DATE = DATEFROMPARTS(YEAR(@Hoy), MONTH(@Hoy), 1);

SELECT
    (SELECT ISNULL(SUM(f.Total),0) FROM Facturas f
     WHERE f.Estado=1 AND CAST(f.Fecha AS DATE) = @Hoy)                                   AS VentasDia,
    (SELECT ISNULL(SUM(ROUND(l.Cantidad*l.PrecioUnitario,2) - ROUND(l.Cantidad*l.CostoUnitario,2)),0)
     FROM LineasFactura l JOIN Facturas f ON f.Id=l.FacturaId
     WHERE f.Estado=1 AND CAST(f.Fecha AS DATE) = @Hoy)                                   AS MargenDia,
    (SELECT ISNULL(SUM(e.Monto),0) FROM Egresos e
     WHERE CAST(e.Fecha AS DATE) BETWEEN @IniMes AND @Hoy)                                AS EgresosMes;


/* ----------------------------------------------------------------------------
   CUADRE 16 - Toda operacion tiene usuario responsable, y ese usuario existe.
   NOTA: detecta el defecto D-01 (el literal "Dionis"), porque ese valor no
   corresponde a ningun correo de AspNetUsers.
   ---------------------------------------------------------------------------- */
INSERT INTO @Resultados
SELECT 16,
       N'Toda operacion tiene usuario responsable existente',
       (SELECT COUNT(*) FROM Facturas WHERE UsuarioId IS NULL OR LTRIM(RTRIM(UsuarioId)) = '')
     + (SELECT COUNT(*) FROM Cobros   WHERE UsuarioId IS NULL OR LTRIM(RTRIM(UsuarioId)) = '')
     + (SELECT COUNT(*) FROM Pagos    WHERE UsuarioId IS NULL OR LTRIM(RTRIM(UsuarioId)) = '')
     + (SELECT COUNT(*) FROM Egresos  WHERE UsuarioId IS NULL OR LTRIM(RTRIM(UsuarioId)) = '')
     + (SELECT COUNT(*) FROM Cobros c WHERE c.UsuarioId IS NOT NULL
          AND NOT EXISTS (SELECT 1 FROM AspNetUsers u WHERE u.UserName = c.UsuarioId OR u.Email = c.UsuarioId))
     + (SELECT COUNT(*) FROM Pagos p WHERE p.UsuarioId IS NOT NULL
          AND NOT EXISTS (SELECT 1 FROM AspNetUsers u WHERE u.UserName = p.UsuarioId OR u.Email = p.UsuarioId)),
       'REVISAR DETALLE';

-- DETALLE 16 - usuarios registrados en documentos que NO existen en Identity
SELECT 'Cobro' AS Documento, c.Id, c.UsuarioId, c.Monto FROM Cobros c
WHERE c.UsuarioId IS NOT NULL
  AND NOT EXISTS (SELECT 1 FROM AspNetUsers u WHERE u.UserName = c.UsuarioId OR u.Email = c.UsuarioId)
UNION ALL
SELECT 'Pago', p.Id, p.UsuarioId, p.Monto FROM Pagos p
WHERE p.UsuarioId IS NOT NULL
  AND NOT EXISTS (SELECT 1 FROM AspNetUsers u WHERE u.UserName = p.UsuarioId OR u.Email = p.UsuarioId);


/* ----------------------------------------------------------------------------
   CUADRE 17 - Las alertas de reabastecimiento listan exactamente los productos
   activos, que manejan inventario, cuya existencia no supera su minimo.
   ---------------------------------------------------------------------------- */
INSERT INTO @Resultados
SELECT 17,
       N'Alertas de reabastecimiento: ni uno mas ni uno menos',
       (SELECT COUNT(*) FROM Productos
        WHERE Activo = 1 AND ManejaInventario = 1 AND Existencia <= ExistenciaMinima),
       'COMPARAR CON PANTALLA';

-- DETALLE 17 - la lista exacta que la pantalla debe mostrar
SELECT p.Codigo, p.Descripcion, p.Existencia, p.ExistenciaMinima,
       CASE WHEN p.Existencia <= 0 THEN 'Agotado' ELSE 'Bajo minimo' END AS Estado
FROM Productos p
WHERE p.Activo = 1 AND p.ManejaInventario = 1 AND p.Existencia <= p.ExistenciaMinima
ORDER BY p.Existencia ASC, p.Codigo;


/* ============================================================================
   RESUMEN
   ============================================================================ */
SELECT Cuadre, Titulo, Descuadres, Veredicto FROM @Resultados ORDER BY Cuadre;

SELECT COUNT(*) AS CuadresAutomaticos,
       SUM(CASE WHEN Veredicto = 'OK' THEN 1 ELSE 0 END) AS EnVerde,
       SUM(CASE WHEN Veredicto IN ('DESCUADRE','CRITICO') THEN 1 ELSE 0 END) AS Fallidos
FROM @Resultados;
