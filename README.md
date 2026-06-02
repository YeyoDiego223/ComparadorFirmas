# Comparador de Firmas Manuscritas

Aplicación de escritorio en **C# / Windows Forms (.NET 9)** que compara dos
**firmas manuscritas** a partir de imágenes y estima qué tan parecidas son,
usando únicamente técnicas básicas de procesamiento de imágenes (**sin librerías
externas**, solo `System.Drawing`).

> Proyecto de la asignatura *Procesamiento de Voz e Imagen*.

---

## Captura de la aplicación

![Ventana principal del comparador](capturas/app.png)

---

## Características

- Carga de dos firmas desde archivo (PNG, JPG, BMP, GIF).
- Preprocesamiento automático: escala de grises, binarización por **Otsu**,
  recorte al área de la firma y reescalado a tamaño común.
- Cálculo de un **porcentaje de similitud** con veredicto por colores.
- Vista del **preprocesado** (firmas binarizadas) para hacer transparente el cálculo.
- 100 % .NET, sin dependencias externas.

---

## Cómo funciona

La comparación se realiza en cinco etapas; ambas firmas pasan por el mismo
preprocesamiento antes de compararse.

| # | Etapa | Descripción |
|---|-------|-------------|
| 1 | **Escala de grises** | Conversión RGB → gris por luminancia: `0.299·R + 0.587·G + 0.114·B` |
| 2 | **Binarización (Otsu)** | Umbral automático que separa tinta del fondo maximizando la varianza entre clases |
| 3 | **Recorte (bounding box)** | Aísla el área de la firma → normaliza la posición |
| 4 | **Reescalado** | Lleva ambas firmas a 256 × 128 px (interpolación bicúbica) → normaliza la escala |
| 5 | **Métricas** | Calcula IoU y coincidencia píxel a píxel |

### Métricas de similitud

- **IoU (solapamiento de tinta):** `tinta en ambas / tinta en al menos una`
- **Coincidencia píxel a píxel:** `píxeles iguales / total`
- **Similitud final:** `(0.7 · IoU + 0.3 · Coincidencia) · 100`

  Se pondera más el IoU porque la coincidencia simple se infla con el fondo blanco común.

### Interpretación

| Similitud | Veredicto |
|-----------|-----------|
| ≥ 70 % | Firmas muy parecidas |
| 45 % – 69 % | Parecido moderado |
| < 45 % | Firmas diferentes |

---

## Requisitos

- Windows
- [.NET 9 SDK](https://dotnet.microsoft.com/download)
- Visual Studio 2022 (opcional, recomendado)

## Ejecución

Desde Visual Studio: abre `ComparadorFirmas.csproj` y pulsa **F5**.

Desde terminal:

```bash
dotnet run
```

## Uso

1. Pulsa **Cargar firma 1** y elige una imagen.
2. Pulsa **Cargar firma 2** y elige la segunda imagen.
3. Pulsa **Comparar firmas**.
4. Lee el porcentaje de similitud, el veredicto y el detalle de las métricas.

---

## Estructura del proyecto

```
ComparadorFirmas/
├── ComparadorFirmas.csproj   → Proyecto WinForms (.NET 9)
├── Program.cs                → Punto de entrada
├── FormPrincipal.cs          → Interfaz gráfica y eventos
├── ComparadorImagenes.cs     → Motor de procesamiento y comparación
└── Documentacion.md          → Documentación técnica detallada
```

---

## Limitaciones

La comparación se basa en superposición de píxeles, por lo que es sensible a la
rotación y a diferencias de trazo. No es una verificación biométrica robusta;
estima parecido entre dos imágenes capturadas de forma similar. Una mejora futura
sería usar **OpenCvSharp** (contornos, descriptores, SSIM) para mayor precisión.

---

## Documentación

Documentación técnica completa en [`Documentacion.md`](Documentacion.md).
