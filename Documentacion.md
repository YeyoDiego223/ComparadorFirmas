# Comparador de Firmas Manuscritas
### Documentación técnica del sistema

**Asignatura:** Procesamiento de Voz e Imagen
**Tecnología:** C# / Windows Forms (.NET 9)
**Tipo de sistema:** Aplicación de escritorio para comparar imágenes de firmas

---

## 1. Introducción

El sistema es una aplicación de escritorio en **Windows Forms (C#)** que permite
comparar dos **firmas manuscritas** a partir de imágenes cargadas desde archivo y
estimar qué tan parecidas son entre sí.

A diferencia de una *firma digital* (que es un mecanismo criptográfico basado en
hashes y claves), aquí se trabaja con **firmas biométricas en formato de imagen**:
el programa procesa los píxeles de cada firma y calcula un porcentaje de similitud
mediante técnicas básicas de procesamiento de imágenes, **sin usar librerías
externas** (solo el espacio de nombres `System.Drawing` de .NET).

### 1.1 Objetivo

Dadas dos imágenes de firma, determinar un **porcentaje de similitud** y un
**veredicto** (parecidas, parecido moderado, diferentes), mostrando además el
resultado del preprocesamiento para hacer transparente cómo se llega al resultado.

---

## 2. Requisitos

| Requisito | Detalle |
|-----------|---------|
| Sistema operativo | Windows |
| Entorno | Visual Studio 2022 (o superior) |
| Plataforma | .NET 9 (SDK instalado) |
| Lenguaje | C# |
| Interfaz | Windows Forms |
| Dependencias externas | Ninguna (solo `System.Drawing`) |

---

## 3. Estructura del proyecto

```
ComparadorFirmas/
├── ComparadorFirmas.csproj   → Definición del proyecto WinForms (.NET 9)
├── Program.cs                → Punto de entrada de la aplicación
├── FormPrincipal.cs          → Interfaz gráfica y manejo de eventos
└── ComparadorImagenes.cs     → Motor de procesamiento y comparación
```

### 3.1 Responsabilidad de cada archivo

- **Program.cs**: arranca la aplicación y lanza el formulario principal.
- **FormPrincipal.cs**: construye la interfaz (botones, previsualizaciones,
  etiquetas de resultado), gestiona la carga de imágenes y dispara la comparación.
- **ComparadorImagenes.cs**: contiene toda la lógica de procesamiento de imagen
  y el cálculo de las métricas de similitud.

---

## 4. Cómo funciona (flujo del algoritmo)

El proceso de comparación se realiza en cinco etapas. Ambas firmas pasan por el
mismo preprocesamiento antes de compararse.

### Etapa 1 — Conversión a escala de grises

Cada píxel en color (RGB) se convierte a un único valor de gris usando la fórmula
de **luminancia**, que pondera los canales según la sensibilidad del ojo humano:

```
gris = 0.299·R + 0.587·G + 0.114·B
```

La lectura de píxeles se hace con **LockBits** (acceso directo a memoria), que es
mucho más rápido que `GetPixel` píxel por píxel.

### Etapa 2 — Binarización automática (método de Otsu)

Para separar la **tinta** (la firma) del **fondo** (el papel), se aplica un umbral.
En lugar de fijar un valor manual, se usa el **método de Otsu**, que recorre el
histograma de la imagen y elige automáticamente el umbral que **maximiza la
varianza entre las dos clases** (tinta y fondo). Todo píxel más oscuro que el
umbral se considera tinta.

### Etapa 3 — Recorte al área de la firma (bounding box)

Se localiza el rectángulo mínimo que contiene toda la tinta y se recorta la imagen
a esa región. Con esto se **eliminan los márgenes** y se normaliza la **posición**
de la firma dentro de la imagen, de modo que dos firmas centradas de forma distinta
puedan compararse de forma justa.

### Etapa 4 — Reescalado a tamaño común

El recorte se redimensiona a un tamaño fijo de **256 × 128 píxeles** usando
interpolación bicúbica de alta calidad. Esto normaliza la **escala**: firmas
grandes y pequeñas quedan en el mismo lienzo antes de compararse. Tras el
reescalado se vuelve a binarizar con Otsu para obtener la **máscara final**
(matriz de booleanos: `true` = tinta).

### Etapa 5 — Cálculo de métricas de similitud

Sobre las dos máscaras normalizadas se calculan:

- **IoU (Intersection over Union) — solapamiento de tinta:**

  ```
  IoU = (píxeles con tinta en AMBAS) / (píxeles con tinta en AL MENOS UNA)
  ```

  Es la métrica más representativa porque mide cuánto se superpone la tinta real
  de ambas firmas.

- **Coincidencia píxel a píxel:**

  ```
  Coincidencia = (píxeles iguales, tinta o fondo) / (total de píxeles)
  ```

  Incluye también el fondo coincidente.

- **Similitud final (puntaje combinado, 0–100):**

  ```
  Similitud = (0.7 · IoU + 0.3 · Coincidencia) · 100
  ```

  Se pondera más el IoU porque la coincidencia simple se "infla" con el fondo
  blanco que ambas imágenes comparten.

### 4.1 Interpretación del resultado

| Similitud | Veredicto |
|-----------|-----------|
| ≥ 70 % | Firmas muy parecidas |
| 45 % – 69 % | Parecido moderado |
| < 45 % | Firmas diferentes |

---

## 5. Uso de la aplicación

1. Ejecutar la aplicación (desde Visual Studio con **F5**, o `dotnet run`).
2. Pulsar **"Cargar firma 1"** y seleccionar una imagen (PNG, JPG, BMP, GIF).
3. Pulsar **"Cargar firma 2"** y seleccionar la segunda imagen.
4. Pulsar **"Comparar firmas"**.
5. Leer el resultado:
   - Porcentaje de **similitud** y veredicto (con color: verde / ámbar / rojo).
   - Detalle de **IoU** y **coincidencia píxel a píxel**.
   - Vista del **preprocesado** (las dos firmas binarizadas) en la parte inferior.

---

## 6. Componentes técnicos clave

### 6.1 Clase `ResultadoComparacion`
Estructura de datos que devuelve el comparador. Contiene el IoU, la coincidencia,
la similitud final y las dos máscaras binarias para previsualización.

### 6.2 Clase estática `ComparadorImagenes`
Núcleo del sistema. Métodos principales:

| Método | Función |
|--------|---------|
| `Comparar(Bitmap, Bitmap)` | Orquesta todo y calcula las métricas |
| `NormalizarABinario(Bitmap)` | Aplica etapas 1–4 y devuelve la máscara |
| `AEscalaDeGrises(...)` | Lectura rápida con LockBits + luminancia |
| `Otsu(...)` | Umbral automático |
| `MascaraABitmap(...)` | Convierte la máscara a imagen para previsualizar |

### 6.3 Clase `FormPrincipal`
Construye la interfaz por código (sin diseñador), maneja la carga de archivos
(copiando el Bitmap para no bloquear el archivo en disco) y muestra resultados.

---

## 7. Limitaciones y posibles mejoras

**Limitaciones del enfoque básico:**

- La comparación se basa en **superposición de píxeles**, por lo que es sensible
  a la **rotación** y a diferencias de trazo dentro del recuadro normalizado.
- No es una verificación biométrica robusta de identidad; sirve para estimar
  parecido entre dos imágenes de firma capturadas de forma similar.

**Mejoras futuras posibles:**

- Usar **OpenCvSharp** (OpenCV para .NET) para invariancia a rotación.
- Comparar por **descriptores de forma** (contornos, momentos de Hu) o **SSIM**
  (índice de similitud estructural).
- Permitir **dibujar la firma con el mouse** además de cargarla desde archivo.
- Añadir un umbral de decisión ajustable por el usuario.

---

## 8. Conclusión

El sistema demuestra la aplicación de técnicas fundamentales de **procesamiento de
imágenes** (escala de grises, binarización por Otsu, segmentación por bounding box,
normalización y métricas de similitud) integradas en una aplicación de escritorio
funcional en C#/Windows Forms, sin depender de librerías externas. Esto lo hace
adecuado como práctica didáctica y como base extensible hacia métodos de
comparación más avanzados.
