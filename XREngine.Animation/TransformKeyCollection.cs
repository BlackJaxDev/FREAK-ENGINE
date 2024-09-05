//using Extensions;
//using System;
//using System.Collections;
//using System.Collections.Generic;
//using System.Linq;
//using XREngine.ComponentModel;
//using XREngine.Core.Files;
//using XREngine.Core.Files.Serialization;
//using XREngine.Core.Maths.Transforms;

//namespace XREngine.Animation
//{
//    [TFileExt("tkc", ManualXmlConfigSerialize = true)]
//    [TFileDef("Transform Key Collection")]
//    public class TransformKeyCollection : TFileObject
//    {
//        public TransformKeyCollection() { }

//        public float LengthInSeconds { get; private set; }
//        public ETransformOrder TransformOrder { get; set; } = ETransformOrder.TRS;
//        / <summary>
//        / If true, translation value overwrites the bind state translation.
//        / If false, translation value is added to the bind state translation.
//        / </summary>
//        public bool AbsoluteTranslation { get; set; } = false;
//        / <summary>
//        / If true, rotation value is relative to the world.
//        / If false, rotation value is relative to the bind state rotation.
//        / </summary>
//        public bool AbsoluteRotation { get; set; } = false;
        
//        public PropAnimFloat TranslationX { get; } = new PropAnimFloat() { DefaultValue = 0.0f, TickSelf = false };
//        public PropAnimFloat TranslationY { get; } = new PropAnimFloat() { DefaultValue = 0.0f, TickSelf = false };
//        public PropAnimFloat TranslationZ { get; } = new PropAnimFloat() { DefaultValue = 0.0f, TickSelf = false };
//        public PropAnimFloat ScaleX { get; } = new PropAnimFloat() { DefaultValue = 0.0f, TickSelf = false };
//        public PropAnimFloat ScaleY { get; } = new PropAnimFloat() { DefaultValue = 0.0f, TickSelf = false };
//        public PropAnimFloat ScaleZ { get; } = new PropAnimFloat() { DefaultValue = 0.0f, TickSelf = false };
//        public PropAnimQuat Rotation { get; } = new PropAnimQuat() { DefaultValue = Quat.Identity, TickSelf = false };

//        public void SetLength(float seconds, bool stretchAnimation, bool notifyChanged = true)
//        {
//            LengthInSeconds = seconds;
//            TranslationX.SetLength(seconds, stretchAnimation, notifyChanged);
//            TranslationY.SetLength(seconds, stretchAnimation, notifyChanged);
//            TranslationZ.SetLength(seconds, stretchAnimation, notifyChanged);
//            ScaleX.SetLength(seconds, stretchAnimation, notifyChanged);
//            ScaleY.SetLength(seconds, stretchAnimation, notifyChanged);
//            ScaleZ.SetLength(seconds, stretchAnimation, notifyChanged);
//            Rotation.SetLength(seconds, stretchAnimation, notifyChanged);
//        }

//        public void Progress(float delta)
//        {
//            TranslationX.Progress(delta);
//            TranslationY.Progress(delta);
//            TranslationZ.Progress(delta);
//            ScaleX.Progress(delta);
//            ScaleY.Progress(delta);
//            ScaleZ.Progress(delta);
//            Rotation.Progress(delta);
//        }

//        private static unsafe void GetTrackValue(float* animPtr, float* bindPtr, PropAnimFloat track, int index)
//            => animPtr[index] = (track?.Keyframes?.Count ?? 0) == 0 ? bindPtr[index] : track.CurrentPosition;
//        private static unsafe void GetTrackValue(float* animPtr, float* bindPtr, PropAnimFloat track, int index, float time)
//            => animPtr[index] = (track?.Keyframes?.Count ?? 0) == 0 ? bindPtr[index] : track.GetValue(time);

//        / <summary>
//        / Retrieves the parts of the transform at the requested frame second.
//        / Uses the defaultTransform for tracks that have no keys.
//        / </summary>
//        public unsafe void GetTransform(ITransform bindState,
//            out Vector3 translation, out Quat rotation, out Vector3 scale)
//        {
//            Vector3 t, s;
//            Vector3 bt = bindState.Translation.Value;
//            Vector3 bs = bindState.Scale.Value;

//            float* pt = (float*)&t;
//            float* ps = (float*)&s;
//            float* pbt = (float*)&bt;
//            float* pbs = (float*)&bs;

