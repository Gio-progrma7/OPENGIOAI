using OPENGIOAI.Themas;

namespace OPENGIOAI.Vistas
{
    partial class FrmModelos
    {
        /// <summary>
        /// Required designer variable.
        /// </summary>
        private System.ComponentModel.IContainer components = null;

        /// <summary>
        /// Clean up any resources being used.
        /// </summary>
        /// <param name="disposing">true if managed resources should be disposed; otherwise, false.</param>
        protected override void Dispose(bool disposing)
        {
            if (disposing && (components != null))
            {
                components.Dispose();
            }
            base.Dispose(disposing);
        }

        #region Windows Form Designer generated code

        /// <summary>
        /// Required method for Designer support - do not modify
        /// the contents of this method with the code editor.
        /// </summary>
        private void InitializeComponent()
        {
            pnllChat = new Panel();
            btnGuardarChat = new Button();
            label11 = new Label();
            comboBoxApiChat = new ModernComboBox();
            label2 = new Label();
            checkBoxChat = new CheckBox();
            comboBoxMChat = new ModernComboBox();
            label1 = new Label();
            pnlGem = new Panel();
            btnGuardaGem = new Button();
            label12 = new Label();
            comboBoxApiGem = new ModernComboBox();
            label3 = new Label();
            checkBoxGem = new CheckBox();
            comboBoxMGem = new ModernComboBox();
            label4 = new Label();
            pnlOllla = new Panel();
            label13 = new Label();
            label5 = new Label();
            btnGuardarOlla = new Button();
            comboBoxApiOlla = new ModernComboBox();
            checkBoxOlla = new CheckBox();
            comboBoxmOlla = new ModernComboBox();
            label6 = new Label();
            pnlClau = new Panel();
            label14 = new Label();
            label7 = new Label();
            btnGuardarClau = new Button();
            comboBoxApiClau = new ModernComboBox();
            checkBoxClua = new CheckBox();
            comboBoxMClau = new ModernComboBox();
            label8 = new Label();
            pnlDeesp = new Panel();
            label9 = new Label();
            label19 = new Label();
            btnGuardarDeesp = new Button();
            comboBoxApiDesp = new ModernComboBox();
            checkBoxDeesp = new CheckBox();
            comboBoxMDesp = new ModernComboBox();
            label10 = new Label();
            pnlOpenroute = new Panel();
            label16 = new Label();
            label15 = new Label();
            btnOpenroute = new Button();
            ComboxApiOpenroute = new ModernComboBox();
            checkBoxOpenroute = new CheckBox();
            ComboxMOpenroute = new ModernComboBox();
            label18 = new Label();
            label17 = new Label();
            label20 = new Label();
            pnllChat.SuspendLayout();
            pnlGem.SuspendLayout();
            pnlOllla.SuspendLayout();
            pnlClau.SuspendLayout();
            pnlDeesp.SuspendLayout();
            pnlOpenroute.SuspendLayout();
            SuspendLayout();
            // 
            // pnllChat
            // 
            pnllChat.BackColor = Color.FromArgb(30, 41, 59);
            pnllChat.Controls.Add(btnGuardarChat);
            pnllChat.Controls.Add(label11);
            pnllChat.Controls.Add(comboBoxApiChat);
            pnllChat.Controls.Add(label2);
            pnllChat.Controls.Add(checkBoxChat);
            pnllChat.Controls.Add(comboBoxMChat);
            pnllChat.Controls.Add(label1);
            pnllChat.Location = new Point(33, 84);
            pnllChat.Name = "pnllChat";
            pnllChat.Size = new Size(221, 224);
            pnllChat.TabIndex = 0;
            // 
            // btnGuardarChat
            // 
            btnGuardarChat.FlatAppearance.BorderColor = Color.Blue;
            btnGuardarChat.FlatAppearance.MouseOverBackColor = Color.FromArgb(0, 0, 64);
            btnGuardarChat.FlatStyle = FlatStyle.Flat;
            btnGuardarChat.ForeColor = Color.White;
            btnGuardarChat.Location = new Point(115, 187);
            btnGuardarChat.Name = "btnGuardarChat";
            btnGuardarChat.Size = new Size(75, 23);
            btnGuardarChat.TabIndex = 7;
            btnGuardarChat.Tag = "1";
            btnGuardarChat.Text = "Guardar";
            btnGuardarChat.UseVisualStyleBackColor = true;
            btnGuardarChat.Click += btnGuardarChat_Click;
            // 
            // label11
            // 
            label11.AutoSize = true;
            label11.BackColor = Color.Transparent;
            label11.ForeColor = Color.White;
            label11.Location = new Point(19, 129);
            label11.Name = "label11";
            label11.Size = new Size(48, 15);
            label11.TabIndex = 6;
            label11.Text = "API KEY";
            // 
            // comboBoxApiChat
            // 
            comboBoxApiChat.BackColor = Color.Black;
            comboBoxApiChat.DrawMode = DrawMode.OwnerDrawFixed;
            comboBoxApiChat.DropDownStyle = ComboBoxStyle.DropDownList;
            comboBoxApiChat.FlatStyle = FlatStyle.Flat;
            comboBoxApiChat.ForeColor = Color.White;
            comboBoxApiChat.FormattingEnabled = true;
            comboBoxApiChat.ItemHeight = 20;
            comboBoxApiChat.Location = new Point(19, 147);
            comboBoxApiChat.Name = "comboBoxApiChat";
            comboBoxApiChat.Size = new Size(171, 26);
            comboBoxApiChat.TabIndex = 5;
            comboBoxApiChat.SelectedIndexChanged += comboBoxApiChat_SelectedIndexChanged;
            // 
            // label2
            // 
            label2.AutoSize = true;
            label2.BackColor = Color.Transparent;
            label2.ForeColor = Color.White;
            label2.Location = new Point(19, 71);
            label2.Name = "label2";
            label2.Size = new Size(160, 15);
            label2.TabIndex = 3;
            label2.Text = "MODELO PREDETERMINADO";
            // 
            // checkBoxChat
            // 
            checkBoxChat.AutoSize = true;
            checkBoxChat.BackColor = Color.Transparent;
            checkBoxChat.Font = new Font("Segoe UI", 11.25F, FontStyle.Regular, GraphicsUnit.Point, 0);
            checkBoxChat.ForeColor = Color.Gray;
            checkBoxChat.Location = new Point(143, 3);
            checkBoxChat.Name = "checkBoxChat";
            checkBoxChat.Size = new Size(75, 24);
            checkBoxChat.TabIndex = 2;
            checkBoxChat.Text = "Utilizar";
            checkBoxChat.UseVisualStyleBackColor = false;
            checkBoxChat.Visible = false;
            // 
            // comboBoxMChat
            // 
            comboBoxMChat.BackColor = Color.Black;
            comboBoxMChat.DrawMode = DrawMode.OwnerDrawFixed;
            comboBoxMChat.DropDownStyle = ComboBoxStyle.DropDownList;
            comboBoxMChat.FlatStyle = FlatStyle.Flat;
            comboBoxMChat.ForeColor = Color.White;
            comboBoxMChat.FormattingEnabled = true;
            comboBoxMChat.ItemHeight = 20;
            comboBoxMChat.Location = new Point(19, 90);
            comboBoxMChat.Name = "comboBoxMChat";
            comboBoxMChat.Size = new Size(171, 26);
            comboBoxMChat.TabIndex = 1;
            // 
            // label1
            // 
            label1.AutoSize = true;
            label1.BackColor = Color.Transparent;
            label1.ForeColor = Color.White;
            label1.Location = new Point(17, 13);
            label1.Name = "label1";
            label1.Size = new Size(47, 15);
            label1.TabIndex = 0;
            label1.Text = "OpenAI";
            // 
            // pnlGem
            // 
            pnlGem.BackColor = Color.FromArgb(30, 41, 59);
            pnlGem.Controls.Add(btnGuardaGem);
            pnlGem.Controls.Add(label12);
            pnlGem.Controls.Add(comboBoxApiGem);
            pnlGem.Controls.Add(label3);
            pnlGem.Controls.Add(checkBoxGem);
            pnlGem.Controls.Add(comboBoxMGem);
            pnlGem.Controls.Add(label4);
            pnlGem.Location = new Point(337, 84);
            pnlGem.Name = "pnlGem";
            pnlGem.Size = new Size(221, 224);
            pnlGem.TabIndex = 1;
            // 
            // btnGuardaGem
            // 
            btnGuardaGem.FlatAppearance.BorderColor = Color.Blue;
            btnGuardaGem.FlatAppearance.MouseOverBackColor = Color.FromArgb(0, 0, 64);
            btnGuardaGem.FlatStyle = FlatStyle.Flat;
            btnGuardaGem.ForeColor = Color.White;
            btnGuardaGem.Location = new Point(117, 187);
            btnGuardaGem.Name = "btnGuardaGem";
            btnGuardaGem.Size = new Size(75, 23);
            btnGuardaGem.TabIndex = 9;
            btnGuardaGem.Tag = "3";
            btnGuardaGem.Text = "Guardar";
            btnGuardaGem.UseVisualStyleBackColor = true;
            btnGuardaGem.Click += btnGuardaGem_Click;
            // 
            // label12
            // 
            label12.AutoSize = true;
            label12.BackColor = Color.Transparent;
            label12.ForeColor = Color.White;
            label12.Location = new Point(21, 123);
            label12.Name = "label12";
            label12.Size = new Size(48, 15);
            label12.TabIndex = 8;
            label12.Text = "API KEY";
            // 
            // comboBoxApiGem
            // 
            comboBoxApiGem.BackColor = Color.Black;
            comboBoxApiGem.DrawMode = DrawMode.OwnerDrawFixed;
            comboBoxApiGem.DropDownStyle = ComboBoxStyle.DropDownList;
            comboBoxApiGem.FlatStyle = FlatStyle.Flat;
            comboBoxApiGem.ForeColor = Color.White;
            comboBoxApiGem.FormattingEnabled = true;
            comboBoxApiGem.ItemHeight = 20;
            comboBoxApiGem.Location = new Point(21, 141);
            comboBoxApiGem.Name = "comboBoxApiGem";
            comboBoxApiGem.Size = new Size(171, 26);
            comboBoxApiGem.TabIndex = 7;
            comboBoxApiGem.SelectedIndexChanged += comboBoxApiGem_SelectedIndexChanged;
            // 
            // label3
            // 
            label3.AutoSize = true;
            label3.BackColor = Color.Transparent;
            label3.ForeColor = Color.White;
            label3.Location = new Point(21, 72);
            label3.Name = "label3";
            label3.Size = new Size(160, 15);
            label3.TabIndex = 3;
            label3.Text = "MODELO PREDETERMINADO";
            // 
            // checkBoxGem
            // 
            checkBoxGem.AutoSize = true;
            checkBoxGem.BackColor = Color.Transparent;
            checkBoxGem.Font = new Font("Segoe UI", 11.25F, FontStyle.Regular, GraphicsUnit.Point, 0);
            checkBoxGem.ForeColor = Color.Gray;
            checkBoxGem.Location = new Point(143, 4);
            checkBoxGem.Name = "checkBoxGem";
            checkBoxGem.Size = new Size(75, 24);
            checkBoxGem.TabIndex = 2;
            checkBoxGem.Text = "Utilizar";
            checkBoxGem.UseVisualStyleBackColor = false;
            checkBoxGem.Visible = false;
            // 
            // comboBoxMGem
            // 
            comboBoxMGem.BackColor = Color.Black;
            comboBoxMGem.DrawMode = DrawMode.OwnerDrawFixed;
            comboBoxMGem.DropDownStyle = ComboBoxStyle.DropDownList;
            comboBoxMGem.FlatStyle = FlatStyle.Flat;
            comboBoxMGem.ForeColor = Color.White;
            comboBoxMGem.FormattingEnabled = true;
            comboBoxMGem.ItemHeight = 20;
            comboBoxMGem.Location = new Point(19, 90);
            comboBoxMGem.Name = "comboBoxMGem";
            comboBoxMGem.Size = new Size(171, 26);
            comboBoxMGem.TabIndex = 1;
            // 
            // label4
            // 
            label4.AutoSize = true;
            label4.BackColor = Color.Transparent;
            label4.ForeColor = Color.White;
            label4.Location = new Point(19, 17);
            label4.Name = "label4";
            label4.Size = new Size(86, 15);
            label4.TabIndex = 0;
            label4.Text = "Google Gemini";
            // 
            // pnlOllla
            // 
            pnlOllla.BackColor = Color.FromArgb(30, 41, 59);
            pnlOllla.Controls.Add(label13);
            pnlOllla.Controls.Add(label5);
            pnlOllla.Controls.Add(btnGuardarOlla);
            pnlOllla.Controls.Add(comboBoxApiOlla);
            pnlOllla.Controls.Add(checkBoxOlla);
            pnlOllla.Controls.Add(comboBoxmOlla);
            pnlOllla.Controls.Add(label6);
            pnlOllla.Location = new Point(636, 87);
            pnlOllla.Name = "pnlOllla";
            pnlOllla.Size = new Size(221, 221);
            pnlOllla.TabIndex = 2;
            // 
            // label13
            // 
            label13.AutoSize = true;
            label13.BackColor = Color.Transparent;
            label13.ForeColor = Color.White;
            label13.Location = new Point(19, 120);
            label13.Name = "label13";
            label13.Size = new Size(48, 15);
            label13.TabIndex = 13;
            label13.Text = "API KEY";
            label13.Visible = false;
            // 
            // label5
            // 
            label5.AutoSize = true;
            label5.BackColor = Color.Transparent;
            label5.ForeColor = Color.White;
            label5.Location = new Point(19, 69);
            label5.Name = "label5";
            label5.Size = new Size(160, 15);
            label5.TabIndex = 12;
            label5.Text = "MODELO PREDETERMINADO";
            // 
            // btnGuardarOlla
            // 
            btnGuardarOlla.FlatAppearance.BorderColor = Color.Blue;
            btnGuardarOlla.FlatAppearance.MouseOverBackColor = Color.FromArgb(0, 0, 64);
            btnGuardarOlla.FlatStyle = FlatStyle.Flat;
            btnGuardarOlla.ForeColor = Color.White;
            btnGuardarOlla.Location = new Point(118, 184);
            btnGuardarOlla.Name = "btnGuardarOlla";
            btnGuardarOlla.Size = new Size(75, 23);
            btnGuardarOlla.TabIndex = 11;
            btnGuardarOlla.Tag = "4";
            btnGuardarOlla.Text = "Guardar";
            btnGuardarOlla.UseVisualStyleBackColor = true;
            btnGuardarOlla.Click += btnGuardarOlla_Click;
            // 
            // comboBoxApiOlla
            // 
            comboBoxApiOlla.BackColor = Color.Black;
            comboBoxApiOlla.DrawMode = DrawMode.OwnerDrawFixed;
            comboBoxApiOlla.DropDownStyle = ComboBoxStyle.DropDownList;
            comboBoxApiOlla.FlatStyle = FlatStyle.Flat;
            comboBoxApiOlla.ForeColor = Color.White;
            comboBoxApiOlla.FormattingEnabled = true;
            comboBoxApiOlla.ItemHeight = 20;
            comboBoxApiOlla.Location = new Point(21, 138);
            comboBoxApiOlla.Name = "comboBoxApiOlla";
            comboBoxApiOlla.Size = new Size(174, 26);
            comboBoxApiOlla.TabIndex = 9;
            comboBoxApiOlla.Visible = false;
            // 
            // checkBoxOlla
            // 
            checkBoxOlla.AutoSize = true;
            checkBoxOlla.BackColor = Color.Transparent;
            checkBoxOlla.Font = new Font("Segoe UI", 11.25F, FontStyle.Regular, GraphicsUnit.Point, 0);
            checkBoxOlla.ForeColor = Color.Gray;
            checkBoxOlla.Location = new Point(143, 5);
            checkBoxOlla.Name = "checkBoxOlla";
            checkBoxOlla.Size = new Size(75, 24);
            checkBoxOlla.TabIndex = 2;
            checkBoxOlla.Text = "Utilizar";
            checkBoxOlla.UseVisualStyleBackColor = false;
            checkBoxOlla.Visible = false;
            // 
            // comboBoxmOlla
            // 
            comboBoxmOlla.BackColor = Color.Black;
            comboBoxmOlla.DrawMode = DrawMode.OwnerDrawFixed;
            comboBoxmOlla.DropDownStyle = ComboBoxStyle.DropDownList;
            comboBoxmOlla.FlatStyle = FlatStyle.Flat;
            comboBoxmOlla.ForeColor = Color.White;
            comboBoxmOlla.FormattingEnabled = true;
            comboBoxmOlla.ItemHeight = 20;
            comboBoxmOlla.Location = new Point(19, 87);
            comboBoxmOlla.Name = "comboBoxmOlla";
            comboBoxmOlla.Size = new Size(174, 26);
            comboBoxmOlla.TabIndex = 1;
            // 
            // label6
            // 
            label6.AutoSize = true;
            label6.BackColor = Color.Transparent;
            label6.ForeColor = Color.White;
            label6.Location = new Point(21, 14);
            label6.Name = "label6";
            label6.Size = new Size(84, 15);
            label6.TabIndex = 0;
            label6.Text = "Ollama (Local)";
            // 
            // pnlClau
            // 
            pnlClau.BackColor = Color.FromArgb(30, 41, 59);
            pnlClau.Controls.Add(label14);
            pnlClau.Controls.Add(label7);
            pnlClau.Controls.Add(btnGuardarClau);
            pnlClau.Controls.Add(comboBoxApiClau);
            pnlClau.Controls.Add(checkBoxClua);
            pnlClau.Controls.Add(comboBoxMClau);
            pnlClau.Controls.Add(label8);
            pnlClau.Location = new Point(33, 330);
            pnlClau.Name = "pnlClau";
            pnlClau.Size = new Size(221, 228);
            pnlClau.TabIndex = 3;
            // 
            // label14
            // 
            label14.AutoSize = true;
            label14.BackColor = Color.Transparent;
            label14.ForeColor = Color.White;
            label14.Location = new Point(21, 128);
            label14.Name = "label14";
            label14.Size = new Size(48, 15);
            label14.TabIndex = 12;
            label14.Text = "API KEY";
            // 
            // label7
            // 
            label7.AutoSize = true;
            label7.BackColor = Color.Transparent;
            label7.ForeColor = Color.White;
            label7.Location = new Point(19, 70);
            label7.Name = "label7";
            label7.Size = new Size(160, 15);
            label7.TabIndex = 11;
            label7.Text = "MODELO PREDETERMINADO";
            // 
            // btnGuardarClau
            // 
            btnGuardarClau.FlatAppearance.BorderColor = Color.Blue;
            btnGuardarClau.FlatAppearance.MouseOverBackColor = Color.FromArgb(0, 0, 64);
            btnGuardarClau.FlatStyle = FlatStyle.Flat;
            btnGuardarClau.ForeColor = Color.White;
            btnGuardarClau.Location = new Point(115, 190);
            btnGuardarClau.Name = "btnGuardarClau";
            btnGuardarClau.Size = new Size(75, 23);
            btnGuardarClau.TabIndex = 8;
            btnGuardarClau.Tag = "2";
            btnGuardarClau.Text = "Guardar";
            btnGuardarClau.UseVisualStyleBackColor = true;
            btnGuardarClau.Click += btnGuardarClau_Click;
            // 
            // comboBoxApiClau
            // 
            comboBoxApiClau.BackColor = Color.Black;
            comboBoxApiClau.DrawMode = DrawMode.OwnerDrawFixed;
            comboBoxApiClau.DropDownStyle = ComboBoxStyle.DropDownList;
            comboBoxApiClau.FlatStyle = FlatStyle.Flat;
            comboBoxApiClau.ForeColor = Color.White;
            comboBoxApiClau.FormattingEnabled = true;
            comboBoxApiClau.ItemHeight = 20;
            comboBoxApiClau.Location = new Point(21, 146);
            comboBoxApiClau.Name = "comboBoxApiClau";
            comboBoxApiClau.Size = new Size(171, 26);
            comboBoxApiClau.TabIndex = 9;
            comboBoxApiClau.SelectedIndexChanged += comboBoxApiClau_SelectedIndexChanged;
            // 
            // checkBoxClua
            // 
            checkBoxClua.AutoSize = true;
            checkBoxClua.BackColor = Color.Transparent;
            checkBoxClua.Font = new Font("Segoe UI", 11.25F, FontStyle.Regular, GraphicsUnit.Point, 0);
            checkBoxClua.ForeColor = Color.Gray;
            checkBoxClua.Location = new Point(143, 6);
            checkBoxClua.Name = "checkBoxClua";
            checkBoxClua.Size = new Size(75, 24);
            checkBoxClua.TabIndex = 2;
            checkBoxClua.Text = "Utilizar";
            checkBoxClua.UseVisualStyleBackColor = false;
            checkBoxClua.Visible = false;
            // 
            // comboBoxMClau
            // 
            comboBoxMClau.BackColor = Color.Black;
            comboBoxMClau.DrawMode = DrawMode.OwnerDrawFixed;
            comboBoxMClau.DropDownStyle = ComboBoxStyle.DropDownList;
            comboBoxMClau.FlatStyle = FlatStyle.Flat;
            comboBoxMClau.ForeColor = Color.White;
            comboBoxMClau.FormattingEnabled = true;
            comboBoxMClau.ItemHeight = 20;
            comboBoxMClau.Location = new Point(19, 88);
            comboBoxMClau.Name = "comboBoxMClau";
            comboBoxMClau.Size = new Size(173, 26);
            comboBoxMClau.TabIndex = 1;
            // 
            // label8
            // 
            label8.AutoSize = true;
            label8.BackColor = Color.Transparent;
            label8.ForeColor = Color.White;
            label8.Location = new Point(17, 15);
            label8.Name = "label8";
            label8.Size = new Size(100, 15);
            label8.TabIndex = 0;
            label8.Text = "Anthropic Claude";
            // 
            // pnlDeesp
            // 
            pnlDeesp.BackColor = Color.FromArgb(30, 41, 59);
            pnlDeesp.Controls.Add(label9);
            pnlDeesp.Controls.Add(label19);
            pnlDeesp.Controls.Add(btnGuardarDeesp);
            pnlDeesp.Controls.Add(comboBoxApiDesp);
            pnlDeesp.Controls.Add(checkBoxDeesp);
            pnlDeesp.Controls.Add(comboBoxMDesp);
            pnlDeesp.Controls.Add(label10);
            pnlDeesp.Location = new Point(334, 330);
            pnlDeesp.Name = "pnlDeesp";
            pnlDeesp.Size = new Size(221, 228);
            pnlDeesp.TabIndex = 4;
            // 
            // label9
            // 
            label9.AutoSize = true;
            label9.BackColor = Color.Transparent;
            label9.ForeColor = Color.White;
            label9.Location = new Point(24, 128);
            label9.Name = "label9";
            label9.Size = new Size(48, 15);
            label9.TabIndex = 15;
            label9.Text = "API KEY";
            // 
            // label19
            // 
            label19.AutoSize = true;
            label19.BackColor = Color.Transparent;
            label19.ForeColor = Color.White;
            label19.Location = new Point(19, 70);
            label19.Name = "label19";
            label19.Size = new Size(160, 15);
            label19.TabIndex = 14;
            label19.Text = "MODELO PREDETERMINADO";
            // 
            // btnGuardarDeesp
            // 
            btnGuardarDeesp.FlatAppearance.BorderColor = Color.Blue;
            btnGuardarDeesp.FlatAppearance.MouseOverBackColor = Color.FromArgb(0, 0, 64);
            btnGuardarDeesp.FlatStyle = FlatStyle.Flat;
            btnGuardarDeesp.ForeColor = Color.White;
            btnGuardarDeesp.Location = new Point(118, 190);
            btnGuardarDeesp.Name = "btnGuardarDeesp";
            btnGuardarDeesp.Size = new Size(75, 23);
            btnGuardarDeesp.TabIndex = 13;
            btnGuardarDeesp.Tag = "5";
            btnGuardarDeesp.Text = "Guardar";
            btnGuardarDeesp.UseVisualStyleBackColor = true;
            btnGuardarDeesp.Click += btnGuardarDeesp_Click;
            // 
            // comboBoxApiDesp
            // 
            comboBoxApiDesp.BackColor = Color.Black;
            comboBoxApiDesp.DrawMode = DrawMode.OwnerDrawFixed;
            comboBoxApiDesp.DropDownStyle = ComboBoxStyle.DropDownList;
            comboBoxApiDesp.FlatStyle = FlatStyle.Flat;
            comboBoxApiDesp.ForeColor = Color.White;
            comboBoxApiDesp.FormattingEnabled = true;
            comboBoxApiDesp.ItemHeight = 20;
            comboBoxApiDesp.Location = new Point(22, 146);
            comboBoxApiDesp.Name = "comboBoxApiDesp";
            comboBoxApiDesp.Size = new Size(165, 26);
            comboBoxApiDesp.TabIndex = 11;
            comboBoxApiDesp.SelectedIndexChanged += comboBoxApiDesp_SelectedIndexChanged;
            // 
            // checkBoxDeesp
            // 
            checkBoxDeesp.AutoSize = true;
            checkBoxDeesp.BackColor = Color.Transparent;
            checkBoxDeesp.Font = new Font("Segoe UI", 11.25F, FontStyle.Regular, GraphicsUnit.Point, 0);
            checkBoxDeesp.ForeColor = Color.Gray;
            checkBoxDeesp.Location = new Point(146, 6);
            checkBoxDeesp.Name = "checkBoxDeesp";
            checkBoxDeesp.Size = new Size(75, 24);
            checkBoxDeesp.TabIndex = 2;
            checkBoxDeesp.Text = "Utilizar";
            checkBoxDeesp.UseVisualStyleBackColor = false;
            checkBoxDeesp.Visible = false;
            // 
            // comboBoxMDesp
            // 
            comboBoxMDesp.BackColor = Color.Black;
            comboBoxMDesp.DrawMode = DrawMode.OwnerDrawFixed;
            comboBoxMDesp.DropDownStyle = ComboBoxStyle.DropDownList;
            comboBoxMDesp.FlatStyle = FlatStyle.Flat;
            comboBoxMDesp.ForeColor = Color.White;
            comboBoxMDesp.FormattingEnabled = true;
            comboBoxMDesp.ItemHeight = 20;
            comboBoxMDesp.Location = new Point(19, 88);
            comboBoxMDesp.Name = "comboBoxMDesp";
            comboBoxMDesp.Size = new Size(171, 26);
            comboBoxMDesp.TabIndex = 1;
            // 
            // label10
            // 
            label10.AutoSize = true;
            label10.BackColor = Color.Transparent;
            label10.ForeColor = Color.White;
            label10.Location = new Point(22, 15);
            label10.Name = "label10";
            label10.Size = new Size(58, 15);
            label10.TabIndex = 0;
            label10.Text = "DeepSeek";
            // 
            // pnlOpenroute
            // 
            pnlOpenroute.BackColor = Color.FromArgb(30, 41, 59);
            pnlOpenroute.Controls.Add(label16);
            pnlOpenroute.Controls.Add(label15);
            pnlOpenroute.Controls.Add(btnOpenroute);
            pnlOpenroute.Controls.Add(ComboxApiOpenroute);
            pnlOpenroute.Controls.Add(checkBoxOpenroute);
            pnlOpenroute.Controls.Add(ComboxMOpenroute);
            pnlOpenroute.Controls.Add(label18);
            pnlOpenroute.Location = new Point(636, 330);
            pnlOpenroute.Name = "pnlOpenroute";
            pnlOpenroute.Size = new Size(221, 228);
            pnlOpenroute.TabIndex = 5;
            // 
            // label16
            // 
            label16.AutoSize = true;
            label16.BackColor = Color.Transparent;
            label16.ForeColor = Color.White;
            label16.Location = new Point(21, 128);
            label16.Name = "label16";
            label16.Size = new Size(48, 15);
            label16.TabIndex = 16;
            label16.Text = "API KEY";
            // 
            // label15
            // 
            label15.AutoSize = true;
            label15.BackColor = Color.Transparent;
            label15.ForeColor = Color.White;
            label15.Location = new Point(19, 70);
            label15.Name = "label15";
            label15.Size = new Size(160, 15);
            label15.TabIndex = 15;
            label15.Text = "MODELO PREDETERMINADO";
            // 
            // btnOpenroute
            // 
            btnOpenroute.FlatAppearance.BorderColor = Color.Blue;
            btnOpenroute.FlatAppearance.MouseOverBackColor = Color.FromArgb(0, 0, 64);
            btnOpenroute.FlatStyle = FlatStyle.Flat;
            btnOpenroute.ForeColor = Color.White;
            btnOpenroute.Location = new Point(118, 190);
            btnOpenroute.Name = "btnOpenroute";
            btnOpenroute.Size = new Size(75, 23);
            btnOpenroute.TabIndex = 8;
            btnOpenroute.Tag = "2";
            btnOpenroute.Text = "Guardar";
            btnOpenroute.UseVisualStyleBackColor = true;
            btnOpenroute.Click += btnOpenroute_Click;
            // 
            // ComboxApiOpenroute
            // 
            ComboxApiOpenroute.BackColor = Color.Black;
            ComboxApiOpenroute.DrawMode = DrawMode.OwnerDrawFixed;
            ComboxApiOpenroute.DropDownStyle = ComboBoxStyle.DropDownList;
            ComboxApiOpenroute.FlatStyle = FlatStyle.Flat;
            ComboxApiOpenroute.ForeColor = Color.White;
            ComboxApiOpenroute.FormattingEnabled = true;
            ComboxApiOpenroute.ItemHeight = 20;
            ComboxApiOpenroute.Location = new Point(21, 146);
            ComboxApiOpenroute.Name = "ComboxApiOpenroute";
            ComboxApiOpenroute.Size = new Size(171, 26);
            ComboxApiOpenroute.TabIndex = 9;
            ComboxApiOpenroute.SelectedIndexChanged += ComboxApiOpenroute_SelectedIndexChanged;
            // 
            // checkBoxOpenroute
            // 
            checkBoxOpenroute.AutoSize = true;
            checkBoxOpenroute.BackColor = Color.Transparent;
            checkBoxOpenroute.Font = new Font("Segoe UI", 11.25F, FontStyle.Regular, GraphicsUnit.Point, 0);
            checkBoxOpenroute.ForeColor = Color.Gray;
            checkBoxOpenroute.Location = new Point(143, 6);
            checkBoxOpenroute.Name = "checkBoxOpenroute";
            checkBoxOpenroute.Size = new Size(75, 24);
            checkBoxOpenroute.TabIndex = 2;
            checkBoxOpenroute.Text = "Utilizar";
            checkBoxOpenroute.UseVisualStyleBackColor = false;
            checkBoxOpenroute.Visible = false;
            // 
            // ComboxMOpenroute
            // 
            ComboxMOpenroute.BackColor = Color.Black;
            ComboxMOpenroute.DrawMode = DrawMode.OwnerDrawFixed;
            ComboxMOpenroute.DropDownStyle = ComboBoxStyle.DropDownList;
            ComboxMOpenroute.FlatStyle = FlatStyle.Flat;
            ComboxMOpenroute.ForeColor = Color.White;
            ComboxMOpenroute.FormattingEnabled = true;
            ComboxMOpenroute.ItemHeight = 20;
            ComboxMOpenroute.Location = new Point(21, 88);
            ComboxMOpenroute.Name = "ComboxMOpenroute";
            ComboxMOpenroute.Size = new Size(173, 26);
            ComboxMOpenroute.TabIndex = 1;
            // 
            // label18
            // 
            label18.AutoSize = true;
            label18.BackColor = Color.Transparent;
            label18.ForeColor = Color.White;
            label18.Location = new Point(19, 15);
            label18.Name = "label18";
            label18.Size = new Size(71, 15);
            label18.TabIndex = 0;
            label18.Text = "OpenRouter";
            // 
            // label17
            // 
            label17.AutoSize = true;
            label17.Font = new Font("Segoe UI", 14.25F, FontStyle.Regular, GraphicsUnit.Point, 0);
            label17.ForeColor = Color.White;
            label17.Location = new Point(38, 10);
            label17.Name = "label17";
            label17.Size = new Size(129, 25);
            label17.TabIndex = 6;
            label17.Tag = "";
            label17.Text = "Proveedores /";
            // 
            // label20
            // 
            label20.AutoSize = true;
            label20.ForeColor = SystemColors.ControlDark;
            label20.Location = new Point(161, 19);
            label20.Name = "label20";
            label20.Size = new Size(292, 15);
            label20.TabIndex = 7;
            label20.Text = "Configuración de servicios externos y modelos locales";
            // 
            // FrmModelos
            // 
            AutoScaleDimensions = new SizeF(7F, 15F);
            AutoScaleMode = AutoScaleMode.Font;
            BackColor = Color.FromArgb(15, 23, 42);
            ClientSize = new Size(910, 598);
            Controls.Add(label20);
            Controls.Add(label17);
            Controls.Add(pnlOpenroute);
            Controls.Add(pnlDeesp);
            Controls.Add(pnlClau);
            Controls.Add(pnlOllla);
            Controls.Add(pnlGem);
            Controls.Add(pnllChat);
            Name = "FrmModelos";
            Text = "FrmModelos";
            Load += FrmModelos_Load;
            pnllChat.ResumeLayout(false);
            pnllChat.PerformLayout();
            pnlGem.ResumeLayout(false);
            pnlGem.PerformLayout();
            pnlOllla.ResumeLayout(false);
            pnlOllla.PerformLayout();
            pnlClau.ResumeLayout(false);
            pnlClau.PerformLayout();
            pnlDeesp.ResumeLayout(false);
            pnlDeesp.PerformLayout();
            pnlOpenroute.ResumeLayout(false);
            pnlOpenroute.PerformLayout();
            ResumeLayout(false);
            PerformLayout();
        }

