using System;
using System.Data;
using System.Data.SqlClient;
using System.Drawing;
using System.Windows.Forms;
using NewTechApp.Data;

namespace NewTechApp.UI
{
    public partial class AddEditOrderForm : Form
    {
        private readonly int? _orderId;
        private TextBox txtArticle, txtCost;
        private ComboBox cmbStatus, cmbPartner;
        private DateTimePicker dtpDate;
        private Button btnSave, btnCancel;

        public AddEditOrderForm(int? orderId)
        {
            _orderId = orderId;
            Text = _orderId.HasValue ? "Изменение заказа" : "Добавление заказа";
            Font = new Font("Bahnschrift Light SemiCondensed", 10f);
            BackColor = Color.White;
            StartPosition = FormStartPosition.CenterParent;
            Width = 560; Height = 380;

            var lbl1 = new Label { Text = "Артикул:", Left = 30, Top = 30, Width = 150, Font = new Font("Bahnschrift Light SemiCondensed", 10f) };
            txtArticle = new TextBox { Left = 190, Top = 30, Width = 300, Font = new Font("Bahnschrift Light SemiCondensed", 10f) };
            var lbl2 = new Label { Text = "Статус заказа:", Left = 30, Top = 70, Width = 150, Font = new Font("Bahnschrift Light SemiCondensed", 10f) };
            cmbStatus = new ComboBox { Left = 190, Top = 70, Width = 300, DropDownStyle = ComboBoxStyle.DropDownList, Font = new Font("Bahnschrift Light SemiCondensed", 10f) };
            var lbl3 = new Label { Text = "Дата заказа:", Left = 30, Top = 110, Width = 150, Font = new Font("Bahnschrift Light SemiCondensed", 10f) };
            dtpDate = new DateTimePicker { Left = 190, Top = 110, Width = 300, Format = DateTimePickerFormat.Short, Font = new Font("Bahnschrift Light SemiCondensed", 10f) };
            var lbl4 = new Label { Text = "Стоимость (р):", Left = 30, Top = 150, Width = 150, Font = new Font("Bahnschrift Light SemiCondensed", 10f) };
            txtCost = new TextBox { Left = 190, Top = 150, Width = 300, Font = new Font("Bahnschrift Light SemiCondensed", 10f) };
            var lbl5 = new Label { Text = "Партнёр:", Left = 30, Top = 190, Width = 150, Font = new Font("Bahnschrift Light SemiCondensed", 10f) };
            cmbPartner = new ComboBox { Left = 190, Top = 190, Width = 300, DropDownStyle = ComboBoxStyle.DropDownList, Font = new Font("Bahnschrift Light SemiCondensed", 10f) };

            btnSave = new Button
            {
                Text = "Сохранить",
                Left = 190,
                Top = 240,
                Width = 140,
                BackColor = ColorTranslator.FromHtml("#0C4882"),
                ForeColor = Color.White,
                Font = new Font("Bahnschrift Light SemiCondensed", 10f, FontStyle.Bold)
            };
            btnSave.Click += (s, e) => Save();

            btnCancel = new Button
            {
                Text = "Отмена",
                Left = 350,
                Top = 240,
                Width = 140,
                BackColor = ColorTranslator.FromHtml("#BBDCFA"),
                ForeColor = Color.Black,
                Font = new Font("Bahnschrift Light SemiCondensed", 10f)
            };
            btnCancel.Click += (s, e) => DialogResult = DialogResult.Cancel;

            Controls.AddRange(new Control[] { lbl1, txtArticle, lbl2, cmbStatus, lbl3, dtpDate, lbl4, txtCost, lbl5, cmbPartner, btnSave, btnCancel });
            Load += AddEditOrderForm_Load;
        }

        private void AddEditOrderForm_Load(object sender, EventArgs e)
        {
            LoadLookups();
            if (_orderId.HasValue) LoadOrder(_orderId.Value);
            else dtpDate.Value = DateTime.Today;
        }

