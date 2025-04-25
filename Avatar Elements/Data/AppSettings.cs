using System;
using System.Collections.Generic;
using System.Drawing; // For Point and Color
using System.Linq;
using Avatar_Elements.Data; // Assuming other data classes are here
using Newtonsoft.Json; // For JsonConverter attribute

namespace Avatar_Elements.Data {
    /// <summary>
    /// Stores all application settings, including global configurations and avatar profiles.
    /// </summary>
    public class AppSettings {
        /// <summary>
        /// List of all configured avatar profiles.
        /// </summary>
        public List<AvatarProfile> Profiles { get; set; }

        /// <summary>
        /// If true, the application starts minimized to the system tray.
        /// </summary>
        public bool StartMinimized { get; set; } = false;

        /// <summary>
        /// The global hotkey configuration used to show/hide the preview window.
        /// </summary>
        public HotkeyConfig GlobalHideShowHotkey { get; set; }

        /// <summary>
        /// The Guid of the last profile that was active when the application closed.
        /// Used to restore the last session state. Null if none was active or saved.
        /// </summary>
        public Guid? LastActiveProfileId { get; set; }

        /// <summary>
        /// The last known position of the preview window on the screen.
        /// Null if the position hasn't been saved yet.
        /// Requires custom JsonConverter for Point serialization.
        /// </summary>
        [JsonConverter(typeof(JsonPointConverter))]
        public Point? PreviewWindowPosition { get; set; } = null;

        // --- Global Lighting Settings ---


        /// <summary>
        /// List of light sources defined globally, affecting all profiles.
        /// </summary>
        public List<LightSource> GlobalLights { get; set; }

        // --- ADDED: Global Animation Settings ---
        /// <summary>
        /// List of globally defined animations that can be triggered.
        /// </summary>
        public List<AnimationTimeline> GlobalAnimations { get; set; } = new List<AnimationTimeline>();
        // --- END ADDED ---


        /// <summary>
        /// Constructor. Initializes default values and empty lists.
        /// </summary>
        public AppSettings()
        {
            Profiles = new List<AvatarProfile>();
            GlobalHideShowHotkey = new HotkeyConfig(); // Default to none
            GlobalLights = new List<LightSource>(); // Initialize empty list
            GlobalAnimations = new List<AnimationTimeline>(); // <<< ADDED Initialization
        }

        /// <summary>
        /// Helper method to retrieve a profile by its unique ID.
        /// Returns null if the profile is not found.
        /// </summary>
        public AvatarProfile GetProfileById(Guid id)
        {
            // Use Linq's FirstOrDefault for potentially slightly cleaner code
            return Profiles?.FirstOrDefault(p => p.Id == id);
        }

        /// <summary>
        /// Helper method to retrieve an animation timeline by its unique ID.
        /// Returns null if the timeline is not found.
        /// </summary>
        public AnimationTimeline GetAnimationById(Guid id)
        {
            // Use Linq's FirstOrDefault
            return GlobalAnimations?.FirstOrDefault(a => a.Id == id);
        }

    }

    // --- JsonPointConverter (No changes needed) ---
    // (Existing JsonPointConverter class remains here)
    public class JsonPointConverter : JsonConverter {
        public override bool CanConvert(Type objectType)
        {
            return objectType == typeof(Point) || objectType == typeof(Point?);
        }

        public override void WriteJson(JsonWriter writer, object value, JsonSerializer serializer)
        {
            if (value == null) { writer.WriteNull(); return; }
            Point point = (Point)value;
            writer.WriteStartObject();
            writer.WritePropertyName("X"); writer.WriteValue(point.X);
            writer.WritePropertyName("Y"); writer.WriteValue(point.Y);
            writer.WriteEndObject();
        }

        public override object ReadJson(JsonReader reader, Type objectType, object existingValue, JsonSerializer serializer)
        {
            if (reader.TokenType == JsonToken.Null)
            {
                if (Nullable.GetUnderlyingType(objectType) != null) return null;
                return Point.Empty;
            }
            if (reader.TokenType == JsonToken.StartObject)
            {
                int x = 0, y = 0;
                while (reader.Read())
                {
                    if (reader.TokenType == JsonToken.EndObject) break;
                    if (reader.TokenType == JsonToken.PropertyName)
                    {
                        string propName = reader.Value.ToString();
                        if (!reader.Read()) break;
                        if (propName.Equals("X", StringComparison.OrdinalIgnoreCase)) x = Convert.ToInt32(reader.Value);
                        else if (propName.Equals("Y", StringComparison.OrdinalIgnoreCase)) y = Convert.ToInt32(reader.Value);
                    }
                }
                return new Point(x, y);
            }
            return Point.Empty;
        }
    }
}