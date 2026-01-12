using System;
using System.Drawing;
using System.Windows.Forms;
using System.IO;
using System.Runtime.InteropServices; // 新增：用于调用 Windows API
using Microsoft.Win32;                // 新增：用于监听系统颜色设置变化
using Microsoft.Web.WebView2.Core;
using Microsoft.Web.WebView2.WinForms;
using Microsoft.Data.Sqlite;

namespace CialloBrowser
{
    public class Form1 : Form
    {
        private WebView2 webView;
        private Panel topPanel;
        private TextBox txtUrl;
        private Button btnGo, btnBack, btnForward, btnRefresh, btnHome, btnHistory, btnClear;

        private const string BrowserName = "Ciallo浏览器";
        private readonly string fixedUserDataFolder = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "UserData");

        public Form1()
        {
            this.Text = $"{BrowserName} - 初始化中...";
            this.Size = new Size(1200, 800);
            this.StartPosition = FormStartPosition.CenterScreen;
            try { this.Icon = new Icon("logo.ico"); } catch 
            {
                // 如果是单文件嵌入模式，尝试从资源读取
                try {
                    var assembly = System.Reflection.Assembly.GetExecutingAssembly();
                    // 注意：这里的资源名取决于您的命名空间，默认是 CialloBrowser.logo.ico
                    // 如果图标不显示，可能需要检查资源名
                    using (var stream = assembly.GetManifestResourceStream("CialloBrowser.logo.ico"))
                    {
                        if(stream != null) this.Icon = new Icon(stream);
                    }
                } catch {}
            }

            // 🔥🔥🔥 1. 初始化时应用深色模式 🔥🔥🔥
            UpdateTitleBarTheme();

            // 🔥🔥🔥 2. 监听系统颜色变化事件 🔥🔥🔥
            SystemEvents.UserPreferenceChanged += (s, e) => 
            {
                if (e.Category == UserPreferenceCategory.General)
                {
                    UpdateTitleBarTheme();
                }
            };

            // --- 1. 顶部面板 ---
            topPanel = new Panel() { Dock = DockStyle.Top, Height = 45, Padding = new Padding(5), BackColor = Color.WhiteSmoke };
            this.Controls.Add(topPanel);

            // --- 2. 按钮群 ---
            btnBack = CreateButton("←", 10);
            btnBack.Click += (s, e) => { if (webView.CanGoBack) webView.GoBack(); };
            topPanel.Controls.Add(btnBack);

            btnForward = CreateButton("→", 50);
            btnForward.Click += (s, e) => { if (webView.CanGoForward) webView.GoForward(); };
            topPanel.Controls.Add(btnForward);

            btnRefresh = CreateButton("↻", 90);
            btnRefresh.Click += (s, e) => webView.Reload();
            topPanel.Controls.Add(btnRefresh);

            btnHome = CreateButton("🏠", 130);
            btnHome.Click += (s, e) => NavigateToHome();
            topPanel.Controls.Add(btnHome);

            btnHistory = CreateButton("H", 170);
            btnHistory.Click += (s, e) => ShowHistoryWindow();
            topPanel.Controls.Add(btnHistory);

            btnClear = CreateButton("🧹", 210);
            btnClear.ForeColor = Color.Red;
            btnClear.Click += (s, e) => ShowClearDataDialog(); 
            topPanel.Controls.Add(btnClear);

            btnGo = new Button() { Text = "Go", Size = new Size(50, 30), Location = new Point(topPanel.Width - 65, 7), Anchor = AnchorStyles.Top | AnchorStyles.Right };
            btnGo.Click += (s, e) => NavigateToSite();
            topPanel.Controls.Add(btnGo);

            // --- 3. 地址栏 ---
            txtUrl = new TextBox() { 
                Location = new Point(255, 9), 
                Height = 30, 
                Font = new Font("Segoe UI", 10), 
                Width = topPanel.Width - 255 - 80, 
                Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right 
            };
            
            txtUrl.KeyDown += (s, e) => { if (e.KeyCode == Keys.Enter) NavigateToSite(); };
            txtUrl.DoubleClick += (s, e) => txtUrl.SelectAll();
            topPanel.Controls.Add(txtUrl);

            // --- 4. 浏览器主体 ---
            webView = new WebView2() { Dock = DockStyle.Fill };
            this.Controls.Add(webView);
            webView.BringToFront();

            InitializeWebView();
        }

