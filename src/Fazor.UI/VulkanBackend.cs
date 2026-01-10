#if INCLUDE_VULKAN_BACKEND
using Silk.NET.Windowing;
using Silk.NET.Maths;
using Silk.NET.Vulkan;
using Silk.NET.Vulkan.Extensions.KHR;
using Silk.NET.Core.Native;
using SkiaSharp;
using Sandbox.UI;
using Sandbox.UI.Skia;
using System.Runtime.InteropServices;
using System.Text;
using VkSemaphore = Silk.NET.Vulkan.Semaphore;
using VkImage = Silk.NET.Vulkan.Image;

namespace Fazor.UI;

/// <summary>
/// Vulkan graphics backend for Fazor.
/// Provides hardware-accelerated rendering using Vulkan and SkiaSharp.
/// </summary>
public class VulkanBackend : IGraphicsBackend
{
    private Vk? _vk;
    private Instance _instance;
    private PhysicalDevice _physicalDevice;
    private Device _device;
    private Queue _graphicsQueue;
    private SurfaceKHR _windowSurface;
    private KhrSurface? _khrSurface;
    private KhrSwapchain? _khrSwapchain;
    private SwapchainKHR _swapchain;
    private VkImage[] _swapchainImages = Array.Empty<VkImage>();
    private ImageView[] _swapchainImageViews = Array.Empty<ImageView>();
    private Format _swapchainImageFormat;
    private Extent2D _swapchainExtent;
    
    private CommandPool _commandPool;
    private CommandBuffer[] _commandBuffers = Array.Empty<CommandBuffer>();
    private VkSemaphore _imageAvailableSemaphore;
    private VkSemaphore _renderFinishedSemaphore;
    private Fence _inFlightFence;
    
    private GRContext? _grContext;
    private GRVkBackendContext? _grVkBackendContext;
    private SKSurface? _skSurface;
    private SkiaPanelRenderer? _renderer;
    private IWindow? _window;
    private uint _graphicsQueueFamilyIndex;
    private uint _presentQueueFamilyIndex;
    private Queue _presentQueue;
    
    // Track instance/device extensions for Skia
    private string[] _instanceExtensions = Array.Empty<string>();
    private string[] _deviceExtensions = Array.Empty<string>();

    public void Initialize(IWindow window)
    {
        _window = window;
        _vk = Vk.GetApi();
        
        Console.WriteLine("[VulkanBackend] Initializing Vulkan backend...");
        
        // Create Vulkan instance with required extensions
        CreateInstance();
        Console.WriteLine("[VulkanBackend] Instance created");
        
        // Create window surface
        CreateWindowSurface();
        Console.WriteLine("[VulkanBackend] Window surface created");
        
        // Select physical device that supports our surface
        SelectPhysicalDevice();
        Console.WriteLine("[VulkanBackend] Physical device selected");
        
        // Create logical device and queues
        CreateDevice();
        Console.WriteLine("[VulkanBackend] Logical device created");
        
        // Create swapchain
        CreateSwapchain();
        Console.WriteLine("[VulkanBackend] Swapchain created");
        
        // Create command pool
        CreateCommandPool();
        Console.WriteLine("[VulkanBackend] Command pool created");
        
        // Create synchronization objects
        CreateSyncObjects();
        Console.WriteLine("[VulkanBackend] Sync objects created");
        
        // Create Skia GRContext with Vulkan backend
        CreateGRContext();
        Console.WriteLine("[VulkanBackend] SkiaSharp GRContext created");
        
        _renderer = new SkiaPanelRenderer();
        
        CreateRenderSurface();
        Console.WriteLine("[VulkanBackend] SKSurface created");
        
        Console.WriteLine("[VulkanBackend] Vulkan backend initialized successfully!");
    }

