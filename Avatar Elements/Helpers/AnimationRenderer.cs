// >>> START MODIFICATION: Helpers/AnimationRenderer.cs
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.Drawing.Imaging;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading; // For CancellationToken
using System.Threading.Tasks;
using Avatar_Elements.Data; // For AnimationTimeline, Keyframe, Vector3 etc.


namespace Avatar_Elements.Helpers {
    /// <summary>
    /// Contains logic for pre-rendering animation frame data (unlit color, geometry)
    /// and applying lighting dynamically.
    /// </summary>
    public static class AnimationRenderer {
        /// <summary>
        /// Holds the pre-calculated per-pixel data for a single frame of an animation,
        /// after transformation but before lighting.
        /// </summary>
        public class FramePixelData : IDisposable {
            public int Width { get; }
            public int Height { get; }
            public Color[] Colors { get; } // Includes Alpha
            public Vector3[] WorldPositions { get; } // Stores effective world position after animation offset
            public Vector3[] WorldNormals { get; }   // Stores world-space normal

            public FramePixelData(int width, int height)
            {
                Width = width;
                Height = height;
                int pixelCount = width * height;
                Colors = new Color[pixelCount];
                WorldPositions = new Vector3[pixelCount];
                WorldNormals = new Vector3[pixelCount]; // Default Vector3 is (0,0,0)
            }

            public void SetPixel(int x, int y, Color color, Vector3 worldPos, Vector3 worldNormal)
            {
                int index = y * Width + x;
                if (index >= 0 && index < Colors.Length) // Basic bounds check
                {
                    Colors[index] = color;
                    WorldPositions[index] = worldPos;
                    WorldNormals[index] = worldNormal;
                }
            }

            public void GetPixel(int x, int y, out Color color, out Vector3 worldPos, out Vector3 worldNormal)
            {
                int index = y * Width + x;
                if (index >= 0 && index < Colors.Length)
                {
                    color = Colors[index];
                    worldPos = WorldPositions[index];
                    worldNormal = WorldNormals[index];
                }
                else
                {
                    // Return default/empty values if out of bounds
                    color = Color.Transparent;
                    worldPos = Vector3.Zero;
                    worldNormal = new Vector3(0, 0, -1); // Default normal facing viewer
                }
            }

            public void Dispose()
            {
                // Nothing explicit to dispose in this version (arrays are managed)
                // If we used unmanaged resources later, dispose them here.
                GC.SuppressFinalize(this);
            }
        }

