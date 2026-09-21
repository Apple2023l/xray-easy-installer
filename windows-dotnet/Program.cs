using System.Drawing.Drawing2D;
using System.Net;
using System.Net.Sockets;
using System.Reflection;
using System.Text;
using System.Text.Json;
using QRCoder;
using Renci.SshNet;

namespace XrayInstaller.Windows;

internal static class Program
{
    [STAThread]
    private static void Main()
    {
        ApplicationConfiguration.Initialize();
        Application.Run(new InstallerForm());
    }
}

internal sealed class InstallerForm : Form
{
    private enum UiLanguage { Russian, English, Persian, Chinese }

    private const int ServerPort = 8435;
    private static readonly Color Background = Color.FromArgb(32, 35, 45);
    private static readonly Color PanelBackground = Color.FromArgb(41, 45, 57);
    private static readonly Color FieldBackground = Color.FromArgb(24, 27, 34);
    private static readonly Color Foreground = Color.FromArgb(238, 240, 246);
    private static readonly Color Secondary = Color.FromArgb(174, 179, 194);
    private static readonly Color Accent = Color.FromArgb(52, 122, 246);
    private static readonly Color Success = Color.FromArgb(67, 214, 111);

    private static readonly (string Name, string Domain)[] Sites =
    [
        ("Xbox", "www.xbox.com"), ("Microsoft", "www.microsoft.com"),
        ("Mozilla", "www.mozilla.org"), ("Google", "www.google.com"),
        ("Amazon", "www.amazon.com"), ("Meta", "www.facebook.com"),
        ("NVIDIA", "www.nvidia.com"), ("Adobe", "www.adobe.com"),
        ("Intel", "www.intel.com"), ("AMD", "www.amd.com"),
        ("IBM", "www.ibm.com"), ("Cisco", "www.cisco.com"),
        ("Salesforce", "www.salesforce.com"), ("Oracle", "www.oracle.com"),
        ("Samsung", "www.samsung.com"), ("Netflix", "www.netflix.com"),
        ("Spotify", "www.spotify.com"), ("GitHub", "github.com"),
        ("Cloudflare", "www.cloudflare.com"), ("OpenAI", "openai.com")
    ];

    private readonly TextBox _ip = Input();
    private readonly TextBox _login = Input("root");
    private readonly TextBox _password = Input();
    private readonly ComboBox _profile = Combo();
    private readonly ComboBox _site = Combo();
    private readonly TextBox _domain = Input();
    private readonly Button _installButton = PrimaryButton("Подключиться и установить");
    private readonly Label _status = LabelText("Введите данные нового сервера.", Secondary);
    private readonly ProgressBar _progress = new() { Style = ProgressBarStyle.Continuous, Minimum = 0, Maximum = 100, Visible = false, Width = 180 };
    private readonly RichTextBox _log = OutputBox();
    private readonly TextBox _resultLink = Input();

    private readonly RichTextBox _podkopLink = OutputBox(82);
    private readonly ComboBox _xhttpMode = Combo();
    private readonly RichTextBox _podkopJson = OutputBox();
    private readonly Label _podkopStatus = LabelText("Вставьте ссылку или сначала установите сервер.", Secondary);

    private readonly RichTextBox _shadowLink = OutputBox(92);
    private readonly PictureBox _shadowQr = new() { SizeMode = PictureBoxSizeMode.Zoom, Dock = DockStyle.Fill };
    private Bitmap? _currentQr;
    private readonly TabControl _tabs = new() { Dock = DockStyle.Fill, Padding = new Point(16, 7) };
    private readonly ComboBox _languagePicker = Combo();
    private UiLanguage _language = UiLanguage.Russian;
    private bool _running;

