# Time Tracking Studio ⏱️ (Rust & Tauri v2)

**Time Tracking Studio** es una aplicación de escritorio nativa, moderna, ultraligera y de alto rendimiento para **Windows 10 y Windows 11**, diseñada para profesionales independientes, desarrolladores, agencias y estudios que desean gestionar sus clientes, proyectos y seguimiento de tiempos trabajados **con privacidad absoluta (100% local)**, sin telemetría, sin suscripciones en la nube y con arranque instantáneo.

La aplicación está construida íntegramente en **Rust** utilizando el framework de nueva generación **Tauri v2**, el motor de renderizado acelerado por hardware **Microsoft WebView2**, persistencia relacional con **SQLite** embebido de forma estática (`rusqlite bundled`) y generación directa de binarios standalone `.exe`.

---

## 🌟 Características Principales

- **🔒 100% Local y Privada:** Sin llamadas de red, sin publicidad, sin rastreadores ni telemetría. La base de datos SQLite reside íntegramente en tu propio equipo (`%LocalAppData%\TimeTrackingStudio\timetracker.db`).
- **⏱ Cronómetro Global en Vivo:** Barra superior persistente con selector dinámico de clientes y tareas activas, display digital monospace de alta precisión (`00:00:00`), controles de iniciar, pausar, reanudar, descartar y guardar sesión en la base de datos con un clic.
- **👥 Directorio de Clientes:** Fichas de clientes con datos de contacto (empresa, email, teléfono), tarifas por hora configurables, divisa personalizada (`USD`, `EUR`, `ARS`, `MXN`, `GBP`), estado activo/inactivo y cálculo automático de facturación acumulada.
- **📁 Control Total de Trabajos y Tareas:** Registro de tareas y proyectos con estados oficiales:
  - 🟡 `Pendiente`
  - 🟠 `En progreso`
  - 🔵 `En revisión`
  - 🟢 `Entregado`
  - 🟣 `Facturado`
  - Prioridades (`Baja`, `Media`, `Alta`, `Urgente`), cálculo automático de horas estimadas vs registradas, barras de progreso dinámicas y selector de estado rápido en tabla.
- **📊 Panel de Control (Dashboard):** Tarjetas de KPIs en tiempo real (Horas Hoy, Total Facturable Hoy, Horas Mes, Trabajos Activos, Clientes Totales) y visualizador gráfico dinámico del rendimiento en los últimos 7 días.
- **⏱ Hoja de Tiempos (Timesheet):** Registro cronológico detallado con filtros por cliente, trabajo y rango de fechas, adición manual de horas trabajadas y eliminación de registros.
- **📄 Reportes Ejecutivos y Exportación (`outputs/`):** Generador de informes maquetados en formato **HTML** formal y exportación completa a tablas de datos **CSV** con botón directo para abrir la carpeta contenedora.
- **⚡ Binario Standalone Ultra Ligero (`dist/TimeTrackingStudio.exe`):** Ejecutable único de aproximadamente ~11 MB con motor SQLite compilado estáticamente, sin necesidad de instalar runtimes externos como .NET o Python.

---

## 🏛 Arquitectura y Componentes del Sistema

El sistema implementa una arquitectura por capas desacoplada basada en comandos asíncronos nativos IPC:

```text
┌────────────────────────────────────────────────────────┐
│                   CAPA DE INTERFAZ WEB                 │
│         (HTML5, CSS Moderno Fluent, Vanilla JS)        │
│          src/index.html, src/styles.css, src/app.js    │
└───────────────────────────▲────────────────────────────┘
                            │ Tauri IPC (invoke commands)
┌───────────────────────────▼────────────────────────────┐
│                    CAPA TAURI COMMANDS                 │
│         src-tauri/src/commands.rs (Handlers)           │
│   (get_clients, add_job, add_time_log, export_csv...)  │
└───────────────────────────▲────────────────────────────┘
                            │ Tipado estricto & serialización
┌───────────────────────────▼────────────────────────────┐
│                 CAPA DE NEGOCIO Y MODELOS              │
│       src-tauri/src/models.rs & export.rs              │
│      (Client, Job, TimeLog, DashboardMetrics)          │
└───────────────────────────▲────────────────────────────┘
                            │ Mutex<Connection> + rusqlite
┌───────────────────────────▼────────────────────────────┐
│                  CAPA DE PERSISTENCIA                  │
│                src-tauri/src/db.rs                     │
│         SQLite Local (%LocalAppData%/TimeTrackingStudio)│
└────────────────────────────────────────────────────────┘
```

### Componentes:

1. **Capa Frontend (`src/`):**
   - [`index.html`](file:///d:/Proyectos/Repos/TimeTrackingStudio/src/index.html): Estructura semántica, barra superior de cronómetro, sidebar de navegación, modales y tablas de datos.
   - [`styles.css`](file:///d:/Proyectos/Repos/TimeTrackingStudio/src/styles.css): Diseño moderno, claro y funcional con paleta neutral de alto contraste y controles inspirados en Fluent Design.
   - [`app.js`](file:///d:/Proyectos/Repos/TimeTrackingStudio/src/app.js): Controlador reactivo del cronómetro, llamadas asíncronas IPC vía Tauri y actualización dinámica del DOM.

2. **Capa Backend en Rust (`src-tauri/src/`):**
   - [`models.rs`](file:///d:/Proyectos/Repos/TimeTrackingStudio/src-tauri/src/models.rs): Estructuras de dominio fuertemente tipadas y serializables (`Client`, `Job`, `TimeLog`, `DashboardMetrics`) y lógica de cálculo numérico puro.
   - [`db.rs`](file:///d:/Proyectos/Repos/TimeTrackingStudio/src-tauri/src/db.rs): Gestor SQLite (`rusqlite`) con claves foráneas activas, transacciones seguras, semilla de datos inicial y soporte para bases de datos en memoria para testing.
   - [`export.rs`](file:///d:/Proyectos/Repos/TimeTrackingStudio/src-tauri/src/export.rs): Exportación estructurada a formato CSV y generación de informes ejecutivos en HTML dentro de la carpeta `outputs/`.
   - [`commands.rs`](file:///d:/Proyectos/Repos/TimeTrackingStudio/src-tauri/src/commands.rs): Handlers de comandos expuestos al frontend para operaciones CRUD y utilidades de sistema.
   - [`lib.rs`](file:///d:/Proyectos/Repos/TimeTrackingStudio/src-tauri/src/lib.rs) & [`main.rs`](file:///d:/Proyectos/Repos/TimeTrackingStudio/src-tauri/src/main.rs): Inicialización del runtime de Tauri v2 y registro del estado global y comandos.

---

## 📁 Estructura del Proyecto

```text
TimeTrackingStudio/
├── assets/                          # Iconos oficiales (.ico, .png)
├── config/                          # Configuración local de base de datos
├── dist/                            # Ejecutable final compilado (.exe)
│   └── TimeTrackingStudio.exe
├── docs/                            # Documentación técnica y arquitectura
│   ├── arquitectura_tauri_v2.md
│   └── changelog.md
├── outputs/                         # Salida de reportes CSV e informes HTML
├── src/                             # Código fuente de la interfaz web
│   ├── app.js
│   ├── index.html
│   └── styles.css
├── src-tauri/                       # Backend nativo en Rust con Tauri v2
│   ├── capabilities/                # Permisos de seguridad desktop
│   ├── icons/                       # Iconos empaquetados para la app
│   ├── src/                         # Módulos Rust (commands, db, export, models, lib, main)
│   ├── tests/                       # Pruebas de integración automatizadas
│   │   └── calculations_tests.rs
│   ├── build.rs                     # Script de construcción de Tauri
│   ├── Cargo.toml                   # Dependencias Rust de la app Tauri
│   └── tauri.conf.json              # Configuración de ventana y bundler Tauri v2
├── tests/                           # Suite de pruebas de referencia
│   └── calculations_tests.rs
├── .gitignore                       # Exclusiones git (target, temporales)
├── Cargo.toml                       # Espacio de trabajo (workspace) raíz Cargo
├── LICENSE.txt                      # Licencia MIT completa
└── README.md                        # Documentación general y guía de ejecución
```

---

## 🚀 Guía de Instalación y Ejecución

### Prerrequisitos
- **Windows 10 versión 1809 o superior / Windows 11**.
- **Rust Toolchain** instalado (Rust 1.90+): `rustc` y `cargo`.
- **Microsoft WebView2 Runtime** (incluido por defecto en Windows 10/11).

### 1. Ejecutar en Modo Desarrollo
Para compilar y lanzar la aplicación en modo desarrollo:
```powershell
cargo run --manifest-path src-tauri/Cargo.toml
```

### 2. Ejecutar la Suite de Pruebas Automatizadas
Para ejecutar las pruebas unitarias y de integración de modelos y base de datos:
```powershell
cargo test --manifest-path src-tauri/Cargo.toml
```

### 3. Compilar el Binario Ejecutable Standalone (.exe)
Para generar el archivo ejecutable optimizado de producción:
```powershell
cargo build --release --manifest-path src-tauri/Cargo.toml
```

El binario ejecutable standalone queda listo en:
```text
dist/TimeTrackingStudio.exe
```

---

## 📄 Licencia

Este proyecto está distribuido bajo los términos de la Licencia **MIT**. Consulta el archivo [`LICENSE.txt`](file:///d:/Proyectos/Repos/TimeTrackingStudio/LICENSE.txt) para más información.
