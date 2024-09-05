//using Extensions;
//using System;
//using System.Linq;
//using XREngine.ComponentModel;
//using XREngine.Core.Files;
//using XREngine.Core.Files.Serialization;
//using XREngine.Core.Maths.Transforms;
//using XREngine.Rendering.Cameras;

//namespace XREngine.Animation
//{
//    [Serializable]
//    [TFileExt("pkc", ManualXmlConfigSerialize = true)]
//    [TFileDef("Perspective Camera Key Collection")]
//    public class PerspectiveCameraKeyCollection : TFileObject
//    {
//        public PerspectiveCameraKeyCollection() { }
        
//        public float LengthInSeconds { get; private set; }
//        public ERotationOrder EulerOrder { get; set; }
//        public bool VerticalFOV { get; set; }
//        public bool OverrideAspect { get; set; }

//        public PropAnimFloat TranslationX => _tracks[0];
//        public PropAnimFloat TranslationY => _tracks[1];
//        public PropAnimFloat TranslationZ => _tracks[2];

//        public PropAnimFloat RotationX => _tracks[3];
//        public PropAnimFloat RotationY => _tracks[4];
//        public PropAnimFloat RotationZ => _tracks[5];
        
//        public PropAnimFloat FOV => _tracks[6];
//        public PropAnimFloat Aspect => _tracks[7];
//        public PropAnimFloat NearZ => _tracks[8];
//        public PropAnimFloat FarZ => _tracks[9];

//        public PropAnimFloat this[int index]
//        {
//            get => _tracks.IndexInRange(index) ? _tracks[index] : null;
//            set
//            {
//                if (_tracks.IndexInRange(index))
//                    _tracks[index] = value;
//            }
//        }

//        private PropAnimFloat[] _tracks = new PropAnimFloat[]
//        {
//            new PropAnimFloat() { DefaultValue = 0.0f, TickSelf = false },  //tx
//            new PropAnimFloat() { DefaultValue = 0.0f, TickSelf = false },  //ty
//            new PropAnimFloat() { DefaultValue = 0.0f, TickSelf = false },  //tz

//            new PropAnimFloat() { DefaultValue = 0.0f, TickSelf = false },  //rx
//            new PropAnimFloat() { DefaultValue = 0.0f, TickSelf = false },  //ry
//            new PropAnimFloat() { DefaultValue = 0.0f, TickSelf = false },  //rz

//            new PropAnimFloat() { DefaultValue = 90.0f, TickSelf = false },  //fov
//            new PropAnimFloat() { DefaultValue = 9.0f / 16.0f, TickSelf = false },  //aspect
//            new PropAnimFloat() { DefaultValue = 0.1f, TickSelf = false },  //nearZ
//            new PropAnimFloat() { DefaultValue = 1000.0f, TickSelf = false },  //farZ
//        };

//        public void SetLength(float seconds, bool stretchAnimation, bool notifyChanged = true)
//        {
//            LengthInSeconds = seconds;
//            foreach (var track in _tracks)
//                track.SetLength(seconds, stretchAnimation, notifyChanged);
//        }

//        public void ResetProgress() => _tracks.ForEach(x => x.CurrentTime = 0.0f);
//        public void Progress(float delta) => _tracks.ForEach(x => x.Progress(delta));

//        public void SetCameraKeyframe(float second, PerspectiveCamera cameraReference)
//        {
//            var kf = new FloatKeyframe(second, cameraReference.Translation.X, 0.0f, EVectorInterpType.CubicHermite);
//            TranslationX.Keyframes.Add(kf);
//            kf.GenerateTangents();

//            kf = new FloatKeyframe(second, cameraReference.Translation.Y, 0.0f, EVectorInterpType.CubicHermite);
//            TranslationY.Keyframes.Add(kf);
//            kf.GenerateTangents();

//            kf = new FloatKeyframe(second, cameraReference.Translation.Z, 0.0f, EVectorInterpType.CubicHermite);
//            TranslationZ.Keyframes.Add(kf);
//            kf.GenerateTangents();

