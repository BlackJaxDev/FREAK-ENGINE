using XREngine.Data.Core;

namespace XREngine.Data.Rendering
{
    /// <summary>
    /// Describes how any number of bones affect a vertex when transformed.
    /// Contains no actual transformation information.
    /// </summary>
    public class VertexWeightGroup : XRBase
    {
        //TODO: use event dictionary. bone index -> weight

        private int _weightLimit = 0;
        private EventDictionary<int, float> _weights;
        private List<int>? _locked = null;

        /// <summary>
        /// The maximum number of weights that can be applied to a vertex.
        /// A value of 0 or less means no limit.
        /// </summary>
        public int WeightLimit
        {
            get => _weightLimit;
            set => SetField(ref _weightLimit, value);
        }

        /// <summary>
        /// The list of bone weights that affect the vertex.
        /// Key is bone index (into UtilizedBones list), value is the weight.
        /// </summary>
        public EventDictionary<int, float> Weights
        {
            get => _weights;
            set => SetField(ref _weights, value);
        }

        /// <summary>
        /// The indices of the weights that are locked and cannot be modified.
        /// </summary>
        public List<int>? Locked
        {
            get => _locked;
            set => SetField(ref _locked, value);
        }

        public VertexWeightGroup(int boneIndex, int weightLimit = -1)
        {
            _weightLimit = weightLimit;
            _weights = new EventDictionary<int, float> { { boneIndex, 1.0f } };
        }

        public VertexWeightGroup(IDictionary<int, float> weights, int weightLimit = -1)
        {
            _weightLimit = weightLimit;
            _weights = new EventDictionary<int, float>(weights);
        }

        /// <summary>
        /// Locks a weight at the specified index so it cannot be modified.
        /// </summary>
        /// <param name="index"></param>
        public void Lock(int index)
        {
            _locked ??= [];
            _locked.Add(index);
        }

        /// <summary>
        /// Unlocks a weight at the specified index so it can be modified.
        /// </summary>
        /// <param name="index"></param>
        public void Unlock(int index)
        {
            if (_locked is null)
                return;

            _locked.Remove(index);
            if (_locked.Count == 0)
                _locked = null;
        }

        /// <summary>
        /// Checks if a weight at the specified index is locked.
        /// </summary>
        /// <param name="index"></param>
        /// <returns></returns>
        public bool IsLocked(int index)
            => _locked?.Contains(index) ?? false;

        ///// <summary>
        ///// Reduces the number of weights down to the weight limit by removing the smallest weights and normalizing the rest.
        ///// </summary>
        //public void Optimize()
        //{
        //    if (WeightLimit < 0)
        //        return;

        //    if (Weights.Count > WeightLimit)
        //    {
        //        int[] toRemove = new int[Weights.Count - WeightLimit];
        //        for (int i = 0; i < toRemove.Length; ++i)
        //            for (int j = 0; j < Weights.Count; ++j)
        //                if (!toRemove.Contains(j + 1) &&
        //                    (toRemove[i] == 0 || Weights[j].Weight < Weights[toRemove[i] - 1].Weight))
        //                    toRemove[i] = j + 1;

        //        //TODO: update locked list with new indices
        //        foreach (int k in toRemove)
        //            Weights.RemoveAt(k - 1);
        //    }

        //    int count = Math.Min(Weights.Count, WeightLimit);
        //    BoneWeight[] optimized = new BoneWeight[count];
        //    for (int i = 0; i < Weights.Count; ++i)
        //        optimized[i] = Weights[i];

        //    Normalize();
        //}

        ///// <summary>
        ///// Makes sure all weights add up to 1.0f.
        ///// Does not modify any locked weights.
        ///// </summary>
        //public void Normalize(int weightDecimalPlaces = 7)
        //{
        //    float denom = 0.0f, num = 1.0f;
        //    for (int i = 0; i < Weights.Count; i++)
        //    {
        //        float bw = Weights[i].Weight;
        //        if (IsLocked(i))
        //            num -= bw;
        //        else
        //            denom += bw;
        //    }

        //    //Don't do anything if all weights are locked
        //    if (denom <= 0.0f || num <= 0.0f)
        //        return;

        //    for (int i = 0; i < Weights.Count; i++)
        //    {
        //        //Only normalize unlocked weights used in the calculation
        //        if (IsLocked(i))
        //            continue;

        //        BoneWeight b = Weights[i];
        //        Weights[i] = new BoneWeight(b.BoneIndex, MathF.Round(b.Weight / denom * num, weightDecimalPlaces));
        //    }
        //}

        public static bool operator ==(VertexWeightGroup left, VertexWeightGroup right)
        {
            if (left is null)
                return right is null;
            return left.Equals(right);
        }

        public static bool operator !=(VertexWeightGroup left, VertexWeightGroup right)
        {
            if (left is null)
                return right is not null;
            return !left.Equals(right);
        }

        public override bool Equals(object? obj)
        {
            if (obj is not VertexWeightGroup other ||
                Weights.Count != other.Weights.Count)
                return false;

            for (int i = 0; i < Weights.Count; ++i)
                if (Weights[i] != other.Weights[i])
                    return false;

            return true;
        }

        public override int GetHashCode()
            => Weights.GetHashCode();
    }
}