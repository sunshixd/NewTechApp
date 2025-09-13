using System;
using System.Collections.Generic;
using System.Data;
using System.Data.SqlClient;
using System.Drawing;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;
using NewTechApp.Data;

namespace NewTechApp.UI
{
    public partial class MainForm : Form
    {
        // Таблицы / колонки
        const string T_PRODUCTS = "dbo.Products";
        const string COL_ID = "ProductID";

        // данные пользователя
        readonly string _login, _fullName, _role, _photoUrl;

        // права
        bool _canEdit = false;

        // UI состояние
        string _currentSort = "StockQty DESC";
        bool _isReady = false;
        Image _phImg;
        Dictionary<int, Image> _imgCache = new Dictionary<int, Image>();
        CancellationTokenSource _imgCts;

        public MainForm(string login, string fullName, string role, string photoUrl)
        {
            InitializeComponent();

            _login = login; _fullName = fullName; _role = role; _photoUrl = photoUrl;
            _canEdit = string.Equals(_role, "Admin", StringComparison.OrdinalIgnoreCase)
                    || string.Equals(_role, "Manager", StringComparison.OrdinalIgnoreCase);
        }

        private void MainForm_Load(object sender, EventArgs e)
        {
            // Настройка формы
            Text = "NewTech — список товаров";
            Font = new Font("Bahnschrift Light SemiCondensed", 10f);
            BackColor = Color.White;
            StartPosition = FormStartPosition.CenterScreen;

            // Настройка кнопок по роли
            btnOrders.Visible = _canEdit;
            btnAdd.Visible = _canEdit;

            // Правильные цвета кнопок
            btnOrders.BackColor = ColorTranslator.FromHtml("#0C4882");
            btnOrders.ForeColor = Color.White;
            btnOrders.Font = new Font("Bahnschrift Light SemiCondensed", 10f, FontStyle.Bold);

            btnAdd.BackColor = ColorTranslator.FromHtml("#0C4882");
            btnAdd.ForeColor = Color.White;
            btnAdd.Font = new Font("Bahnschrift Light SemiCondensed", 10f, FontStyle.Bold);

            btnHistory.BackColor = ColorTranslator.FromHtml("#BBDCFA");
            btnHistory.ForeColor = Color.Black;
            btnHistory.Font = new Font("Bahnschrift Light SemiCondensed", 10f);

            btnLogout.BackColor = ColorTranslator.FromHtml("#BBDCFA");
            btnLogout.ForeColor = Color.Black;
            btnLogout.Font = new Font("Bahnschrift Light SemiCondensed", 10f);

            try
            {
                if (!string.IsNullOrEmpty(_photoUrl) && System.IO.File.Exists(_photoUrl))
                    picUser.Image = Image.FromFile(_photoUrl);
                else
                    picUser.Image = MakePlaceholder("User");
            }
            catch { }

            lblUser.Text = _fullName + " — " + _role;
            lblUser.Font = new Font("Bahnschrift Light SemiCondensed", 10f);

            _phImg = MakePlaceholder("img");
            LoadSuppliers();
            LoadProducts();
            _isReady = true;
            ApplyFilters();
            UpdateDiscountColumn();
            RenderCards();
        }

        Image MakePlaceholder(string text)
        {
            var bmp = new Bitmap(96, 96);
            using (var g = Graphics.FromImage(bmp))
            using (var b = new SolidBrush(Color.LightGray))
            using (var p = new Pen(Color.DarkGray))
            using (var f = new Font("Bahnschrift Light SemiCondensed", 10, FontStyle.Bold))
            {
                g.FillRectangle(b, 0, 0, bmp.Width, bmp.Height);
                g.DrawRectangle(p, 0, 0, bmp.Width - 1, bmp.Height - 1);
                var sz = g.MeasureString(text, f);
                g.DrawString(text, f, Brushes.Black, (bmp.Width - sz.Width) / 2, (bmp.Height - sz.Height) / 2);
            }
            return bmp;
        }

