using System.Runtime.InteropServices;


namespace keyboard_switcher;

public partial class KeyboardSwitcher : Form
{
    public string currentKeyboardId = "";
    public Dictionary<string, KeyboardConfig> keyboardConfig;
    private Dictionary<string, KeyboardConfig> LastSavedConfig = new Dictionary<string, KeyboardConfig>();
    public Dictionary<string, KeyboardFormFields> FormData = new Dictionary<string, KeyboardFormFields>();
    private const int WM_INPUT = 0x00FF;
    private const int RIDEV_INPUTSINK = 0x00000100;
    private const int DEVICE_TYPE_KEYBOARD = 1;
    private const uint WM_INPUTLANGCHANGEREQUEST = 0x0050;

    [DllImport("user32.dll")]
    private static extern uint GetRawInputDeviceList(IntPtr pRawInputDeviceList, ref uint uiNumDevices, uint cbSize);

    [DllImport("user32.dll")]
    private static extern uint GetRawInputDeviceInfo(IntPtr hDevice, uint uiCommand, IntPtr pData, ref uint pcbSize);

    [DllImport("user32.dll")]
    private static extern bool RegisterRawInputDevices(RAWINPUTDEVICE[] pRawInputDevice, uint uiNumDevices, uint cbSize);

    [DllImport("user32.dll")]
    private static extern uint GetRawInputData(IntPtr hRawInput, uint uiCommand, IntPtr pData, ref uint pcbSize, uint cbSizeHeader);

    [DllImport("user32.dll")]
    public static extern bool PostMessage(IntPtr hWnd, uint Msg, IntPtr wParam, IntPtr lParam);

    [DllImport("user32.dll")]
    public static extern IntPtr LoadKeyboardLayout(string pwszKLID, uint Flags);

    public KeyboardSwitcher(Dictionary<string, KeyboardConfig> keyboardConfig)
    {
        InitializeComponent();
        this.keyboardConfig = keyboardConfig;
        this.LastSavedConfig = new Dictionary<string, KeyboardConfig>(keyboardConfig);
        this.Text = "Keyboard Switcher";
        this.Load += new EventHandler(KeyboardSwitcher_Load!);
        this.Width = 25 + 150 + 10 + 150 + 10 + 100 + 25 + 10; // add magic 10 to center the content
        DrawGui();
    }