    private unsafe void CreateInstance()
    {
        // Get required extensions from the window
        byte** windowExtensions = null;
        uint extCount = 0;
        
        if (_window!.VkSurface != null)
        {
            windowExtensions = _window.VkSurface.GetRequiredExtensions(out extCount);
        }
        
        var extensionsList = new List<string>();
        
        if (windowExtensions != null && extCount > 0)
        {
            for (int i = 0; i < (int)extCount; i++)
            {
                extensionsList.Add(SilkMarshal.PtrToString((nint)windowExtensions[i])!);
            }
        }
        else
        {
            // Fallback: add common surface extensions
            extensionsList.Add("VK_KHR_surface");
            if (OperatingSystem.IsWindows())
                extensionsList.Add("VK_KHR_win32_surface");
            else if (OperatingSystem.IsLinux())
            {
                extensionsList.Add("VK_KHR_xlib_surface");
                extensionsList.Add("VK_KHR_xcb_surface");
                extensionsList.Add("VK_KHR_wayland_surface");
            }
            else if (OperatingSystem.IsMacOS())
                extensionsList.Add("VK_MVK_macos_surface");
        }
        
        _instanceExtensions = extensionsList.ToArray();
        Console.WriteLine($"[VulkanBackend] Enabling instance extensions: {string.Join(", ", _instanceExtensions)}");
        
        var appInfo = new ApplicationInfo
        {
            SType = StructureType.ApplicationInfo,
            PApplicationName = (byte*)SilkMarshal.StringToPtr("Fazor"),
            ApplicationVersion = Vk.MakeVersion(1, 0, 0),
            PEngineName = (byte*)SilkMarshal.StringToPtr("Fazor"),
            EngineVersion = Vk.MakeVersion(1, 0, 0),
            ApiVersion = Vk.Version12
        };

        // Convert extension names to native pointers
        var extensionPtrs = stackalloc byte*[extensionsList.Count];
        for (int i = 0; i < extensionsList.Count; i++)
        {
            extensionPtrs[i] = (byte*)SilkMarshal.StringToPtr(extensionsList[i]);
        }

        var createInfo = new InstanceCreateInfo
        {
            SType = StructureType.InstanceCreateInfo,
            PApplicationInfo = &appInfo,
            EnabledExtensionCount = (uint)extensionsList.Count,
            PpEnabledExtensionNames = extensionPtrs
        };

        var result = _vk!.CreateInstance(in createInfo, null, out _instance);
        
        // Clean up string memory
        SilkMarshal.Free((nint)appInfo.PApplicationName);
        SilkMarshal.Free((nint)appInfo.PEngineName);
        for (int i = 0; i < extensionsList.Count; i++)
        {
            SilkMarshal.Free((nint)extensionPtrs[i]);
        }

        if (result != Result.Success)
        {
            throw new Exception($"Failed to create Vulkan instance: {result}");
        }
        
        // Get KHR surface extension
        if (!_vk.TryGetInstanceExtension(_instance, out _khrSurface))
        {
            throw new Exception("Failed to get VK_KHR_surface extension");
        }
    }

    private unsafe void CreateWindowSurface()
    {
        if (_window!.VkSurface == null)
        {
            throw new Exception("Window does not support Vulkan surface creation");
        }
        
        var surfaceHandle = _window.VkSurface.Create<AllocationCallbacks>(_instance.ToHandle(), null);
        _windowSurface = surfaceHandle.ToSurface();
    }

    private unsafe void SelectPhysicalDevice()
    {
        uint deviceCount = 0;
        _vk!.EnumeratePhysicalDevices(_instance, &deviceCount, null);
        
        if (deviceCount == 0)
        {
            throw new Exception("No Vulkan-capable devices found");
        }

        var devices = new PhysicalDevice[deviceCount];
        fixed (PhysicalDevice* devicesPtr = devices)
        {
            _vk.EnumeratePhysicalDevices(_instance, &deviceCount, devicesPtr);
        }
        
        // Find a device that supports graphics and presentation
        foreach (var device in devices)
        {
            if (IsDeviceSuitable(device))
            {
                _physicalDevice = device;
                break;
            }
        }
        
        if (_physicalDevice.Handle == 0)
        {
            // Fall back to first device
            _physicalDevice = devices[0];
        }
        
        // Get queue family indices
        FindQueueFamilies(_physicalDevice);
        
        // Print device info
        PhysicalDeviceProperties props;
        _vk.GetPhysicalDeviceProperties(_physicalDevice, out props);
        Console.WriteLine($"[VulkanBackend] Using device: {SilkMarshal.PtrToString((nint)props.DeviceName)}");
    }

