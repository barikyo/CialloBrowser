using System;
using System.Drawing;
using System.Windows.Forms;
using System.IO;
using System.Runtime.InteropServices;
using Microsoft.Win32;
using Microsoft.Web.WebView2.Core;
using Microsoft.Web.WebView2.WinForms;
using Microsoft.Data.Sqlite;
using System.Collections.Generic; // Framework 需要引用
using System.Threading.Tasks;     // 异步

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

            // 智能查找图标
            try 
            {
                var assembly = System.Reflection.Assembly.GetExecutingAssembly();
                foreach (string name in assembly.GetManifestResourceNames())
                {
                    if (name.EndsWith("logo.ico"))
                    {
                        using (var stream = assembly.GetManifestResourceStream(name))
                        {
                            if (stream != null) this.Icon = new Icon(stream);
                        }
                        break;
                    }
                }
            } 
            catch { }

            // --- 1. 顶部面板 ---
            topPanel = new Panel() { Dock = DockStyle.Top, Height = 45, Padding = new Padding(5) };
            this.Controls.Add(topPanel);

            // --- 2. 按钮群 ---
            btnBack = CreateButton("←", 10);
            btnBack.Click += (s, e) => { if (webView != null && webView.CoreWebView2 != null && webView.CanGoBack) webView.GoBack(); };
            topPanel.Controls.Add(btnBack);

            btnForward = CreateButton("→", 50);
            btnForward.Click += (s, e) => { if (webView != null && webView.CoreWebView2 != null && webView.CanGoForward) webView.GoForward(); };
            topPanel.Controls.Add(btnForward);

            btnRefresh = CreateButton("↻", 90);
            btnRefresh.Click += (s, e) => { if(webView != null && webView.CoreWebView2 != null) webView.Reload(); };
            topPanel.Controls.Add(btnRefresh);

            btnHome = CreateButton("🏠", 130);
            btnHome.Click += (s, e) => NavigateToHome();
            topPanel.Controls.Add(btnHome);

            btnHistory = CreateButton("H", 170);
            btnHistory.Click += (s, e) => ShowHistoryWindow();
            topPanel.Controls.Add(btnHistory);

            btnClear = CreateButton("🧹", 210);
            btnClear.Click += (s, e) => ShowClearDataDialog(); 
            topPanel.Controls.Add(btnClear);

            btnGo = new Button() { 
                Text = "Go", 
                Size = new Size(50, 30), 
                Location = new Point(topPanel.Width - 65, 7), 
                Anchor = AnchorStyles.Top | AnchorStyles.Right,
                FlatStyle = FlatStyle.Flat,
                FlatAppearance = { BorderSize = 0 }
            };
            btnGo.Click += (s, e) => NavigateToSite();
            topPanel.Controls.Add(btnGo);

            // --- 3. 地址栏 ---
            txtUrl = new TextBox() { 
                Location = new Point(255, 9), 
                Height = 30, 
                Font = new Font("Segoe UI", 10), 
                Width = topPanel.Width - 255 - 80, 
                Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right,
                BorderStyle = BorderStyle.FixedSingle
            };
            
            txtUrl.KeyDown += (s, e) => { if (e.KeyCode == Keys.Enter) NavigateToSite(); };
            txtUrl.DoubleClick += (s, e) => txtUrl.SelectAll();
            topPanel.Controls.Add(txtUrl);

            // --- 4. 浏览器主体 ---
            webView = new WebView2() { Dock = DockStyle.Fill };
            this.Controls.Add(webView);
            webView.BringToFront();

            // 监听颜色变化
            ApplyThemeBasedOnSystem();
            SystemEvents.UserPreferenceChanged += (s, e) => 
            {
                if (e.Category == UserPreferenceCategory.General)
                {
                    // Framework 版本这里最好加个 Invoke 检查，防止线程冲突
                    if (this.IsHandleCreated) this.Invoke(new Action(() => ApplyThemeBasedOnSystem()));
                }
            };

            InitializeWebView();
        }

        private Button CreateButton(string text, int x)
        {
            return new Button() { 
                Text = text, Location = new Point(x, 7), Size = new Size(35, 30),
                FlatStyle = FlatStyle.Flat, FlatAppearance = { BorderSize = 0 }
            };
        }

        // --- 深色模式逻辑 ---
        [DllImport("dwmapi.dll")]
        private static extern int DwmSetWindowAttribute(IntPtr hwnd, int attr, ref int attrValue, int attrSize);
        private const int DWMWA_USE_IMMERSIVE_DARK_MODE = 20;

        private void ApplyThemeBasedOnSystem()
        {
            try
            {
                bool isDarkMode = false;
                using (var key = Registry.CurrentUser.OpenSubKey(@"Software\Microsoft\Windows\CurrentVersion\Themes\Personalize"))
                {
                    if (key != null) { var val = key.GetValue("AppsUseLightTheme"); if (val is int i && i == 0) isDarkMode = true; }
                }

                int useImmersiveDarkMode = isDarkMode ? 1 : 0;
                DwmSetWindowAttribute(this.Handle, DWMWA_USE_IMMERSIVE_DARK_MODE, ref useImmersiveDarkMode, sizeof(int));

                if (isDarkMode)
                {
                    this.BackColor = Color.FromArgb(32, 32, 32);     
                    topPanel.BackColor = Color.FromArgb(45, 45, 48); 
                    txtUrl.BackColor = Color.FromArgb(30, 30, 30);
                    txtUrl.ForeColor = Color.White;
                    foreach(Control c in topPanel.Controls) { if(c is Button btn) { btn.BackColor = Color.FromArgb(60, 60, 60); btn.ForeColor = Color.White; } }
                    btnClear.ForeColor = Color.FromArgb(255, 100, 100); 
                }
                else 
                {
                    this.BackColor = SystemColors.Control;
                    topPanel.BackColor = Color.WhiteSmoke;
                    txtUrl.BackColor = Color.White;
                    txtUrl.ForeColor = Color.Black;
                    foreach(Control c in topPanel.Controls) { if(c is Button btn) { btn.BackColor = Color.Transparent; btn.ForeColor = Color.Black; } }
                    btnClear.ForeColor = Color.Red;
                }
                topPanel.Invalidate();
            }
            catch { }
        }

        async void InitializeWebView()
        {
            try
            {
                var options = new CoreWebView2EnvironmentOptions();
                
                // 修正说明
                // 1. 删除了 --use-gl=desktop
                // 2. 删除了 VaapiVideoDecoding (这是 Linux 用的，Windows 用不上)
                // 3. 保留了 D3D11VideoDecoder (这是 Windows 硬件解码的核心)
                // 4. 保留了 AV1 和 HEVC 的开启指令
                
                string args = "--enable-features=D3D11VideoDecoder,HevcVideoDecoding,Av1VideoDecoding,PlatformHEVCDecoderSupport,MsPlayReady " +
                              "--ignore-gpu-blocklist " +
                              "--disable-gpu-driver-bug-workarounds " +
                              "--enable-gpu-rasterization " +
                              "--force-gpu-rasterization";

                options.AdditionalBrowserArguments = args;
                
                var env = await CoreWebView2Environment.CreateAsync(null, fixedUserDataFolder, options);
                await webView.EnsureCoreWebView2Async(env);
                
                // 伪装 User-Agent (为了让 B站 识别)
                var settings = webView.CoreWebView2.Settings;
                settings.UserAgent = "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/130.0.0.0 Safari/537.36 Edg/130.0.0.0";

                webView.CoreWebView2.NewWindowRequested += (s, e) => { e.Handled = true; webView.CoreWebView2.Navigate(e.Uri); };
                webView.SourceChanged += (s, e) => { if (!txtUrl.Focused) { string src = webView.Source.ToString(); if (src.StartsWith("data:")) txtUrl.Text = "🏠 主页"; else txtUrl.Text = src; } };
                webView.CoreWebView2.DocumentTitleChanged += (s, e) => { string t = webView.CoreWebView2.DocumentTitle; this.Text = (string.IsNullOrEmpty(t) || t == "about:blank") ? BrowserName : $"{t} - {BrowserName}"; };
                
                NavigateToHome();
            }
            catch (Exception ex)
            {
                MessageBox.Show("呜...WebView2 初始化失败！请确保电脑上安装了 WebView2 Runtime喵。\n错误: " + ex.Message);
            }
        }

        // --- 核心导航 ---
        void NavigateToSite()
        {
            string input = txtUrl.Text.Trim();
            if (string.IsNullOrEmpty(input) || input == "🏠 主页" || input.ToLower() == "about:blank") { NavigateToHome(); return; }
            if (input.StartsWith("view-source:", StringComparison.OrdinalIgnoreCase)) input = input.Substring("view-source:".Length);

            string targetUrl = "";
            bool looksLikeSearch = false;

            if (input.Contains(" ") || (!input.Contains(".") && !input.Contains(":/"))) looksLikeSearch = true;
            else
            {
                targetUrl = input;
                if (!System.Text.RegularExpressions.Regex.IsMatch(input, @"^[a-zA-Z0-9\+\.\-]+://")) targetUrl = "https://" + targetUrl;
            }

            try
            {
                if (looksLikeSearch) webView.CoreWebView2.Navigate("https://www.bing.com/search?q=" + System.Web.HttpUtility.UrlEncode(input));
                else webView.CoreWebView2.Navigate(targetUrl);
            }
            catch (ArgumentException) { try { webView.CoreWebView2.Navigate("https://www.bing.com/search?q=" + System.Web.HttpUtility.UrlEncode(input)); } catch { } }
            catch (Exception) { try { webView.CoreWebView2.Navigate("https://www.bing.com/search?q=" + System.Web.HttpUtility.UrlEncode(input)); } catch { } }
        }

        // --- 历史记录 ---
        private void ShowHistoryWindow()
        {
            Form historyForm = new Form(); historyForm.Text = "历史记录"; historyForm.Size = new Size(800, 500); historyForm.StartPosition = FormStartPosition.CenterParent;
            try { historyForm.Icon = this.Icon; } catch { }
            ListBox listBox = new ListBox(); listBox.Dock = DockStyle.Fill; listBox.Font = new Font("Segoe UI", 10); listBox.IntegralHeight = false;
            
            string dbPath = Path.Combine(fixedUserDataFolder, "EBWebView", "Default", "History");
            string tempDbPath = Path.GetTempFileName(); 

            if (!File.Exists(dbPath)) { listBox.Items.Add($"暂无记录喵"); }
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
                } catch (Exception ex) { listBox.Items.Add("读取失败: " + ex.Message); }
                finally { try { GC.Collect(); GC.WaitForPendingFinalizers(); if (File.Exists(tempDbPath)) File.Delete(tempDbPath); } catch { } }
            }
            listBox.DoubleClick += (s, e) => { if (listBox.SelectedItem != null) { string item = listBox.SelectedItem.ToString(); int lastSplit = item.LastIndexOf('|'); if (lastSplit > 0) webView.CoreWebView2.Navigate(item.Substring(lastSplit + 1).Trim()); historyForm.Close(); } };
            historyForm.Controls.Add(listBox); historyForm.ShowDialog(this);
        }

        // --- 清理面板 ---
        private void ShowClearDataDialog()
        {
            Form clearForm = new Form(); clearForm.Text = "清除浏览数据喵"; clearForm.Size = new Size(350, 300);
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
            Button btnConfirm = new Button() { Text = "立即清除！", Location = new Point(120, 210), Size = new Size(100, 35), BackColor = Color.MistyRose };
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
                    <div class='hint-text'>输入网址请到最上面的地址栏喵 ↑</div>
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
    }
}



