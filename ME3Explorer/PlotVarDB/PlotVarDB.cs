using System;
using System.IO;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Windows.Forms;
using System.Diagnostics;
using CsvHelper;

namespace ME3Explorer.PlotVarDB
{
    public partial class PlotVarDB : Form
    {
        public int SortStyle = 0; //0 = by id, 1 = by type, 2 = by desc

        public const int COL_PLOTID = 0;
        public const int COL_VARTYPE = 1;
        public const int COL_GAME = 2;
        public const int COL_CATEGORY = 3;
        public const int COL_STATE = 4;
        public const int COL_BROKEN = 5;
        public const int COL_ME2SPEC = 6;
        public const int COL_ME3SPEC = 7;
        public const int COL_NOTES = 8;

        public const int VARTYPE_BOOL = 0;
        public const int VARTYPE_FLOAT = 1;
        public const int VARTYPE_INTEGER = 2;

        public const int GAME_ME1 = 0;
        public const int GAME_ME2 = 1;
        public const int GAME_ME3 = 2;
        public List<PlotVarEntry> entries;
        private bool validating = true;

        public class PlotVarEntry
        {
            public int id { get; set; }
            public int type { get; set; } // 0 = bool, 1 = int, 2 = float
            public int game { get; set; } //0 = me1, 2 = me2, 3 = me3. Use consts
            public string category { get; set; }
            public string state { get; set; }
            public bool broken { get; set; }
            public int me2id { get; set; }
            public int me3id { get; set; }
            public string notes { get; set; }
        }

        public PlotVarDB()
        {
            InitializeComponent();
            this.plotVarTable.ColumnHeadersHeightSizeMode = DataGridViewColumnHeadersHeightSizeMode.DisableResizing;
        }

        private void PlotVarDB_Load(object sender, EventArgs e)
        {
            this.plotVarTable.ColumnHeadersHeightSizeMode = DataGridViewColumnHeadersHeightSizeMode.AutoSize;
            entries = new List<PlotVarEntry>();
            status.Text = "Open DB from the file menu or start entering data to start a new one";

        }


        public string TypeToString(int type)
        {
            string res = "";
            switch (type)
            {
                case 0:
                    res = "Boolean";
                    break;
                case 1:
                    res = "Integer";
                    break;
                case 2:
                    res = "Float";
                    break;
            }
            return res;
        }

        public void RefreshTable()
        {
            validating = false;
            plotVarTable.ClearSelection();
            plotVarTable.Rows[0].Selected = true;
            Debug.WriteLine("Removing row start: " + plotVarTable.CurrentRow.Index);
            while (plotVarTable.Rows[0] != null && !plotVarTable.Rows[0].IsNewRow)
            {
                plotVarTable.Rows.Remove(plotVarTable.Rows[0]);
            } //avoids a bug with clear()
            validating = true;

            foreach (PlotVarEntry p in entries)
            {
                object[] row = new object[] { p.id.ToString(), TypeToString(p.type), GameToString(p.game), p.category, p.state, p.broken, p.me2id > 0 ? p.me2id.ToString() : "", p.me3id > 0 ? p.me3id.ToString() : "", p.notes };
                plotVarTable.Rows.Add(row);
            }
            //    listBox1.Items.Add("ID: " + p.ID + " TYPE: " + TypeToString(p.type) + " DESCRIPTION: " + p.Desc);
            status.Text = "Elements: " + (plotVarTable.Rows.Count - 1);
        }

        private object GameToString(int game)
        {
            switch (game)
            {
                case GAME_ME1:
                    return "Mass Effect";
                case GAME_ME2:
                    return "Mass Effect 2";
                case GAME_ME3:
                    return "Mass Effect 3";
                default:
                    return "";
            }
        }

        //public void RefreshLists()
        //{
        //    listBox1.Items.Clear();
        //    List<PlotVarEntry> temp = new List<PlotVarEntry>();
        //    if (MEVersion == 0)
        //        temp = database.ME1;
        //    if (MEVersion == 1)
        //        temp = database.ME2;
        //    if (MEVersion == 2)
        //        temp = database.ME3;
        //    bool run = true;
        //    while (run)
        //    {
        //        run = false;
        //        for (int i = 0; i < temp.Count - 1; i++)
        //        {
        //            switch (SortStyle)
        //            {
        //                case 0:
        //                    if (temp[i].ID > temp[i + 1].ID)
        //                    {
        //                        run = true;
        //                        PlotVarEntry t = temp[i];
        //                        temp[i] = temp[i + 1];
        //                        temp[i + 1] = t;
        //                    }
        //                    break;