    private static readonly Dictionary<UiLanguage, Dictionary<string, string>> Texts = new()
    {
        [UiLanguage.Russian] = new()
        {
            ["window"]="Xray Installer для Windows", ["language"]="Язык", ["install_tab"]="Установка",
            ["podkop_tab"]="Роутер · JSON", ["shadow_tab"]="Телефон · QR", ["app_title"]="Xray Installer",
            ["server_subtitle"]="VLESS + REALITY или Hysteria 2", ["ip"]="IP сервера",
            ["login"]="SSH-логин", ["password"]="SSH-пароль", ["profile"]="Профиль",
            ["site"]="Сайт маскировки", ["domain"]="Домен Hysteria 2", ["xhttp_profile"]="XHTTP + REALITY — основной",
            ["vision_profile"]="RAW + REALITY + Vision — быстрый", ["install_button"]="Подключиться и установить",
            ["hysteria_profile"]="Hysteria 2 + Salamander — UDP 40460", ["bad_domain"]="Введите домен, направленный на IP сервера.",
            ["ready"]="Введите данные нового сервера.", ["installing"]="Установка сервера…",
            ["copy_link"]="Копировать ссылку", ["progress"]="Ход установки",
            ["connecting"]="Подключение к {0}…\n", ["done"]="Готово — ссылка создана и проверена.",
            ["failed"]="Установка не завершилась. Подробности в журнале.", ["error_prefix"]="ОШИБКА",
            ["podkop_title"]="Конфигуратор Podkop", ["podkop_subtitle"]="VLESS или Hysteria 2 → sing-box outbound JSON",
            ["vless_link"]="Ссылка подключения", ["xhttp_mode"]="Режим XHTTP", ["generate"]="Сформировать outbound",
            ["outbound"]="Outbound Config", ["copy_json"]="Копировать JSON",
            ["paste_first"]="Вставьте ссылку или сначала установите сервер.",
            ["podkop_done"]="Готово: профиль {0}. Вставьте JSON в Outbound Config.",
            ["shadow_title"]="Импорт в Shadowrocket", ["shadow_subtitle"]="Ссылка подключения и QR-код",
            ["refresh_qr"]="Обновить QR", ["copy_vless"]="Копировать ссылку", ["save_qr"]="Сохранить QR…",
            ["bad_ip_title"]="Неверный адрес", ["bad_ip"]="Введите публичный IPv4-адрес сервера.",
            ["bad_login_title"]="Неверный логин", ["bad_login"]="Введите корректный SSH-логин.",
            ["no_password_title"]="Нет пароля", ["no_password"]="Введите SSH-пароль.",
            ["bad_link"]="Нужна корректная VLESS или Hysteria 2 ссылка.", ["bad_uuid"]="VLESS-ссылка содержит некорректный UUID.",
            ["reality_only"]="Поддерживается профиль VLESS + REALITY.", ["missing_parameter"]="В ссылке отсутствует параметр {0}.",
            ["profile_required"]="Нужен профиль XHTTP или RAW/TCP с Vision.", ["no_link_title"]="Нет ссылки",
            ["no_link"]="Вставьте корректную VLESS-ссылку.", ["qr_error"]="Ошибка QR",
            ["save_dialog"]="Сохранить QR-код", ["remote_channel"]="Не удалось выполнить установку на сервере.",
            ["missing_script"]="В приложении отсутствует setup_xray.py.", ["no_server_link"]="Сервер не вернул ссылку подключения."
        },
        [UiLanguage.English] = new()
        {
            ["window"]="Xray Installer for Windows", ["language"]="Language", ["install_tab"]="Install",
            ["podkop_tab"]="Router · JSON", ["shadow_tab"]="Phone · QR", ["app_title"]="Xray Installer",
            ["server_subtitle"]="VLESS + REALITY or Hysteria 2", ["ip"]="Server IP",
            ["login"]="SSH username", ["password"]="SSH password", ["profile"]="Profile",
            ["site"]="Camouflage site", ["domain"]="Hysteria 2 domain", ["xhttp_profile"]="XHTTP + REALITY — primary",
            ["vision_profile"]="RAW + REALITY + Vision — fast", ["install_button"]="Connect and install",
            ["hysteria_profile"]="Hysteria 2 + Salamander — UDP 40460", ["bad_domain"]="Enter a domain that points to the server IP.",
            ["ready"]="Enter the new server details.", ["installing"]="Installing the server…",
            ["copy_link"]="Copy link", ["progress"]="Installation progress",
            ["connecting"]="Connecting to {0}…\n", ["done"]="Done — the link was created and verified.",
            ["failed"]="Installation did not complete. See the log for details.", ["error_prefix"]="ERROR",
            ["podkop_title"]="Podkop configurator", ["podkop_subtitle"]="VLESS or Hysteria 2 → sing-box outbound JSON",
            ["vless_link"]="Connection link", ["xhttp_mode"]="XHTTP mode", ["generate"]="Generate outbound",
            ["outbound"]="Outbound Config", ["copy_json"]="Copy JSON",
            ["paste_first"]="Paste a link or install a server first.",
            ["podkop_done"]="Ready: {0} profile. Paste the JSON into Outbound Config.",
            ["shadow_title"]="Import into Shadowrocket", ["shadow_subtitle"]="Connection link and QR code",
            ["refresh_qr"]="Refresh QR", ["copy_vless"]="Copy link", ["save_qr"]="Save QR…",
            ["bad_ip_title"]="Invalid address", ["bad_ip"]="Enter the server's public IPv4 address.",
            ["bad_login_title"]="Invalid username", ["bad_login"]="Enter a valid SSH username.",
            ["no_password_title"]="Missing password", ["no_password"]="Enter the SSH password.",
            ["bad_link"]="A valid VLESS or Hysteria 2 link is required.", ["bad_uuid"]="The VLESS link contains an invalid UUID.",
            ["reality_only"]="A VLESS + REALITY profile is required.", ["missing_parameter"]="The link is missing the {0} parameter.",
            ["profile_required"]="An XHTTP or RAW/TCP Vision profile is required.", ["no_link_title"]="No link",
            ["no_link"]="Paste a valid VLESS link.", ["qr_error"]="QR error", ["save_dialog"]="Save QR code",
            ["remote_channel"]="The server installation command failed.", ["missing_script"]="setup_xray.py is missing from the application.",
            ["no_server_link"]="The server did not return a connection link."
        },
        [UiLanguage.Persian] = new()
        {
            ["window"]="نصب‌کننده Xray برای ویندوز", ["language"]="زبان", ["install_tab"]="نصب",
            ["podkop_tab"]="روتر · JSON", ["shadow_tab"]="تلفن · QR", ["app_title"]="نصب‌کننده Xray",
            ["server_subtitle"]="VLESS + REALITY یا Hysteria 2", ["ip"]="IP سرور",
            ["login"]="نام کاربری SSH", ["password"]="رمز عبور SSH", ["profile"]="پروفایل",
            ["site"]="سایت پوششی", ["domain"]="دامنه Hysteria 2", ["xhttp_profile"]="XHTTP + REALITY — اصلی",
            ["vision_profile"]="RAW + REALITY + Vision — سریع", ["install_button"]="اتصال و نصب",
            ["hysteria_profile"]="Hysteria 2 + Salamander — UDP 40460", ["bad_domain"]="دامنه‌ای را وارد کنید که به IP سرور اشاره کند.",
            ["ready"]="اطلاعات سرور جدید را وارد کنید.", ["installing"]="در حال نصب سرور…",
            ["copy_link"]="کپی لینک", ["progress"]="روند نصب", ["connecting"]="در حال اتصال به {0}…\n",
            ["done"]="انجام شد — لینک ساخته و بررسی شد.", ["failed"]="نصب کامل نشد. جزئیات را در گزارش ببینید.",
            ["error_prefix"]="خطا", ["podkop_title"]="پیکربندی Podkop",
            ["podkop_subtitle"]="VLESS یا Hysteria 2 → فایل JSON برای sing-box", ["vless_link"]="لینک اتصال",
            ["xhttp_mode"]="حالت XHTTP", ["generate"]="ساخت Outbound", ["outbound"]="پیکربندی Outbound",
            ["copy_json"]="کپی JSON", ["paste_first"]="لینک را وارد کنید یا ابتدا سرور را نصب کنید.",
            ["podkop_done"]="آماده: پروفایل {0}. فایل JSON را در Outbound Config وارد کنید.",
            ["shadow_title"]="ورود به Shadowrocket", ["shadow_subtitle"]="لینک اتصال و کد QR",
            ["refresh_qr"]="به‌روزرسانی QR", ["copy_vless"]="کپی لینک", ["save_qr"]="ذخیره QR…",
            ["bad_ip_title"]="آدرس نامعتبر", ["bad_ip"]="IPv4 عمومی سرور را وارد کنید.",
            ["bad_login_title"]="نام کاربری نامعتبر", ["bad_login"]="نام کاربری معتبر SSH را وارد کنید.",
            ["no_password_title"]="رمز عبور وارد نشده", ["no_password"]="رمز عبور SSH را وارد کنید.",
            ["bad_link"]="یک لینک معتبر VLESS یا Hysteria 2 لازم است.", ["bad_uuid"]="UUID در لینک VLESS نامعتبر است.",
            ["reality_only"]="پروفایل VLESS + REALITY لازم است.", ["missing_parameter"]="پارامتر {0} در لینک وجود ندارد.",
            ["profile_required"]="پروفایل XHTTP یا RAW/TCP Vision لازم است.", ["no_link_title"]="لینکی وجود ندارد",
            ["no_link"]="یک لینک معتبر VLESS وارد کنید.", ["qr_error"]="خطای QR", ["save_dialog"]="ذخیره کد QR",
            ["remote_channel"]="اجرای نصب روی سرور ناموفق بود.", ["missing_script"]="فایل setup_xray.py در برنامه وجود ندارد.",
            ["no_server_link"]="سرور لینک اتصال برنگرداند."
        },
        [UiLanguage.Chinese] = new()
        {
            ["window"]="Xray Windows 安装器", ["language"]="语言", ["install_tab"]="安装",
            ["podkop_tab"]="路由器 · JSON", ["shadow_tab"]="手机 · 二维码", ["app_title"]="Xray 安装器",
            ["server_subtitle"]="VLESS + REALITY 或 Hysteria 2", ["ip"]="服务器 IP",
            ["login"]="SSH 用户名", ["password"]="SSH 密码", ["profile"]="配置模式",
            ["site"]="伪装站点", ["domain"]="Hysteria 2 域名", ["xhttp_profile"]="XHTTP + REALITY — 首选",
            ["vision_profile"]="RAW + REALITY + Vision — 高速", ["install_button"]="连接并安装",
            ["hysteria_profile"]="Hysteria 2 + Salamander — UDP 40460", ["bad_domain"]="请输入指向服务器 IP 的域名。",
            ["ready"]="请输入新服务器信息。", ["installing"]="正在安装服务器…", ["copy_link"]="复制链接",
            ["progress"]="安装进度", ["connecting"]="正在连接 {0}…\n", ["done"]="完成 — 链接已创建并验证。",
            ["failed"]="安装未完成，请查看日志。", ["error_prefix"]="错误", ["podkop_title"]="Podkop 配置生成器",
            ["podkop_subtitle"]="VLESS 或 Hysteria 2 → sing-box outbound JSON", ["vless_link"]="连接链接",
            ["xhttp_mode"]="XHTTP 模式", ["generate"]="生成 outbound", ["outbound"]="Outbound Config",
            ["copy_json"]="复制 JSON", ["paste_first"]="请粘贴链接或先安装服务器。",
            ["podkop_done"]="完成：{0} 配置。请将 JSON 粘贴到 Outbound Config。",
            ["shadow_title"]="导入 Shadowrocket", ["shadow_subtitle"]="连接链接和二维码",
            ["refresh_qr"]="刷新二维码", ["copy_vless"]="复制链接", ["save_qr"]="保存二维码…",
            ["bad_ip_title"]="地址无效", ["bad_ip"]="请输入服务器的公网 IPv4 地址。",
            ["bad_login_title"]="用户名无效", ["bad_login"]="请输入有效的 SSH 用户名。",
            ["no_password_title"]="缺少密码", ["no_password"]="请输入 SSH 密码。",
            ["bad_link"]="需要有效的 VLESS 或 Hysteria 2 链接。", ["bad_uuid"]="VLESS 链接中的 UUID 无效。",
            ["reality_only"]="需要 VLESS + REALITY 配置。", ["missing_parameter"]="链接缺少 {0} 参数。",
            ["profile_required"]="需要 XHTTP 或 RAW/TCP Vision 配置。", ["no_link_title"]="没有链接",
            ["no_link"]="请粘贴有效的 VLESS 链接。", ["qr_error"]="二维码错误", ["save_dialog"]="保存二维码",
            ["remote_channel"]="无法在服务器上执行安装。", ["missing_script"]="应用中缺少 setup_xray.py。",
            ["no_server_link"]="服务器未返回连接链接。"
        }
    };

