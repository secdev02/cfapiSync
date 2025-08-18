﻿using Microsoft.Win32.SafeHandles;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using Vanara.Extensions;
using Vanara.PInvoke;
using Windows.Win32.Storage.FileSystem;
using static Styletronix.CloudFilterApi.SafeHandlers;
using static Vanara.PInvoke.CldApi;

namespace Styletronix
{
    public partial class CloudFilterApi
    {
        public static CF_PLACEHOLDER_BASIC_INFO GetPlaceholderInfoBasic(string fullPath, bool isDirectory)
        {
            Styletronix.Debug.WriteLine("GetPlaceholderInfoBasic: " + fullPath, System.Diagnostics.TraceLevel.Verbose);

            using SafeCreateFileForCldApi h = new(fullPath, isDirectory);

            if (h.IsInvalid)
            {
                int err = Marshal.GetLastWin32Error();
                Debug.WriteLine("GetPlaceholderInfoBasic INVALID Handle! Error " + err + " - " + fullPath, System.Diagnostics.TraceLevel.Error);
                return default;
            }
            try
            {
                return GetPlaceholderInfoBasic(h);
            }
            catch (Exception e)
            {
                Debug.WriteLine("GetPlaceholderInfoBasic FAILED: " + e.Message, System.Diagnostics.TraceLevel.Error);
                return default;
            }
        }
        public static CF_PLACEHOLDER_STANDARD_INFO GetPlaceholderInfoStandard(string fullPath, bool isDirectory)
        {
            Styletronix.Debug.WriteLine("GetPlaceholderInfoStandard: " + fullPath, System.Diagnostics.TraceLevel.Verbose);

            using SafeCreateFileForCldApi h = new(fullPath, isDirectory);

            if (h.IsInvalid)
            {
                int err = Marshal.GetLastWin32Error();
                Styletronix.Debug.WriteLine("GetPlaceholderInfoBasic INVALID Handle! Error " + err + " - " + fullPath, System.Diagnostics.TraceLevel.Warning);
                return default;
            }

            try
            {
                return GetPlaceholderInfoStandard(h);
            }
            catch (Exception e)
            {
                Styletronix.Debug.WriteLine("GetPlaceholderInfoBasic FAILED: " + e.Message, System.Diagnostics.TraceLevel.Error);
                return default;
            }
        }
        public static CF_PLACEHOLDER_STANDARD_INFO GetPlaceholderInfoStandard(HFILE FileHandle)
        {
            Styletronix.Debug.WriteLine("GetPlaceholderInfoStandard by FileHandle " + ((IntPtr)FileHandle).ToString(), System.Diagnostics.TraceLevel.Verbose);

            if (FileHandle.IsInvalid)
            {
                Debug.WriteLine("GetPlaceholderInfoStandard INVALID Handle! " + ((IntPtr)FileHandle).ToString(), System.Diagnostics.TraceLevel.Warning);
                return default;
            }

            try
            {
                int InfoBufferLength = 1024;
                CF_PLACEHOLDER_STANDARD_INFO ResultInfo = default;

                using SafeAllocCoTaskMem bufferPointerHandler = new(InfoBufferLength);

                HRESULT ret = CfGetPlaceholderInfo(FileHandle, CF_PLACEHOLDER_INFO_CLASS.CF_PLACEHOLDER_INFO_STANDARD, bufferPointerHandler, (uint)InfoBufferLength, out uint returnedLength);

                if (returnedLength > 0)
                {
                    ResultInfo = Marshal.PtrToStructure<CF_PLACEHOLDER_STANDARD_INFO>(bufferPointerHandler);
                }
                else
                {
                    Styletronix.Debug.WriteLine("GetPlaceholderInfoStandard Failed: " + ((IntPtr)FileHandle).ToString(), System.Diagnostics.TraceLevel.Warning);
                }

                return ResultInfo;
            }
            catch (Exception e)
            {
                Debug.WriteLine("GetPlaceholderInfoStandard FAILED: " + e.Message, System.Diagnostics.TraceLevel.Error);
                return default;
            }
        }
        public static CF_PLACEHOLDER_BASIC_INFO GetPlaceholderInfoBasic(HFILE FileHandle)
        {
            Styletronix.Debug.WriteLine("GetPlaceholderInfoBasic: " + ((IntPtr)FileHandle).ToString(), System.Diagnostics.TraceLevel.Verbose);

            int InfoBufferLength = 1024;
            CF_PLACEHOLDER_BASIC_INFO ResultInfo = default;

            using SafeAllocCoTaskMem bufferPointerHandler = new(InfoBufferLength);

            if (!FileHandle.IsInvalid)
            {
                HRESULT ret = CfGetPlaceholderInfo(FileHandle, CF_PLACEHOLDER_INFO_CLASS.CF_PLACEHOLDER_INFO_BASIC, bufferPointerHandler, (uint)InfoBufferLength, out uint returnedLength);
                if (returnedLength > 0)
                {
                    ResultInfo = Marshal.PtrToStructure<CF_PLACEHOLDER_BASIC_INFO>(bufferPointerHandler);
                }
                else
                {
                    Styletronix.Debug.WriteLine("GetPlaceholderInfoBasic Failed: " + ((IntPtr)FileHandle).ToString(), System.Diagnostics.TraceLevel.Warning);
                }
            }

            return ResultInfo;
        }
        public static CF_PLACEHOLDER_CREATE_INFO CreatePlaceholderInfo(CloudSyncProvider.Placeholder placeholder, string fileIdentity)
        {
            CF_PLACEHOLDER_CREATE_INFO cfInfo = new()
            {
                FileIdentity = Marshal.StringToCoTaskMemUni(fileIdentity),
                FileIdentityLength = (uint)(fileIdentity.Length * Marshal.SizeOf(fileIdentity[0])),

                RelativeFileName = placeholder.RelativeFileName,
                FsMetadata = new CF_FS_METADATA
                {
                    FileSize = placeholder.FileSize,
                    BasicInfo = CreateFileBasicInfo(placeholder)
                },
                Flags = CF_PLACEHOLDER_CREATE_FLAGS.CF_PLACEHOLDER_CREATE_FLAG_MARK_IN_SYNC
            };

            return cfInfo;
        }
        public static CF_FS_METADATA CreateFSMetaData(CloudSyncProvider.Placeholder placeholder)
        {
            return new CF_FS_METADATA
            {
                FileSize = placeholder.FileSize,
                BasicInfo = CreateFileBasicInfo(placeholder)
            };
        }
        public static Kernel32.FILE_BASIC_INFO CreateFileBasicInfo(CloudSyncProvider.Placeholder placeholder)
        {
            return new Kernel32.FILE_BASIC_INFO
            {
                FileAttributes = (FileFlagsAndAttributes)placeholder.FileAttributes,
                CreationTime = placeholder.CreationTime.ToFileTimeStruct(),
                LastWriteTime = placeholder.LastWriteTime.ToFileTimeStruct(),
                LastAccessTime = placeholder.LastAccessTime.ToFileTimeStruct(),
                ChangeTime = placeholder.LastWriteTime.ToFileTimeStruct()
            };
        }