        //                case 1:
        //                    if (temp[i].type > temp[i + 1].type)
        //                    {
        //                        run = true;
        //                        PlotVarEntry t = temp[i];
        //                        temp[i] = temp[i + 1];
        //                        temp[i + 1] = t;
        //                    }
        //                    break;
        //                case 2:
        //                    if (string.Compare(temp[i].Desc, temp[i + 1].Desc) < 0) 
        //                    {
        //                        run = true;
        //                        PlotVarEntry t = temp[i];
        //                        temp[i] = temp[i + 1];
        //                        temp[i + 1] = t;
        //                    }
        //                    break;
        //            }
        //        }
        //    }
        //    foreach (PlotVarEntry p in temp)
        //        listBox1.Items.Add("ID: " + p.ID + " TYPE: " + TypeToString(p.type) + " DESCRIPTION: " + p.Desc);
        //    status.Text = "Elements: " + listBox1.Items.Count;
        //}

        private void toolStripButton1_Click(object sender, EventArgs e)
        {
            Search();
        }

        public void Search()
        {
            string s = toolStripTextBox1.Text;
            if (s.Length == 0)
                return;
            s = s.ToLower();
            int n = plotVarTable.CurrentRow.Index;
            for (int i = n + 1; i < plotVarTable.Rows.Count; i++)
            {
                DataGridViewCellCollection j = plotVarTable.Rows[i].Cells;

                if (j[COL_PLOTID] != null && j[COL_PLOTID].Value.ToString().ToLower().Contains(s))
                {
                    plotVarTable.ClearSelection();
                    plotVarTable.Rows[i].Selected = true;
                    return;
                };

                if (j[COL_CATEGORY] != null && j[COL_CATEGORY].Value.ToString().ToLower().Contains(s))
                {
                    plotVarTable.ClearSelection();
                    plotVarTable.Rows[i].Selected = true;
                    return;
                };

                if (j[COL_STATE] != null && j[COL_STATE].Value.ToString().ToLower().Contains(s))
                {
                    plotVarTable.ClearSelection();
                    plotVarTable.Rows[i].Selected = true;
                    return;
                };

                if (j[COL_ME2SPEC] != null && j[COL_ME2SPEC].Value.ToString().ToLower().Contains(s))
                {
                    plotVarTable.ClearSelection();
                    plotVarTable.Rows[i].Selected = true;
                    return;
                };

                if (j[COL_ME3SPEC] != null && j[COL_ME3SPEC].Value.ToString().ToLower().Contains(s))
                {
                    plotVarTable.ClearSelection();
                    plotVarTable.Rows[i].Selected = true;
                    return;
                };
            }

        }

        private void toolStripTextBox1_KeyPress(object sender, KeyPressEventArgs e)
        {
            if (e.KeyChar == (char)Keys.Enter)
                Search();
        }

        //private void removeToolStripMenuItem_Click(object sender, EventArgs e)
        //{
        //    int n = listBox1.SelectedIndex;
        //    if (n == -1)
        //        return;
        //    string s = listBox1.Items[n].ToString();
        //    s = s.Substring(4, s.Length - 4);
        //    string[] s2 = s.Split(' ');
        //    int id;
        //    if(!int.TryParse(s2[0], out id))
        //        return;
        //    if(MEVersion == 0)
        //        for(int i=0;i<database.ME1.Count;i++)
        //            if (database.ME1[i].ID == id)
        //            {
        //                database.ME1.RemoveAt(i);
        //                RefreshTable();
        //                return;
        //            }
        //    if (MEVersion == 1)
        //        for (int i = 0; i < database.ME2.Count; i++)
        //            if (database.ME2[i].ID == id)
        //            {
        //                database.ME2.RemoveAt(i);
        //                RefreshTable();
        //                return;
        //            }
        //    if (MEVersion == 2)
        //        for (int i = 0; i < database.ME3.Count; i++)
        //            if (database.ME3[i].ID == id)
        //            {
        //                database.ME3.RemoveAt(i);
        //                RefreshTable();
        //                return;
        //            }
        //}