        // ---深色标题栏区---

        [DllImport("dwmapi.dll")]
        private static extern int DwmSetWindowAttribute(IntPtr hwnd, int attr, ref int attrValue, int attrSize);

        private const int DWMWA_USE_IMMERSIVE_DARK_MODE_BEFORE_20H1 = 19;
        private const int DWMWA_USE_IMMERSIVE_DARK_MODE = 20;

        private void UpdateTitleBarTheme()
        {
            try
            {
                // 1. 判断系统是否是深色模式
                // 读取注册表: HKEY_CURRENT_USER\Software\Microsoft\Windows\CurrentVersion\Themes\Personalize
                // AppsUseLightTheme = 0 (深色), 1 (浅色)
                bool isDarkMode = false;
                using (var key = Registry.CurrentUser.OpenSubKey(@"Software\Microsoft\Windows\CurrentVersion\Themes\Personalize"))
                {
                    if (key != null)
                    {
                        var val = key.GetValue("AppsUseLightTheme");
                        if (val is int i && i == 0)
                        {
                            isDarkMode = true;
                        }
                    }
                }

                // 2. 调用 API 设置标题栏颜色
                int attribute = DWMWA_USE_IMMERSIVE_DARK_MODE;
                int useImmersiveDarkMode = isDarkMode ? 1 : 0;

                if (DwmSetWindowAttribute(this.Handle, attribute, ref useImmersiveDarkMode, sizeof(int)) != 0)
                {
                    // 如果失败，尝试旧版本的 API (针对 Win10 早期版本)
                    DwmSetWindowAttribute(this.Handle, DWMWA_USE_IMMERSIVE_DARK_MODE_BEFORE_20H1, ref useImmersiveDarkMode, sizeof(int));
                }

                // 3. 顺便改一下工具栏颜色，让整体更协调
                if (isDarkMode)
                {
                    topPanel.BackColor = Color.FromArgb(45, 45, 48); // 深灰背景
                    txtUrl.BackColor = Color.FromArgb(30, 30, 30);   // 输入框深黑
                    txtUrl.ForeColor = Color.White;                // 文字变白
                    foreach(Control c in topPanel.Controls) { if(c is Button) { c.BackColor = Color.FromArgb(60, 60, 60); c.ForeColor = Color.White; } }
                    // 特殊处理清除按钮的红色
                    btnClear.ForeColor = Color.FromArgb(255, 100, 100); 
                }
                else
                {
                    topPanel.BackColor = Color.WhiteSmoke;
                    txtUrl.BackColor = Color.White;
                    txtUrl.ForeColor = Color.Black;
                    foreach(Control c in topPanel.Controls) { if(c is Button) { c.BackColor = Control.DefaultBackColor; c.ForeColor = Color.Black; } }
                    btnClear.ForeColor = Color.Red;
                }
            }
            catch 
            {
                // 如果出错（比如不是Win10/11），就保持默认，不让程序崩掉
            }
        }

        // --- 以下是常规功能 ---

        private Button CreateButton(string text, int x)
        {
            return new Button() { Text = text, Location = new Point(x, 7), Size = new Size(35, 30) };
        }

