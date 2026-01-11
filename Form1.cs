using System;
using System.Drawing;
using System.Windows.Forms;
using System.IO; 
using Microsoft.Web.WebView2.Core;
using Microsoft.Web.WebView2.WinForms;

namespace MyLovelyBrowser
{
    public class Form1 : Form
    {
        private WebView2 webView;
        private Panel topPanel;
        private TextBox txtUrl;
        private Button btnGo, btnBack, btnForward, btnRefresh, btnHistory;

        // 历史记录改为绝对路径，确保一定能写进去
        private string historyPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "history.txt");
        private string lastAttemptedUrl = "https://www.bing.com";

        public Form1()
        {
            this.Text = "Ciallo浏览器 - 初始化中...";
            this.Size = new Size(1200, 800);
            this.StartPosition = FormStartPosition.CenterScreen;
            try { this.Icon = new Icon("logo.ico"); } catch { }

            // --- 界面布局 ---
            topPanel = new Panel() { Dock = DockStyle.Top, Height = 45, Padding = new Padding(5), BackColor = Color.WhiteSmoke };
            this.Controls.Add(topPanel);

            btnBack = CreateButton("←", 10, false);
            btnBack.Click += (s, e) => { if (webView.CanGoBack) webView.GoBack(); };
            topPanel.Controls.Add(btnBack);

            btnForward = CreateButton("→", 50, false);
            btnForward.Click += (s, e) => { if (webView.CanGoForward) webView.GoForward(); };
            topPanel.Controls.Add(btnForward);

            btnRefresh = CreateButton("↻", 90, true);
            btnRefresh.Click += (s, e) => 
            {
                // 如果当前是错误页，刷新时重试上次的网址
                if (webView.Source.ToString().StartsWith("data:")) webView.CoreWebView2.Navigate(lastAttemptedUrl);
                else webView.Reload(); 
            };
            topPanel.Controls.Add(btnRefresh);

            btnHistory = CreateButton("H", 130, true);
            btnHistory.Click += (s, e) => ShowHistoryWindow();
            topPanel.Controls.Add(btnHistory);

            btnGo = new Button() { Text = "Go", Size = new Size(50, 30), Location = new Point(topPanel.Width - 65, 7), Anchor = AnchorStyles.Top | AnchorStyles.Right };
            btnGo.Click += (s, e) => NavigateToSite();
            topPanel.Controls.Add(btnGo);

            txtUrl = new TextBox() { Location = new Point(180, 9), Height = 30, Font = new Font("Segoe UI", 10), Width = topPanel.Width - 180 - 80, Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right };
            txtUrl.KeyDown += (s, e) => { if (e.KeyCode == Keys.Enter) NavigateToSite(); };
            txtUrl.Click += (s, e) => txtUrl.SelectAll();
            topPanel.Controls.Add(txtUrl);

            webView = new WebView2() { Dock = DockStyle.Fill };
            this.Controls.Add(webView);
            webView.BringToFront();

            InitializeWebView();
        }

        private Button CreateButton(string text, int x, bool enabled)
        {
            return new Button() { Text = text, Location = new Point(x, 7), Size = new Size(35, 30), Enabled = enabled };
        }

        async void InitializeWebView()
        {
            await webView.EnsureCoreWebView2Async(null);

            // 🔥🔥🔥 核心修复 1：禁止弹出新窗口，强制在当前窗口跳转 🔥🔥🔥
            // 这解决了地址栏不更新、历史记录不生效、点链接跳出窗口的所有问题
            webView.CoreWebView2.NewWindowRequested += (s, e) =>
            {
                e.Handled = true; // 告诉浏览器：你别弹窗，我来处理
                webView.CoreWebView2.Navigate(e.Uri); // 在当前窗口打开该链接
            };

            // 历史后退检查
            webView.CoreWebView2.HistoryChanged += (s, e) =>
            {
                btnBack.Enabled = webView.CanGoBack;
                btnForward.Enabled = webView.CanGoForward;
            };

            // 地址栏同步
            webView.SourceChanged += (s, e) =>
            {
                string currentSrc = webView.Source.ToString();
                // 只有不是错误页(data:)的时候才更新地址栏，避免地址栏显示乱七八糟的代码
                if (!currentSrc.StartsWith("data:"))
                {
                    txtUrl.Text = currentSrc;
                    lastAttemptedUrl = currentSrc; // 更新“上次尝试的网址”
                }
            };

            // 标题同步
            webView.CoreWebView2.DocumentTitleChanged += (s, e) =>
            {
                string title = webView.CoreWebView2.DocumentTitle;
                if(string.IsNullOrEmpty(title)) title = "加载中...";
                this.Text = title;
            };

            // 🔥🔥🔥 核心修复 2：错误页面拦截逻辑优化 🔥🔥🔥
            webView.CoreWebView2.NavigationCompleted += (s, e) =>
            {
                // 排除空白页和 data 页
                if (webView.Source.ToString().StartsWith("data:") || webView.Source.ToString() == "about:blank") return;

                bool isNetworkError = !e.IsSuccess;
                bool isHttpError = (e.HttpStatusCode >= 400);

                if (isNetworkError || isHttpError)
                {
                    string errorTitle = "哎呀，出错了";
                    string errorDesc = "";
                    string errorColor = "#ff6b6b"; 

                    if (isNetworkError)
                    {
                        errorTitle = "无法连接到网络";
                        errorDesc = $"错误代码: {e.WebErrorStatus}";
                    }
                    else if (e.HttpStatusCode == 404)
                    {
                        errorTitle = "找不到页面 (404)";
                        errorDesc = "主人，您要去的地方好像是一片荒原...";
                        errorColor = "#fca311";
                    }
                    else if (e.HttpStatusCode == 403)
                    {
                        errorTitle = "禁止访问 (403)";
                        errorDesc = "这里是禁区！乐奈没有权限进去...";
                    }
                    else
                    {
                        errorTitle = $"服务器报错 ({e.HttpStatusCode})";
                        errorDesc = "服务器好像冒烟了...";
                    }
                    
                    ShowErrorPage(errorTitle, errorDesc, errorColor);
                }
                else
                {
                    // 只有成功才记录历史
                    string title = webView.CoreWebView2.DocumentTitle;
                    if (string.IsNullOrEmpty(title)) title = "网页";
                    RecordHistory(title, webView.Source.ToString());
                }
            };

            webView.CoreWebView2.Navigate(lastAttemptedUrl);
        }

