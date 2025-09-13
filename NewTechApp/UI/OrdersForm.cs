using System;
using System.Data;
using System.Data.SqlClient;
using System.Drawing;
using System.Windows.Forms;
using NewTechApp.Data;

namespace NewTechApp.UI
{
    public partial class OrdersForm : Form
    {
        private readonly bool _isAdmin;
        private TextBox txtSearch;
        private ComboBox cmbSort;
        private Button btnAdd;
        private FlowLayoutPanel flw;
        private string _currentSort = "PartnerName ASC";
        private DataTable _dt;

        public OrdersForm(string role)
        {
            _isAdmin = string.Equals(role, "Admin", StringComparison.OrdinalIgnoreCase);

            Text = "NewTech — заказы";
            Font = new Font("Bahnschrift Light SemiCondensed", 10f);
            BackColor = Color.White;
            StartPosition = FormStartPosition.CenterParent;
            Width = 1100; Height = 700;

            var layout = new TableLayoutPanel { Dock = DockStyle.Fill, ColumnCount = 1, RowCount = 2 };
            layout.RowStyles.Add(new RowStyle(SizeType.Absolute, 80));
            layout.RowStyles.Add(new RowStyle(SizeType.Percent, 100));
            Controls.Add(layout);

            var top = new Panel { Dock = DockStyle.Fill, BackColor = Color.White };
            layout.Controls.Add(top, 0, 0);

            var lblSearch = new Label { Text = "Поиск:", Left = 10, Top = 18, Width = 60, Font = new Font("Bahnschrift Light SemiCondensed", 10f) };
            txtSearch = new TextBox { Left = 70, Top = 16, Width = 280, Font = new Font("Bahnschrift Light SemiCondensed", 10f) };
            txtSearch.TextChanged += (s, e) => ApplyFilterAndRender();

            var lblSort = new Label { Text = "Сортировка по партнёру:", Left = 370, Top = 18, Width = 180, Font = new Font("Bahnschrift Light SemiCondensed", 10f) };
            cmbSort = new ComboBox { Left = 550, Top = 16, Width = 180, DropDownStyle = ComboBoxStyle.DropDownList, Font = new Font("Bahnschrift Light SemiCondensed", 10f) };
            cmbSort.Items.AddRange(new object[] { "По возрастанию", "По убыванию" });
            cmbSort.SelectedIndex = 0;
            cmbSort.SelectedIndexChanged += (s, e) =>
            {
                _currentSort = cmbSort.SelectedIndex == 0 ? "PartnerName ASC" : "PartnerName DESC";
                ApplyFilterAndRender();
            };

            btnAdd = new Button
            {
                Text = "Добавить заказ",
                Left = 750,
                Top = 16,
                Width = 120,
                Height = 25,
                BackColor = ColorTranslator.FromHtml("#0C4882"),
                ForeColor = Color.White,
                Font = new Font("Bahnschrift Light SemiCondensed", 10f, FontStyle.Bold),
                Visible = _isAdmin
            };
            btnAdd.Click += (s, e) =>
            {
                using (var f = new AddEditOrderForm(null))
                {
                    if (f.ShowDialog(this) == DialogResult.OK) LoadDataAndRender();
                }
            };

            top.Controls.Add(lblSearch);
            top.Controls.Add(txtSearch);
            top.Controls.Add(lblSort);
            top.Controls.Add(cmbSort);
            top.Controls.Add(btnAdd);

            flw = new FlowLayoutPanel
            {
                Dock = DockStyle.Fill,
                AutoScroll = true,
                FlowDirection = FlowDirection.TopDown,
                WrapContents = false,
                Padding = new Padding(10),
                Margin = new Padding(0, 20, 0, 0),
                BackColor = Color.White
            };
            layout.Controls.Add(flw, 0, 1);

            Load += OrdersForm_Load;
        }

        private void OrdersForm_Load(object sender, EventArgs e)
        {
            LoadDataAndRender();
        }

        private void LoadDataAndRender()
        {
            // УПРОЩЕННЫЙ ЗАПРОС БЕЗ StatusID
            string sql = @"
SELECT 
    o.OrderID,
    o.Article,
    o.Status,  -- Используем текстовый статус из Orders
    o.OrderDate,
    o.TotalAmount,
    p.PartnerName,
    o.PartnerID
FROM dbo.Orders o
LEFT JOIN dbo.Partners p ON p.PartnerID = o.PartnerID";

            _dt = Db.Table(sql);
            ApplyFilterAndRender();
        }

        private void ApplyFilterAndRender()
        {
            if (_dt == null) return;
            var dv = _dt.DefaultView;

            // УПРОЩЕННЫЙ ФИЛЬТР БЕЗ StatusID
            if (!string.IsNullOrWhiteSpace(txtSearch.Text))
            {
                var q = txtSearch.Text.Replace("'", "''");
                var expr =
                    "Article LIKE '%" + q + "%' OR " +
                    "Status LIKE '%" + q + "%' OR " +  // Фильтруем по текстовому статусу
                    "PartnerName LIKE '%" + q + "%'";
                dv.RowFilter = expr;
            }
            else dv.RowFilter = "";

            dv.Sort = _currentSort;
            RenderCards(dv);
        }