        async void InitializeWebView()
        {
            var env = await CoreWebView2Environment.CreateAsync(null, fixedUserDataFolder);
            await webView.EnsureCoreWebView2Async(env);
            webView.CoreWebView2.NewWindowRequested += (s, e) => { e.Handled = true; webView.CoreWebView2.Navigate(e.Uri); };
            
            webView.SourceChanged += (s, e) =>
            {
                 if (!txtUrl.Focused) 
                 {
                     string src = webView.Source.ToString();
                     if (src.StartsWith("data:")) txtUrl.Text = "🏠 主页"; 
                     else txtUrl.Text = src;
                 }
            };
            
            webView.CoreWebView2.DocumentTitleChanged += (s, e) =>
            {
                string pageTitle = webView.CoreWebView2.DocumentTitle;
                if (string.IsNullOrEmpty(pageTitle) || pageTitle == "about:blank") this.Text = BrowserName;
                else this.Text = $"{pageTitle} - {BrowserName}";
            };
            NavigateToHome();
        }

        // --- 历史记录 (防死机副本模式) ---
        private void ShowHistoryWindow()
        {
            Form historyForm = new Form();
            historyForm.Text = "历史记录";
            historyForm.Size = new Size(800, 500);
            historyForm.StartPosition = FormStartPosition.CenterParent;
            try { historyForm.Icon = this.Icon; } catch { }
            ListBox listBox = new ListBox();
            listBox.Dock = DockStyle.Fill;
            listBox.Font = new Font("Segoe UI", 10);
            listBox.IntegralHeight = false;
            string dbPath = Path.Combine(fixedUserDataFolder, "EBWebView", "Default", "History");
            string tempDbPath = Path.GetTempFileName(); 

            if (!File.Exists(dbPath)) { listBox.Items.Add($"暂无记录"); }
            else {
                try {
                    File.Copy(dbPath, tempDbPath, true);
                    string connectionString = $"Data Source={tempDbPath}";
                    using (var connection = new SqliteConnection(connectionString)) {
                        connection.Open();
                        var command = connection.CreateCommand();
                        command.CommandText = "SELECT title, url FROM urls WHERE url LIKE 'http%' ORDER BY last_visit_time DESC LIMIT 50";
                        using (var reader = command.ExecuteReader()) {
                            while (reader.Read()) {
                                string title = reader.GetString(0); string url = reader.GetString(1);
                                if(string.IsNullOrEmpty(title)) title = "无标题"; listBox.Items.Add($"{title} | {url}");
                            }
                        }
                    }
                } catch (Exception ex) { listBox.Items.Add("读取历史失败: " + ex.Message); }
                finally { try { GC.Collect(); GC.WaitForPendingFinalizers(); if (File.Exists(tempDbPath)) File.Delete(tempDbPath); } catch { } }
            }
            listBox.DoubleClick += (s, e) => {
                if (listBox.SelectedItem != null) {
                    string item = listBox.SelectedItem.ToString(); int lastSplit = item.LastIndexOf('|');
                    if (lastSplit > 0) webView.CoreWebView2.Navigate(item.Substring(lastSplit + 1).Trim());
                    historyForm.Close();
                }
            };
            historyForm.Controls.Add(listBox); historyForm.ShowDialog(this);
        }