//            GetTrackValue(pt, pbt, TranslationX, 0);
//            GetTrackValue(pt, pbt, TranslationY, 1);
//            GetTrackValue(pt, pbt, TranslationZ, 2);

//            GetTrackValue(ps, pbs, ScaleX, 0);
//            GetTrackValue(ps, pbs, ScaleY, 1);
//            GetTrackValue(ps, pbs, ScaleZ, 2);

//            rotation = Rotation.GetValue(Rotation.CurrentTime);
//            if (!AbsoluteRotation)
//                rotation = bindState.Rotation * rotation;

//            translation = t;
//            scale = s;
//        }
//        public unsafe void GetTransform(ITransform bindState,
//            out Vector3 translation, out Quat rotation, out Vector3 scale, float second)
//        {
//            Vector3 t, s;
//            Vector3 bt = bindState.Translation.Value;
//            Vector3 bs = bindState.Scale.Value;

//            float* pt = (float*)&t;
//            float* ps = (float*)&s;
//            float* pbt = (float*)&bt;
//            float* pbs = (float*)&bs;

//            GetTrackValue(pt, pbt, TranslationX, 0, second);
//            GetTrackValue(pt, pbt, TranslationY, 1, second);
//            GetTrackValue(pt, pbt, TranslationZ, 2, second);

//            GetTrackValue(ps, pbs, ScaleX, 0, second);
//            GetTrackValue(ps, pbs, ScaleY, 1, second);
//            GetTrackValue(ps, pbs, ScaleZ, 2, second);

//            rotation = Rotation.GetValue(second);
//            if (!AbsoluteRotation)
//                rotation = bindState.Rotation * rotation;

//            translation = t;
//            scale = s;
//        }
//        / <summary>
//        / Retrieves the parts of the transform at the current frame second.
//        / Uses the defaultTransform for tracks that have no keys.
//        / </summary>
//        public unsafe void GetTransform(out Vector3 translation, out Quat rotation, out Vector3 scale)
//        {
//            translation = new Vector3(
//                TranslationX.CurrentPosition,
//                TranslationY.CurrentPosition,
//                TranslationZ.CurrentPosition);

//            TODO: implement current position property
//            rotation = Rotation.GetValue(Rotation.CurrentTime);

//            scale = new Vector3(
//                ScaleX.CurrentPosition,
//                ScaleY.CurrentPosition,
//                ScaleZ.CurrentPosition);
//        }
//        / <summary>
//        / Retrieves the parts of the transform at the requested frame second.
//        / Uses the defaultTransform for tracks that have no keys.
//        / </summary>
//        public unsafe void GetTransform(out Vector3 translation, out Quat rotation, out Vector3 scale, float second)
//        {
//            translation = new Vector3(
//                TranslationX.GetValue(second),
//                TranslationY.GetValue(second),
//                TranslationZ.GetValue(second));

//            TODO: implement current position property
//            rotation = Rotation.GetValue(second);

//            scale = new Vector3(
//                ScaleX.GetValue(second),
//                ScaleY.GetValue(second),
//                ScaleZ.GetValue(second));
//        }
//        / <summary>
//        / Retrieves the transform at the requested frame second.
//        / Uses the defaultTransform for tracks that have no keys.
//        / </summary>
//        public unsafe ITransform GetTransform(ITransform bindState)
//        {
//            GetTransform(bindState, out Vector3 t, out Quat r, out Vector3 s);
//            return new TTransform(t, r, s, TransformOrder);
//        }
//        / <summary>
//        / Retrieves the transform at the requested frame second.
//        / Uses the defaultTransform for tracks that have no keys.
//        / </summary>
//        public unsafe ITransform GetTransform(ITransform bindState, float second)
//        {
//            GetTransform(bindState, out Vector3 t, out Quat r, out Vector3 s, second);
//            return new TTransform(t, r, s, TransformOrder);
//        }
//        / <summary>
//        / Retrieves the transform at the current frame second.
//        / </summary>
//        public unsafe ITransform GetTransform()
//        {
//            GetTransform(out Vector3 t, out Quat r, out Vector3 s);
//            return new TTransform(t, r, s, TransformOrder);
//        }
//        / <summary>
//        / Retrieves the transform at the requested frame second.
//        / </summary>
//        public unsafe TTransform GetTransform(float second)
//        {
//            GetTransform(out Vector3 t, out Quat r, out Vector3 s, second);
//            return new TTransform(t, r, s, TransformOrder);
//        }
//        / <summary>
//        / Retrieves the transform at the current frame second.
//        / </summary>
//        public unsafe (Vector3 translation, Quat rotation, Vector3 scale) GetTransformParts()
//        {
//            GetTransform(out Vector3 t, out Quat r, out Vector3 s);
//            return (t, r, s);
//        }
//        / <summary>
//        / Retrieves the transform at the requested frame second.
//        / </summary>
//        public unsafe (Vector3 translation, Quat rotation, Vector3 scale) GetTransformParts(float second)
//        {
//            GetTransform(out Vector3 t, out Quat r, out Vector3 s, second);
//            return (t, r, s);
//        }

