using System;
using System.Collections;
using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Media.Imaging;

// utils.windows
// ВЗАИМОДЕЙСТВИЕ С WINDOWS
// ------------------------------------------------------------

//## #import WPF/WindowsBase.dll
//## #import WPF/PresentationCore.dll
//## #import WPF/PresentationFramework.dll
//## #import System.Xaml.dll

//## #namespace

// Утилиты для взаимодействия с Windows
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
            __ShowWindow(__GetConsoleWindow(), __SW_SHOW);
        } else {
            __ShowWindow(__GetConsoleWindow(), __SW_HIDE);
        }
    }


    /// --- ВНУТРЕННИЕ СУЩНОСТИ (НЕ ИСПОЛЬЗУЮТСЯ НАПРЯМУЮ) ---

    [DllImport("kernel32.dll", EntryPoint = "GetConsoleWindow")]
    private static extern IntPtr __GetConsoleWindow();

    [DllImport("user32.dll", EntryPoint = "ShowWindow")]
    private static extern bool __ShowWindow(IntPtr hWnd, int nCmdShow);

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

    private const int __SW_HIDE = 0;
    private const int __SW_SHOW = 5;
}

public class ScriptWindow : Window
{
    private readonly StackPanel mainPanel;
    private readonly double margin = 7.5;
    private readonly double padding = 3;

    public ScriptWindow(double width, double height, bool useScrollBar = false) {
        Width = width;
        Height = height;
        WindowStartupLocation = WindowStartupLocation.CenterScreen;
        FontSize = 14;
        Background = new SolidColorBrush(Color.FromArgb(255, 240, 240, 240));

        mainPanel = new StackPanel();
        mainPanel.Margin = new Thickness(margin, margin, margin, 0);

        if (useScrollBar) {
            ScrollViewer scrollViewer = new ScrollViewer();
            scrollViewer.Content = mainPanel;
            Content = scrollViewer;

        } else {
            ResizeMode = ResizeMode.NoResize;
            Content = mainPanel;
        }
    }

    public void SetIcon(string base64Image) {
        byte[] rawImage = Convert.FromBase64String(base64Image);
        ImageSourceConverter imageConverter = new ImageSourceConverter();
        BitmapSource image = (BitmapSource)imageConverter.ConvertFrom(rawImage);
        Icon = image;
    }

    public TextBlock AddTextBlock(string text = "", bool bold = false) {
        TextBlock textBlock = new TextBlock();
        textBlock.Text = text;
        if (bold) {
            textBlock.FontWeight = FontWeights.Bold;
        }
        textBlock.TextWrapping = TextWrapping.Wrap;

        mainPanel.Children.Add(textBlock);
        return textBlock;
    }

    public TextBox AddTextBox(string text = "") {
        TextBox textBox = new TextBox();
        textBox.Text = text;
        textBox.Margin = new Thickness(0, 0, 0, margin);
        textBox.Padding = new Thickness(padding);

        mainPanel.Children.Add(textBox);
        return textBox;
    }

    public TextBox AddMultiLineTextBox(double height, string text = "") {
        TextBox textBox = AddTextBox(text);

        textBox.Height = height;
        textBox.TextWrapping = TextWrapping.NoWrap;
        textBox.AcceptsReturn = true;
        textBox.VerticalScrollBarVisibility = ScrollBarVisibility.Auto;
        textBox.HorizontalScrollBarVisibility = ScrollBarVisibility.Auto;

        return textBox;
    }

    public ComboBox AddComboBox(IEnumerable itemsSource = null, string selectedItem = "") {
        ComboBox comboBox = new ComboBox();
        comboBox.IsEditable = true;
        comboBox.Margin = new Thickness(0, 0, 0, margin);
        comboBox.Padding = new Thickness(padding);

        comboBox.ItemsSource = itemsSource;
        comboBox.Text = selectedItem;

        mainPanel.Children.Add(comboBox);
        return comboBox;
    }

    public CheckBox AddCheckBox(string text, bool isChecked = false) {
        CheckBox checkBox = new CheckBox();
        checkBox.Content = text;
        checkBox.Margin = new Thickness(0, 0, 0, margin);
       
        checkBox.IsChecked = isChecked;
       
        mainPanel.Children.Add(checkBox);
        return checkBox;
    }

    public RadioButton AddRadioButton(string text, bool isChecked) {
        RadioButton radioButton = new RadioButton();
        radioButton.Content = text;
        radioButton.IsChecked = isChecked;
        radioButton.Margin = new Thickness(0, 0, 0, margin);

        mainPanel.Children.Add(radioButton);
        return radioButton;
    }

    public Button AddButton(string text, bool boldFont = false) {
        Button button = new Button();
        button.Content = text;
        button.Margin = new Thickness(0, 0, 0, margin);
        button.Padding = new Thickness(padding);
        if (boldFont) {
            button.FontWeight = FontWeights.Bold;
        }

        mainPanel.Children.Add(button);
        return button;
    }

    public ListBox AddListBox(double height, IEnumerable itemsSource = null) {
        ListBox listBox = new ListBox();
        listBox.Height = height;
        listBox.Margin = new Thickness(0, 0, 0, margin);

        listBox.ItemsSource = itemsSource;

        mainPanel.Children.Add(listBox);
        return listBox;
    }

    public CheckedListBox AddCheckedListBox(double height, ICollection itemsSource = null) {
        CheckedListBox checkedListBox = new CheckedListBox();
        checkedListBox.Height = height;
        checkedListBox.Items = itemsSource;
        checkedListBox.Margin = new Thickness(0, 0, 0, margin);

        mainPanel.Children.Add(checkedListBox);
        return checkedListBox;
    }

    public Button AddOKButton(string text, bool boldFont = false) {
        Button button = AddButton(text, boldFont);
        button.Click += (sender, e) => DialogResult = true;
        return button;
    }
}

public class CheckedListBox : ScrollViewer
{
    private readonly StackPanel _stackPanel;
    private readonly double margin = 3;
    private ICollection _items;

    public CheckedListBox() {
        Background = new SolidColorBrush(Colors.White);
        _stackPanel = new StackPanel();
        Content = _stackPanel;
    }

    public ICollection Items {
        get {
            return _items;
        }
        set {
            _items = value;
            _stackPanel.Children.Clear();
            if (_items != null) {
                foreach (object item in _items) {
                    CheckBox checkBox = new CheckBox();
                    checkBox.Content = item;
                    checkBox.Margin = new Thickness(margin);
                    _stackPanel.Children.Add(checkBox);
                }
            }
        }
    }

    public void SetItemChecked(int index, bool isChecked) {
        CheckBox checkBox = (CheckBox)_stackPanel.Children[index];
        checkBox.IsChecked = isChecked;
    }

    public bool GetItemChecked(int index) {
        CheckBox checkBox = (CheckBox)_stackPanel.Children[index];
        return checkBox.IsChecked.Value;
    }
}