        public static bool SetInSyncState(string fullPath, CF_IN_SYNC_STATE inSyncState, bool isDirectory)
        {
            string d = fullPath.TrimEnd('\\');
           Styletronix.Debug.WriteLine("SetInSyncState " + inSyncState.ToString() + " - " + d, System.Diagnostics.TraceLevel.Info);

            using SafeCreateFileForCldApi h = new(fullPath, isDirectory);

            if (h.IsInvalid)
            {
                Styletronix.Debug.WriteLine("SetInSyncState INVALID Handle! " + fullPath.TrimEnd('\\'), System.Diagnostics.TraceLevel.Warning);
                return false;
            }

            HRESULT result = CfSetInSyncState((SafeFileHandle)h, inSyncState, CF_SET_IN_SYNC_FLAGS.CF_SET_IN_SYNC_FLAG_NONE);
            Styletronix.Debug.LogResponse(result);
            return result.Succeeded;
        }
        public static bool SetInSyncState(SafeFileHandle fileHandle, CF_IN_SYNC_STATE inSyncState)
        {
            Styletronix.Debug.WriteLine("SetInSyncState " + inSyncState.ToString() + " - FileHandle " + fileHandle.DangerousGetHandle().ToString(), System.Diagnostics.TraceLevel.Info);

            HRESULT res = CfSetInSyncState(fileHandle, inSyncState, CF_SET_IN_SYNC_FLAGS.CF_SET_IN_SYNC_FLAG_NONE);
            Styletronix.Debug.LogResponse(res);

            return res.Succeeded;
        }


