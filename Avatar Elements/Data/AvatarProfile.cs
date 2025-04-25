// >>> START MODIFICATION: Data/AvatarProfile.cs (Full File)
using System;
using System.Collections.Generic;
using System.Drawing; // Required for Point if used in HotkeyConfig, though unlikely
using Avatar_Elements.Data; // For HotkeyConfig, LightSource, AnimationTimeline

namespace Avatar_Elements.Data {
    /// <summary>
    /// Represents a single avatar profile configuration, including visual assets,
    /// hotkey, lighting, depth, and animation settings.
    /// </summary>
    public class AvatarProfile {
        /// <summary>
        /// Unique identifier for this profile.
        /// </summary>
        public Guid Id { get; set; }

        /// <summary>
        /// User-defined name for this profile.
        /// </summary>
        public string ProfileName { get; set; }

        /// <summary>
        /// Full path to the base avatar image file (e.g., PNG with transparency).
        /// </summary>
        public string BaseImagePath { get; set; }

        /// <summary>
        /// Full path to the corresponding depth map image file (grayscale).
        /// </summary>
        public string DepthMapPath { get; set; }

        /// <summary>
        /// Hotkey configuration specifically assigned to activate this profile.
        /// Can be null or have Keys.None if no hotkey is assigned.
        /// </summary>
        public HotkeyConfig Hotkey { get; set; }

        /// <summary>
        /// Controls the virtual Z-depth derived from the depth map.
        /// Default is 1.0.
        /// </summary>
        public float DepthScale { get; set; } = 1.0f;

        /// <summary>
        /// List of light sources specific to this profile.
        /// These lights are added to the global lights when this profile is active.
        /// </summary>
        public List<LightSource> ProfileLights { get; set; }

        /// <summary>
        /// Controls the overall brightness/strength of specular highlights (0=none).
        /// Default is 0.5f.
        /// </summary>
        public float SpecularIntensity { get; set; } = 0.5f;

        /// <summary>
        /// Controls the focus/size of specular highlights (shininess).
        /// Higher values result in smaller, sharper highlights. Default is 32.0f.
        /// </summary>
        public float SpecularPower { get; set; } = 32.0f;

        /// <summary>
        /// List of animation timelines defined specifically for this profile.
        /// Each animation includes its own trigger hotkey (unique within this list).
        /// </summary>
        public List<AnimationTimeline> Animations { get; set; } // <<< ADDED PROPERTY

        /// <summary>
        /// Constructor. Initializes with a new unique ID and default values.
        /// </summary>
        public AvatarProfile()
        {
            Id = Guid.NewGuid();
            ProfileName = "New Profile";
            Hotkey = new HotkeyConfig();
            ProfileLights = new List<LightSource>();
            Animations = new List<AnimationTimeline>(); // <<< ADDED Initialization
            DepthScale = 1.0f;
            SpecularIntensity = 0.5f;
            SpecularPower = 32.0f;
        }

        public override string ToString()
        {
            // Useful for ComboBox display if DisplayMember isn't set
            return ProfileName ?? $"Profile {Id.ToString().Substring(0, 8)}";
        }
    }
}
// <<< END MODIFICATION: Data/AvatarProfile.cs