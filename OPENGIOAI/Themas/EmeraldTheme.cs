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
        // ===== COLORES =====
        public static Color BgDeep = ColorTranslator.FromHtml("#050505");
        public static Color BgSurface = ColorTranslator.FromHtml("#0a0a0a");
        public static Color BgCard = ColorTranslator.FromHtml("#0f0f0f");
        public static Color Glass = Color.FromArgb(13, 16, 185, 129);
        public static Color GlassStrong = Color.FromArgb(25, 16, 185, 129);
        public static Color Emerald500 = ColorTranslator.FromHtml("#10b981");
        public static Color Emerald400 = ColorTranslator.FromHtml("#34d399");
        public static Color Emerald600 = ColorTranslator.FromHtml("#059669");
        public static Color Emerald900 = ColorTranslator.FromHtml("#064e3b");
        public static Color TextPrimary = ColorTranslator.FromHtml("#f0fdf4");
        public static Color TextSecondary = ColorTranslator.FromHtml("#a7f3d0");
        public static Color TextMuted = ColorTranslator.FromHtml("#6ee7b7");
        public static Color Error = ColorTranslator.FromHtml("#f87171");
        public static Color Shadow = Color.FromArgb(30, 0, 0, 0);

        // ===== APLICAR TEMA =====
        public static void ApplyTheme(Form form)
        {
            form.BackColor = BgDeep;
            form.ForeColor = TextPrimary;
            form.Font = new Font("Segoe UI", 9.5F, FontStyle.Regular);
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
                    StyleModernButton(btn, 16);
                }
                else if (control is ComboBox cmb)
                {
                    StyleModernComboBox(cmb);
                }
                else if (control is Label lbl)
                {
                    lbl.ForeColor = TextSecondary;
                    lbl.Font = new Font("Segoe UI", 9F);
                }
                else if (control is TextBox txt)
                {
                    if (txt.Multiline)
                    {
                        StyleRoundedMultilineTextBox(txt, 10);
                    }
                    else
                    {
                        StyleRoundedTextBox(txt, 10);
                    }
                }

                if (control.HasChildren)
                    ApplyToControls(control.Controls);
            }
        }

        // ===== PANEL GLASSMORPHISM =====
        private static void StylePanel(Panel panel)
        {
            panel.BackColor = BgCard;
            panel.ForeColor = TextPrimary;
            panel.Paint += (s, e) =>
            {
                e.Graphics.SmoothingMode = SmoothingMode.AntiAlias;

                // Sombra suave
                using (GraphicsPath shadowPath = GetRoundedPath(
                    new Rectangle(2, 2, panel.Width - 4, panel.Height - 4), 12))
                using (PathGradientBrush shadowBrush = new PathGradientBrush(shadowPath))
                {
                    shadowBrush.CenterColor = Color.FromArgb(20, 0, 0, 0);
                    shadowBrush.SurroundColors = new[] { Color.Transparent };
                    e.Graphics.FillPath(shadowBrush, shadowPath);
                }

                // Fondo con borde glass
                using (GraphicsPath path = GetRoundedPath(
                    new Rectangle(0, 0, panel.Width - 1, panel.Height - 1), 12))
                using (Pen borderPen = new Pen(Glass, 1.5f))
                {
                    e.Graphics.FillPath(new SolidBrush(BgCard), path);
                    e.Graphics.DrawPath(borderPen, path);
                }
            };
        }
        public static void StyleModernButton(Button btn, int radius)
        {
            btn.FlatStyle = FlatStyle.Flat;
            btn.FlatAppearance.BorderSize = 0;
            btn.ForeColor = TextPrimary;
            btn.Cursor = Cursors.Hand;
            btn.Font = new Font("Segoe UI Semibold", 9.5F);
            btn.Padding = new Padding(20, 8, 20, 8);
            btn.BackColor = BgCard; // Fondo negro

            System.Windows.Forms.Timer hoverTimer = new System.Windows.Forms.Timer { Interval = 10 };
            float hoverProgress = 0f;
            bool isHovering = false;

            btn.Resize += (s, e) =>
            {
                btn.Region = new Region(GetRoundedPath(btn.ClientRectangle, radius));
            };

            btn.Paint += (s, e) =>
            {
                e.Graphics.SmoothingMode = SmoothingMode.AntiAlias;
                e.Graphics.TextRenderingHint = System.Drawing.Text.TextRenderingHint.ClearTypeGridFit;
                Rectangle rect = btn.ClientRectangle;

                using (GraphicsPath path = GetRoundedPath(rect, radius))
                {
                    // Fondo negro
                    using (SolidBrush bgBrush = new SolidBrush(BgSurface))
                    {
                        e.Graphics.FillPath(bgBrush, path);
                    }


                    if (btn.Tag == null)
                        btn.Tag = "";
                    // Borde verde con efecto hover
                    Color baseBorder = (btn.Tag!= "" && btn.Tag != "NoTheme"  ? ColorTranslator.FromHtml(btn.Tag.ToString()) :Emerald500);


                    Color hoverBorder = Emerald400;
                    Color currentBorder = InterpolateColor(baseBorder, hoverBorder, hoverProgress);

                    float borderWidth = 2.5f + (hoverProgress * 0.5f); // Borde más grueso en hover

                    using (Pen borderPen = new Pen(currentBorder, borderWidth))
                    {
                        e.Graphics.DrawPath(borderPen, path);
                    }
                }

                // DIBUJAR EL TEXTO
                TextFormatFlags flags = TextFormatFlags.HorizontalCenter |
                                        TextFormatFlags.VerticalCenter |
                                        TextFormatFlags.WordBreak;
                TextRenderer.DrawText(e.Graphics, btn.Text, btn.Font, rect, btn.ForeColor, flags);
            };

            // Animación hover
            hoverTimer.Tick += (s, e) =>
            {
                if (isHovering && hoverProgress < 1f)
                {
                    hoverProgress = Math.Min(1f, hoverProgress + 0.1f);
                    btn.Invalidate();
                }
                else if (!isHovering && hoverProgress > 0f)
                {
                    hoverProgress = Math.Max(0f, hoverProgress - 0.1f);
                    btn.Invalidate();
                }
                else
                {
                    hoverTimer.Stop();
                }
            };

            btn.MouseEnter += (s, e) =>
            {
                isHovering = true;
                hoverTimer.Start();
            };

            btn.MouseLeave += (s, e) =>
            {
                isHovering = false;
                hoverTimer.Start();
            };
        }

        // Método helper para aclarar colores
        private static Color LightenColor(Color color, float amount)
        {
            int r = Math.Min(255, (int)(color.R + (255 - color.R) * amount));
            int g = Math.Min(255, (int)(color.G + (255 - color.G) * amount));
            int b = Math.Min(255, (int)(color.B + (255 - color.B) * amount));
            return Color.FromArgb(color.A, r, g, b);
        }

        // ===== TEXTBOX MODERNO =====
        private static void StyleModernTextBox(TextBox txt)
        {
            txt.BorderStyle = BorderStyle.None;
            txt.BackColor = BgSurface;
            txt.ForeColor = TextPrimary;
            txt.Font = new Font("Segoe UI", 10F);

            Panel container = new Panel
            {
                Size = new Size(txt.Width, txt.Height + 8),
                Location = txt.Location,
                BackColor = Color.Transparent
            };

            txt.Parent.Controls.Add(container);
            txt.Parent.Controls.Remove(txt);
            container.Controls.Add(txt);
            txt.Location = new Point(8, 4);
            txt.Width = container.Width - 16;

            container.Paint += (s, e) =>
            {
                e.Graphics.SmoothingMode = SmoothingMode.AntiAlias;
                using (GraphicsPath path = GetRoundedPath(
                    new Rectangle(0, 0, container.Width - 1, container.Height - 1), 10))
                {
                    e.Graphics.FillPath(new SolidBrush(BgSurface), path);

                    Color borderColor = txt.Focused ? Emerald500 : Glass;
                    using (Pen pen = new Pen(borderColor, txt.Focused ? 2f : 1f))
                    {
                        e.Graphics.DrawPath(pen, path);
                    }
                }
            };

            txt.Enter += (s, e) => container.Invalidate();
            txt.Leave += (s, e) => container.Invalidate();
        }

        // ===== HELPERS =====
        private static GraphicsPath GetRoundedPath(Rectangle rect, int radius)
        {
            GraphicsPath path = new GraphicsPath();

            if (rect.Width <= 0 || rect.Height <= 0)
                return path;

            // 🔒 Limitar el radio para botones pequeños
            int maxRadius = Math.Min(rect.Width, rect.Height) / 2;
            int safeRadius = Math.Min(radius, maxRadius);
            int diameter = safeRadius * 2;

            if (safeRadius <= 0)
            {
                path.AddRectangle(rect);
                return path;
            }

            path.AddArc(rect.X, rect.Y, diameter, diameter, 180, 90);
            path.AddArc(rect.Right - diameter, rect.Y, diameter, diameter, 270, 90);
            path.AddArc(rect.Right - diameter, rect.Bottom - diameter, diameter, diameter, 0, 90);
            path.AddArc(rect.X, rect.Bottom - diameter, diameter, diameter, 90, 90);
            path.CloseFigure();

            return path;
        }

        private static Color InterpolateColor(Color c1, Color c2, float ratio)
        {
            ratio = Math.Max(0, Math.Min(1, ratio));
            int r = (int)(c1.R + (c2.R - c1.R) * ratio);
            int g = (int)(c1.G + (c2.G - c1.G) * ratio);
            int b = (int)(c1.B + (c2.B - c1.B) * ratio);
            return Color.FromArgb(r, g, b);
        }

        // ===== COMBOBOX MODERNO =====
        // ===== COMBOBOX MODERNO =====
        public static void StyleModernComboBox(ComboBox cmb)
        {
            cmb.FlatStyle = FlatStyle.Flat;
            cmb.BackColor = BgSurface;
            cmb.ForeColor = TextPrimary;
            cmb.Font = new Font("Segoe UI", 10F);
            cmb.DropDownStyle = ComboBoxStyle.DropDownList;
            cmb.Cursor = Cursors.Hand;
            cmb.DrawMode = DrawMode.OwnerDrawFixed;
            cmb.ItemHeight = 35;

            bool isFocused = false;
            bool isHovered = false;

            // Crear un UserControl personalizado para el ComboBox
            Panel container = new Panel
            {
                Size = new Size(cmb.Width, cmb.Height),
                Location = cmb.Location,
                BackColor = Color.Transparent
            };

            // Mover el ComboBox dentro del contenedor
            var parent = cmb.Parent;
            parent.Controls.Add(container);
            parent.Controls.Remove(cmb);
            container.Controls.Add(cmb);

            cmb.Location = new Point(1, 1);
            cmb.Width = container.Width - 2;
            cmb.Height = container.Height - 2;

            // Pintar el contenedor (borde redondeado)
            container.Paint += (s, e) =>
            {
                e.Graphics.SmoothingMode = SmoothingMode.AntiAlias;

                using (GraphicsPath path = GetRoundedPath(
                    new Rectangle(0, 0, container.Width - 1, container.Height - 1), 10))
                {
                    // Fondo
                    using (SolidBrush bgBrush = new SolidBrush(BgSurface))
                    {
                        e.Graphics.FillPath(bgBrush, path);
                    }

                    // Borde
                    Color borderColor = isFocused ? Emerald400 : (isHovered ? Emerald500 : Emerald900);
                    float borderWidth = isFocused ? 2f : 1.5f;

                    using (Pen borderPen = new Pen(borderColor, borderWidth))
                    {
                        e.Graphics.DrawPath(borderPen, path);
                    }

                    // Dibujar el texto seleccionado manualmente
                    if (cmb.SelectedIndex >= 0)
                    {
                        string selectedText = cmb.GetItemText(cmb.SelectedItem);
                        TextRenderer.DrawText(
                            e.Graphics,
                            selectedText,
                            cmb.Font,
                            new Rectangle(12, 0, container.Width - 40, container.Height),
                            TextPrimary,
                            TextFormatFlags.Left | TextFormatFlags.VerticalCenter
                        );
                    }

                    // Flecha personalizada
                    int arrowSize = 6;
                    int arrowX = container.Width - 15;
                    int arrowY = container.Height / 2;

                    Point[] arrowPoints = new Point[]
                    {
                new Point(arrowX - arrowSize / 2, arrowY - 2),
                new Point(arrowX + arrowSize / 2, arrowY - 2),
                new Point(arrowX, arrowY + arrowSize / 2 - 2)
                    };

                    using (SolidBrush arrowBrush = new SolidBrush(isFocused ? Emerald400 : Emerald500))
                    {
                        e.Graphics.FillPolygon(arrowBrush, arrowPoints);
                    }
                }
            };

            // Ocultar el ComboBox visual pero mantener funcionalidad
           // cmb.FlatAppearance.BorderSize = 0;
            cmb.BackColor = BgSurface;
            cmb.ForeColor = BgSurface; // Ocultar texto nativo

            // Eventos de hover
            container.MouseEnter += (s, e) =>
            {
                isHovered = true;
                container.Invalidate();
            };

            container.MouseLeave += (s, e) =>
            {
                isHovered = false;
                container.Invalidate();
            };

            container.MouseClick += (s, e) =>
            {
                cmb.DroppedDown = !cmb.DroppedDown;
            };

            // Eventos de foco
            cmb.Enter += (s, e) =>
            {
                isFocused = true;
                container.Invalidate();
            };

            cmb.Leave += (s, e) =>
            {
                isFocused = false;
                container.Invalidate();
            };

            cmb.DropDown += (s, e) =>
            {
                isFocused = true;
                container.Invalidate();
            };

            cmb.DropDownClosed += (s, e) =>
            {
                isFocused = false;
                container.Invalidate();
            };

            cmb.SelectedIndexChanged += (s, e) =>
            {
                container.Invalidate();
            };

            // Dibujar items del dropdown
            cmb.DrawItem += (s, e) =>
            {
                if (e.Index < 0) return;

                e.Graphics.SmoothingMode = SmoothingMode.AntiAlias;
                e.Graphics.TextRenderingHint = System.Drawing.Text.TextRenderingHint.ClearTypeGridFit;

                bool isSelected = (e.State & DrawItemState.Selected) == DrawItemState.Selected;
                Color itemBgColor = isSelected ? Color.FromArgb(25, 16, 185, 129) : BgSurface;

                using (SolidBrush bgBrush = new SolidBrush(itemBgColor))
                {
                    e.Graphics.FillRectangle(bgBrush, e.Bounds);
                }

                // Borde izquierdo para item seleccionado
                if (isSelected)
                {
                    using (SolidBrush accentBrush = new SolidBrush(Emerald500))
                    {
                        e.Graphics.FillRectangle(accentBrush,
                            new Rectangle(e.Bounds.X, e.Bounds.Y, 3, e.Bounds.Height));
                    }
                }

                string itemText = cmb.GetItemText(cmb.Items[e.Index]);
                Color textColor = isSelected ? Emerald400 : TextPrimary;

                TextRenderer.DrawText(
                    e.Graphics,
                    itemText,
                    cmb.Font,
                    new Rectangle(e.Bounds.X + 12, e.Bounds.Y, e.Bounds.Width - 12, e.Bounds.Height),
                    textColor,
                    TextFormatFlags.Left | TextFormatFlags.VerticalCenter
                );
            };

            // Sincronizar tamaños
            cmb.SizeChanged += (s, e) =>
            {
                container.Size = new Size(cmb.Width + 2, cmb.Height + 2);
            };
        }




     

        // ===== TEXTBOX MULTILÍNEA REDONDEADO =====
        // ===== TEXTBOX MULTILÍNEA REDONDEADO =====
        public static void StyleRoundedMultilineTextBox(TextBox txt, int radius = 10)
        {
            // Evitar aplicar el estilo múltiples veces
            if (txt.Tag != null && txt.Tag.ToString() == "styled") return;
            txt.Tag = "styled";

            txt.BorderStyle = BorderStyle.None;
            txt.BackColor = BgSurface;
            txt.ForeColor = TextPrimary;
            txt.Font = new Font("Segoe UI", 10F);
            txt.Multiline = true;

            bool isFocused = false;
            bool isHovered = false;

            // Crear contenedor
            Panel container = new Panel
            {
                Size = new Size(txt.Width + 16, txt.Height + 16),
                Location = txt.Location,
                BackColor = Color.Transparent,
                Name = "container_" + txt.Name
            };

            // Mover el TextBox dentro del contenedor
            var parent = txt.Parent;
            parent.Controls.Add(container);
            parent.Controls.Remove(txt);
            container.Controls.Add(txt);

            txt.Location = new Point(8, 8);
            txt.Width = container.Width - 16;
            txt.Height = container.Height - 16;
            //txt.Anchor = AnchorStyles.Left | AnchorStyles.Right | AnchorStyles.Top | AnchorStyles.Bottom;

            // Handler para Paint - usar variable local
            PaintEventHandler paintHandler = (s, e) =>
            {
                e.Graphics.SmoothingMode = SmoothingMode.AntiAlias;

                using (GraphicsPath path = GetRoundedPath(
                    new Rectangle(0, 0, container.Width - 1, container.Height - 1), radius))
                {
                    // Fondo
                    using (SolidBrush bgBrush = new SolidBrush(BgSurface))
                    {
                        e.Graphics.FillPath(bgBrush, path);
                    }

                    // Borde
                    Color borderColor = isFocused ? Emerald400 : (isHovered ? Emerald500 : Emerald900);
                    float borderWidth = isFocused ? 2f : 1.5f;

                    using (Pen borderPen = new Pen(borderColor, borderWidth))
                    {
                        e.Graphics.DrawPath(borderPen, path);
                    }

                    // Brillo cuando está enfocado
                    if (isFocused)
                    {
                        using (Pen glowPen = new Pen(Color.FromArgb(30, 16, 185, 129), 3f))
                        {
                            e.Graphics.DrawPath(glowPen, path);
                        }
                    }
                }
            };

            container.Paint += paintHandler;

            // Eventos de hover del TextBox
            EventHandler txtMouseEnter = (s, e) =>
            {
                isHovered = true;
                container.Invalidate();
            };

            EventHandler txtMouseLeave = (s, e) =>
            {
                isHovered = false;
                container.Invalidate();
            };

            txt.MouseEnter += txtMouseEnter;
            txt.MouseLeave += txtMouseLeave;

            // Eventos de hover del Container
            EventHandler containerMouseEnter = (s, e) =>
            {
                isHovered = true;
                container.Invalidate();
            };

            EventHandler containerMouseLeave = (s, e) =>
            {
                isHovered = false;
                container.Invalidate();
            };

            container.MouseEnter += containerMouseEnter;
            container.MouseLeave += containerMouseLeave;

            // Eventos de foco
            EventHandler txtEnter = (s, e) =>
            {
                isFocused = true;
                container.Invalidate();
            };

            EventHandler txtLeave = (s, e) =>
            {
                isFocused = false;
                container.Invalidate();
            };

            txt.Enter += txtEnter;
            txt.Leave += txtLeave;

            // Click en el contenedor enfoca el TextBox
            EventHandler containerClick = (s, e) => txt.Focus();
            container.Click += containerClick;

            // Limpiar eventos cuando se dispose
            txt.Disposed += (s, e) =>
            {
                container.Paint -= paintHandler;
                txt.MouseEnter -= txtMouseEnter;
                txt.MouseLeave -= txtMouseLeave;
                container.MouseEnter -= containerMouseEnter;
                container.MouseLeave -= containerMouseLeave;
                txt.Enter -= txtEnter;
                txt.Leave -= txtLeave;
                container.Click -= containerClick;
            };
        }

        // ===== TEXTBOX REDONDEADO SIMPLE =====
        public static void StyleRoundedTextBox(TextBox txt, int radius = 10)
        {
            // Evitar aplicar el estilo múltiples veces
            if (txt.Tag != null && txt.Tag.ToString() == "styled") return;
            txt.Tag = "styled";

            txt.BorderStyle = BorderStyle.None;
            txt.BackColor = BgSurface;
            txt.ForeColor = TextPrimary;
            txt.Font = new Font("Segoe UI", 10F);

            bool isFocused = false;
            bool isHovered = false;

            // Crear contenedor para el borde redondeado
            Panel container = new Panel
            {
                Size = new Size(txt.Width, txt.Height + 8),
                Location = txt.Location,
                BackColor = Color.Transparent,
                Name = "container_" + txt.Name
            };

            // Mover el TextBox dentro del contenedor
            var parent = txt.Parent;
            parent.Controls.Add(container);
            parent.Controls.Remove(txt);
            container.Controls.Add(txt);

            txt.Location = new Point(12, 4);
            txt.Width = container.Width - 24;
            txt.Anchor = AnchorStyles.Left | AnchorStyles.Right | AnchorStyles.Top;

            // Handler para Paint
            PaintEventHandler paintHandler = (s, e) =>
            {
                e.Graphics.SmoothingMode = SmoothingMode.AntiAlias;

                using (GraphicsPath path = GetRoundedPath(
                    new Rectangle(0, 0, container.Width - 1, container.Height - 1), radius))
                {
                    // Fondo
                    using (SolidBrush bgBrush = new SolidBrush(BgSurface))
                    {
                        e.Graphics.FillPath(bgBrush, path);
                    }

                    // Borde con estados
                    Color borderColor = isFocused ? Emerald400 : (isHovered ? Emerald500 : Emerald900);
                    float borderWidth = isFocused ? 2f : 1.5f;

                    using (Pen borderPen = new Pen(borderColor, borderWidth))
                    {
                        e.Graphics.DrawPath(borderPen, path);
                    }

                    // Brillo sutil cuando está enfocado
                    if (isFocused)
                    {
                        using (Pen glowPen = new Pen(Color.FromArgb(30, 16, 185, 129), 3f))
                        {
                            e.Graphics.DrawPath(glowPen, path);
                        }
                    }
                }
            };

            container.Paint += paintHandler;

            // Eventos de hover
            EventHandler txtMouseEnter = (s, e) =>
            {
                isHovered = true;
                container.Invalidate();
            };

            EventHandler txtMouseLeave = (s, e) =>
            {
                isHovered = false;
                container.Invalidate();
            };

            txt.MouseEnter += txtMouseEnter;
            txt.MouseLeave += txtMouseLeave;

            EventHandler containerMouseEnter = (s, e) =>
            {
                isHovered = true;
                container.Invalidate();
            };

            EventHandler containerMouseLeave = (s, e) =>
            {
                isHovered = false;
                container.Invalidate();
            };

            container.MouseEnter += containerMouseEnter;
            container.MouseLeave += containerMouseLeave;

            // Eventos de foco
            EventHandler txtEnter = (s, e) =>
            {
                isFocused = true;
                container.Invalidate();
            };

            EventHandler txtLeave = (s, e) =>
            {
                isFocused = false;
                container.Invalidate();
            };

            txt.Enter += txtEnter;
            txt.Leave += txtLeave;

            // Sincronizar tamaños cuando el TextBox cambie
            EventHandler sizeChanged = (s, e) =>
            {
                container.Size = new Size(txt.Width + 24, txt.Height + 8);
            };
            txt.SizeChanged += sizeChanged;

            // Hacer que el click en el contenedor enfoque el TextBox
            EventHandler containerClick = (s, e) => txt.Focus();
            container.Click += containerClick;

            // Limpiar eventos cuando se dispose
            txt.Disposed += (s, e) =>
            {
                container.Paint -= paintHandler;
                txt.MouseEnter -= txtMouseEnter;
                txt.MouseLeave -= txtMouseLeave;
                container.MouseEnter -= containerMouseEnter;
                container.MouseLeave -= containerMouseLeave;
                txt.Enter -= txtEnter;
                txt.Leave -= txtLeave;
                txt.SizeChanged -= sizeChanged;
                container.Click -= containerClick;
            };
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
