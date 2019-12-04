using DCL;
using DCL.Helpers;
using System.Collections;
using UnityEngine;
using UnityEngine.TestTools;

namespace AssetPromiseKeeper_Tests
{
    public abstract class TestsBase_APK<APKType, AssetPromiseType, AssetType, AssetLibraryType> : TestsBase
        where AssetPromiseType : AssetPromise<AssetType>
        where AssetType : Asset, new()
        where AssetLibraryType : AssetLibrary<AssetType>, new()
        where APKType : AssetPromiseKeeper<AssetType, AssetLibraryType, AssetPromiseType>, new()
    {
        protected APKType keeper;

        [UnitySetUp]
        protected virtual IEnumerator SetUp()
        {
            MemoryManager.i.Initialize();
            keeper = new APKType();
            yield break;
        }

        [UnityTearDown]
        protected virtual IEnumerator TearDown()
        {
            PoolManager.i.Cleanup();
            keeper.Cleanup();
            Caching.ClearCache();
            Resources.UnloadUnusedAssets();
            yield break;
        }
    }
}
