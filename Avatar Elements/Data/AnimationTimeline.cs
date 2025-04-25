// >>> START NEW FILE: Data/AnimationTimeline.cs
using System;
using System.Collections.Generic;

namespace Avatar_Elements.Data {
    /// <summary>
    /// Represents a sequence of keyframes defining a specific animation.
    /// </summary>
    public class AnimationTimeline {
        /// <summary>
        /// Unique identifier for this timeline.
        /// </summary>
        public Guid Id { get; set; }

        /// <summary>
        /// User-defined name for the animation (e.g., "Shake", "Nod", "EnterFromLeft").
        /// </summary>
        public string Name { get; set; } = "New Animation";

        /// <summary>
        /// The sequence of keyframes defining the animation states over time.
        /// IMPORTANT: This list should be kept sorted by Timestamp for correct interpolation.
        /// </summary>
        public List<AnimationKeyframe> Keyframes { get; set; }

        /// <summary>
        /// If true, the animation will repeat from the beginning after reaching the end.
        /// </summary>
        public bool Loop { get; set; } = false;

        /// <summary>
        /// Hotkey configuration specifically assigned to activate this animation timeline.
        /// Can be null or have Keys.None if no hotkey is assigned.
        /// Unique within the owning profile's animation list, but can overlap with other profiles' animation hotkeys
        /// or profile activation hotkeys. Must not conflict with GlobalHideShowHotkey.
        /// </summary>
        public HotkeyConfig Hotkey { get; set; } // <<< ADDED PROPERTY

        /// <summary>
        /// Parameterless constructor for serialization.
        /// </summary>
        public AnimationTimeline()
        {
            Id = Guid.NewGuid();
            Keyframes = new List<AnimationKeyframe>();
            // Add a default starting keyframe?
            Keyframes.Add(new AnimationKeyframe(0.0f, 0, 0, 1.0f)); // Start at time 0, no offset, normal scale
        }

        /// <summary>
        /// Creates a new timeline with a specific name.
        /// </summary>
        /// <param name="name">The name for the timeline.</param>
        public AnimationTimeline(string name) : this() // Calls parameterless constructor first
        {
            Name = name;
        }

        /// <summary>
        /// Sorts the keyframes by timestamp. Should be called after adding/modifying keyframes.
        /// </summary>
        public void SortKeyframes()
        {
            Keyframes?.Sort((a, b) => a.Timestamp.CompareTo(b.Timestamp));
        }

        /// <summary>
        /// Gets the total duration of the animation based on the last keyframe's timestamp.
        /// Returns 0 if no keyframes exist.
        /// </summary>
        public float GetDuration()
        {
            if (Keyframes == null || Keyframes.Count == 0)
            {
                return 0.0f;
            }
            // Assuming list is sorted
            return Keyframes[Keyframes.Count - 1].Timestamp;
        }
    }
}
// <<< END NEW FILE: Data/AnimationTimeline.cs