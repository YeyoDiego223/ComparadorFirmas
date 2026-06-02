using System;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Drawing.Imaging;

namespace ComparadorFirmas
{
    /// <summary>
    /// Resultado de comparar dos firmas.
    /// </summary>
    public class ResultadoComparacion
    {
        public double Iou;            // Solapamiento de tinta: interseccion / union (0..1)
        public double Coincidencia;   // Coincidencia pixel a pixel sobre el total (0..1)
        public double Similitud;      // Puntaje final combinado (0..100)
        public Bitmap Mascara1;       // Firma 1 normalizada y binarizada (para previsualizar)
        public Bitmap Mascara2;       // Firma 2 normalizada y binarizada (para previsualizar)
    }

    /// <summary>
    /// Comparador de firmas manuscritas usando solo .NET (System.Drawing).
    /// Pasos: escala de grises -> umbral automatico (Otsu) -> recorte al area
    /// de la firma -> reescalado a tamano comun -> metricas de similitud.
    /// </summary>
    public static class ComparadorImagenes
    {
        // Tamano comun al que se normalizan ambas firmas antes de comparar.
        private const int NORM_W = 256;
        private const int NORM_H = 128;

        public static ResultadoComparacion Comparar(Bitmap a, Bitmap b)
        {
            bool[,] m1 = NormalizarABinario(a);
            bool[,] m2 = NormalizarABinario(b);

            long interseccion = 0, union = 0, iguales = 0;
            long total = (long)NORM_W * NORM_H;

            for (int y = 0; y < NORM_H; y++)
            {
                for (int x = 0; x < NORM_W; x++)
                {
                    bool t1 = m1[x, y];   // hay tinta en firma 1
                    bool t2 = m2[x, y];   // hay tinta en firma 2

                    if (t1 && t2) interseccion++;
                    if (t1 || t2) union++;
                    if (t1 == t2) iguales++;
                }
            }

            double iou = union == 0 ? 0 : (double)interseccion / union;
            double coincidencia = (double)iguales / total;

            // Puntaje final: ponderamos mas el IoU (solapamiento real de la tinta),
            // porque la coincidencia pixel a pixel se infla con el fondo blanco comun.
            double similitud = (0.7 * iou + 0.3 * coincidencia) * 100.0;

            return new ResultadoComparacion
            {
                Iou = iou,
                Coincidencia = coincidencia,
                Similitud = similitud,
                Mascara1 = MascaraABitmap(m1),
                Mascara2 = MascaraABitmap(m2)
            };
        }

        /// <summary>
        /// Convierte una imagen a una mascara binaria normalizada (true = tinta).
        /// </summary>
        private static bool[,] NormalizarABinario(Bitmap origen)
        {
            // 1) Escala de grises a resolucion original.
            byte[,] gris = AEscalaDeGrises(origen, out int w, out int h);

            // 2) Umbral automatico (Otsu) y deteccion de tinta (pixeles oscuros).
            byte umbral = Otsu(gris, w, h);
            bool[,] tinta = new bool[w, h];
            int minX = w, minY = h, maxX = -1, maxY = -1;
            for (int y = 0; y < h; y++)
            {
                for (int x = 0; x < w; x++)
                {
                    bool esTinta = gris[x, y] < umbral;
                    tinta[x, y] = esTinta;
                    if (esTinta)
                    {
                        if (x < minX) minX = x;
                        if (y < minY) minY = y;
                        if (x > maxX) maxX = x;
                        if (y > maxY) maxY = y;
                    }
                }
            }

            // 3) Recorte al area de la firma (bounding box). Si no hay tinta,
            // usamos toda la imagen.
            if (maxX < 0)
            {
                minX = 0; minY = 0; maxX = w - 1; maxY = h - 1;
            }
            int recW = maxX - minX + 1;
            int recH = maxY - minY + 1;

            // 4) Reescalado del recorte a NORM_W x NORM_H y nuevo umbral.
            using (Bitmap recorte = new Bitmap(recW, recH, PixelFormat.Format24bppRgb))
            {
                using (Graphics g = Graphics.FromImage(recorte))
                {
                    g.Clear(Color.White);
                    g.DrawImage(origen,
                        new Rectangle(0, 0, recW, recH),
                        new Rectangle(minX, minY, recW, recH),
                        GraphicsUnit.Pixel);
                }

                using (Bitmap norm = new Bitmap(NORM_W, NORM_H, PixelFormat.Format24bppRgb))
                {
                    using (Graphics g = Graphics.FromImage(norm))
                    {
                        g.InterpolationMode = InterpolationMode.HighQualityBicubic;
                        g.Clear(Color.White);
                        g.DrawImage(recorte, 0, 0, NORM_W, NORM_H);
                    }

                    byte[,] grisN = AEscalaDeGrises(norm, out int nw, out int nh);
                    byte umbralN = Otsu(grisN, nw, nh);
                    bool[,] mascara = new bool[nw, nh];
                    for (int y = 0; y < nh; y++)
                        for (int x = 0; x < nw; x++)
                            mascara[x, y] = grisN[x, y] < umbralN;
                    return mascara;
                }
            }
        }

