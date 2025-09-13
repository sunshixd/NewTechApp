using System;
using System.Collections.Generic;
using System.Data;
using System.Data.SqlClient;
using System.Drawing;
using System.IO;
using System.Windows.Forms;
using NewTechApp.Data;
using NewTechApp.Utility;

namespace NewTechApp.UI
{
    public partial class ProductForm : Form
    {
        private const string T_PRODUCTS = "dbo.Products";
        private const string COL_ID = "ProductID";
        private const string COL_NAME = "ProductName";
        private const string COL_ARTICLE = "Article";
        private const string COL_SUPPLIER_ID = "SupplierID";
        private const string COL_MIN_PRICE = "MinPartnerPrice";
        private const string COL_PROMO_PRICE = "PromoPrice";
        private const string COL_IMG = "ImageUrl";
        private const string COL_TYPEID = "ProductTypeID";
        private readonly int? _productId;
        private string _currentImagePath;
        // UI
        Label lblId;
        TextBox tbId, tbName, tbArticle;
        ComboBox cmbSupplier;
        NumericUpDown numPrice, numPromo;
        PictureBox pic;
        Button btnLoad, btnSave, btnCancel;

        public ProductForm(int? productId = null)
        {
            _productId = productId;
            BuildUi();
            Text = _productId.HasValue ? "Редактирование товара" : "Добавление товара";
            if (_productId.HasValue) LoadProduct(_productId.Value);
        }

        private void BuildUi()
        {
            StartPosition = FormStartPosition.CenterParent;
            BackColor = Color.White;
            Font = new Font("Bahnschrift Light SemiCondensed", 10f);
            ClientSize = new Size(680, 360);
            FormBorderStyle = FormBorderStyle.FixedDialog;
            MaximizeBox = false;
            MinimizeBox = false;

            int x1 = 16, w1 = 250, y = 16, dy = 34;

            lblId = new Label { Left = x1, Top = y, Width = 140, Text = "ID (только чтение):", Font = new Font("Bahnschrift Light SemiCondensed", 10f) };
            tbId = new TextBox { Left = x1 + 160, Top = y - 2, Width = 90, ReadOnly = true, Visible = _productId.HasValue, Font = new Font("Bahnschrift Light SemiCondensed", 10f) };
            if (!_productId.HasValue) { lblId.Visible = false; }
            Controls.Add(lblId); Controls.Add(tbId); y += dy;

            Controls.Add(new Label { Left = x1, Top = y, Width = 140, Text = "Название *:", Font = new Font("Bahnschrift Light SemiCondensed", 10f) });
            tbName = new TextBox { Left = x1 + 160, Top = y - 2, Width = w1, Font = new Font("Bahnschrift Light SemiCondensed", 10f) };
            Controls.Add(tbName); y += dy;

            Controls.Add(new Label { Left = x1, Top = y, Width = 140, Text = "Артикул:", Font = new Font("Bahnschrift Light SemiCondensed", 10f) });
            tbArticle = new TextBox { Left = x1 + 160, Top = y - 2, Width = w1, Font = new Font("Bahnschrift Light SemiCondensed", 10f) };
            Controls.Add(tbArticle); y += dy;

            Controls.Add(new Label { Left = x1, Top = y, Width = 140, Text = "Поставщик:", Font = new Font("Bahnschrift Light SemiCondensed", 10f) });
            cmbSupplier = new ComboBox { Left = x1 + 160, Top = y - 2, Width = w1, DropDownStyle = ComboBoxStyle.DropDownList, Font = new Font("Bahnschrift Light SemiCondensed", 10f) };
            Controls.Add(cmbSupplier); y += dy;

            Controls.Add(new Label { Left = x1, Top = y, Width = 140, Text = "Мин. цена *:", Font = new Font("Bahnschrift Light SemiCondensed", 10f) });
            numPrice = new NumericUpDown { Left = x1 + 160, Top = y - 2, Width = 120, DecimalPlaces = 2, Minimum = 0, Maximum = 100000000, Font = new Font("Bahnschrift Light SemiCondensed", 10f) };
            Controls.Add(numPrice); y += dy;

            Controls.Add(new Label { Left = x1, Top = y, Width = 140, Text = "Акц. цена:", Font = new Font("Bahnschrift Light SemiCondensed", 10f) });
            numPromo = new NumericUpDown { Left = x1 + 160, Top = y - 2, Width = 120, DecimalPlaces = 2, Minimum = 0, Maximum = 100000000, Font = new Font("Bahnschrift Light SemiCondensed", 10f) };
            Controls.Add(numPromo);

            // Картинка
            pic = new PictureBox
            {
                Left = 440,
                Top = 16,
                Width = 220,
                Height = 160,
                BorderStyle = BorderStyle.FixedSingle,
                SizeMode = PictureBoxSizeMode.Zoom,
                BackColor = Color.White
            };
            Controls.Add(pic);

            btnLoad = new Button
            {
                Left = 440,
                Top = 184,
                Width = 220,
                Height = 28,
                Text = "Загрузить изображение",
                BackColor = ColorTranslator.FromHtml("#BBDCFA"),
                ForeColor = Color.Black,
                Font = new Font("Bahnschrift Light SemiCondensed", 10f)
            };
            btnLoad.Click += (s, e) => LoadImage();
            Controls.Add(btnLoad);

            // Кнопки
            btnSave = new Button
            {
                Left = 440,
                Top = 270,
                Width = 110,
                Height = 34,
                Text = "Сохранить",
                BackColor = ColorTranslator.FromHtml("#0C4882"),
                ForeColor = Color.White,
                Font = new Font("Bahnschrift Light SemiCondensed", 10f, FontStyle.Bold)
            };
            btnCancel = new Button
            {
                Left = 550,
                Top = 270,
                Width = 110,
                Height = 34,
                Text = "Отмена",
                BackColor = ColorTranslator.FromHtml("#BBDCFA"),
                ForeColor = Color.Black,
                Font = new Font("Bahnschrift Light SemiCondensed", 10f)
            };
            btnCancel.Click += (s, e) => DialogResult = DialogResult.Cancel;
            btnSave.Click += (s, e) => SaveProduct();
            Controls.AddRange(new Control[] { btnSave, btnCancel });

            Load += (s, e) => LoadSuppliers();
        }

