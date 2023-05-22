namespace MgcPrxyDrftrVanced
{
    partial class MainForm
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
            InstalledSetsListView = new ListView();
            label1 = new Label();
            SuspendLayout();
            // 
            // InstalledSetsListView
            // 
            InstalledSetsListView.Location = new Point(93, 126);
            InstalledSetsListView.Margin = new Padding(3, 4, 3, 4);
            InstalledSetsListView.Name = "InstalledSetsListView";
            InstalledSetsListView.Size = new Size(294, 378);
            InstalledSetsListView.TabIndex = 0;
            InstalledSetsListView.UseCompatibleStateImageBehavior = false;
            // 
            // label1
            // 
            label1.AutoSize = true;
            label1.Location = new Point(93, 104);
            label1.Name = "label1";
            label1.Size = new Size(98, 18);
            label1.TabIndex = 1;
            label1.Text = "Installed Sets";
            // 
            // MainForm
            // 
            AutoScaleDimensions = new SizeF(8F, 18F);
            AutoScaleMode = AutoScaleMode.Font;
            ClientSize = new Size(1091, 847);
            Controls.Add(label1);
            Controls.Add(InstalledSetsListView);
            Font = new Font("Beleren", 11.25F, FontStyle.Bold, GraphicsUnit.Point);
            Margin = new Padding(3, 4, 3, 4);
            Name = "MainForm";
            Text = "MainForm";
            Load += MainForm_Load;
            ResumeLayout(false);
            PerformLayout();
        }

        #endregion

        private ListView InstalledSetsListView;
        private Label label1;
    }
}