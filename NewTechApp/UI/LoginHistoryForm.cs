using System;
using System.Data;
using System.Drawing;
using System.Windows.Forms;
using NewTechApp.Data;

namespace NewTechApp.UI
{
    public partial class LoginHistoryForm : Form
    {
        TextBox txtFilter;
        DataGridView grid;

        public LoginHistoryForm()
        {
            Text = "История входа";
            Font = new Font("Bahnschrift Light SemiCondensed", 10f);
            BackColor = Color.White;
            StartPosition = FormStartPosition.CenterParent;
            Width = 900;
            Height = 520;

            // --- верхняя панель с фильтром ---
            var top = new Panel { Dock = DockStyle.Top, Height = 60, BackColor = Color.White };
            var lbl = new Label { Text = "Фильтр по логину:", Left = 12, Top = 18, Width = 150, Font = new Font("Bahnschrift Light SemiCondensed", 10f) };
            txtFilter = new TextBox { Left = 160, Top = 16, Width = 220, Font = new Font("Bahnschrift Light SemiCondensed", 10f) };
            txtFilter.TextChanged += (s, e) => ApplyFilter();
            top.Controls.Add(lbl);
            top.Controls.Add(txtFilter);
            Controls.Add(top);

            // --- таблица ---
            grid = new DataGridView
            {
                Dock = DockStyle.Fill,
                ReadOnly = true,
                AllowUserToAddRows = false,
                AllowUserToDeleteRows = false,
                AllowUserToResizeRows = false,
                RowHeadersVisible = false,
                AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill,
                BackgroundColor = Color.White,
                BorderStyle = BorderStyle.None,
                SelectionMode = DataGridViewSelectionMode.FullRowSelect,
                MultiSelect = false,
                ColumnHeadersHeight = 36,
                ColumnHeadersHeightSizeMode = DataGridViewColumnHeadersHeightSizeMode.DisableResizing,
                Font = new Font("Bahnschrift Light SemiCondensed", 10f)
            };
            grid.EnableHeadersVisualStyles = false;
            grid.ColumnHeadersDefaultCellStyle.BackColor = ColorTranslator.FromHtml("#BBDCFA");
            grid.ColumnHeadersDefaultCellStyle.ForeColor = Color.Black;
            grid.ColumnHeadersDefaultCellStyle.Alignment = DataGridViewContentAlignment.MiddleLeft;
            grid.ColumnHeadersDefaultCellStyle.Font = new Font("Bahnschrift Light SemiCondensed", 10f, FontStyle.Bold);
            Controls.Add(grid);
            Load += LoginHistoryForm_Load;
        }

        void LoginHistoryForm_Load(object sender, EventArgs e)
        {
            var dt = Db.Table(
                "SELECT EntryID, AttemptAt, Login, Success, Reason " +
                "FROM dbo.LoginHistory ORDER BY AttemptAt DESC");
            grid.DataSource = dt;

            // читаемые заголовки и размеры
            if (grid.Columns["EntryID"] != null)
            {
                grid.Columns["EntryID"].HeaderText = "ID записи";
                grid.Columns["EntryID"].FillWeight = 10;
                grid.Columns["EntryID"].DefaultCellStyle.Alignment = DataGridViewContentAlignment.MiddleCenter;
            }
            if (grid.Columns["AttemptAt"] != null)
            {
                grid.Columns["AttemptAt"].HeaderText = "Дата и время";
                grid.Columns["AttemptAt"].FillWeight = 22;
                grid.Columns["AttemptAt"].DefaultCellStyle.Format = "dd.MM.yyyy HH:mm";
            }
            if (grid.Columns["Login"] != null)
            {
                grid.Columns["Login"].HeaderText = "Логин";
                grid.Columns["Login"].FillWeight = 18;
            }
            if (grid.Columns["Success"] != null)
            {
                grid.Columns["Success"].HeaderText = "Успешно";
                grid.Columns["Success"].FillWeight = 10;
                grid.Columns["Success"].DefaultCellStyle.Alignment = DataGridViewContentAlignment.MiddleCenter;
            }
            if (grid.Columns["Reason"] != null)
            {
                grid.Columns["Reason"].HeaderText = "Причина";
                grid.Columns["Reason"].FillWeight = 40;
            }
        }

        void ApplyFilter()
        {
            var dt = grid.DataSource as DataTable;
            if (dt == null) return;
            var dv = dt.DefaultView;
            var q = txtFilter.Text.Replace("'", "''");
            dv.RowFilter = string.IsNullOrWhiteSpace(q) ? "" : "Login LIKE '%" + q + "%'";
            dv.Sort = "AttemptAt DESC";
        }
    }
}