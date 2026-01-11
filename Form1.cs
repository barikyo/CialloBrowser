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
        // 核心控件
        private WebView2 webView;
        private Panel topPanel;
        private TextBox txtUrl;
        private Button btnGo, btnBack, btnForward, btnRefresh, btnHistory;

        // 记录最后一次尝试的网址
        private string lastAttemptedUrl = "https://www.bing.com";
        
        // 历史记录文件名
        private const string HistoryFileName = "history.txt";

        public Form1()
        {
            this.Text = "正在初始化...";
            this.Size = new Size(1200, 800);
            this.StartPosition = FormStartPosition.CenterScreen;

            // 尝试设置图标（如果没有图标文件也不会报错）
            try { this.Icon = new Icon("logo.ico"); } catch { }

            // --- UI 布局 ---
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

            webView.CoreWebView2.HistoryChanged += (s, e) =>
            {
                btnBack.Enabled = webView.CanGoBack;
                btnForward.Enabled = webView.CanGoForward;
            };

            webView.SourceChanged += (s, e) =>
            {
                string currentSrc = webView.Source.ToString();
                if (!currentSrc.StartsWith("data:"))
                {
                    txtUrl.Text = currentSrc;
                    lastAttemptedUrl = currentSrc;
                }
            };

            webView.CoreWebView2.DocumentTitleChanged += (s, e) =>
            {
                string title = webView.CoreWebView2.DocumentTitle;
                if(string.IsNullOrEmpty(title)) title = "加载中...";
                this.Text = title + " - 主人的浏览器";
            };

            // 🔥 导航完成：处理错误 + 记录历史
            webView.CoreWebView2.NavigationCompleted += (s, e) =>
            {
                // 1. 错误处理逻辑
                bool isNetworkError = !e.IsSuccess;
                bool isHttpError = (e.HttpStatusCode >= 400);

                if (isNetworkError || isHttpError)
                {
                    string errorTitle = "哎呀，出错了";
                    string errorDesc = "";
                    string errorColor = "#ff6b6b"; // 默认红色

                    if (isNetworkError)
                    {
                        errorTitle = "无法连接到网络";
                        errorDesc = $"错误代码: {e.WebErrorStatus}";
                    }
                    else if (e.HttpStatusCode == 404)
                    {
                        errorTitle = "找不到页面 (404)";
                        errorDesc = "主人，这里什么都没有...是不是地址输错了？";
                        errorColor = "#fca311"; // 橙色
                    }
                    else if (e.HttpStatusCode == 403)
                    {
                        errorTitle = "禁止访问 (403)";
                        errorDesc = "这里是禁区！乐奈没有权限进去...";
                    }
                    else
                    {
                        errorTitle = $"服务器报错啦 ({e.HttpStatusCode})";
                        errorDesc = "对方服务器好像坏掉了...";
                    }

                    // 调用 3 个参数的函数
                    ShowErrorPage(errorTitle, errorDesc, errorColor);
                }
                else 
                {
                    // 2. 成功加载，记录历史
                    string currentUrl = webView.Source.ToString();
                    string currentTitle = webView.CoreWebView2.DocumentTitle;
                    
                    if (!currentUrl.StartsWith("data:") && !string.IsNullOrEmpty(currentTitle))
                    {
                        RecordHistory(currentTitle, currentUrl);
                    }
                }
            };

            webView.CoreWebView2.Navigate(lastAttemptedUrl);
        }

        // --- 历史记录 ---
        private void RecordHistory(string title, string url)
        {
            try
            {
                string logLine = $"{DateTime.Now:MM-dd HH:mm}|{title}|{url}{Environment.NewLine}";
                File.AppendAllText(HistoryFileName, logLine);
            }
            catch { }
        }

        private void ShowHistoryWindow()
        {
            Form historyForm = new Form();
            historyForm.Text = "浏览足迹";
            historyForm.Size = new Size(600, 400);
            historyForm.StartPosition = FormStartPosition.CenterParent;
            // 尝试给历史窗口也加个图标
            try { historyForm.Icon = this.Icon; } catch { }

            ListBox listBox = new ListBox();
            listBox.Dock = DockStyle.Fill;
            listBox.Font = new Font("Segoe UI", 10);
            
            if (File.Exists(HistoryFileName))
            {
                string[] lines = File.ReadAllLines(HistoryFileName);
                Array.Reverse(lines);
                listBox.Items.AddRange(lines);
            }
            else
            {
                listBox.Items.Add("还没有去过任何地方哦...");
            }

            listBox.DoubleClick += (s, e) =>
            {
                if (listBox.SelectedItem != null)
                {
                    string item = listBox.SelectedItem.ToString();
                    string[] parts = item.Split('|');
                    if (parts.Length >= 3)
                    {
                        webView.CoreWebView2.Navigate(parts[2]);
                        historyForm.Close();
                    }
                }
            };

            historyForm.Controls.Add(listBox);
            historyForm.ShowDialog(this);
        }

        // 🔥🔥🔥 修复重点：这里加回了 color 参数 🔥🔥🔥
        private void ShowErrorPage(string title, string desc, string color)
        {
            string htmlContent = $@"
                <html>
                <head>
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
                        <p style='font-size: 14px; color: #999;'>您可以点击上方的刷新按钮重试哦</p>
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