    private void DrawGui()
    {
        const int windowPadding = 25;
        const int controlHeight = 25;
        const int controlWidth = 150;
        const int buttonWidth = 100;
        const int spacing = 10;
        int currentTop = windowPadding;

        foreach (KeyValuePair<string, KeyboardConfig> entry in keyboardConfig)
        {
            // Create a new label for the name
            Label nameLabel = new Label();
            nameLabel.Text = "Name";
            nameLabel.Top = currentTop;
            nameLabel.Left = 0 + windowPadding;
            nameLabel.Width = controlWidth;
            nameLabel.Height = controlHeight;
            this.Controls.Add(nameLabel);

            // Create a new text box for the name
            TextBox nameTextBox = new TextBox();
            nameTextBox.Text = entry.Value.Name;
            nameTextBox.Top = currentTop + controlHeight;
            nameTextBox.Left = 0 + windowPadding;
            nameTextBox.Width = controlWidth;
            nameTextBox.Height = controlHeight;
            this.Controls.Add(nameTextBox);

            // Create a new label for the layout
            Label layoutLabel = new Label();
            layoutLabel.Text = "Layout";
            layoutLabel.Top = currentTop;
            layoutLabel.Left = controlWidth + spacing + windowPadding;
            layoutLabel.Width = controlWidth;
            layoutLabel.Height = controlHeight;
            this.Controls.Add(layoutLabel);

            // Create a new text box for the layout
            TextBox layoutTextBox = new TextBox();
            layoutTextBox.Text = entry.Value.Layout;
            layoutTextBox.Top = currentTop + controlHeight;
            layoutTextBox.Left = controlWidth + spacing + windowPadding;
            layoutTextBox.Width = controlWidth;
            layoutTextBox.Height = controlHeight;
            layoutTextBox.AutoCompleteMode = AutoCompleteMode.SuggestAppend;
            layoutTextBox.AutoCompleteSource = AutoCompleteSource.CustomSource;
            AutoCompleteStringCollection autoCompleteCollection = new AutoCompleteStringCollection();
            foreach (InputLanguage lang in InputLanguage.InstalledInputLanguages)
            {
                autoCompleteCollection.Add(lang.Culture.EnglishName);
            }
            layoutTextBox.AutoCompleteCustomSource = autoCompleteCollection;
            this.Controls.Add(layoutTextBox);

            // Add button to remove the keyboard
            Button removeButton = new Button();
            removeButton.Text = "Remove";
            removeButton.Left = controlWidth * 2 + spacing * 2 + windowPadding;
            removeButton.Top = currentTop + windowPadding;
            removeButton.Width = buttonWidth;
            removeButton.Click += new EventHandler((sender, e) =>
            {
                keyboardConfig.Remove(entry.Key);
                RedrawGui();
            });
            this.Controls.Add(removeButton);

            FormData.Add(entry.Key, new KeyboardFormFields(nameTextBox, layoutTextBox));

            currentTop += controlHeight * 2 + spacing;
        }

        // Add a button to add cancel the config
        Button cancelButton = new Button();
        cancelButton.Text = "Cancel";
        cancelButton.Left = 0 + windowPadding;
        cancelButton.Top = currentTop + windowPadding;
        cancelButton.Width = buttonWidth;
        cancelButton.Click += new EventHandler(CancelButton_Click!);
        this.Controls.Add(cancelButton);

        // add a button to save the config
        Button saveButton = new Button();
        saveButton.Text = "Save";
        saveButton.Left = buttonWidth + spacing + windowPadding;
        saveButton.Top = currentTop + windowPadding;
        saveButton.Width = buttonWidth;
        saveButton.Click += new EventHandler(SaveButton_Click!);
        this.Controls.Add(saveButton);
    }

    private void RedrawGui()
    {
        this.Controls.Clear();
        foreach (KeyValuePair<string, KeyboardFormFields> entry in FormData)
        {
            entry.Value.nameTextBox.Dispose();
            entry.Value.nameTextBox.Clear();
            entry.Value.layoutTextBox.Dispose();
            entry.Value.layoutTextBox.Clear();
        }

        FormData.Clear();

        DrawGui();
    }

    private void CancelButton_Click(object sender, EventArgs e)
    {
        keyboardConfig = new Dictionary<string, KeyboardConfig>(LastSavedConfig);
        RedrawGui();
    }

    private void SaveButton_Click(object sender, EventArgs e)
    {
        foreach (KeyValuePair<string, KeyboardFormFields> entry in FormData)
        {
            keyboardConfig[entry.Key].Name = entry.Value.nameTextBox.Text;
            keyboardConfig[entry.Key].Layout = entry.Value.layoutTextBox.Text;
        }
        Program.SaveConfig(keyboardConfig);
        LastSavedConfig = new Dictionary<string, KeyboardConfig>(keyboardConfig);
    }

    private void KeyboardSwitcher_Load(object sender, EventArgs e)
    {
        RegisterKeyboardDevices();
    }

    protected override void WndProc(ref Message m)
    {
        switch (m.Msg)
        {
            case WM_INPUT:
                HandleRawInput(m.LParam);
                break;
        }

        base.WndProc(ref m);
    }

    private void RegisterKeyboardDevices()
    {
        RAWINPUTDEVICE[] rid = new RAWINPUTDEVICE[1];

        rid[0].usUsagePage = 0x01;
        rid[0].usUsage = 0x06;
        rid[0].dwFlags = RIDEV_INPUTSINK;
        rid[0].hwndTarget = this.Handle;

        if (!RegisterRawInputDevices(rid, (uint)rid.Length, (uint)Marshal.SizeOf(rid[0])))
            throw new ApplicationException("Failed to register raw input device(s).");
    }