        Image LoadImageNoLock(string path)
        {
            try
            {
                using (var fs = new System.IO.FileStream(path, System.IO.FileMode.Open, System.IO.FileAccess.Read, System.IO.FileShare.ReadWrite))
                using (var ms = new System.IO.MemoryStream())
                {
                    fs.CopyTo(ms);
                    return Image.FromStream(new System.IO.MemoryStream(ms.ToArray()));
                }
            }
            catch { return null; }
        }

        void LoadSuppliers()
        {
            var dt = Db.Table("SELECT SupplierID, SupplierName FROM dbo.Suppliers ORDER BY SupplierName");
            var ds = dt.Clone();
            ds.Rows.Add(DBNull.Value, "Все поставщики");
            foreach (DataRow r in dt.Rows) ds.ImportRow(r);
            cmbSuppliers.DisplayMember = "SupplierName";
            cmbSuppliers.ValueMember = "SupplierID";
            cmbSuppliers.DataSource = ds;
            cmbSuppliers.SelectedIndex = 0;
            cmbSuppliers.Font = new Font("Bahnschrift Light SemiCondensed", 10f);
        }

        void LoadProducts()
        {
            var sql = @"
SELECT p.ProductID, p.ProductName, p.Article, p.MinPartnerPrice, 
       NULL AS PromoPrice, -- Временная заглушка
       s.SupplierName,
       ISNULL(m.StockQty, 0) AS StockQty,
       p.ImageUrl
FROM dbo.Products p
LEFT JOIN dbo.Suppliers s ON s.SupplierID = p.SupplierID
LEFT JOIN (
    SELECT pm.ProductID, SUM(pm.RequiredQty) AS StockQty
    FROM dbo.ProductMaterials pm
    GROUP BY pm.ProductID
) m ON m.ProductID = p.ProductID
";
            var dt = Db.Table(sql);
            if (!dt.Columns.Contains("ActiveDiscount")) dt.Columns.Add("ActiveDiscount", typeof(string));
            if (!dt.Columns.Contains("FinalPrice")) dt.Columns.Add("FinalPrice", typeof(decimal));
            grid.DataSource = dt;
            if (grid.Columns.Contains("ImageUrl")) grid.Columns["ImageUrl"].Visible = false;
            _currentSort = "StockQty DESC";
            dt.DefaultView.Sort = _currentSort;
            SetColumnsLook();
        }

        void SetColumnsLook()
        {
            grid.Font = new Font("Bahnschrift Light SemiCondensed", 10f);

            if (grid.Columns.Contains("ProductID"))
            {
                grid.Columns["ProductID"].HeaderText = "ID";
                grid.Columns["ProductID"].FillWeight = 70;
                grid.Columns["ProductID"].DefaultCellStyle.Alignment = DataGridViewContentAlignment.MiddleCenter;
            }
            if (grid.Columns.Contains("ProductName"))
            {
                grid.Columns["ProductName"].HeaderText = "Товар";
                grid.Columns["ProductName"].FillWeight = 420;
                grid.Columns["ProductName"].DefaultCellStyle.Padding = new Padding(6, 0, 0, 0);
            }
            if (grid.Columns.Contains("Article"))
            {
                grid.Columns["Article"].HeaderText = "Артикул";
                grid.Columns["Article"].FillWeight = 100;
            }
            if (grid.Columns.Contains("SupplierName"))
            {
                grid.Columns["SupplierName"].HeaderText = "Поставщик";
                grid.Columns["SupplierName"].FillWeight = 120;
            }
            if (grid.Columns.Contains("StockQty"))
            {
                grid.Columns["StockQty"].HeaderText = "Остаток";
                grid.Columns["StockQty"].DefaultCellStyle.Format = "N0";
                grid.Columns["StockQty"].FillWeight = 90;
            }
            if (grid.Columns.Contains("MinPartnerPrice"))
            {
                grid.Columns["MinPartnerPrice"].HeaderText = "Мин. цена";
                grid.Columns["MinPartnerPrice"].DefaultCellStyle.Format = "N2";
            }
            if (grid.Columns.Contains("PromoPrice"))
            {
                grid.Columns["PromoPrice"].HeaderText = "Акц. цена";
                grid.Columns["PromoPrice"].DefaultCellStyle.Format = "N2";
            }
            if (grid.Columns.Contains("FinalPrice"))
            {
                grid.Columns["FinalPrice"].HeaderText = "Итоговая цена";
                grid.Columns["FinalPrice"].DefaultCellStyle.Format = "N2";
                grid.Columns["FinalPrice"].FillWeight = 120;
            }
            if (grid.Columns.Contains("ActiveDiscount"))
                grid.Columns["ActiveDiscount"].HeaderText = "Скидка";
        }