        // --- GenerateFrameData (Replaces GenerateUnlitFrames) ---
        /// <summary>
        /// Generates pre-calculated per-pixel data (Color, World Position, World Normal)
        /// for each frame of an animation timeline.
        /// </summary>
        /// <returns>A list of FramePixelData objects, or null on failure.</returns>
        public static List<FramePixelData> GenerateFrameData(
    AnimationTimeline timeline,
    byte[] basePixelData, int baseStride, PixelFormat baseFormat,
    byte[] depthPixelData, int depthStride, PixelFormat depthFormat,
    int width, int height,
    float effectiveDepthScale, CancellationToken token, IProgress<string> progress,
    float frameRate = 30.0f)
        {
            // --- Input Validation (No change) ---
            if (timeline?.Keyframes == null || timeline.Keyframes.Count < 1 || frameRate <= 0 ||
                basePixelData == null || depthPixelData == null || width <= 0 || height <= 0)
            { Debug.WriteLine("GenerateFrameData: Invalid input parameters (data arrays, dimensions, etc.)."); return null; }
            if (baseFormat != PixelFormat.Format32bppArgb || depthFormat != PixelFormat.Format24bppRgb)
            { Debug.WriteLine($"GenerateFrameData: Unexpected pixel format received. Base:{baseFormat}, Depth:{depthFormat}"); return null; }
            if (basePixelData.Length < Math.Abs(baseStride) * (height - 1) + width * 4 || depthPixelData.Length < Math.Abs(depthStride) * (height - 1) + width * 3)
            { Debug.WriteLine($"GenerateFrameData: Pixel data array size appears insufficient for dimensions/stride."); return null; }
            // --- End Validation ---

            var results = new List<FramePixelData>();
            timeline.SortKeyframes();
            float duration = timeline.GetDuration();
            int totalFrames = Math.Max(1, (int)Math.Ceiling(duration * frameRate));
            float halfWidth = width / 2.0f;
            float halfHeight = height / 2.0f;
            const int baseBytesPerPixel = 4;
            const int depthBytesPerPixel = 3;

            progress?.Report($"Generating {totalFrames} unlit frames for '{timeline.Name}'...");
            // --- Added detailed START log ---
            var firstKeyTime = timeline.Keyframes.FirstOrDefault()?.Timestamp ?? -1f;
            var lastKeyTime = timeline.Keyframes.LastOrDefault()?.Timestamp ?? -1f;
            Debug.WriteLine($"GenerateFrameData START: Timeline='{timeline.Name}', TotalFrames={totalFrames}, Duration={duration:F3}s, FrameRate={frameRate:F1}fps, FirstKeyT={firstKeyTime:F3}, LastKeyT={lastKeyTime:F3}");
            // --- End Added ---
            Stopwatch sw = Stopwatch.StartNew();

            try
            {
                for (int i = 0; i < totalFrames; i++)
                {
                    token.ThrowIfCancellationRequested();
                    if (i % 5 == 0 || i == totalFrames - 1)
                    {
                        progress?.Report($"Generating unlit frame {i + 1}/{totalFrames}...");
                    }

                    float currentTime = (i / frameRate);
                    if (i == totalFrames - 1) { currentTime = Math.Min(currentTime, duration); }

                    // --- Log time for first few frames ---
                    if (i < 3 || Math.Abs(currentTime) < 0.01) // Log first 3 frames AND any frame very close to time 0
                    {
                        Debug.WriteLine($"  Frame {i}: Calculating state for currentTime = {currentTime:F4}s");
                    }
                    // --- End Log ---

                    AnimationKeyframe currentKeyframe = InterpolateKeyframe(timeline.Keyframes, currentTime);

                    // --- Remainder of frame generation logic (no changes) ---
                    float dx = currentKeyframe.Transform.X;
                    float dy = currentKeyframe.Transform.Y;
                    float scale = Math.Max(0.01f, currentKeyframe.Transform.Z);
                    AnchorPoint anchor = currentKeyframe.Anchor;
                    FramePixelData frameData = new FramePixelData(width, height);
                    for (int py = 0; py < height; py++)
                    {
                        for (int px = 0; px < width; px++)
                        {
                            PointF sourceF = CalculateInverseTransform(px, py, dx, dy, scale, anchor, width, height);
                            int sx = (int)Math.Round(sourceF.X);
                            int sy = (int)Math.Round(sourceF.Y);
                            Color pixelColor = Color.Transparent;
                            Vector3 worldPos = Vector3.Zero;
                            Vector3 worldNormal = new Vector3(0, 0, -1);
                            if (sx >= 0 && sx < width && sy >= 0 && sy < height)
                            {
                                byte baseA, baseR, baseG, baseB;
                                ReadPixel32bppFromArray(basePixelData, sx, sy, baseStride, width, height, out baseA, out baseR, out baseG, out baseB);
                                pixelColor = Color.FromArgb(baseA, baseR, baseG, baseB);
                                if (baseA > 0)
                                {
                                    float depthValue = GetDepthValueFromArray(sx, sy, width, height, depthPixelData, depthStride);
                                    worldNormal = CalculateNormalFromArray(sx, sy, width, height, depthPixelData, depthStride, effectiveDepthScale);
                                    float base_z = (0.5f - depthValue) * effectiveDepthScale;
                                    Vector3 baseWorldPos = new Vector3(sx - halfWidth, halfHeight - sy, base_z);
                                    worldPos = baseWorldPos + new Vector3(dx, -dy, 0);
                                }
                            }
                            frameData.SetPixel(px, py, pixelColor, worldPos, worldNormal);
                        }
                    }
                    results.Add(frameData);
                } // end frame loop
            }
            // --- Exception handling and finally block remain the same ---
            catch (OperationCanceledException)
            {
                Debug.WriteLine($"Operation cancelled during GenerateFrameData for '{timeline.Name}'.");
                foreach (var fd in results) fd?.Dispose();
                results.Clear();
                throw;
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Error during GenerateFrameData for '{timeline.Name}': {ex.Message}\n{ex.StackTrace}");
                foreach (var fd in results) fd?.Dispose();
                results.Clear();
                return null;
            }

            sw.Stop();
            progress?.Report($"Generated {results.Count} unlit frames for '{timeline.Name}'.");
            Debug.WriteLine($"Generated {results.Count} FramePixelData sets for '{timeline.Name}' in {sw.ElapsedMilliseconds} ms.");
            return results;
        }


