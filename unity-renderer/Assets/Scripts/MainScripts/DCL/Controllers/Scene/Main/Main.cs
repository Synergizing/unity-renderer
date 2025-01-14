using DCL.Components;
using DCL.Configuration;
using DCL.Helpers;
using DCL.Interface;
using DCL.SettingsCommon;
using DCL.Social.Chat;
using DCl.Social.Friends;
using DCL.Social.Friends;
using UnityEngine;

namespace DCL
{
    /// <summary>
    /// This is the InitialScene entry point.
    /// Most of the application subsystems should be initialized from this class Awake() event.
    /// </summary>
    public class Main : MonoBehaviour
    {
        private readonly DataStoreRef<DataStore_LoadingScreen> dataStoreLoadingScreen;
        [SerializeField] private bool disableSceneDependencies;
        public static Main i { get; private set; }

        public PoolableComponentFactory componentFactory;

        private PerformanceMetricsController performanceMetricsController;
        protected IKernelCommunication kernelCommunication;

        protected PluginSystem pluginSystem;

        protected virtual void Awake()
        {
            if (i != null)
            {
                Utils.SafeDestroy(this);
                return;
            }

            i = this;

            if (!disableSceneDependencies)
                InitializeSceneDependencies();

            Settings.CreateSharedInstance(new DefaultSettingsFactory());

            // TODO: migrate chat controller singleton into a service in the service locator
            ChatController.CreateSharedInstance(GetComponent<WebInterfaceChatBridge>(), DataStore.i);

            if (!EnvironmentSettings.RUNNING_TESTS)
            {
                performanceMetricsController = new PerformanceMetricsController();
                SetupServices();

                dataStoreLoadingScreen.Ref.loadingHUD.visible.OnChange += OnLoadingScreenVisibleStateChange;
                dataStoreLoadingScreen.Ref.decoupledLoadingHUD.visible.OnChange += OnLoadingScreenVisibleStateChange;
            }

            // TODO (NEW FRIEND REQUESTS): remove when the kernel bridge is production ready
            WebInterfaceFriendsApiBridge webInterfaceFriendsApiBridge = GetComponent<WebInterfaceFriendsApiBridge>();

            FriendsController.CreateSharedInstance(new WebInterfaceFriendsApiBridgeProxy(
                webInterfaceFriendsApiBridge,
                RPCFriendsApiBridge.CreateSharedInstance(Environment.i.serviceLocator.Get<IRPC>(), webInterfaceFriendsApiBridge),
                DataStore.i));

#if UNITY_STANDALONE || UNITY_EDITOR
            Application.quitting += () => DataStore.i.common.isApplicationQuitting.Set(true);
#endif

            InitializeDataStore();
            SetupPlugins();
            InitializeCommunication();
        }

        protected virtual void InitializeDataStore()
        {
            DataStore.i.textureConfig.gltfMaxSize.Set(TextureCompressionSettings.GLTF_TEX_MAX_SIZE_WEB);
            DataStore.i.textureConfig.generalMaxSize.Set(TextureCompressionSettings.GENERAL_TEX_MAX_SIZE_WEB);
            DataStore.i.avatarConfig.useHologramAvatar.Set(true);
        }

        protected virtual void InitializeCommunication()
        {
#if UNITY_WEBGL && !UNITY_EDITOR
            Debug.Log("DCL Unity Build Version: " + DCL.Configuration.ApplicationSettings.version);

            kernelCommunication = new NativeBridgeCommunication(Environment.i.world.sceneController);
#else

            // TODO(Brian): Remove this branching once we finish migrating all tests out of the
            //              IntegrationTestSuite_Legacy base class.
            if (!EnvironmentSettings.RUNNING_TESTS) { kernelCommunication = new WebSocketCommunication(DebugConfigComponent.i.webSocketSSL); }
#endif
        }

        void OnLoadingScreenVisibleStateChange(bool newVisibleValue, bool previousVisibleValue)
        {
            if (newVisibleValue)
            {
                // Prewarm shader variants
                Resources.Load<ShaderVariantCollection>("ShaderVariantCollections/shaderVariants-selected").WarmUp();
                dataStoreLoadingScreen.Ref.loadingHUD.visible.OnChange -= OnLoadingScreenVisibleStateChange;
                dataStoreLoadingScreen.Ref.decoupledLoadingHUD.visible.OnChange -= OnLoadingScreenVisibleStateChange;
            }
        }

        protected virtual void SetupPlugins()
        {
            pluginSystem = PluginSystemFactory.Create();
            pluginSystem.Initialize();
        }

        protected virtual void SetupServices()
        {
            Environment.Setup(ServiceLocatorFactory.CreateDefault());
        }

        protected virtual void Start()
        {
            // this event should be the last one to be executed after initialization
            // it is used by the kernel to signal "EngineReady" or something like that
            // to prevent race conditions like "SceneController is not an object",
            // aka sending events before unity is ready
            WebInterface.SendSystemInfoReport();

            // We trigger the Decentraland logic once everything is initialized.
            WebInterface.StartDecentraland();
        }

        protected virtual void Update()
        {
            performanceMetricsController?.Update();
        }

        [RuntimeInitializeOnLoadMethod]
        static void RunOnStart()
        {
            Application.wantsToQuit += ApplicationWantsToQuit;
        }

        private static bool ApplicationWantsToQuit()
        {
            if (i != null)
                i.Dispose();

            return true;
        }

        protected virtual void Dispose()
        {
            dataStoreLoadingScreen.Ref.loadingHUD.visible.OnChange -= OnLoadingScreenVisibleStateChange;
            dataStoreLoadingScreen.Ref.decoupledLoadingHUD.visible.OnChange -= OnLoadingScreenVisibleStateChange;

            DataStore.i.common.isApplicationQuitting.Set(true);
            Settings.i.SaveSettings();

            pluginSystem?.Dispose();

            if (!EnvironmentSettings.RUNNING_TESTS)
                Environment.Dispose();

            kernelCommunication?.Dispose();
        }

        protected virtual void InitializeSceneDependencies()
        {
            gameObject.AddComponent<UserProfileController>();
            gameObject.AddComponent<RenderingController>();
            gameObject.AddComponent<CatalogController>();
            gameObject.AddComponent<MinimapMetadataController>();
            gameObject.AddComponent<WebInterfaceChatBridge>();
            gameObject.AddComponent<WebInterfaceFriendsApiBridge>();
            gameObject.AddComponent<HotScenesController>();
            gameObject.AddComponent<GIFProcessingBridge>();
            gameObject.AddComponent<RenderProfileBridge>();
            gameObject.AddComponent<AssetCatalogBridge>();
            gameObject.AddComponent<ScreenSizeWatcher>();
            gameObject.AddComponent<SceneControllerBridge>();

            MainSceneFactory.CreateBridges();
            MainSceneFactory.CreateMouseCatcher();
            MainSceneFactory.CreatePlayerSystems();
            CreateEnvironment();
            MainSceneFactory.CreateAudioHandler();
            MainSceneFactory.CreateHudController();
            MainSceneFactory.CreateNavMap();
            MainSceneFactory.CreateEventSystem();
        }

        protected virtual void CreateEnvironment() =>
            MainSceneFactory.CreateEnvironment();
    }
}