        void ApplyFilters()
        {
            if (!_isReady || grid.DataSource == null) return;
            var dt = grid.DataSource as DataTable;
            if (dt == null) return;
            var dv = dt.DefaultView;
            var list = new List<string>();
            if (!string.IsNullOrWhiteSpace(txtSearch.Text))
            {
                var q = txtSearch.Text.Replace("'", "''");
                list.Add("(ProductName LIKE '%" + q + "%' OR Article LIKE '%" + q + "%' OR SupplierName LIKE '%" + q + "%')");
            }
            if (cmbSuppliers.SelectedValue != null && cmbSuppliers.SelectedValue != DBNull.Value)
            {
                var name = cmbSuppliers.Text.Replace("'", "''");
                list.Add("SupplierName = '" + name + "'");
            }
            dv.RowFilter = string.Join(" AND ", list.ToArray());
            dv.Sort = _currentSort;
        }

        decimal CalcDiscount(decimal vol)
        {
            if (vol <= 10000) return 0m;
            if (vol <= 50000) return 0.05m;
            if (vol <= 300000) return 0.10m;
            if (vol <= 1000000) return 0.15m;
            return 0.20m;
        }

        void UpdateDiscountColumn()
        {
            if (!_isReady || grid.DataSource == null) return;
            var dt = grid.DataSource as DataTable;
            if (dt == null) return;
            var d = CalcDiscount((decimal)numVolume.Value);
            foreach (DataRow r in dt.Rows)
            {
                r["ActiveDiscount"] = string.Format("{0:P0}", d);
                var basePrice = r["PromoPrice"] != DBNull.Value ? Convert.ToDecimal(r["PromoPrice"])
                                                                : Convert.ToDecimal(r["MinPartnerPrice"]);
                r["FinalPrice"] = Math.Round(basePrice * (1 - d), 2);
            }
        }

        void RenderCards()
        {
            if (grid.DataSource == null || flw == null) return;
            var dt = grid.DataSource as DataTable;
            var dv = dt.DefaultView;
            _imgCts?.Cancel();
            _imgCts = new CancellationTokenSource();
            flw.SuspendLayout();
            try
            {
                flw.Controls.Clear();
                foreach (DataRowView v in dv)
                {
                    var r = v.Row;
                    int id = Convert.ToInt32(r["ProductID"]);
                    string name = Convert.ToString(r["ProductName"]);
                    string article = Convert.ToString(r["Article"]);
                    string supplier = Convert.ToString(r["SupplierName"]);
                    decimal stock = Convert.ToDecimal(r["StockQty"]);
                    decimal minPrice = r["MinPartnerPrice"] == DBNull.Value ? 0 : Convert.ToDecimal(r["MinPartnerPrice"]);
                    decimal promo = r["PromoPrice"] == DBNull.Value ? 0 : Convert.ToDecimal(r["PromoPrice"]);
                    string imgPath = Convert.ToString(r["ImageUrl"]);
                    var card = BuildCardControl(id, name, article, supplier, stock, minPrice, promo);
                    flw.Controls.Add(card.root);
                    _ = LoadCardImageAsync(card.pic, id, imgPath, _imgCts.Token);
                }
            }
            finally { flw.ResumeLayout(); }
        }