        private bool ValidateForm(out decimal cost)
        {
            cost = 0m;
            if (string.IsNullOrWhiteSpace(txtArticle.Text))
            {
                MessageBox.Show("Укажите артикул.", "Ошибка", MessageBoxButtons.OK, MessageBoxIcon.Error);
                txtArticle.Focus(); return false;
            }
            if (cmbStatus.SelectedValue == null)
            {
                MessageBox.Show("Выберите статус заказа.", "Ошибка", MessageBoxButtons.OK, MessageBoxIcon.Error);
                cmbStatus.DroppedDown = true; return false;
            }
            if (!decimal.TryParse(txtCost.Text.Replace(',', '.'),
                System.Globalization.NumberStyles.Any,
                System.Globalization.CultureInfo.InvariantCulture, out cost) || cost < 0m)
            {
                MessageBox.Show("Стоимость должна быть неотрицательным числом.",
                    "Ошибка", MessageBoxButtons.OK, MessageBoxIcon.Error);
                txtCost.Focus(); return false;
            }
            if (cmbPartner.SelectedValue == null)
            {
                MessageBox.Show("Выберите партнёра.", "Ошибка", MessageBoxButtons.OK, MessageBoxIcon.Error);
                cmbPartner.DroppedDown = true; return false;
            }
            return true;
        }
        private void LoadLookups()
        {
            // Загрузка статусов из таблицы OrderStatuses
            var st = Db.Table("SELECT StatusID, StatusName FROM dbo.OrderStatuses ORDER BY StatusName");
            cmbStatus.DisplayMember = "StatusName";
            cmbStatus.ValueMember = "StatusID";
            cmbStatus.DataSource = st;

            // Загрузка партнеров
            var pr = Db.Table("SELECT PartnerID, PartnerName FROM dbo.Partners ORDER BY PartnerName");
            cmbPartner.DisplayMember = "PartnerName";
            cmbPartner.ValueMember = "PartnerID";
            cmbPartner.DataSource = pr;
        }

        private void LoadOrder(int id)
        {
            // ИСПРАВЛЕННЫЙ ЗАПРОС - используем только существующие столбцы
            var dt = Db.Table(@"
SELECT OrderID, Article, Status, OrderDate, TotalAmount, PartnerID
FROM dbo.Orders WHERE OrderID=@id", new SqlParameter("@id", id));

            if (dt.Rows.Count == 0)
            {
                MessageBox.Show("Заказ не найден.", "Ошибка", MessageBoxButtons.OK, MessageBoxIcon.Error);
                DialogResult = DialogResult.Cancel; return;
            }

            var r = dt.Rows[0];
            txtArticle.Text = Convert.ToString(r["Article"]);

            // ИСПРАВЛЕНО - ищем StatusID по имени статуса из OrderStatuses
            var statusName = Convert.ToString(r["Status"]);
            foreach (DataRowView item in cmbStatus.Items)
            {
                if (item["StatusName"].ToString() == statusName)
                {
                    cmbStatus.SelectedValue = item["StatusID"];
                    break;
                }
            }

            dtpDate.Value = Convert.ToDateTime(r["OrderDate"]);
            txtCost.Text = (r["TotalAmount"] == DBNull.Value ? 0m : Convert.ToDecimal(r["TotalAmount"])).ToString("0.00");

            if (r["PartnerID"] != DBNull.Value)
            {
                cmbPartner.SelectedValue = Convert.ToInt32(r["PartnerID"]);
            }
        }

        private void Save()
        {
            decimal cost;
            if (!ValidateForm(out cost)) return;

            try
            {
                if (_orderId.HasValue)
                {
                    // ИСПРАВЛЕНО - используем имя статуса вместо ID
                    var statusName = cmbStatus.Text;

                    Db.Exec(@"
UPDATE dbo.Orders
   SET Article=@a, Status=@s, OrderDate=@d, TotalAmount=@c, PartnerID=@p
 WHERE OrderID=@id",
                        new SqlParameter("@a", txtArticle.Text.Trim()),
                        new SqlParameter("@s", statusName), // Сохраняем имя статуса
                        new SqlParameter("@d", dtpDate.Value.Date),
                        new SqlParameter("@c", cost),
                        new SqlParameter("@p", (int)cmbPartner.SelectedValue),
                        new SqlParameter("@id", _orderId.Value));
                }
                else
                {
                    var statusName = cmbStatus.Text;

                    Db.Exec(@"
INSERT INTO dbo.Orders(Article, Status, OrderDate, TotalAmount, PartnerID)
VALUES(@a, @s, @d, @c, @p)",
                        new SqlParameter("@a", txtArticle.Text.Trim()),
                        new SqlParameter("@s", statusName), // Сохраняем имя статуса
                        new SqlParameter("@d", dtpDate.Value.Date),
                        new SqlParameter("@c", cost),
                        new SqlParameter("@p", (int)cmbPartner.SelectedValue));
                }

                MessageBox.Show("Данные заказа сохранены.",
                    "Информация", MessageBoxButtons.OK, MessageBoxIcon.Information);
                DialogResult = DialogResult.OK;
            }
            catch (Exception ex)
            {
                MessageBox.Show("Не удалось сохранить заказ. Причина: " + ex.Message,
                    "Ошибка", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }
    }
}