    public InstallerForm()
    {
        BackColor = Background;
        ForeColor = Foreground;
        Font = new Font("Segoe UI", 10F);
        StartPosition = FormStartPosition.CenterScreen;
        MinimumSize = new Size(920, 680);
        Size = new Size(1080, 800);
        Icon = Icon.ExtractAssociatedIcon(Application.ExecutablePath);

        _password.UseSystemPasswordChar = true;
        _site.Items.AddRange(Sites.Select(item => $"{item.Name} · {item.Domain}").ToArray());
        _site.SelectedIndex = 0;
        _domain.PlaceholderText = "vpn.example.com";
        _domain.Enabled = false;
        _profile.SelectedIndexChanged += (_, _) =>
        {
            var hysteria = _profile.SelectedIndex == 2;
            _domain.Enabled = !_running && hysteria;
            _site.Enabled = !_running && !hysteria;
        };
        _xhttpMode.Items.AddRange(["packet-up", "stream-up", "stream-one", "auto"]);
        _xhttpMode.SelectedIndex = 0;
        _resultLink.ReadOnly = true;
        _languagePicker.Items.AddRange(["Русский", "English", "فارسی", "中文"]);
        _languagePicker.Width = 150;
        _languagePicker.SelectedIndex = 0;
        _languagePicker.SelectedIndexChanged += (_, _) =>
        {
            _language = (UiLanguage)_languagePicker.SelectedIndex;
            ApplyLanguage();
        };

        var languageBar = new FlowLayoutPanel
        {
            Dock = DockStyle.Top, Height = 44, BackColor = PanelBackground,
            FlowDirection = FlowDirection.LeftToRight, Padding = new Padding(18, 7, 18, 5), WrapContents = false
        };
        var languageLabel = LabelText("Language", Foreground);
        languageLabel.Name = "languageLabel";
        languageLabel.Padding = new Padding(0, 6, 5, 0);
        languageBar.Controls.Add(languageLabel);
        languageBar.Controls.Add(_languagePicker);
        Controls.Add(_tabs);
        Controls.Add(languageBar);

        _installButton.Click += async (_, _) => await InstallAsync();
        FormClosed += (_, _) => _currentQr?.Dispose();
        ApplyLanguage();
    }

