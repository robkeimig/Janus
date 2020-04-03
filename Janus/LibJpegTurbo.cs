using System;
using System.Collections.Concurrent;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;

namespace Janus
{
    public class LibJpegTurbo
    {
        public const string LibraryName = "turbojpeg";
        private readonly IntPtr _compressorHandle;

        [DllImport(LibraryName, CallingConvention = CallingConvention.Cdecl)]
        public static extern int tjCompress2(IntPtr handle, IntPtr srcBuf, int width, int pitch, int height, int pixelFormat, ref IntPtr jpegBuf, ref ulong jpegSize, int jpegSubsamp, int jpegQual, int flags);
        
        static LibJpegTurbo()
        {
            //Load DLLs.
        }

        public LibJpegTurbo()
        {
            _compressorHandle = tjInitCompress();

            if (_compressorHandle == IntPtr.Zero)
            {
                GetErrorAndThrow();
            }
        }

        internal static class NativeModulesLoader
        {
            private static string _nativePath;

            private static readonly ConcurrentDictionary<string, IntPtr> LoadedLibraries = new ConcurrentDictionary<string, IntPtr>();

            /// <summary>
            /// Sets path to search native libraries
            /// </summary>
            /// <param name="path">Path to search native libraries</param>
            public static void SetNativePath(string path = null)
            {
                _nativePath = path;
            }

            public static string NativePath => _nativePath;

            /// <summary>
            /// Set native libraries Directory
            /// </summary>
            /// <exception cref="DllNotFoundException">Not found Sdk folder</exception>
            public static void LoadLibraries(string unmanagedModule, Action<string> logger = null)
            {
                if (!LoadUnmanagedModules(new[] { unmanagedModule }, logger))
                {
                    throw new Exception($"Unable to load library {unmanagedModule}");
                }
            }

            /// <summary>
            /// Set native libraries Directory
            /// </summary>
            /// <exception cref="DllNotFoundException">Not found Sdk folder</exception>
            public static void LoadLibraries(string[] unmanagedModules, Action<string> logger = null)
            {
                if (!LoadUnmanagedModules(unmanagedModules, logger))
                {
                    throw new Exception($"Unable to load libraries {unmanagedModules.Aggregate("", (current, value) => current + value + "; ")}");
                }
            }
            /// <summary>
            /// Releases specified unmanaged modules
            /// </summary>
            public static void FreeUnmanagedModules()
            {
                while (!LoadedLibraries.IsEmpty)
                {
                    var first = LoadedLibraries.First().Key;
                    if (!LoadedLibraries.TryRemove(first, out var ptr))
                        continue;

                    if (Platform.OperationSystem != OS.Windows)
                    {
                        Dlclose(ptr);
                    }
                    else
                    {
                        FreeLibrary(ptr);
                    }
                }
            }
            /// <summary>
            /// Releases specified unmanaged modules
            /// </summary>
            public static void FreeUnmanagedModules(params string[] unmanagedModules)
            {
                foreach (var name in unmanagedModules)
                {
                    if (!LoadedLibraries.TryGetValue(name, out var ptr))
                        continue;

                    if (Platform.OperationSystem != OS.Windows)
                    {
                        Dlclose(ptr);
                    }
                    else
                    {
                        FreeLibrary(ptr);
                    }
                }
            }

            /// <summary>
            /// Attempts to load native sdk modules from the specific location
            /// </summary>
            /// <param name="unmanagedModules">The names of sdk modules. e.g. "fis_face_detector.dll" on windows.</param>
            /// <param name="logger">logger func</param>
            /// <returns>True if all the modules has been loaded successfully</returns>
            private static bool LoadUnmanagedModules(string[] unmanagedModules, Action<string> logger)
            {
                if (!string.IsNullOrEmpty(NativePath) && Directory.Exists(NativePath))
                {
                    return LoadUnmanagedModules(NativePath, unmanagedModules, logger);
                }

                var location = AppContext.BaseDirectory;

                if (!Directory.Exists(location))
                    return false;

                var osSubfolder = GetOSSubfolder(Platform.OperationSystem);
                var platform = Platform.GetPlatformName();

                var subFolderSearchPattern = $"{osSubfolder}-{platform}*";

                var dirs = Directory.GetDirectories(location, subFolderSearchPattern);

                if (dirs.Length == 0)
                {
                    logger?.Invoke("No suitable directory found to load unmanaged modules");
                    return false;
                }

                foreach (var dir in dirs)
                {
                    logger?.Invoke($"Attempt to load unmanaged modules from {dir}");
                    var result = LoadUnmanagedModules(dir, unmanagedModules, logger);
                    if (result)
                    {
                        return true;
                    }
                }
                return false;
            }