        private void newDatabaseToolStripMenuItem_Click(object sender, EventArgs e)
        {
            entries = new List<PlotVarEntry>();
            RefreshTable();
        }

        private void saveDatabaseToolStripMenuItem_Click(object sender, EventArgs e)
        {
            commitTable();
            SaveFileDialog d = new SaveFileDialog();
            d.Filter = "*.db|*.db";
            if (d.ShowDialog() == System.Windows.Forms.DialogResult.OK)
            {
                FileStream fs = new FileStream(d.FileName, FileMode.Create, FileAccess.Write);
                BitConverter.IsLittleEndian = true;
                fs.Write(BitConverter.GetBytes(entries.Count), 0, 4);
                foreach (PlotVarEntry p in entries)
                {
                    fs.Write(BitConverter.GetBytes(p.id), 0, 4);
                    fs.Write(BitConverter.GetBytes(p.type), 0, 4);
                    fs.Write(BitConverter.GetBytes(p.game), 0, 4);
                    WriteString(fs, p.category ?? "");
                    WriteString(fs, p.state ?? "");
                    fs.WriteByte(BitConverter.GetBytes(p.broken)[0]); //gets 1 byte true/false
                    fs.Write(BitConverter.GetBytes(p.me2id), 0, 4);
                    fs.Write(BitConverter.GetBytes(p.me3id), 0, 4);
                    WriteString(fs, p.notes ?? "");
                }
                fs.Close();
                status.Text = "Saved DB to " + d.FileName;
            }
        }

        private void commitTable()
        {
            List<PlotVarEntry> commitingEntries = new List<PlotVarEntry>();
            foreach (DataGridViewRow row in plotVarTable.Rows)
            {
                if ((string)row.Cells[COL_PLOTID].Value != null && ((string)row.Cells[COL_PLOTID].Value).Trim() != "")
                {
                    PlotVarEntry pve = new PlotVarEntry();
                    pve.id = Convert.ToInt32((string)row.Cells[COL_PLOTID].Value);
                    pve.type = StringToType((string)row.Cells[COL_VARTYPE].Value);
                    pve.game = StringToGame((string)row.Cells[COL_GAME].Value);
                    pve.category = row.Cells[COL_CATEGORY].Value != null ? row.Cells[COL_CATEGORY].Value.ToString() : "";
                    pve.state = row.Cells[COL_STATE].Value != null ? row.Cells[COL_STATE].Value.ToString() : "";
                    pve.broken = row.Cells[COL_BROKEN].Value != null ? (bool)row.Cells[COL_BROKEN].Value : false;
                    pve.me2id = row.Cells[COL_ME2SPEC].Value != null && !row.Cells[COL_ME2SPEC].Value.Equals("") ? Convert.ToInt32(row.Cells[COL_ME2SPEC].Value.ToString()) : 0;
                    pve.me3id = row.Cells[COL_ME3SPEC].Value != null && !row.Cells[COL_ME3SPEC].Value.Equals("") ? Convert.ToInt32(row.Cells[COL_ME3SPEC].Value.ToString()) : 0;
                    pve.notes = row.Cells[COL_NOTES].Value != null ? row.Cells[COL_NOTES].Value.ToString() : "";



                    commitingEntries.Add(pve);
                }
            }

            entries = commitingEntries;
        }

        private int StringToGame(string value)
        {
            switch (value)
            {
                case "Mass Effect":
                    return GAME_ME1;
                case "Mass Effect 2":
                    return GAME_ME2;
                case "Mass Effect 3":
                    return GAME_ME3;
                default:
                    return 3; //unknown.
            }
        }

        private void plotVarTable_CellValidating(object sender,
                                           DataGridViewCellValidatingEventArgs e)
        {
            if (!validating)
            {
                return;
            }
            if ((e.ColumnIndex == COL_PLOTID || e.ColumnIndex == COL_ME2SPEC || e.ColumnIndex == COL_ME3SPEC) && e.RowIndex < plotVarTable.NewRowIndex && e.FormattedValue != null) // Plot ID
            {
                if (e.ColumnIndex == COL_PLOTID && e.FormattedValue.Equals("") && e.RowIndex != plotVarTable.NewRowIndex)
                {
                    e.Cancel = true;
                    status.Text = "Invalid value. Value cannot be empty.";
                }

                if (e.ColumnIndex == COL_ME2SPEC || e.ColumnIndex == COL_ME3SPEC && e.FormattedValue.Equals(""))
                {
                    status.Text = "";
                    return;
                }
                int i;
                if (!int.TryParse(Convert.ToString(e.FormattedValue), out i))
                {
                    e.Cancel = true;
                    status.Text = "Invalid value. Must be an integer.";
                }
                else
                {
                    // the input is numeric 
                    //we're OK
                    status.Text = "";

                }
            }
        }