    private string T(string key, params object[] args)
    {
        var value = Texts[_language][key];
        return args.Length == 0 ? value : string.Format(value, args);
    }

    private void ApplyLanguage()
    {
        var selectedTab = Math.Max(0, _tabs.SelectedIndex);
        var selectedProfile = Math.Max(0, _profile.SelectedIndex);
        Text = T("window");
        var rtl = _language == UiLanguage.Persian;
        RightToLeft = rtl ? RightToLeft.Yes : RightToLeft.No;
        RightToLeftLayout = rtl;
        if (Controls.Find("languageLabel", true).FirstOrDefault() is Label languageLabel)
            languageLabel.Text = T("language");

        _profile.BeginUpdate();
        _profile.Items.Clear();
        _profile.Items.AddRange([T("xhttp_profile"), T("vision_profile"), T("hysteria_profile")]);
        _profile.SelectedIndex = Math.Min(selectedProfile, 2);
        _profile.EndUpdate();
        _installButton.Text = T("install_button");
        _status.Text = _running ? T("installing") :
            string.IsNullOrWhiteSpace(_resultLink.Text) ? T("ready") : T("done");
        if (string.IsNullOrWhiteSpace(_podkopJson.Text)) _podkopStatus.Text = T("paste_first");

        _tabs.SuspendLayout();
        _tabs.TabPages.Clear();
        _tabs.TabPages.Add(BuildInstallTab());
        _tabs.TabPages.Add(BuildPodkopTab());
        _tabs.TabPages.Add(BuildShadowrocketTab());
        _tabs.SelectedIndex = Math.Min(selectedTab, _tabs.TabPages.Count - 1);
        _tabs.ResumeLayout();
        if (!string.IsNullOrWhiteSpace(_podkopJson.Text)) GeneratePodkop();
    }

