using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;

namespace NiVE3.OpenFX.Host.CL
{
    /// <summary>
    /// OpenCL Buffers レンダリング (OFX 1.5) に必要な範囲の OpenCL API
    /// OpenCL.dll (ICD ローダー) は GPU ドライバと共にインストールされるため、
    /// 存在しない環境では ClContextManager が初期化に失敗し OpenCL レンダリングが無効になります
    /// </summary>
    public static unsafe partial class ClNative
    {
        const string Library = "OpenCL.dll";

        public const int CL_SUCCESS = 0;

        public const ulong CL_DEVICE_TYPE_GPU = 1 << 2;

        public const ulong CL_MEM_READ_WRITE = 1 << 0;

        public const ulong CL_MEM_COPY_HOST_PTR = 1 << 5;

        public const uint CL_PLATFORM_NAME = 0x0902;

        public const uint CL_DEVICE_NAME = 0x102B;

        public const nint CL_CONTEXT_PLATFORM = 0x1084;

        [LibraryImport(Library)]
        public static partial int clGetPlatformIDs(uint numEntries, nint* platforms, uint* numPlatforms);

        [LibraryImport(Library)]
        public static partial int clGetPlatformInfo(nint platform, uint paramName, nuint valueSize, void* value, nuint* valueSizeRet);

        [LibraryImport(Library)]
        public static partial int clGetDeviceIDs(nint platform, ulong deviceType, uint numEntries, nint* devices, uint* numDevices);

        [LibraryImport(Library)]
        public static partial int clGetDeviceInfo(nint device, uint paramName, nuint valueSize, void* value, nuint* valueSizeRet);

        [LibraryImport(Library)]
        public static partial nint clCreateContext(nint* properties, uint numDevices, nint* devices, nint pfnNotify, nint userData, int* errcodeRet);

        [LibraryImport(Library)]
        public static partial nint clCreateCommandQueue(nint context, nint device, ulong properties, int* errcodeRet);

        [LibraryImport(Library)]
        public static partial nint clCreateBuffer(nint context, ulong flags, nuint size, void* hostPtr, int* errcodeRet);

        [LibraryImport(Library)]
        public static partial int clEnqueueWriteBuffer(nint commandQueue, nint buffer, uint blockingWrite, nuint offset, nuint size, void* ptr, uint numEventsInWaitList, nint* eventWaitList, nint* ev);

        [LibraryImport(Library)]
        public static partial int clEnqueueReadBuffer(nint commandQueue, nint buffer, uint blockingRead, nuint offset, nuint size, void* ptr, uint numEventsInWaitList, nint* eventWaitList, nint* ev);

        [LibraryImport(Library)]
        public static partial int clEnqueueCopyBuffer(nint commandQueue, nint srcBuffer, nint dstBuffer, nuint srcOffset, nuint dstOffset, nuint size, uint numEventsInWaitList, nint* eventWaitList, nint* ev);

        [LibraryImport(Library)]
        public static partial int clFinish(nint commandQueue);

        [LibraryImport(Library)]
        public static partial int clReleaseMemObject(nint memObject);

        [LibraryImport(Library)]
        public static partial int clReleaseCommandQueue(nint commandQueue);

        [LibraryImport(Library)]
        public static partial int clReleaseContext(nint context);

        /// <summary>
        /// 文字列のプラットフォーム情報を取得します
        /// </summary>
        public static string GetPlatformName(nint platform)
        {
            nuint size;
            if (clGetPlatformInfo(platform, CL_PLATFORM_NAME, 0, null, &size) != CL_SUCCESS || size == 0)
            {
                return "";
            }
            var buffer = stackalloc byte[(int)size];
            clGetPlatformInfo(platform, CL_PLATFORM_NAME, size, buffer, null);
            return Marshal.PtrToStringUTF8((nint)buffer) ?? "";
        }

        /// <summary>
        /// 文字列のデバイス情報を取得します
        /// </summary>
        public static string GetDeviceName(nint device)
        {
            nuint size;
            if (clGetDeviceInfo(device, CL_DEVICE_NAME, 0, null, &size) != CL_SUCCESS || size == 0)
            {
                return "";
            }
            var buffer = stackalloc byte[(int)size];
            clGetDeviceInfo(device, CL_DEVICE_NAME, size, buffer, null);
            return Marshal.PtrToStringUTF8((nint)buffer) ?? "";
        }
    }
}