        private int StringToType(string value)
        {
            switch (value)
            {
                case "Boolean":
                    return VARTYPE_BOOL;
                case "Integer":
                    return VARTYPE_INTEGER;
                case "Float":
                    return VARTYPE_FLOAT;
                default:
                    return 3;
            }
        }

        public void WriteString(FileStream fs, string s)
        {
            fs.Write(BitConverter.GetBytes((int)s.Length), 0, 4);
            fs.Write(GetBytes(s), 0, s.Length);
        }

        public string ReadString(FileStream fs)
        {
            string s = "";
            byte[] buff = new byte[4];
            for (int i = 0; i < 4; i++)
                buff[i] = (byte)fs.ReadByte();
            int count = BitConverter.ToInt32(buff, 0);
            buff = new byte[count];
            for (int i = 0; i < count; i++)
                buff[i] = (byte)fs.ReadByte();
            s = GetString(buff);
            return s;
        }

        public byte[] GetBytes(string str)
        {
            byte[] bytes = new byte[str.Length];
            for (int i = 0; i < str.Length; i++)
                bytes[i] = (byte)str[i];
            return bytes;
        }

        public string GetString(byte[] bytes)
        {
            string s = "";
            for (int i = 0; i < bytes.Length; i++)
                s += (char)bytes[i];
            return s;
        }

        public int ReadInt(FileStream fs)
        {
            int res = 0;
            byte[] buff = new byte[4];
            fs.Read(buff, 0, 4);
            res = BitConverter.ToInt32(buff, 0);
            return res;
        }

        public bool ReadBool(FileStream fs)
        {
            byte[] buff = new byte[1];
            fs.Read(buff, 0, 1);
            return buff[0] != 0;
        }

        private void loadDatabaseToolStripMenuItem_Click(object sender, EventArgs e)
        {
            OpenFileDialog d = new OpenFileDialog();
            d.Filter = "*.db|*.db";
            if (d.ShowDialog() == System.Windows.Forms.DialogResult.OK)
            {
                FileStream fs = new FileStream(d.FileName, FileMode.Open, FileAccess.Read);
                BitConverter.IsLittleEndian = true;
                PlotVarEntry p;
                entries = new List<PlotVarEntry>();
                int count = ReadInt(fs);
                for (int i = 0; i < count; i++)
                {
                    p = new PlotVarEntry();
                    p.id = ReadInt(fs);
                    p.type = ReadInt(fs);
                    p.game = ReadInt(fs);
                    p.category = ReadString(fs);
                    p.state = ReadString(fs);
                    p.broken = ReadBool(fs);
                    p.me2id = ReadInt(fs);
                    p.me3id = ReadInt(fs);
                    p.notes = ReadString(fs);
                    entries.Add(p);
                }
                fs.Close();
                RefreshTable();
                //MessageBox.Show("Done.");
            }
        }

