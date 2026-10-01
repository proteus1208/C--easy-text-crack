using System.Text.RegularExpressions;

namespace AllowedMac;

sealed class MainForm : Form
{
    static string SectionPattern =>
        "(" + Regex.Escape(Guard.BeginMarker) + "\\s*_A = \")([^\"]*)(\"\\s*_B = \")([^\"]*)(\")";

    readonly Label _versionLabel = new();
    readonly Label _welcomeLabel = new();
    readonly Label _macLabel = new();
    readonly ContextMenuStrip _pasteMenu = new();
    string _mac = "";
    bool _editing;
    bool _blocked;

    public MainForm()
    {
        Guard.RefuseDebug();
        Text = "Welcome";
        StartPosition = FormStartPosition.CenterScreen;
        FormBorderStyle = FormBorderStyle.FixedDialog;
        MaximizeBox = false;
        MinimizeBox = false;
        ClientSize = new Size(280, 120);
        KeyPreview = true;

        _welcomeLabel.Text = "welcom!";
        _welcomeLabel.Font = new Font("Segoe UI", 14f);
        _welcomeLabel.AutoSize = true;
        _welcomeLabel.TextAlign = ContentAlignment.MiddleCenter;

        _versionLabel.Text = "Version 1.0";
        _versionLabel.Font = new Font("Segoe UI", 8f);
        _versionLabel.AutoSize = true;
        _versionLabel.ForeColor = SystemColors.GrayText;

        _macLabel.Font = new Font("Consolas", 12f);
        _macLabel.AutoSize = false;
        _macLabel.TextAlign = ContentAlignment.MiddleCenter;
        _macLabel.Visible = false;
        _macLabel.Text = "";

        var pasteItem = new ToolStripMenuItem("Paste");
        pasteItem.Click += (_, _) => PasteMac();
        _pasteMenu.Items.Add(pasteItem);

        Controls.Add(_welcomeLabel);
        Controls.Add(_versionLabel);
        Controls.Add(_macLabel);

        KeyPress += OnKeyPress;
        KeyDown += OnKeyDown;
        Shown += (_, _) => LayoutWelcome();
    }

    void LayoutWelcome()
    {
        _welcomeLabel.Left = (ClientSize.Width - _welcomeLabel.Width) / 2;
        _welcomeLabel.Top = 28;
        _versionLabel.Left = (ClientSize.Width - _versionLabel.Width) / 2;
        _versionLabel.Top = _welcomeLabel.Bottom + 12;
        _macLabel.SetBounds(12, 36, ClientSize.Width - 24, 28);
    }

    void PasteMac()
    {
        if (!_editing || !Clipboard.ContainsText())
            return;

        var pasted = Clipboard.GetText().Replace("\r", "").Replace("\n", "").Trim();
        _mac += pasted;
        _macLabel.Text = _mac;
    }

    protected override bool ProcessCmdKey(ref Message msg, Keys keyData)
    {
        if (AcceptOnlyAt(keyData))
            return true;

        return base.ProcessCmdKey(ref msg, keyData);
    }

    protected override bool ProcessDialogKey(Keys keyData)
    {
        if (AcceptOnlyAt(keyData))
            return true;

        return base.ProcessDialogKey(keyData);
    }

    bool AcceptOnlyAt(Keys keyData)
    {
        if (_editing)
            return false;

        if (_blocked)
            return true;

        var code = keyData & Keys.KeyCode;
        if (code is Keys.ShiftKey or Keys.LShiftKey or Keys.RShiftKey
            or Keys.ControlKey or Keys.LControlKey or Keys.RControlKey
            or Keys.Menu or Keys.LMenu or Keys.RMenu)
            return false;

        var mods = keyData & Keys.Modifiers;
        if (mods == Keys.None || mods == Keys.Shift)
            return false;

        _blocked = true;
        return true;
    }

    void OnKeyPress(object? sender, KeyPressEventArgs e)
    {
        if (_blocked)
        {
            e.Handled = true;
            return;
        }

        if (!_editing)
        {
            if (e.KeyChar == '@')
            {
                _editing = true;
                _mac = "";
                _welcomeLabel.Visible = false;
                _macLabel.Text = "";
                _macLabel.Visible = true;
                _macLabel.ContextMenuStrip = _pasteMenu;
            }
            else
            {
                _blocked = true;
            }

            e.Handled = true;
            return;
        }

        if (e.KeyChar == '#')
        {
            e.Handled = true;
            Finish();
            return;
        }

        if (e.KeyChar == '@' || char.IsControl(e.KeyChar))
        {
            e.Handled = true;
            return;
        }

        _mac += e.KeyChar;
        _macLabel.Text = _mac;
        e.Handled = true;
    }

