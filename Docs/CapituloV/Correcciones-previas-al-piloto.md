# Correcciones previas al piloto — reparto por dueño de módulo

Hallazgos del piloto de QA del Capítulo V. Verificados en `origin/main` y en las ramas de los
tres desarrolladores.

**Última actualización: 30 de julio de 2026.** Este archivo sustituye por completo a la
versión anterior: los prompts de abajo son los **vigentes**, y los de la primera ronda quedaron
obsoletos porque Dionis ya corrigió lo suyo y porque apareció un defecto nuevo (D-03).

---

## Estado al 30 de julio

| # | Defecto | Archivo | Dueño | Severidad | ¿Corregido? | ¿En `main`? |
|---|---|---|---|---|:---:|:---:|
| D-01 | El usuario de cobros y pagos estaba escrito a mano | `RegistrarCobro.razor`, `RegistrarPago.razor` | Dionis | Alta | ✅ `3c240c0` | ⏳ PR #4 |
| R-02 | La fecha de cobros y pagos no se podía capturar | `Pago.cs`, `Cobro.cs` y sus DTO | Dionis | Baja | ✅ `3c240c0` | ⏳ PR #4 |
| H-06 | La devolución a proveedor no ajustaba la deuda | `DevolucionCompraService.cs` | Dionis | Media | ✅ `3c240c0` | ⏳ PR #4 |
| **D-03** | **Los movimientos de una venta no guardan el Id de su factura** | `Factura.cs:103` · `VentaService.cs:147` | **Jeison** | **Alta** | ❌ | ❌ |
| **D-02** | **Registro de usuarios público y sin rol** | `Account/Pages/Register.razor` | **Jeison** | **Alta** | ❌ | ❌ |
| **H-07** | **Editar un producto pisa el costo promedio** | `ProductoService.cs:123` | **Jeison** | Media | ❌ | ❌ |
| **H-08** | **`CLAUDE.md` niega el e-CF, y sí existe** | `CLAUDE.md` | **Jeison** | Baja | ❌ | ❌ |

**Dionis no tiene nada pendiente de programar.** Su PR es el **#4**, ya abierto y listo para
revisar: 182 pruebas en verde, sin conflictos con `main`.

**Jeison tiene los cuatro abiertos.** Su rama no cambia desde el 28 de julio.

---

## Prompt vigente para Jeison — los CUATRO

