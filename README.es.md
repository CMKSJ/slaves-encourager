# Ánimo Laboral / Slaves Encourager

Edita los textos fijos con doble clic, o con clic derecho en un botón (`Ctrl+E`). Guarda, cancela o restaura un texto o el idioma actual. Los cambios se guardan por idioma. [Guía](docs/HOME_TEXT.md).

Convierte tu tiempo en pequeñas recompensas: reloj de ingresos, cuenta atrás hasta la salida, calendario laboral y metas para Windows.

[简体中文](README.md) · [繁體中文](README.zh-Hant.md) · [English](README.en.md) · [日本語](README.ja.md) · **Español** · [한국어](README.ko.md)

<img src="docs/screenshots/es-main.png" width="380" alt="Interfaz en español">

## Uso

Requiere Windows 10/11 y .NET Framework 4.8. Extrae el ZIP y abre `SlavesEncourager.exe`. Funciona sin cuenta ni conexión y no envía telemetría.

El primer inicio muestra datos de ejemplo. Abre «Configuración inicial», introduce tu sueldo y horario, marca descansos o turnos extra en el calendario y guarda. Puedes añadir hasta 20 metas y cambiar su orden. El selector de idioma está arriba a la derecha; la selección se guarda y cancelar restaura el idioma anterior.

Elige animación de solapas, deslizamiento o cifras estáticas, decimales, separadores y plantillas. Arrastra cualquier zona de la ventana principal que no sea un botón. `Ctrl+M` activa el modo mini y `Ctrl+Shift+P` oculta los importes mientras la ventana está activa. «—» oculta en la bandeja y «×» cierra.

## Cálculos y datos

El sueldo se reparte entre los días programados del mes o sobre una base fija de 21,75 días. Los descansos se excluyen; los turnos pasados se consideran completos. No calcula impuestos, primas, horas extra ni asistencia real. Marca los festivos según tu horario. Los turnos nocturnos pertenecen a la fecha de inicio. La moneda solo cambia el símbolo.

El ingreso mensual estimado se reparte entre las metas por orden, sin duplicarlo, y el progreso se reinicia cada mes. No representa ahorros reales.

Los ajustes se guardan en `%LOCALAPPDATA%\NiumaFlip\settings.json`, una ruta interna conservada por compatibilidad. No están cifrados; cada guardado conserva una copia `.bak`.

## Desarrollo y licencia

Se prefiere el contenedor Windows: `./scripts/build.ps1`. Como alternativa con el compilador ya instalado: `./scripts/build.ps1 -Native -VisualCheck`, seguido de `python scripts/validate.py` y `python scripts/package.py`.

[Licencia MIT](LICENSE) · [Desarrollo](docs/DEVELOPMENT.md) · [Contribuir](CONTRIBUTING.md) · [Cambios](CHANGELOG.md)