        private void plotVarTable_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.KeyCode == Keys.Delete)
            {
                deleteCurrentRow();
            }
        }

        private void deleteCurrentRow()
        {
            if (plotVarTable.CurrentRow != null && !plotVarTable.CurrentRow.IsNewRow)
            {
                plotVarTable.Rows.RemoveAt(plotVarTable.CurrentRow.Index);
            }
        }

        private void deleteRowButton_Click(object sender, EventArgs e)
        {
            deleteCurrentRow();
        }

        private void loadDatabasePreR748ToolStripMenuItem_Click(object sender, EventArgs e)
        {

            {
                OpenFileDialog d = new OpenFileDialog();
                d.Filter = "*.db|*.db";
                if (d.ShowDialog() == System.Windows.Forms.DialogResult.OK)
                {
                    FileStream fs = new FileStream(d.FileName, FileMode.Open, FileAccess.Read);
                    BitConverter.IsLittleEndian = true;
                    PlotVarEntry p;
                    entries = new List<PlotVarEntry>();
                    int count = ReadInt(fs);
                    for (int i = 0; i < count; i++)
                    {
                        p = new PlotVarEntry();
                        p.id = ReadInt(fs);
                        p.type = ReadInt(fs);
                        p.state = ReadString(fs);
                        p.game = GAME_ME1;
                    }
                    count = ReadInt(fs);
                    for (int i = 0; i < count; i++)
                    {
                        p = new PlotVarEntry();
                        p.id = ReadInt(fs);
                        p.type = ReadInt(fs);
                        p.state = ReadString(fs);
                        p.game = GAME_ME2;
                    }
                    count = ReadInt(fs);
                    for (int i = 0; i < count; i++)
                    {
                        p = new PlotVarEntry();
                        p.id = ReadInt(fs);
                        p.type = ReadInt(fs);
                        p.state = ReadString(fs);
                        p.game = GAME_ME3;
                    }
                    fs.Close();
                    RefreshTable();
                    //MessageBox.Show("Done.");
                }
            }
        }

        private void exportToCSVToolStripMenuItem_Click(object sender, EventArgs e)
        {
            SaveFileDialog d = new SaveFileDialog();
            d.Filter = "*.csv|*.csv";
            if (d.ShowDialog() == System.Windows.Forms.DialogResult.OK)
            {
                System.IO.StreamWriter stringWriter = new System.IO.StreamWriter(d.FileName);
                var csv = new CsvWriter(stringWriter);
                csv.WriteRecords(entries);
                stringWriter.Close();
                status.Text = "Exported DB to CSV: " + d.FileName;
            }
        }

        private void importFromCSVToolStripMenuItem_Click(object sender, EventArgs e)
        {
            OpenFileDialog d = new OpenFileDialog();
            d.Filter = "*.csv|*.csv";
            if (d.ShowDialog() == System.Windows.Forms.DialogResult.OK)
            {
                commitTable();
                System.IO.StreamReader stringreader = new System.IO.StreamReader(d.FileName);
                var csv = new CsvReader(stringreader);
                var item = csv.GetRecords<PlotVarEntry>();
                List<PlotVarEntry> importingEntries = new List<PlotVarEntry>();

                while (csv.Read())
                {
                    var record = csv.GetRecord<PlotVarEntry>();
                }

                //csv.GetRecords<PlotVarEntry>().ToList();
                stringreader.Close();

                //import
                int recordsImported = 0, recordsUpdated = 0;
                foreach (PlotVarEntry pve in importingEntries)
                {
                    bool import = true;
                    foreach (PlotVarEntry ent in entries)
                    {
                        if (ent.id == pve.id && ent.game == pve.game)
                        {
                            import = false;
                            bool recordUpdated = false;
                            //same entry, merge empty values
                            //vartype
                            if (ent.type != pve.type)
                            {
                                ent.type = pve.type;
                                recordUpdated = true;
                            }

                            //broken
                            if (ent.broken != pve.broken)
                            {
                                ent.broken = pve.broken;
                                recordUpdated = true;
                            }

                            //me2
                            if (ent.me2id != pve.me2id)
                            {
                                ent.me2id = pve.me2id;
                                recordUpdated = true;
                            }

                            //me3id
                            if (ent.me3id != pve.me3id)
                            {
                                ent.me3id = pve.me3id;
                                recordUpdated = true;
                            }

                            //category
                            if (ent.category == null || ent.category.Equals(""))
                            {
                                ent.category = pve.category;
                                recordUpdated = true;
                            }

                            //state
                            if (ent.state == null || ent.state.Equals(""))
                            {
                                ent.state = pve.state;
                                recordUpdated = true;
                            }

                            //notes
                            if (ent.notes == null || ent.notes.Equals(""))
                            {
                                ent.notes = pve.notes;
                                recordUpdated = true;
                            }

                            if (recordUpdated)
                            {
                                recordsUpdated++;
                            }
                        }
                    }
                    if (import)
                    {
                        recordsImported++;
                        entries.Add(pve);
                    }
                }
                RefreshTable();
                status.Text = "Imported from CSV into DB: " + d.FileName + " | " + recordsImported + " records imported | " + recordsUpdated + " records upated";
            }
        }
    }
}