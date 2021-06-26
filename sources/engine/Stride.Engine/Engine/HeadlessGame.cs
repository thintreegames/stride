// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using System;
using System.Reflection;
using System.Threading.Tasks;
using Stride.Core;
using Stride.Core.Annotations;
using Stride.Core.Diagnostics;
using Stride.Core.IO;
using Stride.Core.Serialization.Contents;
using Stride.Core.Storage;
using Stride.Engine.Design;
using Stride.Engine.Processors;
using Stride.Games;
using Stride.Games.Time;
using Stride.Graphics;
using Stride.Streaming;

namespace Stride.Engine
{
    /// <summary>
    /// Main Game class system.
    /// </summary>
    public class HeadlessGame : ComponentBase, IHeadlessGame
    {
        private bool isEndRunRequired;

        private readonly TimeSpan maximumElapsedTime;
        private TimeSpan accumulatedElapsedGameTime;
        private bool forceElapsedTimeToZero;

        private readonly TimerTick autoTickTimer;
        protected readonly ILogger Log;

        internal object TickLock = new object();

        #region Unused

        /// <summary>
        /// Gets or sets a value indicating whether draw can happen as fast as possible, even when <see cref="IsFixedTimeStep"/> is set.
        /// </summary>
        /// <value><c>true</c> if this instance allows desychronized drawing; otherwise, <c>false</c>.</value>
        public bool IsDrawDesynchronized { get; set; } = false;

        /// <summary>
        /// Gets or sets a value indicating whether the mouse should be visible.
        /// </summary>
        /// <value><c>true</c> if the mouse should be visible; otherwise, <c>false</c>.</value>
        public bool IsMouseVisible { get; set; } = false;

        /// <summary>
        /// Is used when we draw without running an update beforehand, so when both <see cref="IsFixedTimeStep"/> 
        /// and <see cref="IsDrawDesynchronized"/> are set.<br/>
        /// It returns a number between 0 and 1 which represents the current position our DrawTime is in relation 
        /// to the previous and next step.<br/>
        /// 0.5 would mean that we are rendering at a time halfway between the previous and next fixed-step.
        /// </summary>
        /// <value>
        /// The draw interpolation factor.
        /// </value>
        public float DrawInterpolationFactor => default;

        /// <summary>
        /// Gets the graphics device.
        /// </summary>
        /// <value>The graphics device.</value>
        public GraphicsDevice GraphicsDevice { get; private set; }

        public GraphicsContext GraphicsContext { get; private set; }

        /// <summary>
        /// Gets the game context.
        /// </summary>
        /// <value>The game context.</value>
        public GameContext Context { get; private set; }

        #endregion

        #region Public Properties



        /// <summary>
        /// The total and delta time to be used for logic running in the update loop.
        /// </summary>
        public GameTime UpdateTime { get; }

        /// <summary>
        /// Gets the current draw time.
        /// </summary>
        /// <value>The current draw time.</value>
        public GameTime DrawTime => default;

        /// <summary>
        /// Gets the <see cref="ContentManager"/>.
        /// </summary>
        public ContentManager Content { get; private set; }

        /// <summary>
        /// Gets the game components registered by this game.
        /// </summary>
        /// <value>The game components.</value>
        public GameSystemCollection GameSystems { get; private set; }

        /// <summary>
        /// Gets or sets the time between each <see cref="Tick"/> when <see cref="IsActive"/> is false.
        /// </summary>
        /// <value>The inactive sleep time.</value>
        public TimeSpan InactiveSleepTime { get; set; }

        /// <summary>
        /// Gets a value indicating whether this instance is active.
        /// </summary>
        /// <value><c>true</c> if this instance is active; otherwise, <c>false</c>.</value>
        public bool IsActive { get; private set; }

        /// <summary>
        /// Gets a value indicating whether this instance is exiting.
        /// </summary>
        /// <value><c>true</c> if this instance is exiting; otherwise, <c>false</c>.</value>
        public bool IsExiting { get; private set; }

