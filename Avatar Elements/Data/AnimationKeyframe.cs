// --- MODIFIED: Data/AnimationKeyframe.cs ---
using System;
using Avatar_Elements.Data; // For Vector3, AnchorPoint, InterpolationType

namespace Avatar_Elements.Data {
    /// <summary>
    /// Represents the state of the avatar transform at a specific point in time within an animation timeline.
    /// </summary>
    public class AnimationKeyframe {
        /// <summary>
        /// The time offset (in seconds) from the start of the timeline where this keyframe applies.
        /// </summary>
        public float Timestamp { get; set; } = 0.0f;

        /// <summary>
        /// The transform state at this keyframe.
        /// X = Relative X offset (pixels).
        /// Y = Relative Y offset (pixels).
        /// Z = Scale factor (1.0 = normal size).
        /// </summary>
        public Vector3 Transform { get; set; } = new Vector3(0, 0, 1.0f); // Default: No offset, normal scale

        /// <summary>
        /// The reference point for applying scale and offset transformations.
        /// </summary>
        public AnchorPoint Anchor { get; set; } = AnchorPoint.Center;

        /// <summary>
        /// Defines how the animation interpolates *from* this keyframe *to* the next one.
        /// The interpolation type of the *last* keyframe in a timeline is ignored.
        /// </summary>
        public InterpolationType OutInterpolation { get; set; } = InterpolationType.Linear; // <<< ADDED PROPERTY

        /// <summary>
        /// Parameterless constructor for serialization.
        /// </summary>
        public AnimationKeyframe()
        {
            // Default values are set by property initializers
            Anchor = AnchorPoint.Center;
            OutInterpolation = InterpolationType.Linear; // Explicitly set default here too
        }

        /// <summary>
        /// Creates a new keyframe with specified values.
        /// </summary>
        /// <param name="timestamp">Time offset in seconds.</param>
        /// <param name="xOffset">Relative X offset in pixels.</param>
        /// <param name="yOffset">Relative Y offset in pixels.</param>
        /// <param name="scale">Scale factor.</param>
        /// <param name="anchor">The anchor point for the transform.</param>
        /// <param name="outInterpolation">The interpolation method to use when leaving this keyframe.</param> // <<< ADDED PARAM
        public AnimationKeyframe(float timestamp, float xOffset, float yOffset, float scale, AnchorPoint anchor = AnchorPoint.Center, InterpolationType outInterpolation = InterpolationType.Linear) // <<< MODIFIED CONSTRUCTOR
        {
            Timestamp = timestamp;
            Transform = new Vector3(xOffset, yOffset, scale);
            Anchor = anchor;
            OutInterpolation = outInterpolation; // <<< ADDED ASSIGNMENT
        }

        /// <summary>
        /// Creates a deep copy of the keyframe.
        /// </summary>
        public AnimationKeyframe Clone()
        {
            return new AnimationKeyframe(
                this.Timestamp,
                this.Transform.X, // Vector3 is struct, direct copy is fine
                this.Transform.Y,
                this.Transform.Z,
                this.Anchor,           // Enum is value type
                this.OutInterpolation  // Enum is value type
            );
        }
    }
}