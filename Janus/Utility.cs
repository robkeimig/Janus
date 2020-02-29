namespace Janus
{
    public class Utility
    {
        public static Tuple Point(float x, float y, float z) => new Tuple(x, y, z, 1.0f);

        public static Tuple Vector(float x, float y, float z) => new Tuple(x, y, z, 0.0f);
    }
}
