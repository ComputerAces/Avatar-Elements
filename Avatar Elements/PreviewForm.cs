// >>> START MODIFICATION: PreviewForm.cs (Full File)
using System;
using System.Drawing;
using System.Windows.Forms;
using System.Runtime.InteropServices;
using System.Drawing.Imaging;
using System.IO;
using System.Diagnostics;
using System.Linq;
using System.Collections.Generic;
using System.Threading.Tasks; // For Parallel.For
using System.Windows.Forms;

// Reference your project's namespaces
using Avatar_Elements.Data;
using Avatar_Elements.Helpers;
using System.ComponentModel;

namespace Avatar_Elements {
    public partial class PreviewForm : Form {
        // --- Member Variables ---
        private const float DepthCalculationMultiplier = 10.0f; // Internal multiplier

        // Image/Cache Data
        private Bitmap _baseAvatar = null;
        private Bitmap _depthMap = null;
        private Bitmap _cachedRenderedAvatar = null; // Holds the static, lit avatar

        // Lighting/Profile Data
        private List<LightSource> _globalLights = new List<LightSource>();
        private List<LightSource> _profileLights = new List<LightSource>();
        private float _depthScale = 1.0f;
        private float _specularIntensity = 0.5f;
        private float _specularPower = 32.0f;

        // Window State/Dragging
        private Point _dragOffset;
        private bool _isDragging = false;
        private Point? _initialPosition = null;
        public event EventHandler<Point> PositionChanged;
        private bool _isLoadPending = false;

        // Animation Playback State
        private Timer _animationTimer = null;
        private List<Bitmap> _playbackFrames = null; // Holds reference to pre-rendered LIT frames
        private int _currentFrameIndex = 0;
        private bool _loopPlayback = false;
        private const int DEFAULT_FRAME_INTERVAL_MS = 33; // Approx 30 fps

        private Size _originalStaticSize = Size.Empty; // To store size when showing static image
        private Point _lastSavedPosition = Point.Empty;

        /// <summary>
        /// Gets the file path of the currently loaded base avatar image.
        /// </summary>
        public string CurrentBasePath { get; private set; } = null;

        /// <summary>
        /// Gets the file path of the currently loaded depth map image.
        /// </summary>
        public string CurrentDepthPath { get; private set; } = null;

        private Bitmap _baseAvatarBitmap = null;    // Holds the loaded base image
        private Bitmap _depthMapBitmap = null;      // Holds the loaded depth map

        private bool _initialDrawComplete = false;


        // --- Constructor ---
        public PreviewForm(Point? initialPosition = null, Size? initialSize = null)
        {
            InitializeComponent();
            this.FormBorderStyle = FormBorderStyle.None;
            this.StartPosition = FormStartPosition.Manual;

            // Ensure Event Handlers are Wired Up AFTER InitializeComponent
            this.MouseDown += PreviewForm_MouseDown;
            this.MouseMove += PreviewForm_MouseMove;
            this.MouseUp += PreviewForm_MouseUp;
            this.Load += PreviewForm_Load; // Make sure this is here!

            // Size and Position logic
            if (initialSize.HasValue && initialSize.Value.Width > 0 && initialSize.Value.Height > 0) { this.ClientSize = initialSize.Value; } else { this.ClientSize = new Size(256, 256); }
            if (initialPosition.HasValue) { this.Location = initialPosition.Value; } else { this.Location = CalculateCenterScreenPosition(this.ClientSize); }
            _lastSavedPosition = this.Location;

            InitializeTimer();
            Debug.WriteLine($"PreviewForm Constructor: Initialized. Size={this.ClientSize}, Pos={this.Location}");
        }

        protected override void OnHandleCreated(EventArgs e)
        {
            base.OnHandleCreated(e);
            Debug.WriteLine($"PreviewForm OnHandleCreated fired. IsDisposed={this.IsDisposed}.");
            // Initial draw is now handled by the Load event.
            // We could potentially call ApplyEffectsAndCache here if needed earlier,
            // but Load event is safer place to also do the drawing.
        }

        /// <summary>
        /// Sets the size of the preview form, typically called when the active profile changes.
        /// Optionally recenters the form after resizing.
        /// </summary>
        /// <param name="newSize">The required size for the form.</param>
        public void SetFixedSize(Size newSize)
        {
            // Ensure running on UI thread
            Action setSizeAction = () => {
                if (this.IsDisposed || newSize.Width <= 0 || newSize.Height <= 0) return;

                if (this.Size != newSize)
                {
                    Debug.WriteLine($"SetFixedSize: Resizing PreviewForm from {this.Size} to {newSize}");
                    try
                    {
                        // Optional: Keep center fixed
                        Point oldCenter = new Point(this.Left + this.Width / 2, this.Top + this.Height / 2);
                        this.Size = newSize;
                        Point newLocation = new Point(oldCenter.X - this.Width / 2, oldCenter.Y - this.Height / 2);
                        this.Location = newLocation;
                        SavePosition(this.Location); // Save new position
                    }
                    catch (Exception ex)
                    {
                        Debug.WriteLine($"SetFixedSize: Error resizing/repositioning: {ex.Message}");
                        try { this.Size = newSize; } catch { } // Attempt simple resize
                    }
                }
                else
                {
                    Debug.WriteLine($"SetFixedSize: PreviewForm already at target size {newSize}.");
                }
            };

            if (this.InvokeRequired) { if (!this.IsDisposed) try { this.BeginInvoke(setSizeAction); } catch {/*Ignore*/} }
            else { setSizeAction(); }
        }