            private static bool LoadUnmanagedModules(string dir, string[] unmanagedModules, Action<string> logger)
            {
                if (!Directory.Exists(dir))
                {
                    logger?.Invoke("No suitable directory found to load unmanaged modules");
                    return false;
                }

                var oldDir = Directory.GetCurrentDirectory();

                dir = Path.GetFullPath(dir);
                Directory.SetCurrentDirectory(dir);

                logger?.Invoke($"Loading unmanaged libraries from {dir}");
                var success = true;

                foreach (var module in unmanagedModules)
                {
                    //Use absolute path for Windows Desktop
                    var fullPath = Path.Combine(dir, module);

                    var fileExist = File.Exists(fullPath);
                    if (!fileExist)
                        logger?.Invoke($"File {fullPath} do not exist.");

                    var libraryPtr = LoadLibrary(fullPath, logger);

                    var fileExistAndLoaded = fileExist && IntPtr.Zero != libraryPtr;
                    if (fileExist && !fileExistAndLoaded)
                        logger?.Invoke($"File {fullPath} cannot be loaded.");
                    else
                    {
                        logger?.Invoke($"Library {fullPath} loaded successfully");
                        LoadedLibraries.TryAdd(module, libraryPtr);
                    }
                    success &= fileExistAndLoaded;
                }
                Directory.SetCurrentDirectory(oldDir);
                return success;
            }


            // ReSharper disable once InconsistentNaming
            private static string GetOSSubfolder(OS operationSystem)
            {
                switch (operationSystem)
                {
                    case OS.Windows:
                        return "win";
                    case OS.Linux:
                        return "linux";
                    case OS.MacOS:
                        return "mac";
                    case OS.Android:
                        return "android";
                    case OS.IOS:
                        return "ios";
                    case OS.WindowsPhone:
                        return "wp";
                    default:
                        throw new ArgumentOutOfRangeException(nameof(operationSystem));
                }
            }

            /// <summary>
            /// Maps the specified executable module into the address space of the calling process.
            /// </summary>
            /// <param name="dllname">The name of the dll</param>
            /// <param name="logger"></param>
            /// <returns>The handle to the library</returns>
            private static IntPtr LoadLibrary(string dllname, Action<string> logger)
            {
                if (Platform.OperationSystem != OS.Windows)
                    return Dlopen(dllname, 2); // 2 == RTLD_NOW


                const int loadLibrarySearchDllLoadDir = 0x00000100;
                const int loadLibrarySearchDefaultDirs = 0x00001000;
                var handler = LoadLibraryEx(dllname, IntPtr.Zero, loadLibrarySearchDllLoadDir | loadLibrarySearchDefaultDirs);
                if (handler != IntPtr.Zero)
                    return handler;

                var error = Marshal.GetLastWin32Error();

                var ex = new System.ComponentModel.Win32Exception(error);
                logger?.Invoke($"LoadLibraryEx {dllname} failed with error code {(uint)error}: {ex.Message}");
                return handler;
            }

            [DllImport("Kernel32.dll", SetLastError = true)]
            private static extern IntPtr LoadLibraryEx([MarshalAs(UnmanagedType.LPStr)]string fileName, IntPtr hFile, int dwFlags);

            /// <summary>
            /// Decrements the reference count of the loaded dynamic-link library (DLL). When the reference count reaches zero, the module is unmapped from the address space of the calling process and the handle is no longer valid
            /// </summary>
            /// <param name="handle">The handle to the library</param>
            /// <returns>If the function succeeds, the return value is true. If the function fails, the return value is false.</returns>
            [DllImport("kernel32.dll", SetLastError = true)]
            [return: MarshalAs(UnmanagedType.Bool)]
            private static extern bool FreeLibrary(IntPtr handle);


            [DllImport("dl", EntryPoint = "dlopen")]
            private static extern IntPtr Dlopen([MarshalAs(UnmanagedType.LPStr)]string dllname, int mode);