    void OnKeyDown(object? sender, KeyEventArgs e)
    {
        if (_blocked || !_editing)
        {
            if (!_editing && !_blocked && IsBareCommandKey(e))
            {
                _blocked = true;
            }

            e.Handled = true;
            e.SuppressKeyPress = _blocked;
            return;
        }

        if (e.KeyCode == Keys.Insert && e.Shift)
        {
            PasteMac();
            e.Handled = true;
            e.SuppressKeyPress = true;
            return;
        }

        if (e.Control && e.KeyCode == Keys.V)
        {
            PasteMac();
            e.Handled = true;
            e.SuppressKeyPress = true;
            return;
        }

        if (e.KeyCode == Keys.Back && _mac.Length > 0)
        {
            _mac = _mac[..^1];
            _macLabel.Text = _mac;
            e.Handled = true;
            e.SuppressKeyPress = true;
        }
    }

    static bool IsBareCommandKey(KeyEventArgs e)
    {
        if (e.Control || e.Alt)
            return false;

        return e.KeyCode is Keys.Enter or Keys.Escape or Keys.Tab or Keys.Back
            or Keys.Insert or Keys.Delete or Keys.Left or Keys.Right or Keys.Up or Keys.Down
            or Keys.Home or Keys.End or Keys.PageUp or Keys.PageDown
            or Keys.F1 or Keys.F2 or Keys.F3 or Keys.F4 or Keys.F5 or Keys.F6
            or Keys.F7 or Keys.F8 or Keys.F9 or Keys.F10 or Keys.F11 or Keys.F12;
    }

    void Finish()
    {
        if (!_editing)
            return;

        Guard.RefuseDebug();
        var normalized = NormalizeMac(_mac);
        if (normalized is null)
        {
            MessageBox.Show(
                this,
                "Enter a MAC address like AA-BB-CC-DD-EE-FF.",
                "Allowed MAC",
                MessageBoxButtons.OK,
                MessageBoxIcon.Information);
            return;
        }

        var path = FindPythonFile();
        if (path is null)
        {
            MessageBox.Show(
                this,
                "Python file with the allowed MAC section was not found.",
                "Allowed MAC",
                MessageBoxButtons.OK,
                MessageBoxIcon.Warning);
            return;
        }

        var digest = Guard.Digest(normalized);
        var text = File.ReadAllText(path);
        var updated = Regex.Replace(
            text,
            SectionPattern,
            m => m.Groups[1].Value + digest + m.Groups[3].Value + Guard.Sign(digest) + m.Groups[5].Value,
            RegexOptions.CultureInvariant);

        if (!updated.Contains($"_A = \"{digest}\"", StringComparison.Ordinal))
        {
            MessageBox.Show(
                this,
                "The allowed MAC section could not be updated.",
                "Allowed MAC",
                MessageBoxButtons.OK,
                MessageBoxIcon.Warning);
            return;
        }

        File.WriteAllText(path, updated);
        Close();
    }

    static string? NormalizeMac(string raw)
    {
        var hex = new string(raw.Where(c => Uri.IsHexDigit(c)).ToArray()).ToUpperInvariant();
        if (hex.Length != 12)
            return null;

        return string.Join("-", Enumerable.Range(0, 6).Select(i => hex.Substring(i * 2, 2)));
    }

    static string? FindPythonFile()
    {
        var seen = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        foreach (var start in new[] { Environment.CurrentDirectory, AppContext.BaseDirectory })
        {
            var dir = new DirectoryInfo(start);
            for (var i = 0; i < 8 && dir is not null; i++, dir = dir.Parent)
            {
                if (!seen.Add(dir.FullName))
                    continue;

                string? fallback = null;
                foreach (var file in EnumeratePythonFiles(dir.FullName))
                {
                    string text;
                    try
                    {
                        text = File.ReadAllText(file);
                    }
                    catch
                    {
                        continue;
                    }

                    if (!text.Contains(Guard.BeginMarker, StringComparison.Ordinal))
                        continue;

                    if (file.EndsWith($"{Path.DirectorySeparatorChar}src{Path.DirectorySeparatorChar}app.py", StringComparison.OrdinalIgnoreCase))
                        return file;

                    fallback ??= file;
                }

                if (fallback is not null)
                    return fallback;
            }
        }

        return null;
    }

    static IEnumerable<string> EnumeratePythonFiles(string root)
    {
        var stack = new Stack<string>();
        stack.Push(root);
        while (stack.Count > 0)
        {
            var dir = stack.Pop();
            IEnumerable<string> children;
            try
            {
                children = Directory.EnumerateDirectories(dir);
            }
            catch
            {
                continue;
            }

            foreach (var child in children)
            {
                var name = Path.GetFileName(child);
                if (name is "env" or "bin" or "obj" or ".git" or "__pycache__" or "node_modules" or ".vs")
                    continue;
                stack.Push(child);
            }

            IEnumerable<string> files;
            try
            {
                files = Directory.EnumerateFiles(dir, "*.py");
            }
            catch
            {
                continue;
            }

            foreach (var file in files)
                yield return file;
        }
    }
}
