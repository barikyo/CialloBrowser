using System;
using System.Drawing;
using System.Windows.Forms;
using System.IO;
using Microsoft.Web.WebView2.Core;
using Microsoft.Web.WebView2.WinForms;
using Microsoft.Data.Sqlite;

namespace MyLovelyBrowser
{
    public class Form1 : Form
    {
        private WebView2 webView;
        private Panel topPanel;
        private TextBox txtUrl;
        private Button btnGo, btnBack, btnForward, btnRefresh, btnHome, btnHistory;

        private const string BrowserName = "Ciallo浏览器";

        public Form1()
        {
            this.Text = $"{BrowserName} - 初始化中...";
            this.Size = new Size(1200, 800);
            this.StartPosition = FormStartPosition.CenterScreen;
            try { this.Icon = new Icon("logo.ico"); } catch { }

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

            btnGo = new Button() { Text = "Go", Size = new Size(50, 30), Location = new Point(topPanel.Width - 65, 7), Anchor = AnchorStyles.Top | AnchorStyles.Right };
            btnGo.Click += (s, e) => NavigateToSite();
            topPanel.Controls.Add(btnGo);

            // --- 3. 地址栏 ---
            txtUrl = new TextBox() { 
                Location = new Point(215, 9), 
                Height = 30, 
                Font = new Font("Segoe UI", 10), 
                Width = topPanel.Width - 215 - 80, 
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

        private Button CreateButton(string text, int x)
        {
            return new Button() { Text = text, Location = new Point(x, 7), Size = new Size(35, 30) };
        }

        async void InitializeWebView()
        {
            await webView.EnsureCoreWebView2Async(null);

            webView.CoreWebView2.NewWindowRequested += (s, e) =>
            {
                e.Handled = true;
                webView.CoreWebView2.Navigate(e.Uri);
            };

            webView.SourceChanged += (s, e) =>
            {
                 if (!txtUrl.Focused) 
                 {
                     string src = webView.Source.ToString();
                     if (src.StartsWith("data:")) txtUrl.Text = "🏠 主页"; // 显示更友好的名字
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

        // --- 历史记录 (直连 SQLite) ---
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

            string exeName = AppDomain.CurrentDomain.FriendlyName; 
            string userDataFolder = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, exeName + ".WebView2");
            string dbPath = Path.Combine(userDataFolder, "EBWebView", "Default", "History");

            if (!File.Exists(dbPath))
            {
                listBox.Items.Add("这里空空如也... (还没开始浏览网页呢)");
            }
            else
            {
                try
                {
                    string connectionString = $"Data Source={dbPath};Mode=ReadOnly";
                    using (var connection = new SqliteConnection(connectionString))
                    {
                        connection.Open();
                        var command = connection.CreateCommand();
                        command.CommandText = "SELECT title, url FROM urls ORDER BY last_visit_time DESC LIMIT 50";
                        using (var reader = command.ExecuteReader())
                        {
                            while (reader.Read())
                            {
                                string title = reader.GetString(0);
                                string url = reader.GetString(1);
                                if(string.IsNullOrEmpty(title)) title = "无标题";
                                listBox.Items.Add($"{title} | {url}");
                            }
                        }
                    }
                }
                catch (Exception ex)
                {
                    listBox.Items.Add("读取失败: " + ex.Message);
                }
            }

            listBox.DoubleClick += (s, e) =>
            {
                if (listBox.SelectedItem != null)
                {
                    string item = listBox.SelectedItem.ToString();
                    int lastSplit = item.LastIndexOf('|');
                    if (lastSplit > 0 && lastSplit < item.Length - 1)
                    {
                        string targetUrl = item.Substring(lastSplit + 1).Trim();
                        webView.CoreWebView2.Navigate(targetUrl);
                        historyForm.Close();
                    }
                }
            };

            historyForm.Controls.Add(listBox);
            historyForm.ShowDialog(this);
        }

        // --- 主页 (新增提示语 + 布局优化) ---
        void NavigateToHome()
        {
            string html = @"
            <html>
            <head>
                <meta charset='utf-8'>
                <meta name='viewport' content='width=device-width, initial-scale=1.0'>
                <title>新标签页</title>
                <style>
                    body { 
                        font-family: 'Segoe UI', sans-serif; 
                        display: flex; flex-direction: column; align-items: center; justify-content: center; 
                        height: 100vh; margin: 0; 
                        background-color: #f9f9f9; color: #333; 
                        transition: background 0.3s, color 0.3s; 
                    }
                    .logo { font-size: 60px; margin-bottom: 20px; cursor: default; }
                    .search-container { position: relative; width: 500px; max-width: 90%; }
                    
                    .search-input { 
                        width: 100%; padding: 15px 20px; 
                        font-size: 18px; border-radius: 30px; border: 1px solid #ddd; 
                        outline: none; box-shadow: 0 4px 10px rgba(0,0,0,0.1); 
                        transition: box-shadow 0.2s; box-sizing: border-box; 
                    }
                    .search-input:focus { box-shadow: 0 6px 15px rgba(0,0,0,0.15); }
                    
                    /* 新增的提示语样式 */
                    .hint-text {
                        margin-top: 15px;
                        font-size: 13px;
                        color: #999;
                        text-align: center;
                    }

                    .suggestions { 
                        position: absolute; top: 55px; left: 0; right: 0; 
                        background: white; border-radius: 15px; 
                        box-shadow: 0 4px 15px rgba(0,0,0,0.1); 
                        overflow: hidden; display: none; z-index: 100; text-align: left; 
                    }
                    .suggestion-item { padding: 10px 20px; cursor: pointer; font-size: 16px; }
                    .suggestion-item:hover { background-color: #eee; }
                    
                    @media (prefers-color-scheme: dark) {
                        body { background-color: #1e1e1e; color: #e0e0e0; }
                        .search-input { background-color: #2d2d2d; border-color: #444; color: white; }
                        .suggestions { background-color: #2d2d2d; border: 1px solid #444; }
                        .suggestion-item:hover { background-color: #3d3d3d; }
                        /* 深色模式下的提示文字颜色 */
                        .hint-text { color: #666; }
                    }
                </style>
            </head>
            <body>
                <div class='logo'>Ciallo ～(∠・ω< )⌒★</div>
                
                <div class='search-container'>
                    <input type='text' id='inputBox' class='search-input' placeholder='Search Bing...' autocomplete='off' />
                    
                    <div class='hint-text'>这个是搜索框！输入网址请到最上面的地址栏哦 ↑</div>

                    <div id='list' class='suggestions'></div>
                </div>

                <script>
                    const inputBox = document.getElementById('inputBox'); 
                    const list = document.getElementById('list');
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

        // --- 核心导航逻辑 (拦截 about:blank) ---
        void NavigateToSite()
        {
            string input = txtUrl.Text.Trim();
            
            // 拦截 about:blank，回 HTML 主页
            if (string.IsNullOrEmpty(input) || input == "🏠 主页" || input.ToLower() == "about:blank") 
            {
                NavigateToHome();
                return;
            }

            string targetUrl = "";
            if (input.Contains(" ") || (!input.Contains(".") && !input.StartsWith("http"))) 
            {
                targetUrl = "https://www.bing.com/search?q=" + System.Web.HttpUtility.UrlEncode(input);
            }
            else 
            { 
                targetUrl = input; 
                if (!targetUrl.StartsWith("http://") && !targetUrl.StartsWith("https://")) 
                {
                    targetUrl = "https://" + targetUrl; 
                }
            }
            webView.CoreWebView2.Navigate(targetUrl);
        }
    }
}