    private unsafe bool IsDeviceSuitable(PhysicalDevice device)
    {
        // Check if device supports required queue families
        uint queueFamilyCount = 0;
        _vk!.GetPhysicalDeviceQueueFamilyProperties(device, &queueFamilyCount, null);
        
        var queueFamilies = new QueueFamilyProperties[queueFamilyCount];
        fixed (QueueFamilyProperties* qfPtr = queueFamilies)
        {
            _vk.GetPhysicalDeviceQueueFamilyProperties(device, &queueFamilyCount, qfPtr);
        }
        
        bool hasGraphics = false;
        bool hasPresent = false;
        
        for (uint i = 0; i < queueFamilyCount; i++)
        {
            if ((queueFamilies[i].QueueFlags & QueueFlags.GraphicsBit) != 0)
                hasGraphics = true;
                
            _khrSurface!.GetPhysicalDeviceSurfaceSupport(device, i, _windowSurface, out var presentSupport);
            if (presentSupport)
                hasPresent = true;
        }
        
        // Check for swapchain extension
        uint extensionCount = 0;
        _vk.EnumerateDeviceExtensionProperties(device, (byte*)null, &extensionCount, null);
        var availableExtensions = new ExtensionProperties[extensionCount];
        fixed (ExtensionProperties* extPtr = availableExtensions)
        {
            _vk.EnumerateDeviceExtensionProperties(device, (byte*)null, &extensionCount, extPtr);
        }
        
        bool hasSwapchain = availableExtensions.Any(ext => 
            SilkMarshal.PtrToString((nint)ext.ExtensionName) == "VK_KHR_swapchain");
        
        return hasGraphics && hasPresent && hasSwapchain;
    }

    private unsafe void FindQueueFamilies(PhysicalDevice device)
    {
        uint queueFamilyCount = 0;
        _vk!.GetPhysicalDeviceQueueFamilyProperties(device, &queueFamilyCount, null);
        
        var queueFamilies = new QueueFamilyProperties[queueFamilyCount];
        fixed (QueueFamilyProperties* qfPtr = queueFamilies)
        {
            _vk.GetPhysicalDeviceQueueFamilyProperties(device, &queueFamilyCount, qfPtr);
        }
        
        _graphicsQueueFamilyIndex = uint.MaxValue;
        _presentQueueFamilyIndex = uint.MaxValue;
        
        for (uint i = 0; i < queueFamilyCount; i++)
        {
            if ((queueFamilies[i].QueueFlags & QueueFlags.GraphicsBit) != 0)
            {
                _graphicsQueueFamilyIndex = i;
            }
            
            _khrSurface!.GetPhysicalDeviceSurfaceSupport(device, i, _windowSurface, out var presentSupport);
            if (presentSupport)
            {
                _presentQueueFamilyIndex = i;
            }
            
            // Prefer a queue that supports both
            if (_graphicsQueueFamilyIndex == i && presentSupport)
            {
                _presentQueueFamilyIndex = i;
                break;
            }
        }
        
        if (_graphicsQueueFamilyIndex == uint.MaxValue || _presentQueueFamilyIndex == uint.MaxValue)
        {
            throw new Exception("Failed to find suitable queue families");
        }
    }

    private unsafe void CreateDevice()
    {
        // Create queue create infos for unique queue families
        var uniqueQueueFamilies = new HashSet<uint> { _graphicsQueueFamilyIndex, _presentQueueFamilyIndex };
        var queueCreateInfos = new DeviceQueueCreateInfo[uniqueQueueFamilies.Count];
        
        float queuePriority = 1.0f;
        int idx = 0;
        foreach (var queueFamily in uniqueQueueFamilies)
        {
            queueCreateInfos[idx++] = new DeviceQueueCreateInfo
            {
                SType = StructureType.DeviceQueueCreateInfo,
                QueueFamilyIndex = queueFamily,
                QueueCount = 1,
                PQueuePriorities = &queuePriority
            };
        }

        // Enable required device extensions
        _deviceExtensions = new[] { "VK_KHR_swapchain" };
        var extensionPtrs = stackalloc byte*[_deviceExtensions.Length];
        for (int i = 0; i < _deviceExtensions.Length; i++)
        {
            extensionPtrs[i] = (byte*)SilkMarshal.StringToPtr(_deviceExtensions[i]);
        }

        var deviceFeatures = new PhysicalDeviceFeatures();

        fixed (DeviceQueueCreateInfo* queueCreateInfosPtr = queueCreateInfos)
        {
            var createInfo = new DeviceCreateInfo
            {
                SType = StructureType.DeviceCreateInfo,
                QueueCreateInfoCount = (uint)queueCreateInfos.Length,
                PQueueCreateInfos = queueCreateInfosPtr,
                PEnabledFeatures = &deviceFeatures,
                EnabledExtensionCount = (uint)_deviceExtensions.Length,
                PpEnabledExtensionNames = extensionPtrs
            };

            var result = _vk!.CreateDevice(_physicalDevice, in createInfo, null, out _device);
            
            // Clean up extension string memory
            for (int i = 0; i < _deviceExtensions.Length; i++)
            {
                SilkMarshal.Free((nint)extensionPtrs[i]);
            }

            if (result != Result.Success)
            {
                throw new Exception($"Failed to create Vulkan device: {result}");
            }
        }

        _vk.GetDeviceQueue(_device, _graphicsQueueFamilyIndex, 0, out _graphicsQueue);
        _vk.GetDeviceQueue(_device, _presentQueueFamilyIndex, 0, out _presentQueue);
        
        // Get swapchain extension
        if (!_vk.TryGetDeviceExtension(_instance, _device, out _khrSwapchain))
        {
            throw new Exception("Failed to get VK_KHR_swapchain extension");
        }
    }

