// >>> START NEW FILE: Data/TransformedFrameGeometry.cs
using System;
using System.Drawing; // For Bitmap
using System.Drawing.Imaging;

namespace Avatar_Elements.Data {
    /// <summary>
    /// Holds the pre-rendered geometric data (depth, normals) for a single
    /// transformed frame of an animation. Helps separate expensive geometry
    /// calculations from dynamic lighting application.
    /// </summary>
    public class TransformedFrameGeometry : IDisposable {
        /// <summary>
        /// The depth map pixels after applying the animation transform for this frame.
        /// Stored typically as a grayscale bitmap where intensity represents depth.
        /// </summary>
        public Bitmap TransformedDepthMap { get; set; }

        /// <summary>
        /// The calculated world-space normal vectors (encoded into RGB: X->R, Y->G, Z->B, scaled 0-255)
        /// after applying the animation transform for this frame.
        /// </summary>
        public Bitmap TransformedNormalMap { get; set; }

        private bool disposedValue;

        // Constructor could potentially take dimensions
        public TransformedFrameGeometry(int width, int height)
        {
            // Initialize bitmaps (consider PixelFormat)
            // Using Format24bppRgb for normals as we need 3 channels. Alpha isn't needed.
            // Using Format8bppIndexed for depth assumes we want grayscale. Needs palette setup.
            // Simpler might be to just use Format24bppRgb for depth too and only use one channel.
            // Let's use Format24bppRgb for both for simplicity for now.
            TransformedDepthMap = new Bitmap(width, height, PixelFormat.Format24bppRgb);
            TransformedNormalMap = new Bitmap(width, height, PixelFormat.Format24bppRgb);
        }

        // Parameterless constructor for potential future use or serialization (if needed)
        public TransformedFrameGeometry() { }


        // --- IDisposable Implementation ---
        protected virtual void Dispose(bool disposing)
        {
            if (!disposedValue)
            {
                if (disposing)
                {
                    // Dispose managed state (managed objects).
                    TransformedDepthMap?.Dispose();
                    TransformedNormalMap?.Dispose();
                    TransformedDepthMap = null;
                    TransformedNormalMap = null;
                }
                // No unmanaged resources to free directly here
                disposedValue = true;
            }
        }

        // Public Dispose method
        public void Dispose()
        {
            Dispose(disposing: true);
            GC.SuppressFinalize(this);
        }

        // Finalizer (just in case Dispose isn't called)
        ~TransformedFrameGeometry()
        {
            Dispose(disposing: false);
        }
    }
}
// <<< END NEW FILE: Data/TransformedFrameGeometry.cs