        /// <summary>
        /// Reads ARGB values from a byte array representing 32bppArgb pixel data.
        /// </summary>
        private static void ReadPixel32bppFromArray(byte[] data, int x, int y, int stride, int width, int height, out byte a, out byte r, out byte g, out byte b)
        {
            // Bounds check for coordinates
            if (x < 0 || x >= width || y < 0 || y >= height)
            {
                a = 0; r = 0; g = 0; b = 0; // Transparent black for out of bounds
                return;
            }

            int offset = y * stride + x * 4; // 4 bytes per pixel for 32bppArgb

            // Check array bounds before reading (most critical check)
            // Ensure offset is non-negative and there are enough bytes remaining for ARGB
            if (offset >= 0 && offset + 3 < data.Length)
            {
                // Read in BGRA order (common in Windows bitmaps)
                b = data[offset + 0];
                g = data[offset + 1];
                r = data[offset + 2];
                a = data[offset + 3];
            }
            else
            {
                // Calculated offset is out of bounds for the provided byte array
                a = 0; r = 0; g = 0; b = 0;
                Debug.WriteLineIf(offset < 0 || offset + 3 >= data.Length, // Only log if calculation results in invalid index
                     $"ReadPixel32bppFromArray: Calculated offset {offset} out of bounds for array length {data.Length} at ({x},{y}). Stride={stride}");
            }
        }