    private unsafe void CreateSwapchain()
    {
        // Query swapchain support
        _khrSurface!.GetPhysicalDeviceSurfaceCapabilities(_physicalDevice, _windowSurface, out var capabilities);
        
        uint formatCount = 0;
        _khrSurface.GetPhysicalDeviceSurfaceFormats(_physicalDevice, _windowSurface, &formatCount, null);
        var formats = new SurfaceFormatKHR[formatCount];
        fixed (SurfaceFormatKHR* formatsPtr = formats)
        {
            _khrSurface.GetPhysicalDeviceSurfaceFormats(_physicalDevice, _windowSurface, &formatCount, formatsPtr);
        }
        
        uint presentModeCount = 0;
        _khrSurface.GetPhysicalDeviceSurfacePresentModes(_physicalDevice, _windowSurface, &presentModeCount, null);
        var presentModes = new PresentModeKHR[presentModeCount];
        fixed (PresentModeKHR* presentModesPtr = presentModes)
        {
            _khrSurface.GetPhysicalDeviceSurfacePresentModes(_physicalDevice, _windowSurface, &presentModeCount, presentModesPtr);
        }
        
        // Choose surface format (prefer BGRA8 SRGB)
        var surfaceFormat = formats[0];
        foreach (var format in formats)
        {
            if (format.Format == Format.B8G8R8A8Srgb && format.ColorSpace == ColorSpaceKHR.SpaceSrgbNonlinearKhr)
            {
                surfaceFormat = format;
                break;
            }
        }
        _swapchainImageFormat = surfaceFormat.Format;
        
        // Choose present mode (prefer mailbox for low latency, fallback to FIFO)
        var presentMode = PresentModeKHR.FifoKhr;
        foreach (var mode in presentModes)
        {
            if (mode == PresentModeKHR.MailboxKhr)
            {
                presentMode = mode;
                break;
            }
        }
        
        // Choose swap extent
        if (capabilities.CurrentExtent.Width != uint.MaxValue)
        {
            _swapchainExtent = capabilities.CurrentExtent;
        }
        else
        {
            var size = _window!.FramebufferSize;
            _swapchainExtent = new Extent2D
            {
                Width = Math.Clamp((uint)size.X, capabilities.MinImageExtent.Width, capabilities.MaxImageExtent.Width),
                Height = Math.Clamp((uint)size.Y, capabilities.MinImageExtent.Height, capabilities.MaxImageExtent.Height)
            };
        }
        
        // Choose image count
        uint imageCount = capabilities.MinImageCount + 1;
        if (capabilities.MaxImageCount > 0 && imageCount > capabilities.MaxImageCount)
        {
            imageCount = capabilities.MaxImageCount;
        }
        
        var createInfo = new SwapchainCreateInfoKHR
        {
            SType = StructureType.SwapchainCreateInfoKhr,
            Surface = _windowSurface,
            MinImageCount = imageCount,
            ImageFormat = surfaceFormat.Format,
            ImageColorSpace = surfaceFormat.ColorSpace,
            ImageExtent = _swapchainExtent,
            ImageArrayLayers = 1,
            ImageUsage = ImageUsageFlags.ColorAttachmentBit | ImageUsageFlags.TransferDstBit,
            PreTransform = capabilities.CurrentTransform,
            CompositeAlpha = CompositeAlphaFlagsKHR.OpaqueBitKhr,
            PresentMode = presentMode,
            Clipped = true,
            OldSwapchain = default
        };
        
        // Handle queue family indices
        var queueFamilyIndices = stackalloc uint[] { _graphicsQueueFamilyIndex, _presentQueueFamilyIndex };
        if (_graphicsQueueFamilyIndex != _presentQueueFamilyIndex)
        {
            createInfo.ImageSharingMode = SharingMode.Concurrent;
            createInfo.QueueFamilyIndexCount = 2;
            createInfo.PQueueFamilyIndices = queueFamilyIndices;
        }
        else
        {
            createInfo.ImageSharingMode = SharingMode.Exclusive;
        }
        
        var result = _khrSwapchain!.CreateSwapchain(_device, in createInfo, null, out _swapchain);
        if (result != Result.Success)
        {
            throw new Exception($"Failed to create swapchain: {result}");
        }
        
        // Get swapchain images
        _khrSwapchain.GetSwapchainImages(_device, _swapchain, &imageCount, null);
        _swapchainImages = new VkImage[imageCount];
        fixed (VkImage* imagesPtr = _swapchainImages)
        {
            _khrSwapchain.GetSwapchainImages(_device, _swapchain, &imageCount, imagesPtr);
        }
        
        // Create image views
        CreateImageViews();
    }