```
Eres el dueño de Inventario/ y Ventas/ del Mini ERP (TFG Grupo 6), y ademas de
la plataforma: Identity, el menu y el nucleo de seguridad. Trabajas en tu rama.

La integracion que hiciste el 28 quedo limpia y esta verificada: el andamio
provisional de permisos no se colo a main y las 29 pantallas quedaron gateadas.

Tienes CUATRO defectos abiertos en tu terreno. Se buscaron en las 15 ramas del
repositorio: siguen abiertos en todas, incluida main. Arreglalos en este orden.

====================================================================
DEFECTO 1 (severidad ALTA, bloquea el piloto de pruebas)
Los movimientos de inventario de una venta no guardan el Id de su
factura
====================================================================

Archivo: src/MiniERP.Domain/Ventas/Factura.cs, linea 103

  ReferenciaTipo = "FACTURA",
  ReferenciaId = Id,          <-- aqui Id todavia vale 0

EL PROBLEMA: Factura.Emitir se ejecuta ANTES de guardar la factura. En ese
momento la entidad es nueva y su Id es 0, asi que todos los movimientos de
salida quedan con ReferenciaId = 0.

Los datos lo confirman sin lugar a dudas:

  Origen del movimiento   ReferenciaId
  COMPRA                  1, 2          <- correcto
  ANULACION               4             <- correcto
  FACTURA                 0,0,0,0,0     <- DEFECTO

La asimetria es la prueba: el mismo patron `ReferenciaId = Id` funciona en
Compra.Recibir (linea 96) y en Factura.Anular (linea 176), porque ahi el
documento YA existe y tiene Id. Solo falla al emitir.

POR QUE IMPORTA: no se puede rastrear que factura provoco una salida de
mercancia, salvo leyendo el texto del campo Motivo. Si el dueño del colmado
pregunta "por que bajo el arroz el martes", no hay forma de responderle con una
consulta. Ademas hace fallar la verificacion 4 de las 17 del piloto, que
fallaria las 20 jornadas seguidas siempre por este mismo motivo.

DONDE ESTA EL ARREGLO:
src/MiniERP.Application/Ventas/Services/VentaService.cs, alrededor de la 147.
Hoy hace:

  ventas.Agregar(factura);
  ventas.AgregarMovimientos(movimientos);
  ...
  await ventas.GuardarAsync(ct);      <- aqui EF le asigna el Id a la factura

Despues de esa linea la factura YA tiene Id y los movimientos siguen rastreados
por EF. Todo corre dentro de EnTransaccionAsync, asi que un segundo guardado
sigue siendo atomico:

  await ventas.GuardarAsync(ct);

  // La factura ya tiene Id: ahora si se puede enlazar cada movimiento
  // con el documento que lo origino.
  foreach (var m in movimientos) m.ReferenciaId = factura.Id;
  await ventas.GuardarAsync(ct);

Decide tu si prefieres esa via u otra, pero cubre el caso.

COMO SE VERIFICA: emitir una factura y ejecutar

  SELECT m.Id, m.ReferenciaTipo, m.ReferenciaId, m.Motivo
  FROM MovimientosInventario m
  WHERE m.ReferenciaTipo = 'FACTURA' AND ISNULL(m.ReferenciaId,0) = 0;

Debe devolver cero filas. Hoy devuelve una por cada linea de cada venta.

====================================================================
DEFECTO 2 (severidad ALTA)
Cualquiera puede crearse una cuenta y entrar
====================================================================

Archivo: src/MiniERP.Web/Components/Account/Pages/Register.razor

Dos problemas juntos:
  1. Es @page "/Account/Register" SIN [Authorize], y esta enlazada en
     NavMenu.razor dentro del bloque <NotAuthorized>.
  2. La linea 84 llama a UserManager.CreateAsync y NUNCA a AddToRoleAsync:
     el usuario nuevo entra autenticado y sin rol.

POR QUE IMPORTA: contradice lo que tu mismo construiste. main ya tiene /usuarios
y /usuarios/{id}/permisos para que el dueño de alta a sus empleados con permisos
controlados, y al lado quedo una puerta por la que cualquiera se registra solo.

COMO SE ARREGLA (elige, pero cubre las dos partes):
  - Cerrar el autorregistro: [Authorize(Policy = Permisos.SistemaUsuarios)] en
    Register.razor y quitar el enlace del menu; o eliminar la pagina y dejar el
    alta solo en /usuarios/nuevo.
  - Si dejas alguna via de alta, que asigne rol, nunca ninguno.

COMO SE VERIFICA: ventana de incognito, ir a /Account/Register sin sesion y
comprobar que rebota. Luego crear un empleado desde /usuarios/nuevo y ver que
sale con rol:

  SELECT u.Email, r.Name FROM AspNetUsers u
  LEFT JOIN AspNetUserRoles ur ON ur.UserId = u.Id
  LEFT JOIN AspNetRoles r ON r.Id = ur.RoleId;

====================================================================
DEFECTO 3 (severidad MEDIA)
Editar un producto borra el costo promedio ponderado
====================================================================

Archivo: src/MiniERP.Application/Inventario/Services/ProductoService.cs
Metodo:  ActualizarAsync, linea 123

  producto.Costo = form.Costo;

El campo Costo es editable a mano en ProductoEditor.razor y al guardar se
sobrescribe sin mas.

POR QUE IMPORTA: Producto.Costo NO es un dato que el usuario deba teclear: es el
costo promedio ponderado que calcula Compra.CalcularCostoPromedio al recibir
mercancia. Es una de las decisiones de dominio que el proyecto defiende por
escrito. Si alguien edita un producto para corregirle una tilde a la
descripcion, de paso puede pisar el costo promedio, y con el la base del margen
de todas las ventas siguientes. Las ventas ya hechas no se mueven, porque el
costo esta congelado en LineaFactura; el daño es hacia adelante.

Fijate en que la linea 132 de ese mismo metodo dice, con toda razon:
  // La existencia no se toca aqui a proposito: solo cambia por movimiento.
El costo merece exactamente el mismo trato.

COMO SE ARREGLA (decide tu): lo mas coherente es no aceptar Costo en la edicion
(solo en el alta, como costo inicial) y mostrarlo de solo lectura indicando que
se actualiza al recibir compras. Si el grupo quiere permitir corregirlo, que sea
un movimiento explicito con motivo, como todo lo demas en este sistema.

====================================================================
DEFECTO 4 (severidad BAJA, pero delicado ante el jurado)
CLAUDE.md afirma que el e-CF no existe, y si existe
====================================================================

Archivo: CLAUDE.md, seccion "Huecos conocidos"

Dice: "No existe el e-CF. No hay XML de comprobante electronico ni QR."

Es FALSO en el codigo actual: EcfService genera el XML, hay e-NCF derivado,
codigo QR y la pantalla /ventas/facturas/{id}/ecf con su rotulo de documento
simulado. Lo que falta es la persistencia (no hay DbSet de
ComprobanteElectronico) y el estado Generado -> Enviado -> Aceptado.

POR QUE IMPORTA: es una afirmacion del propio equipo que contradice a su propio
codigo. Si el asesor la lee y luego abre la pantalla, la credibilidad del resto
del documento se resiente.

====================================================================
REGLAS DE TRABAJO
====================================================================
- Trabaja SOLO en Inventario/, Ventas/ y la plataforma. No toques Clientes/,
  Compras/ ni Finanzas/.
- No cambies logica de negocio para que una prueba pase: si algo no cuadra, dilo.
- Si tocas Factura o ProductoService, agrega la prueba que fije el
  comportamiento nuevo.

====================================================================
IMPORTANTE: ARREGLA LOS CUATRO, NO UNO NI DOS
====================================================================

Esta es la segunda vez que se envia esta lista. Los defectos 2, 3 y 4 ya se
habian reportado antes y siguen abiertos; el 1 es nuevo y salio al ejecutar las
verificaciones de integridad del piloto.

No hagas un push parcial. Si arreglas dos y dejas dos, hay que volver a
revisarte, volver a escribirte y volver a esperar, y el piloto de 20 dias del
Capitulo V sigue bloqueado mientras tanto.

Si alguno no lo puedes o no lo quieres arreglar, DILO con el motivo. Un "este
no lo hago porque X" es una respuesta valida y util. Lo que no sirve es que
quede sin tocar y sin explicacion.

====================================================================
CHECKLIST OBLIGATORIO ANTES DE HACER PUSH
====================================================================

No hagas push hasta que los seis den el resultado esperado. Ejecuta y compara.

1. COMPILA SIN ERRORES NI ADVERTENCIAS
     dotnet build MiniERP.slnx
   Esperado: "Compilacion correcta. 0 Advertencia(s). 0 Errores".

2. LAS PRUEBAS SIGUEN VERDES
     dotnet test
   Esperado: 180 de 180 (o mas, si agregaste pruebas). Si baja de 180,
   rompiste algo: no hagas push.

3. DEFECTO 1 RESUELTO - emite una factura desde el punto de venta y ejecuta:

     SELECT COUNT(*) AS Pendientes FROM MovimientosInventario
     WHERE ReferenciaTipo = 'FACTURA' AND ISNULL(ReferenciaId,0) = 0;

   Esperado: 0. Hoy da una fila por cada linea de cada venta.

4. DEFECTO 2 RESUELTO - abre una ventana de incognito y entra a
   /Account/Register SIN sesion.
   Esperado: rebota. No debe dejarte ver el formulario.

   Y crea un empleado desde /usuarios/nuevo, luego ejecuta:

     SELECT u.Email, r.Name FROM AspNetUsers u
     LEFT JOIN AspNetUserRoles ur ON ur.UserId = u.Id
     LEFT JOIN AspNetRoles r ON r.Id = ur.RoleId;

   Esperado: el empleado nuevo sale CON su rol, no en blanco.

5. DEFECTO 3 RESUELTO - recibe una compra de un producto a un costo distinto
   del que tenia, anota el costo promedio que quedo, y luego edita ese producto
   cambiandole solo la descripcion y guarda. Vuelve a mirar el costo.
   Esperado: el costo promedio NO cambio.

6. DEFECTO 4 RESUELTO - abre CLAUDE.md y busca "No existe el e-CF".
   Esperado: no aparece, o esta reescrito para reflejar lo que si existe.

Cuando los seis pasen, haz el push e integra a main. El piloto del Capitulo V
esta bloqueado esperando estos cuatro arreglos.
```