        // --- GenerateLitFrames (New Method - Phase 3) ---
        /// <summary>
        /// Generates fully lit animation frames by applying lighting settings to pre-calculated FramePixelData.
        /// </summary>
        /// <param name="frameDataList">The list of pre-calculated FramePixelData for the animation.</param>
        /// <param name="globalLights">Current global light sources.</param>
        /// <param name="profileLights">Current profile-specific light sources.</param>
        /// <param name="profileSpecularIntensity">Current profile specular intensity.</param>
        /// <param name="profileSpecularPower">Current profile specular power.</param>
        /// <param name="effectiveDepthScale">The effective depth scale used during geometry calculation (needed for lighting).</param>
        /// <param name="width">Frame width.</param>
        /// <param name="height">Frame height.</param>
        /// <returns>A list of lit Bitmap frames, or null on failure.</returns>
        public static List<Bitmap> GenerateLitFrames(
    List<FramePixelData> frameDataList,
    List<LightSource> globalLights,
    List<LightSource> profileLights,
    float profileSpecularIntensity,
    float profileSpecularPower,
    float effectiveDepthScale,
    int width, int height,
    CancellationToken token, // Added
    IProgress<string> progress) // Added
        {
            // --- Input Validation ---
            if (frameDataList == null || frameDataList.Count == 0)
            { Debug.WriteLine("GenerateLitFrames: No frame data provided."); return null; }
            // --- End Validation ---

            int totalFrames = frameDataList.Count;
            progress?.Report($"Applying lighting to {totalFrames} frames..."); // Initial report
            var litFrames = new List<Bitmap>(totalFrames);
            var activeGlobalLights = globalLights?.Where(l => l != null && l.IsEnabled).ToList() ?? new List<LightSource>();
            var activeProfileLights = profileLights?.Where(l => l != null && l.IsEnabled).ToList() ?? new List<LightSource>();
            float halfWidth = width / 2.0f;
            float halfHeight = height / 2.0f;

            Debug.WriteLine($"Applying lighting to {totalFrames} frames...");
            Stopwatch sw = Stopwatch.StartNew();

            try
            {
                for (int i = 0; i < totalFrames; i++)
                {
                    // --- Cancellation Check (Start of loop) ---
                    token.ThrowIfCancellationRequested();
                    // --- Progress Report ---
                    if (i % 5 == 0 || i == totalFrames - 1) // Report less frequently + last frame
                    {
                        progress?.Report($"Applying lighting to frame {i + 1}/{totalFrames}...");
                    }

                    var frameData = frameDataList[i];
                    // Basic dimension check
                    if (frameData == null || frameData.Width != width || frameData.Height != height)
                    {
                        Debug.WriteLine($"GenerateLitFrames Error: Frame data {i} is null or dimensions mismatch.");
                        // Handle error: skip frame, throw exception, or return partial list?
                        // Throwing is often safest to indicate failure.
                        throw new ArgumentException($"Frame data at index {i} is invalid or dimensions mismatch.");
                    }


                    Bitmap litBitmap = new Bitmap(width, height, PixelFormat.Format32bppArgb);
                    BitmapData litData = null;

                    try
                    {
                        Rectangle rect = new Rectangle(0, 0, width, height);
                        litData = litBitmap.LockBits(rect, ImageLockMode.WriteOnly, litBitmap.PixelFormat);

                        // --- Loop with optional inner cancellation check ---
                        // If using Parallel.For, cancellation needs token check inside the lambda.
                        for (int py = 0; py < height; py++)
                        {
                            // Optional inner check
                            // if (py % 10 == 0) token.ThrowIfCancellationRequested();
                            for (int px = 0; px < width; px++)
                            {
                                frameData.GetPixel(px, py, out Color baseColor, out Vector3 worldPos, out Vector3 worldNormal);
                                if (baseColor.A == 0)
                                { WritePixel(litData, px, py, 0, 0, 0, 0); }
                                else
                                {
                                    Vector3 baseColor_f = ColorToFloat3(baseColor);
                                    Vector3 finalColor_f = ApplyLighting( // This call doesn't need token/progress
                                        baseColor_f, worldPos, worldNormal,
                                        activeGlobalLights, activeProfileLights,
                                        effectiveDepthScale, halfWidth, halfHeight,
                                        profileSpecularIntensity, profileSpecularPower
                                    );
                                    byte finalR = (byte)Math.Max(0, Math.Min(255, (int)(finalColor_f.X * 255.0f)));
                                    byte finalG = (byte)Math.Max(0, Math.Min(255, (int)(finalColor_f.Y * 255.0f)));
                                    byte finalB = (byte)Math.Max(0, Math.Min(255, (int)(finalColor_f.Z * 255.0f)));
                                    WritePixel(litData, px, py, baseColor.A, finalR, finalG, finalB);
                                }
                            }
                        }
                    }
                    finally
                    {
                        // Ensure UnlockBits happens even on error/cancellation within the frame processing
                        if (litData != null) try { litBitmap.UnlockBits(litData); } catch { /* Ignore */ }
                    }
                    litFrames.Add(litBitmap); // Add completed frame
                } // end frame loop
            }
            catch (OperationCanceledException)
            {
                Debug.WriteLine("GenerateLitFrames cancelled.");
                // Clean up partially created frames
                foreach (var bmp in litFrames) bmp?.Dispose();
                litFrames.Clear();
                throw; // Re-throw cancellation
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Error during GenerateLitFrames: {ex.Message}\n{ex.StackTrace}");
                foreach (var bmp in litFrames) bmp?.Dispose();
                litFrames.Clear();
                return null; // Return null or throw specific exception
            }

            sw.Stop();
            // Final progress report
            progress?.Report($"Finished applying lighting to {litFrames.Count} frames.");
            Debug.WriteLine($"Generated {litFrames.Count} lit frames in {sw.ElapsedMilliseconds} ms.");
            return litFrames;
        }



