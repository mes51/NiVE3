using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;

namespace NiVE3.OpenFX.Host.GL
{
    /// <summary>
    /// OpenGL / WGL の P/Invoke 定義 (ホストに必要な最小限のみ)
    /// </summary>
    public static unsafe partial class GlNative
    {
        // GL 定数
        public const uint GL_NO_ERROR = 0;
        public const uint GL_VERSION = 0x1F02;
        public const uint GL_RENDERER = 0x1F01;
        public const uint GL_TEXTURE_2D = 0x0DE1;
        public const uint GL_RGBA = 0x1908;
        public const uint GL_RGBA32F = 0x8814;
        public const uint GL_FLOAT = 0x1406;
        public const uint GL_TEXTURE_MIN_FILTER = 0x2801;
        public const uint GL_TEXTURE_MAG_FILTER = 0x2800;
        public const uint GL_TEXTURE_WRAP_S = 0x2802;
        public const uint GL_TEXTURE_WRAP_T = 0x2803;
        public const uint GL_NEAREST = 0x2600;
        public const uint GL_CLAMP_TO_EDGE = 0x812F;
        public const uint GL_UNPACK_ALIGNMENT = 0x0CF5;
        public const uint GL_PACK_ALIGNMENT = 0x0D05;
        public const uint GL_FRAMEBUFFER = 0x8D40;
        public const uint GL_COLOR_ATTACHMENT0 = 0x8CE0;
        public const uint GL_FRAMEBUFFER_COMPLETE = 0x8CD5;
        public const uint GL_COLOR_BUFFER_BIT = 0x00004000;
        public const uint GL_PROJECTION = 0x1701;
        public const uint GL_MODELVIEW = 0x1700;
        public const uint GL_QUADS = 0x0007;
        public const uint GL_BLEND = 0x0BE2;
        public const uint GL_DEPTH_TEST = 0x0B71;
        public const uint GL_SCISSOR_TEST = 0x0C11;
        public const uint GL_STENCIL_TEST = 0x0B90;
        public const uint GL_CULL_FACE = 0x0B44;
        public const uint GL_ALPHA_TEST = 0x0BC0;
        public const uint GL_LIGHTING = 0x0B50;
        public const uint GL_TEXTURE0 = 0x84C0;
        public const uint GL_ARRAY_BUFFER = 0x8892;
        public const uint GL_ELEMENT_ARRAY_BUFFER = 0x8893;
        public const uint GL_PIXEL_PACK_BUFFER = 0x88EB;
        public const uint GL_PIXEL_UNPACK_BUFFER = 0x88EC;
        public const uint GL_UNPACK_ROW_LENGTH = 0x0CF2;
        public const uint GL_UNPACK_SKIP_ROWS = 0x0CF3;
        public const uint GL_UNPACK_SKIP_PIXELS = 0x0CF4;
        public const uint GL_PACK_ROW_LENGTH = 0x0D02;
        public const uint GL_PACK_SKIP_ROWS = 0x0D03;
        public const uint GL_PACK_SKIP_PIXELS = 0x0D04;

        // PIXELFORMATDESCRIPTOR
        public const uint PFD_DRAW_TO_WINDOW = 0x00000004;
        public const uint PFD_SUPPORT_OPENGL = 0x00000020;
        public const byte PFD_TYPE_RGBA = 0;

        [StructLayout(LayoutKind.Sequential)]
        public struct PIXELFORMATDESCRIPTOR
        {
            public ushort nSize;
            public ushort nVersion;
            public uint dwFlags;
            public byte iPixelType;
            public byte cColorBits;
            public byte cRedBits;
            public byte cRedShift;
            public byte cGreenBits;
            public byte cGreenShift;
            public byte cBlueBits;
            public byte cBlueShift;
            public byte cAlphaBits;
            public byte cAlphaShift;
            public byte cAccumBits;
            public byte cAccumRedBits;
            public byte cAccumGreenBits;
            public byte cAccumBlueBits;
            public byte cAccumAlphaBits;
            public byte cDepthBits;
            public byte cStencilBits;
            public byte cAuxBuffers;
            public byte iLayerType;
            public byte bReserved;
            public uint dwLayerMask;
            public uint dwVisibleMask;
            public uint dwDamageMask;
        }

        // user32 / gdi32
        [LibraryImport("user32.dll", EntryPoint = "CreateWindowExW", StringMarshalling = StringMarshalling.Utf16)]
        public static partial nint CreateWindowExW(uint exStyle, string className, string windowName, uint style, int x, int y, int width, int height, nint parent, nint menu, nint instance, nint param);