//            //kf = new FloatKeyframe(second, cameraReference.Rotation.Pitch, 0.0f, EVectorInterpType.CubicHermite);
//            //RotationX.Keyframes.Add(kf);
//            //kf.GenerateTangents();

//            //kf = new FloatKeyframe(second, cameraReference.Rotation.Yaw, 0.0f, EVectorInterpType.CubicHermite);
//            //RotationY.Keyframes.Add(kf);
//            //kf.GenerateTangents();

//            //kf = new FloatKeyframe(second, cameraReference.Rotation.Roll, 0.0f, EVectorInterpType.CubicHermite);
//            //RotationZ.Keyframes.Add(kf);
//            //kf.GenerateTangents();

//            kf = new FloatKeyframe(second, VerticalFOV ? cameraReference.VerticalFieldOfView : cameraReference.HorizontalFieldOfView, 0.0f, EVectorInterpType.CubicHermite);
//            FOV.Keyframes.Add(kf);
//            kf.GenerateTangents();

//            if (OverrideAspect)
//            {
//                kf = new FloatKeyframe(second, cameraReference.Aspect, 0.0f, EVectorInterpType.CubicHermite);
//                Aspect.Keyframes.Add(kf);
//                kf.GenerateTangents();
//            }

//            kf = new FloatKeyframe(second, cameraReference.NearZ, 0.0f, EVectorInterpType.CubicHermite);
//            NearZ.Keyframes.Add(kf);
//            kf.GenerateTangents();

//            kf = new FloatKeyframe(second, cameraReference.FarZ, 0.0f, EVectorInterpType.CubicHermite);
//            FarZ.Keyframes.Add(kf);
//            kf.GenerateTangents();
//        }
//        public unsafe void UpdateCamera(PerspectiveCamera camera, float second)
//        {
//            Vector3 t, r;
//            Vector4 param;
//            float* pt = (float*)&t;
//            float* pr = (float*)&r;
//            float* pp = (float*)&param;

//            for (int i = 0; i < 3; ++i)
//            {
//                var track = _tracks[i];
//                *pt++ = track.GetValue(second);
//            }
//            for (int i = 3; i < 6; ++i)
//            {
//                var track = _tracks[i];
//                *pr++ = track.GetValue(second);
//            }
//            for (int i = 6; i < 10; ++i)
//            {
//                var track = _tracks[i];
//                *pp++ = track.GetValue(second);
//            }

//            camera.SetAll(t, new Rotator(r, EulerOrder), param.X, VerticalFOV, param.Z, param.W, OverrideAspect ? (float?)param.Y : null);
//        }
//        public unsafe void UpdateCamera(PerspectiveCamera camera)
//        {
//            Vector3 t, r;
//            Vector4 param;
//            float* pt = (float*)&t;
//            float* pr = (float*)&r;
//            float* pp = (float*)&param;

//            for (int i = 0; i < 3; ++i)
//            {
//                var track = _tracks[i];
//                *pt++ = track.CurrentPosition;
//            }
//            for (int i = 3; i < 6; ++i)
//            {
//                var track = _tracks[i];
//                *pr++ = track.CurrentPosition;
//            }
//            for (int i = 6; i < 10; ++i)
//            {
//                var track = _tracks[i];
//                *pp++ = track.CurrentPosition;
//            }

//            camera.SetAll(t, new Rotator(r, EulerOrder), param.X, VerticalFOV, param.Z, param.W, OverrideAspect ? (float?)param.Y : null);
//        }
        
//        public float[] GetValues()
//        {
//            float[] values = new float[10];
//            for (int i = 0; i < 10; ++i)
//            {
//                var track = _tracks[i];
//                values[i] = track.CurrentPosition;
//            }
//            return values;
//        }
//        public float[] GetValues(float second)
//        {
//            float[] values = new float[10];
//            for (int i = 0; i < 10; ++i)
//            {
//                var track = _tracks[i];
//                values[i] = track.GetValue(second);
//            }
//            return values;
//        }