        async Task LoadCardImageAsync(PictureBox pic, int id, string path, CancellationToken token)
        {
            if (token.IsCancellationRequested) return;
            if (_imgCache.TryGetValue(id, out var cached))
            {
                if (!pic.IsDisposed) pic.BeginInvoke((Action)(() => pic.Image = cached));
                return;
            }
            Image img = null;
            if (!string.IsNullOrEmpty(path) && System.IO.File.Exists(path))
            {
                try { img = await Task.Run(() => LoadImageNoLock(path), token); } catch { img = null; }
            }
            if (token.IsCancellationRequested) return;
            var ready = img ?? _phImg;
            _imgCache[id] = ready;
            if (!pic.IsDisposed) pic.BeginInvoke((Action)(() => pic.Image = ready));
        }

        (Panel root, PictureBox pic) BuildCardControl(int id, string name, string article, string supplier,
                                                      decimal stock, decimal minPrice, decimal promo)
        {
            var root = new Panel { Width = Math.Max(860, flw.ClientSize.Width - 30), Height = 110, Margin = new Padding(8), BackColor = Color.White, BorderStyle = BorderStyle.FixedSingle };
            var picBox = new PictureBox { Left = 10, Top = 10, Width = 90, Height = 90, SizeMode = PictureBoxSizeMode.Zoom, Image = _phImg };
            root.Controls.Add(picBox);
            var center = new Panel { Left = 110, Top = 10, Width = root.Width - 110 - 150 - 20, Height = 90, Anchor = AnchorStyles.Left | AnchorStyles.Right | AnchorStyles.Top };
            root.Controls.Add(center);
            var lblTitle = new Label { Left = 8, Top = 0, Width = center.Width - 8, Height = 24, Font = new Font("Bahnschrift Light SemiCondensed", 10f, FontStyle.Bold), Text = $"Товар | {name}" };
            center.Controls.Add(lblTitle);
            var lblLine1 = new Label { Left = 8, Top = 24, Width = center.Width - 8, Height = 20, Text = $"Артикул: {article}   Поставщик: {supplier}", Font = new Font("Bahnschrift Light SemiCondensed", 9f) };
            center.Controls.Add(lblLine1);
            var lblLine2 = new Label { Left = 8, Top = 44, Width = center.Width - 8, Height = 20, Text = $"Остаток: {stock:N0}", Font = new Font("Bahnschrift Light SemiCondensed", 9f) };
            center.Controls.Add(lblLine2);
            var lblLine3 = new Label { Left = 8, Top = 64, Width = center.Width - 8, Height = 20, Font = new Font("Bahnschrift Light SemiCondensed", 9f) };
            if (promo > 0) lblLine3.Text = $"Мин. цена: {minPrice:N2} р   Акц. цена: {promo:N2} р";
            else lblLine3.Text = $"Мин. цена: {minPrice:N2} р";
            center.Controls.Add(lblLine3);
            var lblBatch = new Label { Left = center.Width - 250, Top = 0, Width = 240, Height = 24, Anchor = AnchorStyles.Top | AnchorStyles.Right, TextAlign = ContentAlignment.MiddleRight, Font = new Font("Bahnschrift Light SemiCondensed", 9f) };
            center.Controls.Add(lblBatch);
            var right = new Panel { Left = root.Width - 150 - 10, Top = 10, Width = 140, Height = 90, BorderStyle = BorderStyle.FixedSingle, Anchor = AnchorStyles.Top | AnchorStyles.Right };
            var lblHdr = new Label { Left = 6, Top = 6, Width = 128, Height = 36, Text = "Действующая\nскидка", TextAlign = ContentAlignment.MiddleCenter, Font = new Font("Bahnschrift Light SemiCondensed", 8f) };
            var lblDisc = new Label { Left = 6, Top = 52, Width = 128, Height = 32, TextAlign = ContentAlignment.MiddleCenter, Font = new Font("Bahnschrift Light SemiCondensed", 12f, FontStyle.Bold) };
            right.Controls.Add(lblHdr); right.Controls.Add(lblDisc);
            root.Controls.Add(right);
            if (stock <= 0) root.BackColor = Color.MistyRose;
            ApplyCardDiscount(root, lblDisc, lblBatch, minPrice, promo);
            if (_canEdit)
            {
                var menu = new ContextMenuStrip();
                menu.Items.Add("Редактировать", null, (s, e) => EditProductById(id));
                menu.Items.Add("Изменить акционную цену", null, (s, e) =>
                {
                    string sVal = Prompt.Show("Новая акционная цена:", "Изменить цену");
                    decimal v;
                    if (decimal.TryParse(sVal, out v))
                    {
                        try
                        {
                            Cursor.Current = Cursors.WaitCursor;
                            Db.Exec($"UPDATE {T_PRODUCTS} SET PromoPrice=@p WHERE {COL_ID}=@id",
                                    new SqlParameter("@p", v), new SqlParameter("@id", id));
                            ReloadAndReapply();
                        }
                        catch (Exception ex) { MessageBox.Show("Не удалось сохранить: " + ex.Message, "Ошибка", MessageBoxButtons.OK, MessageBoxIcon.Error); }
                        finally { Cursor.Current = Cursors.Default; }
                    }
                });
                menu.Items.Add("Удалить товар", null, (s, e) => DeleteProductById(id));
                root.ContextMenuStrip = menu;
                root.DoubleClick += (s, e) => EditProductById(id);
            }
            return (root, picBox);
        }