            [DllImport("dl", EntryPoint = "dlclose")]
            private static extern int Dlclose(IntPtr handle);
        }


        /// <summary>
        /// Provide information for the platform which is using. 
        /// </summary>
        internal static class Platform
        {
            static Platform()
            {
                if (RuntimeInformation.IsOSPlatform(OSPlatform.OSX))
                {
                    OperationSystem = OS.MacOS;
                }
                else if (RuntimeInformation.IsOSPlatform(OSPlatform.Linux))
                {
                    OperationSystem = OS.Linux;
                }
                else
                {
                    OperationSystem = OS.Windows;
                }
            }

            /// <summary>
            /// Get the type of the current operating system
            /// </summary>
            public static OS OperationSystem { get; }


            /// <summary>
            /// Returns name of executing platform
            /// </summary>
            /// <returns></returns>
            // ReSharper disable once MemberCanBePrivate.Global
            public static string GetPlatformName()
            {
                switch (IntPtr.Size)
                {
                    case 4:
                        return "x86";
                    case 8:
                        return "x64";
                    default:
                        return "Unknown";
                }
            }
        }

        /// <summary>Type of operating system</summary>
        internal enum OS
        {
            Windows,
            Linux,
            MacOS,
            IOS,
            Android,
            WindowsPhone,
        }

        ///<summary>
        /// Retrieves last error from underlying turbo-jpeg library and throws exception</summary>
        /// <exception cref="TJException"> Throws if low level turbo jpeg function fails </exception>
        public static void GetErrorAndThrow()
        {
            var error = tjGetErrorStr();
            throw new TJException(error);
        }

        public class TJException : Exception
        {
            /// <summary>
            /// Creates new instance of <see cref="TJException"/>
            /// </summary>
            /// <param name="error">Error message from underlying turbo jpeg library</param>
            internal TJException(string error) : base(error)
            {
            }
        }


        /// <summary>
        /// Returns a descriptive error message explaining why the last command failed
        /// </summary>
        /// <returns>A descriptive error message explaining why the last command failed</returns>
        [DllImport(LibraryName, CallingConvention = CallingConvention.Cdecl, CharSet = CharSet.Ansi)]
        [return: MarshalAs(UnmanagedType.LPStr)]
        public static extern string tjGetErrorStr();

        /// <summary>
        /// Create a TurboJPEG compressor instance.
        /// </summary>
        /// <returns>
        /// handle to the newly-created instance, or <see cref="IntPtr.Zero"/> 
        /// if an error occurred (see <see cref="tjGetErrorStr"/>)</returns>
        [DllImport(LibraryName, CallingConvention = CallingConvention.Cdecl)]