        // --- Lighting Calculation (Copied/Adapted from PreviewForm) ---
        // Static version to be called by GenerateLitFrames
        private static Vector3 ApplyLighting(Vector3 baseColor_f, Vector3 surfacePos_w, Vector3 surfaceNormal_w,
                                     List<LightSource> activeGlobalLights, List<LightSource> activeProfileLights,
                                     float effectiveDepthScale, float halfWidth, float halfHeight,
                                     float profileSpecularIntensity, float profileSpecularPower)
        {
            Vector3 accumulatedColor_f = Vector3.Zero;
            Vector3 viewDir_w = new Vector3(0, 0, -1); // Assuming view from -Z

            Action<LightSource> processLight = (light) => {
                Vector3 lightColor_f;
                Vector3 diffuseContrib = Vector3.Zero;
                Vector3 specularContrib = Vector3.Zero;

                switch (light.Type)
                {
                    case LightType.Ambient:
                        lightColor_f = ColorToFloat3(light.Color);
                        diffuseContrib = new Vector3(baseColor_f.X * lightColor_f.X, baseColor_f.Y * lightColor_f.Y, baseColor_f.Z * lightColor_f.Z) * light.Intensity;
                        break;
                    case LightType.Point:
                    case LightType.Directional:
                        lightColor_f = ColorToFloat3(light.Color);
                        float attenuation = 1.0f; float spotEffect = 1.0f;
                        Vector3 lightDir_w; // FROM surface TO light

                        if (light.Type == LightType.Point)
                        {
                            Vector3 lightPos_w = new Vector3(light.Position.X * halfWidth, light.Position.Y * halfHeight, light.Position.Z * (effectiveDepthScale * 0.5f));
                            Vector3 surfaceToLightVec = lightPos_w - surfacePos_w;
                            float distSq = surfaceToLightVec.LengthSquared(); if (distSq < 1e-5f) return;
                            float dist = (float)Math.Sqrt(distSq); lightDir_w = surfaceToLightVec / dist;
                            float denom = light.ConstantAttenuation + light.LinearAttenuation * dist + light.QuadraticAttenuation * distSq;
                            if (denom > 1e-4f) attenuation = 1.0f / denom; else attenuation = 1.0f;
                            attenuation = Math.Max(0.0f, Math.Min(1.0f, attenuation));
                            if (light.SpotCutoffAngle < 90.0f)
                            {
                                Vector3 spotAxis_w = Vector3.Normalize(light.Direction); Vector3 vectorToSurface_w = -lightDir_w;
                                float dotSpot = Vector3.Dot(vectorToSurface_w, spotAxis_w);
                                float cosCutoff = (float)Math.Cos(light.SpotCutoffAngle * Math.PI / 180.0);
                                if (dotSpot <= cosCutoff) spotEffect = 0.0f;
                                else if (light.SpotExponent > 0) spotEffect = (float)Math.Pow(dotSpot, light.SpotExponent);
                                else spotEffect = 1.0f;
                            }
                        }
                        else
                        { // Directional
                            lightDir_w = Vector3.Normalize(-light.Direction);
                            attenuation = 1.0f; spotEffect = 1.0f;
                        }

                        float dotNL = Math.Max(0.0f, Vector3.Dot(surfaceNormal_w, lightDir_w));
                        float diffuseFactor = dotNL * light.Intensity * attenuation * spotEffect;
                        diffuseContrib = new Vector3(baseColor_f.X * lightColor_f.X, baseColor_f.Y * lightColor_f.Y, baseColor_f.Z * lightColor_f.Z) * diffuseFactor;

                        if (profileSpecularIntensity > 0 && dotNL > 0)
                        {
                            Vector3 halfVector_w = Vector3.Normalize(lightDir_w + viewDir_w);
                            float dotNH = Math.Max(0.0f, Vector3.Dot(surfaceNormal_w, halfVector_w));
                            float specFactor = (float)Math.Pow(dotNH, profileSpecularPower);
                            float specIntensityFactor = profileSpecularIntensity * light.Intensity * specFactor * attenuation * spotEffect;
                            specularContrib = lightColor_f * specIntensityFactor;
                        }
                        break;
                    case LightType.Tint: break; // Handled in second pass
                }
                accumulatedColor_f += diffuseContrib + specularContrib;
            };

            // Non-tint pass
            if (activeGlobalLights != null) { foreach (var light in activeGlobalLights) { if (light.Type != LightType.Tint) processLight(light); } }
            if (activeProfileLights != null) { foreach (var light in activeProfileLights) { if (light.Type != LightType.Tint) processLight(light); } }

            // Tint pass
            Action<LightSource> processTint = (light) => {
                if (light.Type == LightType.Tint)
                {
                    Vector3 tintColor_f = ColorToFloat3(light.Color);
                    float tintIntensity = Math.Max(0.0f, Math.Min(1.0f, light.Intensity));
                    accumulatedColor_f = Lerp(accumulatedColor_f, tintColor_f, tintIntensity);
                }
            };
            if (activeGlobalLights != null) { foreach (var light in activeGlobalLights) { processTint(light); } }
            if (activeProfileLights != null) { foreach (var light in activeProfileLights) { processTint(light); } }

            return accumulatedColor_f;
        }