        // --- Load Event ---
        private void PreviewForm_Load(object sender, EventArgs e)
        {
            Debug.WriteLine($"PreviewForm_Load: Event fired. IsHandleCreated={this.IsHandleCreated}, IsDisposed={this.IsDisposed}.");
            // Handle is created here. Perform the initial static render and display.
            if (this.IsDisposed) return; // Extra check

            if (_baseAvatarBitmap != null && _depthMapBitmap != null) // Check if images were loaded
            {
                Debug.WriteLine("PreviewForm_Load: Images loaded, calling ApplyEffectsAndCache...");
                ApplyEffectsAndCache(); // Calculate the static lit cache

                if (_cachedRenderedAvatar != null)
                {
                    Debug.WriteLine("PreviewForm_Load: Cache generated, calling UpdateFormDisplay...");
                    UpdateFormDisplay(); // Display the generated cache
                    _initialDrawComplete = true; // Mark initial draw as done
                }
                else
                {
                    Debug.WriteLine("PreviewForm_Load: ApplyEffectsAndCache resulted in null cache. Display will be blank.");
                    UpdateFormDisplay(); // Attempt to clear display
                    _initialDrawComplete = false; // Mark as not complete
                }
            }
            else
            {
                Debug.WriteLine("PreviewForm_Load: Base or Depth bitmap is null. Cannot render initial view.");
                _cachedRenderedAvatar?.Dispose(); // Ensure cache is clear
                _cachedRenderedAvatar = null;
                UpdateFormDisplay(); // Attempt to clear display
                _initialDrawComplete = false; // Mark as not complete
            }
        }

        // --- Overrides for Layered Window ---
        protected override CreateParams CreateParams
        {
            get
            {
                CreateParams cp = base.CreateParams;
                cp.ExStyle |= (int)NativeMethods.WS_EX_LAYERED;
                return cp;
            }
        }
        protected override void OnPaintBackground(PaintEventArgs e) { /* Do nothing */ }
        protected override void OnPaint(PaintEventArgs e) { /* Do nothing */ }

        // --- Public Methods ---