    private unsafe void CreateImageViews()
    {
        _swapchainImageViews = new ImageView[_swapchainImages.Length];
        
        for (int i = 0; i < _swapchainImages.Length; i++)
        {
            var createInfo = new ImageViewCreateInfo
            {
                SType = StructureType.ImageViewCreateInfo,
                Image = _swapchainImages[i],
                ViewType = ImageViewType.Type2D,
                Format = _swapchainImageFormat,
                Components = new ComponentMapping
                {
                    R = ComponentSwizzle.Identity,
                    G = ComponentSwizzle.Identity,
                    B = ComponentSwizzle.Identity,
                    A = ComponentSwizzle.Identity
                },
                SubresourceRange = new ImageSubresourceRange
                {
                    AspectMask = ImageAspectFlags.ColorBit,
                    BaseMipLevel = 0,
                    LevelCount = 1,
                    BaseArrayLayer = 0,
                    LayerCount = 1
                }
            };
            
            var result = _vk!.CreateImageView(_device, in createInfo, null, out _swapchainImageViews[i]);
            if (result != Result.Success)
            {
                throw new Exception($"Failed to create image view: {result}");
            }
        }
    }

    private unsafe void CreateCommandPool()
    {
        var poolInfo = new CommandPoolCreateInfo
        {
            SType = StructureType.CommandPoolCreateInfo,
            QueueFamilyIndex = _graphicsQueueFamilyIndex,
            Flags = CommandPoolCreateFlags.ResetCommandBufferBit
        };
        
        var result = _vk!.CreateCommandPool(_device, in poolInfo, null, out _commandPool);
        if (result != Result.Success)
        {
            throw new Exception($"Failed to create command pool: {result}");
        }
        
        // Allocate command buffers
        _commandBuffers = new CommandBuffer[_swapchainImages.Length];
        var allocInfo = new CommandBufferAllocateInfo
        {
            SType = StructureType.CommandBufferAllocateInfo,
            CommandPool = _commandPool,
            Level = CommandBufferLevel.Primary,
            CommandBufferCount = (uint)_commandBuffers.Length
        };
        
        fixed (CommandBuffer* commandBuffersPtr = _commandBuffers)
        {
            result = _vk!.AllocateCommandBuffers(_device, in allocInfo, commandBuffersPtr);
            if (result != Result.Success)
            {
                throw new Exception($"Failed to allocate command buffers: {result}");
            }
        }
    }

    private unsafe void CreateSyncObjects()
    {
        var semaphoreInfo = new SemaphoreCreateInfo
        {
            SType = StructureType.SemaphoreCreateInfo
        };
        
        var fenceInfo = new FenceCreateInfo
        {
            SType = StructureType.FenceCreateInfo,
            Flags = FenceCreateFlags.SignaledBit
        };
        
        if (_vk!.CreateSemaphore(_device, in semaphoreInfo, null, out _imageAvailableSemaphore) != Result.Success ||
            _vk.CreateSemaphore(_device, in semaphoreInfo, null, out _renderFinishedSemaphore) != Result.Success ||
            _vk.CreateFence(_device, in fenceInfo, null, out _inFlightFence) != Result.Success)
        {
            throw new Exception("Failed to create synchronization objects");
        }
    }