        private void LoadSuppliers()
        {
            var dt = Db.Table("SELECT SupplierID, SupplierName FROM dbo.Suppliers ORDER BY SupplierName");
            cmbSupplier.DisplayMember = "SupplierName";
            cmbSupplier.ValueMember = "SupplierID";
            cmbSupplier.DataSource = dt;
            if (dt.Rows.Count > 0) cmbSupplier.SelectedIndex = 0;
        }

        private void LoadProduct(int id)
        {
            string sql = "SELECT " + COL_ID + "," + COL_NAME + "," + COL_ARTICLE + "," + COL_SUPPLIER_ID + "," +
                         COL_MIN_PRICE + "," + COL_PROMO_PRICE + "," + COL_IMG +
                         " FROM " + T_PRODUCTS + " WHERE " + COL_ID + "=@id";
            var dt = Db.Table(sql, new SqlParameter("@id", id));
            if (dt.Rows.Count == 0)
            {
                MessageBox.Show("Товар не найден.", "Информация", MessageBoxButtons.OK, MessageBoxIcon.Information);
                Close();
                return;
            }
            var r = dt.Rows[0];
            tbId.Text = Convert.ToString(r[COL_ID]);
            tbName.Text = Convert.ToString(r[COL_NAME]);
            tbArticle.Text = r[COL_ARTICLE] == DBNull.Value ? "" : Convert.ToString(r[COL_ARTICLE]);
            if (r[COL_SUPPLIER_ID] != DBNull.Value)
                cmbSupplier.SelectedValue = Convert.ToInt32(r[COL_SUPPLIER_ID]);
            numPrice.Value = r[COL_MIN_PRICE] == DBNull.Value ? 0 : Convert.ToDecimal(r[COL_MIN_PRICE]);
            numPromo.Value = r[COL_PROMO_PRICE] == DBNull.Value ? 0 : Convert.ToDecimal(r[COL_PROMO_PRICE]);
            _currentImagePath = r[COL_IMG] == DBNull.Value ? null : Convert.ToString(r[COL_IMG]);
            if (!string.IsNullOrEmpty(_currentImagePath) && File.Exists(_currentImagePath))
            {
                try { pic.Image = Image.FromFile(_currentImagePath); } catch { }
            }
        }

