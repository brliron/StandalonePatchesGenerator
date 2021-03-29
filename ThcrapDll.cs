using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;

namespace StandaloneGeneratorV3
{
    class ThcrapDll
    {
        [StructLayout(LayoutKind.Sequential, Pack = 4), Serializable]
        public struct repo_patch_t
        {
            public string patch_id;
            public string title;
        }
        [StructLayout(LayoutKind.Sequential, Pack = 4), Serializable]
        public struct repo_t
        {
            public string id;
            public string title;
            public string contact;
            public IntPtr servers;
            public IntPtr neighbors;
            public IntPtr patches;
        }
        [StructLayout(LayoutKind.Sequential, Pack = 4), Serializable]
        public struct patch_desc_t
        {
            public string repo_id;
            public string patch_id;
        };
        [StructLayout(LayoutKind.Sequential, Pack = 4), Serializable]
        public struct patch_t
        {
            public IntPtr archive;
            public IntPtr id;
            IntPtr title;
            IntPtr servers;
            IntPtr dependencies;
            IntPtr fonts;
            IntPtr ignore;
            Byte update;
            UInt32 level;
            IntPtr config;
            IntPtr motd;
            IntPtr motd_title;
            UInt32 motd_type;
        }

        [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
        public delegate void log_print_cb(string text);
        [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
        public delegate void log_nprint_cb(
            [MarshalAs(UnmanagedType.LPArray, ArraySubType = UnmanagedType.U1, SizeParamIndex = 1)]
            byte[] text,
            UInt32 size);

        // global
        [DllImport("res\\thcrap\\bin\\thcrap.dll", CallingConvention = CallingConvention.Cdecl)]
        public static extern void thcrap_free(IntPtr ptr);

        // log
        [DllImport("res\\thcrap\\bin\\thcrap.dll", CallingConvention = CallingConvention.Cdecl)]
        public static extern void log_init(int console);
        [DllImport("res\\thcrap\\bin\\thcrap.dll", CallingConvention = CallingConvention.Cdecl)]
        public static extern void log_set_hook(log_print_cb hookproc, log_nprint_cb hookproc2);

        // repo
        [DllImport("res\\thcrap\\bin\\thcrap.dll", CallingConvention = CallingConvention.Cdecl)]
        public static extern void RepoFree(IntPtr /* repo_t* */ repo);

        // patch
        [DllImport("res\\thcrap\\bin\\thcrap.dll", CallingConvention = CallingConvention.Cdecl)]
        public static extern patch_t patch_init(string patch_path, IntPtr /* json_t* */ patch_info, UInt32 level);

        // stack
        [DllImport("res\\thcrap\\bin\\thcrap.dll", CallingConvention = CallingConvention.Cdecl)]
        public static extern void stack_add_patch(IntPtr /* patch_t* */ patch);
        [DllImport("res\\thcrap\\bin\\thcrap.dll", CallingConvention = CallingConvention.Cdecl)]
        public static extern void stack_free();
    }
    class ThcrapUpdateDll
    {
        public enum get_status_t
        {
            GET_DOWNLOADING,
            GET_OK,
            GET_CLIENT_ERROR,
            GET_CRC32_ERROR,
            GET_SERVER_ERROR,
            GET_CANCELLED,
            GET_SYSTEM_ERROR,
        }
        [StructLayout(LayoutKind.Sequential, Pack = 4), Serializable]
        public struct progress_callback_status_t
        {
            // Patch containing the file in download
            public IntPtr /* patch_t* */ patch;
            // File name
            public string fn;
            // Download URL
            public string url;
            // File download status or result
            public get_status_t status;
            // Human-readable error message if status is
            // GET_CLIENT_ERROR, GET_SERVER_ERROR or
            // GET_SYSTEM_ERROR, nullptr otherwise.
            public string error;

            // Bytes downloaded for the current file
            public UInt32 file_progress;
            // Size of the current file
            public UInt32 file_size;

            // Number of files downloaded in the current session
            public UInt32 nb_files_downloaded;
            // Number of files to download. Note that it will be 0 if
            // all the files.js haven't been downloaded yet.
            public UInt32 nb_files_total;
        }


        [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
        public delegate int update_filter_func_t(string fn, IntPtr filter_data);
        [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
        public delegate bool progress_callback_t(IntPtr /* progress_callback_status_t* */ status, IntPtr param);


        [DllImport("res\\thcrap\\bin\\thcrap_update.dll", CallingConvention = CallingConvention.Cdecl)]
        public static extern IntPtr /* repo_t** */ RepoDiscover(string start_url);
        [DllImport("res\\thcrap\\bin\\thcrap_update.dll", CallingConvention = CallingConvention.Cdecl)]
        public static extern ThcrapDll.patch_t patch_bootstrap(IntPtr /* patch_desc_t* */ sel, IntPtr /* repo_t* */ repo);
        [DllImport("res\\thcrap\\bin\\thcrap_update.dll", CallingConvention = CallingConvention.Cdecl)]
        public static extern void stack_update(update_filter_func_t filter_func, IntPtr filter_data, progress_callback_t progress_callback, IntPtr progress_param);
    }
}