    private unsafe void CreateGRContext()
    {
        // Create the GetProcedureAddress delegate for SkiaSharp
        GRVkGetProcedureAddressDelegate getProc = (name, instance, device) =>
        {
            if (device != IntPtr.Zero)
            {
                // Device-level function
                return _vk!.GetDeviceProcAddr(new Device((nint)device), name);
            }
            else if (instance != IntPtr.Zero)
            {
                // Instance-level function
                return _vk!.GetInstanceProcAddr(new Instance((nint)instance), name);
            }
            else
            {
                // Global function
                return _vk!.GetInstanceProcAddr(default, name);
            }
        };
        
        // Create GRVkExtensions
        var extensions = GRVkExtensions.Create(
            getProc,
            (IntPtr)_instance.Handle,
            (IntPtr)_physicalDevice.Handle,
            _instanceExtensions,
            _deviceExtensions
        );
        
        // Create backend context
        _grVkBackendContext = new GRVkBackendContext
        {
            VkInstance = (IntPtr)_instance.Handle,
            VkPhysicalDevice = (IntPtr)_physicalDevice.Handle,
            VkDevice = (IntPtr)_device.Handle,
            VkQueue = (IntPtr)_graphicsQueue.Handle,
            GraphicsQueueIndex = _graphicsQueueFamilyIndex,
            GetProcedureAddress = getProc,
            Extensions = extensions,
            MaxAPIVersion = Vk.Version12,
            ProtectedContext = false
        };
        
        // Create the GRContext
        _grContext = GRContext.CreateVulkan(_grVkBackendContext);
        if (_grContext == null)
        {
            throw new Exception("Failed to create Skia Vulkan GRContext");
        }
    }

    private void CreateRenderSurface()
    {
        if (_grContext == null) return;
        
        var imageInfo = new SKImageInfo(
            (int)_swapchainExtent.Width,
            (int)_swapchainExtent.Height,
            SKColorType.Bgra8888,
            SKAlphaType.Premul
        );
        
        // Configure surface properties for RGB subpixel rendering (ClearType on Windows)
        // This makes text appear sharper and more like native Windows text rendering
        // RgbHorizontal is the most common pixel layout on modern LCD displays
        var surfProps = new SKSurfaceProperties(SKPixelGeometry.RgbHorizontal);
        
        // Create a GPU-backed surface with subpixel rendering enabled
        // Using the overload: Create(GRContext, bool, SKImageInfo, int, GRSurfaceOrigin, SKSurfaceProperties, bool)
        _skSurface = SKSurface.Create(_grContext, false, imageInfo, 0, GRSurfaceOrigin.TopLeft, surfProps, false);
        
        if (_skSurface == null)
        {
            throw new Exception("Failed to create SKSurface for rendering");
        }
    }

    public unsafe void Resize(Vector2D<int> size)
    {
        if (_device.Handle == 0 || size.X <= 0 || size.Y <= 0) return;

        // Wait for device to be idle
        _vk!.DeviceWaitIdle(_device);
        
        // Dispose old surface
        _skSurface?.Dispose();
        _skSurface = null;
        
        // Cleanup old swapchain resources
        CleanupSwapchain();
        
        // Recreate swapchain
        CreateSwapchain();
        CreateRenderSurface();
        
        //Console.WriteLine($"[VulkanBackend] Resized to {size.X}x{size.Y}");
    }

    private unsafe void CleanupSwapchain()
    {
        foreach (var imageView in _swapchainImageViews)
        {
            _vk!.DestroyImageView(_device, imageView, null);
        }
        _swapchainImageViews = Array.Empty<ImageView>();
        
        if (_swapchain.Handle != 0)
        {
            _khrSwapchain!.DestroySwapchain(_device, _swapchain, null);
            _swapchain = default;
        }
    }

    public unsafe void Render(RootPanel panel)
    {
        if (_grContext == null || _renderer == null || _skSurface == null) return;

        // Wait for previous frame
        _vk!.WaitForFences(_device, 1, in _inFlightFence, true, ulong.MaxValue);
        
        // Acquire next image
        uint imageIndex = 0;
        var result = _khrSwapchain!.AcquireNextImage(_device, _swapchain, ulong.MaxValue, 
            _imageAvailableSemaphore, default, ref imageIndex);
            
        if (result == Result.ErrorOutOfDateKhr)
        {
            Resize(_window!.FramebufferSize);
            return;
        }
        
        _vk.ResetFences(_device, 1, in _inFlightFence);
        
        // Render to the offscreen surface
        _skSurface.Canvas.Clear(new SKColor(240, 240, 240));
        _renderer.Render(_skSurface.Canvas, panel);
        _skSurface.Canvas.Flush();
        
        // Flush the GRContext to submit Vulkan commands
        _grContext.Flush();
        
        // Get the rendered image and copy it to the swapchain image
        var snapshot = _skSurface.Snapshot();
        CopyImageToSwapchain(snapshot, _swapchainImages[imageIndex]);
        snapshot.Dispose();
        
        // Submit and present
        var waitSemaphore = _imageAvailableSemaphore;
        var waitStages = PipelineStageFlags.ColorAttachmentOutputBit;
        var signalSemaphore = _renderFinishedSemaphore;
        
        var submitInfo = new SubmitInfo
        {
            SType = StructureType.SubmitInfo,
            WaitSemaphoreCount = 1,
            PWaitSemaphores = &waitSemaphore,
            PWaitDstStageMask = &waitStages,
            CommandBufferCount = 0,
            SignalSemaphoreCount = 1,
            PSignalSemaphores = &signalSemaphore
        };
        
        _vk.QueueSubmit(_graphicsQueue, 1, in submitInfo, _inFlightFence);
        
        // Present
        var swapchain = _swapchain;
        var presentInfo = new PresentInfoKHR
        {
            SType = StructureType.PresentInfoKhr,
            WaitSemaphoreCount = 1,
            PWaitSemaphores = &signalSemaphore,
            SwapchainCount = 1,
            PSwapchains = &swapchain,
            PImageIndices = &imageIndex
        };
        
        result = _khrSwapchain.QueuePresent(_presentQueue, in presentInfo);
        if (result == Result.ErrorOutOfDateKhr || result == Result.SuboptimalKhr)
        {
            Resize(_window!.FramebufferSize);
        }
    }

