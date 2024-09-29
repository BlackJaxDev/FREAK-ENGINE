namespace XREngine.Data.Rendering
{
    //public struct IndexPoint
    //{
    //    public IndexPoint() { }
    //    public IndexPoint(int vertexIndex)
    //        => VertexIndex = vertexIndex;

    //    public int VertexIndex;

    //    public override readonly string ToString()
    //        => VertexIndex.ToString();

    //    public string WriteToString()
    //        => VertexIndex.ToString();

    //    public void ReadFromString(string str)
    //    {
    //        if (!int.TryParse(str, out VertexIndex))
    //            VertexIndex = 0;
    //    }

    //    public int GetSize()
    //        => sizeof(int);

    //    public void WriteToPointer(VoidPtr address)
    //        => address.WriteInt(VertexIndex, false);

    //    public void ReadFromPointer(VoidPtr address, int size)
    //        => VertexIndex = address.ReadInt(false);

    //    public static implicit operator IndexPoint(int i) => new(i);
    //    public static implicit operator int(IndexPoint i) => i.VertexIndex;
    //}
}
