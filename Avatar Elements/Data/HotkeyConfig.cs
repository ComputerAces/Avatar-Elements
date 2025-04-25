// >>> START MODIFICATION: Data/HotkeyConfig.cs
using System;
using System.Text;
using System.Windows.Forms; // For Keys enum

namespace Avatar_Elements.Data {
    public class HotkeyConfig {
        public Keys Key { get; set; } = Keys.None;
        public bool Control { get; set; }
        public bool Alt { get; set; }
        public bool Shift { get; set; }

        /// <summary>
        /// Creates a shallow copy of this HotkeyConfig object.
        /// </summary>
        /// <returns>A new HotkeyConfig instance with the same property values.</returns>
        public HotkeyConfig Clone()
        {
            // Shallow copy is sufficient since all members are value types (Keys enum, bool)
            return new HotkeyConfig
            {
                Key = this.Key,
                Control = this.Control,
                Alt = this.Alt,
                Shift = this.Shift
            };
        }

        public override string ToString()
        {
            if (Key == Keys.None) return "None";
            var sb = new StringBuilder();
            if (Control) sb.Append("Ctrl + ");
            if (Alt) sb.Append("Alt + ");
            if (Shift) sb.Append("Shift + ");
            sb.Append(Enum.GetName(typeof(Keys), Key) ?? Key.ToString());
            return sb.ToString();
        }
    }
}
// <<< END MODIFICATION: Data/HotkeyConfig.cs