    private unsafe void CopyImageToSwapchain(SKImage sourceImage, VkImage destImage)
    {
        // Read pixels from GPU to CPU - create a CPU bitmap to hold the data
        var imageInfo = new SKImageInfo(
            (int)_swapchainExtent.Width,
            (int)_swapchainExtent.Height,
            SKColorType.Bgra8888,
            SKAlphaType.Premul
        );
        
        using var bitmap = new SKBitmap(imageInfo);
        if (!sourceImage.ReadPixels(bitmap.Info, bitmap.GetPixels(), bitmap.RowBytes, 0, 0))
        {
            Console.WriteLine("[VulkanBackend] Warning: Could not read pixels from GPU image");
            return;
        }
        
        // Create staging buffer for pixel data
        ulong imageSize = (ulong)(bitmap.RowBytes * bitmap.Height);
        
        var bufferInfo = new BufferCreateInfo
        {
            SType = StructureType.BufferCreateInfo,
            Size = imageSize,
            Usage = BufferUsageFlags.TransferSrcBit,
            SharingMode = SharingMode.Exclusive
        };
        
        Silk.NET.Vulkan.Buffer stagingBuffer;
        _vk!.CreateBuffer(_device, in bufferInfo, null, out stagingBuffer);
        
        // Get memory requirements
        MemoryRequirements memRequirements;
        _vk.GetBufferMemoryRequirements(_device, stagingBuffer, out memRequirements);
        
        // Allocate memory
        var allocInfo = new MemoryAllocateInfo
        {
            SType = StructureType.MemoryAllocateInfo,
            AllocationSize = memRequirements.Size,
            MemoryTypeIndex = FindMemoryType(memRequirements.MemoryTypeBits, 
                MemoryPropertyFlags.HostVisibleBit | MemoryPropertyFlags.HostCoherentBit)
        };
        
        DeviceMemory stagingBufferMemory;
        _vk.AllocateMemory(_device, in allocInfo, null, out stagingBufferMemory);
        _vk.BindBufferMemory(_device, stagingBuffer, stagingBufferMemory, 0);
        
        // Copy pixel data to staging buffer
        void* data;
        _vk.MapMemory(_device, stagingBufferMemory, 0, imageSize, 0, &data);
        System.Buffer.MemoryCopy(bitmap.GetPixels().ToPointer(), data, (long)imageSize, (long)imageSize);
        _vk.UnmapMemory(_device, stagingBufferMemory);
        
        // Create command buffer for the copy
        var cmdAllocInfo = new CommandBufferAllocateInfo
        {
            SType = StructureType.CommandBufferAllocateInfo,
            CommandPool = _commandPool,
            Level = CommandBufferLevel.Primary,
            CommandBufferCount = 1
        };
        
        CommandBuffer commandBuffer;
        _vk.AllocateCommandBuffers(_device, in cmdAllocInfo, &commandBuffer);
        
        var beginInfo = new CommandBufferBeginInfo
        {
            SType = StructureType.CommandBufferBeginInfo,
            Flags = CommandBufferUsageFlags.OneTimeSubmitBit
        };
        
        _vk.BeginCommandBuffer(commandBuffer, in beginInfo);
        
        // Transition destination image to TRANSFER_DST_OPTIMAL
        var barrier = new ImageMemoryBarrier
        {
            SType = StructureType.ImageMemoryBarrier,
            OldLayout = ImageLayout.Undefined,
            NewLayout = ImageLayout.TransferDstOptimal,
            SrcQueueFamilyIndex = Vk.QueueFamilyIgnored,
            DstQueueFamilyIndex = Vk.QueueFamilyIgnored,
            Image = destImage,
            SubresourceRange = new ImageSubresourceRange
            {
                AspectMask = ImageAspectFlags.ColorBit,
                BaseMipLevel = 0,
                LevelCount = 1,
                BaseArrayLayer = 0,
                LayerCount = 1
            },
            SrcAccessMask = 0,
            DstAccessMask = AccessFlags.TransferWriteBit
        };
        
        _vk.CmdPipelineBarrier(
            commandBuffer,
            PipelineStageFlags.TopOfPipeBit,
            PipelineStageFlags.TransferBit,
            0,
            0, null,
            0, null,
            1, &barrier
        );
        
        // Copy buffer to image
        var region = new BufferImageCopy
        {
            BufferOffset = 0,
            BufferRowLength = 0,
            BufferImageHeight = 0,
            ImageSubresource = new ImageSubresourceLayers
            {
                AspectMask = ImageAspectFlags.ColorBit,
                MipLevel = 0,
                BaseArrayLayer = 0,
                LayerCount = 1
            },
            ImageOffset = new Offset3D(0, 0, 0),
            ImageExtent = new Extent3D(_swapchainExtent.Width, _swapchainExtent.Height, 1)
        };
        
        _vk.CmdCopyBufferToImage(
            commandBuffer,
            stagingBuffer,
            destImage,
            ImageLayout.TransferDstOptimal,
            1,
            &region
        );
        
        // Transition to PRESENT_SRC_KHR
        barrier.OldLayout = ImageLayout.TransferDstOptimal;
        barrier.NewLayout = ImageLayout.PresentSrcKhr;
        barrier.SrcAccessMask = AccessFlags.TransferWriteBit;
        barrier.DstAccessMask = 0;
        
        _vk.CmdPipelineBarrier(
            commandBuffer,
            PipelineStageFlags.TransferBit,
            PipelineStageFlags.BottomOfPipeBit,
            0,
            0, null,
            0, null,
            1, &barrier
        );
        
        _vk.EndCommandBuffer(commandBuffer);
        
        // Submit the command buffer
        var submitInfo = new SubmitInfo
        {
            SType = StructureType.SubmitInfo,
            CommandBufferCount = 1,
            PCommandBuffers = &commandBuffer
        };
        
        _vk.QueueSubmit(_graphicsQueue, 1, in submitInfo, default);
        _vk.QueueWaitIdle(_graphicsQueue);
        
        // Cleanup
        _vk.FreeCommandBuffers(_device, _commandPool, 1, &commandBuffer);
        _vk.DestroyBuffer(_device, stagingBuffer, null);
        _vk.FreeMemory(_device, stagingBufferMemory, null);
    }

