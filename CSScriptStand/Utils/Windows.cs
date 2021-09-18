﻿using System;
using System.Drawing;
using System.Runtime.InteropServices;
using System.Windows.Forms;

// utils.windows
// ВЗАИМОДЕЙСТВИЕ С WINDOWS (18.09.2021)
// ------------------------------------------------------------

///// #namespace

// Утилиты для взаимодействия с API Windows
public static class WindowsUtils
{
    // Перемещение файла в корзину Windows
    public static bool MoveFileToRecycleBin(string path, bool silent = false) {
        __FileOperationFlags flags = silent ?
            (__FileOperationFlags.FOF_NOCONFIRMATION | __FileOperationFlags.FOF_NOERRORUI | __FileOperationFlags.FOF_SILENT) :
            (__FileOperationFlags.FOF_NOCONFIRMATION | __FileOperationFlags.FOF_WANTNUKEWARNING);
        try {
            if (IntPtr.Size == 8) {
                __SHFILEOPSTRUCT_x64 fs = new __SHFILEOPSTRUCT_x64 {
                    wFunc = __FileOperationType.FO_DELETE,
                    pFrom = path + '\0' + '\0',
                    fFlags = __FileOperationFlags.FOF_ALLOWUNDO | flags
                };
                __SHFileOperation_x64(ref fs);
            } else {
                __SHFILEOPSTRUCT_x86 fs = new __SHFILEOPSTRUCT_x86 {
                    wFunc = __FileOperationType.FO_DELETE,
                    pFrom = path + '\0' + '\0',
                    fFlags = __FileOperationFlags.FOF_ALLOWUNDO | flags
                };
                __SHFileOperation_x86(ref fs);
            }
            return true;
        } catch {
            return false;
        }
    }

    // Скрытие/отображениие текущего окна консоли
    public static void SetVisibleConsoleWindow(bool visible) {
        if (visible) {
            ShowWindow(GetConsoleWindow(), SW_SHOW);
        } else {
            ShowWindow(GetConsoleWindow(), SW_HIDE);
        }
    }


    /// --- ВНУТРЕННИЕ СУЩНОСТИ (НЕ ИСПОЛЬЗУЮТСЯ НАПРЯМУЮ) ---

    [DllImport("kernel32.dll")]
    private static extern IntPtr GetConsoleWindow();

    [DllImport("user32.dll")]
    private static extern bool ShowWindow(IntPtr hWnd, int nCmdShow);

    [DllImport("shell32.dll", CharSet = CharSet.Auto, EntryPoint = "SHFileOperation")]
    private static extern int __SHFileOperation_x86(ref __SHFILEOPSTRUCT_x86 FileOp);

    [DllImport("shell32.dll", CharSet = CharSet.Auto, EntryPoint = "SHFileOperation")]
    private static extern int __SHFileOperation_x64(ref __SHFILEOPSTRUCT_x64 FileOp);

    private enum __FileOperationType : uint
    {
        FO_MOVE = 0x0001,
        FO_COPY = 0x0002,
        FO_DELETE = 0x0003,
        FO_RENAME = 0x0004,
    }

    [Flags]
    private enum __FileOperationFlags : ushort
    {
        FOF_SILENT = 0x0004,
        FOF_NOCONFIRMATION = 0x0010,
        FOF_ALLOWUNDO = 0x0040,
        FOF_SIMPLEPROGRESS = 0x0100,
        FOF_NOERRORUI = 0x0400,
        FOF_WANTNUKEWARNING = 0x4000,
    }

    [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Auto, Pack = 1)]
    private struct __SHFILEOPSTRUCT_x86
    {
        public IntPtr hwnd;
        [MarshalAs(UnmanagedType.U4)]
        public __FileOperationType wFunc;
        public string pFrom;
        public string pTo;
        public __FileOperationFlags fFlags;
        [MarshalAs(UnmanagedType.Bool)]
        public bool fAnyOperationsAborted;
        public IntPtr hNameMappings;
        public string lpszProgressTitle;
    }

    [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Auto)]
    private struct __SHFILEOPSTRUCT_x64
    {
        public IntPtr hwnd;
        [MarshalAs(UnmanagedType.U4)]
        public __FileOperationType wFunc;
        public string pFrom;
        public string pTo;
        public __FileOperationFlags fFlags;
        [MarshalAs(UnmanagedType.Bool)]
        public bool fAnyOperationsAborted;
        public IntPtr hNameMappings;
        public string lpszProgressTitle;
    }

    private const int SW_HIDE = 0;
    private const int SW_SHOW = 5;
}