//        / <summary>
//        / Clears all keyframes and sets tracks to the proper length.
//        / </summary>
//        public void ResetKeys()
//        {
//            ResetTrack(TranslationX);
//            ResetTrack(TranslationY);
//            ResetTrack(TranslationZ);
//            ResetTrack(ScaleX);
//            ResetTrack(ScaleY);
//            ResetTrack(ScaleZ);
//            ResetTrack(Rotation);
//        }

//        private void ResetTrack(PropAnimFloat track)
//        {
//            track.Keyframes.Clear();
//            track.SetLength(LengthInSeconds, false);
//        }
//        private void ResetTrack(PropAnimQuat track)
//        {
//            track.Keyframes.Clear();
//            track.SetLength(LengthInSeconds, false);
//        }

//        public override void ManualRead(SerializeElement node)
//        {
//            if (string.Equals(node.MemberInfo.Name, nameof(TransformKeyCollection), StringComparison.InvariantCulture))
//            {
//                LengthInSeconds = node.GetAttributeValue(nameof(LengthInSeconds), out float length) ? length : 0.0f;

//                ResetKeys();

//                foreach (SerializeElement targetTrackElement in node.Children)
//                {
//                    switch (targetTrackElement.Name)
//                    {
//                        case nameof(TranslationX):
//                            ReadTrack(targetTrackElement, TranslationX);
//                            break;
//                        case nameof(TranslationY):
//                            ReadTrack(targetTrackElement, TranslationY);
//                            break;
//                        case nameof(TranslationZ):
//                            ReadTrack(targetTrackElement, TranslationZ);
//                            break;
//                        case nameof(ScaleX):
//                            ReadTrack(targetTrackElement, ScaleX);
//                            break;
//                        case nameof(ScaleY):
//                            ReadTrack(targetTrackElement, ScaleY);
//                            break;
//                        case nameof(ScaleZ):
//                            ReadTrack(targetTrackElement, ScaleZ);
//                            break;
//                        case nameof(Rotation):
//                            ReadTrack(targetTrackElement, Rotation);
//                            break;
//                    }
//                }
//            }
//            else
//            {
//                LengthInSeconds = 0;
//                ResetKeys();
//            }
//        }

//        private static void ReadTrack(SerializeElement targetTrackElement, PropAnimFloat track)
//        {
//            if (!targetTrackElement.GetAttributeValue("Count", out int keyCount))
//                return;

//            track.Keyframes.Clear();

//            float[] seconds = null, inValues = null, outValues = null, inTans = null, outTans = null;
//            EVectorInterpType[] interpolation = null;

//            foreach (SerializeElement keyframePartElement in targetTrackElement.Children)
//            {
//                if (keyframePartElement.MemberInfo.Name == "Interpolation" && keyframePartElement.Content.GetObjectAs(out interpolation))
//                    continue;

//                if (!keyframePartElement.Content.GetObjectAs(out float[] floats))
//                    continue;
                
//                switch (keyframePartElement.Name)
//                {
//                    case "Second": seconds = floats; break;
//                    case "InValues": inValues = floats; break;
//                    case "OutValues": outValues = floats; break;
//                    case "InTangents": inTans = floats; break;
//                    case "OutTangents": outTans = floats; break;
//                }
//            }
//            for (int i = 0; i < keyCount; ++i)
//            {
//                FloatKeyframe kf = new FloatKeyframe(
//                    seconds[i],
//                    inValues[i],
//                    outValues[i],
//                    inTans[i],
//                    outTans[i],
//                    interpolation[i]);