    private unsafe uint FindMemoryType(uint typeFilter, MemoryPropertyFlags properties)
    {
        PhysicalDeviceMemoryProperties memProperties;
        _vk!.GetPhysicalDeviceMemoryProperties(_physicalDevice, out memProperties);
        
        for (uint i = 0; i < memProperties.MemoryTypeCount; i++)
        {
            if ((typeFilter & (1 << (int)i)) != 0 &&
                (memProperties.MemoryTypes[(int)i].PropertyFlags & properties) == properties)
            {
                return i;
            }
        }
        
        throw new Exception("Failed to find suitable memory type");
    }

    public unsafe void Dispose()
    {
        if (_device.Handle != 0)
        {
            _vk!.DeviceWaitIdle(_device);
        }
        
        _skSurface?.Dispose();
        _grContext?.Dispose();
        
        if (_vk != null && _device.Handle != 0)
        {
            _vk.DestroySemaphore(_device, _imageAvailableSemaphore, null);
            _vk.DestroySemaphore(_device, _renderFinishedSemaphore, null);
            _vk.DestroyFence(_device, _inFlightFence, null);
            
            _vk.DestroyCommandPool(_device, _commandPool, null);
            
            CleanupSwapchain();
            
            _vk.DestroyDevice(_device, null);
        }
        
        if (_khrSurface != null && _windowSurface.Handle != 0)
        {
            _khrSurface.DestroySurface(_instance, _windowSurface, null);
        }
        
        if (_vk != null && _instance.Handle != 0)
        {
            _vk.DestroyInstance(_instance, null);
        }
        
        _khrSwapchain?.Dispose();
        _khrSurface?.Dispose();
        _vk?.Dispose();
    }
}
#endif