    private TabPage BuildInstallTab()
    {
        var page = NewPage(T("install_tab"));
        var root = StackPanel();
        page.Controls.Add(root);
        root.Controls.Add(Header(T("app_title"), T("server_subtitle")));

        var form = new TableLayoutPanel
        {
            AutoSize = true, Width = 960, ColumnCount = 2, BackColor = Background,
            Padding = new Padding(0, 0, 0, 8)
        };
        form.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 180));
        form.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
        AddRow(form, T("ip"), _ip);
        AddRow(form, T("login"), _login);
        AddRow(form, T("password"), _password);
        AddRow(form, T("profile"), _profile);
        AddRow(form, T("site"), _site);
        AddRow(form, T("domain"), _domain);
        root.Controls.Add(form);

        var action = new FlowLayoutPanel
        {
            AutoSize = true, Width = 960, BackColor = Background,
            FlowDirection = FlowDirection.LeftToRight, Padding = new Padding(0, 4, 0, 8), WrapContents = false
        };
        action.Controls.Add(_installButton);
        action.Controls.Add(_progress);
        _status.AutoSize = true;
        _status.Padding = new Padding(8, 9, 0, 0);
        action.Controls.Add(_status);
        root.Controls.Add(action);

        var result = new TableLayoutPanel { Width = 960, Height = 40, ColumnCount = 2, BackColor = Background };
        result.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
        result.ColumnStyles.Add(new ColumnStyle(SizeType.AutoSize));
        _resultLink.Dock = DockStyle.Fill;
        var copy = SecondaryButton(T("copy_link"));
        copy.Click += (_, _) => Copy(_resultLink.Text);
        result.Controls.Add(_resultLink, 0, 0);
        result.Controls.Add(copy, 1, 0);
        root.Controls.Add(result);

        root.Controls.Add(SectionTitle(T("progress")));
        _log.Width = 960;
        _log.Height = 330;
        root.Controls.Add(_log);
        root.SetFlowBreak(_log, true);
        return page;
    }

    private TabPage BuildPodkopTab()
    {
        var page = NewPage(T("podkop_tab"));
        var root = StackPanel();
        page.Controls.Add(root);
        root.Controls.Add(Header(T("podkop_title"), T("podkop_subtitle")));
        root.Controls.Add(SectionTitle(T("vless_link")));
        _podkopLink.Width = 960;
        root.Controls.Add(_podkopLink);

        var controls = new FlowLayoutPanel
        {
            AutoSize = true, Width = 960, BackColor = Background,
            FlowDirection = FlowDirection.LeftToRight, Padding = new Padding(0, 8, 0, 8)
        };
        controls.Controls.Add(LabelText(T("xhttp_mode"), Foreground));
        _xhttpMode.Width = 160;
        controls.Controls.Add(_xhttpMode);
        var generate = PrimaryButton(T("generate"));
        generate.Click += (_, _) => GeneratePodkop();
        controls.Controls.Add(generate);
        root.Controls.Add(controls);

        var title = new FlowLayoutPanel { AutoSize = true, Width = 960, BackColor = Background };
        title.Controls.Add(SectionTitle(T("outbound")));
        var copy = SecondaryButton(T("copy_json"));
        copy.Click += (_, _) => Copy(_podkopJson.Text);
        title.Controls.Add(copy);
        root.Controls.Add(title);

        _podkopJson.Width = 960;
        _podkopJson.Height = 410;
        root.Controls.Add(_podkopJson);
        _podkopStatus.AutoSize = true;
        _podkopStatus.Padding = new Padding(0, 8, 0, 0);
        root.Controls.Add(_podkopStatus);
        return page;
    }

    private TabPage BuildShadowrocketTab()
    {
        var page = NewPage(T("shadow_tab"));
        var root = StackPanel();
        page.Controls.Add(root);
        root.Controls.Add(Header(T("shadow_title"), T("shadow_subtitle")));
        _shadowLink.Width = 960;
        root.Controls.Add(_shadowLink);

        var actions = new FlowLayoutPanel
        {
            AutoSize = true, Width = 960, BackColor = Background,
            FlowDirection = FlowDirection.LeftToRight, Padding = new Padding(0, 8, 0, 8)
        };
        var refresh = PrimaryButton(T("refresh_qr"));
        refresh.Click += (_, _) => RefreshQr();
        var copy = SecondaryButton(T("copy_vless"));
        copy.Click += (_, _) => Copy(_shadowLink.Text);
        var save = SecondaryButton(T("save_qr"));
        save.Click += (_, _) => SaveQr();
        actions.Controls.AddRange([refresh, copy, save]);
        root.Controls.Add(actions);

        var qrPanel = new Panel { Width = 960, Height = 510, BackColor = Color.White, Padding = new Padding(18) };
        qrPanel.Controls.Add(_shadowQr);
        root.Controls.Add(qrPanel);
        return page;
    }

    private async Task InstallAsync()
    {
        if (!IPAddress.TryParse(_ip.Text.Trim(), out var address) ||
            address.AddressFamily != AddressFamily.InterNetwork || IsPrivate(address))
        {
            MessageBox.Show(this, T("bad_ip"), T("bad_ip_title"),
                MessageBoxButtons.OK, MessageBoxIcon.Warning);
            return;
        }
        if (!System.Text.RegularExpressions.Regex.IsMatch(_login.Text.Trim(), @"^[A-Za-z_][A-Za-z0-9._-]*$"))
        {
            MessageBox.Show(this, T("bad_login"), T("bad_login_title"),
                MessageBoxButtons.OK, MessageBoxIcon.Warning);
            return;
        }
        if (string.IsNullOrEmpty(_password.Text))
        {
            MessageBox.Show(this, T("no_password"), T("no_password_title"),
                MessageBoxButtons.OK, MessageBoxIcon.Warning);
            return;
        }

        var ip = address.ToString();
        var login = _login.Text.Trim();
        var password = _password.Text;
        var profile = _profile.SelectedIndex switch { 1 => "vision", 2 => "hysteria2", _ => "xhttp" };
        var masquerade = Sites[Math.Max(0, _site.SelectedIndex)].Domain;
        var site = profile == "hysteria2" ? _domain.Text.Trim().ToLowerInvariant() : masquerade;
        if (!System.Text.RegularExpressions.Regex.IsMatch(site,
                @"^(?=.{1,253}$)[a-z0-9]+(?:[a-z0-9-]*[a-z0-9])?(?:\.[a-z0-9]+(?:[a-z0-9-]*[a-z0-9])?)+$"))
        {
            MessageBox.Show(this, T("bad_domain"), T("bad_ip_title"),
                MessageBoxButtons.OK, MessageBoxIcon.Warning);
            return;
        }
        _password.Clear();
        SetRunning(true);
        _log.Text = T("connecting", ip);

        try
        {
            var result = await Task.Run(() => InstallServer(ip, login, password, site, masquerade, profile,
                T("missing_script"), T("remote_channel"), T("no_server_link"), HandleRemoteOutput));
            _resultLink.Text = result.Link;
            _podkopLink.Text = result.Link;
            _shadowLink.Text = result.Link;
            _status.ForeColor = Success;
            _status.Text = T("done");
            _progress.Value = 100;
            GeneratePodkop();
            RefreshQr(false);
        }
        catch (Exception error)
        {
            _status.ForeColor = Secondary;
            _status.Text = T("failed");
            _log.AppendText($"\n{T("error_prefix")}: {error.Message}\n");
        }
        finally
        {
            password = string.Empty;
            SetRunning(false);
        }
    }

    private static (string Link, string Log) InstallServer(
        string ip, string login, string password, string site, string masquerade, string profile,
        string missingScript, string remoteError, string noServerLink, Action<string> onOutput)
    {
        var auth = new PasswordAuthenticationMethod(login, password);
        var connection = new ConnectionInfo(ip, 22, login, auth)
        {
            Timeout = TimeSpan.FromSeconds(15)
        };
        using var ssh = new SshClient(connection);
        using var sftp = new SftpClient(connection);
        ssh.HostKeyReceived += (_, args) => args.CanTrust = true;
        sftp.HostKeyReceived += (_, args) => args.CanTrust = true;
        ssh.KeepAliveInterval = TimeSpan.FromSeconds(30);
        ssh.Connect();
        sftp.Connect();

        var remote = $"/tmp/xray-installer-{Guid.NewGuid():N}.py";
        try
        {
            using var script = Assembly.GetExecutingAssembly()
                .GetManifestResourceStream("XrayInstaller.setup_xray.py")
                ?? throw new InvalidOperationException(missingScript);
            sftp.UploadFile(script, remote, true);

            using var command = ssh.CreateCommand(
                $"python3 {ShellQuote(remote)} --ip {ShellQuote(ip)} --site {ShellQuote(site)} " +
                $"--masquerade {ShellQuote(masquerade)} --profile {ShellQuote(profile)}");
            command.CommandTimeout = TimeSpan.FromMinutes(25);
            var output = new StringBuilder();
            var outputLock = new object();
            void AddLine(string line)
            {
                lock (outputLock) output.AppendLine(line);
                onOutput(line + Environment.NewLine);
            }
            static void Pump(Stream stream, Action<string> addLine)
            {
                using var reader = new StreamReader(stream, Encoding.UTF8, true, 4096, leaveOpen: true);
                while (reader.ReadLine() is { } line) addLine(line);
            }
            var execution = command.BeginExecute();
            var stdoutTask = Task.Run(() => Pump(command.OutputStream, AddLine));
            var stderrTask = Task.Run(() => Pump(command.ExtendedOutputStream, AddLine));
            command.EndExecute(execution);
            Task.WaitAll(stdoutTask, stderrTask);
            string log;
            lock (outputLock) log = output.ToString();
            if (command.ExitStatus != 0)
                throw new InvalidOperationException(log.Trim().Length > 0 ? log.Trim() : remoteError);
            var link = log.Split(['\r', '\n'], StringSplitOptions.RemoveEmptyEntries)
                .Select(line => line.Trim())
                .FirstOrDefault(line => line.StartsWith("vless://", StringComparison.OrdinalIgnoreCase) ||
                                        line.StartsWith("hysteria2://", StringComparison.OrdinalIgnoreCase) ||
                                        line.StartsWith("hy2://", StringComparison.OrdinalIgnoreCase));
            if (string.IsNullOrWhiteSpace(link))
                throw new InvalidOperationException(noServerLink);
            return (link, log);
        }
        finally
        {
            try { if (sftp.Exists(remote)) sftp.DeleteFile(remote); } catch { }
            sftp.Disconnect();
            ssh.Disconnect();
        }
    }

    private void HandleRemoteOutput(string text)
    {
        if (InvokeRequired)
        {
            BeginInvoke(() => HandleRemoteOutput(text));
            return;
        }
        _log.AppendText(text);
        _log.SelectionStart = _log.TextLength;
        _log.ScrollToCaret();
        var match = System.Text.RegularExpressions.Regex.Match(
            text, @"::progress::(?<percent>\d{1,3})::(?<stage>[^\r\n]+)");
        if (!match.Success || !int.TryParse(match.Groups["percent"].Value, out var percent)) return;
        _progress.Value = Math.Clamp(percent, 0, 100);
        _status.Text = $"{match.Groups["stage"].Value} · {_progress.Value}%";
    }

    private void GeneratePodkop()
    {
        try
        {
            var (outbound, profile) = BuildPodkop(_podkopLink.Text.Trim(), _xhttpMode.Text);
            _podkopJson.Text = JsonSerializer.Serialize(outbound, new JsonSerializerOptions { WriteIndented = true });
            _podkopStatus.ForeColor = Success;
            _podkopStatus.Text = T("podkop_done",
                profile == "xhttp" ? "XHTTP" : profile == "vision" ? "Vision" : profile);
        }
        catch (Exception error)
        {
            _podkopJson.Clear();
            _podkopStatus.ForeColor = Secondary;
            _podkopStatus.Text = error.Message;
        }
    }

    private (Dictionary<string, object?> Outbound, string Profile) BuildPodkop(string value, string mode)
    {
        if (!Uri.TryCreate(value, UriKind.Absolute, out var uri) ||
            string.IsNullOrWhiteSpace(uri.Host) || uri.Port <= 0)
            throw new FormatException(T("bad_link"));

        var query = ParseQuery(uri.Query);
        if (uri.Scheme.Equals("hysteria2", StringComparison.OrdinalIgnoreCase) ||
            uri.Scheme.Equals("hy2", StringComparison.OrdinalIgnoreCase))
        {
            var authPassword = Uri.UnescapeDataString(uri.UserInfo);
            if (string.IsNullOrWhiteSpace(authPassword)) throw new FormatException(T("bad_link"));
            foreach (var key in new[] { "sni", "obfs-password" })
                if (!query.TryGetValue(key, out var parameter) || string.IsNullOrWhiteSpace(parameter))
                    throw new FormatException(T("missing_parameter", key));
            var tagPart = uri.Host.Split('.').FirstOrDefault() ?? "server";
            return (new Dictionary<string, object?>
            {
                ["type"] = "hysteria2",
                ["tag"] = "hy2-" + tagPart,
                ["server"] = uri.Host,
                ["server_port"] = uri.Port,
                ["password"] = authPassword,
                ["obfs"] = new Dictionary<string, object?>
                {
                    ["type"] = query.GetValueOrDefault("obfs", "salamander"),
                    ["password"] = query["obfs-password"]
                },
                ["tls"] = new Dictionary<string, object?>
                {
                    ["enabled"] = true,
                    ["server_name"] = query["sni"]
                }
            }, "Hysteria 2");
        }
        if (!uri.Scheme.Equals("vless", StringComparison.OrdinalIgnoreCase))
            throw new FormatException(T("bad_link"));

        var uuid = Uri.UnescapeDataString(uri.UserInfo);
        if (!Guid.TryParse(uuid, out _)) throw new FormatException(T("bad_uuid"));
        if (!query.TryGetValue("security", out var security) || !security.Equals("reality", StringComparison.OrdinalIgnoreCase))
            throw new FormatException(T("reality_only"));
        foreach (var key in new[] { "sni", "pbk", "sid" })
            if (!query.ContainsKey(key) || string.IsNullOrWhiteSpace(query[key]))
                throw new FormatException(T("missing_parameter", key));

        var type = query.GetValueOrDefault("type", "tcp").ToLowerInvariant();
        var flow = query.GetValueOrDefault("flow", string.Empty).ToLowerInvariant();
        var isXhttp = type == "xhttp";
        var isVision = (type is "tcp" or "raw") && flow == "xtls-rprx-vision";
        if (!isXhttp && !isVision)
            throw new FormatException(T("profile_required"));

        var outbound = new Dictionary<string, object?>
        {
            ["type"] = "vless",
            ["server"] = uri.Host,
            ["server_port"] = uri.Port,
            ["uuid"] = uuid,
            ["tls"] = new Dictionary<string, object?>
            {
                ["enabled"] = true,
                ["server_name"] = query["sni"],
                ["utls"] = new Dictionary<string, object?>
                {
                    ["enabled"] = true,
                    ["fingerprint"] = query.GetValueOrDefault("fp", "chrome")
                },
                ["reality"] = new Dictionary<string, object?>
                {
                    ["enabled"] = true,
                    ["public_key"] = query["pbk"],
                    ["short_id"] = query["sid"]
                }
            }
        };
        if (isXhttp)
        {
            outbound["transport"] = new Dictionary<string, object?>
            {
                ["type"] = "xhttp",
                ["mode"] = mode,
                ["host"] = query.GetValueOrDefault("host", query["sni"]),
                ["path"] = query.GetValueOrDefault("path", "/"),
                ["x_padding_bytes"] = "100-1000"
            };
            return (outbound, "xhttp");
        }
        outbound["flow"] = "xtls-rprx-vision";
        return (outbound, "vision");
    }

    private void RefreshQr(bool showError = true)
    {
        var value = _shadowLink.Text.Trim();
        if (!value.StartsWith("vless://", StringComparison.OrdinalIgnoreCase) &&
            !value.StartsWith("hysteria2://", StringComparison.OrdinalIgnoreCase) &&
            !value.StartsWith("hy2://", StringComparison.OrdinalIgnoreCase))
        {
            if (showError) MessageBox.Show(this, T("no_link"), T("no_link_title"));
            return;
        }
        try
        {
            using var generator = new QRCodeGenerator();
            using var data = generator.CreateQrCode(value, QRCodeGenerator.ECCLevel.Q);
            using var qr = new QRCode(data);
            var next = qr.GetGraphic(6, Color.Black, Color.White, true);
            var previous = _currentQr;
            _currentQr = next;
            _shadowQr.Image = _currentQr;
            previous?.Dispose();
        }
        catch (Exception error)
        {
            if (showError) MessageBox.Show(this, error.Message, T("qr_error"));
        }
    }

    private void SaveQr()
    {
        if (_currentQr is null) RefreshQr();
        if (_currentQr is null) return;
        using var dialog = new SaveFileDialog
        {
            Title = T("save_dialog"), Filter = "PNG image|*.png", FileName = "xray-qr.png", DefaultExt = "png"
        };
        if (dialog.ShowDialog(this) == DialogResult.OK)
            _currentQr.Save(dialog.FileName, System.Drawing.Imaging.ImageFormat.Png);
    }

    private void SetRunning(bool running)
    {
        _running = running;
        _installButton.Enabled = !running;
        _ip.Enabled = !running;
        _login.Enabled = !running;
        _password.Enabled = !running;
        _profile.Enabled = !running;
        _site.Enabled = !running && _profile.SelectedIndex != 2;
        _domain.Enabled = !running && _profile.SelectedIndex == 2;
        _progress.Visible = running;
        if (running) _progress.Value = 2;
        _status.ForeColor = Secondary;
        if (running) _status.Text = T("installing");
    }

    private static Dictionary<string, string> ParseQuery(string query)
    {
        var result = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        foreach (var part in query.TrimStart('?').Split('&', StringSplitOptions.RemoveEmptyEntries))
        {
            var pieces = part.Split('=', 2);
            var key = WebUtility.UrlDecode(pieces[0]).ToLowerInvariant();
            var value = pieces.Length > 1 ? WebUtility.UrlDecode(pieces[1]) : string.Empty;
            result[key] = value;
        }
        return result;
    }

    private static string ShellQuote(string value) => "'" + value.Replace("'", "'\"'\"'") + "'";

    private static bool IsPrivate(IPAddress address)
    {
        var bytes = address.GetAddressBytes();
        return bytes[0] is 10 or 127 ||
               bytes[0] == 172 && bytes[1] is >= 16 and <= 31 ||
               bytes[0] == 192 && bytes[1] == 168 ||
               bytes[0] == 169 && bytes[1] == 254 ||
               bytes[0] >= 224;
    }

    private static void Copy(string value)
    {
        if (!string.IsNullOrWhiteSpace(value)) Clipboard.SetText(value);
    }

    private static TabPage NewPage(string title) => new(title) { BackColor = Background, Padding = new Padding(18) };

    private static FlowLayoutPanel StackPanel() => new()
    {
        Dock = DockStyle.Fill, FlowDirection = FlowDirection.TopDown,
        WrapContents = false, AutoScroll = true, BackColor = Background
    };

    private static Control Header(string title, string subtitle)
    {
        var panel = new Panel { Height = 76, Width = 960, BackColor = Background, Margin = new Padding(0, 0, 0, 10) };
        var mark = LabelText("✦", Foreground);
        mark.Font = new Font("Segoe UI Symbol", 32F);
        mark.Location = new Point(0, 4);
        mark.AutoSize = true;
        var titleLabel = LabelText(title, Foreground);
        titleLabel.Font = new Font("Segoe UI Semibold", 20F);
        titleLabel.Location = new Point(62, 3);
        titleLabel.AutoSize = true;
        var subtitleLabel = LabelText(subtitle, Secondary);
        subtitleLabel.Font = new Font("Segoe UI", 11F);
        subtitleLabel.Location = new Point(64, 42);
        subtitleLabel.AutoSize = true;
        panel.Controls.AddRange([mark, titleLabel, subtitleLabel]);
        return panel;
    }

    private static void AddRow(TableLayoutPanel form, string text, Control control)
    {
        var row = form.RowCount++;
        form.RowStyles.Add(new RowStyle(SizeType.Absolute, 44));
        var label = LabelText(text, Foreground);
        label.Dock = DockStyle.Fill;
        label.TextAlign = ContentAlignment.MiddleLeft;
        control.Dock = DockStyle.Fill;
        control.Margin = new Padding(0, 5, 0, 5);
        form.Controls.Add(label, 0, row);
        form.Controls.Add(control, 1, row);
    }

    private static Label SectionTitle(string text)
    {
        var label = LabelText(text, Foreground);
        label.Font = new Font("Segoe UI Semibold", 11F);
        label.AutoSize = true;
        label.Margin = new Padding(0, 10, 0, 6);
        return label;
    }

    private static Label LabelText(string text, Color color) => new()
    {
        Text = text, ForeColor = color, BackColor = Background, AutoSize = true
    };

    private static TextBox Input(string value = "") => new()
    {
        Text = value, BackColor = FieldBackground, ForeColor = Foreground,
        BorderStyle = BorderStyle.FixedSingle, Font = new Font("Segoe UI", 10.5F)
    };

    private static ComboBox Combo() => new()
    {
        DropDownStyle = ComboBoxStyle.DropDownList, BackColor = FieldBackground,
        ForeColor = Foreground, FlatStyle = FlatStyle.Flat, Font = new Font("Segoe UI", 10F)
    };

    private static RichTextBox OutputBox(int height = 200) => new()
    {
        Height = height, BackColor = FieldBackground, ForeColor = Foreground,
        BorderStyle = BorderStyle.FixedSingle, Font = new Font("Cascadia Mono", 9.5F),
        DetectUrls = false, WordWrap = true
    };

    private static Button PrimaryButton(string text) => StyledButton(text, Accent);
    private static Button SecondaryButton(string text) => StyledButton(text, PanelBackground);

    private static Button StyledButton(string text, Color color) => new()
    {
        Text = text, AutoSize = true, Height = 34, FlatStyle = FlatStyle.Flat,
        BackColor = color, ForeColor = Color.White, Padding = new Padding(8, 3, 8, 3),
        Cursor = Cursors.Hand
    };
}
