using System;
using System.Data;
using System.Data.SqlClient;
using System.Drawing;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Windows.Forms;
using NewTechApp.Data;
using NewTechApp.UI;

namespace NewTechApp.Auth
{
    public partial class LoginForm : Form
    {
        // UI
        TextBox txtLogin, txtPassword, txtCaptcha;
        CheckBox chkShow;
        Button btnLogin, btnRefreshCaptcha, btnGuest;
        Label lblTitle, lblError, lblLockTimer, lblCaptcha;
        Panel pnlCaptcha;
        PictureBox picCaptcha;
        // state
        string _captchaText = "";
        int _failTotal = 0;
        int _failWithCaptcha = 0;
        DateTime? _lockedUntil = null;
        Timer _lockTimer;
        bool _blocked = false;
        bool _captchaOk = false;

        public LoginForm()
        {
            InitializeComponent();
            ApplyStyle();
        }

        private void ApplyStyle()
        {
            Text = "Вход в систему NewTech";
            Font = new Font("Bahnschrift Light SemiCondensed", 12f);
            BackColor = Color.White;
            StartPosition = FormStartPosition.CenterScreen;
            Width = 560; Height = 560;

            lblTitle = new Label
            {
                Text = "Вход в систему NewTech",
                Dock = DockStyle.Top,
                Height = 60,
                TextAlign = ContentAlignment.MiddleCenter,
                Font = new Font("Bahnschrift Light SemiCondensed", 20, FontStyle.Bold),
                ForeColor = ColorTranslator.FromHtml("#0C4882")
            };
            Controls.Add(lblTitle);

            var lblL = new Label { Text = "Логин:", Left = 40, Top = 90, Width = 120, Font = new Font("Bahnschrift Light SemiCondensed", 12f) };
            txtLogin = new TextBox { Left = 170, Top = 90, Width = 320, Font = new Font("Bahnschrift Light SemiCondensed", 12f) };
            var lblP = new Label { Text = "Пароль:", Left = 40, Top = 130, Width = 120, Font = new Font("Bahnschrift Light SemiCondensed", 12f) };
            txtPassword = new TextBox { Left = 170, Top = 130, Width = 320, UseSystemPasswordChar = true, Font = new Font("Bahnschrift Light SemiCondensed", 12f) };
            chkShow = new CheckBox { Left = 170, Top = 165, Width = 160, Text = "Показать пароль", Font = new Font("Bahnschrift Light SemiCondensed", 10f) };
            chkShow.CheckedChanged += (s, e) => txtPassword.UseSystemPasswordChar = !chkShow.Checked;

            btnLogin = new Button
            {
                Text = "Войти",
                Left = 170,
                Top = 205,
                Width = 160,
                Height = 36,
                BackColor = ColorTranslator.FromHtml("#0C4882"),
                ForeColor = Color.White,
                Font = new Font("Bahnschrift Light SemiCondensed", 12f, FontStyle.Bold)
            };
            btnLogin.Click += BtnLogin_Click;

            btnGuest = new Button
            {
                Text = "Войти как гость",
                Left = 330,
                Top = 205,
                Width = 160,
                Height = 36,
                BackColor = ColorTranslator.FromHtml("#BBDCFA"),
                ForeColor = Color.Black,
                Font = new Font("Bahnschrift Light SemiCondensed", 12f)
            };
            btnGuest.Click += (s, e) =>
            {
                var main = new MainForm("guest", "Гость", "Guest", null);
                main.Show();
                Hide();
            };

            lblError = new Label { Left = 40, Top = 250, Width = 450, ForeColor = Color.Red, Font = new Font("Bahnschrift Light SemiCondensed", 10f) };
            lblLockTimer = new Label { Left = 40, Top = 275, Width = 450, ForeColor = Color.DarkRed, Font = new Font("Bahnschrift Light SemiCondensed", 10f) };

            pnlCaptcha = new Panel
            {
                Left = 40,
                Top = 310,
                Width = 450,
                Height = 140,
                BackColor = ColorTranslator.FromHtml("#BBDCFA"),
                Visible = false,
                Anchor = AnchorStyles.Top | AnchorStyles.Left
            };
            lblCaptcha = new Label
            {
                Text = "Введите символы с картинки:",
                Left = 10,
                Top = 10,
                Width = 260,
                AutoSize = false,
                Font = new Font("Bahnschrift Light SemiCondensed", 10f)
            };
            picCaptcha = new PictureBox
            {
                Left = 10,
                Top = 40,
                Width = 200,
                Height = 60,
                BorderStyle = BorderStyle.FixedSingle,
                SizeMode = PictureBoxSizeMode.Zoom,
                Anchor = AnchorStyles.Top | AnchorStyles.Left
            };
            txtCaptcha = new TextBox
            {
                Left = 220,
                Top = 40,
                Width = 200,
                Anchor = AnchorStyles.Top | AnchorStyles.Left,
                Font = new Font("Bahnschrift Light SemiCondensed", 12f)
            };
            txtCaptcha.TextChanged += TxtCaptcha_TextChanged;

            btnRefreshCaptcha = new Button
            {
                Text = "Обновить",
                Left = 220,
                Top = 40 + 28 + 8,
                Width = 200,
                Height = 28,
                Anchor = AnchorStyles.Top | AnchorStyles.Left,
                BackColor = ColorTranslator.FromHtml("#0C4882"),
                ForeColor = Color.White,
                Font = new Font("Bahnschrift Light SemiCondensed", 10f)
            };
            btnRefreshCaptcha.Click += (s, e) =>
            {
                ShowCaptcha();
                txtCaptcha.Clear();
                txtCaptcha.Focus();
            };

            pnlCaptcha.Controls.Add(lblCaptcha);
            pnlCaptcha.Controls.Add(picCaptcha);
            pnlCaptcha.Controls.Add(txtCaptcha);
            pnlCaptcha.Controls.Add(btnRefreshCaptcha);
            btnRefreshCaptcha.BringToFront();
            pnlCaptcha.Height = btnRefreshCaptcha.Bottom + 12;

            Controls.AddRange(new Control[]
            {
                lblL, txtLogin, lblP, txtPassword, chkShow, btnLogin, btnGuest, lblError, lblLockTimer, pnlCaptcha
            });

            AcceptButton = btnLogin;
            FormClosing += (s, e) => Application.Exit();
            Shown += (s, e) =>
            {
                btnRefreshCaptcha.Top = txtCaptcha.Bottom + 8;
                pnlCaptcha.Height = btnRefreshCaptcha.Bottom + 12;
                btnRefreshCaptcha.BringToFront();
            };
        }