//                track.Keyframes.Add(kf);
//            }
//        }
//        private static void ReadTrack(SerializeElement targetTrackElement, PropAnimQuat track)
//        {
//            if (!targetTrackElement.GetAttributeValue("Count", out int keyCount))
//                return;

//            track.Keyframes.Clear();

//            float[] seconds = null;
//            Quat[] inValues = null, outValues = null, inTans = null, outTans = null;
//            ERadialInterpType[] interpolation = null;

//            foreach (SerializeElement keyframePartElement in targetTrackElement.Children)
//            {
//                if (keyframePartElement.MemberInfo.Name == "Interpolation" && keyframePartElement.Content.GetObjectAs(out interpolation))
//                    continue;

//                if (keyframePartElement.MemberInfo.Name == "Second" && keyframePartElement.Content.GetObjectAs(out seconds))
//                    continue;

//                if (!keyframePartElement.Content.GetObjectAs(out Quat[] quats))
//                    continue;
                
//                switch (keyframePartElement.Name)
//                {
//                    case "InValues": inValues = quats; break;
//                    case "OutValues": outValues = quats; break;
//                    case "InTangents": inTans = quats; break;
//                    case "OutTangents": outTans = quats; break;
//                }
//            }
//            for (int i = 0; i < keyCount; ++i)
//            {
//                QuatKeyframe kf = new QuatKeyframe(
//                    seconds[i],
//                    inValues[i],
//                    outValues[i],
//                    inTans[i],
//                    outTans[i],
//                    interpolation[i]);

//                track.Keyframes.Add(kf);
//            }
//        }

//        public override void ManualWrite(SerializeElement node)
//        {
//            node.Name = nameof(TransformKeyCollection);
//            node.AddAttribute(nameof(LengthInSeconds), LengthInSeconds);
//            WriteTrack(node, nameof(TranslationX), TranslationX);
//            WriteTrack(node, nameof(TranslationY), TranslationX);
//            WriteTrack(node, nameof(TranslationZ), TranslationX);
//            WriteTrack(node, nameof(ScaleX), ScaleX);
//            WriteTrack(node, nameof(ScaleY), ScaleY);
//            WriteTrack(node, nameof(ScaleZ), ScaleZ);
//            WriteTrack(node, nameof(Rotation), Rotation);
//        }

//        private static void WriteTrack(SerializeElement node, string name, PropAnimFloat track)
//        {
//            if (track.Keyframes.Count <= 0)
//                return;
            
//            TODO: write combined, not separate
//            SerializeElement trackElement = new SerializeElement(null, new TSerializeMemberInfo(null, name));
//            trackElement.AddAttribute("Count", track.Keyframes.Count);
//            trackElement.AddChildElementObject("Second", track.Select(x => x.Second).ToArray());
//            trackElement.AddChildElementObject("InValues", track.Select(x => x.InValue).ToArray());
//            trackElement.AddChildElementObject("OutValues", track.Select(x => x.OutValue).ToArray());
//            trackElement.AddChildElementObject("InTangents", track.Select(x => x.InTangent).ToArray());
//            trackElement.AddChildElementObject("OutTangents", track.Select(x => x.OutTangent).ToArray());
//            trackElement.AddChildElementObject("Interpolation", track.Select(x => x.InterpolationType).ToArray());
//            node.Children.Add(trackElement);
//        }
//        private static void WriteTrack(SerializeElement node, string name, PropAnimQuat track)
//        {
//            if (track.Keyframes.Count <= 0)
//                return;

//            TODO: write combined, not separate
//            SerializeElement trackElement = new SerializeElement(null, new TSerializeMemberInfo(null, name));
//            trackElement.AddAttribute("Count", track.Keyframes.Count);
//            trackElement.AddChildElementObject("Second", track.Select(x => x.Second).ToArray());
//            trackElement.AddChildElementObject("InValues", track.Select(x => x.InValue).ToArray());
//            trackElement.AddChildElementObject("OutValues", track.Select(x => x.OutValue).ToArray());
//            trackElement.AddChildElementObject("InTangents", track.Select(x => x.InTangent).ToArray());
//            trackElement.AddChildElementObject("OutTangents", track.Select(x => x.OutTangent).ToArray());
//            trackElement.AddChildElementObject("Interpolation", track.Select(x => x.InterpolationType).ToArray());
//            node.Children.Add(trackElement);
//        }
//    }
//}