        // --- 历史记录 (修复路径问题) ---
        private void RecordHistory(string title, string url)
        {
            try
            {
                // 忽略 data: 页面
                if (url.StartsWith("data:")) return;

                string logLine = $"{DateTime.Now:MM-dd HH:mm}|{title}|{url}{Environment.NewLine}";
                // 使用 AppendAllText 会自动创建文件
                File.AppendAllText(historyPath, logLine);
            }
            catch(Exception ex) 
            {
                // 可以在这里打断点调试，但在生产环境静默失败防止崩溃
                System.Diagnostics.Debug.WriteLine("写历史失败: " + ex.Message);
            }
        }

        private void ShowHistoryWindow()
        {
            Form historyForm = new Form();
            historyForm.Text = "浏览足迹";
            historyForm.Size = new Size(600, 400);
            historyForm.StartPosition = FormStartPosition.CenterParent;
            try { historyForm.Icon = this.Icon; } catch { }

            ListBox listBox = new ListBox();
            listBox.Dock = DockStyle.Fill;
            listBox.Font = new Font("Segoe UI", 10);
            
            if (File.Exists(historyPath))
            {
                string[] lines = File.ReadAllLines(historyPath);
                Array.Reverse(lines); // 最新的在上面
                listBox.Items.AddRange(lines);
            }
            else
            {
                listBox.Items.Add($"还没有历史记录哦 (文件路径: {historyPath})");
            }

            listBox.DoubleClick += (s, e) =>
            {
                if (listBox.SelectedItem != null)
                {
                    string item = listBox.SelectedItem.ToString();
                    string[] parts = item.Split('|');
                    if (parts.Length >= 3)
                    {
                        string targetUrl = parts[2];
                        webView.CoreWebView2.Navigate(targetUrl);
                        historyForm.Close();
                    }
                }
            };

            historyForm.Controls.Add(listBox);
            historyForm.ShowDialog(this);
        }

        // --- 错误页生成 ---
        private void ShowErrorPage(string title, string desc, string color)
        {
            string htmlContent = $@"
                <html>
                <head>
                    <meta name='viewport' content='initial-scale=1,minimum-scale=1,width=device-width,interactive-widget=resizes-content'>
					<meta charset='utf-8'>
                    <style>
                        body {{ font-family: 'Segoe UI', sans-serif; background-color: #f0f2f5; display: flex; justify-content: center; align-items: center; height: 100vh; margin: 0; }}
                        .container {{ text-align: center; background: white; padding: 40px; border-radius: 20px; box-shadow: 0 4px 15px rgba(0,0,0,0.1); max-width: 500px; }}
                        h1 {{ color: {color}; margin-bottom: 10px; font-size: 32px; }} 
                        p {{ color: #666; font-size: 18px; margin-bottom: 30px; }}
                        .icon {{ font-size: 80px; margin-bottom: 20px; }}
                    </style>
                </head>
                <body>
                    <div class='container'>
                        <div class='icon'>(＞﹏＜)</div>
                        <h1>{title}</h1>
                        <p>{desc}</p>
                        <p style='font-size: 14px; color: #999;'>您可以点击上方的刷新按钮重试哦~</p>
                    </div>
                </body>
                </html>";

            webView.NavigateToString(htmlContent);
        }

        void NavigateToSite()
        {
            string url = txtUrl.Text.Trim();
            if (!string.IsNullOrEmpty(url))
            {
                if (!url.StartsWith("http://") && !url.StartsWith("https://")) url = "https://" + url;
                lastAttemptedUrl = url;
                webView.CoreWebView2.Navigate(url);
            }
        }
    }
}