---

## Prompt vigente para Dionis — ya no tiene que programar

```
Tu commit 3c240c0 quedo verificado: los tres defectos corregidos, compila con
0 errores y las pruebas suben de 180 a 182 con las dos que agregaste. El merge
de main que hiciste el 30 (97a6767) tambien quedo limpio: ni tus correcciones
se perdieron ni el gateo de permisos de main se rompio.

YA TIENES EL PR ABIERTO: es el #4, lo abri yo para ponerlo en cola porque el
piloto de pruebas estaba bloqueado. El trabajo es tuyo y asi consta en la nota
de autoria del PR.

QUE FALTA: que alguien lo revise y lo fusione. Pidele a Jeison que lo integre,
o coordinen quien revisa.

POR QUE CORRE PRISA: el piloto de 20 dias registra 52 cobros y 26 pagos. Si
arranca sin tu correccion en main, las 78 operaciones quedan firmadas con un
nombre fijo y hay que repetir el piloto entero.
```

---

## Decisiones de grupo — no las toque nadie por su cuenta

Estas cuatro cambian el significado de un reporte o el modelo de datos, y afectan a más de un
módulo. **Requieren acuerdo, no un arreglo individual.**

**H-03 — El reloj es UTC y el colmado opera en UTC−4.** El panel calcula «hoy» con
`DateTime.UtcNow`, y a partir de las 8 de la noche las ventas cuentan en el día siguiente. El
arreglo del panel sería de Samuel, pero **la causa es sistémica**: todas las fechas se guardan
en UTC y todos los filtros por rango de los cinco módulos arrastran el mismo desfase. Hay que
decidir la política de zona horaria antes de tocar nada.

**H-04 — La merma no resta de la utilidad.** El estado de resultados es
`ingresos − costo de lo vendido − egresos`, la fórmula que fija `CLAUDE.md`. La mercancía
perdida baja del inventario pero no toca la utilidad. Cambiar la fórmula es desviarse de lo
documentado: hay que decidirlo y escribirlo.

**H-05 — La anulación reescribe el pasado.** Anular una factura de ayer la borra de los
reportes de ayer, sin registrar la salida del dinero ni cuándo ocurrió. Lo correcto
contablemente es una nota de crédito, que es funcionalidad nueva.

**R-03 y R-04 — Cobros y pagos no se imputan a un documento concreto.** No hay `FacturaId` en
`Cobro` ni `CompraId` en `Pago`: la imputación es FIFO calculada al vuelo, así que la
antigüedad de saldos es una estimación. Cambiarlo es modelo de datos y migración.