        static byte[] Sha256(string s)
        {
            using (var sha = SHA256.Create())
                return sha.ComputeHash(Encoding.UTF8.GetBytes(s));
        }

        void LogAttempt(string login, bool ok, string reason)
        {
            Db.Exec("INSERT INTO dbo.LoginHistory(Login,Success,Reason) VALUES(@l,@s,@r)",
                new SqlParameter("@l", (object)login ?? DBNull.Value),
                new SqlParameter("@s", ok),
                new SqlParameter("@r", (object)reason ?? DBNull.Value));
        }

        void UpdateLoginButtonEnabled()
        {
            if (IsLockedNow())
            {
                btnLogin.Enabled = false;
                return;
            }
            if (pnlCaptcha.Visible)
            {
                btnLogin.Enabled = _captchaOk;
            }
            else
            {
                btnLogin.Enabled = true;
            }
        }

        void TxtCaptcha_TextChanged(object sender, EventArgs e)
        {
            var current = (txtCaptcha.Text ?? "").Trim();
            _captchaOk = string.Equals(current, _captchaText, StringComparison.OrdinalIgnoreCase);
            txtCaptcha.BackColor = _captchaOk ? Color.Honeydew : Color.White;
            if (_captchaOk && (lblError.Text == "Капча неверна." || lblError.Text == "Введите капчу."))
                lblError.Text = "";
            UpdateLoginButtonEnabled();
        }

        void ShowCaptcha()
        {
            using (var bmp = new Bitmap(200, 60))
            using (var g = Graphics.FromImage(bmp))
            {
                g.Clear(Color.White);
                var chars = "ABCDEFGHJKMNPQRSTUVWXYZ23456789";
                var rand = new Random();
                var text = new string(Enumerable.Repeat(chars, 4).Select(s => s[rand.Next(s.Length)]).ToArray());
                _captchaText = text;

                using (var font = new Font("Bahnschrift Light SemiCondensed", 24, FontStyle.Bold))
                {
                    for (int i = 0; i < text.Length; i++)
                    {
                        var x = 15 + i * 40 + rand.Next(-10, 10);
                        var y = 10 + rand.Next(-5, 5);
                        g.TranslateTransform(x, y);
                        g.RotateTransform(rand.Next(-15, 15));
                        g.DrawString(text[i].ToString(), font, Brushes.Black, 0, 0);
                        g.DrawLine(Pens.Red, -5, -5, 30, 30);
                        g.ResetTransform();
                    }
                }

                if (picCaptcha.Image != null) picCaptcha.Image.Dispose();
                picCaptcha.Image = (Bitmap)bmp.Clone();
                _captchaOk = false;
                txtCaptcha.BackColor = Color.White;
                pnlCaptcha.Visible = true;
                UpdateLoginButtonEnabled();
            }
        }

