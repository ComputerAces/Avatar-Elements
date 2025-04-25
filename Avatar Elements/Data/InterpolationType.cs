// >>> START NEW FILE: Data/InterpolationType.cs
using System;

namespace Avatar_Elements.Data {
    /// <summary>
    /// Defines the type of easing function used for interpolation between keyframes.
    /// </summary>
    public enum InterpolationType {
        /// <summary>
        /// No easing, constant rate of change.
        /// </summary>
        Linear,
        /// <summary>
        /// Starts slow, accelerates (Quadratic).
        /// </summary>
        EaseInQuad,
        /// <summary>
        /// Starts fast, decelerates (Quadratic).
        /// </summary>
        EaseOutQuad,
        /// <summary>
        /// Starts slow, accelerates, then decelerates (Quadratic).
        /// </summary>
        EaseInOutQuad
        // Add more types later if needed (Cubic, Sine, etc.)
    }
}
// <<< END NEW FILE: Data/InterpolationType.cs