        void ApplyCardDiscount(Panel root, Label lblDisc, Label lblBatch, decimal minPrice, decimal promo)
        {
            var d = CalcDiscount((decimal)numVolume.Value);
            lblDisc.Text = $"{Math.Round(d * 100m)}%";
            decimal baseForFinal = promo > 0 ? promo : minPrice;
            decimal final = Math.Round(baseForFinal * (1 - d), 2);
            lblBatch.Text = $"Стоимость партии: {final:N2} р";
            Color bg = d == 0m ? Color.White :
                       d <= 0.05m ? Color.FromArgb(235, 250, 235) :
                       d <= 0.10m ? Color.FromArgb(215, 245, 215) :
                       d <= 0.15m ? Color.FromArgb(195, 240, 195) :
                                    Color.FromArgb(255, 230, 200);
            root.BackColor = bg;
        }

        void Grid_CellFormatting(object sender, DataGridViewCellFormattingEventArgs e)
        {
            if (grid.Columns[e.ColumnIndex].Name == "StockQty" && e.Value != null)
            {
                decimal stock;
                if (decimal.TryParse(e.Value.ToString(), out stock) && stock <= 0)
                    grid.Rows[e.RowIndex].DefaultCellStyle.BackColor = Color.MistyRose;
            }
            if (grid.Columns[e.ColumnIndex].Name == "MinPartnerPrice" &&
                grid.Rows[e.RowIndex].Cells["PromoPrice"].Value != DBNull.Value)
            {
                grid.Rows[e.RowIndex].Cells["MinPartnerPrice"].Style.ForeColor = Color.Red;
                grid.Rows[e.RowIndex].Cells["MinPartnerPrice"].Style.Font = new Font(grid.Font, FontStyle.Strikeout);
            }
            if (grid.Columns.Contains("ActiveDiscount"))
            {
                var text = Convert.ToString(grid.Rows[e.RowIndex].Cells["ActiveDiscount"].Value);
                int d;
                if (!string.IsNullOrEmpty(text) && text.EndsWith("%") && int.TryParse(text.TrimEnd('%'), out d))
                {
                    var st = grid.Rows[e.RowIndex].DefaultCellStyle;
                    if (d == 0) st.BackColor = Color.White;
                    else if (d <= 5) st.BackColor = Color.FromArgb(235, 250, 235);
                    else if (d <= 10) st.BackColor = Color.FromArgb(215, 245, 215);
                    else if (d <= 15) st.BackColor = Color.FromArgb(195, 240, 195);
                    else st.BackColor = Color.FromArgb(255, 230, 200);
                }
            }
        }

        void Grid_ColumnHeaderMouseClick(object sender, DataGridViewCellMouseEventArgs e)
        {
            var name = grid.Columns[e.ColumnIndex].Name;
            if (string.IsNullOrEmpty(_currentSort) || !_currentSort.StartsWith(name))
                _currentSort = name + " ASC";
            else
                _currentSort = _currentSort.EndsWith("ASC") ? name + " DESC" : name + " ASC";
            var dt = grid.DataSource as DataTable;
            if (dt != null) dt.DefaultView.Sort = _currentSort;
            RenderCards();
        }