        // --- Helper Methods (Copied/Adapted from PreviewForm or previous version) ---
        private static AnimationKeyframe InterpolateKeyframe(List<AnimationKeyframe> sortedKeyframes, float time)
        {
            // Log only if time is very close to 0 or potentially problematic
            bool logThisCall = Math.Abs(time) < 0.05f; // Log if time is near 0
            if (logThisCall) Debug.WriteLine($"    InterpolateKeyframe called for time = {time:F4}s");

            if (sortedKeyframes == null || sortedKeyframes.Count == 0)
                return new AnimationKeyframe();

            AnimationKeyframe firstKey = sortedKeyframes[0];
            AnimationKeyframe lastKey = sortedKeyframes[sortedKeyframes.Count - 1];

            // Handle time before or exactly at the first keyframe
            if (time <= firstKey.Timestamp + 1e-6f) // Use epsilon for float comparison
            {
                if (logThisCall || time < firstKey.Timestamp - 1e-6f) // Log if significantly before or near zero
                    Debug.WriteLine($"    InterpolateKeyframe: Time {time:F4} <= FirstKeyTime {firstKey.Timestamp:F3}. Returning first keyframe clone.");
                return firstKey.Clone();
            }

            // Handle time exactly at or beyond the last keyframe
            if (time >= lastKey.Timestamp - 1e-6f)
            {
                if (logThisCall || time > lastKey.Timestamp + 1e-6f) // Log if significantly after or near zero
                    Debug.WriteLine($"    InterpolateKeyframe: Time {time:F4} >= LastKeyTime {lastKey.Timestamp:F3}. Returning last keyframe clone.");
                return lastKey.Clone();
            }


            // Find the segment (prevKey and nextKey)
            AnimationKeyframe prevKey = firstKey; // Initialize
            AnimationKeyframe nextKey = lastKey;  // Initialize
            for (int i = 1; i < sortedKeyframes.Count; i++) // Start checking from the second keyframe
            {
                if (sortedKeyframes[i].Timestamp >= time - 1e-6f) // Find the first keyframe whose time is >= current time
                {
                    nextKey = sortedKeyframes[i];
                    prevKey = sortedKeyframes[i - 1]; // The previous one is our starting key
                    break; // Found the segment
                }
            }

            if (logThisCall) Debug.WriteLine($"    InterpolateKeyframe: Using segment T=[{prevKey.Timestamp:F3} -> {nextKey.Timestamp:F3}] for time {time:F4}");

            // Calculate interpolation factor 't' within the segment
            float t = 0.0f;
            float segmentDuration = nextKey.Timestamp - prevKey.Timestamp;

            if (segmentDuration > 1e-6f)
            {
                t = (time - prevKey.Timestamp) / segmentDuration;
                t = Math.Max(0.0f, Math.Min(1.0f, t)); // Clamp t strictly between 0 and 1
            }
            else
            {
                // If duration is negligible, technically time should equal one of the keyframes
                // due to earlier checks. Return prevKey state as a safe default.
                if (logThisCall) Debug.WriteLine($"    InterpolateKeyframe: Segment duration near zero. Returning prevKey (T={prevKey.Timestamp:F3}).");
                return prevKey.Clone();
            }

            // Apply easing function based on the outgoing interpolation type of the previous keyframe
            float easedT = ApplyEasing(t, prevKey.OutInterpolation);

            if (logThisCall) Debug.WriteLine($"    InterpolateKeyframe: Raw T = {t:F4}, Eased T ({prevKey.OutInterpolation}) = {easedT:F4}");

            // Interpolate the transform values
            Vector3 interpolatedTransform = Lerp(prevKey.Transform, nextKey.Transform, easedT);
            // Anchor point is discrete, typically use the starting keyframe's anchor for the segment
            AnchorPoint interpolatedAnchor = prevKey.Anchor;

            // Create and return the calculated state at the requested time
            var resultKey = new AnimationKeyframe(time, interpolatedTransform.X, interpolatedTransform.Y, interpolatedTransform.Z, interpolatedAnchor, prevKey.OutInterpolation); // Include interp type for reference
            if (logThisCall) Debug.WriteLine($"    InterpolateKeyframe: Result Transform = ({resultKey.Transform.X:F1}, {resultKey.Transform.Y:F1}, {resultKey.Transform.Z:F2})");
            return resultKey;
        }

