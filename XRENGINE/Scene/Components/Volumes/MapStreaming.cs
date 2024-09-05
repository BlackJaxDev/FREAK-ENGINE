//using XREngine.ComponentModel;
//using XREngine.Core.Files;
//using XREngine.Core.Maths.Transforms;
//using XREngine.Physics;
//using XREngine.Worlds;

//namespace XREngine.Components.Scene.Volumes
//{
//    public class MapStreamingVolumeComponent : TriggerVolumeComponent
//    {
//        public MapStreamingVolumeComponent() : this(Vector3.Zero) { }
//        public MapStreamingVolumeComponent(Vector3 halfExtents)
//            : base(halfExtents) { }

//        [TSerialize]
//        public GlobalFileRef<Map> MapToLoad { get; set; }

//        protected override async void OnEntered(XRCollisionObject obj)
//        {
//            base.OnEntered(obj);

//            //TODO: check if obj is linked to pawn, and if player is allowed to trigger map streaming
//            var map = await MapToLoad.GetInstanceAsync();
//            OwningWorld.SpawnMap(map);
//        }
//        protected override void OnLeft(XRCollisionObject obj)
//        {
//            base.OnLeft(obj);

//            if (MapToLoad.IsLoaded)
//                OwningWorld.DespawnMap(MapToLoad.File);
//        }
//    }
//}