        void EditPromo()
        {
            if (!_canEdit) { MessageBox.Show("Недостаточно прав.", "Достав запрещён", MessageBoxButtons.OK, MessageBoxIcon.Warning); return; }
            if (grid.CurrentRow == null) return;
            int id = Convert.ToInt32(grid.CurrentRow.Cells["ProductID"].Value);
            string s = Prompt.Show("Новая акционная цена:", "Изменить цену");
            decimal v;
            if (decimal.TryParse(s, out v))
            {
                try
                {
                    grid.Enabled = false; Cursor.Current = Cursors.WaitCursor;
                    Db.Exec($"UPDATE {T_PRODUCTS} SET PromoPrice=@p WHERE {COL_ID}=@id",
                        new SqlParameter("@p", v), new SqlParameter("@id", id));
                }
                catch (Exception ex)
                {
                    MessageBox.Show("Не удалось сохранить: " + ex.Message, "Ошибка", MessageBoxButtons.OK, MessageBoxIcon.Error);
                }
                finally { Cursor.Current = Cursors.Default; grid.Enabled = true; }
                ReloadAndReapply();
            }
        }

        void DeleteProduct()
        {
            if (!_canEdit) { MessageBox.Show("Недостаточно прав.", "Доступ запрещён", MessageBoxButtons.OK, MessageBoxIcon.Warning); return; }
            if (grid.CurrentRow == null) return;
            int id = Convert.ToInt32(grid.CurrentRow.Cells["ProductID"].Value);
            DeleteProductById(id);
        }

