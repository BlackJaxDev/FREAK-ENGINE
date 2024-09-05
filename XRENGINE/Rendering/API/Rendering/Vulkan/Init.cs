using Silk.NET.Vulkan;

namespace XREngine.Rendering.Vulkan
{
    public unsafe partial class VulkanRenderer(XRWindow window) : AbstractRenderer<Vk>(window)
    {
        protected override Vk GetAPI()
            => Vk.GetApi();

        protected override void Initialize()
        {
            if (Window?.VkSurface is null)
                throw new Exception("Windowing platform doesn't support Vulkan.");

            CreateInstance();
            SetupDebugMessenger();
            CreateSurface();
            PickPhysicalDevice();
            CreateLogicalDevice();
            CreateCommandPool();

            CreateDescriptorSetLayout();
            CreateAllSwapChainObjects();

            //CreateTestModel();
            //CreateUniformBuffers();

            CreateSyncObjects();
        }

        protected override void CleanUp()
        {
            DestroyAllSwapChainObjects();
            DestroyDescriptorSetLayout();
            //DestroyTestModel();

            DestroySyncObjects();
            DestroyCommandPool();

            DestroyLogicalDevice();
            DestroyValidationLayers();
            DestroySurface();
            DestroyInstance();
        }

        // It should be noted that in a real world application, you're not supposed to actually call vkAllocateMemory for every individual buffer.
        // The maximum number of simultaneous memory allocations is limited by the maxMemoryAllocationCount physical device limit, which may be as low as 4096 even on high end hardware like an NVIDIA GTX 1080.
        // The right way to allocate memory for a large number of objects at the same time is to create a custom allocator that splits up a single allocation among many different objects by using the offset parameters that we've seen in many functions.

        private void AllocateMemory(MemoryAllocateInfo allocInfo, DeviceMemory* memPtr)
        {
            AllocationCallbacks callbacks = new()
            {
                PfnAllocation = new PfnAllocationFunction(Allocated),
                PfnReallocation = new PfnReallocationFunction(Reallocated),
                PfnFree = new PfnFreeFunction(Freed),
                PfnInternalAllocation = new PfnInternalAllocationNotification(InternalAllocated),
                PfnInternalFree = new PfnInternalFreeNotification(InternalFreed)
            };
            if (Api!.AllocateMemory(device, ref allocInfo, null, memPtr) != Result.Success)
                throw new Exception("Failed to allocate memory.");
        }

        public static unsafe void* Allocated(void* pUserData, nuint size, nuint alignment, SystemAllocationScope allocationScope)
        {
            //Output.Log();
            return null;
        }

        private void* Reallocated(void* pUserData, void* pOriginal, nuint size, nuint alignment, SystemAllocationScope allocationScope)
        {
            return null;
        }

        private void Freed(void* pUserData, void* pMemory)
        {

        }
        private void InternalAllocated(void* pUserData, nuint size, InternalAllocationType allocationType, SystemAllocationScope allocationScope)
        {

        }

        private void InternalFreed(void* pUserData, nuint size, InternalAllocationType allocationType, SystemAllocationScope allocationScope)
        {

        }

        //private VkModel? _testModel;

        //private void CreateTestModel()
        //{
        //    VkTexture tex = new(this, "Assets/Textures/test.png")
        //    {
        //        CreateSampler = true
        //    };
        //    tex.Allocate();

        //    _testModel = new VkModel(this)
        //    {
        //        Textures = [tex]
        //    };
        //    _testModel.Generate();
        //    //_testModel.LoadFromOBJ("Assets/Models/test.obj");
        //}

        //private void DestroyTestModel()
        //{
        //    _testModel?.Destroy();
        //}

        public void DeviceWaitIdle()
            => Api!.DeviceWaitIdle(device);
    }
}