        public class SafeHandlers
        {
            public class SafeCreateFileForCldApi : IDisposable
            {
                public SafeCreateFileForCldApi(string fullPath, bool isDirectory)
                {
                    FILE_ACCESS_FLAGS accessFlag = isDirectory ? FILE_ACCESS_FLAGS.FILE_GENERIC_READ : FILE_ACCESS_FLAGS.FILE_READ_EA;
                    FILE_FLAGS_AND_ATTRIBUTES attributsFlag = isDirectory ? FILE_FLAGS_AND_ATTRIBUTES.FILE_FLAG_BACKUP_SEMANTICS : FILE_FLAGS_AND_ATTRIBUTES.FILE_ATTRIBUTE_NORMAL | FILE_FLAGS_AND_ATTRIBUTES.FILE_FLAG_OVERLAPPED;

                    _handle = Windows.Win32.PInvoke.CreateFileW(@"\\?\" + fullPath,
                         accessFlag,
                           FILE_SHARE_MODE.FILE_SHARE_READ |
                           FILE_SHARE_MODE.FILE_SHARE_WRITE |
                           FILE_SHARE_MODE.FILE_SHARE_DELETE,
                           null,
                           FILE_CREATION_DISPOSITION.OPEN_EXISTING,
                           attributsFlag,
                           null);
                }

                public bool IsInvalid => _handle.IsInvalid;

                public static implicit operator SafeFileHandle(SafeCreateFileForCldApi instance)
                {
                    return instance._handle;
                }
                public static implicit operator HFILE(SafeCreateFileForCldApi instance)
                {
                    return instance._handle;
                }

                private readonly SafeFileHandle _handle;

                private bool disposedValue;
                protected virtual void Dispose(bool disposing)
                {
                    if (!disposedValue)
                    {
                        if (disposing)
                        {
                            _handle?.Dispose();
                        }
                        disposedValue = true;
                    }
                }
                public void Dispose()
                {
                    Dispose(disposing: true);
                    GC.SuppressFinalize(this);
                }
            }
            public class SafeOpenFileWithOplock : IDisposable
            {
                public SafeOpenFileWithOplock(string fullPath, CF_OPEN_FILE_FLAGS Flags)
                {
                    CfOpenFileWithOplock(fullPath, Flags, out _handle);
                }

                public bool IsInvalid => _handle.IsInvalid;

                public static implicit operator SafeHCFFILE(SafeOpenFileWithOplock instance)
                {
                    return instance._handle;
                }
                public static implicit operator HFILE(SafeOpenFileWithOplock instance)
                {
                    return instance._handle.DangerousGetHandle();
                }

                private readonly SafeHCFFILE _handle;


                private bool disposedValue;

                protected virtual void Dispose(bool disposing)
                {
                    if (!disposedValue)
                    {
                        if (disposing)
                        {
                            _handle?.Dispose();
                        }
                        disposedValue = true;
                    }
                }

                public void Dispose()
                {
                    // Ändern Sie diesen Code nicht. Fügen Sie Bereinigungscode in der Methode "Dispose(bool disposing)" ein.
                    Dispose(disposing: true);
                    GC.SuppressFinalize(this);
                }
            }
            public class SafeAllocCoTaskMem : IDisposable
            {
                private readonly IntPtr _pointer;
                public readonly int Size;

                public SafeAllocCoTaskMem(int size)
                {
                    Size = size;
                    _pointer = Marshal.AllocCoTaskMem(Size);
                }
                public SafeAllocCoTaskMem(object structure)
                {
                    Size = Marshal.SizeOf(structure);
                    _pointer = Marshal.AllocCoTaskMem(Size);
                    Marshal.StructureToPtr(structure, _pointer, false);
                }
                public SafeAllocCoTaskMem(string data)
                {
                    Size = data.Length * Marshal.SystemDefaultCharSize;
                    _pointer = Marshal.StringToCoTaskMemUni(data);
                }

                public static implicit operator IntPtr(SafeAllocCoTaskMem instance)
                {
                    return instance._pointer;
                }

                #region "Dispose"

                private bool disposedValue;
                protected virtual void Dispose(bool disposing)
                {
                    if (!disposedValue)
                    {
                        if (disposing)
                        {

                        }

                        Marshal.FreeCoTaskMem(_pointer);
                        disposedValue = true;
                    }
                }

                ~SafeAllocCoTaskMem()
                {
                    // Ändern Sie diesen Code nicht. Fügen Sie Bereinigungscode in der Methode "Dispose(bool disposing)" ein.
                    Dispose(disposing: false);
                }

                public void Dispose()
                {
                    // Ändern Sie diesen Code nicht. Fügen Sie Bereinigungscode in der Methode "Dispose(bool disposing)" ein.
                    Dispose(disposing: true);
                    GC.SuppressFinalize(this);
                }

                #endregion
            }
            public class SafeTransferKey : IDisposable
            {
                private readonly CF_TRANSFER_KEY TransferKey;
                private readonly HFILE handle;