    private void HandleRawInput(IntPtr hRawInput)
    {
        uint dwSize = 0;
        GetRawInputData(hRawInput, 0x10000003, IntPtr.Zero, ref dwSize, (uint)Marshal.SizeOf(typeof(RAWINPUTHEADER)));

        IntPtr data = Marshal.AllocHGlobal((int)dwSize);
        GetRawInputData(hRawInput, 0x10000003, data, ref dwSize, (uint)Marshal.SizeOf(typeof(RAWINPUTHEADER)));

        RAWINPUT raw = (RAWINPUT)Marshal.PtrToStructure(data, typeof(RAWINPUT))!;

        if (raw.header.dwType == DEVICE_TYPE_KEYBOARD)
        {
            var deviceId = GetDeviceId(raw.header.hDevice);
            if (deviceId == null)
                return; // TODO: Handle this? Shouldn't happen?

            handleKeyboardInput(deviceId);
        }

        Marshal.FreeHGlobal(data);
    }

    private void handleKeyboardInput(string deviceId)
    {
        if (currentKeyboardId == deviceId)
            return;

        if (!keyboardConfig.ContainsKey(deviceId))
        {
            keyboardConfig.Add(deviceId, new KeyboardConfig("Unkown", ""));
            RedrawGui();
            return;
        }

        if (keyboardConfig.ContainsKey(deviceId))
            SwitchKeyboardLayout(keyboardConfig[deviceId].Layout);
        else
        {
            keyboardConfig.Add(deviceId, new KeyboardConfig("Unkown", ""));
            RedrawGui();
        }

        currentKeyboardId = deviceId;
    }

    private string? GetDeviceId(IntPtr hDevice)
    {
        uint dwSize = 0;
        GetRawInputDeviceInfo(hDevice, 0x20000007, IntPtr.Zero, ref dwSize);

        IntPtr data = Marshal.AllocHGlobal((int)dwSize);
        GetRawInputDeviceInfo(hDevice, 0x20000007, data, ref dwSize);


        if (data == IntPtr.Zero)
        {
            Marshal.FreeHGlobal(data);
            return null;
        }

        var deviceId = Marshal.PtrToStringAnsi(data);
        Marshal.FreeHGlobal(data);

        return deviceId;
    }

    public void SwitchKeyboardLayout(string layout)
    {
        foreach (InputLanguage lang in InputLanguage.InstalledInputLanguages)
        {
            if (lang.Culture.EnglishName != layout)
                continue;

            IntPtr hkl = LoadKeyboardLayout(lang.Culture.KeyboardLayoutId.ToString("X8"), 0);
            PostMessage(0xffff, WM_INPUTLANGCHANGEREQUEST, IntPtr.Zero, hkl);
            break;
        }
    }
}

public class KeyboardFormFields
{
    public KeyboardFormFields(TextBox nameTextBox, TextBox layoutTextBox)
    {
        this.nameTextBox = nameTextBox;
        this.layoutTextBox = layoutTextBox;
    }

    public TextBox nameTextBox;
    public TextBox layoutTextBox;

}

[StructLayout(LayoutKind.Sequential)]
public struct RAWINPUTDEVICE
{
    public ushort usUsagePage;
    public ushort usUsage;
    public uint dwFlags;
    public IntPtr hwndTarget;
}

[StructLayout(LayoutKind.Sequential)]
public struct RAWINPUTHEADER
{
    public uint dwType;
    public uint dwSize;
    public IntPtr hDevice;
    public IntPtr wParam;
}

[StructLayout(LayoutKind.Sequential)]
public struct RAWINPUT
{
    public RAWINPUTHEADER header;
    public RAWKEYBOARD keyboard;
}

[StructLayout(LayoutKind.Sequential)]
public struct RAWKEYBOARD
{
    public ushort MakeCode;
    public ushort Flags;
    public ushort Reserved;
    public ushort VKey;
    public uint Message;
    public uint ExtraInformation;
}