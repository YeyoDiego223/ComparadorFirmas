using System;
using System.Drawing;
using System.Windows.Forms;

namespace ComparadorFirmas
{
    public class FormPrincipal : Form
    {
        private PictureBox pic1, pic2, picMask1, picMask2;
        private Button btnCargar1, btnCargar2, btnComparar;
        private Label lblResultado, lblDetalle;
        private Bitmap firma1, firma2;

        public FormPrincipal()
        {
            Text = "Comparador de Firmas Manuscritas";
            Width = 720;
            Height = 560;
            StartPosition = FormStartPosition.CenterScreen;
            BackColor = Color.White;

            // --- Previsualizaciones de las firmas originales ---
            pic1 = NuevoPictureBox(20, 40);
            pic2 = NuevoPictureBox(370, 40);

            btnCargar1 = NuevoBoton("Cargar firma 1", 20, 220, (s, e) => CargarFirma(1));
            btnCargar2 = NuevoBoton("Cargar firma 2", 370, 220, (s, e) => CargarFirma(2));

            Controls.Add(NuevaEtiqueta("Firma 1", 20, 18));
            Controls.Add(NuevaEtiqueta("Firma 2", 370, 18));

            // --- Boton comparar ---
            btnComparar = NuevoBoton("Comparar firmas", 270, 260, (s, e) => Comparar());
            btnComparar.Width = 180;
            btnComparar.Height = 36;
            btnComparar.Enabled = false;

            // --- Resultado ---
            lblResultado = new Label
            {
                Left = 20, Top = 310, Width = 660, Height = 36,
                Font = new Font("Segoe UI", 14F, FontStyle.Bold),
                TextAlign = ContentAlignment.MiddleCenter,
                Text = "Carga dos firmas para comparar"
            };
            lblDetalle = new Label
            {
                Left = 20, Top = 348, Width = 660, Height = 24,
                TextAlign = ContentAlignment.MiddleCenter,
                ForeColor = Color.DimGray
            };
            Controls.Add(lblResultado);
            Controls.Add(lblDetalle);

            // --- Previsualizacion del preprocesamiento (mascaras binarias) ---
            Controls.Add(NuevaEtiqueta("Preprocesado 1", 20, 378));
            Controls.Add(NuevaEtiqueta("Preprocesado 2", 370, 378));
            picMask1 = NuevoPictureBox(20, 398);
            picMask1.Height = 90; picMask1.Width = 180;
            picMask2 = NuevoPictureBox(370, 398);
            picMask2.Height = 90; picMask2.Width = 180;
        }

        private PictureBox NuevoPictureBox(int x, int y)
        {
            var p = new PictureBox
            {
                Left = x, Top = y, Width = 330, Height = 170,
                BorderStyle = BorderStyle.FixedSingle,
                SizeMode = PictureBoxSizeMode.Zoom,
                BackColor = Color.WhiteSmoke
            };
            Controls.Add(p);
            return p;
        }

        private Button NuevoBoton(string texto, int x, int y, EventHandler onClick)
        {
            var b = new Button { Text = texto, Left = x, Top = y, Width = 150, Height = 30 };
            b.Click += onClick;
            Controls.Add(b);
            return b;
        }

        private Label NuevaEtiqueta(string texto, int x, int y)
        {
            return new Label
            {
                Text = texto, Left = x, Top = y, AutoSize = true,
                Font = new Font("Segoe UI", 9F, FontStyle.Bold)
            };
        }

        private void CargarFirma(int cual)
        {
            using (var ofd = new OpenFileDialog())
            {
                ofd.Filter = "Imagenes|*.png;*.jpg;*.jpeg;*.bmp;*.gif|Todos|*.*";
                if (ofd.ShowDialog() != DialogResult.OK) return;

                try
                {
                    // Copiamos a un Bitmap propio para no bloquear el archivo en disco.
                    Bitmap cargada;
                    using (var temp = new Bitmap(ofd.FileName))
                        cargada = new Bitmap(temp);

                    if (cual == 1)
                    {
                        firma1?.Dispose();
                        firma1 = cargada;
                        pic1.Image = firma1;
                    }
                    else
                    {
                        firma2?.Dispose();
                        firma2 = cargada;
                        pic2.Image = firma2;
                    }
                }
                catch (Exception ex)
                {
                    MessageBox.Show("No se pudo abrir la imagen:\n" + ex.Message,
                        "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    return;
                }
            }

            btnComparar.Enabled = firma1 != null && firma2 != null;
        }

        private void Comparar()
        {
            if (firma1 == null || firma2 == null) return;

            Cursor = Cursors.WaitCursor;
            try
            {
                ResultadoComparacion r = ComparadorImagenes.Comparar(firma1, firma2);

                picMask1.Image?.Dispose();
                picMask2.Image?.Dispose();
                picMask1.Image = r.Mascara1;
                picMask2.Image = r.Mascara2;

                lblResultado.Text = $"Similitud: {r.Similitud:0.0}%   ->   {Veredicto(r.Similitud)}";
                lblResultado.ForeColor = ColorVeredicto(r.Similitud);
                lblDetalle.Text = $"Solapamiento de tinta (IoU): {r.Iou * 100:0.0}%   |   " +
                                  $"Coincidencia pixel a pixel: {r.Coincidencia * 100:0.0}%";
            }
            finally
            {
                Cursor = Cursors.Default;
            }
        }

        private static string Veredicto(double similitud)
        {
            if (similitud >= 70) return "Firmas muy parecidas";
            if (similitud >= 45) return "Parecido moderado";
            return "Firmas diferentes";
        }

        private static Color ColorVeredicto(double similitud)
        {
            if (similitud >= 70) return Color.ForestGreen;
            if (similitud >= 45) return Color.DarkGoldenrod;
            return Color.Firebrick;
        }
    }
}