        [LibraryImport("user32.dll")]
        public static partial nint GetDC(nint hwnd);

        [LibraryImport("user32.dll")]
        public static partial int ReleaseDC(nint hwnd, nint hdc);

        [LibraryImport("user32.dll")]
        [return: MarshalAs(UnmanagedType.Bool)]
        public static partial bool DestroyWindow(nint hwnd);

        [LibraryImport("gdi32.dll")]
        public static partial int ChoosePixelFormat(nint hdc, in PIXELFORMATDESCRIPTOR pfd);

        [LibraryImport("gdi32.dll")]
        [return: MarshalAs(UnmanagedType.Bool)]
        public static partial bool SetPixelFormat(nint hdc, int format, in PIXELFORMATDESCRIPTOR pfd);

        // WGL
        [LibraryImport("opengl32.dll")]
        public static partial nint wglCreateContext(nint hdc);

        [LibraryImport("opengl32.dll")]
        [return: MarshalAs(UnmanagedType.Bool)]
        public static partial bool wglDeleteContext(nint hglrc);

        [LibraryImport("opengl32.dll")]
        [return: MarshalAs(UnmanagedType.Bool)]
        public static partial bool wglMakeCurrent(nint hdc, nint hglrc);

        [LibraryImport("opengl32.dll", EntryPoint = "wglGetProcAddress", StringMarshalling = StringMarshalling.Utf8)]
        public static partial nint wglGetProcAddress(string name);

        // OpenGL 1.1 (opengl32.dll 直エクスポート)
        [LibraryImport("opengl32.dll")]
        public static partial uint glGetError();

        [LibraryImport("opengl32.dll")]
        public static partial byte* glGetString(uint name);

        [LibraryImport("opengl32.dll")]
        public static partial void glViewport(int x, int y, int width, int height);

        [LibraryImport("opengl32.dll")]
        public static partial void glEnable(uint cap);

        [LibraryImport("opengl32.dll")]
        public static partial void glDisable(uint cap);

        [LibraryImport("opengl32.dll")]
        public static partial void glGenTextures(int n, uint* textures);

        [LibraryImport("opengl32.dll")]
        public static partial void glDeleteTextures(int n, uint* textures);

        [LibraryImport("opengl32.dll")]
        public static partial void glBindTexture(uint target, uint texture);

        [LibraryImport("opengl32.dll")]
        public static partial void glTexParameteri(uint target, uint pname, int param);

        [LibraryImport("opengl32.dll")]
        public static partial void glTexImage2D(uint target, int level, int internalFormat, int width, int height, int border, uint format, uint type, void* pixels);

        [LibraryImport("opengl32.dll")]
        public static partial void glPixelStorei(uint pname, int param);

        [LibraryImport("opengl32.dll")]
        public static partial void glReadPixels(int x, int y, int width, int height, uint format, uint type, void* pixels);

        [LibraryImport("opengl32.dll")]
        public static partial void glClearColor(float red, float green, float blue, float alpha);

        [LibraryImport("opengl32.dll")]
        public static partial void glClear(uint mask);

        [LibraryImport("opengl32.dll")]
        public static partial void glFinish();

        [LibraryImport("opengl32.dll")]
        public static partial void glMatrixMode(uint mode);

        [LibraryImport("opengl32.dll")]
        public static partial void glLoadIdentity();

        [LibraryImport("opengl32.dll")]
        public static partial void glOrtho(double left, double right, double bottom, double top, double zNear, double zFar);

        [LibraryImport("opengl32.dll")]
        public static partial void glBegin(uint mode);

        [LibraryImport("opengl32.dll")]
        public static partial void glEnd();

        [LibraryImport("opengl32.dll")]
        public static partial void glTexCoord2f(float s, float t);

        [LibraryImport("opengl32.dll")]
        public static partial void glVertex2f(float x, float y);

        [LibraryImport("opengl32.dll")]
        public static partial void glColor4f(float red, float green, float blue, float alpha);

        /// <summary>
        /// wglGetProcAddress で拡張関数のポインタを取得します
        /// </summary>
        /// <param name="name">関数名</param>
        /// <returns>関数ポインタ。取得できなかった場合は 0</returns>
        public static nint GetExtensionFunction(string name)
        {
            var address = wglGetProcAddress(name);
            // 1, 2, 3, -1 は失敗を表す値
            return address is 0 or 1 or 2 or 3 or -1 ? 0 : address;
        }
    }
}