        // --- ADDED: Easing Function Helper ---
        // In Helpers/AnimationRenderer.cs
        /// <summary>
        /// Applies an easing function to the interpolation factor 't'.
        /// </summary>
        /// <param name="t">The raw interpolation factor (0.0 to 1.0).</param>
        /// <param name="type">The type of easing to apply.</param>
        /// <returns>The eased interpolation factor.</returns>
        private static float ApplyEasing(float t, InterpolationType type)
        {
            // Clamp t just in case, although InterpolateKeyframe should already do this
            t = Math.Max(0.0f, Math.Min(1.0f, t));

            switch (type)
            {
                case InterpolationType.EaseInQuad:
                    // Formula: t^2
                    return t * t;

                case InterpolationType.EaseOutQuad:
                    // Formula: 1 - (1-t)^2  which simplifies to t * (2f - t)
                    return t * (2f - t);

                case InterpolationType.EaseInOutQuad:
                    // Formula:
                    // t < 0.5 ? 2 * t^2
                    // t >= 0.5 ? 1 - pow(-2*t + 2, 2) / 2  which simplifies to -1 + t * (4f - 2f * t)
                    if (t < 0.5f)
                    {
                        return 2f * t * t;
                    }
                    else
                    {
                        // Simplified version: -1 + (4 * t) - (2 * t * t)
                        return -1f + t * (4f - 2f * t);
                    }

                case InterpolationType.Linear:
                default:
                    // No easing, return original t
                    return t;
            }
            // Future easing functions (Cubic, Sine, etc.) can be added here.
            /*
            Example Cubic Easing:
            case InterpolationType.EaseInCubic:
                return t * t * t;
            case InterpolationType.EaseOutCubic:
                float t1 = 1f - t;
                return 1f - t1 * t1 * t1; // 1 - (1-t)^3
            case InterpolationType.EaseInOutCubic:
                if (t < 0.5f)
                {
                    return 4f * t * t * t; // 4 * t^3
                }
                else
                {
                     float t2 = -2f * t + 2f;
                     return 1f - (t2 * t2 * t2) / 2f; // 1 - ((-2t+2)^3)/2
                }
            */
        }

        private static PointF CalculateInverseTransform(int px, int py, float dx, float dy, float scale, AnchorPoint anchor, int width, int height)
        {
            float centerX = width / 2.0f; float centerY = height / 2.0f;
            float translatedPx = px - dx; float translatedPy = py - dy; // Invert translation
            float sourceX = translatedPx; float sourceY = translatedPy;
            float anchorX = centerX; float anchorY = centerY;
            switch (anchor)
            {
                case AnchorPoint.Top: anchorY = 0; break;
                case AnchorPoint.Bottom: anchorY = height; break;
                case AnchorPoint.Left: anchorX = 0; break;
                case AnchorPoint.Right: anchorX = width; break;
                case AnchorPoint.Center: default: break;
            }
            if (Math.Abs(scale) > 1e-4f)
            { // Invert scale relative to anchor
                sourceX = anchorX + (translatedPx - anchorX) / scale;
                sourceY = anchorY + (translatedPy - anchorY) / scale;
            }
            else { sourceX = centerX; sourceY = centerY; } // Fallback for zero scale
            return new PointF(sourceX, sourceY);
        }

        private static byte ReadDepthByte(BitmapData depthData, int x, int y, int bytesPerPixel)
        { int offset = y * depthData.Stride + x * bytesPerPixel; return Marshal.ReadByte(depthData.Scan0, offset); }

        private static void ReadPixel32bpp(BitmapData data, int x, int y, out byte a, out byte r, out byte g, out byte b)
        { int offset = y * data.Stride + x * 4; b = Marshal.ReadByte(data.Scan0, offset + 0); g = Marshal.ReadByte(data.Scan0, offset + 1); r = Marshal.ReadByte(data.Scan0, offset + 2); a = Marshal.ReadByte(data.Scan0, offset + 3); }

        private static unsafe void WritePixel(BitmapData data, int x, int y, byte a, byte r, byte g, byte b)
        { byte* ptr = (byte*)data.Scan0; int offset = y * data.Stride + x * 4; ptr[offset + 0] = b; ptr[offset + 1] = g; ptr[offset + 2] = r; ptr[offset + 3] = a; }

        private static unsafe void WritePixel(BitmapData data, int x, int y, byte r, byte g, byte b) // For 24bpp
        { byte* ptr = (byte*)data.Scan0; int offset = y * data.Stride + x * 3; ptr[offset + 0] = b; ptr[offset + 1] = g; ptr[offset + 2] = r; }