        #endregion

        private Panel pnllChat;
        private Label label2;
        private CheckBox checkBoxChat;
        private ModernComboBox comboBoxMChat;
        private Label label1;
        private Panel pnlGem;
        private Label label3;
        private CheckBox checkBoxGem;
        private ModernComboBox comboBoxMGem;
        private Label label4;
        private Panel pnlOllla;
        private CheckBox checkBoxOlla;
        private ModernComboBox comboBoxmOlla;
        private Label label6;
        private Panel pnlClau;
        private CheckBox checkBoxClua;
        private ModernComboBox comboBoxMClau;
        private Label label8;
        private Panel pnlDeesp;
        private CheckBox checkBoxDeesp;
        private ModernComboBox comboBoxMDesp;
        private Label label10;
        private Label label11;
        private ModernComboBox comboBoxApiChat;
        private Label label12;
        private ModernComboBox comboBoxApiGem;
        private ModernComboBox comboBoxApiOlla;
        private ModernComboBox comboBoxApiClau;
        private ModernComboBox comboBoxApiDesp;
        private Button btnGuardarChat;
        private Button btnGuardaGem;
        private Button btnGuardarOlla;
        private Button btnGuardarClau;
        private Button btnGuardarDeesp;
        private Panel pnlOpenroute;
        private Button btnOpenroute;
        private ModernComboBox ComboxApiOpenroute;
        private CheckBox checkBoxOpenroute;
        private ModernComboBox ComboxMOpenroute;
        private Label label18;
        private Label label13;
        private Label label5;
        private Label label14;
        private Label label7;
        private Label label9;
        private Label label19;
        private Label label16;
        private Label label15;
        private Label label17;
        private Label label20;
    }
}