        /// <summary>
        /// Lectura rapida de pixeles con LockBits y conversion a gris (luminancia).
        /// </summary>
        private static byte[,] AEscalaDeGrises(Bitmap bmp, out int w, out int h)
        {
            w = bmp.Width;
            h = bmp.Height;
            byte[,] gris = new byte[w, h];

            Rectangle rect = new Rectangle(0, 0, w, h);
            BitmapData data = bmp.LockBits(rect, ImageLockMode.ReadOnly, PixelFormat.Format32bppArgb);
            try
            {
                int stride = data.Stride;
                IntPtr scan0 = data.Scan0;
                byte[] buffer = new byte[Math.Abs(stride) * h];
                System.Runtime.InteropServices.Marshal.Copy(scan0, buffer, 0, buffer.Length);

                for (int y = 0; y < h; y++)
                {
                    int fila = y * stride;
                    for (int x = 0; x < w; x++)
                    {
                        int i = fila + x * 4;     // BGRA
                        byte bb = buffer[i];
                        byte gg = buffer[i + 1];
                        byte rr = buffer[i + 2];
                        gris[x, y] = (byte)(0.299 * rr + 0.587 * gg + 0.114 * bb);
                    }
                }
            }
            finally
            {
                bmp.UnlockBits(data);
            }
            return gris;
        }

        /// <summary>
        /// Umbral automatico por el metodo de Otsu (maximiza la varianza entre clases).
        /// </summary>
        private static byte Otsu(byte[,] gris, int w, int h)
        {
            int[] hist = new int[256];
            for (int y = 0; y < h; y++)
                for (int x = 0; x < w; x++)
                    hist[gris[x, y]]++;

            long total = (long)w * h;
            double sumaTotal = 0;
            for (int i = 0; i < 256; i++) sumaTotal += (double)i * hist[i];

            double sumaFondo = 0;
            long pesoFondo = 0;
            double maxVar = -1;
            int umbral = 127;

            for (int i = 0; i < 256; i++)
            {
                pesoFondo += hist[i];
                if (pesoFondo == 0) continue;
                long pesoFrente = total - pesoFondo;
                if (pesoFrente == 0) break;

                sumaFondo += (double)i * hist[i];
                double mediaFondo = sumaFondo / pesoFondo;
                double mediaFrente = (sumaTotal - sumaFondo) / pesoFrente;

                double varEntre = (double)pesoFondo * pesoFrente
                                  * (mediaFondo - mediaFrente) * (mediaFondo - mediaFrente);
                if (varEntre > maxVar)
                {
                    maxVar = varEntre;
                    umbral = i;
                }
            }
            return (byte)umbral;
        }

        /// <summary>
        /// Convierte una mascara binaria en un Bitmap (tinta=negro, fondo=blanco)
        /// para previsualizar el preprocesamiento.
        /// </summary>
        private static Bitmap MascaraABitmap(bool[,] mascara)
        {
            Bitmap bmp = new Bitmap(NORM_W, NORM_H, PixelFormat.Format24bppRgb);
            for (int y = 0; y < NORM_H; y++)
                for (int x = 0; x < NORM_W; x++)
                    bmp.SetPixel(x, y, mascara[x, y] ? Color.Black : Color.White);
            return bmp;
        }
    }
}
