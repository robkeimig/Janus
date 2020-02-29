namespace Janus
{
    public struct Tuple
    {
        public Tuple (float x, float y, float z, float w)
        {
            X = x;
            Y = y;
            Z = z;
            W = w;
        }

        public float X;
        public float Y;
        public float Z;
        public float W;
        
        public bool IsPoint() => W == 1;

        public bool IsVector() => W == 0;
    }
}
