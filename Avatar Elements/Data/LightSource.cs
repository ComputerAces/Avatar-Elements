using System;
using System.Drawing; // For Color
using Newtonsoft.Json; // For Json serialization attributes if needed

namespace Avatar_Elements.Data {
    /// <summary>
    /// Represents a light source in the scene (Point or Directional).
    /// </summary>
    public class LightSource {
        /// <summary>
        /// Gets or sets whether this light source is active.
        /// </summary>
        public bool IsEnabled { get; set; } = true;

        /// <summary>
        /// Gets or sets the type of light source.
        /// </summary>
        public LightType Type { get; set; } = LightType.Point;

        /// <summary>
        /// Gets or sets the position of the light source in normalized coordinates
        /// (-1 to +1 for X, Y, Z relative to avatar bounds/DepthScale).
        /// Used only if Type is Point.
        /// </summary>
        public Vector3 Position { get; set; } = Vector3.Zero; // Default to center

        /// <summary>
        /// Gets or sets the direction vector.
        /// If Type is Directional, this is the direction the light travels.
        /// If Type is Point, this defines the center axis for the spotlight effect.
        /// Should be a normalized vector (length 1).
        /// </summary>
        public Vector3 Direction { get; set; } = new Vector3(0, 0, -1); // Default pointing forward

        /// <summary>
        /// Gets or sets the color of the light.
        /// </summary>
        [JsonConverter(typeof(JsonColorConverter))] // Helper needed for Color serialization
        public Color Color { get; set; } = Color.White;

        /// <summary>
        /// Gets or sets the brightness intensity of the light (>= 0.0).
        /// </summary>
        public float Intensity { get; set; } = 1.0f;

        // --- Attenuation Properties (for Point lights) ---

        /// <summary>
        /// Gets or sets the constant attenuation factor (>= 0.0). Used only if Type is Point.
        /// Default is 1.0 (part of denominator, so no attenuation by default if others are 0).
        /// </summary>
        public float ConstantAttenuation { get; set; } = 1.0f;

        /// <summary>
        /// Gets or sets the linear attenuation factor (>= 0.0). Used only if Type is Point.
        /// Default is 0.0 (no linear falloff).
        /// </summary>
        public float LinearAttenuation { get; set; } = 0.0f;

        /// <summary>
        /// Gets or sets the quadratic attenuation factor (>= 0.0). Used only if Type is Point.
        /// Default is 0.0 (no quadratic falloff).
        /// </summary>
        public float QuadraticAttenuation { get; set; } = 0.0f;

        // --- Spotlight Properties (for Point lights) ---

        /// <summary>
        /// Gets or sets the spotlight cutoff half-angle in degrees (0-90).
        /// A value of 90 or greater means the light is effectively omnidirectional.
        /// Used only if Type is Point.
        /// </summary>
        public float SpotCutoffAngle { get; set; } = 90.0f; // Default: omnidirectional

        /// <summary>
        /// Gets or sets the spotlight exponent (>= 0.0). Controls the focus/softness.
        /// 0 means uniform intensity within the cone. Higher values concentrate light towards the center.
        /// Used only if Type is Point.
        /// </summary>
        public float SpotExponent { get; set; } = 0.0f; // Default: uniform cone

        /// <summary>
        /// Parameterless constructor for serialization and instantiation.
        /// </summary>
        public LightSource() { }

        // Consider adding methods here if needed, e.g., a method to ensure Direction is normalized.
        public void NormalizeDirection()
        {
            this.Direction.Normalize();
        }
        /// <summary>
        /// Creates a shallow copy of this LightSource object.
        /// </summary>
        /// <returns>A new LightSource instance with the same property values.</returns>
        public LightSource Clone()
        {
            // Vector3 is a struct, so direct assignment creates a copy.
            // Color needs conversion to be copied correctly if mutable (though System.Drawing.Color is immutable).
            // Lists/Reference types would need deep copying if they existed and were mutable.
            return new LightSource
            {
                IsEnabled = this.IsEnabled,
                Type = this.Type,
                Position = this.Position, // Struct copy
                Direction = this.Direction, // Struct copy
                Color = Color.FromArgb(this.Color.ToArgb()), // Value copy for Color
                Intensity = this.Intensity,
                ConstantAttenuation = this.ConstantAttenuation,
                LinearAttenuation = this.LinearAttenuation,
                QuadraticAttenuation = this.QuadraticAttenuation,
                SpotCutoffAngle = this.SpotCutoffAngle,
                SpotExponent = this.SpotExponent
            };
        }

    }

    // --- Helper Class for JSON Serialization of System.Drawing.Color ---
    // Place this helper class either within the same file (if simple) or in a separate Helpers file.
    // Requires reference to Newtonsoft.Json.
    public class JsonColorConverter : JsonConverter {
        public override bool CanConvert(Type objectType)
        {
            return objectType == typeof(Color);
        }

        public override void WriteJson(JsonWriter writer, object value, JsonSerializer serializer)
        {
            Color color = (Color)value;
            // Store as ARGB integer
            writer.WriteValue(color.ToArgb());
        }

        public override object ReadJson(JsonReader reader, Type objectType, object existingValue, JsonSerializer serializer)
        {
            if (reader.TokenType == JsonToken.Integer)
            {
                // Read ARGB integer
                int argb = Convert.ToInt32(reader.Value);
                return Color.FromArgb(argb);
            }
            else if (reader.TokenType == JsonToken.String)
            {
                // Optional: Handle color names or hex strings if needed
                try
                {
                    return ColorTranslator.FromHtml(reader.Value.ToString());
                }
                catch
                {
                    // Fallback or error handling
                    return Color.White; // Default fallback
                }
            }
            // Fallback for unexpected types
            return Color.White;
        }


    }


}