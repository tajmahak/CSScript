using System;
using System.Collections;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Data;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Threading;

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

            }
            else {
                __SHFILEOPSTRUCT_x86 fs = new __SHFILEOPSTRUCT_x86 {
                    wFunc = __FileOperationType.FO_DELETE,
                    pFrom = path + '\0' + '\0',
                    fFlags = __FileOperationFlags.FOF_ALLOWUNDO | flags
                };
                __SHFileOperation_x86(ref fs);
            }
            return true;

        }
        catch {
            return false;
        }
    }

    // Скрытие/отображениие текущего окна консоли
    public static void SetVisibleConsoleWindow(bool visible) {
        if (visible) {
            __ShowWindow(__GetConsoleWindow(), __SW_SHOW);
        }
        else {
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
    public double MarginValue;

    public double PaddingValue;

    public ScriptWindow(double width, double height) {
        Width = width;
        Height = height;
        WindowStartupLocation = WindowStartupLocation.CenterScreen;
        Background = new SolidColorBrush(Color.FromArgb(255, 240, 240, 240));
        ResizeMode = ResizeMode.NoResize;
        SetFont(14);

        mainPanel = new ScriptStackPanel(this);
        Content = mainPanel;
    }

    public ScriptWindow SetScrollable() {
        ScrollViewer scrollViewer = new ScrollViewer {
            Content = mainPanel
        };
        Content = scrollViewer;
        ResizeMode = ResizeMode.CanResize;

        return this;
    }

    public ScriptWindow SetFont(double fontSize) {
        FontSize = fontSize;
        MarginValue = fontSize / 2;
        PaddingValue = fontSize / 5;

        return this;
    }

    public ScriptWindow SetIcon(string base64Image) {
        byte[] rawImage = Convert.FromBase64String(base64Image);
        ImageSourceConverter imageConverter = new ImageSourceConverter();
        BitmapSource image = (BitmapSource)imageConverter.ConvertFrom(rawImage);
        Icon = image;

        return this;
    }


    public void Invoke(Action action) {
        Dispatcher.Invoke(action);
    }

    /// <summary>
    /// Запуск асинхронной операции, при этом новая операция не запустится до завершения работы предыдущей.
    /// </summary>
    public bool BeginSingleInvoke(Action action, Action onCompletedAction = null) {
        if (sigleTask != null) {
            return false;
        }
        sigleTask = Task.Factory.StartNew(() => {
            try {
                action();
            }
            catch (Exception ex) {
                Invoke(() => { MessageBox.Show(ex.Message, "", MessageBoxButton.OK, MessageBoxImage.Error); });
            }
            finally {
                try {
                    if (onCompletedAction != null) {
                        onCompletedAction();
                    }
                }
                finally {
                    sigleTask = null;
                }
            }
        });
        return true;
    }


    public TextBlock AddTextBlock(string text = "", bool bold = false) {
        return mainPanel.AddTextBlock(text, bold);
    }

    public TextBox AddTextBox(string text = "") {
        return mainPanel.AddTextBox(text);
    }

    public TextBox AddMultiLineTextBox(double height, string text = "") {
        return mainPanel.AddMultiLineTextBox(height, text);
    }

    public ComboBox AddComboBox(IEnumerable itemsSource = null, string selectedItem = "") {
        return mainPanel.AddComboBox(itemsSource, selectedItem);
    }

    public CheckBox AddCheckBox(string text, bool isChecked = false) {
        return mainPanel.AddCheckBox(text, isChecked);
    }

    public RadioButton AddRadioButton(string text, bool isChecked = false) {
        return mainPanel.AddRadioButton(text, isChecked);
    }

    public Button AddButton(string text, bool boldFont = false) {
        return mainPanel.AddButton(text, boldFont);
    }

    public ListBox AddListBox(double height, IEnumerable itemsSource = null) {
        return mainPanel.AddListBox(height, itemsSource);
    }

    public ScriptCheckedListBox AddCheckedListBox(double height, ICollection itemsSource = null) {
        return mainPanel.AddCheckedListBox(height, itemsSource);
    }

    public Button AddOKButton(string text, bool boldFont = false) {
        return mainPanel.AddOKButton(text, boldFont);
    }

    public ScriptUniformGrid AddGrid(int columnsCount = 2) {
        return mainPanel.AddGrid(columnsCount);
    }

    public ScriptListView AddListView(double height, IEnumerable itemsSource, IEnumerable<ScriptListViewColumn> columns) {
        return mainPanel.AddListView(height, itemsSource, columns);
    }

    private readonly ScriptStackPanel mainPanel;
    private Task sigleTask;
}

public class ScriptStackPanel : StackPanel
{
    private readonly ScriptWindow window;
    public ScriptStackPanel(ScriptWindow window) {
        this.window = window;
    }

    private UIElement Last() {
        // LINQ почему-то не срабатывает
        return Children.Count == 0 ? null : Children[Children.Count - 1];
    }


    public TextBlock AddTextBlock(string text = "", bool bold = false) {
        TextBlock textBlock = new TextBlock {
            Text = text
        };
        if (bold) {
            textBlock.FontWeight = FontWeights.Bold;
        }
        textBlock.TextWrapping = TextWrapping.Wrap;

        double topMargin = 0;
        textBlock.Margin = new Thickness(window.MarginValue, topMargin, window.MarginValue, 0);
        textBlock.Padding = new Thickness(window.PaddingValue);

        Children.Add(textBlock);
        return textBlock;
    }

    public TextBox AddTextBox(string text = "") {
        TextBox textBox = new TextBox {
            Text = text,
        };

        double topMargin = Last() is TextBlock ? 0 : window.MarginValue;
        textBox.Margin = new Thickness(window.MarginValue, topMargin, window.MarginValue, 0);
        textBox.Padding = new Thickness(window.PaddingValue);

        Children.Add(textBox);
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
        ComboBox comboBox = new ComboBox {
            IsEditable = true,
            ItemsSource = itemsSource,
            Text = selectedItem
        };

        double topMargin = Last() is TextBlock ? 0 : window.MarginValue;
        comboBox.Margin = new Thickness(window.MarginValue, topMargin, window.MarginValue, 0);
        comboBox.Padding = new Thickness(window.PaddingValue);

        Children.Add(comboBox);
        return comboBox;
    }

    public CheckBox AddCheckBox(string text, bool isChecked = false) {
        CheckBox checkBox = new CheckBox {
            Content = text,
            IsChecked = isChecked
        };

        double topMargin = Last() is TextBlock ? 0 : window.MarginValue;
        checkBox.Margin = new Thickness(window.MarginValue, topMargin, window.MarginValue, 0);
        //checkBox.Padding = new Thickness(window.PaddingValue);

        Children.Add(checkBox);
        return checkBox;
    }

    public RadioButton AddRadioButton(string text, bool isChecked = false) {
        RadioButton radioButton = new RadioButton {
            Content = text,
            IsChecked = isChecked
        };

        double topMargin = Last() is TextBlock ? 0 : window.MarginValue;
        radioButton.Margin = new Thickness(window.MarginValue, topMargin, window.MarginValue, 0);
        //radioButton.Padding = new Thickness(window.PaddingValue);

        Children.Add(radioButton);
        return radioButton;
    }

    public Button AddButton(string text, bool boldFont = false) {
        Button button = new Button {
            Content = text,
        };
        if (boldFont) {
            button.FontWeight = FontWeights.Bold;
        }

        double topMargin = Last() is TextBlock ? 0 : window.MarginValue;
        button.Margin = new Thickness(window.MarginValue, topMargin, window.MarginValue, 0);
        button.Padding = new Thickness(window.PaddingValue);

        Children.Add(button);
        return button;
    }

    public ListBox AddListBox(double height, IEnumerable itemsSource = null) {
        ListBox listBox = new ListBox {
            Height = height,
            ItemsSource = itemsSource
        };

        double topMargin = Last() is TextBlock ? 0 : window.MarginValue;
        listBox.Margin = new Thickness(window.MarginValue, topMargin, window.MarginValue, 0);
        listBox.Padding = new Thickness(window.PaddingValue);

        Children.Add(listBox);
        return listBox;
    }

    public ScriptCheckedListBox AddCheckedListBox(double height, ICollection itemsSource = null) {
        ScriptCheckedListBox checkedListBox = new ScriptCheckedListBox(window) {
            Height = height,
            Items = itemsSource
        };

        double topMargin = Last() is TextBlock ? 0 : window.MarginValue;
        checkedListBox.Margin = new Thickness(window.MarginValue, topMargin, window.MarginValue, 0);
        checkedListBox.Padding = new Thickness(window.PaddingValue);

        Children.Add(checkedListBox);
        return checkedListBox;
    }

    public Button AddOKButton(string text, bool boldFont = false) {
        Button button = AddButton(text, boldFont);
        button.Click += (sender, e) => window.DialogResult = true;
        return button;
    }

    public ScriptUniformGrid AddGrid(int columnsCount = 2) {
        ScriptUniformGrid grid = new ScriptUniformGrid(window, columnsCount);

        Children.Add(grid);
        return grid;
    }

    public ScriptListView AddListView(double height, IEnumerable itemsSource, IEnumerable<ScriptListViewColumn> columns) {
        ScriptListView listView = new ScriptListView {
            Height = height,
            ItemsSource = itemsSource
        };

        double topMargin = Last() is TextBlock ? 0 : window.MarginValue;
        listView.Margin = new Thickness(window.MarginValue, topMargin, window.MarginValue, 0);
        listView.Padding = new Thickness(window.PaddingValue);

        GridView gridView = new GridView {
            AllowsColumnReorder = true
        };
        foreach (ScriptListViewColumn column in columns) {
            GridViewColumn gridViewColumn = new GridViewColumn {
                DisplayMemberBinding = new Binding(column.Binding),
                Header = column.Header,
            };
            if (column.Width.HasValue) {
                gridViewColumn.Width = column.Width.Value;
            }
            gridView.Columns.Add(gridViewColumn);
        }

        listView.View = gridView;

        Children.Add(listView);
        return listView;
    }
}

public class ScriptCheckedListBox : ScrollViewer
{
    private readonly StackPanel panel;
    private ICollection items;
    private readonly ScriptWindow window;

    public ScriptCheckedListBox(ScriptWindow window) {
        this.window = window;
        Background = new SolidColorBrush(Colors.White);
        panel = new StackPanel();
        Content = panel;
    }

    public ICollection Items {
        get {
            return items;
        }
        set {
            items = value;
            panel.Children.Clear();
            if (items != null) {
                foreach (object item in items) {
                    CheckBox checkBox = new CheckBox {
                        Content = item,
                        Margin = new Thickness(window.PaddingValue),
                    };
                    panel.Children.Add(checkBox);
                }
            }
        }
    }

    public void SetItemChecked(int index, bool isChecked) {
        CheckBox checkBox = (CheckBox)panel.Children[index];
        checkBox.IsChecked = isChecked;
    }

    public bool GetItemChecked(int index) {
        CheckBox checkBox = (CheckBox)panel.Children[index];
        return checkBox.IsChecked.Value;
    }
}

public class ScriptUniformGrid : UniformGrid
{
    private readonly ScriptStackPanel[] panels;
    public ScriptUniformGrid(ScriptWindow window, int columnsCount) {
        Columns = columnsCount;
        Rows = 1;

        panels = new ScriptStackPanel[columnsCount];
        for (int c = 0; c < columnsCount; c++) {
            ScriptStackPanel panel = new ScriptStackPanel(window);
            panels[c] = panel;
            Children.Add(panel);
        }
    }

    public ScriptStackPanel this[int columnIndex] {
        get {
            return panels[columnIndex];
        }
    }

    public void SetScrollable(int index, double height) {
        Children.RemoveAt(index);
        ScrollViewer scrollViewer = new ScrollViewer {
            Height = height,
            Content = panels[index]
        };
        Children.Insert(index, scrollViewer);
    }
}

public class ScriptListView : ListView
{
    //// Не доработано
    //public ScriptListView SetMultiselect() {
    //    SelectionMode = SelectionMode.Multiple;

    //    ItemContainerStyle = new Style(typeof(ListBoxItem));
    //    ItemContainerStyle.Setters.Add(new EventSetter(MouseEnterEvent, new MouseEventHandler((sender, e) => {
    //        if (e.LeftButton == MouseButtonState.Pressed) {
    //            ListBoxItem lbi = sender as ListBoxItem;
    //            lbi.IsSelected = true;
    //            lbi.Focus();
    //            SelectedItems.Add(lbi);
    //        }
    //    })));
    //    return this;
    //}
}

public class ScriptListViewColumn
{
    public string Header { get; set; }

    public string Binding { get; set; }

    public double? Width { get; set; }
}