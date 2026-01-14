# 🌐 Ciallo Browser (Ciallo浏览器)

![Build Status](https://img.shields.io/github/actions/workflow/status/barikyo/CialloBrowser/build.yml?label=Build&style=flat-square)
![License](https://img.shields.io/badge/license-MIT-blue?style=flat-square)
![Platform](https://img.shields.io/badge/platform-Windows-0078D6?style=flat-square)
![.NET](https://img.shields.io/badge/.NET-8.0-512BD4?style=flat-square)

一个基于 **Microsoft WebView2 (Chromium)** 内核，使用 **C# WinForms** 打造的轻量级、现代化、极简风格浏览器。<br>
支持Windows10及更新系统。（支持.NET8.0 Runtime的系统可使用）

> **已废弃.NET 8.0版。需要安装 .NET 8.0 Runtime，对于新手来说，使用时并不方便，且对旧设备兼容性差。**
---
**注意：由于Webview2限制，可能无法播放HEVC视频。**
## ✨ 功能特性 (Features)

* **🚀 强劲内核**：基于 Edge Chromium (WebView2)，网页加载速度快，兼容性极佳。
* **🏠 智能主页**：
    * 内置精美起始页，支持 **系统深色/浅色模式** 自动切换。
    * 集成 Bing 搜索，支持 **实时搜索建议 (Search Suggestions)**。
    * 智能拦截 `about:blank`，防止出现空白页。
* **🔍 智能地址栏**：
    * 自动识别网址与搜索词。
    * 鼠标点击不强制全选，编辑更自由。
    * 输入联想提示。
* **🕰️ 历史记录**：
    * **直连内核数据库**：直接读取 WebView2 底层 SQLite 数据，无需手动记录文件。
    * **只读模式**：实时查看，不占用文件锁，稳定可靠。
* **🛡️ 隐私与安全**：
    * 无多余后台进程。
    * 防止恶意弹窗（拦截 `NewWindowRequested` 并在当前标签打开）。
* **🎨 个性化**：
    * 拥有独立的应用程序图标和版本信息。
    * 窗口标题栏智能跟随网页变化。

---

## 🛠️ 如何构建 (How to Build)

本项目支持 **GitHub Actions 云端自动构建**，您无需在本地安装 Visual Studio。

1.  **Fork** 本仓库。
2.  确保仓库根目录下包含 `logo.ico` 文件。
3.  点击仓库上方的 **Actions** 标签页。
4.  选择 **Build My Browser** 工作流并手动触发 (Workflow Dispatch)，或直接 Push 代码触发。
5.  构建完成后，在 Artifacts 中下载 `.exe` 压缩包即可运行。

### 本地构建需求
* Visual Studio 2022
* .NET 8.0 Desktop Development workload
* NuGet 包：`Microsoft.Web.WebView2`, `Microsoft.Data.Sqlite`

---

## 📝 待办事项 (TODO List)

为了让浏览器更加完美，我们计划在未来添加以下功能：

- [ ] **多标签页支持 (Multi-Tab Support)**
    - [ ] 实现标签栏 UI。
    - [ ] 支持 Ctrl+T / Ctrl+W 快捷键。
- [ ] **书签/收藏夹功能 (Bookmarks)**
    - [ ] 添加/删除收藏。
    - [ ] 书签栏显示。
- [ ] **下载管理 (Download Manager)**
    - [ ] 自定义下载路径。
    - [ ] 查看下载进度和历史。
- [ ] **设置界面 (Settings UI)**
    - [ ] 自定义主页搜索引擎（Google/Bilibili等）。
    - [ ] 清除缓存/Cookie 选项。
- [ ] **隐身模式 (Incognito Mode)**
    - [ ] 不记录历史和 Cookie 的浏览窗口。
- [ ] **网页缩放控制**
    - [ ] 状态栏增加缩放比例调节。

---

## 📄 许可证 (License)

本项目采用 [MIT License](LICENSE) 开源。