        // --- 高级清理面板 (修复版) ---
        private void ShowClearDataDialog()
        {
            Form clearForm = new Form(); clearForm.Text = "清除浏览数据"; clearForm.Size = new Size(350, 300);
            clearForm.StartPosition = FormStartPosition.CenterParent; clearForm.FormBorderStyle = FormBorderStyle.FixedDialog;
            clearForm.MaximizeBox = false; clearForm.MinimizeBox = false; try { clearForm.Icon = this.Icon; } catch { }
            Label lblTitle = new Label() { Text = "请选择要清除的内容：", Location = new Point(20, 20), AutoSize = true, Font = new Font("Segoe UI", 10, FontStyle.Bold) };
            clearForm.Controls.Add(lblTitle);
            CheckBox chkHistory = new CheckBox() { Text = "浏览历史记录", Location = new Point(30, 60), AutoSize = true, Checked = true };
            CheckBox chkCookies = new CheckBox() { Text = "Cookie 和其他网站数据", Location = new Point(30, 90), AutoSize = true, Checked = true };
            CheckBox chkCache = new CheckBox() { Text = "缓存的图片和文件", Location = new Point(30, 120), AutoSize = true, Checked = true };
            CheckBox chkAll = new CheckBox() { Text = "清除所有 (彻底重置)", Location = new Point(30, 160), AutoSize = true, Font = new Font("Segoe UI", 9, FontStyle.Bold), ForeColor = Color.Red };
            chkAll.CheckedChanged += (s, e) => { bool isAll = chkAll.Checked; chkHistory.Checked = isAll; chkCookies.Checked = isAll; chkCache.Checked = isAll; chkHistory.Enabled = !isAll; chkCookies.Enabled = !isAll; chkCache.Enabled = !isAll; };
            clearForm.Controls.Add(chkHistory); clearForm.Controls.Add(chkCookies); clearForm.Controls.Add(chkCache); clearForm.Controls.Add(chkAll);
            Button btnConfirm = new Button() { Text = "立即清除", Location = new Point(120, 210), Size = new Size(100, 35), BackColor = Color.MistyRose };
            btnConfirm.Click += async (s, e) => {
                btnConfirm.Text = "清理中..."; btnConfirm.Enabled = false;
                try {
                    CoreWebView2Profile profile = webView.CoreWebView2.Profile;
                    if (chkAll.Checked) await profile.ClearBrowsingDataAsync(CoreWebView2BrowsingDataKinds.AllProfile);
                    else {
                        CoreWebView2BrowsingDataKinds flags = (CoreWebView2BrowsingDataKinds)0;
                        if (chkHistory.Checked) flags |= CoreWebView2BrowsingDataKinds.BrowsingHistory;
                        if (chkCookies.Checked) flags |= CoreWebView2BrowsingDataKinds.Cookies;
                        if (chkCache.Checked) flags |= CoreWebView2BrowsingDataKinds.DiskCache;
                        if (flags != (CoreWebView2BrowsingDataKinds)0) await profile.ClearBrowsingDataAsync(flags);
                    }
                    MessageBox.Show("清理完成！✨", "提示"); clearForm.Close(); if (chkAll.Checked || chkHistory.Checked) NavigateToHome();
                } catch (Exception ex) { MessageBox.Show("清理失败: " + ex.Message); clearForm.Close(); }
            };
            clearForm.Controls.Add(btnConfirm); clearForm.ShowDialog(this);
        }