//        /// <summary>
//        /// Clears all keyframes and sets tracks to the proper length.
//        /// </summary>
//        public void ResetKeys()
//        {
//            foreach (var track in _tracks)
//            {
//                track.Keyframes.Clear();
//                track.SetLength(LengthInSeconds, false);
//            }
//        }
//        private static string[] TrackNames { get; } = new string[]
//        {
//            "TranslationX",
//            "TranslationY",
//            "TranslationZ",

//            "RotationX",
//            "RotationY",
//            "RotationZ",

//            "FOV",
//            "Aspect",
//            "NearZ",
//            "FarZ",
//        };
//        public override void ManualRead(SerializeElement node)
//        {
//            if (string.Equals(node.MemberInfo.Name, nameof(TransformKeyCollection), StringComparison.InvariantCulture))
//            {
//                if (node.GetAttributeValue(nameof(LengthInSeconds), out float length))
//                    LengthInSeconds = length;
//                else
//                    LengthInSeconds = 0.0f;

//                ResetKeys();

//                foreach (SerializeElement targetTrackElement in node.Children)
//                {
//                    int trackIndex = TrackNames.IndexOf(targetTrackElement.Name.ToString());
//                    if (!_tracks.IndexInRange(trackIndex))
//                        continue;
                        
//                    PropAnimFloat track = _tracks[trackIndex];
//                    if (targetTrackElement.GetAttributeValue("Count", out int keyCount))
//                    {
//                        float[] seconds = null, inValues = null, outValues = null, inTans = null, outTans = null;
//                        EVectorInterpType[] interpolation = null;

//                        foreach (SerializeElement keyframePartElement in targetTrackElement.Children)
//                        {
//                            if (keyframePartElement.MemberInfo.Name == "Interpolation" && 
//                                keyframePartElement.Content.GetObjectAs(out EVectorInterpType[] array1))
//                            {
//                                interpolation = array1;
//                            }
//                            else if (keyframePartElement.Content.GetObjectAs(out float[] array2))
//                            {
//                                switch (keyframePartElement.Name)
//                                {
//                                    case "Second": seconds = array2; break;
//                                    case "InValues": inValues = array2; break;
//                                    case "OutValues": outValues = array2; break;
//                                    case "InTangents": inTans = array2; break;
//                                    case "OutTangents": outTans = array2; break;
//                                }
//                            }
//                        }
//                        for (int i = 0; i < keyCount; ++i)
//                        {
//                            FloatKeyframe kf = new FloatKeyframe(
//                                seconds[i],
//                                inValues[i],
//                                outValues[i],
//                                inTans[i],
//                                outTans[i],
//                                interpolation[i]);
//                            track.Keyframes.Add(kf);
//                        }
//                    }
//                    _tracks[trackIndex] = track;
//                }
                
//            }
//            else
//            {
//                LengthInSeconds = 0;
//                ResetKeys();
//            }
//        }
//        public override void ManualWrite(SerializeElement node)
//        {
//            node.Name = nameof(TransformKeyCollection);
//            node.AddAttribute(nameof(LengthInSeconds), LengthInSeconds);
//            for (int i = 0; i < 9; ++i)
//            {
//                var track = _tracks[i];
//                if (track.Keyframes.Count > 0)
//                {
//                    SerializeElement trackElement = new SerializeElement(null, new TSerializeMemberInfo(null, TrackNames[i]));
//                    trackElement.AddAttribute("Count", track.Keyframes.Count);
//                    trackElement.AddChildElementObject("Second", track.Select(x => x.Second).ToArray());
//                    trackElement.AddChildElementObject("InValues", track.Select(x => x.InValue).ToArray());
//                    trackElement.AddChildElementObject("OutValues", track.Select(x => x.OutValue).ToArray());
//                    trackElement.AddChildElementObject("InTangents", track.Select(x => x.InTangent).ToArray());
//                    trackElement.AddChildElementObject("OutTangents", track.Select(x => x.OutTangent).ToArray());
//                    trackElement.AddChildElementObject("Interpolation", track.Select(x => x.InterpolationType).ToArray());
//                    node.Children.Add(trackElement);
//                }
//            }
//        }
//    }
//}