        void DeleteProductById(int id)
        {
            if (MessageBox.Show("Удалить товар?", "Подтверждение",
                MessageBoxButtons.YesNo, MessageBoxIcon.Question) != DialogResult.Yes) return;
            if (IsProductInOrders(id))
            {
                MessageBox.Show("Удаление невозможно: товар присутствует в заказах.", "Ошибка",
                    MessageBoxButtons.OK, MessageBoxIcon.Error);
                return;
            }
            try
            {
                grid.Enabled = false; Cursor.Current = Cursors.WaitCursor;
                Db.Exec(@"
IF OBJECT_ID('dbo.ProductMaterials') IS NOT NULL
    DELETE FROM dbo.ProductMaterials WHERE ProductID=@id;
DELETE FROM " + T_PRODUCTS + " WHERE " + COL_ID + @"=@id;",
                    new SqlParameter("@id", id));
            }
            catch (Exception ex)
            {
                MessageBox.Show("Не удалось удалить: " + ex.Message, "Ошибка", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
            finally
            {
                Cursor.Current = Cursors.Default; grid.Enabled = true;
            }
            ReloadAndReapply();
        }

        bool IsProductInOrders(int productId)
        {
            var t = Db.Table("SELECT OBJECT_ID('dbo.OrderItems') AS ObjId");
            bool tableExists = t.Rows.Count > 0 && t.Rows[0]["ObjId"] != DBNull.Value;
            if (!tableExists) return false;
            var dt = Db.Table("SELECT TOP 1 1 FROM dbo.OrderItems WHERE ProductID=@id",
                new SqlParameter("@id", productId));
            return dt.Rows.Count > 0;
        }

        void AddProduct()
        {
            if (!_canEdit) { MessageBox.Show("Недостаточно прав.", "Доступ запрещён", MessageBoxButtons.OK, MessageBoxIcon.Warning); return; }
            if (Application.OpenForms["ProductForm"] != null)
            {
                MessageBox.Show("Окно редактирования уже открыто.", "Информация", MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }
            using (var form = new ProductForm(null))
            {
                form.Name = "ProductForm";
                if (form.ShowDialog(this) == DialogResult.OK)
                    ReloadAndReapply();
            }
        }

        void EditProductById(int id)
        {
            if (!_canEdit) { MessageBox.Show("Недостаточно прав.", "Доступ запрещён", MessageBoxButtons.OK, MessageBoxIcon.Warning); return; }
            if (Application.OpenForms["ProductForm"] != null)
            {
                MessageBox.Show("Окно редактирования уже открыто.", "Информация", MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }
            using (var form = new ProductForm(id))
            {
                form.Name = "ProductForm";
                if (form.ShowDialog(this) == DialogResult.OK)
                    ReloadAndReapply();
            }
        }

        void ReloadAndReapply()
        {
            grid.Enabled = false;
            Cursor.Current = Cursors.WaitCursor;
            try
            {
                grid.SuspendLayout();
                LoadProducts();
                ApplyFilters();
                UpdateDiscountColumn();
                RenderCards();
            }
            finally
            {
                grid.ResumeLayout();
                grid.Enabled = true;
                Cursor.Current = Cursors.Default;
            }
        }

        protected override void OnHandleCreated(EventArgs e)
        {
            base.OnHandleCreated(e);
            try
            {
                typeof(DataGridView).InvokeMember("DoubleBuffered",
                    System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.SetProperty,
                    null, grid, new object[] { true });
            }
            catch { }
        }

        private static class Prompt
        {
            public static string Show(string text, string caption, string defaultValue = "")
            {
                using (var form = new Form())
                using (var lbl = new Label())
                using (var txt = new TextBox())
                using (var ok = new Button())
                using (var cancel = new Button())
                {
                    form.Text = caption;
                    form.FormBorderStyle = FormBorderStyle.FixedDialog;
                    form.StartPosition = FormStartPosition.CenterParent;
                    form.ClientSize = new Size(420, 140);
                    form.MinimizeBox = false; form.MaximizeBox = false;
                    form.ShowIcon = false; form.ShowInTaskbar = false;
                    lbl.Text = text; lbl.SetBounds(12, 12, 394, 24);
                    lbl.Font = new Font("Bahnschrift Light SemiCondensed", 10f);
                    txt.Text = defaultValue; txt.SetBounds(12, 40, 394, 28);
                    txt.Font = new Font("Bahnschrift Light SemiCondensed", 10f);
                    ok.Text = "OK"; ok.DialogResult = DialogResult.OK; ok.SetBounds(230, 80, 80, 28);
                    ok.BackColor = ColorTranslator.FromHtml("#0C4882"); ok.ForeColor = Color.White;
                    ok.Font = new Font("Bahnschrift Light SemiCondensed", 10f);
                    cancel.Text = "Отмена"; cancel.DialogResult = DialogResult.Cancel; cancel.SetBounds(326, 80, 80, 28);
                    cancel.BackColor = ColorTranslator.FromHtml("#BBDCFA"); cancel.ForeColor = Color.Black;
                    cancel.Font = new Font("Bahnschrift Light SemiCondensed", 10f);
                    form.Controls.AddRange(new Control[] { lbl, txt, ok, cancel });
                    form.AcceptButton = ok; form.CancelButton = cancel;
                    return form.ShowDialog() == DialogResult.OK ? txt.Text : string.Empty;
                }
            }
        }

        private void btnHistory_Click(object sender, EventArgs e)
        {
            new LoginHistoryForm().ShowDialog(this);
        }

        private void btnOrders_Click(object sender, EventArgs e)
        {
            using (var f = new OrdersForm(_role))
                f.ShowDialog(this);
        }

        private void btnAdd_Click(object sender, EventArgs e)
        {
            AddProduct();
        }

        private void btnLogout_Click(object sender, EventArgs e)
        {
            var loginForm = new NewTechApp.Auth.LoginForm();
            loginForm.Show();
            this.Hide();
        }

        private void cmbSuppliers_SelectedIndexChanged(object sender, EventArgs e)
        {
            if (_isReady) { ApplyFilters(); RenderCards(); }
        }

        private void txtSearch_TextChanged(object sender, EventArgs e)
        {
            if (_isReady) { ApplyFilters(); RenderCards(); }
        }

        private void numVolume_ValueChanged(object sender, EventArgs e)
        {
            if (_isReady) { UpdateDiscountColumn(); RenderCards(); }
        }
    }
}