        /// <summary>
        /// Reads depth value (0-1) from a byte array (assuming Format24bppRgb, Blue channel).
        /// Clamps coordinates.
        /// </summary>
        /// <summary>
        /// Reads depth value (0-1) from a byte array (assuming Format24bppRgb, Blue channel).
        /// Clamps coordinates.
        /// </summary>
        private static float GetDepthValueFromArray(int x, int y, int width, int height, byte[] depthPixelData, int depthStride)
        {
            // Clamp coordinates first to ensure valid indexing within row boundaries
            x = Math.Max(0, Math.Min(width - 1, x));
            y = Math.Max(0, Math.Min(height - 1, y));

            int offset = y * depthStride + x * 3; // 3 bytes per pixel for 24bppRgb

            // Check array bounds before reading
            // We only need the byte at the exact offset (Blue channel)
            if (offset >= 0 && offset < depthPixelData.Length)
            {
                // Read the Blue channel (offset + 0). Assuming grayscale was stored here during conversion.
                return depthPixelData[offset] / 255.0f;
            }
            else
            {
                Debug.WriteLineIf(offset < 0 || offset >= depthPixelData.Length, // Log only if index truly invalid
                     $"GetDepthValueFromArray: Calculated offset {offset} out of bounds for array length {depthPixelData.Length} at ({x},{y}). Stride={depthStride}");
                return 0.5f; // Default middle depth on error
            }
        }

        private static Vector3 CalculateNormalFromArray(int x, int y, int width, int height, byte[] depthPixelData, int depthStride, float effectiveDepthScale)
        {
            // Read neighbouring depth values using the array helper
            // This automatically handles clamping coordinates via GetDepthValueFromArray
            float d00 = GetDepthValueFromArray(x - 1, y - 1, width, height, depthPixelData, depthStride);
            float d10 = GetDepthValueFromArray(x, y - 1, width, height, depthPixelData, depthStride);
            float d20 = GetDepthValueFromArray(x + 1, y - 1, width, height, depthPixelData, depthStride);
            float d01 = GetDepthValueFromArray(x - 1, y, width, height, depthPixelData, depthStride);
            float d21 = GetDepthValueFromArray(x + 1, y, width, height, depthPixelData, depthStride);
            float d02 = GetDepthValueFromArray(x - 1, y + 1, width, height, depthPixelData, depthStride);
            float d12 = GetDepthValueFromArray(x, y + 1, width, height, depthPixelData, depthStride);
            float d22 = GetDepthValueFromArray(x + 1, y + 1, width, height, depthPixelData, depthStride);

            // Sobel filter for gradients
            float gradientX = (d20 + 2.0f * d21 + d22) - (d00 + 2.0f * d01 + d02);
            float gradientY = (d02 + 2.0f * d12 + d22) - (d00 + 2.0f * d10 + d20);

            // Construct normal vector
            Vector3 normal = new Vector3(gradientX * effectiveDepthScale, -gradientY * effectiveDepthScale, -2.0f); // Assuming Z points out, Y points up in world
            normal.Normalize();

            // Handle potential zero-length normal (e.g., flat area)
            if (normal.LengthSquared() < 1e-6f)
            {
                return new Vector3(0, 0, -1); // Default normal pointing towards viewer
            }
            return normal;
        }


        private static Color EncodeNormalToColor(Vector3 normal)
        { int r = (int)Math.Max(0, Math.Min(255, (normal.X + 1.0f) * 0.5f * 255.0f)); int g = (int)Math.Max(0, Math.Min(255, (normal.Y + 1.0f) * 0.5f * 255.0f)); int b = (int)Math.Max(0, Math.Min(255, (normal.Z + 1.0f) * 0.5f * 255.0f)); return Color.FromArgb(255, r, g, b); }
        // Needed for ApplyLighting
        private static Vector3 ColorToFloat3(Color c) => new Vector3(c.R / 255.0f, c.G / 255.0f, c.B / 255.0f);
        private static Vector3 Lerp(Vector3 a, Vector3 b, float t) { t = Math.Max(0.0f, Math.Min(1.0f, t)); return a * (1.0f - t) + b * t; }

    }
}
// <<< END MODIFICATION: Helpers/AnimationRenderer.cs