        private void LoadImage()
        {
            using (var ofd = new OpenFileDialog { Filter = "Images|*.jpg;*.jpeg;*.png;*.bmp" })
            {
                if (ofd.ShowDialog() == DialogResult.OK)
                {
                    try
                    {
                        var image = Image.FromFile(ofd.FileName);
                        // Resize image to 300x200
                        var resizedImage = ImageHelper.SaveResized(image, 300, 200, _currentImagePath);
                        pic.Image = Image.FromFile(resizedImage);
                        _currentImagePath = resizedImage;
                    }
                    catch (Exception ex)
                    {
                        MessageBox.Show("Не удалось открыть изображение: " + ex.Message, "Ошибка",
                            MessageBoxButtons.OK, MessageBoxIcon.Error);
                    }
                }
            }
        }

        private int GetDefaultProductTypeId()
        {
            try
            {
                var dt = Db.Table("SELECT TOP 1 ProductTypeID FROM dbo.ProductTypes ORDER BY ProductTypeID");
                if (dt.Rows.Count > 0) return Convert.ToInt32(dt.Rows[0][0]);
            }
            catch { /* таблицы может не быть — это ок */ }
            return 1;
        }

        private void SaveProduct()
        {
            try
            {
                if (string.IsNullOrWhiteSpace(tbName.Text))
                {
                    MessageBox.Show("Заполните название товара.", "Ошибка", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    return;
                }
                if (numPrice.Value <= 0)
                {
                    MessageBox.Show("Цена должна быть больше 0.", "Ошибка", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    return;
                }

                var ps = new List<SqlParameter>
        {
            new SqlParameter("@n", tbName.Text.Trim()),
            new SqlParameter("@a", (object)tbArticle.Text.Trim() ?? DBNull.Value),
            new SqlParameter("@sid", cmbSupplier.SelectedValue == null ? (object)DBNull.Value : (object)(int)cmbSupplier.SelectedValue),
            new SqlParameter("@p", numPrice.Value),
            new SqlParameter("@cost", numPrice.Value), // CostPrice = MinPartnerPrice
            new SqlParameter("@prodTime", 0), // ProductionTimeHours = 0
            new SqlParameter("@workshop", 1), // WorkshopNumber = 1
            new SqlParameter("@img", (object)_currentImagePath ?? DBNull.Value)
        };

                if (_productId.HasValue)
                {
                    string sql = "UPDATE " + T_PRODUCTS + " SET " +
                                 "ProductName=@n," +
                                 "Article=@a," +
                                 "SupplierID=@sid," +
                                 "MinPartnerPrice=@p," +
                                 "CostPrice=@cost," +
                                 "ProductionTimeHours=@prodTime," +
                                 "WorkshopNumber=@workshop," +
                                 "ImageUrl=@img " +
                                 "WHERE ProductID=@id";
                    ps.Add(new SqlParameter("@id", _productId.Value));
                    Db.Exec(sql, ps.ToArray());
                }
                else
                {
                    int typeId = GetDefaultProductTypeId();
                    ps.Add(new SqlParameter("@pt", typeId));
                    string sql = "INSERT INTO " + T_PRODUCTS + "(" +
                                 "ProductTypeID,ProductName,Article,SupplierID," +
                                 "MinPartnerPrice,CostPrice,ProductionTimeHours,WorkshopNumber,ImageUrl) " +
                                 "OUTPUT INSERTED.ProductID " +
                                 "VALUES(@pt,@n,@a,@sid,@p,@cost,@prodTime,@workshop,@img)";
                    var dtId = Db.Table(sql, ps.ToArray());
                    if (dtId.Rows.Count > 0)
                        tbId.Text = Convert.ToString(dtId.Rows[0]["ProductID"]);
                }
                DialogResult = DialogResult.OK;
                Close();
            }
            catch (Exception ex)
            {
                MessageBox.Show("Ошибка сохранения: " + ex.Message, "Ошибка",
                    MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }
    }
}