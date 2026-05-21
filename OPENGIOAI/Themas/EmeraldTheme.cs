using OPENGIOAI.Properties;
using System;
using System.Collections.Generic;
using System.Drawing.Drawing2D;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace OPENGIOAI.Themas
{
    public static class EmeraldTheme
    {
        // ═══════════════════════════════════════════════════════════════════
        //  MODO: Oscuro / Claro
        // ═══════════════════════════════════════════════════════════════════

        private static bool _isDark = true;

        /// <summary>true = modo oscuro (por defecto), false = modo claro.</summary>
        public static bool IsDark => _isDark;

        /// <summary>Se dispara en el hilo UI cada vez que cambia el tema.</summary>
        public static event Action? ThemeChanged;

        public static void SetTheme(bool dark)
        {
            if (_isDark == dark) return;
            _isDark = dark;
            ThemeChanged?.Invoke();
        }
        public static void ToggleTheme() => SetTheme(!_isDark);

        // ═══════════════════════════════════════════════════════════════════
        //  PALETA OSCURA
        // ═══════════════════════════════════════════════════════════════════
        private static readonly Color _dBgDeep    = ColorTranslator.FromHtml("#080808");
        private static readonly Color _dBgSurface = ColorTranslator.FromHtml("#1A1A1C");
        private static readonly Color _dBgCard    = ColorTranslator.FromHtml("#191919");
        private static readonly Color _dTextPri   = Color.White;
        private static readonly Color _dTextSec   = ColorTranslator.FromHtml("#D3D3ED");
        private static readonly Color _dTextMut   = ColorTranslator.FromHtml("#D3D3ED");

        // ═══════════════════════════════════════════════════════════════════
        //  PALETA CLARA
        // ═══════════════════════════════════════════════════════════════════
        private static readonly Color _lBgDeep    = Color.White;
        private static readonly Color _lBgSurface = ColorTranslator.FromHtml("#F0F6FF");
        private static readonly Color _lBgCard    = ColorTranslator.FromHtml("#E8F1FF");
        private static readonly Color _lTextPri   = ColorTranslator.FromHtml("#002647");
        private static readonly Color _lTextSec   = ColorTranslator.FromHtml("#1E4545");
        private static readonly Color _lTextMut   = ColorTranslator.FromHtml("#4158D0");

        // ═══════════════════════════════════════════════════════════════════
        //  ACENTOS (iguales en ambos modos)
        // ═══════════════════════════════════════════════════════════════════
        public static readonly Color Emerald500 = ColorTranslator.FromHtml("#080808");
        public static readonly Color Emerald400 = ColorTranslator.FromHtml("#94E6EC");
        public static readonly Color Emerald600 = ColorTranslator.FromHtml("#4158D0");
        public static readonly Color Emerald900 = ColorTranslator.FromHtml("#1E4545");
        public static readonly Color Error       = ColorTranslator.FromHtml("#f87171");

        // ═══════════════════════════════════════════════════════════════════
        //  PROPIEDADES DINÁMICAS (cambian con el modo)
        // ═══════════════════════════════════════════════════════════════════
        public static Color BgDeep      => _isDark ? _dBgDeep    : _lBgDeep;
        public static Color BgSurface   => _isDark ? _dBgSurface : _lBgSurface;
        public static Color BgCard      => _isDark ? _dBgCard    : _lBgCard;
        public static Color Glass       => _isDark ? Color.FromArgb(13, 54, 96, 201)
                                                   : Color.FromArgb(30, 54, 96, 201);
        public static Color GlassStrong => _isDark ? Color.FromArgb(25, 54, 96, 201)
                                                   : Color.FromArgb(60, 54, 96, 201);
        public static Color TextPrimary   => _isDark ? _dTextPri  : _lTextPri;
        public static Color TextSecondary => _isDark ? _dTextSec  : _lTextSec;
        public static Color TextMuted     => _isDark ? _dTextMut  : _lTextMut;
        public static Color Shadow        => _isDark ? Color.FromArgb(30, 0, 0, 0)
                                                     : Color.FromArgb(15, 0, 38, 71);

        // ===== APLICAR TEMA =====
        public static void ApplyTheme(Form form)
        {
            form.BackColor = BgDeep;
            form.ForeColor = TextPrimary;
            form.Font = new Font("Segoe UI", 9.5F, FontStyle.Regular);
            
            // Forzar DoubleBuffered en el formulario
            var prop = typeof(Control).GetProperty("DoubleBuffered", 
                System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic);
            prop?.SetValue(form, true, null);

            ApplyToControls(form.Controls);
        }

        private static void ApplyToControls(Control.ControlCollection controls)
        {
            foreach (Control control in controls)
            {
                if (control is Panel panel)
                {
                    StylePanel(panel);
                }
                else if (control is Button btn)
                {
                    StyleModernButton(btn);
                }
                else if (control is ComboBox cmb)
                {
                    StyleModernComboBox(cmb);
                }
                else if (control is Label lbl)
                {
                    lbl.ForeColor = (lbl.Tag?.ToString() == "Primary") ? TextPrimary : TextSecondary;
                    if (lbl.Font.Size < 10) lbl.Font = new Font("Segoe UI", 9F);
                }
                else if (control is TextBox txt)
                {
                    StyleModernTextBox(txt);
                }

                if (control.HasChildren)
                    ApplyToControls(control.Controls);
            }
        }

        // ===== PANEL MINIMALISTA =====
        private static void StylePanel(Panel panel)
        {
            panel.BackColor = BgCard;
            panel.ForeColor = TextPrimary;
            // Quitamos el evento Paint pesado que hacía sombras y glassmorphism
        }

        public static void StyleModernButton(Button btn)
        {
            btn.FlatStyle = FlatStyle.Flat;
            btn.FlatAppearance.BorderSize = 1;
            btn.FlatAppearance.BorderColor = Glass;
            btn.FlatAppearance.MouseOverBackColor = Glass;
            btn.FlatAppearance.MouseDownBackColor = GlassStrong;
            
            btn.ForeColor = TextPrimary;
            btn.Cursor = Cursors.Hand;
            btn.Font = new Font("Segoe UI Semibold", 9.5F);
            btn.Padding = new Padding(10, 5, 10, 5);
            btn.BackColor = BgSurface;

            // Eliminamos Paint manual y timers de animación para mejorar la fluidez
        }

        // ===== TEXTBOX MINIMALISTA =====
        private static void StyleModernTextBox(TextBox txt)
        {
            txt.BorderStyle = BorderStyle.FixedSingle;
            txt.BackColor = BgSurface;
            txt.ForeColor = TextPrimary;
            txt.Font = new Font("Segoe UI", 10F);
            
            // Quitamos la creación de contenedores dinámicos que sobrecargan el árbol de controles
        }

        // ===== HELPERS SIMPLIFICADOS =====
        private static GraphicsPath GetRoundedPath(Rectangle rect, int radius)
        {
            GraphicsPath path = new GraphicsPath();
            if (rect.Width <= 0 || rect.Height <= 0) return path;

            int diameter = radius * 2;
            path.AddArc(rect.X, rect.Y, diameter, diameter, 180, 90);
            path.AddArc(rect.Right - diameter, rect.Y, diameter, diameter, 270, 90);
            path.AddArc(rect.Right - diameter, rect.Bottom - diameter, diameter, diameter, 0, 90);
            path.AddArc(rect.X, rect.Bottom - diameter, diameter, diameter, 90, 90);
            path.CloseFigure();
            return path;
        }

        // ===== COMBOBOX MINIMALISTA =====
        public static void StyleModernComboBox(ComboBox cmb)
        {
            cmb.FlatStyle = FlatStyle.Flat;
            cmb.BackColor = BgSurface;
            cmb.ForeColor = TextPrimary;
            cmb.Font = new Font("Segoe UI", 10F);
            cmb.DropDownStyle = ComboBoxStyle.DropDownList;
            
            // Quitamos el OwnerDraw que consume recursos de dibujado manual
        }




     

        public static void StyleRoundedTextBox(TextBox txt, int radius = 10)
        {
            // En lugar de crear contenedores y Paint manual, usamos propiedades nativas
            // para máxima fluidez. El redondeo se aplica vía Region si es necesario,
            // pero priorizamos el rendimiento.
            txt.BorderStyle = BorderStyle.FixedSingle;
            txt.BackColor = BgSurface;
            txt.ForeColor = TextPrimary;
            txt.Font = new Font("Segoe UI", 10F);
            
            // Si realmente se requiere redondeo, se puede usar Region, pero para TextBoxes
            // nativos suele causar problemas de renderizado en los bordes.
            // Optamos por un diseño minimalista "Flat".
        }

        public static void StyleRoundedMultilineTextBox(TextBox txt, int radius = 10)
        {
            txt.Multiline = true;
            txt.BorderStyle = BorderStyle.FixedSingle;
            txt.BackColor = BgSurface;
            txt.ForeColor = TextPrimary;
            txt.Font = new Font("Segoe UI", 10F);
        }




        #region Forms
        // ===== ABRIR O REUTILIZAR FORM =====
        public static void OpenOrShowFormInPanel(Panel panel, Form form)
        {
            // Buscar si ya existe un form del mismo tipo
            Form existingForm = panel.Controls
                .OfType<Form>()
                .FirstOrDefault(f => f.GetType() == form.GetType());

            // Ocultar todos los forms actuales
            foreach (Form f in panel.Controls.OfType<Form>())
                f.Hide();

            // Si ya existe → solo mostrarlo
            if (existingForm != null)
            {
                existingForm.Show();
                existingForm.BringToFront();
                panel.Tag = existingForm;
                return;
            }

            // Configurar nuevo form
            form.TopLevel = false;
            form.FormBorderStyle = FormBorderStyle.None;
            form.Dock = DockStyle.Fill;

            panel.Controls.Add(form);
            panel.Tag = form;

            form.Show();
            form.BringToFront();
        }

        // ===== ABRIR CON FADE IN =====
        public static void OpenFormFade(Panel panel, Form form, int speed = 10)
        {
            Form existingForm = panel.Controls
                .OfType<Form>()
                .FirstOrDefault(f => f.GetType() == form.GetType());

            foreach (Form f in panel.Controls.OfType<Form>())
                f.Hide();

            if (existingForm != null)
            {
                FadeIn(existingForm, speed);
                panel.Tag = existingForm;
                return;
            }

            form.TopLevel = false;
            form.FormBorderStyle = FormBorderStyle.None;
            form.Dock = DockStyle.Fill;
            form.Opacity = 0;

            panel.Controls.Add(form);
            panel.Tag = form;

            form.Show();
            FadeIn(form, speed);
        }

        private static void FadeIn(Form form, int speed)
        {
            System.Windows.Forms.Timer timer = new System.Windows.Forms.Timer();
            timer.Interval = speed;
            timer.Tick += (s, e) =>
            {
                if (form.Opacity < 1)
                    form.Opacity += 0.05;
                else
                {
                    timer.Stop();
                    timer.Dispose();
                }
            };
            timer.Start();
        }

        // ===== SLIDE =====
        public static void OpenFormSlide(Panel panel, Form form, SlideDirection direction = SlideDirection.Left)
        {
            Form existingForm = panel.Controls
                .OfType<Form>()
                .FirstOrDefault(f => f.GetType() == form.GetType());

            foreach (Form f in panel.Controls.OfType<Form>())
                f.Hide();

            if (existingForm != null)
            {
                existingForm.Show();
                existingForm.BringToFront();
                panel.Tag = existingForm;
                return;
            }

            form.TopLevel = false;
            form.FormBorderStyle = FormBorderStyle.None;
            form.Size = panel.Size;

            Point startPos = Point.Empty;
            Point endPos = new Point(0, 0);

            switch (direction)
            {
                case SlideDirection.Left:
                    startPos = new Point(panel.Width, 0);
                    break;
                case SlideDirection.Right:
                    startPos = new Point(-panel.Width, 0);
                    break;
                case SlideDirection.Top:
                    startPos = new Point(0, panel.Height);
                    break;
                case SlideDirection.Bottom:
                    startPos = new Point(0, -panel.Height);
                    break;
            }

            form.Location = startPos;

            panel.Controls.Add(form);
            panel.Tag = form;

            form.Show();
            form.BringToFront();

            System.Windows.Forms.Timer slideTimer = new System.Windows.Forms.Timer();
            slideTimer.Interval = 10;
            int step = 0;
            int totalSteps = 20;

            slideTimer.Tick += (s, e) =>
            {
                step++;
                float progress = (float)step / totalSteps;
                float eased = EaseOutCubic(progress);

                int newX = (int)(startPos.X + (endPos.X - startPos.X) * eased);
                int newY = (int)(startPos.Y + (endPos.Y - startPos.Y) * eased);

                form.Location = new Point(newX, newY);

                if (step >= totalSteps)
                {
                    form.Location = endPos;
                    form.Dock = DockStyle.Fill;
                    slideTimer.Stop();
                    slideTimer.Dispose();
                }
            };

            slideTimer.Start();
        }

        // ===== CERRAR FORM ACTUAL =====
        public static void CloseCurrentForm(Panel panel)
        {
           
            if (panel.Tag is Form form)
            {
                panel.Controls.Remove(form);
                form.Close();
                panel.Tag = null;
            }

        }

        // ===== OCULTAR FORM ACTUAL =====
        public static void HideCurrentForm(Panel panel)
        {
            if (panel.Tag is Form form)
                form.Hide();
        }

        // ===== VERIFICAR SI HAY FORM =====
        public static bool HasFormOpen(Panel panel)
        {
            return panel.Tag is Form;
        }

        // ===== OBTENER FORM ACTUAL =====
        public static Form GetCurrentForm(Panel panel)
        {
            return panel.Tag as Form;
        }

        // ===== ENUM DIRECCIÓN =====
        public enum SlideDirection
        {
            Left,
            Right,
            Top,
            Bottom
        }

        // ===== EASING =====
        private static float EaseOutCubic(float t)
        {
            return 1 - (float)Math.Pow(1 - t, 3);
        }
        #endregion


    }
}
