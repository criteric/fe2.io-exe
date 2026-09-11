using System;
using System.Drawing;
using System.Windows.Forms;
using Microsoft.Web.WebView2.Core;
using Microsoft.Web.WebView2.WinForms;

namespace FE2IODesktop;

internal static class Program
{
    [STAThread]
    static void Main()
    {
        ApplicationConfiguration.Initialize();
        Application.Run(new MainForm());
    }
}

public sealed class MainForm : Form
{
    private readonly WebView2 webView;
    private readonly ToolStripStatusLabel status;

    public MainForm()
    {
        Text = "FE2.IO Desktop";
        Width = 1280;
        Height = 800;
        MinimumSize = new Size(900, 600);
        StartPosition = FormStartPosition.CenterScreen;

        var toolbar = new ToolStrip();

        var back = new ToolStripButton("←");
        back.Click += (_, _) => { if (webView.CanGoBack) webView.GoBack(); };

        var forward = new ToolStripButton("→");
        forward.Click += (_, _) => { if (webView.CanGoForward) webView.GoForward(); };

        var reload = new ToolStripButton("⟳");
        reload.Click += (_, _) => webView.Reload();

        var home = new ToolStripButton("FE2.IO");
        home.Click += (_, _) => webView.CoreWebView2?.Navigate("https://fe2.io/");

        toolbar.Items.Add(back);
        toolbar.Items.Add(forward);
        toolbar.Items.Add(reload);
        toolbar.Items.Add(new ToolStripSeparator());
        toolbar.Items.Add(home);
        Controls.Add(toolbar);

        var statusBar = new StatusStrip();
        status = new ToolStripStatusLabel("FE2.IO yükleniyor...");
        statusBar.Items.Add(status);
        Controls.Add(statusBar);

        webView = new WebView2
        {
            Dock = DockStyle.Fill,
            CreationProperties = new CoreWebView2CreationProperties()
        };
        Controls.Add(webView);
        webView.BringToFront();

        Load += async (_, _) =>
        {
            try
            {
                await webView.EnsureCoreWebView2Async();
                webView.CoreWebView2.Settings.AreDefaultContextMenusEnabled = true;
                webView.CoreWebView2.Settings.AreDevToolsEnabled = true;
                webView.CoreWebView2.Settings.IsStatusBarEnabled = false;

                webView.CoreWebView2.NavigationStarting += (_, e) =>
                    status.Text = "Yükleniyor: " + e.Uri;

                webView.CoreWebView2.NavigationCompleted += (_, e) =>
                    status.Text = e.IsSuccess ? "FE2.IO hazır" : "Sayfa yüklenemedi";

                webView.CoreWebView2.NewWindowRequested += (_, e) =>
                {
                    e.Handled = true;
                    webView.CoreWebView2.Navigate(e.Uri);
                };

                webView.CoreWebView2.Navigate("https://fe2.io/");
            }
            catch (Exception ex)
            {
                MessageBox.Show(
                    "WebView2 başlatılamadı.\n\n" + ex.Message +
                    "\n\nMicrosoft Edge WebView2 Runtime'ın kurulu olduğundan emin ol.",
                    "FE2.IO Desktop",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Error);
            }
        };
    }
}