        /// <summary>
        /// Gets or sets a value indicating whether the elapsed time between each update should be constant,
        /// see <see cref="TargetElapsedTime"/> to configure the duration.
        /// </summary>
        /// <value><c>true</c> if this instance is fixed time step; otherwise, <c>false</c>.</value>
        public bool IsFixedTimeStep { get; set; }


        /// <summary>
        /// Gets the launch parameters.
        /// </summary>
        /// <value>The launch parameters.</value>
        public LaunchParameters LaunchParameters { get; private set; }

        /// <summary>
        /// Gets a value indicating whether this instance is running.
        /// </summary>
        public bool IsRunning { get; private set; }

        /// <summary>
        /// Gets the service container.
        /// </summary>
        /// <value>The service container.</value>
        [NotNull]
        public ServiceRegistry Services { get; }

        /// <summary>
        /// Gets or sets the target elapsed time, this is the duration of each tick/update
        /// when <see cref="IsFixedTimeStep"/> is enabled.
        /// </summary>
        /// <value>The target elapsed time.</value>
        public TimeSpan TargetElapsedTime { get; set; }

        /// <summary>
        /// Gets the abstract window.
        /// </summary>
        /// <value>The window.</value>
        public GameWindow Window => null;

        #endregion

        #region Public Events

        /// <summary>
        /// Occurs when [activated].
        /// </summary>
        public event EventHandler<EventArgs> Activated;

        /// <summary>
        /// Occurs when [deactivated].
        /// </summary>
        public event EventHandler<EventArgs> Deactivated;

        /// <summary>
        /// Occurs when [exiting].
        /// </summary>
        public event EventHandler<EventArgs> Exiting;

        /// <summary>
        /// Occurs when [window created].
        /// </summary>
        public event EventHandler<EventArgs> WindowCreated;

        #endregion


        /// <summary>
        /// Static event that will be fired when a game is initialized
        /// </summary>
        public static event EventHandler GameStarted;

        /// <summary>
        /// Static event that will be fired when a game is destroyed
        /// </summary>
        public static event EventHandler GameDestroyed;

        private readonly LogListener logListener;

        private DatabaseFileProvider databaseFileProvider;

        /// <summary>
        /// Gets the script system.
        /// </summary>
        /// <value>The script.</value>
        public ScriptSystem Script { get; }

        /// <summary>
        /// Gets the scene system.
        /// </summary>
        /// <value>The scene system.</value>
        public SceneSystem SceneSystem { get; }

        /// <summary>
        /// Gets the streaming system.
        /// </summary>
        /// <value>The streaming system.</value>
        public StreamingManager Streaming { get; }

        /// <summary>
        /// Gets or sets the console log mode. See remarks.
        /// </summary>
        /// <value>The console log mode.</value>
        /// <remarks>
        /// Defines how the console will be displayed when running the game. By default, on Windows, It will open only on debug
        /// if there are any messages logged.
        /// </remarks>
        public ConsoleLogMode ConsoleLogMode
        {
            get
            {
                var consoleLogListener = logListener as ConsoleLogListener;
                return consoleLogListener != null ? consoleLogListener.LogMode : default(ConsoleLogMode);
            }
            set
            {
                var consoleLogListener = logListener as ConsoleLogListener;
                if (consoleLogListener != null)
                {
                    consoleLogListener.LogMode = value;
                }
            }
        }

