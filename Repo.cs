using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;

namespace StandaloneGeneratorV3
{
    class RepoPatch
    {
        public Repo Repo { get; set; }
        public string Id { get; set; }
        public string Title { get; set; }
        // Initialized when calling AddToStack
        public string Archive { get; set; }

        public static RepoPatch From_repo_patch_t(Repo repo, IntPtr ptr)
        {
            IntPtr patch_id_pointer = Marshal.ReadIntPtr(ptr);
            if (patch_id_pointer == IntPtr.Zero)
                return null;

            ThcrapDll.repo_patch_t patch = Marshal.PtrToStructure<ThcrapDll.repo_patch_t>(ptr);
            RepoPatch outRepo = new RepoPatch();
            outRepo.Repo = repo;
            outRepo.Id = ThcrapHelper.PtrToStringUTF8(patch.patch_id);
            outRepo.Title = ThcrapHelper.PtrToStringUTF8(patch.title);
            return outRepo;
        }
        IntPtr AllocStructure<T>(T inObj)
        {
            IntPtr ptr = Marshal.AllocHGlobal(Marshal.SizeOf<T>());
            Marshal.StructureToPtr(inObj, ptr, false);
            return ptr;
        }
        public void AddToStack()
        {
            ThcrapDll.patch_desc_t patch_desc;
            patch_desc.repo_id = ThcrapHelper.StringUTF8ToPtr(this.Repo.Id);
            patch_desc.patch_id = ThcrapHelper.StringUTF8ToPtr(this.Id);

            IntPtr patch_desc_ptr = AllocStructure(patch_desc);
            ThcrapDll.patch_t patch_info = ThcrapDll.patch_bootstrap_wrapper(patch_desc_ptr, this.Repo.repo_ptr);

            this.Archive = Marshal.PtrToStringAnsi(patch_info.archive);
            ThcrapDll.patch_t patch_full = ThcrapDll.patch_init(this.Archive, IntPtr.Zero, 0);

            IntPtr patch_full_ptr = AllocStructure(patch_full);
            ThcrapDll.stack_add_patch(patch_full_ptr);

            // TODO: use DestroyStructure instead?
            Marshal.DestroyStructure<ThcrapDll.patch_desc_t>(patch_desc_ptr);
            Marshal.DestroyStructure<ThcrapDll.patch_t>(patch_full_ptr);
            Marshal.FreeHGlobal(patch_desc_ptr);
            Marshal.FreeHGlobal(patch_full_ptr);
        }
    }

    class Repo
    {
        public IntPtr repo_ptr;
        public string Id { get; set; }
        public string Title { get; set; }
        public List<RepoPatch> Patches { get; set; }
        public IEnumerable<RepoPatch> PatchesFiltered { get; set; }
        public void UpdateFilter(string filter)
        {
            if (this.Id.ToLower().Contains(filter))
                this.PatchesFiltered = this.Patches;
            else
                this.PatchesFiltered = this.Patches.Where((RepoPatch patch) => patch.Id.ToLower().Contains(filter));
        }

        public static List<Repo> Discovery(string start_url)
        {
            IntPtr repo_list = ThcrapDll.RepoDiscover_wrapper(start_url);
            if (repo_list == IntPtr.Zero)
                return new List<Repo>();

            var out_list = new List<Repo>();
            int current_repo = 0;
            IntPtr repo_ptr = Marshal.ReadIntPtr(repo_list, current_repo * IntPtr.Size);
            while (repo_ptr != IntPtr.Zero)
            {
                out_list.Add(new Repo(repo_ptr));
                current_repo++;
                repo_ptr = Marshal.ReadIntPtr(repo_list, current_repo * IntPtr.Size);
            }
            
            ThcrapDll.thcrap_free(repo_list);
            return out_list;
        }

        private Repo(IntPtr repo_ptr)
        {
            ThcrapDll.repo_t repo = Marshal.PtrToStructure<ThcrapDll.repo_t>(repo_ptr);

            Id = ThcrapHelper.PtrToStringUTF8(repo.id);
            Title = ThcrapHelper.PtrToStringUTF8(repo.title);
            this.repo_ptr = repo_ptr;

            Patches = new List<RepoPatch>();
            IntPtr current_patch_ptr = repo.patches;
            RepoPatch current_patch = RepoPatch.From_repo_patch_t(this, current_patch_ptr);
            while (current_patch != null)
            {
                Patches.Add(current_patch);
                current_patch_ptr += Marshal.SizeOf<ThcrapDll.repo_patch_t>();
                current_patch = RepoPatch.From_repo_patch_t(this, current_patch_ptr);
            }

            PatchesFiltered = Patches;
        }
        ~Repo()
        {
            ThcrapDll.RepoFree(repo_ptr);
        }
    }
}
