public class KeyboardConfig
{
    public KeyboardConfig(string name, string layout)
    {
        Name = name;
        Layout = layout;
    }

    public string Name { get; set; }
    public string Layout { get; set; }
}