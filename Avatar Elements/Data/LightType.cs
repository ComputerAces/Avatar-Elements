namespace Avatar_Elements.Data {
    /// <summary>
    /// Specifies the type of a light source.
    /// </summary>
    public enum LightType {
        /// <summary>
        /// A global light affecting all surfaces equally. Only Color and Intensity are typically used.
        /// </summary>
        Ambient, // <<< ADDED

        /// <summary>
        /// A light source that emits light from a single point in all directions (or within a cone if spotlight properties are set).
        /// </summary>
        Point,

        /// <summary>
        /// A light source that emits parallel rays from an infinitely distant source (like the sun).
        /// </summary>
        Directional,

        Tint
    }
}