        public static extern IntPtr tjInitCompress();
        public enum TJPixelFormats
        {
            /// <summary>
            /// RGB pixel format.  The red, green, and blue components in the image are
            /// stored in 3-byte pixels in the order R, G, B from lowest to highest byte
            /// address within each pixel.
            /// </summary>
            TJPF_RGB = 0,
            /// <summary>
            /// BGR pixel format.  The red, green, and blue components in the image are
            /// stored in 3-byte pixels in the order B, G, R from lowest to highest byte
            /// address within each pixel.
            /// </summary>
            TJPF_BGR,
            /// <summary>
            /// RGBX pixel format.  The red, green, and blue components in the image are
            /// stored in 4-byte pixels in the order R, G, B from lowest to highest byte
            /// address within each pixel.  The X component is ignored when compressing
            /// and undefined when decompressing. 
            /// </summary>
            TJPF_RGBX,
            /// <summary>
            /// BGRX pixel format.  The red, green, and blue components in the image are
            /// stored in 4-byte pixels in the order B, G, R from lowest to highest byte
            /// address within each pixel.  The X component is ignored when compressing
            /// and undefined when decompressing.
            ///  </summary>
            TJPF_BGRX,
            /// <summary>
            /// XBGR pixel format.  The red, green, and blue components in the image are
            /// stored in 4-byte pixels in the order R, G, B from highest to lowest byte
            /// address within each pixel.  The X component is ignored when compressing
            /// and undefined when decompressing. 
            /// </summary>
            TJPF_XBGR,
            /// <summary>
            /// XRGB pixel format.  The red, green, and blue components in the image are
            /// stored in 4-byte pixels in the order B, G, R from highest to lowest byte
            /// address within each pixel.  The X component is ignored when compressing
            /// and undefined when decompressing.
            /// </summary>
            TJPF_XRGB,
            /// <summary>
            /// Grayscale pixel format.  Each 1-byte pixel represents a luminance
            /// (brightness) level from 0 to 255.
            /// </summary>
            TJPF_GRAY,
            /// <summary>
            /// RGBA pixel format.  This is the same as <see cref="TJPF_RGBX"/>, except that when
            /// decompressing, the X component is guaranteed to be 0xFF, which can be
            /// interpreted as an opaque alpha channel.
            /// </summary>
            TJPF_RGBA,
            /// <summary>
            /// BGRA pixel format.  This is the same as <see cref="TJPF_BGRX"/>, except that when
            /// decompressing, the X component is guaranteed to be 0xFF, which can be
            /// interpreted as an opaque alpha channel.
            /// </summary>
            TJPF_BGRA,
            /// <summary>
            /// ABGR pixel format.  This is the same as <see cref="TJPF_XBGR"/>, except that when
            /// decompressing, the X component is guaranteed to be 0xFF, which can be
            /// interpreted as an opaque alpha channel.
            /// </summary>
            TJPF_ABGR,
            /// <summary>
            /// ARGB pixel format.  This is the same as <see cref="TJPF_XRGB"/>, except that when
            /// decompressing, the X component is guaranteed to be 0xFF, which can be
            /// interpreted as an opaque alpha channel.
            /// </summary>
            TJPF_ARGB,
            /// <summary>
            /// CMYK pixel format.  Unlike RGB, which is an additive color model used
            /// primarily for display, CMYK (Cyan/Magenta/Yellow/Key) is a subtractive
            /// color model used primarily for printing.  In the CMYK color model, the
            /// value of each color component typically corresponds to an amount of cyan,
            /// magenta, yellow, or black ink that is applied to a white background.  In
            /// order to convert between CMYK and RGB, it is necessary to use a color
            /// management system (CMS.)  A CMS will attempt to map colors within the
            /// printer's gamut to perceptually similar colors in the display's gamut and
            /// vice versa, but the mapping is typically not 1:1 or reversible, nor can it
            /// be defined with a simple formula.  Thus, such a conversion is out of scope
            /// for a codec library.  However, the TurboJPEG API allows for compressing
            /// CMYK pixels into a YCCK JPEG image (see #TJCS_YCCK) and decompressing YCCK
            /// JPEG images into CMYK pixels. 
            /// </summary>
            TJPF_CMYK
        };

        /// <summary>
        /// Flags for turbo jpeg
        /// </summary>
        [Flags]
        public enum TJFlags
        {
            /// <summary>
            /// Flags not set
            /// </summary>
            NONE = 0,
            /// <summary>
            /// The uncompressed source/destination image is stored in bottom-up (Windows, OpenGL) order, 
            /// not top-down (X11) order.
            /// </summary>
            BOTTOMUP = 2,
            /// <summary>
            /// When decompressing an image that was compressed using chrominance subsampling, 
            /// use the fastest chrominance upsampling algorithm available in the underlying codec.  
            /// The default is to use smooth upsampling, which creates a smooth transition between 
            /// neighboring chrominance components in order to reduce upsampling artifacts in the decompressed image.
            /// </summary>
            FASTUPSAMPLE = 256,
            /// <summary>
            /// Disable buffer (re)allocation.  If passed to <see cref="TurboJpegImport.tjCompress2"/> or #tjTransform(), 
            /// this flag will cause those functions to generate an error 
            /// if the JPEG image buffer is invalid or too small rather than attempting to allocate or reallocate that buffer.  
            /// This reproduces the behavior of earlier versions of TurboJPEG.
            /// </summary>
            NOREALLOC = 1024,
            /// <summary>
            /// Use the fastest DCT/IDCT algorithm available in the underlying codec.  The
            /// default if this flag is not specified is implementation-specific.  For
            /// example, the implementation of TurboJPEG for libjpeg[-turbo] uses the fast
            /// algorithm by default when compressing, because this has been shown to have
            /// only a very slight effect on accuracy, but it uses the accurate algorithm
            /// when decompressing, because this has been shown to have a larger effect. 
            /// </summary>
            FASTDCT = 2048,
            /// <summary>
            /// Use the most accurate DCT/IDCT algorithm available in the underlying codec.
            /// The default if this flag is not specified is implementation-specific.  For
            /// example, the implementation of TurboJPEG for libjpeg[-turbo] uses the fast
            /// algorithm by default when compressing, because this has been shown to have
            /// only a very slight effect on accuracy, but it uses the accurate algorithm
            /// when decompressing, because this has been shown to have a larger effect.
            /// </summary>
            ACCURATEDCT = 4096
        }

