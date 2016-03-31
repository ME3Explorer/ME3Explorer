using System.Windows.Forms;

namespace ME3Explorer.PlotVarDB
{
    partial class PlotVarDB
    {
        /// <summary>
        /// Erforderliche Designervariable.
        /// </summary>
        private System.ComponentModel.IContainer components = null;

        /// <summary>
        /// Verwendete Ressourcen bereinigen.
        /// </summary>
        /// <param name="disposing">True, wenn verwaltete Ressourcen gelöscht werden sollen; andernfalls False.</param>
        protected override void Dispose(bool disposing)
        {
            if (disposing && (components != null))
            {
                components.Dispose();
            }
            base.Dispose(disposing);
        }

        #region Vom Windows Form-Designer generierter Code

        /// <summary>
        /// Erforderliche Methode für die Designerunterstützung.
        /// Der Inhalt der Methode darf nicht mit dem Code-Editor geändert werden.
        /// </summary>
        private void InitializeComponent()
        {
            System.ComponentModel.ComponentResourceManager resources = new System.ComponentModel.ComponentResourceManager(typeof(PlotVarDB));
            this.menuStrip1 = new System.Windows.Forms.MenuStrip();
            this.fileToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.newDatabaseToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.loadDatabaseToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.saveDatabaseToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.toolStrip1 = new System.Windows.Forms.ToolStrip();
            this.ME1Button = new System.Windows.Forms.ToolStripButton();
            this.ME2Button = new System.Windows.Forms.ToolStripButton();
            this.ME3Button = new System.Windows.Forms.ToolStripButton();
            this.toolStripTextBox1 = new System.Windows.Forms.ToolStripTextBox();
            this.toolStripButton1 = new System.Windows.Forms.ToolStripButton();
            this.statusStrip1 = new System.Windows.Forms.StatusStrip();
            this.status = new System.Windows.Forms.ToolStripStatusLabel();
            this.plotVarTable = new System.Windows.Forms.DataGridView();
            this.toolStripSeparator1 = new System.Windows.Forms.ToolStripSeparator();
            this.toolStripSeparator2 = new System.Windows.Forms.ToolStripSeparator();
            this.deleteRowButton = new System.Windows.Forms.ToolStripButton();
            this.plotIDColumn = new System.Windows.Forms.DataGridViewTextBoxColumn();
            this.varTypeColumn = new System.Windows.Forms.DataGridViewComboBoxColumn();
            this.descriptionColumn = new System.Windows.Forms.DataGridViewTextBoxColumn();
            this.menuStrip1.SuspendLayout();
            this.toolStrip1.SuspendLayout();
            this.statusStrip1.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)(this.plotVarTable)).BeginInit();
            this.SuspendLayout();
            // 
            // menuStrip1
            // 
            this.menuStrip1.Items.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this.fileToolStripMenuItem});
            this.menuStrip1.Location = new System.Drawing.Point(0, 0);
            this.menuStrip1.Name = "menuStrip1";
            this.menuStrip1.Size = new System.Drawing.Size(514, 24);
            this.menuStrip1.TabIndex = 0;
            this.menuStrip1.Text = "menuStrip1";
            this.menuStrip1.Visible = false;
            // 
            // fileToolStripMenuItem
            // 
            this.fileToolStripMenuItem.DropDownItems.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this.newDatabaseToolStripMenuItem,
            this.loadDatabaseToolStripMenuItem,
            this.saveDatabaseToolStripMenuItem});
            this.fileToolStripMenuItem.Name = "fileToolStripMenuItem";
            this.fileToolStripMenuItem.Size = new System.Drawing.Size(37, 20);
            this.fileToolStripMenuItem.Text = "File";
            // 
            // newDatabaseToolStripMenuItem
            // 
            this.newDatabaseToolStripMenuItem.Name = "newDatabaseToolStripMenuItem";
            this.newDatabaseToolStripMenuItem.Size = new System.Drawing.Size(151, 22);
            this.newDatabaseToolStripMenuItem.Text = "New Database";
            this.newDatabaseToolStripMenuItem.Click += new System.EventHandler(this.newDatabaseToolStripMenuItem_Click);
            // 
            // loadDatabaseToolStripMenuItem
            // 
            this.loadDatabaseToolStripMenuItem.Name = "loadDatabaseToolStripMenuItem";
            this.loadDatabaseToolStripMenuItem.Size = new System.Drawing.Size(151, 22);
            this.loadDatabaseToolStripMenuItem.Text = "Load Database";
            this.loadDatabaseToolStripMenuItem.Click += new System.EventHandler(this.loadDatabaseToolStripMenuItem_Click);
            // 
            // saveDatabaseToolStripMenuItem
            // 
            this.saveDatabaseToolStripMenuItem.Name = "saveDatabaseToolStripMenuItem";
            this.saveDatabaseToolStripMenuItem.Size = new System.Drawing.Size(151, 22);
            this.saveDatabaseToolStripMenuItem.Text = "Save Database";
            this.saveDatabaseToolStripMenuItem.Click += new System.EventHandler(this.saveDatabaseToolStripMenuItem_Click);
            // 
            // toolStrip1
            // 
            this.toolStrip1.Items.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this.ME1Button,
            this.ME2Button,
            this.ME3Button,
            this.toolStripSeparator1,
            this.toolStripTextBox1,
            this.toolStripButton1,
            this.toolStripSeparator2,
            this.deleteRowButton});
            this.toolStrip1.Location = new System.Drawing.Point(0, 0);
            this.toolStrip1.Name = "toolStrip1";
            this.toolStrip1.Size = new System.Drawing.Size(514, 25);
            this.toolStrip1.TabIndex = 1;
            this.toolStrip1.Text = "toolStrip1";
            // 
            // ME1Button
            // 
            this.ME1Button.DisplayStyle = System.Windows.Forms.ToolStripItemDisplayStyle.Text;
            this.ME1Button.Image = ((System.Drawing.Image)(resources.GetObject("ME1Button.Image")));
            this.ME1Button.ImageTransparentColor = System.Drawing.Color.Magenta;
            this.ME1Button.Name = "ME1Button";
            this.ME1Button.Size = new System.Drawing.Size(34, 22);
            this.ME1Button.Text = "ME1";
            this.ME1Button.Click += new System.EventHandler(this.toolStripButton2_Click);
            // 
            // ME2Button
            // 
            this.ME2Button.DisplayStyle = System.Windows.Forms.ToolStripItemDisplayStyle.Text;
            this.ME2Button.Image = ((System.Drawing.Image)(resources.GetObject("ME2Button.Image")));
            this.ME2Button.ImageTransparentColor = System.Drawing.Color.Magenta;
            this.ME2Button.Name = "ME2Button";
            this.ME2Button.Size = new System.Drawing.Size(34, 22);
            this.ME2Button.Text = "ME2";
            this.ME2Button.Click += new System.EventHandler(this.toolStripButton3_Click);
            // 
            // ME3Button
            // 
            this.ME3Button.Checked = true;
            this.ME3Button.CheckState = System.Windows.Forms.CheckState.Checked;
            this.ME3Button.DisplayStyle = System.Windows.Forms.ToolStripItemDisplayStyle.Text;
            this.ME3Button.Image = ((System.Drawing.Image)(resources.GetObject("ME3Button.Image")));
            this.ME3Button.ImageTransparentColor = System.Drawing.Color.Magenta;
            this.ME3Button.Name = "ME3Button";
            this.ME3Button.Size = new System.Drawing.Size(34, 22);
            this.ME3Button.Text = "ME3";
            this.ME3Button.Click += new System.EventHandler(this.toolStripButton4_Click);
            // 
            // toolStripTextBox1
            // 
            this.toolStripTextBox1.Name = "toolStripTextBox1";
            this.toolStripTextBox1.Size = new System.Drawing.Size(100, 25);
            this.toolStripTextBox1.KeyPress += new System.Windows.Forms.KeyPressEventHandler(this.toolStripTextBox1_KeyPress);
            // 
            // toolStripButton1
            // 
            this.toolStripButton1.DisplayStyle = System.Windows.Forms.ToolStripItemDisplayStyle.Text;
            this.toolStripButton1.Image = ((System.Drawing.Image)(resources.GetObject("toolStripButton1.Image")));
            this.toolStripButton1.ImageTransparentColor = System.Drawing.Color.Magenta;
            this.toolStripButton1.Name = "toolStripButton1";
            this.toolStripButton1.Size = new System.Drawing.Size(46, 22);
            this.toolStripButton1.Text = "Search";
            this.toolStripButton1.Click += new System.EventHandler(this.toolStripButton1_Click);
            // 
            // statusStrip1
            // 
            this.statusStrip1.Items.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this.status});
            this.statusStrip1.Location = new System.Drawing.Point(0, 262);
            this.statusStrip1.Name = "statusStrip1";
            this.statusStrip1.Size = new System.Drawing.Size(514, 22);
            this.statusStrip1.TabIndex = 2;
            this.statusStrip1.Text = "statusStrip1";
            // 
            // status
            // 
            this.status.Name = "status";
            this.status.Size = new System.Drawing.Size(0, 17);
            // 
            // plotVarTable
            // 
            this.plotVarTable.ColumnHeadersHeightSizeMode = System.Windows.Forms.DataGridViewColumnHeadersHeightSizeMode.AutoSize;
            this.plotVarTable.Columns.AddRange(new System.Windows.Forms.DataGridViewColumn[] {
            this.plotIDColumn,
            this.varTypeColumn,
            this.descriptionColumn});
            this.plotVarTable.Dock = System.Windows.Forms.DockStyle.Fill;
            this.plotVarTable.Location = new System.Drawing.Point(0, 25);
            this.plotVarTable.MultiSelect = false;
            this.plotVarTable.Name = "plotVarTable";
            this.plotVarTable.Size = new System.Drawing.Size(514, 237);
            this.plotVarTable.TabIndex = 4;
            this.plotVarTable.CellValidating += new System.Windows.Forms.DataGridViewCellValidatingEventHandler(this.plotVarTable_CellValidating);
            this.plotVarTable.KeyDown += new System.Windows.Forms.KeyEventHandler(this.plotVarTable_KeyDown);
            // 
            // toolStripSeparator1
            // 
            this.toolStripSeparator1.Name = "toolStripSeparator1";
            this.toolStripSeparator1.Size = new System.Drawing.Size(6, 25);
            // 
            // toolStripSeparator2
            // 
            this.toolStripSeparator2.Name = "toolStripSeparator2";
            this.toolStripSeparator2.Size = new System.Drawing.Size(6, 25);
            // 
            // deleteRowButton
            // 
            this.deleteRowButton.DisplayStyle = System.Windows.Forms.ToolStripItemDisplayStyle.Text;
            this.deleteRowButton.Image = ((System.Drawing.Image)(resources.GetObject("deleteRowButton.Image")));
            this.deleteRowButton.ImageTransparentColor = System.Drawing.Color.Magenta;
            this.deleteRowButton.Name = "deleteRowButton";
            this.deleteRowButton.Size = new System.Drawing.Size(70, 22);
            this.deleteRowButton.Text = "Delete Row";
            this.deleteRowButton.Click += new System.EventHandler(this.deleteRowButton_Click);
            // 
            // plotIDColumn
            // 
            this.plotIDColumn.Frozen = true;
            this.plotIDColumn.HeaderText = "Plot ID";
            this.plotIDColumn.Name = "plotIDColumn";
            this.plotIDColumn.ToolTipText = "The ID that defines this plot element.";
            // 
            // varTypeColumn
            // 
            this.varTypeColumn.Frozen = true;
            this.varTypeColumn.HeaderText = "Variable Type";
            this.varTypeColumn.Items.AddRange(new object[] {
            "Boolean",
            "Float",
            "Integer"});
            this.varTypeColumn.Name = "varTypeColumn";
            this.varTypeColumn.Resizable = System.Windows.Forms.DataGridViewTriState.True;
            this.varTypeColumn.SortMode = System.Windows.Forms.DataGridViewColumnSortMode.Automatic;
            this.varTypeColumn.ToolTipText = "Type of value this variable holds.";
            // 
            // descriptionColumn
            // 
            this.descriptionColumn.AutoSizeMode = System.Windows.Forms.DataGridViewAutoSizeColumnMode.Fill;
            this.descriptionColumn.HeaderText = "Description";
            this.descriptionColumn.Name = "descriptionColumn";
            this.descriptionColumn.ToolTipText = "Description of the stored Plot ID value and how it is used";
            // 
            // PlotVarDB
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(514, 284);
            this.Controls.Add(this.plotVarTable);
            this.Controls.Add(this.statusStrip1);
            this.Controls.Add(this.toolStrip1);
            this.Controls.Add(this.menuStrip1);
            this.Icon = ((System.Drawing.Icon)(resources.GetObject("$this.Icon")));
            this.MainMenuStrip = this.menuStrip1;
            this.Name = "PlotVarDB";
            this.Text = "Plot Database";
            this.Load += new System.EventHandler(this.PlotVarDB_Load);
            this.menuStrip1.ResumeLayout(false);
            this.menuStrip1.PerformLayout();
            this.toolStrip1.ResumeLayout(false);
            this.toolStrip1.PerformLayout();
            this.statusStrip1.ResumeLayout(false);
            this.statusStrip1.PerformLayout();
            ((System.ComponentModel.ISupportInitialize)(this.plotVarTable)).EndInit();
            this.ResumeLayout(false);
            this.PerformLayout();

        }

        #endregion

        private System.Windows.Forms.MenuStrip menuStrip1;
        private System.Windows.Forms.ToolStrip toolStrip1;
        private System.Windows.Forms.ToolStripButton ME1Button;
        private System.Windows.Forms.ToolStripButton ME2Button;
        private System.Windows.Forms.ToolStripButton ME3Button;
        private System.Windows.Forms.ToolStripTextBox toolStripTextBox1;
        private System.Windows.Forms.ToolStripButton toolStripButton1;
        private System.Windows.Forms.ToolStripMenuItem fileToolStripMenuItem;
        private System.Windows.Forms.ToolStripMenuItem loadDatabaseToolStripMenuItem;
        private System.Windows.Forms.ToolStripMenuItem saveDatabaseToolStripMenuItem;
        private System.Windows.Forms.ToolStripMenuItem newDatabaseToolStripMenuItem;
        private System.Windows.Forms.StatusStrip statusStrip1;
        private System.Windows.Forms.ToolStripStatusLabel status;
        private System.Windows.Forms.DataGridView plotVarTable;
        private ToolStripSeparator toolStripSeparator1;
        private ToolStripSeparator toolStripSeparator2;
        private ToolStripButton deleteRowButton;
        private DataGridViewTextBoxColumn plotIDColumn;
        private DataGridViewComboBoxColumn varTypeColumn;
        private DataGridViewTextBoxColumn descriptionColumn;
    }
}