                public SafeTransferKey(HFILE handle)
                {
                    this.handle = handle;

                    CfGetTransferKey(this.handle, out TransferKey).ThrowIfFailed();
                }
                public SafeTransferKey(SafeFileHandle safeHandle)
                {
                    handle = safeHandle;

                    CfGetTransferKey(handle, out TransferKey).ThrowIfFailed();
                }


                public static implicit operator CF_TRANSFER_KEY(SafeTransferKey instance)
                {
                    return instance.TransferKey;
                }


                private bool disposedValue;
                protected virtual void Dispose(bool disposing)
                {
                    if (!disposedValue)
                    {
                        if (disposing)
                        {

                        }

                        if (!handle.IsInvalid)
                        {
                            CfReleaseTransferKey(handle, TransferKey);
                        }

                        disposedValue = true;
                    }
                }

                ~SafeTransferKey()
                {
                    // Ändern Sie diesen Code nicht. Fügen Sie Bereinigungscode in der Methode "Dispose(bool disposing)" ein.
                    Dispose(disposing: false);
                }

                public void Dispose()
                {
                    // Ändern Sie diesen Code nicht. Fügen Sie Bereinigungscode in der Methode "Dispose(bool disposing)" ein.
                    Dispose(disposing: true);
                    GC.SuppressFinalize(this);
                }
            }
            public class SafePlaceHolderList : Vanara.InteropServices.SafeNativeArray<CF_PLACEHOLDER_CREATE_INFO>
            {
                protected override void Dispose(bool disposing)
                {
                    if (Elements != null)
                    {
                        foreach (CF_PLACEHOLDER_CREATE_INFO item in Elements)
                        {
                            if (item.FileIdentity != IntPtr.Zero) { Marshal.FreeCoTaskMem(item.FileIdentity); }
                        }
                    }

                    base.Dispose(disposing);
                }
            }
        }

        public class AutoDisposeList<T> : List<T>, IDisposable where T : IDisposable
        {
            public void Dispose()
            {
                foreach (T obj in this)
                {
                    obj.Dispose();
                }
            }
        }

        //public class CallbackSuspensionHelper : IDisposable
        //{
        //    private ConcurrentDictionary<string, int> suspendFileCallbackList = new();
        //    private bool disposedValue;

        //    public class SuspendedValue : IDisposable
        //    {
        //        private bool disposedValue;
        //        private readonly string value;
        //        private readonly ConcurrentDictionary<string, int> suspendFileCallbackList;

        //        public SuspendedValue(string value, ConcurrentDictionary<string, int> suspendFileCallbackList)
        //        {
        //            this.value = value;
        //            this.suspendFileCallbackList = suspendFileCallbackList;
        //        }

        //        protected virtual void Dispose(bool disposing)
        //        {
        //            if (!disposedValue)
        //            {
        //                if (disposing)
        //                {
        //                    int? ret = suspendFileCallbackList?.AddOrUpdate(value, 1, (k, v) => v--);

        //                    // TODO: Remove item from dictionary if <= 0
        //                    // Currently unknown how to do it thread safe

        //                    //if (ret <= 0)
        //                    //{
        //                    //   if ( suspendFileCallbackList.TryRemove(value, out ret))
        //                    //    {
        //                    //        if (ret >0)
        //                    //        {
        //                    //            suspendFileCallbackList.AddOrUpdate(value, ret, (k, v) => v++);
        //                    //        }
        //                    //    }
        //                    //}
        //                }

        //                disposedValue = true;
        //            }
        //        }
        //        public void Dispose()
        //        {
        //            // Ändern Sie diesen Code nicht. Fügen Sie Bereinigungscode in der Methode "Dispose(bool disposing)" ein.
        //            Dispose(disposing: true);
        //            GC.SuppressFinalize(this);
        //        }
        //    }

        //    public bool IsSuspended(string value)
        //    {
        //        if (!suspendFileCallbackList.TryGetValue(value, out int result))
        //        {
        //            return false;
        //        }

        //        return result > 0;
        //    }
        //    public SuspendedValue SetSuspension(string value)
        //    {
        //        return new SuspendedValue(value, suspendFileCallbackList);
        //    }

        //    protected virtual void Dispose(bool disposing)
        //    {
        //        if (!disposedValue)
        //        {
        //            if (disposing)
        //            {
        //                suspendFileCallbackList?.Clear();
        //                suspendFileCallbackList = null;
        //            }

        //            disposedValue = true;
        //        }
        //    }
        //    public void Dispose()
        //    {
        //        // Ändern Sie diesen Code nicht. Fügen Sie Bereinigungscode in der Methode "Dispose(bool disposing)" ein.
        //        Dispose(disposing: true);
        //        GC.SuppressFinalize(this);
        //    }
        //}

    }
}
