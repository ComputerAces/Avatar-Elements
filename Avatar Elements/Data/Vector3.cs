using System;

namespace Avatar_Elements.Data {
    /// <summary>
    /// Represents a 3D vector or point using single-precision floating-point values.
    /// </summary>
    public struct Vector3 {
        public float X;
        public float Y;
        public float Z;

        public static readonly Vector3 Zero = new Vector3(0f, 0f, 0f);
        public static readonly Vector3 One = new Vector3(1f, 1f, 1f);
        // Add common vectors if needed (e.g., UnitX, UnitY, UnitZ)

        public Vector3(float x, float y, float z)
        {
            X = x;
            Y = y;
            Z = z;
        }

        public float Length()
        {
            return (float)Math.Sqrt(X * X + Y * Y + Z * Z);
        }

        public float LengthSquared()
        {
            return X * X + Y * Y + Z * Z;
        }

        public void Normalize()
        {
            float length = Length();
            if (length > 1e-6f) // Avoid division by zero or near-zero
            {
                float invLength = 1.0f / length;
                X *= invLength;
                Y *= invLength;
                Z *= invLength;
            }
            else
            {
                X = 0;
                Y = 0;
                Z = 0; // Or handle as an error/special case
            }
        }

        public static Vector3 Normalize(Vector3 value)
        {
            Vector3 result = value;
            result.Normalize();
            return result;
        }

        public static float Dot(Vector3 vector1, Vector3 vector2)
        {
            return vector1.X * vector2.X + vector1.Y * vector2.Y + vector1.Z * vector2.Z;
        }

        // --- Operator Overloads ---
        public static Vector3 operator +(Vector3 a, Vector3 b)
        {
            return new Vector3(a.X + b.X, a.Y + b.Y, a.Z + b.Z);
        }

        public static Vector3 operator -(Vector3 a, Vector3 b)
        {
            return new Vector3(a.X - b.X, a.Y - b.Y, a.Z - b.Z);
        }

        public static Vector3 operator -(Vector3 a) // Negation
        {
            return new Vector3(-a.X, -a.Y, -a.Z);
        }

        public static Vector3 operator *(Vector3 vector, float scaleFactor)
        {
            return new Vector3(vector.X * scaleFactor, vector.Y * scaleFactor, vector.Z * scaleFactor);
        }

        public static Vector3 operator *(float scaleFactor, Vector3 vector)
        {
            return new Vector3(vector.X * scaleFactor, vector.Y * scaleFactor, vector.Z * scaleFactor);
        }

        public static Vector3 operator /(Vector3 vector, float divisor)
        {
            float factor = 1.0f / divisor;
            return new Vector3(vector.X * factor, vector.Y * factor, vector.Z * factor);
        }

        // --- Equality ---
        public override bool Equals(object obj)
        {
            return obj is Vector3 vector && Equals(vector);
        }

        public bool Equals(Vector3 other)
        {
            // Use tolerance for float comparison if needed
            return X == other.X && Y == other.Y && Z == other.Z;
        }

        public override int GetHashCode()
        {
            int hashCode = 17; // Use prime numbers
            hashCode = hashCode * 31 + X.GetHashCode();
            hashCode = hashCode * 31 + Y.GetHashCode();
            hashCode = hashCode * 31 + Z.GetHashCode();
            return hashCode;
        }

        public static bool operator ==(Vector3 left, Vector3 right)
        {
            return left.Equals(right);
        }

        public static bool operator !=(Vector3 left, Vector3 right)
        {
            return !(left == right);
        }

        public override string ToString()
        {
            return $"({X:F2}, {Y:F2}, {Z:F2})"; // Format to 2 decimal places for readability
        }
    }
}