        void NavigateToHome()
        {
            string html = @"
            <html>
            <head>
                <meta charset='utf-8'>
                <meta name='viewport' content='width=device-width, initial-scale=1.0'>
                <title>新标签页</title>
                <style>
                    body { font-family: 'Segoe UI', sans-serif; display: flex; flex-direction: column; align-items: center; justify-content: center; height: 100vh; margin: 0; background-color: #f9f9f9; color: #333; transition: background 0.3s, color 0.3s; }
                    .logo { font-size: 60px; margin-bottom: 20px; cursor: default; }
                    .search-container { position: relative; width: 500px; max-width: 90%; }
                    .search-input { width: 100%; padding: 15px 20px; font-size: 18px; border-radius: 30px; border: 1px solid #ddd; outline: none; box-shadow: 0 4px 10px rgba(0,0,0,0.1); transition: box-shadow 0.2s; box-sizing: border-box; }
                    .search-input:focus { box-shadow: 0 6px 15px rgba(0,0,0,0.15); }
                    .hint-text { margin-top: 15px; font-size: 13px; color: #999; text-align: center; }
                    .suggestions { position: absolute; top: 55px; left: 0; right: 0; background: white; border-radius: 15px; box-shadow: 0 4px 15px rgba(0,0,0,0.1); overflow: hidden; display: none; z-index: 100; text-align: left; }
                    .suggestion-item { padding: 10px 20px; cursor: pointer; font-size: 16px; }
                    .suggestion-item:hover { background-color: #eee; }
                    @media (prefers-color-scheme: dark) {
                        body { background-color: #1e1e1e; color: #e0e0e0; }
                        .search-input { background-color: #2d2d2d; border-color: #444; color: white; }
                        .suggestions { background-color: #2d2d2d; border: 1px solid #444; }
                        .suggestion-item:hover { background-color: #3d3d3d; }
                        .hint-text { color: #666; }
                    }
                </style>
            </head>
            <body>
                <div class='logo'>Ciallo ～(∠・ω< )⌒★</div>
                <div class='search-container'>
                    <input type='text' id='inputBox' class='search-input' placeholder='Search Bing...' autocomplete='off' />
                    <div class='hint-text'>输入网址请到最上面的地址栏哦 ↑</div>
                    <div id='list' class='suggestions'></div>
                </div>
                <script>
                    const inputBox = document.getElementById('inputBox'); const list = document.getElementById('list');
                    inputBox.addEventListener('input', function() {
                        const val = this.value; if (!val) { list.style.display = 'none'; return; }
                        const script = document.createElement('script');
                        script.src = 'https://api.bing.com/qsonhs.aspx?type=cb&q=' + encodeURIComponent(val) + '&cb=bingCallback';
                        document.body.appendChild(script);
                    });
                    inputBox.addEventListener('keydown', function(e) { if (e.key === 'Enter') doSearch(this.value); });
                    window.bingCallback = function(data) {
                        list.innerHTML = '';
                        if (data && data.AS && data.AS.Results && data.AS.Results.length > 0) {
                            data.AS.Results[0].Suggests.forEach(item => {
                                const div = document.createElement('div'); div.className = 'suggestion-item'; div.innerText = item.Txt;
                                div.onclick = function() { doSearch(item.Txt); }; list.appendChild(div);
                            }); list.style.display = 'block';
                        } else { list.style.display = 'none'; }
                    };
                    function doSearch(text) { if(text) window.location.href = 'https://www.bing.com/search?q=' + encodeURIComponent(text); }
                    document.addEventListener('click', function(e) { if (e.target !== inputBox) list.style.display = 'none'; });
                </script>
            </body>
            </html>";
            webView.NavigateToString(html);
        }
        // --- 核心导航 (兼容所有协议、防崩、无警告) ---
        void NavigateToSite()
        {
            string input = txtUrl.Text.Trim();
            
            // 1. 基础拦截
            if (string.IsNullOrEmpty(input) || input == "🏠 主页" || input.ToLower() == "about:blank") 
            {
                NavigateToHome(); 
                return;
            }

            // 2. 特殊协议处理
            if (input.StartsWith("view-source:", StringComparison.OrdinalIgnoreCase))
            {
                input = input.Substring("view-source:".Length);
            }

            string targetUrl = "";
            bool looksLikeSearch = false;

            // 3. 智能判断
            if (input.Contains(" ") || (!input.Contains(".") && !input.Contains(":/")))
            {
                looksLikeSearch = true;
            }
            else
            {
                targetUrl = input;
                // 正则判断是否缺协议头
                if (!System.Text.RegularExpressions.Regex.IsMatch(input, @"^[a-zA-Z0-9\+\.\-]+://"))
                {
                    targetUrl = "https://" + targetUrl;
                }
            }

            // 4. 执行导航 (防崩)
            try
            {
                if (looksLikeSearch)
                {
                    string searchUrl = "https://www.bing.com/search?q=" + System.Web.HttpUtility.UrlEncode(input);
                    webView.CoreWebView2.Navigate(searchUrl);
                }
                else
                {
                    webView.CoreWebView2.Navigate(targetUrl);
                }
            }
            catch (System.ArgumentException)
            {
                // 捕获无效格式错误 (如 xxx:https://)
                string fallbackUrl = "https://www.bing.com/search?q=" + System.Web.HttpUtility.UrlEncode(input);
                try { webView.CoreWebView2.Navigate(fallbackUrl); } catch { }
            }
            // 👇👇👇 修改了这里：去掉了 ex 变量，编译器就不唠叨啦！
            catch (Exception) 
            {
                // 捕获其他未知错误
                string fallbackUrl = "https://www.bing.com/search?q=" + System.Web.HttpUtility.UrlEncode(input);
                try { webView.CoreWebView2.Navigate(fallbackUrl); } catch { }
            }
        }
    }
}