        private void RenderCards(DataView dv)
        {
            flw.SuspendLayout();
            try
            {
                flw.Controls.Clear();
                foreach (DataRowView v in dv)
                {
                    var r = v.Row;
                    int id = Convert.ToInt32(r["OrderID"]);
                    string article = Convert.ToString(r["Article"]);
                    string status = Convert.ToString(r["Status"]);  // Берем текст статуса
                    string partner = Convert.ToString(r["PartnerName"]);
                    DateTime date = Convert.ToDateTime(r["OrderDate"]);
                    decimal cost = r["TotalAmount"] == DBNull.Value ? 0 : Convert.ToDecimal(r["TotalAmount"]);
                    var card = BuildCard(id, article, status, partner, date, cost);
                    flw.Controls.Add(card);
                }
            }
            finally { flw.ResumeLayout(); }
        }

        private Panel BuildCard(int id, string article, string status, string partner, DateTime date, decimal cost)
        {
            var root = new Panel
            {
                Width = Math.Max(860, flw.ClientSize.Width - 30),
                Height = 110,
                Margin = new Padding(8),
                BackColor = Color.White,
                BorderStyle = BorderStyle.FixedSingle,
                Tag = id
            };

            var left = new Panel { Left = 10, Top = 10, Width = root.Width - 180, Height = 90, Anchor = AnchorStyles.Left | AnchorStyles.Right | AnchorStyles.Top };
            root.Controls.Add(left);

            var title = new Label
            {
                Text = "Артикул заказа: " + article,
                Font = new Font("Bahnschrift Light SemiCondensed", 10f, FontStyle.Bold),
                Left = 8,
                Top = 2,
                Width = left.Width - 16,
                Height = 24
            };
            left.Controls.Add(title);

            var l1 = new Label { Text = "Статус заказа: " + status, Left = 8, Top = 28, Width = left.Width - 16, Height = 20, Font = new Font("Bahnschrift Light SemiCondensed", 9f) };
            var l2 = new Label { Text = "Партнёр: " + partner, Left = 8, Top = 48, Width = left.Width - 16, Height = 20, Font = new Font("Bahnschrift Light SemiCondensed", 9f) };
            var l3 = new Label { Text = "Стоимость: " + cost.ToString("N2") + " р", Left = 8, Top = 68, Width = left.Width - 16, Height = 20, Font = new Font("Bahnschrift Light SemiCondensed", 9f) };

            left.Controls.Add(l1);
            left.Controls.Add(l2);
            left.Controls.Add(l3);

            var right = new Panel { Left = root.Width - 160, Top = 10, Width = 140, Height = 90, BorderStyle = BorderStyle.FixedSingle, Anchor = AnchorStyles.Top | AnchorStyles.Right };
            var d = new Label { Text = "Дата заказа", Left = 6, Top = 6, Width = 128, Height = 24, TextAlign = ContentAlignment.MiddleCenter, Font = new Font("Bahnschrift Light SemiCondensed", 8f) };
            var dv = new Label { Text = date.ToString("dd.MM.yyyy"), Left = 6, Top = 36, Width = 128, Height = 40, TextAlign = ContentAlignment.MiddleCenter, Font = new Font("Bahnschrift Light SemiCondensed", 12f, FontStyle.Bold) };

            right.Controls.Add(d);
            right.Controls.Add(dv);
            root.Controls.Add(right);

            root.DoubleClick += (s, e) =>
            {
                if (!_isAdmin)
                {
                    MessageBox.Show("Редактировать заказы может только администратор.", "Доступ запрещён", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    return;
                }
                using (var f = new AddEditOrderForm(id))
                {
                    if (f.ShowDialog(this) == DialogResult.OK) LoadDataAndRender();
                }
            };

            var menu = new ContextMenuStrip();
            if (_isAdmin)
            {
                menu.Items.Add("Изменить", null, (s, e) =>
                {
                    using (var f = new AddEditOrderForm(id))
                    {
                        if (f.ShowDialog(this) == DialogResult.OK) LoadDataAndRender();
                    }
                });
                menu.Items.Add("Удалить", null, (s, e) =>
                {
                    if (MessageBox.Show("Удалить заказ №" + id + "?", "Подтверждение",
                        MessageBoxButtons.YesNo, MessageBoxIcon.Warning) != DialogResult.Yes) return;
                    try
                    {
                        Db.Exec("DELETE FROM dbo.Orders WHERE OrderID=@id", new SqlParameter("@id", id));
                        MessageBox.Show("Заказ удалён.", "Информация", MessageBoxButtons.OK, MessageBoxIcon.Information);
                        LoadDataAndRender();
                    }
                    catch (Exception ex)
                    {
                        MessageBox.Show("Не удалось удалить заказ. Причина: " + ex.Message,
                            "Ошибка", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    }
                });
            }
            else
            {
                menu.Items.Add("Просмотр (только чтение)", null, (s, e) => { });
            }
            root.ContextMenuStrip = menu;

            return root;
        }
    }
}