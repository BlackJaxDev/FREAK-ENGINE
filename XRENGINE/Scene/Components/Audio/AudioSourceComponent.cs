namespace XREngine.Components.Scene
{
    public class AudioSourceComponent : XRComponent//, IAudioSource
    {
//        private LocalFileRef<AudioParameters> _parametersRef;

//        [Category("State")]
//        public EventList<AudioInstance> Instances { get; set; }

//        public bool PlayOnSpawn { get; set; }
//        public int Priority { get; set; } = 0;

//        public GlobalFileRef<AudioFile> AudioFileRef { get; set; }
//        public LocalFileRef<AudioParameters> ParametersRef
//        {
//            get => _parametersRef;
//            set
//            {
//                if (_parametersRef != null)
//                {
//                    _parametersRef.Loaded -= ParametersRef_Loaded;
//                }
//                _parametersRef = value;
//                if (_parametersRef != null)
//                {
//                    _parametersRef.Loaded += ParametersRef_Loaded;
//                }
//            }
//        }

//        private void ParametersRef_Loaded(AudioParameters parameters)
//            => UpdateTransform(parameters);

//        protected override async void OnSpawned()
//        {
//            base.OnSpawned();
//            bool play = PlayOnSpawn;
//#if EDITOR
//            if (Engine.EditorState.InEditMode)
//                play = false;
//#endif
//            if (play)
//                await PlayAsync();
//        }

//        AudioFile IAudioSource.Audio => AudioFileRef?.File;
//        AudioParameters IAudioSource.Parameters => ParametersRef?.File;

//        /// <summary>
//        /// Plays the sound.
//        /// </summary>
//        public async Task PlayAsync()
//        {
//            AudioFile file = await AudioFileRef?.GetInstanceAsync();
//            if (file is null)
//                return;

//            var instance = Engine.Audio.CreateNewInstance(this);
//            Instances.Add(instance);
//            Engine.Audio.Play(instance);
//        }

//        protected override void OnWorldTransformChanged(bool recalcChildWorldTransformsNow = true)
//        {
//            base.OnWorldTransformChanged(recalcChildWorldTransformsNow);
//            UpdateTransform(ParametersRef?.File);
//        }

//        private void UpdateTransform(AudioParameters parameters)
//        {
//            if (parameters?.Position != null)
//                parameters.Position.OverrideValue = WorldPoint;

//            if (parameters?.Direction != null)
//                parameters.Direction.OverrideValue = WorldForwardVec;

//            if (parameters?.Velocity != null)
//                parameters.Velocity.OverrideValue = Velocity;
//        }
    }
}
