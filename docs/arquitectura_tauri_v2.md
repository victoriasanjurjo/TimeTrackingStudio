# Arquitectura Técnica - Time Tracking Studio (Rust & Tauri v2)

## 1. Visión General del Sistema

**Time Tracking Studio (Tauri v2)** es una aplicación de escritorio nativa para Windows 10 y Windows 11 construida con **Rust** en el backend y **Tauri v2** para la renderización de la interfaz a través de **Microsoft WebView2**.

La solución mantiene estrictamente la filosofía fundacional: **100% local, privada, sin telemetría ni suscripciones en la nube**, empleando almacenamiento relacional local en **SQLite**.

---

## 2. Diagrama de Arquitectura

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

---

## 3. Justificación de Decisiones Técnicas

| Decisión Técnica | Alternativa Evaluada | Justificación |
|---|---|---|
| **Rust & Tauri v2** | Electron / .NET WinUI 3 | Tauri v2 proporciona binarios compilados en código máquina (~11 MB frente a >150 MB en Electron), arranque instantáneo (<100 ms), uso de memoria inferior a 40 MB y la seguridad de tipos y memoria sin recolector de basura de Rust. |
| **rusqlite (bundled)** | ORMs pesados (Diesel / SeaORM) / SQLite dinámico | `bundled` compila el motor C de SQLite 3 directamente dentro del binario Rust de forma estática. No requiere DLLs de terceros instaladas en el sistema del cliente, garantizando portabilidad absoluta y consultas SQL optimizadas con índices dedicados. |
| **Interfaz Web Ligera (HTML/CSS/JS)** | React / Vue / Angular / Svelte con bundlers pesados | La interfaz utiliza estándares web modernos (ES Modules, Flexbox/Grid nativos, Variables CSS) cargados directamente por el runtime de Tauri. Cero tiempo de empaquetado de assets web, cero dependencias frágiles de node_modules en producción y rendimiento nativo fluido a 60 fps. |
| **Tauri Core IPC** | Servidor HTTP / WebSockets locales | El canal de comunicación nativo de Tauri utiliza buffers binarios internos directos entre el webview y el proceso Rust, eliminando el overhead de puertos de red locales y posibles advertencias de cortafuegos en Windows. |
| **Formato Ejecutable Standalone (.exe)** | Instaladores MSIX / InnoSetup complejos | Un binario ejecutable único `.exe` en `dist/` permite ejecución inmediata portable ("portable app") y distribución sin fricción de certificados auto-firmados en entornos corporativos o personales. |

---

## 4. Componentes Principales

1. **`src-tauri/src/models.rs`:**
   - Define las estructuras de datos fuertemente tipadas (`Client`, `Job`, `TimeLog`, `DashboardMetrics`, `DailyHourStat`).
   - Métodos utilitarios puros para cálculos de horas decimales (`calculate_hours`), montos facturables (`calculate_amount`), avance porcentual (`calculate_progress`) y formateo de duración (`00:00:00`).

2. **`src-tauri/src/db.rs`:**
   - Gestión del ciclo de vida de la base de datos relacional SQLite mediante `Mutex<Connection>`.
   - Inicialización idempotente del esquema DDL con `PRAGMA foreign_keys = ON` e índices dedicados (`idx_jobs_client`, `idx_timelogs_job`, `idx_timelogs_start`).
   - Semilla inicial demostrativa al arrancar por primera vez si no existen registros.
   - Soporte nativo para base de datos en memoria (`DbState::new_in_memory()`) para pruebas unitarias e integración aisladas.

3. **`src-tauri/src/export.rs`:**
   - Generación de informes exportables en `outputs/`.
   - Exportación a CSV con formato RFC 4180 y codificación UTF-8.
   - Generación de informe ejecutivo en HTML formal con tarjetas de métricas, estilos tipográficos limpios y tablas tabulares completas.

4. **`src-tauri/src/commands.rs`:**
   - 17 comandos IPC registrados en Tauri para atender cada una de las interacciones de la interfaz: CRUD de clientes, proyectos, hoja de tiempos, cálculo de KPIs y exportaciones.

5. **`src/` (Frontend):**
   - `index.html`: Estructura semántica con barra superior de cronómetro persistente, barra lateral de navegación, vistas modulares y formularios modales.
   - `styles.css`: Sistema de diseño moderno, limpio y neutral con estética Fluent y contraste óptimo.
   - `app.js`: Controlador reactivo que maneja el cronómetro en vivo de precisión de 1s, llamadas asíncronas a Rust e interacción de la interfaz.

---

## 5. Instrucciones de Compilación y Ejecución

### Prerrequisitos
- **Rust Toolchain:** Versión estable 1.90+ (`rustc` y `cargo`).
- **Microsoft WebView2 Runtime:** Preinstalado en Windows 10/11.

### 1. Ejecutar en Modo Desarrollo
```powershell
cargo run --manifest-path src-tauri/Cargo.toml
```

### 2. Ejecutar la Suite de Pruebas Automatizadas
```powershell
cargo test --manifest-path src-tauri/Cargo.toml
```

### 3. Compilar el Binario Ejecutable de Producción
```powershell
cargo build --release --manifest-path src-tauri/Cargo.toml
```
El archivo binario standalone resultante queda ubicado en:
```text
dist/TimeTrackingStudio.exe
```