        /// <summary>
        /// Loads the base avatar and depth map images. Stops animation, disposes old images.
        /// Does not apply lighting or update display; RefreshEffects should be called after this.
        /// </summary>
        /// <returns>True if images were loaded successfully or cleared successfully, false otherwise.</returns>
        public bool LoadAvatar(string baseImagePath, string depthMapPath)
        {
            Debug.WriteLine($"PreviewForm: Loading Avatar '{baseImagePath ?? "null"}', Depth '{depthMapPath ?? "null"}'");
            StopPlayback(false); // Stop any animation

            // --- Dispose previous bitmaps ---
            DisposeBitmaps(disposeStaticCache: true); // Clear static cache and loaded images
            _initialDrawComplete = false; // Reset draw flag
                                          // --- End Dispose ---

            this.CurrentBasePath = baseImagePath;
            this.CurrentDepthPath = depthMapPath;

            // Handle null/empty/missing paths
            if (string.IsNullOrEmpty(baseImagePath) || !File.Exists(baseImagePath) ||
                string.IsNullOrEmpty(depthMapPath) || !File.Exists(depthMapPath))
            {
                Debug.WriteLine("LoadAvatar: Invalid or missing image path(s).");
                this.CurrentBasePath = null; this.CurrentDepthPath = null;
                // Ensure display is cleared if form already visible
                if (this.IsHandleCreated && !this.IsDisposed) UpdateFormDisplay();
                return false;
            }

            bool success = false;
            Bitmap loadedBase = null;
            Bitmap loadedDepth = null;

            try
            {
                // Load Base Image
                byte[] baseBytes = File.ReadAllBytes(baseImagePath);
                using (var ms = new MemoryStream(baseBytes)) { loadedBase = new Bitmap(ms); }
                // Ensure base image is 32bppArgb
                if (loadedBase.PixelFormat != PixelFormat.Format32bppArgb)
                {
                    Debug.WriteLine($"Converting base image from {loadedBase.PixelFormat} to {PixelFormat.Format32bppArgb}");
                    Bitmap convertedBase = new Bitmap(loadedBase.Width, loadedBase.Height, PixelFormat.Format32bppArgb);
                    using (Graphics g = Graphics.FromImage(convertedBase)) { g.Clear(Color.Transparent); g.DrawImageUnscaled(loadedBase, 0, 0); }
                    loadedBase.Dispose(); loadedBase = convertedBase;
                }

                // Load Depth Map
                byte[] depthBytes = File.ReadAllBytes(depthMapPath);
                using (var ms = new MemoryStream(depthBytes)) { loadedDepth = new Bitmap(ms); }

                // Validate Dimensions
                if (loadedBase.Size != loadedDepth.Size)
                {
                    Debug.WriteLine("LoadAvatar Warning: Base/Depth dimension mismatch.");
                    MessageBox.Show($"Base image dimensions ({loadedBase.Width}x{loadedBase.Height}) do not match depth map dimensions ({loadedDepth.Width}x{loadedDepth.Height}). Lighting may be incorrect.",
                                    "Dimension Mismatch", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                }
                if (loadedBase.Width <= 0 || loadedBase.Height <= 0) { throw new ArgumentException("Loaded base image has invalid dimensions."); }

                // --- Assign to Member Fields ---
                _baseAvatarBitmap = loadedBase; loadedBase = null; // Transfer ownership
                _depthMapBitmap = loadedDepth; loadedDepth = null; // Transfer ownership
                success = true;
                // --- End Assign ---

                // --- Removed size setting and _isLoadPending ---
            }
            catch (FileNotFoundException fnfEx) { MessageBox.Show($"Error loading avatar images: File not found.\n{fnfEx.FileName}", "Load Error", MessageBoxButtons.OK, MessageBoxIcon.Error); success = false; }
            catch (OutOfMemoryException oomEx) { MessageBox.Show($"Error loading avatar images: Out of memory.\n{oomEx.Message}", "Load Error", MessageBoxButtons.OK, MessageBoxIcon.Error); success = false; }
            catch (Exception ex) { MessageBox.Show($"An unexpected error occurred loading avatar images:\n{ex.Message}", "Load Error", MessageBoxButtons.OK, MessageBoxIcon.Error); success = false; }
            finally
            {
                loadedBase?.Dispose();
                loadedDepth?.Dispose();
                if (!success) // Cleanup if load failed
                {
                    DisposeBitmaps(disposeStaticCache: true); // Clear everything including member fields
                    CurrentBasePath = null; CurrentDepthPath = null;
                    if (this.IsHandleCreated && !this.IsDisposed) UpdateFormDisplay(); // Attempt to clear display
                }
            }

            Debug.WriteLine($"PreviewForm: LoadAvatar finished (Success={success}).");
            // Render/Display is triggered later by ControlPanelForm or Load event
            return success;
        }


        /// <summary>
        /// Applies new lighting settings and profile properties, recalculates the static lit frame,
        /// and updates the display. Stops any running animation.
        /// </summary>
        public void RefreshEffects(List<LightSource> globalLights, List<LightSource> profileLights, float depthScale, float specularIntensity, float specularPower)
        {
            // --- ADDED Handle Check ---
            if (!this.IsHandleCreated || this.IsDisposed)
            {
                Debug.WriteLine("RefreshEffects skipped: Handle not created or form disposed.");
                // Store settings anyway so they are ready when Load happens? Or rely on ControlPanel calling again?
                // Let's just store them.
                _globalLights = globalLights ?? new List<LightSource>();
                _profileLights = profileLights ?? new List<LightSource>();
                _depthScale = Math.Max(0.001f, depthScale);
                _specularIntensity = specularIntensity;
                _specularPower = specularPower;
                return;
            }
            // --- END Handle Check ---

            Debug.WriteLine($"PreviewForm: Refreshing effects. DepthScale={depthScale:F2}, SpecIntensity={specularIntensity:F2}, SpecPower={specularPower:F1}");
            StopPlayback(false); // Stop animation

            // Store new settings
            _globalLights = globalLights ?? new List<LightSource>();
            _profileLights = profileLights ?? new List<LightSource>();
            _depthScale = Math.Max(0.001f, depthScale);
            _specularIntensity = specularIntensity;
            _specularPower = specularPower;

            ApplyEffectsAndCache(); // Recalculate static cache
            UpdateFormDisplay();    // Update display with static cache
        }

        // --- Internal Rendering & Display Logic ---

        /// <summary>
        /// Calculates and caches the statically lit avatar frame (_cachedRenderedAvatar).
        /// </summary>
        private void ApplyEffectsAndCache()
        {
            // --- Use member fields _baseAvatarBitmap and _depthMapBitmap ---
            if (_baseAvatarBitmap == null || _depthMapBitmap == null || _depthScale <= 0)
            { Debug.WriteLine("ApplyEffectsAndCache skipped: Base/Depth map missing or invalid depth scale."); _cachedRenderedAvatar?.Dispose(); _cachedRenderedAvatar = null; return; }
            if (_baseAvatarBitmap.Size != _depthMapBitmap.Size)
            { Debug.WriteLine("ApplyEffectsAndCache skipped: Base/Depth map dimension mismatch."); _cachedRenderedAvatar?.Dispose(); _cachedRenderedAvatar = null; return; }
            // --- End Use member fields ---

            int width = _baseAvatarBitmap.Width;
            int height = _baseAvatarBitmap.Height;
            float halfWidth = width / 2.0f;
            float halfHeight = height / 2.0f;
            float effectiveDepthScale = _depthScale * DepthCalculationMultiplier;

            List<LightSource> enabledGlobalLights = _globalLights?.Where(l => l != null && l.IsEnabled).ToList() ?? new List<LightSource>();
            List<LightSource> enabledProfileLights = _profileLights?.Where(l => l != null && l.IsEnabled).ToList() ?? new List<LightSource>();

            Debug.WriteLine($"ApplyEffectsAndCache START: Eff Scale={effectiveDepthScale:F2}, Globals={enabledGlobalLights.Count}, Profiles={enabledProfileLights.Count}, SpecInt={_specularIntensity:F2}, SpecPow={_specularPower:F1}");
            Stopwatch sw = Stopwatch.StartNew();

            _cachedRenderedAvatar?.Dispose();
            _cachedRenderedAvatar = new Bitmap(width, height, PixelFormat.Format32bppArgb);

            BitmapData baseData = null;
            BitmapData depthData = null;
            BitmapData targetData = null;

            try
            {
                Rectangle rect = new Rectangle(0, 0, width, height);
                // --- Use member fields for LockBits ---
                baseData = _baseAvatarBitmap.LockBits(rect, ImageLockMode.ReadOnly, PixelFormat.Format32bppArgb);
                depthData = _depthMapBitmap.LockBits(rect, ImageLockMode.ReadOnly, _depthMapBitmap.PixelFormat);
                // --- End Use member fields ---
                targetData = _cachedRenderedAvatar.LockBits(rect, ImageLockMode.WriteOnly, PixelFormat.Format32bppArgb);

                IntPtr basePtr = baseData.Scan0; IntPtr depthPtr = depthData.Scan0; IntPtr targetPtr = targetData.Scan0;
                int baseStride = baseData.Stride; int depthStride = depthData.Stride; int targetStride = targetData.Stride;
                int baseBytesPerPixel = 4; int depthBytesPerPixel = Image.GetPixelFormatSize(_depthMapBitmap.PixelFormat) / 8;
                bool isDepthGrayscale = (depthBytesPerPixel == 1);
                if (depthBytesPerPixel < 1) { throw new InvalidOperationException("Depth map has invalid pixel format."); }

                Parallel.For(0, height, y =>
                {
                    // --- Pixel processing loop remains the same ---
                    for (int x = 0; x < width; x++)
                    {
                        int baseOffset = y * baseStride + x * baseBytesPerPixel; int targetOffset = y * targetStride + x * baseBytesPerPixel;
                        byte baseB = 0, baseG = 0, baseR = 0, baseA = 0;
                        if (baseOffset >= 0 && baseOffset + 3 < baseBytesPerPixel * width * height) // Basic bounds check
                        {
                            baseB = Marshal.ReadByte(basePtr, baseOffset); baseG = Marshal.ReadByte(basePtr, baseOffset + 1);
                            baseR = Marshal.ReadByte(basePtr, baseOffset + 2); baseA = Marshal.ReadByte(basePtr, baseOffset + 3);
                        }
                        Vector3 finalColor_f;

                        if (baseA == 0) { finalColor_f = Vector3.Zero; }
                        else
                        {
                            float depthValue = GetDepthValue(x, y, width, height, depthData, depthBytesPerPixel, isDepthGrayscale);
                            float z_coordinate = (0.5f - depthValue) * effectiveDepthScale;
                            Vector3 surfacePos = new Vector3(x - halfWidth, halfHeight - y, z_coordinate);
                            Vector3 surfaceNormal = CalculateNormal(x, y, width, height, depthData, depthBytesPerPixel, effectiveDepthScale, isDepthGrayscale);
                            Vector3 baseColor_f = new Vector3(baseR / 255.0f, baseG / 255.0f, baseB / 255.0f);
                            finalColor_f = ApplyLighting(baseColor_f, surfacePos, surfaceNormal, enabledGlobalLights, enabledProfileLights, effectiveDepthScale, halfWidth, halfHeight, _specularIntensity, _specularPower);
                        }
                        if (targetOffset >= 0 && targetOffset + 3 < baseBytesPerPixel * width * height) // Basic bounds check
                        {
                            byte finalR = (byte)Math.Max(0, Math.Min(255, (int)(finalColor_f.X * 255.0f)));
                            byte finalG = (byte)Math.Max(0, Math.Min(255, (int)(finalColor_f.Y * 255.0f)));
                            byte finalB = (byte)Math.Max(0, Math.Min(255, (int)(finalColor_f.Z * 255.0f)));
                            Marshal.WriteByte(targetPtr, targetOffset, finalB); Marshal.WriteByte(targetPtr, targetOffset + 1, finalG);
                            Marshal.WriteByte(targetPtr, targetOffset + 2, finalR); Marshal.WriteByte(targetPtr, targetOffset + 3, baseA);
                        }
                    }
                }); // End Parallel.For

                sw.Stop();
                Debug.WriteLine($"PreviewForm: Effects applied and cached in {sw.ElapsedMilliseconds} ms.");
            }
            catch (Exception ex) { Debug.WriteLine($"PreviewForm: Error during ApplyEffectsAndCache: {ex.Message}\n{ex.StackTrace}"); _cachedRenderedAvatar?.Dispose(); _cachedRenderedAvatar = null; }
            finally
            {
                // --- Use member fields for UnlockBits ---
                if (baseData != null && _baseAvatarBitmap != null) try { _baseAvatarBitmap.UnlockBits(baseData); } catch { }
                if (depthData != null && _depthMapBitmap != null) try { _depthMapBitmap.UnlockBits(depthData); } catch { }
                if (targetData != null && _cachedRenderedAvatar != null) try { _cachedRenderedAvatar.UnlockBits(targetData); } catch { }
                // --- End Use member fields ---
            }
        }

        private float GetDepthValue(int x, int y, int width, int height, BitmapData depthData, int bytesPerPixel, bool isDepthGrayscale)
        { /* ... (implementation remains same) ... */
            x = Math.Max(0, Math.Min(width - 1, x)); y = Math.Max(0, Math.Min(height - 1, y)); int offset = y * depthData.Stride + x * bytesPerPixel;
            try { return Marshal.ReadByte(depthData.Scan0, offset) / 255.0f; } catch { return 0.5f; }
        }

        private Vector3 CalculateNormal(int x, int y, int width, int height, BitmapData depthData, int bytesPerPixel, float effectiveDepthScale, bool isDepthGrayscale)
        { /* ... (implementation remains same, uses local GetDepthValue) ... */
            float d00 = GetDepthValueLocalNorm(x - 1, y - 1, width, height, depthData, bytesPerPixel); float d10 = GetDepthValueLocalNorm(x, y - 1, width, height, depthData, bytesPerPixel);
            float d20 = GetDepthValueLocalNorm(x + 1, y - 1, width, height, depthData, bytesPerPixel); float d01 = GetDepthValueLocalNorm(x - 1, y, width, height, depthData, bytesPerPixel);
            float d21 = GetDepthValueLocalNorm(x + 1, y, width, height, depthData, bytesPerPixel); float d02 = GetDepthValueLocalNorm(x - 1, y + 1, width, height, depthData, bytesPerPixel);
            float d12 = GetDepthValueLocalNorm(x, y + 1, width, height, depthData, bytesPerPixel); float d22 = GetDepthValueLocalNorm(x + 1, y + 1, width, height, depthData, bytesPerPixel);
            float gradientX = (d20 + 2.0f * d21 + d22) - (d00 + 2.0f * d01 + d02); float gradientY = (d02 + 2.0f * d12 + d22) - (d00 + 2.0f * d10 + d20);
            Vector3 normal = new Vector3(gradientX * effectiveDepthScale, -gradientY * effectiveDepthScale, -2.0f); normal.Normalize();
            if (normal.LengthSquared() < 1e-4f) return new Vector3(0, 0, -1); return normal;
        }
        // Helper for CalculateNormal to avoid repeated checks/try-catch
        private float GetDepthValueLocalNorm(int x, int y, int width, int height, BitmapData depthData, int bytesPerPixel)
        { x = Math.Max(0, Math.Min(width - 1, x)); y = Math.Max(0, Math.Min(height - 1, y)); int offset = y * depthData.Stride + x * bytesPerPixel; return Marshal.ReadByte(depthData.Scan0, offset) / 255.0f; }


        private Vector3 ApplyLighting(Vector3 baseColor_f, Vector3 surfacePos_w, Vector3 surfaceNormal_w,
                                     List<LightSource> activeGlobalLights, List<LightSource> activeProfileLights,
                                     float effectiveDepthScale, float halfWidth, float halfHeight,
                                     float profileSpecularIntensity, float profileSpecularPower)
        { /* ... (implementation remains same as fixed previously) ... */
            Vector3 accumulatedColor_f = Vector3.Zero; Vector3 viewDir_w = new Vector3(0, 0, -1);
            Action<LightSource> processLight = (light) => { /* ... logic for Ambient, Point, Directional with Diffuse + Specular ... */
                Vector3 lightColor_f; Vector3 diffuseContrib = Vector3.Zero; Vector3 specularContrib = Vector3.Zero;
                switch (light.Type)
                {
                    case LightType.Ambient:
                        lightColor_f = ColorToFloat3(light.Color);
                        diffuseContrib = new Vector3(baseColor_f.X * lightColor_f.X, baseColor_f.Y * lightColor_f.Y, baseColor_f.Z * lightColor_f.Z) * light.Intensity; break;
                    case LightType.Point:
                    case LightType.Directional:
                        lightColor_f = ColorToFloat3(light.Color); float attenuation = 1.0f; float spotEffect = 1.0f; Vector3 lightDir_w;
                        if (light.Type == LightType.Point)
                        { /* ... Point light attenuation/spot/lightDir_w calc ... */
                            Vector3 lightPos_w = new Vector3(light.Position.X * halfWidth, light.Position.Y * halfHeight, light.Position.Z * (effectiveDepthScale * 0.5f)); Vector3 surfaceToLightVec = lightPos_w - surfacePos_w; float distSq = surfaceToLightVec.LengthSquared(); if (distSq < 1e-5f) return; float dist = (float)Math.Sqrt(distSq); lightDir_w = surfaceToLightVec / dist;
                            float denom = light.ConstantAttenuation + light.LinearAttenuation * dist + light.QuadraticAttenuation * distSq; if (denom > 1e-4f) attenuation = 1.0f / denom; else attenuation = 1.0f; attenuation = Math.Max(0.0f, Math.Min(1.0f, attenuation));
                            if (light.SpotCutoffAngle < 90.0f) { Vector3 spotAxis_w = Vector3.Normalize(light.Direction); Vector3 vectorToSurface_w = -lightDir_w; float dotSpot = Vector3.Dot(vectorToSurface_w, spotAxis_w); float cosCutoff = (float)Math.Cos(light.SpotCutoffAngle * Math.PI / 180.0); if (dotSpot <= cosCutoff) spotEffect = 0.0f; else if (light.SpotExponent > 0) spotEffect = (float)Math.Pow(dotSpot, light.SpotExponent); else spotEffect = 1.0f; }
                        }
                        else { lightDir_w = Vector3.Normalize(-light.Direction); attenuation = 1.0f; spotEffect = 1.0f; }
                        float dotNL = Math.Max(0.0f, Vector3.Dot(surfaceNormal_w, lightDir_w)); float diffuseFactor = dotNL * light.Intensity * attenuation * spotEffect;
                        diffuseContrib = new Vector3(baseColor_f.X * lightColor_f.X, baseColor_f.Y * lightColor_f.Y, baseColor_f.Z * lightColor_f.Z) * diffuseFactor;
                        if (profileSpecularIntensity > 0 && dotNL > 0) { Vector3 halfVector_w = Vector3.Normalize(lightDir_w + viewDir_w); float dotNH = Math.Max(0.0f, Vector3.Dot(surfaceNormal_w, halfVector_w)); float specFactor = (float)Math.Pow(dotNH, profileSpecularPower); float specIntensityFactor = profileSpecularIntensity * light.Intensity * specFactor * attenuation * spotEffect; specularContrib = lightColor_f * specIntensityFactor; }
                        break;
                    case LightType.Tint: break; // Handled later
                }
                accumulatedColor_f += diffuseContrib + specularContrib;
            };
            if (activeGlobalLights != null) { foreach (var light in activeGlobalLights) { if (light.Type != LightType.Tint) processLight(light); } }
            if (activeProfileLights != null) { foreach (var light in activeProfileLights) { if (light.Type != LightType.Tint) processLight(light); } }
            Action<LightSource> processTint = (light) => { if (light.Type == LightType.Tint) { Vector3 tintColor_f = ColorToFloat3(light.Color); float tintIntensity = Math.Max(0.0f, Math.Min(1.0f, light.Intensity)); accumulatedColor_f = Lerp(accumulatedColor_f, tintColor_f, tintIntensity); } };
            if (activeGlobalLights != null) { foreach (var light in activeGlobalLights) { processTint(light); } }
            if (activeProfileLights != null) { foreach (var light in activeProfileLights) { processTint(light); } }
            return accumulatedColor_f;
        }

        private static Vector3 Lerp(Vector3 a, Vector3 b, float t)
        { /* ... (implementation remains same) ... */ t = Math.Max(0.0f, Math.Min(1.0f, t)); return a * (1.0f - t) + b * t; }
        private Vector3 ColorToFloat3(Color c) => new Vector3(c.R / 255.0f, c.G / 255.0f, c.B / 255.0f);
        private Color Float3ToColor(Vector3 v) => Color.FromArgb(255, (int)Math.Max(0, Math.Min(255, v.X * 255.0f)), (int)Math.Max(0, Math.Min(255, v.Y * 255.0f)), (int)Math.Max(0, Math.Min(255, v.Z * 255.0f)));

        // --- Update Layered Window ---

        /// <summary>
        /// Updates the layered window using the static cached rendered avatar.
        /// </summary>
        private void UpdateFormDisplay()
        {
            if (!this.IsHandleCreated || this.IsDisposed)
            {
                Debug.WriteLine($"UpdateFormDisplay skipped: HandleCreated={this.IsHandleCreated}, IsDisposed={this.IsDisposed}.");
                return;
            }

            Action updateAction = () => {
                if (this.IsDisposed) return;

                Size currentSize = this.Size;
                if (currentSize.IsEmpty || currentSize.Width <= 0 || currentSize.Height <= 0)
                { Debug.WriteLine($"UpdateFormDisplay: Form size invalid ({currentSize}), cannot update display reliably."); return; }

                // --- Log cache state before drawing ---
                bool cacheExists = _cachedRenderedAvatar != null;
                Size cacheSize = _cachedRenderedAvatar?.Size ?? Size.Empty;
                Debug.WriteLine($"UpdateFormDisplay: Attempting draw. Is _cachedRenderedAvatar null? {!cacheExists}. Cache Size: {cacheSize}");
                // ---

                // Update the display using the static cache (which might be null)
                UpdateLayeredWindowWithBitmap(_cachedRenderedAvatar);
            };

            if (this.InvokeRequired) { if (!this.IsDisposed) try { this.BeginInvoke(updateAction); } catch { /* Ignore */ } }
            else { if (!this.IsDisposed) { updateAction(); } }
        }

        /// <summary>
        /// Updates the layered window display using the provided Bitmap.
        /// (Used for both static cache and animation frames).
        /// </summary>
        private void UpdateLayeredWindowWithBitmap(Bitmap frameBitmap)
        {
            // Ensure thread safety for UI updates if timer ticks on different thread
            if (this.InvokeRequired)
            {
                // Check if form handle still exists before invoking
                if (this.IsHandleCreated && !this.IsDisposed)
                {
                    try { this.BeginInvoke(new Action(() => UpdateLayeredWindowWithBitmapInternal(frameBitmap))); }
                    catch (Exception ex) { Debug.WriteLine($"BeginInvoke failed in UpdateLayeredWindowWithBitmap: {ex.Message}"); } // Catch potential ObjectDisposedException
                }
                return;
            }

            // Already on UI thread
            UpdateLayeredWindowWithBitmapInternal(frameBitmap);
        }

        /// <summary>
        /// Internal method containing the actual layered window update logic. Must run on UI thread.
        /// </summary>
        private void UpdateLayeredWindowWithBitmapInternal(Bitmap frameBitmap)
        {
            // --- Initial Checks ---
            if (!this.IsHandleCreated || this.IsDisposed)
            { Debug.WriteLine("UpdateLayeredWindowWithBitmapInternal skipped: Invalid form state."); return; }

            // Use current form dimensions
            int formWidth = this.Width;
            int formHeight = this.Height;

            // Ensure form dimensions are valid
            if (formWidth <= 0 || formHeight <= 0)
            { Debug.WriteLine($"UpdateLayeredWindowWithBitmapInternal skipped: Invalid form dimensions W:{formWidth} H:{formHeight}."); return; }

            Bitmap canvasBitmap = null; // Bitmap matching form size
            IntPtr hBitmap = IntPtr.Zero; // HBitmap for UpdateLayeredWindow

            try
            {
                // Create the canvas matching the current form size
                canvasBitmap = new Bitmap(formWidth, formHeight, PixelFormat.Format32bppArgb);

                using (Graphics g = Graphics.FromImage(canvasBitmap))
                {
                    // Start with a fully transparent canvas
                    g.Clear(Color.Transparent);

                    // If a valid frameBitmap is provided, draw it centered
                    if (frameBitmap != null && frameBitmap.Width > 0 && frameBitmap.Height > 0)
                    {
                        int frameWidth = frameBitmap.Width;
                        int frameHeight = frameBitmap.Height;

                        // Calculate top-left corner for centered drawing
                        Point drawOffset = new Point(
                            Math.Max(0, (formWidth - frameWidth) / 2),
                            Math.Max(0, (formHeight - frameHeight) / 2)
                        );

                        // Draw the frame onto the canvas
                        // Use DrawImageUnscaled for performance and direct pixel mapping
                        g.DrawImageUnscaled(frameBitmap, drawOffset.X, drawOffset.Y);
                    }
                    else
                    {
                        // If frameBitmap is null or invalid, the canvas remains transparent (clears display)
                        Debug.WriteLineIf(frameBitmap != null, "UpdateLayeredWindowWithBitmapInternal: Invalid frameBitmap dimensions, drawing transparent.");
                    }
                } // Graphics object disposed here

                // Get Hbitmap from the prepared canvas
                hBitmap = canvasBitmap.GetHbitmap(Color.FromArgb(0)); // Use 0 for alpha background
                if (hBitmap == IntPtr.Zero)
                {
                    throw new Exception("Failed to get Hbitmap from canvas bitmap.");
                }

                // --- Call the P/Invoke helper ---
                // This helper handles DC creation/release and HBitmap deletion
                UpdateWithHBitmap(hBitmap);
                // hBitmap is now owned and disposed by UpdateWithHBitmap

            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Error in UpdateLayeredWindowWithBitmapInternal: {ex.Message}");
                // Optionally try to clear display on error?
                // if (hBitmap != IntPtr.Zero) { NativeMethods.DeleteObject(hBitmap); hBitmap = IntPtr.Zero; } // Clean up potential handle
                // UpdateLayeredWindowWithBitmapInternal(null); // Recursive call might be risky
            }
            finally
            {
                // Dispose the managed canvas bitmap
                canvasBitmap?.Dispose();
                // Note: hBitmap is now disposed within UpdateWithHBitmap's finally block
            }
        }

        /// <summary>
        /// Performs the actual UpdateLayeredWindow call with the provided HBitmap handle.
        /// This method handles the DC creation/selection/release and ensures the passed HBitmap is deleted.
        /// </summary>
        /// <param name="hBitmapToUse">The HBitmap handle to display. This method takes ownership and will delete it.</param>
        private void UpdateWithHBitmap(IntPtr hBitmapToUse)
        {
            // Validate state before proceeding
            if (!this.IsHandleCreated || this.IsDisposed)
            {
                Debug.WriteLine("UpdateWithHBitmap skipped: Form handle not created or form disposed.");
                if (hBitmapToUse != IntPtr.Zero) NativeMethods.DeleteObject(hBitmapToUse); // Clean up if form invalid
                return;
            }
            if (hBitmapToUse == IntPtr.Zero)
            {
                Debug.WriteLine("UpdateWithHBitmap skipped: hBitmapToUse is IntPtr.Zero.");
                return; // Cannot update with null bitmap handle
            }


            IntPtr screenDc = IntPtr.Zero;
            IntPtr memDc = IntPtr.Zero;
            IntPtr oldBitmap = IntPtr.Zero;

            try
            {
                screenDc = NativeMethods.GetDC(IntPtr.Zero); // DC for entire screen
                if (screenDc == IntPtr.Zero) throw new Win32Exception(Marshal.GetLastWin32Error(), "GetDC(IntPtr.Zero) failed.");

                memDc = NativeMethods.CreateCompatibleDC(screenDc); // Memory DC compatible with screen
                if (memDc == IntPtr.Zero) throw new Win32Exception(Marshal.GetLastWin32Error(), "CreateCompatibleDC failed.");

                // Select the bitmap into the memory DC
                // IMPORTANT: Do this *before* deleting the passed hBitmapToUse in the finally block
                oldBitmap = NativeMethods.SelectObject(memDc, hBitmapToUse);
                if (oldBitmap == IntPtr.Zero)
                {
                    // If SelectObject fails, the hBitmapToUse was likely invalid.
                    // We should still try to clean up DCs but throw to indicate failure.
                    throw new Win32Exception(Marshal.GetLastWin32Error(), "SelectObject failed. hBitmap might be invalid.");
                }


                // Define size and position for the window update using current form state
                NativeMethods.Size newSize = new NativeMethods.Size(this.Width, this.Height);
                NativeMethods.Point sourceLocation = new NativeMethods.Point(0, 0); // Top-left of the source bitmap (canvas)
                NativeMethods.Point screenLocation = new NativeMethods.Point(this.Left, this.Top); // Current window position

                // Define blend function for alpha blending
                NativeMethods.BLENDFUNCTION blend = new NativeMethods.BLENDFUNCTION
                {
                    BlendOp = NativeMethods.AC_SRC_OVER,
                    BlendFlags = 0,
                    SourceConstantAlpha = 255, // Use per-pixel alpha
                    AlphaFormat = NativeMethods.AC_SRC_ALPHA
                };

                // --- Update the layered window ---
                bool success = NativeMethods.UpdateLayeredWindow(
                    this.Handle, screenDc, ref screenLocation, ref newSize,
                    memDc, ref sourceLocation, 0, ref blend, NativeMethods.ULW_ALPHA
                );

                if (!success)
                {
                    // Log error but don't necessarily throw, allow cleanup to proceed
                    Debug.WriteLine($"UpdateLayeredWindow failed with error code: {Marshal.GetLastWin32Error()}");
                }
            }
            catch (Win32Exception winEx)
            {
                Debug.WriteLine($"Win32Exception in UpdateWithHBitmap: {winEx.Message} (ErrorCode: {winEx.NativeErrorCode})");
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Exception in UpdateWithHBitmap: {ex.Message}");
            }
            finally
            {
                // --- CRITICAL Cleanup Sequence ---
                // 1. Release screen DC
                if (screenDc != IntPtr.Zero) NativeMethods.ReleaseDC(IntPtr.Zero, screenDc);
                // 2. If an old bitmap was selected into the memDC, select it back
                if (oldBitmap != IntPtr.Zero && memDc != IntPtr.Zero)
                {
                    NativeMethods.SelectObject(memDc, oldBitmap);
                    // After selecting back, oldBitmap is no longer needed.
                    // We don't delete oldBitmap as it's typically a system default bitmap.
                }
                // 3. Delete the HBitmap handle that was passed into this function
                if (hBitmapToUse != IntPtr.Zero)
                {
                    NativeMethods.DeleteObject(hBitmapToUse);
                }
                // 4. Delete the memory DC
                if (memDc != IntPtr.Zero) NativeMethods.DeleteDC(memDc);
            }
        }

        // --- Dragging Logic ---
        private void PreviewForm_MouseDown(object sender, MouseEventArgs e) { if (e.Button == MouseButtons.Left) { _isDragging = true; _dragOffset = e.Location; this.Cursor = Cursors.SizeAll; } }
        private void PreviewForm_MouseMove(object sender, MouseEventArgs e) { if (_isDragging) { Point spt = this.PointToScreen(e.Location); this.Location = new Point(spt.X - _dragOffset.X, spt.Y - _dragOffset.Y); } }
        private void PreviewForm_MouseUp(object sender, MouseEventArgs e) { if (e.Button == MouseButtons.Left) { _isDragging = false; this.Cursor = Cursors.Default; SavePosition(this.Location); } }

        // --- Positioning Logic ---
        private Point CalculateCenterScreenPosition(Size windowSize)
        { /* ... (implementation remains same) ... */
            Screen screen = Screen.FromHandle(this.Handle); if (screen == null) screen = Screen.PrimaryScreen ?? Screen.AllScreens.FirstOrDefault(); if (screen == null) return new Point(100, 100);
            Rectangle area = screen.WorkingArea; int left = area.Left + (area.Width - windowSize.Width) / 2; int top = area.Top + (area.Height - windowSize.Height) / 2;
            left = Math.Max(area.Left, Math.Min(left, area.Right - windowSize.Width)); top = Math.Max(area.Top, Math.Min(top, area.Bottom - windowSize.Height)); return new Point(left, top);
        }
        private void SavePosition(Point location) { PositionChanged?.Invoke(this, location); _initialPosition = location; }

        /// <summary>
        /// Starts playing the provided sequence of pre-rendered lit frames.
        /// </summary>
        /// <param name="frames">The list of Bitmaps to display (MUST NOT be disposed by caller while playing).</param>
        /// <param name="loop">Whether the animation should loop.</param>
        public void StartPlayback(List<Bitmap> frames, bool loop)
        {
            if (frames == null || frames.Count == 0)
            { Debug.WriteLine("StartPlayback called with no frames."); StopPlayback(true); return; }

            // Use the existing form size, which should have been set by ControlPanelForm
            Debug.WriteLine($"StartPlayback called with {frames.Count} frames. Loop: {loop}. Using existing Form Size: {this.Size}");
            StopPlayback(false); // Stop previous without resetting display/size

            _playbackFrames = frames;
            _loopPlayback = loop;
            _currentFrameIndex = -1;

            // Action to start timer (runs on UI thread)
            Action startTimerAction = () => {
                if (this.IsDisposed) return;

                // Store original size if this is the first playback (might still be useful for manual revert?)
                // Or maybe not needed now size is fixed per profile? Let's remove it for now.
                // if (_originalStaticSize.IsEmpty && _cachedRenderedAvatar != null) { /*...*/ }

                // Initialize and start Timer
                InitializeTimer();
                if (_animationTimer != null)
                {
                    _animationTimer.Interval = DEFAULT_FRAME_INTERVAL_MS;
                    _animationTimer.Start();
                    Debug.WriteLine("Animation Timer Started.");
                    // Trigger first frame draw immediately
                    AnimationTimer_Tick(null, EventArgs.Empty);
                }
                else { Debug.WriteLine("Error: Animation Timer is null after InitializeTimer call."); }
            };

            // Ensure the action runs on the UI thread
            if (this.InvokeRequired) { if (!this.IsDisposed) try { this.BeginInvoke(startTimerAction); } catch {/*Ignore*/} }
            else { startTimerAction(); }
        }

        // --- MODIFIED: StopPlayback Method ---
        // In PreviewForm.cs
        // --- REMOVED resizing logic ---
        public void StopPlayback(bool resetToStatic = true)
        {
            // Stop the timer first (same logic as before)
            if (_animationTimer != null)
            {
                Action stopTimerAction = () => { if (_animationTimer != null && _animationTimer.Enabled) { _animationTimer.Stop(); Debug.WriteLine("Animation Playback Timer Stopped."); } };
                if (this.InvokeRequired) { if (!this.IsDisposed) try { this.BeginInvoke(stopTimerAction); } catch { /* Ignore */ } } else { stopTimerAction(); }
            }

            _playbackFrames = null;
            _currentFrameIndex = 0;

            if (resetToStatic)
            {
                Debug.WriteLine("Resetting display to static avatar after playback stop.");
                // Action to update display (needs UI thread)
                Action resetDisplayAction = () =>
                {
                    if (this.IsDisposed) return;
                    // --- REMOVED resizing back logic ---
                    // The form size remains fixed for the profile.
                    // Just update the display with the static cache.
                    UpdateFormDisplay(); // This now draws the static cache centered in the current fixed size
                                         // _originalStaticSize = Size.Empty; // No longer needed? Or keep for initial load? Let's clear it.
                };

                // Execute reset action on UI thread
                if (this.InvokeRequired) { if (!this.IsDisposed) try { this.BeginInvoke(resetDisplayAction); } catch { /* Ignore */ } }
                else { resetDisplayAction(); }
            }
        }

        /// <summary>
        /// Creates the Timer object if it doesn't exist. Must be called on UI thread.
        /// </summary>
        private void InitializeTimer()
        {
            if (_animationTimer == null)
            {
                _animationTimer = new Timer();
                _animationTimer.Tick += AnimationTimer_Tick;
            }
        }

        private void AnimationTimer_Tick(object sender, EventArgs e)
        {
            // --- ADDED: Log Tick entry ---
            // Debug.WriteLine($"Tick: CurrentIndex={_currentFrameIndex}, PlaybackFrames={_playbackFrames?.Count ?? -1}"); // Can be verbose
            // ---

            if (_playbackFrames == null || _playbackFrames.Count == 0)
            {
                // This case should ideally be prevented by StartPlayback validation, but handle defensively
                Debug.WriteLine("Tick Error: Playback frames list is null or empty. Stopping.");
                StopPlayback(true);
                return;
            }

            _currentFrameIndex++; // Move to the next frame

            if (_currentFrameIndex >= _playbackFrames.Count)
            {
                // Reached the end
                if (_loopPlayback)
                {
                    _currentFrameIndex = 0; // Loop back to the beginning
                    Debug.WriteLine("Tick: Looping back to frame 0.");
                }
                else
                {
                    Debug.WriteLine("Tick: Reached end of non-looping animation. Stopping.");
                    StopPlayback(true); // Stop and reset display
                    return;
                }
            }

            // Safety check for index validity after potential looping/stopping
            if (_currentFrameIndex < 0 || _currentFrameIndex >= _playbackFrames.Count)
            {
                Debug.WriteLine($"Tick Error: Invalid frame index ({_currentFrameIndex}) after boundary checks. Stopping.");
                StopPlayback(true);
                return;
            }

            // --- ADDED: Specific Log for Frame 0 ---
            if (_currentFrameIndex == 0)
            {
                Debug.WriteLine("Tick: Processing and displaying Frame 0.");
            }
            // ---

            try
            {
                // Get the bitmap for the current frame index
                Bitmap currentFrameBitmap = _playbackFrames[_currentFrameIndex];

                // Check if the bitmap itself is valid before attempting to display it
                if (currentFrameBitmap == null || currentFrameBitmap.Width <= 0 || currentFrameBitmap.Height <= 0)
                {
                    Debug.WriteLine($"Tick Error: Bitmap at index {_currentFrameIndex} is null or invalid. Stopping playback.");
                    StopPlayback(true);
                    return;
                }

                // Display the current frame
                UpdateLayeredWindowWithBitmap(currentFrameBitmap); // This method handles InvokeRequired if needed
            }
            catch (ArgumentOutOfRangeException ex) // Catch if index becomes invalid between checks (less likely now)
            {
                Debug.WriteLine($"Tick Error: Frame index out of range ({_currentFrameIndex}). Stopping playback. {ex.Message}");
                StopPlayback(true);
            }
            catch (Exception ex) // Catch other potential errors (e.g., accessing disposed bitmap if cache logic flawed)
            {
                Debug.WriteLine($"Tick Error: Unexpected exception processing frame {_currentFrameIndex}. Stopping playback. {ex.Message}\n{ex.StackTrace}");
                StopPlayback(true);
            }
        }

        /// <summary>
        /// Updates the list of frames being played back. Needs thread safety.
        /// </summary>
        public void UpdatePlaybackFrames(List<Bitmap> newFrames)
        {
            Debug.WriteLine($"PreviewForm received UpdatePlaybackFrames request ({newFrames?.Count ?? 0} frames).");
            // This needs to be thread-safe with the timer tick accessing _playbackFrames.
            // Option 1: Stop timer, swap, restart (simplest, might cause tiny visual glitch)
            // Option 2: Use lock (might cause timer tick to wait)
            // Option 3: Use Concurrent collection (more complex)

            // Let's try Option 1 (Stop/Start Timer) - Ensure executed on UI thread
            Action updateAction = () => {
                bool timerWasEnabled = _animationTimer?.Enabled ?? false;
                if (timerWasEnabled) _animationTimer.Stop();

                // Note: We are getting a reference to the list from the cache.
                // ControlPanelForm is responsible for disposing the *old* list it replaced.
                _playbackFrames = newFrames;

                // Reset frame index? Or try to maintain position? Reset is safer.
                _currentFrameIndex = 0;

                // Restart timer only if it was running before
                if (timerWasEnabled && _playbackFrames != null && _playbackFrames.Count > 0)
                {
                    _animationTimer.Start();
                    // Immediately display the new frame 0
                    if (_currentFrameIndex < _playbackFrames.Count)
                    {
                        UpdateLayeredWindowWithBitmap(_playbackFrames[_currentFrameIndex]);
                    }
                }
                else if (!timerWasEnabled && _playbackFrames == null)
                {
                    // If timer was stopped and new frames are null, ensure display is reset
                    UpdateFormDisplay(); // Show static cache
                }
            };

            if (this.InvokeRequired)
            {
                this.BeginInvoke(updateAction);
            }
            else
            {
                updateAction();
            }
        }

        // --- Resource Cleanup ---
        private void DisposeBitmaps(bool disposeStaticCache = true)
        {
            if (disposeStaticCache)
            {
                _cachedRenderedAvatar?.Dispose();
                _cachedRenderedAvatar = null;
            }
            // Now correctly disposes the member fields
            _baseAvatarBitmap?.Dispose();
            _baseAvatarBitmap = null;
            _depthMapBitmap?.Dispose();
            _depthMapBitmap = null;
            // _playbackFrames reference is cleared in StopPlayback, disposal is handled by ControlPanelForm cache
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                StopPlayback(false); // Stop timer
                _animationTimer?.Dispose(); // Dispose timer
                _animationTimer = null;
                DisposeBitmaps(disposeStaticCache: true); // Dispose loaded images and static cache
                components?.Dispose();
            }
            base.Dispose(disposing);
        }

    } // End class
} // End namespace