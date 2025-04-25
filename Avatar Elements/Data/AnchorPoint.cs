// >>> START NEW FILE: Data/AnchorPoint.cs
using System;

namespace Avatar_Elements.Data {
    /// <summary>
    /// Defines the reference point used for applying scale and offset transformations during animation.
    /// </summary>
    public enum AnchorPoint {
        /// <summary>
        /// Transformations are relative to the center of the image.
        /// </summary>
        Center,
        /// <summary>
        /// Transformations keep the bottom edge fixed. Scaling occurs upwards. Offsets are relative to bottom-center.
        /// </summary>
        Bottom,
        /// <summary>
        /// Transformations keep the top edge fixed. Scaling occurs downwards. Offsets are relative to top-center.
        /// </summary>
        Top,
        /// <summary>
        /// Transformations keep the left edge fixed. Scaling occurs rightwards. Offsets are relative to left-center.
        /// </summary>
        Left,
        /// <summary>
        /// Transformations keep the right edge fixed. Scaling occurs leftwards. Offsets are relative to right-center.
        /// </summary>
        Right
        // Could potentially add TopLeft, BottomRight etc. later if needed
    }
}
// <<< END NEW FILE: Data/AnchorPoint.cs