        void StartLock(int minutes)
        {
            _lockedUntil = DateTime.Now.AddMinutes(minutes);
            if (_lockTimer != null) _lockTimer.Stop();
            _lockTimer = new Timer { Interval = 1000 };
            _lockTimer.Tick += (s, e) =>
            {
                var left = _lockedUntil.Value - DateTime.Now;
                if (left <= TimeSpan.Zero)
                {
                    _lockTimer.Stop();
                    lblLockTimer.Text = "";
                    _failWithCaptcha = 0;
                    _blocked = true;
                    lblError.Text = "Вход заблокирован до перезапуска.";
                    btnLogin.Enabled = false;
                }
                else
                {
                    lblLockTimer.Text = string.Format("Блокировка: {0:mm\\:ss}", left);
                    btnLogin.Enabled = false;
                }
            };
            _lockTimer.Start();
            btnLogin.Enabled = false;
        }

        bool IsLockedNow() => _lockedUntil.HasValue && DateTime.Now < _lockedUntil.Value;

        void BtnLogin_Click(object sender, EventArgs e)
        {
            lblError.Text = "";
            if (_blocked)
            {
                lblError.Text = "Вход заблокирован до перезапуска.";
                return;
            }
            if (IsLockedNow()) return;

            if (pnlCaptcha.Visible && !_captchaOk)
            {
                lblError.Text = "Введите капчу.";
                txtCaptcha.Focus();
                return;
            }

            var login = txtLogin.Text.Trim();
            var pass = txtPassword.Text;
            if (string.IsNullOrEmpty(login) || string.IsNullOrEmpty(pass))
            {
                lblError.Text = "Введите логин и пароль.";
                return;
            }

            if (pnlCaptcha.Visible)
            {
                if (!string.Equals(txtCaptcha.Text.Trim(), _captchaText, StringComparison.OrdinalIgnoreCase))
                {
                    _failTotal++; _failWithCaptcha++;
                    lblError.Text = "Капча неверна.";
                    LogAttempt(login, false, "CaptchaFailed");
                    _captchaOk = false;
                    UpdateLoginButtonEnabled();
                    if (_failWithCaptcha >= 2) { StartLock(3); return; }
                    ShowCaptcha(); txtCaptcha.Clear(); return;
                }
            }

            var dt = Db.Table("SELECT TOP 1 * FROM dbo.Users WHERE Login=@l", new SqlParameter("@l", login));
            if (dt.Rows.Count == 0)
            {
                _failTotal++; lblError.Text = "Неверный логин/пароль.";
                LogAttempt(login, false, "NoUser");
                if (!pnlCaptcha.Visible) { ShowCaptcha(); txtCaptcha.Focus(); }
                else
                {
                    _failWithCaptcha++;
                    if (_failWithCaptcha >= 2) { StartLock(3); return; }
                }
                return;
            }

            var row = dt.Rows[0];
            var hash = (byte[])row["PasswordHash"];
            var ok = hash.SequenceEqual(Sha256(pass));
            if (!ok)
            {
                _failTotal++; lblError.Text = "Неверный логин/пароль.";
                LogAttempt(login, false, "BadPassword");
                if (!pnlCaptcha.Visible) { ShowCaptcha(); txtCaptcha.Focus(); }
                else
                {
                    _failWithCaptcha++;
                    if (_failWithCaptcha >= 2) { StartLock(3); return; }
                    if (!IsLockedNow() && _failTotal > 3) { _blocked = true; lblError.Text = "Вход заблокирован до перезапуска."; }
                }
                return;
            }

            LogAttempt(login, true, null);
            var fullName = Convert.ToString(row["FullName"]);
            var role = Convert.ToString(row["Role"]);
            var photoUrl = row["PhotoUrl"] as string;
            var main = new MainForm(login, fullName, role, photoUrl);
            main.Show();
            Hide();
        }
    }
}