        /// <summary>
        /// Gets or sets the default console log level.
        /// </summary>
        /// <value>The console log level.</value>
        public LogMessageType ConsoleLogLevel
        {
            get
            {
                var consoleLogListener = logListener as ConsoleLogListener;
                return consoleLogListener != null ? consoleLogListener.LogLevel : default(LogMessageType);
            }
            set
            {
                var consoleLogListener = logListener as ConsoleLogListener;
                if (consoleLogListener != null)
                {
                    consoleLogListener.LogLevel = value;
                }
            }
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="Game"/> class.
        /// </summary>
        public HeadlessGame()
        {
            // Internals
            Log = GlobalLogger.GetLogger(GetType().GetTypeInfo().Name);
            UpdateTime = new GameTime();
            autoTickTimer = new TimerTick();
            IsFixedTimeStep = true;
            maximumElapsedTime = TimeSpan.FromMilliseconds(500.0);
            TargetElapsedTime = TimeSpan.FromTicks(TimeSpan.TicksPerSecond / 60); // target elapsed time is by default 60Hz

            // Externals
            Services = new ServiceRegistry();

            GraphicsDevice = GraphicsDevice.New(DeviceCreationFlags.VideoSupport, new GraphicsProfile[]
            {
                GraphicsProfile.Level_11_0,
                GraphicsProfile.Level_11_1,
                GraphicsProfile.Level_11_2
            });
            Services.AddService<IGraphicsDeviceService>(new GraphicsDeviceServiceLocal(GraphicsDevice));

            // Database file provider
            Services.AddService<IDatabaseFileProviderService>(new DatabaseFileProviderService(null));

            GameSystems = new GameSystemCollection(Services);
            Services.AddService<IGameSystemCollection>(GameSystems);

            // Setup registry
            Services.AddService<IGame>(this);

            // Register the logger backend before anything else
            logListener = GetLogListener();

            if (logListener != null)
                GlobalLogger.GlobalMessageLogged += logListener;

            // Create all core services, except Input which is created during `Initialize'.
            // Registration takes place in `Initialize'.
            Script = new ScriptSystem(Services);
            Services.AddService(Script);

            SceneSystem = new SceneSystem(Services);
            Services.AddService(SceneSystem);

            Streaming = new StreamingManager(Services);
        }

        /// <inheritdoc/>
        protected override void Destroy()
        {
            OnGameDestroyed(this);

            DestroyAssetDatabase();

            base.Destroy();

            if (logListener != null)
                GlobalLogger.GlobalMessageLogged -= logListener;
        }

        /// <inheritdoc/>
        protected void PrepareContext()
        {
            Content = new ContentManager(Services);
            Services.AddService<IContentManager>(Content);
            Services.AddService(Content);

            // Init assets
            databaseFileProvider = InitializeAssetDatabase();
            ((DatabaseFileProviderService)Services.GetService<IDatabaseFileProviderService>()).FileProvider = databaseFileProvider;
        }

        internal static DatabaseFileProvider InitializeAssetDatabase()
        {
            // Create and mount database file system
            var objDatabase = ObjectDatabase.CreateDefaultDatabase();

            // Only set a mount path if not mounted already
            var mountPath = VirtualFileSystem.ResolveProviderUnsafe("/asset", true).Provider == null ? "/asset" : null;
            var result = new DatabaseFileProvider(objDatabase, mountPath);

            return result;
        }

        private static void OnGameStarted(HeadlessGame game) => GameStarted?.Invoke(game, null);
        private static void OnGameDestroyed(HeadlessGame game) => GameDestroyed?.Invoke(game, null);



        /// <summary>
        /// Loads the content.
        /// </summary>
        protected virtual Task LoadContent()
        {
            return Task.FromResult(true);
        }

        public void Run(string strScene)
        {
            if (IsRunning)
            {
                throw new InvalidOperationException("Cannot run this instance while it is already running");
            }

            PrepareContext();

            InitializeBeforeRun();

            var scene = Content.Load<Scene>(strScene);
            SceneSystem.SceneInstance = new SceneInstance(Services, scene);
        }

        public void Tick()
        {
            lock (TickLock)
            {
                // If this instance is existing, then don't make any further update/draw
                if (IsExiting)
                {
                    CheckEndRun();
                    return;
                }

                // If this instance is not active, sleep for an inactive sleep time
                if (!IsActive)
                {
                    Utilities.Sleep(InactiveSleepTime);
                    return;
                }

                RawTickProducer();
            }
        }

        public void Exit()
        {
            Destroy();
        }

        protected virtual void RawTickProducer()
        {
            try
            {
                // Update the timer
                autoTickTimer.Tick();

                var elapsedAdjustedTime = autoTickTimer.ElapsedTimeWithPause;

                if (forceElapsedTimeToZero)
                {
                    elapsedAdjustedTime = TimeSpan.Zero;
                    forceElapsedTimeToZero = false;
                }

                if (elapsedAdjustedTime > maximumElapsedTime)
                {
                    elapsedAdjustedTime = maximumElapsedTime;
                }

                int updateCount = 1;
                var singleFrameElapsedTime = elapsedAdjustedTime;

                if (IsFixedTimeStep)
                {
                    // If the rounded TargetElapsedTime is equivalent to current ElapsedAdjustedTime
                    // then make ElapsedAdjustedTime = TargetElapsedTime. We take the same internal rules as XNA
                    if (Math.Abs(elapsedAdjustedTime.Ticks - TargetElapsedTime.Ticks) < (TargetElapsedTime.Ticks >> 6))
                    {
                        elapsedAdjustedTime = TargetElapsedTime;
                    }

                    // Update the accumulated time
                    accumulatedElapsedGameTime += elapsedAdjustedTime;

                    // Calculate the number of update to issue
                    updateCount = (int)(accumulatedElapsedGameTime.Ticks / TargetElapsedTime.Ticks);

                    // We are going to call Update updateCount times, so we can subtract this from accumulated elapsed game time
                    accumulatedElapsedGameTime = new TimeSpan(accumulatedElapsedGameTime.Ticks - (updateCount * TargetElapsedTime.Ticks));
                    singleFrameElapsedTime = TargetElapsedTime;
                }

                RawTick(singleFrameElapsedTime, updateCount);
            }
            catch (Exception ex)
            {
                Log.Error("Unexpected exception", ex);
                throw;
            }
        }

        protected void RawTick(TimeSpan elapsedTimePerUpdate, int updateCount = 1)
        {
            TimeSpan totalElapsedTime = TimeSpan.Zero;

            // Reset the time of the next frame
            for (int i = 0; i < updateCount && !IsExiting; i++)
            {
                UpdateTime.Update(UpdateTime.Total + elapsedTimePerUpdate, elapsedTimePerUpdate, true);
                Update(UpdateTime);
                totalElapsedTime += elapsedTimePerUpdate;
            }
        }

        protected virtual LogListener GetLogListener() => new ConsoleLogListener();

        internal void InitializeBeforeRun()
        {
            try
            {
                // Initialize this instance and all game systems before trying to create the device.
                Initialize();

                IsRunning = true;
                IsActive = true;

                autoTickTimer.Reset();
                UpdateTime.Reset(UpdateTime.Total);

                // Run the first time an update
                Update(UpdateTime);
            }
            catch (Exception ex)
            {
                Log.Error("Unexpected exception", ex);
                throw;
            }
        }

        protected void Initialize()
        {
            // ---------------------------------------------------------
            // Add common GameSystems - Adding order is important
            // (Unless overriden by gameSystem.UpdateOrder)
            // ---------------------------------------------------------

            // Initialize the systems
            GameSystems.Initialize();

            Content.Serializer.LowLevelSerializerSelector = ParameterContainerExtensions.DefaultSceneSerializerSelector;

            // Add the scheduler system
            // - Must be after Input, so that scripts are able to get latest input
            // - Must be before Entities/Camera/Audio/UI, so that scripts can apply
            // changes in the same frame they will be applied
            GameSystems.Add(Script);

            GameSystems.Add(Streaming);
            GameSystems.Add(SceneSystem);

            // TODO: data-driven?
            Content.Serializer.RegisterSerializer(new ImageSerializer());

            OnGameStarted(this);
        }

        protected void Update(GameTime gameTime)
        {
            GameSystems.Update(gameTime);
        }

        private void DestroyAssetDatabase()
        {
            if (databaseFileProvider != null)
            {
                if (Services.GetService<IDatabaseFileProviderService>() is DatabaseFileProviderService dbfp)
                    dbfp.FileProvider = null;
                databaseFileProvider.Dispose();
                databaseFileProvider = null;
            }
        }

        private void CheckEndRun()
        {
            if (IsExiting && IsRunning && isEndRunRequired)
            {
                IsRunning = false;
            }
        }
    }
}
