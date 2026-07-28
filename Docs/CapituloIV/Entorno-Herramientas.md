# Entorno y herramientas

---

## 4.x.1 Plataforma y lenguaje

El sistema se desarrolló íntegramente en **C# sobre .NET 10**, la versión con soporte a largo
plazo vigente al momento de la construcción. La elección responde a un requisito explícito del
anteproyecto —que el sistema se construyera en **ASP.NET Core y C#**— y a una ventaja práctica
para un equipo pequeño: un solo lenguaje gobierna el dominio, la lógica, el acceso a datos y la
interfaz, lo que reduce el costo de que tres desarrolladores trabajen en módulos distintos.

## 4.x.2 Interfaz: Blazor Server

La capa de presentación se construyó con **Blazor Server**, el modelo de ASP.NET Core en el que
los componentes se ejecutan en el servidor y sincronizan la pantalla del navegador en tiempo
real mediante una conexión persistente.

Se eligió Blazor Server sobre las alternativas por tres razones. Primero, **evita escribir
JavaScript**: la interfaz se programa en C#, con el mismo modelo de objetos que el dominio, lo
que elimina la duplicación de validaciones entre cliente y servidor. Segundo, el equipo es
pequeño y no dispone de un especialista en interfaz. Tercero —y esto es lo que hace la decisión
defendible— la debilidad conocida de Blazor Server, que **exige conexión permanente con el
servidor**, no aplica a este caso: el sistema opera en la red local del comercio y el alcance
excluye por escrito el funcionamiento sin conexión y la aplicación móvil.

## 4.x.3 Persistencia: SQL Server y Entity Framework Core

La base de datos es **Microsoft SQL Server 2022**, con **Entity Framework Core** como mapeador
objeto-relacional bajo el enfoque **código primero** (*code-first*): el modelo se define en
clases de C# y las migraciones derivan de él el esquema físico.

Dos decisiones acompañan esta elección. La primera es usar **autenticación de Windows** para la
conexión, de modo que **no exista ninguna contraseña en el repositorio**; la cadena de conexión
apunta a `localhost` y cada desarrollador ajusta su instancia mediante los *user-secrets* de
.NET, que viven fuera del código compartido. La segunda es mantener **un único contexto de
datos** (`MiniErpDbContext`) que agrupa la identidad y los cinco módulos: eso da **una sola
línea de migraciones** para tres desarrolladores, que es más fácil de coordinar que varias
líneas paralelas, aunque obliga a crear las migraciones en serie.

## 4.x.4 Autenticación e identidad

La gestión de usuarios se apoya en **ASP.NET Core Identity**, que aporta el almacenamiento de
usuarios y roles, el cifrado de contraseñas y el manejo de sesiones. Sobre esa base se
construyó el control de acceso por permisos que documenta la sección 4.8. Usar el componente
estándar en lugar de un mecanismo propio evita reimplementar —y con ello arriesgar— la parte
más delicada de la seguridad: el almacenamiento de credenciales.

## 4.x.5 Herramientas de trabajo

| Herramienta | Uso | Nota |
|---|---|---|
| Visual Studio 2026 | Entorno de desarrollo | Exigido por el formato de solución `.slnx` y por .NET 10 |
| Git y GitHub | Control de versiones | Repositorio **privado**, con los cuatro integrantes |
| xUnit | Pruebas automatizadas | 180 pruebas de dominio al cierre de este capítulo |
| Entity Framework Core Tools | Migraciones | Se generan desde la capa de infraestructura |

El repositorio se organizó con **una rama por desarrollador**, nombrada con su autor
(`jeison/…`, `dionis/…`, `samuel/…`), y la integración a la rama principal se hizo mediante
solicitudes de incorporación (*pull requests*). Esta convención no es decorativa: hace que el
historial del proyecto **diga quién construyó qué** sin que nadie tenga que preguntarlo, que es
justamente lo que permite sustentar la autoría individual dentro de un trabajo grupal.

Las versiones de los paquetes se declaran **en un único archivo central**
(`Directory.Packages.props`) y nunca en los proyectos individuales, para que las cuatro capas
usen exactamente las mismas versiones y no aparezcan incompatibilidades silenciosas.

## 4.x.6 Instalación en un solo paso

El sistema se diseñó para que **arrancar la aplicación sea el único paso de instalación**. Al
iniciar, el programa crea la base de datos si no existe, aplica las migraciones pendientes y
siembra lo indispensable para operar: los roles, un usuario administrador, las unidades de
medida, las categorías iniciales y las secuencias de comprobantes. Todo el proceso es
**idempotente**: ejecutarlo mil veces produce el mismo resultado que ejecutarlo una vez.

La contraseña del administrador inicial **se genera al azar y se muestra una sola vez en la
consola**, dentro de un recuadro, en lugar de quedar escrita en el código o en la
documentación. Esta decisión responde al requerimiento no funcional de que no existan
credenciales en el repositorio, y a una realidad del piloto: el sistema se instala en un
comercio, no en un laboratorio.
