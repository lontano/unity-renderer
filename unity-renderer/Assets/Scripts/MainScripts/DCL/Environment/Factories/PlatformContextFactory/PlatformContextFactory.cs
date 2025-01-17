using DCL.Rendering;
using UnityEngine;

namespace DCL
{
    public static class PlatformContextFactory
    {
        public static PlatformContext CreateDefault()
        {
            if (InitialSceneReferences.i != null)
                return CreateDefault(InitialSceneReferences.i.bridgeGameObject);

            return CreateDefault(null);
        }

        public static PlatformContext CreateDefault(GameObject bridgesGameObject)
        {
            return new PlatformContext(
                memoryManager: new MemoryManager(),
                cullingController: CullingController.Create(),
                clipboard: Clipboard.Create(),
                physicsSyncController: new PhysicsSyncController(),
                parcelScenesCleaner: new ParcelScenesCleaner(),
                webRequest: WebRequestController.Create(),
                serviceProviders: new ServiceProviders(),
                idleChecker: new IdleChecker(),
                avatarsLODController: new AvatarsLODController(),
                featureFlagController: new FeatureFlagController(bridgesGameObject));
        }
    }
}