        /// <summary>
        /// Chrominance subsampling options.
        /// <para>
        /// When pixels are converted from RGB to YCbCr (see #TJCS_YCbCr) or from CMYK
        /// to YCCK (see #TJCS_YCCK) as part of the JPEG compression process, some of
        /// the Cb and Cr (chrominance) components can be discarded or averaged together
        /// to produce a smaller image with little perceptible loss of image clarity
        /// (the human eye is more sensitive to small changes in brightness than to
        /// small changes in color.)  This is called "chrominance subsampling".
        /// </para>
        /// </summary>
        public enum TJSubsamplingOptions
        {
            /// <summary>
            /// 4:4:4 chrominance subsampling (no chrominance subsampling).  The JPEG or * YUV image will contain one chrominance component for every pixel in the source image.
            /// </summary>
            TJSAMP_444 = 0,

            /// <summary>
            /// 4:2:2 chrominance subsampling.  The JPEG or YUV image will contain one
            /// chrominance component for every 2x1 block of pixels in the source image.
            /// </summary>
            TJSAMP_422,

            /// <summary>
            /// 4:2:0 chrominance subsampling.  The JPEG or YUV image will contain one
            /// chrominance component for every 2x2 block of pixels in the source image.
            /// </summary>
            TJSAMP_420,

            /// <summary>
            /// Grayscale.  The JPEG or YUV image will contain no chrominance components.
            /// </summary>
            TJSAMP_GRAY,

            /// <summary>
            /// 4:4:0 chrominance subsampling.  The JPEG or YUV image will contain one
            /// chrominance component for every 1x2 block of pixels in the source image. 
            /// </summary>
            /// <remarks>4:4:0 subsampling is not fully accelerated in libjpeg-turbo.</remarks>
            TJSAMP_440,

            /// <summary>
            /// 4:1:1 chrominance subsampling.  The JPEG or YUV image will contain one
            /// chrominance component for every 4x1 block of pixels in the source image.
            /// JPEG images compressed with 4:1:1 subsampling will be almost exactly the
            /// same size as those compressed with 4:2:0 subsampling, and in the
            /// aggregate, both subsampling methods produce approximately the same
            /// perceptual quality.  However, 4:1:1 is better able to reproduce sharp
            /// horizontal features.
            /// </summary>
            /// <remarks> 4:1:1 subsampling is not fully accelerated in libjpeg-turbo.</remarks>
            TJSAMP_411
        };


        /// <summary>
        /// Free an image buffer previously allocated by TurboJPEG.  You should always
        /// use this function to free JPEG destination buffer(s) that were automatically
        /// (re)allocated by <see cref="tjCompress2"/> or <see cref="tjTransform"/> or that were manually
        /// allocated using <see cref="tjAlloc"/>. 
        /// </summary>
        /// <param name="buffer">Address of the buffer to free</param>
        /// <seealso cref="tjAlloc"/>
        [DllImport(LibraryName, CallingConvention = CallingConvention.Cdecl)]
        public static extern void tjFree(IntPtr buffer);


        public byte[] Compress(IntPtr srcPtr, int stride, int width, int height, TJPixelFormats pixelFormat, TJSubsamplingOptions subSamp, int quality, TJFlags flags)
        {
            var buf = IntPtr.Zero;
            ulong bufSize = 0;

            try
            {
                var result = tjCompress2(
                    _compressorHandle,
                    srcPtr,
                    width,
                    stride,
                    height,
                    (int)pixelFormat,
                    ref buf,
                    ref bufSize,
                    (int)subSamp,
                    quality,
                    (int)flags);

                if (result == -1)
                {
                    GetErrorAndThrow();
                }

                var jpegBuf = new byte[bufSize];
                Marshal.Copy(buf, jpegBuf, 0, (int)bufSize);
                return jpegBuf;
            }
            finally
            {
                tjFree(buf);
            }
        }
    }
}