public class WinFormBuilder : Form
{
    public static WinFormBuilder Create(int width, int height) {
        if (!initVisualStyles) {
            Application.EnableVisualStyles();
            Application.SetCompatibleTextRenderingDefault(false);
            initVisualStyles = true;
        }
        return new WinFormBuilder(width, height);
    }


    public Label AddLabel(string text = "", bool bold = false) {
        Label label = new Label();
        label.Text = text;
        label.AutoSize = true;
        if (bold) {
            label.Font = new Font(Font, FontStyle.Bold);
        }

        panel.Controls.Add(label);
        return label;
    }

    public TextBox AddTextBox(string text = "") {
        TextBox textBox = new TextBox();
        if (text != null) {
            textBox.Text = text;
        }
        textBox.WordWrap = false;
        textBox.ScrollBars = ScrollBars.Both;

        textBox.Width = panel.Width - panel.Padding.Left - panel.Padding.Right - 4;
        panel.Controls.Add(textBox);
        return textBox;
    }

    public TextBox AddMultiLineTextBox(int height, string text = "") {
        TextBox textBox = AddTextBox(text);
        textBox.Multiline = true;
        textBox.ShortcutsEnabled = true;
        textBox.Height = height;

        return textBox;
    }

    public Button AddButton(string text) {
        Button button = new Button();
        button.Text = text;
        button.Width = panel.Width - panel.Padding.Left - panel.Padding.Right - 4;
        button.AutoSize = true;

        panel.Controls.Add(button);
        return button;
    }

    public Button AddOKButton(string text) {
        Button button = AddButton(text);
        button.Click += OKButton_Click;
        button.AutoSize = true;
        return button;
    }

    public CheckBox AddCheckBox(string text, bool check = false) {
        CheckBox checkBox = new CheckBox();
        checkBox.AutoSize = true;
        checkBox.Text = text;
        checkBox.Checked = check;

        panel.Controls.Add(checkBox);
        return checkBox;
    }

    public RadioButton AddRadioButton(string text, bool check = false) {
        RadioButton radioButton = new RadioButton();
        radioButton.Text = text;
        radioButton.Checked = check;
        radioButton.AutoSize = true;

        panel.Controls.Add(radioButton);
        return radioButton;
    }

    public ComboBox AddComboBox(object[] items = null, string text = null) {
        ComboBox comboBox = new ComboBox();

        comboBox.Width = panel.Width - panel.Padding.Left - panel.Padding.Right - 4;
        comboBox.Margin = new Padding(3, 3, 3, 10);

        if (items != null) {
            comboBox.Items.AddRange(items);
        }
        if (text != null) {
            comboBox.Text = text;
        }

        panel.Controls.Add(comboBox);
        return comboBox;
    }

    public CheckedListBox AddCheckedListBox(object[] items = null) {
        CheckedListBox checkedListBox = new CheckedListBox();
        checkedListBox.Width = panel.Width - panel.Padding.Left - panel.Padding.Right - 4;
        if (items != null) {
            checkedListBox.Items.AddRange(items);
        }
        panel.Controls.Add(checkedListBox);
        return checkedListBox;
    }


    private static bool initVisualStyles;

    private readonly FlowLayoutPanel panel;

    private WinFormBuilder(int width, int height) : base() {
        AutoScaleMode = AutoScaleMode.Dpi;
        AutoScroll = true;
        ClientSize = new Size(width, height);
        Font = new Font("Segoe UI", 10.2F, FontStyle.Regular, GraphicsUnit.Point, 204);
        FormBorderStyle = FormBorderStyle.FixedToolWindow;

        panel = new FlowLayoutPanel();
        panel.AutoScroll = true;
        panel.Dock = DockStyle.Fill;
        panel.WrapContents = false;
        panel.FlowDirection = FlowDirection.TopDown;
        panel.Padding = new Padding(10);
        Controls.Add(panel);
    }

    private void OKButton_Click(object sender, EventArgs e) {
        DialogResult = DialogResult.OK;
    }
}
